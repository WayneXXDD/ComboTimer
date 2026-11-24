// Windows server discovery and aggregate ICMP measurements adapted from PingPlugin (MIT).
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace WayneComboTimer;

internal sealed class PingMonitor : IDisposable
{
    internal sealed record Reading(string Address, double? Last, double Average, double[] History);
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;
    private Reading reading = new("", null, 0, []);
    private volatile bool active;
    private int disposed;
    internal int QueueSize = 20;
    internal int TrackingMode = 0;
    internal Reading Current => Volatile.Read(ref reading);

    internal PingMonitor() => worker = Task.Run(Loop);
    internal void SetActive(bool value) => active = value;

    private async Task Loop()
    {
        IPAddress? address = null;
        var history = new Queue<double>();
        int passes = 0;
        var token = cancellation.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!active)
                {
                    address = null; passes = 0; history.Clear();
                    Volatile.Write(ref reading, new("", null, 0, []));
                    await Task.Delay(500, token);
                    continue;
                }
                if (passes++ % 3 == 0)
                {
                    var found = FindServer(Environment.ProcessId);
                    if (!Equals(found, address)) history.Clear();
                    address = found;
                }
                double? last = null;
                if (address != null)
                {
                    var managed = ManagedPing(address, token);
                    var native = TrackingMode == 1 ? Task.FromResult<double?>(null) :
                        Task.Run(() => HopPing(address), token);
                    var managedResult = await managed;
                    var nativeResult = await native.WaitAsync(token);
                    last = TrackingMode == 2 ? nativeResult :
                        TrackingMode == 1 ? managedResult : Choose(managedResult, nativeResult);
                }
                if (active && !token.IsCancellationRequested)
                {
                    if (last.HasValue)
                    {
                        history.Enqueue(last.Value);
                        while (history.Count > Math.Clamp(QueueSize, 1, 300)) history.Dequeue();
                    }
                    Volatile.Write(ref reading, new(address?.ToString() ?? "", last,
                        history.Count == 0 ? 0 : history.Average(), history.ToArray()));
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception)
            {
                Volatile.Write(ref reading, new("", null, 0, []));
                address = null; passes = 0; history.Clear();
            }
            try { await Task.Delay(3000, token); }
            catch (OperationCanceledException) { break; }
        }
    }

    internal static double? Choose(double? managed, double? native)
        => native.HasValue ? managed.HasValue && Math.Abs(managed.Value - native.Value) > 30
            ? Math.Max(managed.Value, native.Value) : native : managed;

    private static async Task<double?> ManagedPing(IPAddress address, CancellationToken token)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(address, 1500).WaitAsync(token);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
        }
        catch (PingException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    private static double? HopPing(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4) return null;
        uint hops = 0, rtt = 0;
        return GetRTTAndHopCount(BitConverter.ToUInt32(bytes), ref hops, 51, ref rtt) == 1 ? rtt : null;
    }

    internal static bool IsGamePort(ushort port)
        => port is >= 54992 and <= 54994 or >= 55006 and <= 55007 or >= 55021 and <= 55040 or >= 55296 and <= 55551;

    internal static IPAddress? FindServer(int processId)
    {
        int size = 0;
        var error = GetExtendedTcpTable(0, ref size, false, 2, 4, 0);
        if (error != 122 || size < 4) return null;
        // The table can change between calls. Retry with a resized allocation.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var capacity = size;
            var memory = Marshal.AllocHGlobal(capacity);
            try
            {
                error = GetExtendedTcpTable(memory, ref size, false, 2, 4, 0);
                if (error == 122) continue;
                if (error != 0) return null;
                int rowSize = Marshal.SizeOf<TcpRow>();
                int count = Math.Min(Marshal.ReadInt32(memory), (capacity - 4) / rowSize);
                for (int i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<TcpRow>(memory + 4 + i * rowSize);
                    var port = (ushort)(((row.RemotePort & 255) << 8) | ((row.RemotePort >> 8) & 255));
                    if (row.ProcessId != processId || row.State != 5 || !IsGamePort(port)) continue;
                    var address = new IPAddress(row.RemoteAddress);
                    if (!IPAddress.IsLoopback(address) && !address.Equals(IPAddress.Any)) return address;
                }
                return null;
            }
            finally { Marshal.FreeHGlobal(memory); }
        }
        return null;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        active = false;
        cancellation.Cancel();
        // Do not block plugin unload on an operating-system network call.
        _ = worker.ContinueWith(_ => cancellation.Dispose(), TaskScheduler.Default);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TcpRow
    {
        public uint State, LocalAddress, LocalPort, RemoteAddress, RemotePort, ProcessId;
    }

    [DllImport("iphlpapi.dll")]
    private static extern uint GetExtendedTcpTable(nint buffer, ref int size, [MarshalAs(UnmanagedType.Bool)] bool sort,
        int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll")]
    private static extern int GetRTTAndHopCount(uint address, ref uint hops, uint maxHops, ref uint rtt);
}
