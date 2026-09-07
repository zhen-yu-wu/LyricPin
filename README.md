# LyricPin

一款轻量、简洁的 Windows 网易云桌面歌词工具。

LyricPin 通过网易云音乐客户端的本地调试接口读取播放状态与歌词，支持逐字高亮、长歌词滚动、毛玻璃背景和悬停播放控制。程序仅驻留在系统托盘，不会修改或安装文件到网易云音乐目录。

## 功能特性

- 与网易云音乐播放进度实时同步
- 支持 YRC 逐字歌词高亮
- 支持一行或三行歌词布局
- 三行歌词自然滑动切换
- 长歌词根据当前播放进度横向滚动
- 鼠标悬停显示上一首、播放/暂停和下一首按钮
- 自定义字体大小、字体颜色与歌词宽度
- 可调节的圆角毛玻璃背景与透明度
- 支持始终置顶、固定位置和鼠标穿透
- 浅色现代化托盘设置菜单
- 仅驻留系统托盘，不显示任务栏按钮

## 下载与使用

前往 [Releases](https://github.com/zhen-yu-wu/LyricPin/releases) 下载最新版本。

推荐普通用户下载独立运行版 `LyricPin.exe`。该版本是单文件程序，无需安装，也无需额外安装 .NET 运行库。

### 首次启动

1. 完全退出正在运行的网易云音乐，包括系统托盘中的网易云音乐进程。
2. 运行 `LyricPin.exe`。
3. LyricPin 会使用本地调试参数启动网易云音乐。
4. 在网易云音乐中播放歌曲，桌面歌词会自动显示。
5. 右键单击 LyricPin 系统托盘图标可打开设置。

> LyricPin 启动后不会显示任务栏按钮，这是正常行为。所有设置和退出操作都位于系统托盘菜单中。

## 设置说明

| 设置分组 | 可用选项 |
| --- | --- |
| 播放控制 | 上一首、播放/暂停、下一首、悬停控制按钮开关 |
| 歌词样式 | 显示行数、歌词宽度、字体大小、字体颜色 |
| 毛玻璃 | 毛玻璃开关、背景透明度 |
| 窗口行为 | 始终置顶、固定位置、鼠标穿透 |

默认使用三行歌词、`300 px` 宽度和 `20 px` 字体。毛玻璃背景默认关闭。

## 工作原理与隐私

LyricPin 使用 `127.0.0.1:9223` 与本机网易云音乐客户端通信，并读取当前歌曲、播放进度及歌词信息。

- 调试端口仅监听本机地址 `127.0.0.1`
- 不需要网易云账号密码
- 不会上传播放记录或个人信息
- 不会修改网易云音乐安装目录
- 远程逐字歌词仅从网易云音乐官方接口获取

## 系统要求

- Windows 10 或 Windows 11（64 位）
- 网易云音乐 Windows 客户端

独立运行版不需要安装 .NET。紧凑版及源码开发需要 [.NET 8 Desktop Runtime/SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

## 源码运行

克隆仓库并进入项目目录：

```powershell
git clone https://github.com/zhen-yu-wu/LyricPin.git
cd LyricPin
dotnet run --project .\LyricPin.csproj
```

调试前同样需要完全退出网易云音乐，再启动 LyricPin。

## 构建

普通 Release 构建：

```powershell
dotnet build .\LyricPin.csproj --configuration Release
```

### 独立运行版

包含 .NET 运行库，可直接复制到其他 Windows 电脑运行，当前体积约为 `63 MB`：

```powershell
dotnet publish .\LyricPin.csproj `
  -p:PublishProfile=PortableWinX64 `
  -o .\publish\LyricPin-portable-win-x64
```

### 紧凑版

当前体积约为 `0.24 MB`，目标电脑需要预先安装 .NET 8 Desktop Runtime：

```powershell
dotnet publish .\LyricPin.csproj `
  -p:PublishProfile=CompactWinX64 `
  -o .\publish\LyricPin-compact-win-x64
```

项目没有启用程序集裁剪，因为 WPF 和 Windows Forms 的动态加载机制可能在裁剪后导致运行时功能缺失。

## 常见问题

### 播放歌曲后没有歌词

请同时退出 LyricPin 和网易云音乐，确认网易云音乐进程已完全关闭，然后先启动 LyricPin。直接启动已经运行的网易云音乐无法补加本地调试参数。

### 歌词不同步或跳转进度后没有更新

先尝试切换歌曲或重新启动 LyricPin。网易云音乐客户端升级后，其内部接口可能发生变化，也可能暂时影响歌词读取。

### 开启鼠标穿透后无法拖动歌词

右键单击系统托盘图标，在“窗口行为”中关闭“鼠标穿透”，然后再移动歌词窗口。

### Windows 提示未知发布者

当前发布文件未进行商业代码签名，Windows SmartScreen 可能显示提示。请仅从本项目 GitHub Releases 页面下载。

## 兼容性说明

LyricPin 依赖网易云音乐客户端的内部播放与歌词接口。网易云音乐版本更新后，相关功能可能需要同步适配。

本项目是非官方第三方工具，与网易云音乐及其开发公司无隶属或合作关系。
