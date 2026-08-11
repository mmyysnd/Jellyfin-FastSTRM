Jellyfin FastSTRM Plugin
这是一个专为 Jellyfin 打造的 .strm 文件播放加速插件，完美解决网页端播放直链时由于转码探测导致的漫长起播延迟问题。

使用方法

方式一：直接下载安装（推荐）

进入本项目的 Releases 页面（ https://github.com/mmyysnd/Jellyfin-FastSTRM/releases ）。下载最新编译好的 FastSTRM.dll 文件。
将下载的 FastSTRM.dll 文件放入你 Jellyfin 服务端数据目录下的 plugins/FastSTRM 文件夹内（如果没有 FastSTRM 文件夹，请自行新建）。
重启你的 Jellyfin 服务程序。
进入 Jellyfin 的“控制台” -> “插件”页面，确认 FastSTRM 插件已成功加载。

方式二：下载源码自行编译
如果你想自行编译此项目，请确保你已经安装了 .NET 9.0 SDK。

```# 克隆仓库
git clone https://github.com/mmyysnd/Jellyfin-FastSTRM.git
cd FastSTRM

# 编译生成 dll 文件

dotnet build -c Release
```
编译成功后，在 bin/Release/net9.0/ 目录下找到 FastSTRM.dll 文件，按照上方“方式一”的部署步骤，将其放进 Jellyfin 的插件目录并重启服务即可。
