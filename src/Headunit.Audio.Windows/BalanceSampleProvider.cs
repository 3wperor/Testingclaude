using Headunit.Core.Playback;
using NAudio.Wave;

namespace Headunit.Audio;

/// <summary>
/// Applies <see cref="BalanceLaw"/> to an interleaved stereo stream. Runs on
/// the audio thread: no allocation, no locks — the balance is a single volatile
/// float the UI writes and this reads once per buffer.
/// </summary>
internal sealed class BalanceSampleProvider(ISampleProvider source) : ISampleProvider
{
    private volatile float _balance;

    public WaveFormat WaveFormat => source.WaveFormat;

    public float Balance
    {
        get => _balance;
        set => _balance = Math.Clamp(value, -1f, 1f);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var read = source.Read(buffer, offset, count);
        if (source.WaveFormat.Channels != 2)
        {
            return read;
        }

        var (left, right) = BalanceLaw.Gains(_balance);
        if (left == 1f && right == 1f)
        {
            return read;
        }

        for (var i = offset; i < offset + read - 1; i += 2)
        {
            buffer[i] *= left;
            buffer[i + 1] *= right;
        }

        return read;
    }
}
