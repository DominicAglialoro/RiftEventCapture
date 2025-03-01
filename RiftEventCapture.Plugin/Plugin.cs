﻿using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RhythmRift;
using RhythmRift.Enemies;
using RiftEventCapture.Common;
using Shared;
using Shared.Pins;
using Shared.RhythmEngine;
using Shared.SceneLoading.Payloads;

namespace RiftEventCapture.Plugin;

[BepInPlugin("programmatic.riftEventCapture", "RiftEventCapture", "1.0.0.0")]
internal class Plugin : BaseUnityPlugin {
    public static ConfigEntry<bool> RecordNormalGameplay;
    public static ConfigEntry<bool> RecordGoldenLuteGameplay;

    public new static ManualLogSource Logger { get; private set; }

    private static bool shouldCaptureEvents;
    private static CaptureSession currentSession;
    private static RRStageController stageController;

    private void Awake() {
        RecordNormalGameplay = Config.Bind("General", "RecordNormalGameplay", true, "Record events when playing a chart without the Golden Lute modifier enabled");
        RecordGoldenLuteGameplay = Config.Bind("General", "RecordGoldenLuteGameplay", true, "Record events when playing a chart with the Golden Lute modifier enabled");

        Logger = base.Logger;
        Logger.LogInfo("Loaded RiftEventCapture");

        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.BeginPlay), RRStageController_BeginPlay);
        typeof(RRStageController).CreateMethodHook(nameof(RRStageController.ShowResultsScreen), RRStageController_ShowResultsScreen);
        typeof(RRStageController).CreateILHook(nameof(RRStageController.ProcessHitData), RRStageController_ProcessHitData_IL);
        typeof(RRStageController).CreateILHook(nameof(RRStageController.HandleKilledBoundEnemy), RRStageController_HandleKilledBoundEnemy_IL);
        typeof(RRWyrmEnemy).CreateMethodHook(nameof(RRWyrmEnemy.ScoreHoldSegment), RRWyrmEnemy_ScoreHoldSegment);
        typeof(RRWyrmEnemy).CreateMethodHook(nameof(RRWyrmEnemy.PerformDeathBehaviour), RRWyrmEnemy_PerformDeathBehavior);
    }

    private static bool TryGetCurrentSession(RRStageController rrStageController) {
        if (currentSession != null)
            return true;

        if (!shouldCaptureEvents || rrStageController == null || !(PinsController.IsPinActive("GoldenLute") ? RecordGoldenLuteGameplay.Value : RecordNormalGameplay.Value))
            return false;

        var beatmap = rrStageController.BeatmapPlayer._activeBeatmap;

        if (beatmap == null)
            return false;

        var stageContextInfo = rrStageController._stageFlowUiController._stageContextInfo;
        var metadata = new ChartMetadata(stageContextInfo.StageDisplayName, Util.GameDifficultyToCommonDifficulty(stageContextInfo.StageDifficulty));
        var beatData = new BeatData(beatmap.bpm, beatmap.beatDivisions, beatmap.BeatTimings.ToArray());

        currentSession = CaptureSession.CreateNewSession(metadata, beatData);

        return true;
    }

    private static void OnRecordInput(RRStageController rrStageController, RREnemyController.EnemyHitData hitData, int inputScore, int perfectBonusScore) {
        if (!RiftUtilityHelper.IsInputSuccess(hitData.InputRating) || !TryGetCurrentSession(rrStageController))
            return;

        var stageInputRecord = rrStageController._stageInputRecord;
        int baseMultiplier = stageInputRecord._stageScoringDefinition.GetComboMultiplier(stageInputRecord.CurrentComboCount);
        int vibeMultiplier = rrStageController._isVibePowerActive ? 2 : 1;
        var enemy = hitData.Enemy;
        var beatData = currentSession.BeatData;

        currentSession.Capture(new RiftEvent(
            EventType.EnemyHit,
            beatData.GetTimestampFromBeat(hitData.InputBeat),
            beatData.GetTimestampFromBeat(hitData.TargetBeat),
            Util.EnemyIdToType(enemy.EnemyTypeId),
            enemy.CurrentGridPosition.x,
            inputScore * baseMultiplier * vibeMultiplier + perfectBonusScore,
            inputScore,
            baseMultiplier,
            vibeMultiplier,
            perfectBonusScore,
            enemy.IsPartOfVibeChain));
    }

    private static void OnVibeChainSuccessFromHit(RRStageController rrStageController, RREnemyController.EnemyHitData hitData)
        => OnVibeChainSuccess(rrStageController, hitData.Enemy, hitData.InputBeat, hitData.TargetBeat);

    private static void OnVibeChainSuccessFromWyrmKill(RRStageController rrStageController, IRREnemyDataAccessor boundEnemy) {
        var enemy = (RREnemy) boundEnemy;
        float beat = enemy.TargetHitBeatNumber + Math.Max(0, enemy.EnemyLength - 1);

        OnVibeChainSuccess(rrStageController, enemy, beat, beat);
    }

    private static void OnVibeChainSuccess(RRStageController rrStageController, RREnemy enemy, float inputBeat, float targetBeat) {
        if (!TryGetCurrentSession(rrStageController))
            return;

        var beatData = currentSession.BeatData;

        currentSession.Capture(new RiftEvent(
            EventType.VibeGained,
            beatData.GetTimestampFromBeat(inputBeat),
            beatData.GetTimestampFromBeat(targetBeat),
            Util.EnemyIdToType(enemy.EnemyTypeId),
            enemy.CurrentGridPosition.x,
            0,
            0,
            1,
            1,
            0,
            true));
    }

    private static void RRStageController_BeginPlay(Action<RRStageController> beginPlay, RRStageController rrStageController) {
        beginPlay(rrStageController);
        currentSession = null;
        stageController = rrStageController;

        if (rrStageController._stageScenePayload is not RhythmRiftScenePayload) {
            shouldCaptureEvents = false;

            return;
        }

        Logger.LogInfo($"Begin playing {rrStageController._stageFlowUiController._stageContextInfo.StageDisplayName}");
        shouldCaptureEvents = true;
    }

    private static void RRStageController_ShowResultsScreen(Action<RRStageController, bool, float, int, bool, bool> showResultsScreen,
        RRStageController rrStageController, bool isNewHighScore, float trackProgressPercentage, int awardedDiamonds = 0,
        bool didNotFinish = false, bool cheatsDetected = false) {
        showResultsScreen(rrStageController, isNewHighScore, trackProgressPercentage, awardedDiamonds, didNotFinish, cheatsDetected);

        if (currentSession == null || didNotFinish)
            return;

        Logger.LogInfo("Completed stage");

        var result = currentSession.Complete();
        string name = $"{result.Metadata.Name}_{result.Metadata.Difficulty}";
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RiftEventCapture");

        Directory.CreateDirectory(directory);

        string path;
        int num = 0;

        do {
            path = Path.Combine(directory, $"{name}_{num}.bin");
            num++;
        } while (File.Exists(path));

        result.SaveToFile(path);
        Logger.LogInfo($"Saved capture result to {path}");
        shouldCaptureEvents = false;
        currentSession = null;
        stageController = null;
    }

    private static void RRStageController_ProcessHitData_IL(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<StageInputRecord>(nameof(StageInputRecord.RecordInput)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 6);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 8);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 7);
        cursor.EmitCall(OnRecordInput);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<RRStageController>(nameof(RRStageController.VibeChainSuccess)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte) 22);
        cursor.EmitCall(OnVibeChainSuccessFromHit);
    }

    private static void RRStageController_HandleKilledBoundEnemy_IL(ILContext il) {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCall<RRStageController>(nameof(RRStageController.VibeChainSuccess)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitCall(OnVibeChainSuccessFromWyrmKill);
    }

    private static bool RRWyrmEnemy_ScoreHoldSegment(Func<RRWyrmEnemy, bool> scoreHoldSegment, RRWyrmEnemy rrWyrmEnemy) {
        bool result = scoreHoldSegment(rrWyrmEnemy);

        if (!result || !TryGetCurrentSession(stageController))
            return result;

        var timestamp = currentSession.BeatData.GetTimestampFromBeat(rrWyrmEnemy.TargetHitBeatNumber + rrWyrmEnemy._holdSegmentsScored);

        currentSession.Capture(new RiftEvent(
            EventType.HoldSegment,
            timestamp,
            timestamp,
            EnemyType.Wyrm,
            rrWyrmEnemy.CurrentGridPosition.x,
            333,
            333,
            1,
            1,
            0,
            rrWyrmEnemy.IsPartOfVibeChain
        ));

        return true;
    }

    private static void RRWyrmEnemy_PerformDeathBehavior(Action<RRWyrmEnemy, FmodTimeCapsule, bool> performDeathBehavior, RRWyrmEnemy rrWyrmEnemy, FmodTimeCapsule fmodTimeCapsule, bool diedFromPlayerDamage) {
        if (!rrWyrmEnemy.IsBeingHeld || !TryGetCurrentSession(stageController)) {
            performDeathBehavior(rrWyrmEnemy, fmodTimeCapsule, diedFromPlayerDamage);

            return;
        }

        performDeathBehavior(rrWyrmEnemy, fmodTimeCapsule, diedFromPlayerDamage);

        var timestamp = currentSession.BeatData.GetTimestampFromBeat(rrWyrmEnemy.TargetHitBeatNumber + Math.Max(0, rrWyrmEnemy.EnemyLength - 1));

        currentSession.Capture(new RiftEvent(
            EventType.HoldComplete,
            timestamp,
            timestamp,
            EnemyType.Wyrm,
            rrWyrmEnemy.CurrentGridPosition.x,
            0,
            0,
            1,
            1,
            0,
            rrWyrmEnemy.IsPartOfVibeChain
        ));
    }
}