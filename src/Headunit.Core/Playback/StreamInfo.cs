namespace Headunit.Core.Playback;

/// <summary>What the LCD's kbps / kHz / stereo readouts show for the loaded file.</summary>
public sealed record StreamInfo(int SampleRate, int Channels, int? Kbps)
{
    public bool IsStereo => Channels >= 2;

    /// <summary>Sample rate as the LCD shows it: 44100 → 44, 48000 → 48, 22050 → 22.</summary>
    public int Khz => (int)Math.Round(SampleRate / 1000.0, MidpointRounding.ToZero);

    /// <summary>
    /// Average bitrate from file size and duration. For VBR files this is the
    /// honest figure — the same one a file manager shows — and it costs no
    /// decoding. Null when the duration isn't known yet.
    /// </summary>
    public static int? EstimateKbps(long fileBytes, TimeSpan duration)
    {
        if (fileBytes <= 0 || duration <= TimeSpan.Zero)
        {
            return null;
        }

        var kbps = fileBytes * 8 / duration.TotalSeconds / 1000.0;
        return (int)Math.Clamp(Math.Round(kbps), 1, 9999);
    }
}
