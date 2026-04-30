# SeatTooLong

通过 USB 摄像头实时检测用户是否在座，连续久坐超过阈值后弹窗 + 悬浮窗提醒用户起身活动。

## 功能特性

- **智能检测**：基于 OpenCV Haar Cascade 人脸检测判断用户是否在座，无需手动打卡
- **双重提醒**：Windows Toast 通知 + 桌面悬浮窗实时计时与变色提醒
- **隐私保障**：画面仅在内存中实时分析，不保存任何图片/视频到磁盘，零数据外传
- **统计报表**：本地 SQLite 记录久坐/休息数据，可视化展示近 7/30 天趋势
- **灵活配置**：久坐阈值、休息时长、检测间隔、摄像头选择等均可自定义
- **中英双语**：支持中文/英文界面切换
- **开机自启**：注册 Windows 启动项，启动后自动最小化到系统托盘

## 系统要求

- Windows 10 1903+ / Windows 11
- .NET 8.0 Runtime
- USB 摄像头

## 快速开始

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
- 打开设置
- 退出

### 运行测试

```bash
dotnet test SeatTooLong.Tests
```

## 项目结构

```
SeatTooLong.sln
├── SeatTooLong.Core/          # 核心业务逻辑（无 UI 依赖）
│   ├── SittingMonitor.cs      # 状态机（Idle→Sitting→Alerting→Resting）
│   ├── MonitoringService.cs   # 编排：摄像头 + 检测器 + 状态机
│   ├── Settings/              # 应用设置（JSON 持久化）
│   ├── Statistics/            # SQLite 统计服务
│   └── Localization/          # 中英文本地化
├── SeatTooLong.App/           # WPF 应用层
│   ├── Services/              # 平台实现（摄像头、检测器、通知、自启）
│   └── Views/                 # UI 窗口（悬浮窗、设置、报表）
├── SeatTooLong.Tests/         # 单元测试（52 个，TDD 开发）
└── doc/PRD.md                 # 产品需求文档
```

## 技术栈

| 组件 | 技术 |
|---|---|
| 框架 | .NET 8 + WPF |
| 摄像头采集 | OpenCvSharp4 |
| 人脸检测 | Haar Cascade (frontalface_default) |
| 通知 | Microsoft.Toolkit.Uwp.Notifications |
| 数据库 | Microsoft.Data.Sqlite |
| 图表 | LiveCharts2 (SkiaSharp) |
| 托盘图标 | Hardcodet.NotifyIcon.Wpf |
| 测试 | xUnit + Moq |

## 检测原理

1. 每 5 秒（可配置）从摄像头采集一帧
2. 灰度化 + 直方图均衡化后使用 Haar Cascade 检测人脸
3. 最大人脸尺寸 ≥ 85px → 判定为"在座"（过滤远处/背景人脸）
4. 连续"在座"累计达到阈值（默认 45 分钟）→ 触发提醒
5. 短暂遮挡（< 30 秒）不视为离开，避免误判

## 状态机

```
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
|---|---|---|
| 久坐阈值 | 45 分钟 | 15–120 分钟 |
| 建议休息时长 | 5 分钟 | 1–15 分钟 |
| 检测间隔 | 5 秒 | 3/5/10 秒 |
| 摄像头 | 默认设备 | 下拉选择 |
| 开机自启 | 开启 | 开/关 |
| 语言 | 跟随系统 | 中文/English |
| 悬浮窗 | 显示 | 开/关 |
| 悬浮窗透明度 | 80% | 30%–100% |

配置文件位于 `%LOCALAPPDATA%\SeatTooLong\settings.json`，统计数据位于 `%LOCALAPPDATA%\SeatTooLong\stats.db`。

## 开发说明

本项目采用 **TDD（测试驱动开发）** 模式，核心逻辑均有对应单元测试覆盖：

- `SittingMonitorTests` — 状态机完整转换逻辑
- `MonitoringServiceTests` — 服务编排
- `HaarPersonDetectorTests` — 真实图片检测准确性
- `SqliteStatisticsRepositoryTests` — 数据持久化
- `StatisticsServiceTests` — 统计记录逻辑
- `JsonSettingsServiceTests` — 设置读写
- `LocalizationServiceTests` — 多语言
- `NotificationMessageBuilderTests` — 通知消息构建

## License

MIT
