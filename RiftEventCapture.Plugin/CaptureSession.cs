using System;
using System.Collections.Generic;
using RiftEventCapture.Common;

namespace RiftEventCapture.Plugin;

public class CaptureSession {
    public static event Action<CaptureSession> NewSession;

    internal static CaptureSession CreateNewSession(ChartMetadata metadata, BeatData beatData) {
        var session = new CaptureSession(metadata, beatData);

        NewSession?.Invoke(session);

        return session;
    }

    public event Action<RiftEvent> EventCaptured;
    public event Action<CaptureResult> SessionCompleted;

    public ChartMetadata Metadata { get; }
    public BeatData BeatData { get; }
    public IReadOnlyList<RiftEvent> RiftEvents => riftEvents;

    private readonly List<RiftEvent> riftEvents;

    public CaptureSession(ChartMetadata metadata, BeatData beatData) {
        Metadata = metadata;
        BeatData = beatData;
        riftEvents = new List<RiftEvent>();
    }

    public void Capture(RiftEvent riftEvent) {
        riftEvents.Add(riftEvent);
        EventCaptured?.Invoke(riftEvent);
    }

    public CaptureResult Complete() {
        riftEvents.Sort();

        var result = new CaptureResult(Metadata, BeatData, riftEvents.ToArray());

        SessionCompleted?.Invoke(result);
        EventCaptured = null;
        SessionCompleted = null;

        return result;
    }
}