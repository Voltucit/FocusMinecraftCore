using System.Diagnostics;

namespace FMCore.Downloads;

public static partial class FileDownloader
{
    internal sealed class TransferMeter
    {
        private const double WindowSeconds = 5;

        private readonly Stopwatch _timer =
            Stopwatch.StartNew();

        private readonly Queue<(double Seconds, long Bytes)>
            _samples = new();

        private (double Seconds, long Bytes) _baseline;

        private double _lastSampleSeconds;

        public (double Speed, TimeSpan? Remaining) Measure(
            long transferredBytes,
            long remainingBytes)
        {
            double now = _timer.Elapsed.TotalSeconds;
            double cutoff = now - WindowSeconds;

            while (_samples.TryPeek(out var sample)
                   && sample.Seconds <= cutoff)
            {
                _baseline = _samples.Dequeue();
            }

            double elapsed = Math.Min(
                WindowSeconds,
                now - _baseline.Seconds);

            double speed = elapsed > 0
                ? (transferredBytes - _baseline.Bytes) / elapsed
                : 0;

            // 每隔约 250 毫秒记录一次，避免每个数据块都入队。
            if (now - _lastSampleSeconds >= 0.25)
            {
                _samples.Enqueue((now, transferredBytes));
                _lastSampleSeconds = now;
            }

            if (remainingBytes <= 0)
                return (speed, TimeSpan.Zero);

            if (speed <= 0)
                return (speed, null);

            double remainingSeconds =
                remainingBytes / speed;

            if (!double.IsFinite(remainingSeconds)
                || remainingSeconds
                >= TimeSpan.MaxValue.TotalSeconds)
            {
                return (speed, null);
            }

            return (
                speed,
                TimeSpan.FromSeconds(remainingSeconds));
        }
    }
}

