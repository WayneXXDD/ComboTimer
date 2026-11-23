namespace WayneComboTimer;

internal static class ViperStarter
{
    internal const uint HonedSteel = 3672;
    internal const uint HonedReavers = 3772;

    internal static float? Remaining(uint statusId, float seconds)
        => statusId is HonedSteel or HonedReavers && float.IsFinite(seconds) && seconds > 0
            ? seconds
            : null;
}
