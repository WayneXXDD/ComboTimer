namespace WayneComboTimer;

internal static class ViperStarter
{
    internal const uint SteelFangs = 34606, ReavingFangs = 34607;
    internal const uint HonedSteel = 3672, HonedReavers = 3772;

    internal static uint FromStatus(uint statusId) => statusId switch
    {
        HonedSteel => SteelFangs,
        HonedReavers => ReavingFangs,
        _ => 0u
    };

    internal static float? Remaining(uint starter, uint effectiveAction, bool highlighted, float seconds)
        => starter is SteelFangs or ReavingFangs && starter == effectiveAction && highlighted &&
           float.IsFinite(seconds) && seconds > 0 ? seconds : null;
}
