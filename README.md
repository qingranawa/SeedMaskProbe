# SeedMaskProbe

[![EXILED](https://img.shields.io/badge/EXILED-9.14.2-5865F2)](https://github.com/ExMod-Team/EXILED) [![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7-5865F2)](https://github.com/northwood-studios/LabAPI) [![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.2.7-2f3136)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![.NET%20Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

> SeedMaskProbe 是一个面向 SCP: Secret Laboratory 的 EXILED 服务端插件，用于遮罩普通玩家通过客户端控制台 `seed` 命令看到的地图 Seed。

> **当前发布版本：** `0.1.1`

---

## 能做什么

- 在地图生成完成后，为每个客户端发送可接受的伪造 Seed。
- 防止玩家通过 Seed 命令获取当前回合真实 Seed 值。
- 可以选择伪造的 Seed 值为：固定伪造值 / 每回合随机伪造值。

## 重要限制

> 客户端为了正确生成地图，仍然需要先收到服务器真实 Seed。

## 兼容性

- SCP: Secret Laboratory：`14.2.7`。
- EXILED：`9.14.2`。
- 目标框架：`.NET Framework 4.8`。

## 安装

1. 从 Releases 下载 `SeedMaskProbe.dll`

2. 将 DLL 放入：

```text
AppData/EXILED/Plugins/SeedMaskProbe.dll
```

3. 第一次启动插件后会自动创建：

```text
AppData/EXILED/Plugins/SeedMaskProbe/seed-mask.json
```

---

## 伪造值配置

`seed-mask.json` 只有一个配置字段 `fake_seed`。

**固定值：**

```json
{
  "fake_seed": "246813579"
}
```

**每回合随机生成值：**

```json
{
  "fake_seed": "${RANDOM}"
}
```

固定值建议使用带引号的正十进制整数，范围为：

```text
1 ～ 2147483646
```

推荐使用 10 位数字时，范围为：

```text
1000000000 ～ 2147483646
```

缺失、为空或格式错误时会回退到内置伪造值。配置值如果与当前真实 Seed 相同，插件会自动调整为相邻的合法值。

## 延迟配置

推荐的测试服/生产服基础配置：

```yaml
is_enabled: true
debug: false
mask_delay_seconds: 0.1
late_join_mask_delay_seconds: 0.1
```

两个延迟都表示客户端连接或地图生成路径的服务端计时器延迟。当前代码将最低值限制为 `0.1` 秒。

`0.1` 秒可以降低普通玩家在认证阶段撞上真实值的概率，但不能保证真实 Seed 从未到达客户端。`debug` 默认关闭；打开后会记录伪造值以及连接玩家标识，生产环境建议保持 `false`。

## 构建

构建需要本机已经安装 .NET SDK、.NET Framework 4.8 开发组件，以及目标 SCP:SL 版本的 Managed 程序集。游戏程序集不包含在本仓库中，也不随项目发布。

在 PowerShell 中显式传入 Managed 目录：

```powershell
dotnet build .\SeedMaskProbe.csproj -c Release `
  -p:ScpSlManagedPath="C:\Path\To\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed"
```

也可以设置环境变量：

```powershell
$env:SCPSL_MANAGED_PATH = "C:\Path\To\SCPSL_Data\Managed"
dotnet build .\SeedMaskProbe.csproj -c Release
```

缺少 `ScpSlManagedPath` 或必要程序集时，项目会明确失败，不会使用开发者电脑上的默认路径。

## 测试

**纯逻辑测试：**

```powershell
dotnet run --project .\tests\SeedMaskProbe.Tests\SeedMaskProbe.Tests.csproj -c Release
```

测试覆盖固定值、`${RANDOM}`、非法值回退、范围归一化以及真实值碰撞修正。

最终发布文件位于：

```text
release/SeedMaskProbe.dll
```

## 许可证

本项目使用 MIT License。
