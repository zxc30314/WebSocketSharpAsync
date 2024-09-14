internal class Program
{
    private static readonly CancellationTokenSource cts = new();

    private static async Task Main(string[] args)
    {
        List<Task> list = new();
        for (var i = 0; i < 100; i++)
        {
            list.Add(Task.Run(Start));
        }

        await Task.WhenAll(list.ToArray());
    }

    private static void OnClose(string obj) => Console.WriteLine(obj);

    private static void OnError(Exception obj) => Console.WriteLine(obj);

    private static void OnMessage(string obj) => Console.WriteLine(obj);

    private static async Task Start()
    {
        var webSocketClient = new WebSocketClient(new("ws://localhost:5000/echo"), OnMessage, OnError, OnClose);
        await webSocketClient.ConnectAsync(cts.Token);

        await webSocketClient.SendAsync("AAA", cts.Token);
        await webSocketClient.CloseAsync("Logout", cts.Token);
        await Task.Delay(5000);
    }
}