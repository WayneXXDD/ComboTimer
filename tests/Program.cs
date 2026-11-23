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
        [75] = 4, [78] = 26, [84] = 56, [87] = 50, [88] = 58,
        [86] = 62, [7397] = 72, [3554] = 64, [3556] = 64, [7526] = 80,
        [9] = 4, [15] = 26, [7381] = 40, [16459] = 90, [25748] = 90, [25749] = 90,
        [31] = 4, [37] = 26, [41] = 40,
        [3617] = 2, [3623] = 26, [3621] = 40, [36928] = 96, [36929] = 96,
        [16137] = 4, [16139] = 26, [16141] = 40, [16146] = 60, [16147] = 60,
        [36937] = 100, [36938] = 100,
        [2240] = 4, [2242] = 26, [2254] = 52,
        [2866] = 2, [2868] = 26,
        [7477] = 4, [7478] = 30, [7479] = 40, [7483] = 35,
        [7504] = 35, [7512] = 50, [7525] = 80, [16530] = 90, [7530] = 52, [37002] = 52,
        [15989] = 2, [15993] = 25, [24373] = 5, [24374] = 30, [24376] = 45,
        [34678] = 86, [34679] = 86, [44879] = 2, [44883] = 12
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
// Upgraded Machinist and Samurai starters and Red Mage's enchanted sword actions.
CheckCombo(7411, 100, true);
CheckCombo(7412, 100, true);
CheckCombo(7413, 100, false);
CheckCombo(25780, 100, true);
CheckCombo(36963, 100, true);
CheckCombo(7481, 100, false);
CheckCombo(7527, 34, false);
CheckCombo(7527, 35, true);
CheckCombo(7528, 49, false);
CheckCombo(7528, 50, true);
CheckCombo(45961, 50, true);
CheckCombo(7529, 100, false);
CheckCombo(7530, 52, true);
CheckCombo(37002, 52, true);
CheckCombo(37003, 100, false);
CheckCombo(7525, 79, false);
CheckCombo(7525, 80, true);
CheckCombo(16530, 89, false);
CheckCombo(16530, 90, true);
CheckCombo(25858, 100, false);

// Class/job changes and level sync retain the next action's actual unlock level.
CheckCombo(9, 3, false);
CheckCombo(9, 4, true);
CheckCombo(15, 25, false);
CheckCombo(15, 26, true);
CheckCombo(31, 3, false);
CheckCombo(31, 4, true);
CheckCombo(87, 49, false);
CheckCombo(87, 50, true);
CheckCombo(88, 57, false);
CheckCombo(88, 58, true);
CheckCombo(2242, 25, false);
CheckCombo(2242, 26, true);
CheckCombo(3617, 1, false);
CheckCombo(3617, 2, true);
CheckCombo(3623, 25, false);
CheckCombo(3623, 26, true);
CheckCombo(7477, 3, false);
CheckCombo(7477, 4, true);
CheckCombo(7478, 29, false);
CheckCombo(7478, 30, true);
CheckCombo(7479, 39, false);
CheckCombo(7479, 40, true);
CheckCombo(7483, 34, false);
CheckCombo(7483, 35, true);

// Cover basic, area and secondary native combos across every other supported PvE job.
CheckCombo(7381, 40, true);
CheckCombo(16459, 90, true);
CheckCombo(25748, 90, true);
CheckCombo(25749, 90, true);
CheckCombo(25750, 100, false);
CheckCombo(37, 26, true);
CheckCombo(41, 40, true);
CheckCombo(45, 100, false);
CheckCombo(3621, 40, true);
CheckCombo(36928, 96, true);
CheckCombo(36929, 96, true);
CheckCombo(36930, 100, false);
CheckCombo(16137, 4, true);
CheckCombo(16139, 26, true);
CheckCombo(16141, 40, true);
CheckCombo(16146, 60, true);
CheckCombo(16147, 60, true);
CheckCombo(36937, 100, true);
CheckCombo(36938, 100, true);
CheckCombo(36939, 100, false);
CheckCombo(2240, 4, true);
CheckCombo(2254, 52, true);
CheckCombo(15989, 2, true);
CheckCombo(15993, 25, true);
CheckCombo(15994, 100, false);
CheckCombo(24373, 5, true);
CheckCombo(24374, 30, true);
CheckCombo(24376, 45, true);
CheckCombo(24377, 100, false);
CheckCombo(34678, 86, true);
CheckCombo(34679, 86, true);
CheckCombo(34680, 100, false);
CheckCombo(44879, 2, true);
CheckCombo(44883, 12, true);
CheckCombo(44885, 100, false);
Console.WriteLine("Passed 98 standard combo availability checks across all native PvE combo jobs.");
