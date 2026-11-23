using System;
using WayneComboTimer;

static void Check(uint status, uint displayedAction, bool glow, float seconds, float? expected)
{
    var actual = ViperStarter.Remaining(ViperStarter.FromStatus(status), displayedAction, glow, seconds);
    if (actual != expected)
        throw new Exception($"Status {status}, displayed action {displayedAction}, glow {glow}, time {seconds}: expected {expected}, got {actual}.");
}

Check(3672, 34606, true, 18.25f, 18.25f);
Check(3772, 34607, true, 43.75f, 43.75f);
Check(3672, 34607, true, 25, null);
Check(3772, 34606, true, 25, null);
Check(3672, 34608, true, 31, null);
Check(3772, 34609, true, 29, null);
Check(3672, 34606, false, 12, null);
Check(3772, 34607, false, 12, null);
Check(3672, 34614, true, 12, null);
Check(3772, 34615, true, 12, null);
Check(0, 0, true, 30, null);
Check(3772, 34607, true, 0, null);
Check(3672, 34606, true, float.NaN, null);
Console.WriteLine("Passed 13 Viper starter timer checks.");
