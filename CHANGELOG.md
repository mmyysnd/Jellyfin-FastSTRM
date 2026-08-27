# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [3.0.0] - 2026-08-27

### 🚀 Major Architectural Rewrite & Features (重大重构与全新特性)

- **核心播放拦截管线重构 (IMediaSourceManager Decorator)**:
  - 彻底摒弃旧版侵入式的 HTTP ActionFilter / PlaybackInfoFilter 机制，改为在 Jellyfin 依赖注入容器中以装饰器模式包装 `IMediaSourceManager`。
  - 实现高效智能的三路分流策略：
    1. **非 `.strm` 文件**：100% 原始透传，不产生任何副作用；
    2. **已有流信息的 `.strm` 文件**（`RunTimeTicks > 0`）：直接禁用 `allowMediaProbe`，跳过远程 `ffprobe` 网络探测，真正实现秒开起播；
    3. **缺少流信息的 `.strm` 文件**（首次入库或未探测）：放行官方媒体探测，保证流信息正确写入数据库并维持兼容性。
  - 除核心拦截点 `GetPlaybackMediaSources` 外，其余所有接口方法均 100% 委托底层原生实现，保证稳定性。

- **新增后台预探测服务 (`ProbeService`)**:
  - 新增 `ProbeService` 作为 Hosted Singleton 运行在 Jellyfin 生命周期中。
  - 支持全媒体库 `.strm` 文件的后台异步探测：
    - **增量探测 (Incremental)**：仅扫描并探测数据库中缺失流信息的 `.strm` 媒体；
    - **全量探测 (Full)**：对全库所有 `.strm` 媒体执行元数据与流信息深度刷新。
  - 支持 **入库自动探测 (Auto-Probe on Item Added)**：监听 `ILibraryManager.ItemAdded` 事件，当新 `.strm` 文件入库时自动在后台触发媒体探测，无需用户手动干预。
  - 提供探测任务的取消与中断控制机制（支持优雅停止与异常跳过）。

- **全新控制台 Web 管理面板 (Admin Dashboard)**:
  - 新增专用的 Jellyfin 控制台插件配置页面（`configPage.html` & `configPage.js`）。
  - **媒体统计仪表盘**：实时展示 `.strm 媒体总数`、`已有流信息数` 以及 `缺少流信息数`。
  - **任务控制中心**：支持一键选择“增量探测”或“全量探测”模式并启动/停止任务。
  - **实时进度监控**：可视化进度条与状态显示（空闲、运行中、停止中），精确显示当前正在探测的媒体名称与索引百分比。
  - **自动化选项配置**：支持在界面中一键开启/关闭“Strm 入库自动探测”功能。

- **新增 RESTful 管理 API (`FastStrmController`)**:
  - 提供需管理员权限 (`RequiresElevation`) 的 API 接口：
    - `GET /FastStrm/Summary`: 获取媒体库 `.strm` 统计概览。
    - `POST /FastStrm/Probe/Start?mode={Incremental|Full}`: 启动预探测任务。
    - `POST /FastStrm/Probe/Stop`: 停止当前预探测任务。
    - `GET /FastStrm/Probe/Status`: 查询当前探测进度及状态。

- **规范化打包与清单支持**:
  - 新增 `meta.json` 与 `build.yaml` 清单文件，支持发布至自定义 Jellyfin 插件源仓库。
  - 升级适配 **.NET 9.0** 与 **Jellyfin 10.11+**。

---

## [2.0.0] - 2026-08-20

### Added
- 引入 `FastStrmPlaybackInfoFilter` 尝试在播放请求阶段拦截并修改媒体源信息。
- 完善针对直链流媒体播放探测超时的基础处理。

### Changed
- 重构部分服务注册逻辑。

---

## [1.0.0] - 2026-08-10

### Added
- 初始版本发布。
- 提供基础的 `.strm` 播放探测拦截原型，尝试消除 Web 端起播等待延迟。
