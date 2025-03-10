﻿using System;
using System.Collections.Generic;
using System.IO;

namespace RiftEventCapture.Common;

public class CaptureResult {
    private const string HEADER = "RIFT_EVENT_CAPTURE";
    private const int FORMAT_VERSION = 0;

    public SessionInfo SessionInfo { get; }
    public BeatData BeatData { get; }
    public IReadOnlyList<RiftEvent> RiftEvents => riftEvents;

    private readonly RiftEvent[] riftEvents;

    public CaptureResult(SessionInfo sessionInfo, BeatData beatData, RiftEvent[] riftEvents) {
        SessionInfo = sessionInfo;
        BeatData = beatData;
        this.riftEvents = riftEvents;
    }

    public void SaveToFile(string path) {
        using var writer = new BinaryWriter(File.Create(path));

        writer.Write(HEADER);
        writer.Write(FORMAT_VERSION);
        writer.Write(SessionInfo.ChartName);
        writer.Write(SessionInfo.ChartID);
        writer.Write((int) SessionInfo.ChartDifficulty);

        var pins = SessionInfo.Pins;

        writer.Write(pins.Count);

        foreach (string pin in pins)
            writer.Write(pin);

        writer.Write(BeatData.BPM);
        writer.Write(BeatData.BeatDivisions);

        var beatTimings = BeatData.BeatTimings;

        writer.Write(beatTimings.Count);

        foreach (double time in beatTimings)
            writer.Write(time);

        writer.Write(riftEvents.Length);

        foreach (var riftEvent in riftEvents) {
            writer.Write((int) riftEvent.EventType);
            writer.Write(riftEvent.Time.Time);
            writer.Write(riftEvent.Time.Beat);
            writer.Write(riftEvent.TargetTime.Time);
            writer.Write(riftEvent.TargetTime.Beat);
            writer.Write((int) riftEvent.EnemyType);
            writer.Write(riftEvent.Column);
            writer.Write(riftEvent.TotalScore);
            writer.Write(riftEvent.BaseScore);
            writer.Write(riftEvent.BaseMultiplier);
            writer.Write(riftEvent.VibeMultiplier);
            writer.Write(riftEvent.PerfectBonus);
            writer.Write(riftEvent.VibeChain);
        }
    }

    public static CaptureResult LoadFromFile(string path) {
        using var reader = new BinaryReader(File.OpenRead(path));

        string header = reader.ReadString();

        if (header != HEADER)
            throw new InvalidOperationException("Not a valid capture file");

        int formatVersion = reader.ReadInt32();

        if (formatVersion < 0 || formatVersion > FORMAT_VERSION)
            throw new InvalidOperationException("Invalid version number");

        string chartName = reader.ReadString();
        string chartId = reader.ReadString();
        int chartDifficulty = reader.ReadInt32();
        int pinsCount = reader.ReadInt32();
        string[] pins = new string[pinsCount];

        for (int i = 0; i < pinsCount; i++)
            pins[i] = reader.ReadString();

        var sessionInfo = new SessionInfo(chartName, chartId, (Difficulty) chartDifficulty, pins);
        int bpm = reader.ReadInt32();
        int beatDivisions = reader.ReadInt32();
        int beatTimingsCount = reader.ReadInt32();
        double[] beatTimings = new double[beatTimingsCount];

        for (int i = 0; i < beatTimingsCount; i++)
            beatTimings[i] = reader.ReadDouble();

        var beatData = new BeatData(bpm, beatDivisions, beatTimings);
        int riftEventsCount = reader.ReadInt32();
        var riftEvents = new RiftEvent[riftEventsCount];

        for (int i = 0; i < riftEventsCount; i++) {
            int eventType = reader.ReadInt32();
            double timeTime = reader.ReadDouble();
            double timeBeat = reader.ReadDouble();
            double targetTimeTime = reader.ReadDouble();
            double targetTimeBeat = reader.ReadDouble();
            int enemyType = reader.ReadInt32();
            int column = reader.ReadInt32();
            int totalScore = reader.ReadInt32();
            int baseScore = reader.ReadInt32();
            int baseMultiplier = reader.ReadInt32();
            int vibeMultiplier = reader.ReadInt32();
            int perfectBonus = reader.ReadInt32();
            bool vibeChain = reader.ReadBoolean();

            riftEvents[i] = new RiftEvent(
                (EventType) eventType,
                new Timestamp(timeTime, timeBeat),
                new Timestamp(targetTimeTime, targetTimeBeat),
                (EnemyType) enemyType,
                column,
                totalScore,
                baseScore,
                baseMultiplier,
                vibeMultiplier,
                perfectBonus,
                vibeChain);
        }

        return new CaptureResult(sessionInfo, beatData, riftEvents);
    }
}