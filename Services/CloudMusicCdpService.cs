using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using LyricPin.Models;

namespace LyricPin.Services;

public sealed partial class CloudMusicCdpService : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(1)
    };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly byte[] _receiveBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
    private readonly MemoryStream _messageBuffer = new(16 * 1024);

    private ClientWebSocket? _socket;
    private int _messageId;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly CancellationToken _shutdownToken;
    private int _disposed;

    public CloudMusicCdpService()
    {
        _shutdownToken = _shutdown.Token;
    }

    public bool IsConnected => _socket?.State == WebSocketState.Open;

    public Task<bool> PreviousTrackAsync() => ExecuteCommandAsync(PreviousTrackExpression);

    public Task<bool> TogglePlaybackAsync() => ExecuteCommandAsync(TogglePlaybackExpression);

    public Task<bool> NextTrackAsync() => ExecuteCommandAsync(NextTrackExpression);

    public async Task<CloudMusicLyricSnapshot?> GetCurrentLyricAsync()
    {
        var value = await ExecuteAsync(LyricExpression);
        if (value is null)
        {
            return null;
        }

        try
        {
            return ParseSnapshot(value.Value);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            return null;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _shutdown.Cancel();
        _ = DisposeResourcesAsync();
    }

    private async Task DisposeResourcesAsync()
    {
        // The active request must finish using the pooled buffer before it is returned.
        await _gate.WaitAsync();
        try
        {
            ResetConnection();
            _httpClient.Dispose();
            _messageBuffer.Dispose();
            ArrayPool<byte>.Shared.Return(_receiveBuffer);
            _shutdown.Dispose();
        }
        finally
        {
            // Keep the managed semaphore available for callers already waiting on it.
            _gate.Release();
        }
    }

    private async Task<bool> ExecuteCommandAsync(string expression)
    {
        var result = await ExecuteAsync(expression);
        return result?.ValueKind == JsonValueKind.True;
    }

    private async Task<JsonElement?> ExecuteAsync(string expression)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return null;
        }

        var entered = false;
        try
        {
            await _gate.WaitAsync(_shutdownToken);
            entered = true;
            _shutdownToken.ThrowIfCancellationRequested();
            await EnsureConnectedAsync();
            return await EvaluateAsync(expression);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            if (entered)
            {
                ResetConnection();
            }
            return null;
        }
        finally
        {
            if (entered)
            {
                _gate.Release();
            }
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_socket?.State == WebSocketState.Open)
        {
            return;
        }

        using var response = await _httpClient.GetAsync("http://127.0.0.1:9223/json", _shutdownToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(_shutdownToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: _shutdownToken);

        var target = document.RootElement.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("type", out var type) && type.GetString() == "page" &&
            item.TryGetProperty("url", out var url) &&
            url.GetString()?.StartsWith("orpheus://", StringComparison.Ordinal) == true);

        if (target.ValueKind == JsonValueKind.Undefined ||
            !target.TryGetProperty("webSocketDebuggerUrl", out var webSocketUrl))
        {
            throw new InvalidOperationException("网易云调试页面不可用。");
        }

        _socket = new ClientWebSocket();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        await _socket.ConnectAsync(new Uri(webSocketUrl.GetString()!), timeout.Token);
        var ready = await EvaluateAsync(BootstrapExpression);
        if (ready.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException("网易云播放器尚未完成初始化。");
        }

        var readerReady = await EvaluateAsync(InstallLyricReaderExpression);
        if (readerReady.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException("歌词读取器初始化失败。");
        }
    }

    private async Task<JsonElement> EvaluateAsync(string expression, bool awaitPromise = false)
    {
        if (_socket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("网易云调试连接未建立。");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var id = Interlocked.Increment(ref _messageId);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            id,
            method = "Runtime.evaluate",
            @params = new
            {
                expression,
                returnByValue = true,
                awaitPromise
            }
        });

        await _socket.SendAsync(
            payload,
            WebSocketMessageType.Text,
            endOfMessage: true,
            timeout.Token);

        while (true)
        {
            var message = await ReceiveMessageAsync(_socket, timeout.Token);
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id)
            {
                continue;
            }

            if (root.TryGetProperty("error", out var error))
            {
                throw new InvalidOperationException(error.ToString());
            }

            var result = root.GetProperty("result");
            if (result.TryGetProperty("exceptionDetails", out var exception))
            {
                throw new InvalidOperationException(exception.ToString());
            }

            return result.GetProperty("result").GetProperty("value").Clone();
        }
    }

    private async Task<ReadOnlyMemory<byte>> ReceiveMessageAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        _messageBuffer.SetLength(0);
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(_receiveBuffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("网易云关闭了调试连接。");
            }

            _messageBuffer.Write(_receiveBuffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        if (!_messageBuffer.TryGetBuffer(out var message))
        {
            throw new InvalidOperationException("无法读取网易云响应缓冲区。");
        }

        return new ReadOnlyMemory<byte>(message.Array!, message.Offset, message.Count);
    }

    private static CloudMusicLyricSnapshot? ParseSnapshot(JsonElement value)
    {
        var name = value.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        _ = long.TryParse(value.GetProperty("id").GetString(), out var songId);
        var artist = value.TryGetProperty("artist", out var artistElement)
            ? artistElement.GetString() ?? string.Empty
            : string.Empty;
        var durationSeconds = value.TryGetProperty("duration", out var durationElement)
            ? durationElement.GetDouble()
            : 0;
        var isPlaying = value.TryGetProperty("playing", out var playingElement) &&
                        playingElement.GetBoolean();
        var lineIndex = value.TryGetProperty("index", out var indexElement)
            ? indexElement.GetInt32()
            : 0;
        var text = value.TryGetProperty("text", out var textElement)
            ? textElement.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        var previousText = value.TryGetProperty("previousText", out var previousElement)
            ? previousElement.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        var nextText = value.TryGetProperty("nextText", out var nextElement)
            ? nextElement.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        var lineProgress = value.TryGetProperty("lineProgress", out var progressElement)
            ? progressElement.GetDouble()
            : 0;

        return new CloudMusicLyricSnapshot(
            songId,
            name,
            artist,
            TimeSpan.FromSeconds(durationSeconds),
            isPlaying,
            lineIndex,
            previousText,
            text,
            nextText,
            lineProgress);
    }

    private void ResetConnection()
    {
        _socket?.Dispose();
        _socket = null;
    }
}
