# 发布流程（自动化）

本仓库已配置 GitHub Actions 自动发布。

## 触发规则

- 触发条件：推送版本 Tag，格式必须为 `vX.Y.Z`（例如 `v1.2.3`）
- Tag 必须指向 `origin/main` 上的提交，否则工作流会失败
- 发布版本来源：Tag 本身（去掉前缀 `v` 后作为发布版本）
- 发布目标：GitHub Releases

## 自动执行内容

当推送 `vX.Y.Z` Tag 后，`Release` 工作流会自动执行：

1. 校验 Tag 格式是否为 `vX.Y.Z`
2. 运行单元测试：`dotnet test SeatTooLong.Tests --verbosity minimal`
3. 安装 Inno Setup
4. 调用 `scripts/build-installer.ps1 -Version X.Y.Z -Clean` 构建安装包
5. 校验安装包是否生成：`artifacts/installer/SeatTooLong-Setup-x64-X.Y.Z.exe`
6. 创建 GitHub Release（标题格式：`SeatTooLong vX.Y.Z (YYYY-MM-DD)`）并上传安装包

## 手动发版步骤

1. 确保本地代码和测试状态正常
2. 创建并推送 Tag（以 patch 为例）：

```powershell
git tag v1.0.1
git push origin v1.0.1
```

1. 在 GitHub Actions 查看 `Release` 工作流
2. 在 GitHub Releases 确认发布记录和安装包附件

## 说明

- `Directory.Build.props` 的 `<AppVersion>` 仍可作为本地默认版本来源（不显式传 `-Version` 时生效）
- 自动发布场景下，版本以 Tag 为准
- 如果 Tag 不符合 `vX.Y.Z`，工作流会失败并提示修复
- 如果 Tag 指向的提交不在 `origin/main` 上，工作流会失败并提示修复
