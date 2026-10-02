# LyricPin

轻量的 Windows 网易云桌面歌词工具，支持逐字高亮、长歌词滚动和悬停播放控制。

> [!IMPORTANT]
> **使用前，请在网易云音乐中进入「设置 → 系统」，开启 SMTC。**
>
> 完成设置后，**完全退出网易云音乐（包括系统托盘），再启动 LyricPin**。

## 下载与使用

适用于 Windows 10 / 11（64 位）和网易云音乐 Windows 客户端。

从 [Releases](https://github.com/zhen-yu-wu/LyricPin/releases) 下载：

- **独立运行版（推荐）**：无需安装 .NET，直接运行 `LyricPin.exe`。
- **紧凑版**：需要安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)。

### 首次使用

1. 在网易云音乐的 **设置 → 系统** 中开启 **SMTC**。
2. 完全退出网易云音乐，包括系统托盘中的进程。
3. 启动 `LyricPin.exe`，它会在后台唤醒网易云音乐。
4. 从网易云系统托盘图标打开客户端，播放歌曲，即可显示桌面歌词。
5. 右键 LyricPin 托盘图标，调整设置或退出。

LyricPin 不显示任务栏按钮。它启动网易云时会尝试隐藏主窗口、保留系统托盘图标；部分客户端版本可能短暂显示窗口。网易云已经运行时，LyricPin 不会重复启动或隐藏它，但无法为已有进程补加调试参数。

## 功能与设置

所有设置均位于 LyricPin 托盘右键菜单。

| 设置分组 | 功能                                                      |
| -------- | --------------------------------------------------------- |
| 播放控制 | 上一首、播放 / 暂停、下一首、悬停控制按钮                 |
| 歌词样式 | 一行 / 三行、逐字高亮、长歌词滚动、宽度、字体、字号与颜色 |
| 毛玻璃   | 背景开关与透明度                                          |
| 窗口行为 | 始终置顶、固定位置、鼠标穿透                              |
| 启动设置 | 开机自启动                                                |

默认显示三行歌词，宽度为 `300 px`，字体为 `20 px`；毛玻璃和开机自启动默认关闭。

在「歌词样式 → 字体」中可选择默认字体（Segoe UI）、微软雅黑或宋体。默认字体保持不变，切换后立即生效。

**开机自启动也会在后台唤醒网易云音乐。** 勾选「启动设置 → 开机自启动」后，LyricPin 会在当前用户登录 Windows 时运行；取消勾选即可关闭，无需管理员权限。启用后请保持程序位置不变，移动程序后需从新位置取消勾选并重新开启。

目前仅开机自启动设置会保留；其他样式、窗口选项和窗口位置在重启后恢复默认。

## 常见问题

- **没有歌词或无法连接**：确认已开启 **SMTC**，然后退出 LyricPin 和网易云音乐，再先启动 LyricPin。
- **歌词不同步**：尝试切换歌曲或重启程序。网易云客户端更新可能导致内部接口变化，需要适配。
- **无法拖动歌词**：在「窗口行为」中关闭「固定位置」和「鼠标穿透」。
- **Windows 提示未知发布者**：当前程序未进行代码签名，请从本项目 Releases 下载。

## 源码运行与构建

开发需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。运行前同样需要开启 SMTC，并完全退出网易云音乐。

### 获取源码并运行

```powershell
git clone https://github.com/zhen-yu-wu/LyricPin.git
cd LyricPin
dotnet run --project .\LyricPin.csproj
```

### Release 构建

```powershell
dotnet build .\LyricPin.csproj --configuration Release
```

### 发布独立运行版

包含 .NET 运行库，输出为 Windows x64 单文件程序：

```powershell
dotnet publish .\LyricPin.csproj `
  -p:PublishProfile=PortableWinX64 `
  -o .\publish\LyricPin-portable-win-x64
```

### 发布紧凑版

目标电脑需安装 .NET 8 Desktop Runtime：

```powershell
dotnet publish .\LyricPin.csproj `
  -p:PublishProfile=CompactWinX64 `
  -o .\publish\LyricPin-compact-win-x64
```

两种发布配置均未启用程序集裁剪，以保留 WPF / Windows Forms 运行所需功能。

## 项目结构

- `MainWindow*.cs` / `MainWindow.xaml`：歌词窗口、同步、动画、托盘与启动设置。
- `Models/`：歌曲与歌词状态快照。
- `Services/`：网易云启动、后台窗口处理、CDP 通信与开机自启动。
- `Ui/`：菜单绘制、托盘图标和窗口背景效果。
- `Properties/PublishProfiles/`：两种发布配置。

## 工作原理与说明

LyricPin 通过本机调试接口 `127.0.0.1:9223` 读取网易云的播放状态和歌词；远程逐字歌词从网易云官方接口获取。不需要账号密码，不上传播放记录，不修改网易云安装目录。

本项目为非官方第三方工具，与网易云音乐无隶属或合作关系。功能依赖客户端内部接口，兼容性可能随网易云版本变化。
