using System;
using System.Collections.Generic;

namespace RiftEventCapture.Common;

public class BeatData {
    public int BPM { get; }
    public int BeatDivisions { get; }
    public IReadOnlyList<double> BeatTimings => beatTimings;

    private readonly double[] beatTimings;

    public BeatData(int bpm, int beatDivisions, double[] beatTimings) {
        BPM = bpm;
        BeatDivisions = beatDivisions;
        this.beatTimings = beatTimings;
    }

    public double GetBeatFromTime(double time) {
        if (double.IsPositiveInfinity(time))
            return double.PositiveInfinity;

        if (double.IsNegativeInfinity(time))
            return double.NegativeInfinity;

        if (beatTimings.Length <= 1)
            return time / (60d / Math.Max(1, BPM)) + 1d;

        int beatIndex = GetBeatIndexFromTime(time);
        double previous = beatTimings[beatIndex];
        double next = beatTimings[beatIndex + 1];

        return beatIndex + 1 + (time - previous) / (next - previous);
    }

    public double GetTimeFromBeat(double beat) {
        if (double.IsPositiveInfinity(beat))
            return double.PositiveInfinity;

        if (double.IsNegativeInfinity(beat))
            return double.NegativeInfinity;

        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1d);

        if (beat <= 1d) {
            double first = beatTimings[0];
            double second = beatTimings[1];

            return first - (second - first) * (1d - beat);
        }

        if (beat < beatTimings.Length) {
            double previous = beatTimings[(int) beat - 1];
            double next = beatTimings[(int) beat];

            return previous + (next - previous) * (beat % 1d);
        }

        double last = beatTimings[beatTimings.Length - 1];
        double secondToLast = beatTimings[beatTimings.Length - 2];

        return last + (last - secondToLast) * (beat - beatTimings.Length);
    }

    public double GetTimeFromBeat(int beat) {
        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1);

        if (beat < 1) {
            double first = beatTimings[0];
            double second = beatTimings[1];

            return first - (second - first) * (1 - beat);
        }

        if (beat <= beatTimings.Length)
            return beatTimings[beat - 1];

        double last = beatTimings[beatTimings.Length - 1];
        double secondToLast = beatTimings[beatTimings.Length - 2];

        return last + (last - secondToLast) * (beat - beatTimings.Length);
    }

    public Timestamp GetTimestampFromTime(double time) => new(time, GetBeatFromTime(time));

    public Timestamp GetTimestampFromBeat(double beat) => new(GetTimeFromBeat(beat), beat);

    public double GetBeatLengthAtTime(double time) {
        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM);

        int beatIndex = GetBeatIndexFromTime(time);

        return beatTimings[beatIndex + 1] - beatTimings[beatIndex];
    }

    public double GetBeatLengthForBeat(int beat) {
        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM);

        if (beat < 1)
            return beatTimings[1] - beatTimings[0];

        if (beat < beatTimings.Length)
            return beatTimings[beat] - beatTimings[beat - 1];

        return beatTimings[beatTimings.Length - 1] - beatTimings[beatTimings.Length - 2];
    }

    private int GetBeatIndexFromTime(double time) {
        int min = 0;
        int max = beatTimings.Length - 1;

        while (max >= min) {
            int mid = (min + max) / 2;

            if (beatTimings[mid] > time)
                max = mid - 1;
            else
                min = mid + 1;
        }

        return Math.Max(0, Math.Min(max, beatTimings.Length - 2));
    }
}