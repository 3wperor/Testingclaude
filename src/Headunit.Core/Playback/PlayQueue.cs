namespace Headunit.Core.Playback;

public enum RepeatMode
{
    Off,
    All,
    One,
}

/// <summary>
/// The ordered list of tracks and the cursor into it.
/// <para>
/// Shuffle is modelled as a permutation of positions rather than as a random
/// jump on every advance. That is the difference between "shuffle" and "random":
/// a permutation plays every track once before any track repeats, and it makes
/// <em>previous</em> mean the track you actually just heard.
/// </para>
/// </summary>
public sealed class PlayQueue
{
    private readonly List<Track> _tracks = [];
    private readonly List<int> _order = [];
    private readonly Random _random;
    private int _position = -1;
    private bool _shuffle;

    public PlayQueue(Random? random = null) => _random = random ?? new Random();

    public IReadOnlyList<Track> Tracks => _tracks;

    public int Count => _tracks.Count;

    /// <summary>Index into <see cref="Tracks"/>, or -1 when nothing is cued.</summary>
    public int CurrentIndex => _position >= 0 && _position < _order.Count ? _order[_position] : -1;

    public Track? Current => CurrentIndex >= 0 ? _tracks[CurrentIndex] : null;

    public RepeatMode Repeat { get; set; } = RepeatMode.Off;

    public bool Shuffle
    {
        get => _shuffle;
        set
        {
            if (_shuffle == value)
            {
                return;
            }

            _shuffle = value;
            RebuildOrderKeepingCurrent();
        }
    }

    public void Replace(IEnumerable<Track> tracks)
    {
        _tracks.Clear();
        _tracks.AddRange(tracks);
        _position = _tracks.Count > 0 ? 0 : -1;
        RebuildOrder();
    }

    public void Append(IEnumerable<Track> tracks)
    {
        var current = CurrentIndex;
        _tracks.AddRange(tracks);
        RebuildOrder();

        if (current >= 0)
        {
            _position = _order.IndexOf(current);
        }
        else if (_tracks.Count > 0)
        {
            _position = 0;
        }
    }

    public void Clear()
    {
        _tracks.Clear();
        _order.Clear();
        _position = -1;
    }

    public bool MoveTo(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= _tracks.Count)
        {
            return false;
        }

        _position = _order.IndexOf(trackIndex);
        return true;
    }

    /// <summary>
    /// Advances the cursor.
    /// <paramref name="userInitiated"/> distinguishes pressing Next from a track
    /// ending on its own: repeat-one loops forever on its own, but must still
    /// let the user escape by pressing Next.
    /// </summary>
    public bool MoveNext(bool userInitiated)
    {
        if (_order.Count == 0)
        {
            return false;
        }

        if (Repeat == RepeatMode.One && !userInitiated)
        {
            return true;
        }

        if (_position + 1 < _order.Count)
        {
            _position++;
            return true;
        }

        if (Repeat == RepeatMode.All)
        {
            if (_shuffle)
            {
                RebuildOrder();
            }

            _position = 0;
            return true;
        }

        return false;
    }

    public bool MovePrevious()
    {
        if (_order.Count == 0)
        {
            return false;
        }

        if (_position > 0)
        {
            _position--;
            return true;
        }

        if (Repeat == RepeatMode.All)
        {
            _position = _order.Count - 1;
            return true;
        }

        return false;
    }

    private void RebuildOrder()
    {
        _order.Clear();
        for (var i = 0; i < _tracks.Count; i++)
        {
            _order.Add(i);
        }

        if (_shuffle)
        {
            // Fisher-Yates, seeded from the injected Random so tests are deterministic.
            for (var i = _order.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }

        if (_position >= _order.Count)
        {
            _position = _order.Count - 1;
        }
    }

    /// <summary>
    /// Reorders without interrupting playback: whatever is playing stays
    /// playing, and becomes the head of the new order.
    /// </summary>
    private void RebuildOrderKeepingCurrent()
    {
        var current = CurrentIndex;
        RebuildOrder();

        if (current < 0)
        {
            return;
        }

        if (_shuffle)
        {
            // Shuffling on: the playing track becomes the head of the new order,
            // so the rest of the shuffle unfolds from here.
            var at = _order.IndexOf(current);
            if (at > 0)
            {
                _order.RemoveAt(at);
                _order.Insert(0, current);
            }

            _position = 0;
        }
        else
        {
            // Shuffling off: the order is natural again, so resume from where
            // this track actually sits in the album, not from the top.
            _position = _order.IndexOf(current);
        }
    }
}
