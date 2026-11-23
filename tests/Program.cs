using System;
using WayneComboTimer;

static void Check(uint status, float seconds, float? expected)
{
    var actual = ViperStarter.Remaining(status, seconds);
    if (actual != expected)
        throw new Exception($"Status {status}, time {seconds}: expected {expected}, got {actual}.");
}

// Both enhancements continue to count down independently of the current hotbar action.
Check(3672, 18.25f, 18.25f);
Check(3772, 43.75f, 43.75f);
Check(3672, 60f, 60f);
Check(3772, 0.01f, 0.01f);
Check(0, 30, null);
Check(3673, 30, null);
Check(3772, 0, null);
Check(3672, -1, null);
Check(3672, float.NaN, null);
Check(3772, float.PositiveInfinity, null);
Console.WriteLine("Passed 10 Viper enhancement timer checks.");
