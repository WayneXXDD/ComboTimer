using System.Collections.Generic;

namespace WayneComboTimer;

internal static class ComboActions
{
    internal static bool HasNext(uint lastAction, byte playerLevel, IReadOnlyDictionary<uint, byte> nextLevels)
    {
        // ActionCombo can record a base action even when an upgraded or enchanted action was used.
        var lookupAction = lastAction switch
        {
            16479 => 75u,    // Raiden Thrust -> True Thrust
            25770 => 86u,    // Draconian Fury -> Doom Spike
            25771 => 84u,    // Heavens' Thrust -> Full Thrust
            25772 => 88u,    // Chaotic Spring -> Chaos Thrust
            36954 => 78u,    // Lance Barrage -> Vorpal Thrust
            36955 => 87u,    // Spiral Blow -> Disembowel
            7411 => 2866u,   // Heated Split Shot -> Split Shot
            7412 => 2868u,   // Heated Slug Shot -> Slug Shot
            25780 => 7483u,  // Fuko -> Fuga
            36963 => 7477u,  // Gyofu -> Hakaze
            7527 => 7504u,   // Enchanted Riposte -> Riposte
            7528 => 7512u,   // Enchanted Zwerchhau -> Zwerchhau
            45961 => 7512u,  // Alternate Enchanted Zwerchhau -> Zwerchhau
            _ => lastAction
        };

        return nextLevels.TryGetValue(lookupAction, out var nextLevel) && playerLevel >= nextLevel;
    }
}
