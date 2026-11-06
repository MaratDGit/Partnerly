using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR.Client;
using Partnerly.Agent.Commands;
using Partnerly.Agent.Config;
using Partnerly.Agent.Security;
using Partnerly.Agent.UI;

namespace Partnerly.Agent.Net
{
    // The connection to the site: receives commands, answers them, reports PC events.
    public sealed class AgentConnection : IAsyncDisposable
    {
        private readonly AgentConfig _config;
        private readonly CommandRunner _runner;
        private readonly SemaphoreSlim _busy = new(4);
        private CancellationTokenSource _stop = new();
        private HubConnection? _hub;
        private int _generation;

        public AgentConnection(AgentConfig config, CommandRunner runner)
        {
            _config = config;
            _runner = runner;
        }

        // Text for the tray menu plus the state that picks the icon.
        public event Action<string, AgentState>? StatusChanged;

        // Raised once, the first time the site accepted this computer.
        public event Action? FirstConnected;

        public AgentState State { get; private set; } = AgentState.Connecting;

        public string LastProblem { get; private set; } = string.Empty;

        public bool IsConnected => _hub?.State == HubConnectionState.Connected;

        private void Report(string text, AgentState state)
        {
            State = state;
            StatusChanged?.Invoke(text, state);
        }

        // Starts (or restarts) the connection loop; returns when the first attempt succeeded,
        // the user paused the program or the site refused the token.
        public async Task RunAsync()
        {
            if (!_config.Enabled)
            {
                Report("Paused - turned off in this menu", AgentState.Paused);
                return;
            }

            if (_stop.IsCancellationRequested)
            {
                _stop.Dispose();
                _stop = new CancellationTokenSource();
            }

            var generation = ++_generation;
            var stop = _stop;
            var hub = Build(stop, generation);
            _hub = hub;

            // The first connection can fail (no network yet at start-up, site restarting): keep trying.
            var delay = TimeSpan.FromSeconds(3);
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    Report("Connecting...", AgentState.Connecting);
                    await hub.StartAsync(stop.Token);
                    await HelloAsync(hub);
                    Report("Connected", AgentState.Online);
                    LastProblem = string.Empty;
                    AgentLog.Write("Connected to " + _config.Server);
                    FirstConnected?.Invoke();
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    LastProblem = Explain(ex);
                    AgentLog.Write("Could not connect: " + ex.GetBaseException().Message);
                    Report("Offline - " + LastProblem, AgentState.Offline);
                    try { await Task.Delay(delay, stop.Token); } catch (OperationCanceledException) { return; }
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
                }
            }
        }

        private HubConnection Build(CancellationTokenSource stop, int generation)
        {
            var hub = new HubConnectionBuilder()
                .WithUrl(_config.Server.TrimEnd('/') + "/computerHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_config.Token);
                    if (IsThisComputer(_config.Server))
                    {
                        // A site running on this very PC (development) uses a self-made certificate.
                        // The traffic never leaves the PC, so it is accepted; real sites must have a valid one.
                        options.HttpMessageHandlerFactory = handler =>
                        {
                            if (handler is HttpClientHandler http)
                            {
                                http.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                            }

                            return handler;
                        };
                        options.WebSocketConfiguration = socket => socket.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                    }
                })
                .WithAutomaticReconnect(new KeepTrying())
                .Build();
            hub.ServerTimeout = TimeSpan.FromSeconds(45);
            hub.KeepAliveInterval = TimeSpan.FromSeconds(15);

            hub.On<string, string, string>("Execute", (commandId, op, argsJson) => _ = HandleAsync(hub, commandId, op, argsJson));
            hub.On("Replaced", async () =>
            {
                AgentLog.Write("Another Partnerly Agent connected with the same token. This one stops.");
                Report("Another copy of the program took over this computer", AgentState.Problem);
                LastProblem = "Another copy of the program is connected with the same token.";
                stop.Cancel();
                await hub.StopAsync();
            });
            hub.On("Rejected", async () =>
            {
                AgentLog.Write("The site refused the token. Make a new token on the Integrations page and enter it again.");
                LastProblem = "The site does not accept this token (it was renewed or the computer was deleted on the site).";
                Report("Token refused - use \"Connection\" in the menu", AgentState.Problem);
                stop.Cancel();
                await hub.StopAsync();
            });
            hub.Reconnecting += _ =>
            {
                if (generation == _generation) { Report("Connecting...", AgentState.Connecting); }
                return Task.CompletedTask;
            };
            hub.Reconnected += async _ =>
            {
                if (generation != _generation) { return; }
                await HelloAsync(hub);
                Report("Connected", AgentState.Online);
            };
            hub.Closed += _ =>
            {
                if (generation == _generation && !stop.IsCancellationRequested)
                {
                    Report("Offline", AgentState.Offline);
                }

                return Task.CompletedTask;
            };
            return hub;
        }

        // The user turned the program off: tell the site (so the card says "paused"), then disconnect.
        public async Task PauseAsync(string reason = "paused")
        {
            var hub = _hub;
            _generation++;
            _stop.Cancel();
            if (hub != null)
            {
                try
                {
                    if (hub.State == HubConnectionState.Connected)
                    {
                        await hub.SendAsync("Goodbye", reason);
                    }

                    await hub.StopAsync();
                }
                catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or HttpRequestException)
                {
                    // already disconnected
                }
            }

            Report("Paused - turned off in this menu", AgentState.Paused);
        }

        public async Task ResumeAsync()
        {
            _config.Enabled = true;
            await RunAsync();
        }

        private static bool IsThisComputer(string server) =>
            Uri.TryCreate(server, UriKind.Absolute, out var uri) && (uri.IsLoopback || uri.Host == "localhost");

        public async Task SendEventAsync(string type, JsonObject payload)
        {
            if (_hub?.State == HubConnectionState.Connected)
            {
                try { await _hub.SendAsync("Event", type, payload.ToJsonString()); }
                catch (Exception ex) { AgentLog.Write("Event " + type + " was not sent: " + ex.Message); }
            }
        }

        // The site learns the name of the PC and the lists of allowed folders / programs, so the
        // card editor can offer them in a dropdown. Old sites do not know "Settings": that is fine.
        private async Task HelloAsync(HubConnection hub)
        {
            await hub.SendAsync("Hello", Environment.MachineName,
                System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                typeof(AgentConnection).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
            await SendSettingsAsync(hub);
        }

        public async Task SendSettingsAsync()
        {
            if (_hub?.State == HubConnectionState.Connected)
            {
                await SendSettingsAsync(_hub);
            }
        }

        private async Task SendSettingsAsync(HubConnection hub)
        {
            try { await hub.SendAsync("Settings", _config.ToSiteJson()); }
            catch (Exception ex) { AgentLog.Write("Settings were not sent to the site (an older site?): " + ex.Message); }
        }

        // Human words for the usual reasons.
        public static string Explain(Exception ex)
        {
            var root = ex.GetBaseException();
            var text = root.Message;
            if (root is System.Security.Authentication.AuthenticationException || text.Contains("SSL", StringComparison.OrdinalIgnoreCase))
            {
                return "the certificate of the site is not trusted";
            }

            if (root is SocketException socket)
            {
                return socket.SocketErrorCode switch
                {
                    SocketError.HostNotFound or SocketError.NoData => "the site address was not found (check the address and the internet)",
                    SocketError.ConnectionRefused => "the site does not answer (is it running?)",
                    SocketError.TimedOut => "the site did not answer in time",
                    SocketError.NetworkUnreachable or SocketError.HostUnreachable => "no internet connection",
                    _ => text
                };
            }

            if (text.Contains("404", StringComparison.Ordinal) || text.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
            {
                return "the address is not a Partnerly site (or an old version without computers)";
            }

            return text.Length > 70 ? text[..70] : text;
        }

        private async Task HandleAsync(HubConnection hub, string commandId, string op, string argsJson)
        {
            await _busy.WaitAsync();
            try
            {
                var args = JsonNode.Parse(argsJson) as JsonObject ?? new JsonObject();
                AgentLog.Write("Command: " + op);
                var data = await _runner.RunAsync(op, args, _stop.Token);
                await hub.SendAsync("Result", commandId, true, data.ToJsonString(), (string?)null);
            }
            catch (AgentException ex)
            {
                AgentLog.Write("Refused " + op + ": " + ex.Message);
                await Reply(hub, commandId, ex.Message);
            }
            catch (Exception ex)
            {
                AgentLog.Write("Failed " + op + ": " + ex.Message);
                await Reply(hub, commandId, "The command failed: " + ex.Message);
            }
            finally
            {
                _busy.Release();
            }
        }

        private static async Task Reply(HubConnection hub, string commandId, string error)
        {
            try { await hub.SendAsync("Result", commandId, false, "{}", error); }
            catch (Exception) { /* the connection is gone; the site will time out */ }
        }

        public async ValueTask DisposeAsync()
        {
            _generation++;
            _stop.Cancel();
            if (_hub != null)
            {
                try { await _hub.DisposeAsync(); }
                catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException) { /* closing anyway */ }
            }
        }

        // Retries forever: 0, 2, 5, 10, 30 seconds, then every 30 seconds.
        private sealed class KeepTrying : IRetryPolicy
        {
            private static readonly int[] Seconds = { 0, 2, 5, 10, 30 };

            public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
                TimeSpan.FromSeconds(Seconds[Math.Min(retryContext.PreviousRetryCount, Seconds.Length - 1)]);
        }
    }
}
