namespace Headunit.Core.Playback;

/// <summary>
/// One playable item. At M1 the metadata is just the file name; real tag
/// reading arrives with the library in M4, which is why <see cref="Duration"/>
/// is nullable — until a decoder opens the file we genuinely do not know it.
/// </summary>
public sealed record Track(string Path, string Title, TimeSpan? Duration = null)
{
    public static Track FromFile(string path)
        => new(path, System.IO.Path.GetFileNameWithoutExtension(path));

    public Track WithDuration(TimeSpan duration) => this with { Duration = duration };
}

public static class AudioFormats
{
    /// <summary>Extensions we will attempt to open. Decoding lives in the platform layer.</summary>
    public static readonly IReadOnlySet<string> SupportedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".m4a", ".aac", ".wav", ".wma", ".flac", ".ogg", ".oga", ".opus",
        };

    public static bool IsSupported(string path)
        => SupportedExtensions.Contains(System.IO.Path.GetExtension(path));
}
