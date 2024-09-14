using System.Net.WebSockets;
using System.Text;

public abstract class WebSocketBehavior
{
    public WebSocketContext Context { get; init; }
    private WebSocket _webSocket => Context?.WebSocket;

    protected internal abstract Task OnOpenAsync();
    protected abstract Task OnMessageAsync(string receivedMessage, CancellationToken ctsToken);
    protected internal abstract Task OnErrorAsync(Exception exception);

    protected abstract Task OnCloseAsync(WebSocketCloseStatus resultCloseStatus, string? resultCloseStatusDescription, CancellationToken cancellationToken);

    protected async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        if (_webSocket != null)
        {
            await _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
    }

    public async Task HandleWebSocketAsync(CancellationTokenSource cts, Action<string, string> kill)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            var result = await _webSocket.ReceiveAsync(new(buffer), cts.Token);
            while (!result.CloseStatus.HasValue && !cts.Token.IsCancellationRequested)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await OnMessageAsync(receivedMessage, cts.Token);
                if (!_webSocket.CloseStatus.HasValue)
                {
                    result = await _webSocket.ReceiveAsync(new(buffer), cts.Token);
                }
            }

            kill?.Invoke(Context.RequestUri.AbsolutePath, Context.SecWebSocketKey);
            await OnCloseAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, cts.Token);
        }
        catch (WebSocketException e)
        {
            kill?.Invoke(Context.RequestUri.AbsolutePath, Context.SecWebSocketKey);
            // await OnCloseAsync(WebSocketCloseStatus.NormalClosure,  e.WebSocketErrorCode.ToString(), cts.Token);
        }
    }
}