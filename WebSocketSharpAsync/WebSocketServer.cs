using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

public class WebSocketServer(string uri) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Func<HttpListenerWebSocketContext, WebSocketBehavior>> _pathList = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocketBehavior>> list = new();
    private CancellationTokenSource _cts;
    private HttpListener _httpListener;
    public bool IsRunning { get; private set; }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_httpListener);
        await CastAndDispose(_cts);
        GC.SuppressFinalize(this);
        Console.WriteLine("WebSocketSharpAsync server dispose.\n");
        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    public void AddWebSocketService<T>(string path) where T : WebSocketBehavior, new()
    {
        var hasBackslash = path.First() == '/';
        path = !hasBackslash ? $"/{path}" : path;
        _pathList.TryAdd(path, x => new T { Context = x });
    }

    public async Task StartAsync()
    {
        if (IsRunning)
        {
            return;
        }

        _httpListener = new();
        _httpListener.Prefixes.Add(uri);
        _cts = new();
        _httpListener.Start();
        IsRunning = true;
        Console.WriteLine("WebSocketSharpAsync server started.");
        while (!_cts.Token.IsCancellationRequested)
        {
            await Loop();
        }

        IsRunning = false;
        _httpListener.Stop();
        Console.WriteLine("WebSocketSharpAsync server stopped.");
    }

    private async Task Loop()
    {
        var httpContext = await _httpListener.GetContextAsync();
        if (httpContext.Request.IsWebSocketRequest)
        {
            var path = httpContext.Request.Url.AbsolutePath;
            list.TryAdd(path, new());

            if (_pathList.TryGetValue(path, out var factory))
            {
                _ = Task.Run(async () =>
                {
                    var wsContext = await httpContext.AcceptWebSocketAsync(null);
                    var webSocketBehavior = factory(wsContext);
                    try
                    {
                        if (list.TryGetValue(path, out var dictionary))
                        {
                            dictionary.TryAdd(wsContext.SecWebSocketKey, webSocketBehavior);
                        }

                        await webSocketBehavior.OnOpenAsync();
                        await webSocketBehavior.HandleWebSocketAsync(_cts, KillConnect);
                    }
                    catch (Exception e)
                    {
                        webSocketBehavior.OnErrorAsync(e);
                        webSocketBehavior.Context.WebSocket.Dispose();
                    }
                }, _cts.Token);
            }
            else
            {
                httpContext.Response.StatusCode = 404;
                httpContext.Response.Close();
            }
        }
        else
        {
            httpContext.Response.StatusCode = 400;
            httpContext.Response.Close();
        }
    }


    private void KillConnect(string path, string webSocketKey)
    {
        if (!list.TryGetValue(path, out var dic))
        {
            return;
        }

        dic.TryRemove(webSocketKey, out _);
    }

    public void ListConnect()
    {
        foreach (var webSocketBehavior in list)
        {
            var webSocketKeys = webSocketBehavior.Value.Keys;
            foreach (var webSocketKey in webSocketKeys)
            {
                Console.WriteLine(webSocketKey);
            }
        }
    }

    public async Task KillAsync(string path)
    {
        var hasBackslash = path.First() == '/';
        path = !hasBackslash ? $"/{path}" : path;
        if (!list.TryGetValue(path, out var dic))
        {
            return;
        }

        foreach (var webSocketBehavior in dic.Values)
        {
            webSocketBehavior.Context.WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "kill", default);
        }
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
        _httpListener.Stop();
    }

    public void Dispose()
    {
        ((IDisposable)_httpListener).Dispose();
        _cts.Dispose();
        Console.WriteLine("WebSocketSharpAsync server disposed.\n");
    }
}