// =============================================================================
//  Utils/FpsCounter.cs
//  Rolling-window FPS calculator.
//  Keeps the last N frame timestamps and divides to get a smooth average.
// =============================================================================

namespace BodyTracker.Utils;

public sealed class FpsCounter
{
    private readonly Queue<long> _timestamps;
    private readonly int         _windowSize;
    private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();

    /// <param name="windowSize">
    ///   How many recent frames to average over.
    ///   Larger = smoother number; smaller = more responsive to speed changes.
    /// </param>
    public FpsCounter(int windowSize = 30)
    {
        _windowSize = windowSize;
        _timestamps = new Queue<long>(windowSize + 1);
    }

    /// <summary>Call once per rendered frame.  Returns the current FPS estimate.</summary>
    public double Tick()
    {
        long now = _sw.ElapsedMilliseconds;
        _timestamps.Enqueue(now);

        // Keep only the last _windowSize+1 entries
        while (_timestamps.Count > _windowSize + 1)
            _timestamps.Dequeue();

        if (_timestamps.Count < 2) return 0;

        long span = now - _timestamps.Peek();    // ms over the window
        return span > 0 ? (_timestamps.Count - 1) * 1000.0 / span : 0;
    }

    /// <summary>Elapsed milliseconds since the counter was created.</summary>
    public long ElapsedMs => _sw.ElapsedMilliseconds;
}
