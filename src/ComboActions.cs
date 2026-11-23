using System.Collections.Generic;

namespace WayneComboTimer;

internal static class ComboActions
{
    internal static bool HasNext(uint lastAction, byte playerLevel, IReadOnlyDictionary<uint, byte> nextLevels)
    {
        // ActionCombo records the base action even when an upgraded Dragoon action was used.
        var lookupAction = lastAction switch
        {
            16479 => 75u,    // Raiden Thrust -> True Thrust
            25770 => 86u,    // Draconian Fury -> Doom Spike
            25771 => 84u,    // Heavens' Thrust -> Full Thrust
            25772 => 88u,    // Chaotic Spring -> Chaos Thrust
            36954 => 78u,    // Lance Barrage -> Vorpal Thrust
            36955 => 87u,    // Spiral Blow -> Disembowel
            _ => lastAction
        };

        return nextLevels.TryGetValue(lookupAction, out var nextLevel) && playerLevel >= nextLevel;
    }
}
