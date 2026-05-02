# SeatTooLong

通过 USB 摄像头实时检测用户是否在座，连续久坐超过阈值后弹窗 + 悬浮窗提醒用户起身活动。

## 功能特性

- **智能检测**：基于 OpenCV DNN + UltraFace ONNX 模型判断用户是否在桌前，并结合侧脸/上半身规则兜底，无需手动打卡
- **双重提醒**：Windows Toast 通知 + 桌面悬浮窗实时计时与变色提醒
- **摄像头预览**：托盘可打开当前摄像头实时预览窗口，便于校准角度与排查遮挡/设备异常
- **隐私保障**：默认情况下画面仅在内存中实时分析；只有手动开启录制时才会写入本地 JPEG/JSON 素材
- **手动素材录制**：需要优化检测效果时，可从托盘手动开始/停止录制本地 JPEG 帧与 JSON 元数据
- **统计报表**：本地 SQLite 记录久坐/休息数据，可视化展示近 7/30 天趋势
- **灵活配置**：久坐阈值、休息时长、检测间隔、离开宽限期、摄像头选择等均可自定义
- **中英双语**：支持中文/英文界面切换
- **开机自启**：注册 Windows 启动项，启动后自动最小化到系统托盘

## 系统要求

- Windows 10 1903+ / Windows 11
- USB 摄像头

使用官方安装包时已自带 .NET 8 运行时，无需用户额外安装 .NET。

## 快速开始

### 安装包安装

1. 下载 `SeatTooLong-Setup-x64-<版本号>.exe`
2. 双击运行安装程序，按向导完成安装
3. 从开始菜单启动 `SeatTooLong`

启动后应用自动最小化到系统托盘，右键托盘图标可打开统计报表、摄像头预览和设置。

### 从源码构建

```bash
git clone <repo-url>
cd seatTooLong
dotnet build SeatTooLong.sln
```

### 运行

```bash
dotnet run --project SeatTooLong.App
```

启动后应用自动最小化到系统托盘，右键托盘图标可：

- 打开主界面 / 统计报表
- 暂停 / 恢复监测
- 开始 / 停止录制检测优化素材
- 预览当前摄像头
- 打开设置
- 退出

### 运行测试

```bash
dotnet test SeatTooLong.Tests
```

### 构建安装包

生成安装包需要：

- .NET 8 SDK
- Inno Setup 6

```powershell
.\scripts\build-installer.ps1 -Clean
```

脚本会先发布 self-contained 的 `win-x64` 应用到 `artifacts\publish\win-x64`，再通过 Inno Setup 生成 `artifacts\installer\SeatTooLong-Setup-x64-<版本号>.exe`。

构建安装包时会根据 `installer\ChineseSimplified.messages.json` 自动生成简体中文 Inno Setup 语言文件到 `artifacts\installer-languages\ChineseSimplified.generated.isl`。

如果 `ISCC.exe` 不在 `PATH` 或默认安装目录中，可以显式传入路径：

```powershell
.\scripts\build-installer.ps1 -InnoSetupCompiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

### 版本号管理

- 项目版本单一来源为仓库根目录 `Directory.Build.props` 的 `<AppVersion>`（SemVer：`X.Y.Z`）
- `dotnet build/publish` 会自动使用该版本写入程序集版本元数据
- `build-installer.ps1` 默认从 `Directory.Build.props` 读取版本并通过 `/DMyAppVersion=...` 注入安装脚本

本地手动构建时，通常只需要修改 `Directory.Build.props` 中的 `<AppVersion>`，然后重新执行安装包构建脚本。

如需临时覆盖版本（例如 CI 构建号），可以显式传入：

```powershell
.\scripts\build-installer.ps1 -Version 1.2.3
```

### 自动版本发布（GitHub Actions）

- 发布触发方式：推送版本 Tag（格式：`vX.Y.Z`，例如 `v1.2.3`）
- Tag 必须指向 `origin/main` 上的提交
- 自动发布时版本来源：推送的 Tag（去掉 `v` 后作为发布版本）
- 发布目标：GitHub Releases

发布工作流会自动执行：

1. 校验 Tag 格式
2. 运行 `dotnet test SeatTooLong.Tests --verbosity minimal`
3. 调用 `scripts/build-installer.ps1 -Version <tag-version> -Clean` 构建安装包
4. 创建 GitHub Release（标题：`SeatTooLong v<版本号> (YYYY-MM-DD)`）并上传 `SeatTooLong-Setup-x64-<版本号>.exe`

完整说明见 `doc/RELEASE.md`。

## 项目结构

```text
SeatTooLong.sln
├── SeatTooLong.Core/          # 核心业务逻辑（无 UI 依赖）
│   ├── SittingMonitor.cs      # 状态机（Idle→Sitting→Alerting→Resting）
│   ├── MonitoringService.cs   # 编排：摄像头 + 检测器 + 状态机
│   ├── Settings/              # 应用设置（JSON 持久化）
│   ├── Statistics/            # SQLite 统计服务
│   └── Localization/          # 中英文本地化
├── SeatTooLong.App/           # WPF 应用层
│   ├── Assets/Models/         # 打包的 ONNX / OpenCV 检测模型
│   ├── Services/              # 平台实现（摄像头、检测器、通知、自启）
│   └── Views/                 # UI 窗口（悬浮窗、设置、报表、预览）
├── installer/                 # Inno Setup 脚本与安装器文案
├── scripts/                   # 发布与安装包构建脚本
├── SeatTooLong.Tests/         # 单元测试（TDD 开发）
└── doc/PRD.md                 # 产品需求文档
```

## 技术栈

| 组件 | 技术 |
| --- | --- |
| 框架 | .NET 8 + WPF |
| 摄像头采集 | OpenCvSharp4 |
| 在座检测 | OpenCV DNN (UltraFace ONNX) + Profile/UpperBody Cascade 兜底 |
| 通知 | Microsoft.Toolkit.Uwp.Notifications |
| 数据库 | Microsoft.Data.Sqlite |
| 图表 | LiveCharts2 (SkiaSharp) |
| 托盘图标 | Hardcodet.NotifyIcon.Wpf |
| 打包 | PowerShell + Inno Setup 6 |
| 测试 | xUnit + Moq |

## 检测原理

1. 每 2 秒（可配置为 1/2/3/5/10 秒）从摄像头采集一帧
2. 优先使用 OpenCV DNN + UltraFace ONNX 在原始、均衡化和轻微旋转后的图像上检测正脸
3. 若正脸未命中，则回退到侧脸和上半身级联规则，提升桌前姿态识别鲁棒性
4. `SeatedFaceRule` / `SeatedProfileFaceRule` / `SeatedUpperBodyRule` 会过滤远处、边缘裁切和比例异常的误检
5. 连续"在座"累计达到阈值（默认 45 分钟）→ 触发提醒
6. 短暂遮挡或临时离开低于离开宽限期（默认 2 秒，可配置）时不视为离开，避免误判

## 状态机

```text
空闲 (Idle) ──检测到人──▶ 久坐中 (Sitting)
     ▲                        │
     │                   达到阈值
     │                        ▼
     │                  提醒中 (Alerting)
     │                        │
     │                   检测到离开
     │                        ▼
     └───倒计时结束───── 休息中 (Resting)
```

## 配置说明

设置通过托盘菜单"设置"打开，支持：

| 设置项 | 默认值 | 范围 |
| --- | --- | --- |
| 久坐阈值 | 45 分钟 | 15–120 分钟 |
| 建议休息时长 | 5 分钟 | 1–15 分钟 |
| 检测间隔 | 2 秒 | 1/2/3/5/10 秒 |
| 离开宽限期 | 2 秒 | 1–30 秒 |
| 摄像头 | 默认设备 | 下拉选择 |
| 开机自启 | 开启 | 开/关 |
| 语言 | 跟随系统 | 中文/English |
| 悬浮窗 | 显示 | 开/关 |
| 悬浮窗透明度 | 80% | 30%–100% |

配置文件位于 `%LOCALAPPDATA%\SeatTooLong\settings.json`，统计数据位于 `%LOCALAPPDATA%\SeatTooLong\stats.db`。

## 摄像头预览与排查

托盘菜单中的“预览当前摄像头”会打开一个实时刷新窗口，直接复用当前采集链路而不会额外写盘。摄像头服务会依次尝试 DirectShow、Media Foundation 和 OpenCV 默认后端；若读帧失败，会自动重开并再试一次，预览窗口与托盘状态会同步显示摄像头异常。

## 手动录制检测素材

为了改进摄像头状态检测，可在系统托盘菜单中手动开始/停止录制素材。录制默认关闭，只有手动开始后才会把检测间隔采集到的画面保存到本地；应用不会上传录制内容。

录制文件位于 `%LOCALAPPDATA%\SeatTooLong\recordings\<录制会话>`。每一帧会保存为一张 JPEG，并生成同名 JSON 元数据，包含时间、检测结果、当前久坐状态和相关持续时间。素材使用完后可手动删除对应录制会话目录。

## 开发说明

本项目采用 **TDD（测试驱动开发）** 模式，核心逻辑均有对应单元测试覆盖：

- `SittingMonitorTests` — 状态机完整转换逻辑
- `MonitoringServiceTests` — 服务编排
- `HaarPersonDetectorTests` — 视觉检测回归（DNN 主链路 + 负样本约束）
- `SeatedFaceRuleTests` — 人脸规则过滤与误检约束
- `SqliteStatisticsRepositoryTests` — 数据持久化
- `StatisticsServiceTests` — 统计记录逻辑
- `JsonSettingsServiceTests` — 设置读写
- `LocalizationServiceTests` — 多语言
- `NotificationMessageBuilderTests` — 通知消息构建

## License

MIT
