# Jellyfin-FastSTRM

<p align="center">
  <strong>⚡ 专为 Jellyfin 打造的 .strm 媒体秒开加速插件 | Instant Playback Plugin for Jellyfin STRM Streams</strong>
</p>

<p align="center">
  <a href="https://github.com/mmyysnd/Jellyfin-FastSTRM/releases"><img src="https://img.shields.io/github/v/release/mmyysnd/Jellyfin-FastSTRM?color=blue&label=Release" alt="Latest Release"></a>
  <a href="https://jellyfin.org/"><img src="https://img.shields.io/badge/Jellyfin-10.11%2B-purple.svg" alt="Jellyfin Version"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-9.0-blueviolet.svg" alt=".NET 9.0"></a>
  <a href="https://github.com/mmyysnd/Jellyfin-FastSTRM/releases"><img src="https://img.shields.io/github/downloads/mmyysnd/Jellyfin-FastSTRM/total?color=green" alt="Downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/mmyysnd/Jellyfin-FastSTRM" alt="License"></a>
</p>

---

## 📖 项目简介 (Introduction)

**FastSTRM** 是一个专为 **Jellyfin** 媒体服务器设计的 `.strm` 文件播放加速插件。

通过在 Jellyfin 底层注入 `IMediaSourceManager` 拦截装饰器，配合**全库预探测**与**入库自动探测**机制，彻底消除播放直链或网盘 STRM 媒体时由 `ffprobe` 远程网络探测引起的漫长等待，实现**毫秒级秒开起播**。

### 🎯 适用场景与解决的痛点
- **AList / OpenList / WebDAV 挂载**：解决 115网盘、阿里云盘、夸克网盘、天翼云、OneDrive 等挂载 STRM 播放转圈问题。
- **CloudDrive2 (CD2) / Rclone 挂载**：消除远程挂载直链每次起播耗时 3~15 秒的 `ffprobe` 网络探测。
- **避免探测超时与播放失败**：防止因远程网盘直链限速或响应慢导致的播放超时报错。
- **多端秒开**：无论 Web 网页端、电视 TV 端（Android TV / Apple TV）、手机端还是第三方客户端，均可享受秒开体验。

---

## ✨ 核心特性 (Key Features)

- ⚡ **毫秒级极速起播**：直接拦截播放源解析管线，已探测媒体跳过远程网络 probe，点击即播。
- 🛡️ **安全三路路由分流**：
  - **非 `.strm` 本地文件**：100% 原始透传，零副作用；
  - **已有流信息的 `.strm` 文件**：直接跳过探测（`allowMediaProbe = false`），秒开直连；
  - **未探测的 `.strm` 文件**：安全回退至官方媒体探测并写入数据库，保证兼容性。
- 🔍 **全库预探测引擎 (Probe Engine)**：
  - **增量探测 (Incremental)**：快速扫描库内缺少流信息的 STRM 视频，省时省流量；
  - **全量探测 (Full)**：对全库 STRM 媒体深度刷新媒体流元数据与时长。
- 🤖 **入库自动后台探测**：实时监听媒体库新入库事件，新文件入库自动触发后台探测入库，全程无感。
- 📊 **可视化控制台面板**：内置 Jellyfin 管理员后台 UI，直观展示媒体统计、实时进度条及一键启停任务。
- 🌐 **RESTful API 接口**：支持外部自动化脚本查询状态及启停探测任务。

---

## 🛠️ 工作原理 (How It Works)

```text
[ 用户发起播放请求 ]
         │
         ▼
[ MediaSourceInterceptor (装饰器拦截) ]
         │
 ┌───────┴────────────────────────────┐
 │                                    │
 ▼ 是 .strm 文件                      ▼ 非 .strm 本地文件
检查 RunTimeTicks 流时长               100% 透传原生管线
 │
 ├──▶ 已有流信息 (RunTimeTicks > 0) ──▶ allowMediaProbe = false ──▶ 🚀 毫秒级秒开
 │
 └──▶ 缺少流信息 (首次播放)         ──▶ allowMediaProbe = true  ──▶ 📡 走原生探测并入库
```

---

## 📦 安装与部署 (Installation)

### 方式一：直接下载 Release 安装（推荐）

1. 前往 [Releases 页面](https://github.com/mmyysnd/Jellyfin-FastSTRM/releases) 下载最新的 `FastSTRM.dll` 或 `FastSTRM.zip`。
2. 在 Jellyfin 插件目录中新建 `FastSTRM` 文件夹，并将 `FastSTRM.dll` 放入其中：

| 运行环境 | 常见插件路径 (Plugin Path) |
| :--- | :--- |
| **Docker (官方 / LSIO / 各种 NAS)** | `<配置映射路径>/plugins/FastSTRM/FastSTRM.dll` 或 `/config/plugins/FastSTRM/FastSTRM.dll` |
| **Linux (Debian/Ubuntu/CentOS)** | `/var/lib/jellyfin/plugins/FastSTRM/FastSTRM.dll` |
| **Windows 服务版 (默认安装)** | `C:\ProgramData\Jellyfin\Server\plugins\FastSTRM\FastSTRM.dll` |
| **Windows 便携版 / 用户模式** | `%LOCALAPPDATA%\jellyfin\plugins\FastSTRM\FastSTRM.dll` |

3. **重启 Jellyfin 服务**。
4. 登录 Jellyfin 管理员账号，进入 **控制台 -> 插件**，即可看到 **FastSTRM** 已激活。

---

### 方式二：从源码编译构建

系统需安装 [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)。

```bash
# 1. 克隆代码仓库
git clone https://github.com/mmyysnd/Jellyfin-FastSTRM.git
cd Jellyfin-FastSTRM

# 2. 编译发布版本
dotnet build -c Release
```

编译完成后，产物位于 `bin/Release/net9.0/FastSTRM.dll`，将其复制到上述插件目录即可。

---

## 🖥️ 控制台使用指南 (Usage Guide)

进入 Jellyfin 管理控制台，在左侧导航栏的 **插件** 列表中点击 **FastSTRM**：

1. **媒体库统计仪表盘**：
   - `.strm 媒体数量`：当前媒体库中全部 STRM 视频总数。
   - `已有流信息`：已完成探测、支持毫秒级秒开的视频数。
   - `缺少流信息`：尚未获取流信息的视频数。
2. **执行预探测**：
   - **增量探测**（推荐）：仅探测“缺少流信息”的视频，速度极快。
   - **全量探测**：扫描全库所有 STRM 视频并刷新流信息。
   - 点击 **启动预探测** 后可实时查看进度百分比与当前处理文件名，可随时点击 **停止**。
3. **Strm 入库自动探测**：
   - 勾选 **Strm入库自动探测** 并点击 **保存**。当媒体库扫描到新加入的 STRM 文件时，插件将在后台自动完成探测，无需手动操作。

---

## 🔌 API 接口 (REST API Reference)

FastSTRM 提供了标准 RESTful API（需管理员权限 Token 或已登录会话）：

| 请求方式 | 路径 | 描述 |
| :--- | :--- | :--- |
| `GET` | `/FastStrm/Summary` | 获取媒体库 STRM 统计概览（总数/已探测/未探测） |
| `POST` | `/FastStrm/Probe/Start?mode=Incremental` | 启动预探测任务（参数 `Incremental` 或 `Full`） |
| `POST` | `/FastStrm/Probe/Stop` | 停止当前正在运行的预探测任务 |
| `GET` | `/FastStrm/Probe/Status` | 查询当前预探测任务状态、进度及当前处理文件名 |

---

## 🌐 English Overview

**FastSTRM** is a high-performance Jellyfin plugin designed to eliminate playback start delay for `.strm` files (such as AList, WebDAV, CloudDrive, or remote cloud storage streams).

- **Zero-Delay Playback**: Intercepts `IMediaSourceManager` to skip remote `ffprobe` network calls when stream info is already available.
- **Background Prober**: Full/incremental batch probing and automatic background probing on newly added `.strm` media.
- **Dashboard UI**: Clean Web management panel in the Jellyfin Dashboard.
- **Compatibility**: Requires Jellyfin 10.11+ and .NET 9.0.
