using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.ServerEvents;
using MapGeneration;
using MEC;
using Mirror;
using ExiledPlayerEvents = Exiled.Events.Handlers.Player;
using LabServerEvents = LabApi.Events.Handlers.ServerEvents;

namespace SeedMaskProbe;

/// <summary>
/// 在客户端完成地图生成后覆盖其本地 Seed，仅用于验证 SeedMaskProbe 方案。
/// </summary>
public sealed class SeedMaskPlugin : Plugin<SeedMaskProbeConfig>
{
    private bool isRunning;

    private int? currentDecoySeed;

    private int roundGeneration;

    private readonly HashSet<NetworkConnection> scheduledConnections = new();

    private string settingsPath = string.Empty;

    public override string Name => "SeedMaskProbe";

    public override string Author => "Qingran";

    public override Version Version => new(0, 1, 1);

    public override Version RequiredExiledVersion => new(9, 14, 2);

    public override void OnEnabled()
    {
        isRunning = true;
        currentDecoySeed = null;
        roundGeneration++;
        scheduledConnections.Clear();
        settingsPath = Path.Combine(Paths.Plugins, Name, "seed-mask.json");
        EnsureSettingsFile();

        LabServerEvents.MapGenerated += OnMapGenerated;
        ExiledPlayerEvents.Joined += OnPlayerJoined;
        ExiledPlayerEvents.Verified += OnPlayerVerified;

        base.OnEnabled();
        Log.Info("[SeedMaskProbe] 已启用：地图生成完成后覆盖客户端 Seed。");
    }

    public override void OnDisabled()
    {
        isRunning = false;
        currentDecoySeed = null;
        roundGeneration++;
        scheduledConnections.Clear();

        LabServerEvents.MapGenerated -= OnMapGenerated;
        ExiledPlayerEvents.Joined -= OnPlayerJoined;
        ExiledPlayerEvents.Verified -= OnPlayerVerified;

        base.OnDisabled();
    }

    private void OnMapGenerated(MapGeneratedEventArgs _)
    {
        if (!isRunning || !Config.IsEnabled)
        {
            return;
        }

        roundGeneration++;
        scheduledConnections.Clear();
        currentDecoySeed = SelectRoundDecoy();
        foreach (Player player in Player.Enumerable)
        {
            SchedulePlayerMask(
                player,
                Config.MaskDelaySeconds,
                "map-generated",
                roundGeneration);
        }
    }

    private void OnPlayerJoined(JoinedEventArgs ev)
    {
        if (!isRunning || !Config.IsEnabled || !SeedSynchronizer.MapGenerated)
        {
            return;
        }

        currentDecoySeed ??= SelectRoundDecoy();
        SchedulePlayerMask(
            ev.Player,
            Config.LateJoinMaskDelaySeconds,
            "joined",
            roundGeneration);
    }

    private void OnPlayerVerified(VerifiedEventArgs ev)
    {
        if (!isRunning || !Config.IsEnabled || !SeedSynchronizer.MapGenerated)
        {
            return;
        }

        currentDecoySeed ??= SelectRoundDecoy();
        SchedulePlayerMask(
            ev.Player,
            Config.LateJoinMaskDelaySeconds,
            "late-join",
            roundGeneration);
    }

    private void SchedulePlayerMask(
        Player player,
        float delaySeconds,
        string reason,
        int scheduledRoundGeneration)
    {
        if (!player.IsConnected || player.IsNPC)
        {
            return;
        }

        NetworkConnection connection = player.Connection;
        if (!scheduledConnections.Add(connection))
        {
            return;
        }

        float safeDelay = Math.Max(0.1f, delaySeconds);
        int decoySeed = currentDecoySeed ?? SelectRoundDecoy();
        Timing.CallDelayed(
            safeDelay,
            () => TryMaskPlayer(
                player,
                connection,
                decoySeed,
                reason,
                scheduledRoundGeneration));
    }

    private void TryMaskPlayer(
        Player player,
        NetworkConnection connection,
        int decoySeed,
        string reason,
        int scheduledRoundGeneration)
    {
        if (!isRunning
            || !Config.IsEnabled
            || scheduledRoundGeneration != roundGeneration
            || !SeedSynchronizer.MapGenerated
            || !player.IsConnected
            || player.IsNPC)
        {
            return;
        }

        if (!ReferenceEquals(player.Connection, connection))
        {
            return;
        }

        connection.Send(
            new SeedSynchronizer.SeedMessage
            {
                Value = decoySeed,
            });

        if (Config.Debug)
        {
            Log.Debug(
                $"[SeedMaskProbe] 已发送客户端遮罩。" +
                $" Player={player.UserId}; Reason={reason}; Decoy={decoySeed}");
        }
    }

    private int SelectRoundDecoy()
    {
        string? configuredValue = ReadConfiguredValue();
        int decoySeed = SeedMasking.ResolveConfiguredValue(
            configuredValue,
            SeedSynchronizer.Seed,
            SeedMasking.GenerateRandomSeed);

        if (Config.Debug)
        {
            string mode = SeedMasking.IsRandomPlaceholder(configuredValue)
                ? "random"
                : "fixed-or-fallback";
            Log.Debug($"[SeedMaskProbe] 已选择本回合客户端遮罩。Mode={mode}; Decoy={decoySeed}");
        }

        return decoySeed;
    }

    private string? ReadConfiguredValue()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                EnsureSettingsFile();
            }

            string json = File.ReadAllText(settingsPath, Encoding.UTF8);
            string? configuredValue = SeedMaskSettingsLoader.ReadFakeSeed(json);
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                Log.Warn("[SeedMaskProbe] seed-mask.json 缺少 fake_seed，使用备用伪造值。");
                return null;
            }

            return configuredValue;
        }
        catch (Exception exception)
        {
            Log.Error($"[SeedMaskProbe] 读取 seed-mask.json 失败，使用备用伪造值。Error={exception.Message}");
            return null;
        }
    }

    private void EnsureSettingsFile()
    {
        try
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            if (!File.Exists(settingsPath))
            {
                File.WriteAllText(settingsPath, SeedMaskSettingsLoader.DefaultJson, new UTF8Encoding(false));
            }
        }
        catch (Exception exception)
        {
            Log.Error($"[SeedMaskProbe] 创建 seed-mask.json 失败。Error={exception.Message}");
        }
    }
}

/// <summary>
/// SeedMaskProbe 配置。
/// </summary>
public sealed class SeedMaskProbeConfig : Exiled.API.Interfaces.IConfig
{
    public bool IsEnabled { get; set; } = true;

    public bool Debug { get; set; } = false;

    public float MaskDelaySeconds { get; set; } = 0.1f;

    public float LateJoinMaskDelaySeconds { get; set; } = 0.1f;
}
