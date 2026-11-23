using System;
using System.Collections.Generic;
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

static void CheckCombo(uint action, byte level, bool expected)
{
    // Base-action predecessors from the current game data, plus Simple Tweaks' two exceptions.
    var nextLevels = new Dictionary<uint, byte>
    {
        [75] = 4, [78] = 26, [84] = 56, [87] = 1, [88] = 0,
        [86] = 62, [7397] = 72, [3554] = 64, [3556] = 64, [7526] = 80
    };
    var actual = ComboActions.HasNext(action, level, nextLevels);
    if (actual != expected)
        throw new Exception($"Combo after {action}, level {level}: expected {expected}, got {actual}.");
}

// All six upgraded steps were absent from ActionCombo's predecessor column.
CheckCombo(16479, 100, true);
CheckCombo(25770, 100, true);
CheckCombo(25771, 100, true);
CheckCombo(25772, 100, true);
CheckCombo(36954, 100, true);
CheckCombo(36955, 100, true);

// Retain level-sync limits, both fourth steps and the end of each finished combo.
CheckCombo(75, 3, false);
CheckCombo(75, 4, true);
CheckCombo(84, 55, false);
CheckCombo(84, 56, true);
CheckCombo(3554, 63, false);
CheckCombo(3554, 64, true);
CheckCombo(3556, 63, false);
CheckCombo(3556, 64, true);
CheckCombo(36952, 100, false);
CheckCombo(16477, 100, false);

// Other actions keep the original table lookup and level check.
CheckCombo(7526, 79, false);
CheckCombo(7526, 80, true);
CheckCombo(0, 100, false);
Console.WriteLine("Passed 19 standard combo availability checks.");
