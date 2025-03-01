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
        if (beatTimings.Length <= 1)
            return time / (60d / Math.Max(1, BPM)) + 1d;

        for (int i = 0; i < beatTimings.Length - 1; i++) {
            if (time >= beatTimings[i + 1])
                continue;

            double previous = beatTimings[i];
            double next = beatTimings[i + 1];

            return i + 1 + (time - previous) / (next - previous);
        }

        double last = beatTimings[beatTimings.Length - 1];
        double secondToLast = beatTimings[beatTimings.Length - 2];

        return beatTimings.Length + (time - last) / (last - secondToLast);
    }

    public double GetTimeFromBeat(double beat) {
        if (beat <= 1d)
            return beatTimings.Length > 0 ? beatTimings[0] : 0d;

        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1d);

        if ((int) beat < beatTimings.Length) {
            double previous = beatTimings[(int) beat - 1];
            double next = beatTimings[(int) beat];

            return previous + (next - previous) * (beat % 1d);
        }

        double last = beatTimings[beatTimings.Length - 1];
        double secondToLast = beatTimings[beatTimings.Length - 2];

        return last + (last - secondToLast) * (beat - beatTimings.Length);
    }

    public double GetTimeFromBeat(int beat) {
        if (beat <= 1)
            return beatTimings.Length > 0 ? beatTimings[0] : 0d;

        if (beatTimings.Length <= 1)
            return 60d / Math.Max(1, BPM) * (beat - 1);

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

        for (int i = 0; i < beatTimings.Length - 1; i++) {
            if (time < beatTimings[i + 1])
                return beatTimings[i + 1] - beatTimings[i];
        }

        return beatTimings[beatTimings.Length - 1] - beatTimings[beatTimings.Length - 2];
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
}