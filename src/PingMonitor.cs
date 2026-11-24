// Windows server discovery and aggregate ICMP measurements adapted from PingPlugin (MIT).
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace WayneComboTimer;

internal sealed class PingMonitor : IDisposable
{
    internal sealed record Reading(string Address, double? Last, double Average, double[] History,
        bool UsingDataCenter = false, string? Error = null);
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;
    private Reading reading = new("", null, 0, []);
    private volatile bool active;
    private uint dataCenterId;
    private int disposed;
    internal int QueueSize = 20;
    internal int TrackingMode = 0;
    internal Reading Current => Volatile.Read(ref reading);

    internal PingMonitor() => worker = Task.Run(Loop);
    internal void SetActive(bool value, uint currentDataCenterId)
    {
        Volatile.Write(ref dataCenterId, currentDataCenterId);
        active = value;
    }

    private async Task Loop()
    {
        IPAddress? address = null;
        bool usingDataCenter = false;
        uint lastDataCenterId = uint.MaxValue;
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
                    lastDataCenterId = uint.MaxValue;
                    Volatile.Write(ref reading, new("", null, 0, []));
                    await Task.Delay(500, token);
                    continue;
                }
                var currentDataCenterId = Volatile.Read(ref dataCenterId);
                if (passes++ % 3 == 0 || address == null || currentDataCenterId != lastDataCenterId)
                {
                    IPAddress? detected = null;
                    // TCP discovery can be unavailable with a VPN, proxy, Wine or restricted OS API.
                    try { detected = FindServer(Environment.ProcessId); }
                    catch (Exception) { }
                    var found = SelectAddress(detected, currentDataCenterId);
                    if (!Equals(found, address)) history.Clear();
                    address = found;
                    usingDataCenter = address != null && !IsUsableAddress(detected);
                    lastDataCenterId = currentDataCenterId;
                }
                double? last = null;
                if (address != null)
                {
                    int mode = Volatile.Read(ref TrackingMode);
                    var managed = mode == 2 ? Task.FromResult<double?>(null) :
                        TryMeasurement(() => ManagedPing(address, token), token);
                    var native = mode == 1 ? Task.FromResult<double?>(null) :
                        TryMeasurement(() => Task.Run(() => HopPing(address), token), token);
                    await Task.WhenAll(managed, native);
                    last = mode == 2 ? native.Result : mode == 1 ? managed.Result :
                        Choose(managed.Result, native.Result);
                }
                if (active && !token.IsCancellationRequested)
                {
                    if (last.HasValue) history.Enqueue(last.Value);
                    while (history.Count > Math.Clamp(Volatile.Read(ref QueueSize), 1, 300)) history.Dequeue();
                    Volatile.Write(ref reading, new(address?.ToString() ?? "", last,
                        history.Count == 0 ? 0 : history.Average(), history.ToArray(), usingDataCenter,
                        address == null ? "無法辨識目前的遊戲或資料中心位址。" :
                        last.HasValue ? null : "未收到延遲測量回應，將自動重試。"));
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception)
            {
                // Retain the address and successful history after a transient measurement failure.
                if (active && !token.IsCancellationRequested)
                    Volatile.Write(ref reading, new(address?.ToString() ?? "", null,
                        history.Count == 0 ? 0 : history.Average(), history.ToArray(), usingDataCenter,
                        "本次延遲測量失敗，將自動重試。"));
                passes = 0;
            }
            try { await Task.Delay(3000, token); }
            catch (OperationCanceledException) { break; }
        }
    }

    internal static double? Choose(double? managed, double? native)
        => native.HasValue ? managed.HasValue && Math.Abs(managed.Value - native.Value) > 30
            ? Math.Max(managed.Value, native.Value) : native : managed;

    internal static async Task<double?> TryMeasurement(Func<Task<double?>> measure, CancellationToken token)
    {
        try { return await measure().WaitAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception) { return null; }
    }

    private static async Task<double?> ManagedPing(IPAddress address, CancellationToken token)
    {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(address, 1500).WaitAsync(token);
        return reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
    }

    private static double? HopPing(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4) return null;
        uint hops = 0, rtt = 0;
        return GetRTTAndHopCount(BitConverter.ToUInt32(bytes), ref hops, 51, ref rtt) == 1 ? rtt : null;
    }

    private static bool IsUsableAddress(IPAddress? address)
        => address != null && !IPAddress.IsLoopback(address) &&
            !address.Equals(IPAddress.Any) && !address.Equals(IPAddress.IPv6Any);

    internal static IPAddress? SelectAddress(IPAddress? detected, uint currentDataCenterId)
        => IsUsableAddress(detected) ? detected : DataCenterAddress(currentDataCenterId);

    // Same lobby-address fallback as PingPlugin's ClientStateAddressDetector.
    // These are DC endpoints, not a claim that a forwarded world connection was discovered.
    internal static IPAddress? DataCenterAddress(uint id)
    {
        var address = id switch
        {
            1 => "119.252.36.6",    // Elemental
            2 => "119.252.36.7",    // Gaia
            3 => "119.252.36.8",    // Mana
            4 => "204.2.29.6",     // Aether
            5 => "204.2.29.7",     // Primal
            6 => "80.239.145.6",   // Chaos
            7 => "80.239.145.7",   // Light
            8 => "204.2.29.8",     // Crystal
            9 => "153.254.80.103", // Materia
            10 => "119.252.36.9",  // Meteor
            11 => "204.2.29.9",    // Dynamis
            12 => "80.239.145.8",  // Shadow
            _ => null
        };
        return address is null ? null : IPAddress.Parse(address);
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
