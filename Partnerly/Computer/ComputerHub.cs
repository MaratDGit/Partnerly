using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;
using Partnerly.Automation.Integration;
using Partnerly.Integrations.Common;

namespace Partnerly.Computer
{
    // Endpoint of the Partnerly Agent program running on a user's PC. It does not use
    // the site cookie: the program proves itself with the pairing token (ComputerToken),
    // which is bound to exactly one "computer" connection of one user.
    public class ComputerHub : Hub
    {
        private const int MaxEventBytes = 8 * 1024;
        private const string ConnectionItem = "computerConnection";

        private readonly IIntegrationConnectionService _connections;
        private readonly ComputerGateway _gateway;
        private readonly AutomationTriggerDispatcher _dispatcher;
        private readonly ILogger<ComputerHub> _logger;

        public ComputerHub(
            IIntegrationConnectionService connections,
            ComputerGateway gateway,
            AutomationTriggerDispatcher dispatcher,
            ILogger<ComputerHub> logger)
        {
            _connections = connections;
            _gateway = gateway;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var connection = await AuthenticateAsync();
            if (connection == null)
            {
                // Tell the program why, so it can show "token refused" instead of retrying for ever.
                await Clients.Caller.SendAsync("Rejected");
                Context.Abort();
                return;
            }

            Context.Items[ConnectionItem] = connection.Id;
            await _gateway.AttachAsync(connection.Id, Context.ConnectionId);
            await RaiseAsync(connection, ComputerTypeCodes.StatusChanged, new JsonObject
            {
                ["state"] = "online",
                ["machine"] = connection.GetString("machine")
            }, config => ComputerEvents.MatchesStatus(config, "online"));
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.Items.TryGetValue(ConnectionItem, out var value) && value is Guid id && _gateway.Detach(id, Context.ConnectionId))
            {
                var connection = await _connections.GetAsync(id);
                if (connection != null)
                {
                    await RaiseAsync(connection, ComputerTypeCodes.StatusChanged, new JsonObject
                    {
                        ["state"] = "offline",
                        ["machine"] = connection.GetString("machine")
                    }, config => ComputerEvents.MatchesStatus(config, "offline"));
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // The agent introduces itself once after connecting; the site shows this on the card.
        public async Task Hello(string machine, string os, string version)
        {
            var connection = await CurrentAsync();
            if (connection == null)
            {
                return;
            }

            connection.Set("machine", Limit(machine, 80));
            connection.Set("os", Limit(os, 120));
            connection.Set("agentVersion", Limit(version, 30));
            connection.Set("lastSeen", DateTime.UtcNow.ToString("O"));
            connection.Set("lastState", "online");
            connection.MarkOk(DateTime.UtcNow);
            await _connections.SaveAsync(connection);
        }

        // The person turned the program off or removed it: the card can say so instead of just "offline".
        public async Task Goodbye(string reason)
        {
            var connection = await CurrentAsync();
            if (connection == null)
            {
                return;
            }

            connection.Set("lastState", reason is "paused" or "uninstalled" ? reason : "offline");
            connection.Set("lastSeen", DateTime.UtcNow.ToString("O"));
            await _connections.SaveAsync(connection);
        }

        // The lists the PC allows (folders, programs, scripts), so the card editor can offer them in a dropdown.
        // Only short plain texts are kept; the PC decides what it really allows, this is just a hint.
        public async Task Settings(string json)
        {
            var connection = await CurrentAsync();
            if (connection == null || string.IsNullOrEmpty(json) || json.Length > 32 * 1024 || JsonNode.Parse(json) is not JsonObject source)
            {
                return;
            }

            var clean = new JsonObject();
            foreach (var key in new[] { "folders", "programs", "scripts", "confirm" })
            {
                var list = new JsonArray();
                if (source[key] is JsonArray items)
                {
                    foreach (var item in items.Take(60))
                    {
                        var text = item?.ToString();
                        if (!string.IsNullOrWhiteSpace(text) && text.Length <= 260)
                        {
                            list.Add(text);
                        }
                    }
                }

                clean[key] = list;
            }

            connection.Set("settings", clean.ToJsonString());
            await _connections.SaveAsync(connection);
        }

        public Task Result(string commandId, bool ok, string? dataJson, string? error)
        {
            if (Context.Items.TryGetValue(ConnectionItem, out var value) && value is Guid id)
            {
                var data = new JsonObject();
                if (!string.IsNullOrWhiteSpace(dataJson) && dataJson.Length < 14 * 1024 * 1024)
                {
                    try { data = JsonNode.Parse(dataJson) as JsonObject ?? new JsonObject(); }
                    catch (System.Text.Json.JsonException) { /* an unreadable answer counts as empty */ }
                }

                _gateway.Complete(id, commandId, new ComputerResponse { Ok = ok, Data = data, Error = Limit(error, 400) });
            }

            return Task.CompletedTask;
        }

        // The agent reports what happened on the PC. What may be reported (folders, programs)
        // is chosen in the agent's own settings on that PC, not here.
        public async Task Event(string type, string payloadJson)
        {
            if (payloadJson == null || payloadJson.Length > MaxEventBytes || !_gateway.AllowEvent(CurrentId()))
            {
                return;
            }

            var connection = await CurrentAsync();
            if (connection == null || JsonNode.Parse(payloadJson) is not JsonObject payload)
            {
                return;
            }

            payload["machine"] = connection.GetString("machine");
            switch (type)
            {
                case "file_created":
                    await RaiseAsync(connection, ComputerTypeCodes.FileCreated, payload, config => ComputerEvents.MatchesFile(config, payload));
                    break;
                case "program_started":
                    await RaiseAsync(connection, ComputerTypeCodes.ProgramStarted, payload, config => ComputerEvents.MatchesProgram(config, payload));
                    break;
                case "file_changed":
                    await RaiseAsync(connection, ComputerTypeCodes.FileChanged, payload, config => ComputerEvents.MatchesFile(config, payload));
                    break;
                case "file_deleted":
                    await RaiseAsync(connection, ComputerTypeCodes.FileDeleted, payload, config => ComputerEvents.MatchesFile(config, payload));
                    break;
                case "program_stopped":
                    await RaiseAsync(connection, ComputerTypeCodes.ProgramStopped, payload, config => ComputerEvents.MatchesProgram(config, payload));
                    break;
                case "session_changed":
                    await RaiseAsync(connection, ComputerTypeCodes.SessionChanged, payload, config => ComputerEvents.MatchesStatus(config, payload["state"]?.ToString() ?? string.Empty));
                    break;
                case "clipboard_changed":
                    await RaiseAsync(connection, ComputerTypeCodes.ClipboardChanged, payload, config => ComputerEvents.MatchesClipboard(config, payload));
                    break;
                case "power_changed":
                    await RaiseAsync(connection, ComputerTypeCodes.PowerChanged, payload, config => ComputerEvents.MatchesStatus(config, payload["state"]?.ToString() ?? string.Empty));
                    break;
                case "drive_changed":
                    await RaiseAsync(connection, ComputerTypeCodes.DriveChanged, payload, config => ComputerEvents.MatchesDrive(config, payload, checkState: true));
                    break;
                case "idle_changed":
                    await RaiseAsync(connection, ComputerTypeCodes.IdleChanged, payload, config => ComputerEvents.MatchesStatus(config, payload["state"]?.ToString() ?? string.Empty));
                    break;
                case "disk_low":
                    await RaiseAsync(connection, ComputerTypeCodes.DiskLow, payload, config => ComputerEvents.MatchesDrive(config, payload, checkState: false));
                    break;
            }
        }

        private Guid CurrentId() =>
            Context.Items.TryGetValue(ConnectionItem, out var value) && value is Guid id ? id : Guid.Empty;

        private async Task<IntegrationConnection?> CurrentAsync()
        {
            var id = CurrentId();
            return id == Guid.Empty ? null : await _connections.GetAsync(id);
        }

        private async Task<IntegrationConnection?> AuthenticateAsync()
        {
            var http = Context.GetHttpContext();
            var token = http?.Request.Query["access_token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                var header = http?.Request.Headers["Authorization"].ToString() ?? string.Empty;
                token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..] : string.Empty;
            }

            if (!ComputerToken.TryParse(token, out var id, out var secret))
            {
                return null;
            }

            var connection = await _connections.GetAsync(id);
            if (connection == null || connection.Kind != IntegrationKinds.Computer || !ComputerToken.Matches(secret, connection.GetString("tokenHash")))
            {
                _logger.LogWarning("Computer hub: a connection with a wrong token was refused.");
                return null;
            }

            return connection;
        }

        private async Task RaiseAsync(IntegrationConnection connection, string type, JsonObject payload, Func<System.Text.Json.JsonElement, bool> matches)
        {
            try
            {
                await _dispatcher.EnqueueIntegrationAsync(type, connection.OwnerUserId, connection.Id, payload, matches);
            }
            catch (Exception ex)
            {
                // A failing automation must never break the agent's connection.
                _logger.LogWarning(ex, "Computer event {Type} could not start agents.", type);
            }
        }

        private static string? Limit(string? text, int max) =>
            text == null ? null : (text.Length > max ? text[..max] : text);
    }
}
