namespace Headunit.Core.Playback;

/// <summary>
/// Stereo balance: centre is unity on both sides, and moving away from centre
/// only ever <em>attenuates</em> the opposite channel — it never boosts, so
/// balance can't push a loud master into clipping.
/// </summary>
public static class BalanceLaw
{
    /// <param name="balance">-1 is hard left, 0 centre, +1 hard right.</param>
    public static (float Left, float Right) Gains(float balance)
    {
        var b = Math.Clamp(balance, -1f, 1f);
        return (b > 0 ? 1f - b : 1f, b < 0 ? 1f + b : 1f);
    }
}
