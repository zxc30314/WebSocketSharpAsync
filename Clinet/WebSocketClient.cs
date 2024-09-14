using System.Net.WebSockets;
using System.Text;

internal class WebSocketClient
{
    private readonly Action<string> _onClose;
    private readonly Action<Exception> _onError;
    private readonly Action<string> _onMessage;
    private readonly ClientWebSocket _ws;
    private readonly Uri serverUri;

    public WebSocketClient(Uri serverUri, Action<string> onMessage, Action<Exception> onError, Action<string> onClose)
    {
        _ws = new();
        _onClose = onClose;
        _onError = onError;
        _onMessage = onMessage;
        this.serverUri = serverUri;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _ws.ConnectAsync(serverUri, cancellationToken);
            Task.Run(async () => { await Receive(_ws, cancellationToken); }, cancellationToken);
        }
        catch (Exception e)
        {
            _onError?.Invoke(e);
        }
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new(buffer), WebSocketMessageType.Text, true, cancellationToken);
        }
        catch (Exception e)
        {
            _onError?.Invoke(e);
        }
    }

    public async Task CloseAsync(string statusDescription, CancellationToken cancellationToken)
    {
        try
        {
            await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, statusDescription, cancellationToken);
            _onClose?.Invoke(statusDescription);
        }
        catch (Exception e)
        {
            _onError?.Invoke(e);
        }
    }

    private async Task Receive(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _onClose?.Invoke(result.CloseStatusDescription);
                    ws.Dispose();
                    break;
                }

                Console.WriteLine($"Message received: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                _onMessage?.Invoke(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        }
        catch (WebSocketException e)
        {
            _onClose?.Invoke(e.WebSocketErrorCode.ToString());
        }
    }
}