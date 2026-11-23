namespace WayneComboTimer;

internal static class ViperStarter
{
    internal const uint HonedSteel = 3672;
    internal const uint HonedReavers = 3772;

    internal static float? Remaining(uint statusId, float seconds)
    {
        if (statusId is not (HonedSteel or HonedReavers) || !float.IsFinite(seconds))
            return null;

        // Self-applied statuses use a negative duration until the server's EffectResult arrives.
        var remaining = MathF.Abs(seconds);
        return remaining > 0 ? remaining : null;
    }
}
