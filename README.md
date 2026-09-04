# LyricPin

LyricPin 是一款轻量级 Windows 桌面歌词工具，通过本地调试接口同步网易云音乐的播放状态与歌词。

## 功能

- 网易云音乐歌词与播放进度同步
- 当前歌词逐字高亮
- 一行或三行歌词显示
- 三行歌词自然滑动切换
- 超长当前歌词按播放进度横向移动
- 自定义字体大小、颜色与歌词宽度
- 始终置顶、固定位置和鼠标穿透
- 仅驻留系统托盘
- 半透明亚克力托盘菜单

## 环境要求

- Windows 10 或 Windows 11
- 网易云音乐 Windows 客户端
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## 开发运行

```powershell
dotnet run --project .\LyricPin.csproj
```

首次使用时，请先完全退出网易云音乐，再启动 LyricPin。LyricPin 会使用本地端口 `9223` 启动网易云音乐并读取播放信息，数据不会上传到第三方服务器。

## 构建

```powershell
dotnet build .\LyricPin.csproj --configuration Release
```

生成结果位于 `bin\Release` 目录。
