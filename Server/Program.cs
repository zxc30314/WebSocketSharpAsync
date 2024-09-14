internal class Program
{
    private static async Task Main(string[] args)
    {
        var server = new WebSocketServer("http://localhost:5000/");
        server.AddWebSocketService<EchoBehavior>("echo");
        server.StartAsync();
        await Task.Delay(5000);
        await server.KillAsync("echo");
        await Task.Delay(15000);
        server.ListConnect();
        // await server.DisposeAsync();
    }
}