using System.Net.WebSockets;
using System.Text;

public class EchoBehavior : WebSocketBehavior
{
    protected override async Task OnOpenAsync()
    {
        Console.WriteLine("WebSocketSharpAsync connection opened.");
        await Task.CompletedTask;
    }

    protected override async Task OnMessageAsync(string receivedMessage, CancellationToken ctsToken)
    {
        Console.WriteLine($"Received: {receivedMessage}");
        var responseMessage = $"Echo: {receivedMessage}";
        var buffer = Encoding.UTF8.GetBytes(responseMessage);
        await SendAsync(new(buffer), WebSocketMessageType.Text, true, ctsToken);
    }

    protected override async Task OnErrorAsync(Exception exception)
    {
        Console.WriteLine($"Error: {exception.Message}");
        await Task.CompletedTask;
    }

    protected override async Task OnCloseAsync(WebSocketCloseStatus resultCloseStatus, string? resultCloseStatusDescription, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Connection closed: {resultCloseStatus}, {resultCloseStatusDescription}");
        await Context.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, resultCloseStatusDescription, cancellationToken);
        await Task.CompletedTask;
    }
}