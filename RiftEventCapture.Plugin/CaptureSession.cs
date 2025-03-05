using System;
using System.Collections.Generic;
using RiftEventCapture.Common;

namespace RiftEventCapture.Plugin;

public class CaptureSession : IDisposable {
    public static event Action<CaptureSession> NewSession;

    internal static CaptureSession CreateNewSession(SessionInfo sessionInfo, BeatData beatData) {
        var session = new CaptureSession(sessionInfo, beatData);

        NewSession?.Invoke(session);

        return session;
    }

    public event Action<RiftEvent> EventCaptured;
    public event Action<CaptureResult> SessionCompleted;

    public SessionInfo SessionInfo { get; }
    public BeatData BeatData { get; }
    public IReadOnlyList<RiftEvent> RiftEvents => riftEvents;

    private readonly List<RiftEvent> riftEvents;

    public CaptureSession(SessionInfo sessionInfo, BeatData beatData) {
        BeatData = beatData;
        SessionInfo = sessionInfo;
        riftEvents = new List<RiftEvent>();
    }

    public void Capture(RiftEvent riftEvent) {
        riftEvents.Add(riftEvent);
        EventCaptured?.Invoke(riftEvent);
    }

    public CaptureResult Complete() {
        riftEvents.Sort();

        var result = new CaptureResult(SessionInfo, BeatData, riftEvents.ToArray());

        SessionCompleted?.Invoke(result);
        Dispose();

        return result;
    }

    public void Dispose() {
        EventCaptured = null;
        SessionCompleted = null;
    }
}