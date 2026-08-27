# Jellyfin-FastSTRM

[![Release](https://img.shields.io/github/v/release/mmyysnd/Jellyfin-FastSTRM?color=blue&label=version)](https://github.com/mmyysnd/Jellyfin-FastSTRM/releases)
[![Target](https://img.shields.io/badge/Jellyfin-10.11%2B-purple.svg)](https://jellyfin.org/)
[![Framework](https://img.shields.io/badge/.NET-9.0-blueviolet.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/mmyysnd/Jellyfin-FastSTRM)](LICENSE)

专为 Jellyfin 打造的 `.strm` 媒体文件播放加速插件。通过底层管道拦截与流信息预探测机制，彻底消除播放直链或网盘 STRM 媒体时的 `ffprobe` 远程探测延迟，实现毫秒级秒开起播。

---

## 💡 为什么需要 FastSTRM？

在 Jellyfin 中播放 `.strm` 虚拟流媒体文件时，服务端默认会在每次点击播放时通过 `ffprobe` 建立远程网络连接以探测媒体流参数。面对云盘挂载（如 WebDAV、AList、OpenList 等）或远程直链时，这种探测往往需要消耗 **3~15 秒** 不等的网络等待时间，极大影响观影体验。

**FastSTRM 3.0** 通过在底层注册 `IMediaSourceManager` 装饰器，智能判断媒体流状态：
- 对于已具备流信息的媒体，**直接跳过远程 ffprobe 网络探测**，直接下发播放直链，起播速度提升至毫秒级。
- 配合 **全库预探测** 与 **入库自动后台探测**，将耗时的网络探测移至后台异步完成，做到真正意义上的无感极速播放。

---

## ✨ 核心特性

- ⚡ **毫秒级秒开**：通过依赖注入无缝拦截播放源解析，已探测媒体 0 延迟直接起播。
- 🛡️ **安全三路路由**：
  - 非 `.strm` 文件：100% 原始透传；
  - 已探测 `.strm` 文件：跳过网络探测，秒开直连；
  - 未探测 `.strm` 文件：无缝回退至官方探测管线，确保兼容性。
- 🔍 **全库预探测引擎**：
  - **增量探测 (Incremental)**：仅针对库内缺少流信息的 STRM 媒体发起后台探测；
  - **全量探测 (Full)**：支持对库内全部 STRM 文件重新抓取流信息与时长。
- 🤖 **入库自动探测**：后台监听媒体库新增事件，新文件入库时自动完成媒体流探测入库。
- 📊 **可视化控制台面板**：内置于 Jellyfin 控制台的 Web 仪表盘，支持实时查看媒体库统计、任务执行进度以及一键控制。
- 🌐 **RESTful API 支持**：提供标准管理接口，方便脚本或第三方工具调用监控。

---

## 🛠️ 工作原理

```
[ 用户发起播放请求 ]
         │
         ▼
[ MediaSourceInterceptor (装饰器拦截) ]
         │
 ┌───────┴────────────────────────────┐
 │                                    │
 ▼ 是 .strm 文件                      ▼ 非 .strm 本地文件
检查 RunTimeTicks 流时长               直接走原生播放管线
 │
 ├──▶ 已有流信息 (RunTimeTicks > 0) ──▶ allowMediaProbe = false ──▶ 🚀 毫秒级秒开
 │
 └──▶ 缺少流信息 (首次播放)         ──▶ allowMediaProbe = true  ──▶ 📡 走原生探测并写入数据库
```

---

## 📦 安装与部署

### 方式一：直接下载 Release 安装（推荐）

1. 进入 [Releases 页面](https://github.com/mmyysnd/Jellyfin-FastSTRM/releases) 下载最新的 `FastSTRM.dll`。
2. 进入 Jellyfin 服务端的数据目录，在 `plugins` 文件夹下创建 `FastSTRM` 子目录：
   - **Linux / Docker**: `/config/plugins/FastSTRM/`
   - **Windows**: `%ProgramData%\Jellyfin\Server\plugins\FastSTRM\`
3. 将下载的 `FastSTRM.dll` 复制到该目录下。
4. 重启 Jellyfin 服务。
5. 登录 Jellyfin 管理员账户，进入 **控制台 -> 插件**，即可看到已成功加载的 **FastSTRM**。

> [!TIP]
> 如果您使用的是 Docker 容器部署，将 `FastSTRM.dll` 放入宿主机映射的 `config/plugins/FastSTRM/` 目录并重启容器即可生效。

---

### 方式二：从源码编译构建

构建前请确保系统已安装 [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)。

```bash
# 1. 克隆代码仓库
git clone https://github.com/mmyysnd/Jellyfin-FastSTRM.git
cd Jellyfin-FastSTRM

# 2. 编译发布版本
dotnet build -c Release
```

编译完成后，构建产物位于 `bin/Release/net9.0/FastSTRM.dll`，按照上述步骤放入插件目录即可。

---

## 🖥️ 控制台使用指南

进入 Jellyfin 管理员控制台，在左侧导航栏的 **插件** 分组中点击 **FastSTRM** 进入管理面板：

1. **媒体库统计**：
   - `.strm 媒体数量`：当前媒体库中全部 STRM 视频总数。
   - `已有流信息`：已完成探测、具备秒开条件的媒体数。
   - `缺少流信息`：尚未获取流信息、首次播放仍需探测的媒体数。
2. **启动预探测**：
   - **增量探测（推荐）**：仅处理“缺少流信息”的视频项，速度快、资源占用低。
   - **全量探测**：扫描全库所有 STRM 视频并强制刷新流元数据。
   - 点击 **启动预探测** 后，进度条将实时显示当前探测百分比与当前处理的文件名；可在任何时候点击 **停止** 终止任务。
3. **入库自动探测**：
   - 勾选 **Strm入库自动探测** 并点击 **保存**，当媒体库扫描到新加入的 STRM 文件时，后台服务将自动对其进行探测，无需人工干预。

> [!NOTE]
> 预探测使用 Jellyfin 官方的媒体刷新接口写入媒体流信息，探测结果会持久化至 Jellyfin 数据库中，下次启动或重启均无需重复探测。

---

## 🔌 API 接口说明

FastSTRM 提供了用于远程管理和自动化的 RESTful 接口（需管理员权限 Token 或会话认证）：

| 请求方式 | 路径 | 说明 |
| :--- | :--- | :--- |
| `GET` | `/FastStrm/Summary` | 获取库内 STRM 媒体总数、已探测数与未探测数 |
| `POST` | `/FastStrm/Probe/Start?mode=Incremental` | 启动预探测任务（`Incremental` 增量 / `Full` 全量） |
| `POST` | `/FastStrm/Probe/Stop` | 停止当前正在运行的预探测任务 |
| `GET` | `/FastStrm/Probe/Status` | 获取当前预探测任务状态、进度及当前处理文件名 |

---

## 📋 兼容性要求

- **Jellyfin Server**: 10.11.0 或更高版本
- **.NET 运行环境**: .NET 9.0
