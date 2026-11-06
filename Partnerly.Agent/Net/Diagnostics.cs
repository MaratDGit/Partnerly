using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Partnerly.Agent.Config;

namespace Partnerly.Agent.Net
{
    // "Check connection" in the tray menu: walks the same way the program goes to the site and
    // says in plain words where it stops. Never prints the token.
    public static class Diagnostics
    {
        public static async Task<string> RunAsync(AgentConfig config, AgentConnection connection)
        {
            var report = new StringBuilder();
            void Line(bool ok, string text) => report.AppendLine((ok ? "[ OK ]  " : "[ !! ]  ") + text);

            Line(config.Enabled, config.Enabled ? "The program is turned on." : "The program is turned OFF in the tray menu - turn it on.");
            if (!Uri.TryCreate(config.Server, UriKind.Absolute, out var uri))
            {
                Line(false, "The site address is not valid. Use \"Connection...\" in the tray menu.");
                return report.ToString();
            }

            Line(true, "Site address: " + uri.GetLeftPart(UriPartial.Authority));
            Line(config.Token.StartsWith("pc.", StringComparison.Ordinal), config.Token.StartsWith("pc.", StringComparison.Ordinal)
                ? "The token is in the settings."
                : "The token looks wrong (it must start with pc.). Use \"Connection...\".");

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(uri.Host);
                Line(true, "The address was found (" + addresses.Length + " IP).");
            }
            catch (SocketException)
            {
                Line(false, "The site address was not found. Check the spelling and the internet connection.");
                return report.ToString();
            }

            try
            {
                using var tcp = new TcpClient();
                var connect = tcp.ConnectAsync(uri.Host, uri.Port);
                if (await Task.WhenAny(connect, Task.Delay(5000)) != connect || !tcp.Connected)
                {
                    Line(false, "The site does not answer on port " + uri.Port + ". Is it running? Is a firewall or VPN in the way?");
                    return report.ToString();
                }

                Line(true, "The site answers.");
            }
            catch (SocketException ex)
            {
                Line(false, "Cannot reach the site: " + AgentConnection.Explain(ex));
                return report.ToString();
            }

            try
            {
                var handler = new HttpClientHandler();
                if (uri.IsLoopback || uri.Host == "localhost")
                {
                    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                }

                using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                using var response = await http.PostAsync(config.Server.TrimEnd('/') + "/computerHub/negotiate?negotiateVersion=1", null);
                Line(response.IsSuccessStatusCode, response.IsSuccessStatusCode
                    ? "This is a Partnerly site with computers support."
                    : "The address answers, but it is not a Partnerly site with computers support (code " + (int)response.StatusCode + ").");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Line(false, "The secure connection failed: " + AgentConnection.Explain(ex));
                return report.ToString();
            }

            Line(connection.IsConnected, connection.IsConnected
                ? "Connected - the site sees this computer."
                : "Not connected now: " + (connection.LastProblem.Length > 0 ? connection.LastProblem : "the program is trying; wait a minute and check again."));
            return report.ToString();
        }
    }
}
