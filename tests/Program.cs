using System;
using WayneComboTimer;

static void Check(uint status, float seconds, float? expected)
{
    var actual = ViperStarter.Remaining(status, seconds);
    if (actual != expected)
        throw new Exception($"Status {status}, time {seconds}: expected {expected}, got {actual}.");
}

// A refresh first appears as a negative local duration, then receives server confirmation.
// Switching between the two enhancements must keep the display populated throughout.
Check(3672, 53.2f, 53.2f);
Check(3772, -60f, 60f);
Check(3772, 59.4f, 59.4f);
Check(3672, -60f, 60f);
Check(3672, 59.4f, 59.4f);

// Reading the duration has no 60-second cutoff and accepts subsecond durations.
Check(3672, 61f, 61f);
Check(3772, 90f, 90f);
Check(3772, -90f, 90f);
Check(3672, 0.01f, 0.01f);
Check(3772, -0.01f, 0.01f);

// Missing, expired, unrelated and non-finite status readings cannot create a timer.
Check(0, 60, null);
Check(3673, -60, null);
Check(3772, 0, null);
Check(3672, -0f, null);
Check(3672, float.NaN, null);
Check(3772, float.PositiveInfinity, null);
Check(3672, float.NegativeInfinity, null);
Console.WriteLine("Passed 17 Viper refresh and duration checks.");
