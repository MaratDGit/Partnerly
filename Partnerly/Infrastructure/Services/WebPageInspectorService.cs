using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Partnerly.Infrastructure.Services
{
    public class WebPageInspectorService : IWebPageInspectorService
    {
        private const int MaxBytes = 1_500_000;
        private const int MaxRedirects = 5;
        private const int MaxListItems = 400;
        private const int MaxVisibleChars = 80_000;

        private static readonly Regex TitleRegex = new(@"<title[^>]*>([\s\S]*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex MetaDescRegex = new(@"<meta\b[^>]*name\s*=\s*[""']description[""'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex MetaOgRegex = new(@"<meta\b[^>]*property\s*=\s*[""']og:description[""'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ContentAttrRegex = new(@"content\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HeadingRegex = new(@"<h([1-6])\b[^>]*>([\s\S]*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LinkRegex = new(@"<a\b([^>]*)>([\s\S]*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ImageRegex = new(@"<img\b([^>]*)/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex FormRegex = new(@"<form\b([^>]*)>([\s\S]*?)</form>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex InputRegex = new(@"<(input|select|textarea)\b([^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TableRegex = new(@"<table\b[^>]*>([\s\S]*?)</table>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RowRegex = new(@"<tr\b[^>]*>([\s\S]*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CellRegex = new(@"<t[hd]\b[^>]*>([\s\S]*?)</t[hd]>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AttrRegex = new(@"([a-zA-Z_:][\w:.-]*)\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.Compiled);
        private static readonly Regex ScriptStyleRegex = new(@"<(script|style|noscript|template|svg)\b[^>]*>[\s\S]*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new(@"<!--[\s\S]*?-->", RegexOptions.Compiled);

        private readonly HttpClient _httpClient;
        private readonly ILogger<WebPageInspectorService> _logger;

        public WebPageInspectorService(
            HttpClient httpClient,
            ILogger<WebPageInspectorService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<WebPageInspectResult> InspectAsync(string? url, CancellationToken cancellationToken = default)
        {
            if (!TryNormalizeUrl(url, out var current) || current == null)
            {
                return Fail(WebPageInspectResult.ErrorInvalidUrl, url);
            }

            var requested = current.ToString();

            try
            {
                HttpResponseMessage? response = null;
                for (var hop = 0; hop <= MaxRedirects; hop++)
                {
                    if (!await IsPublicHttpUrlAsync(current, cancellationToken))
                        return Fail(WebPageInspectResult.ErrorBlockedUrl, requested);

                    using var request = new HttpRequestMessage(HttpMethod.Get, current);
                    request.Headers.TryAddWithoutValidation(
                        "User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,text/plain;q=0.8,*/*;q=0.1");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "en,ru;q=0.8,hy;q=0.7");

                    response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    if (IsRedirect(response.StatusCode) && response.Headers.Location != null)
                    {
                        var next = response.Headers.Location.IsAbsoluteUri
                            ? response.Headers.Location
                            : new Uri(current, response.Headers.Location);
                        response.Dispose();
                        current = next;
                        continue;
                    }

                    break;
                }

                if (response == null)
                    return Fail(WebPageInspectResult.ErrorFetchFailed, requested);

                using (response)
                {
                    if (!await IsPublicHttpUrlAsync(current, cancellationToken))
                        return Fail(WebPageInspectResult.ErrorBlockedUrl, requested);

                    var html = await ReadLimitedStringAsync(response, cancellationToken);
                    if (html == null)
                        return Fail(WebPageInspectResult.ErrorTooLarge, requested);

                    var result = new WebPageInspectResult
                    {
                        Success = true,
                        RequestedUrl = requested,
                        FinalUrl = current.ToString(),
                        StatusCode = (int)response.StatusCode,
                        ContentType = response.Content.Headers.ContentType?.ToString(),
                        ByteCount = Encoding.UTF8.GetByteCount(html),
                        Html = html,
                        Analysis = Analyze(html, current)
                    };

                    return result;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Fail(WebPageInspectResult.ErrorTimeout, requested);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Web inspector failed for {Host}", current.Host);
                return Fail(WebPageInspectResult.ErrorFetchFailed, requested);
            }
        }

        private static WebPageInspectResult Fail(string code, string? url) =>
            new()
            {
                Success = false,
                ErrorCode = code,
                RequestedUrl = url
            };

        private static bool TryNormalizeUrl(string? url, out Uri? uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var trimmed = url.Trim();
            if (!trimmed.Contains("://", StringComparison.Ordinal))
                trimmed = "https://" + trimmed;

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out uri) || uri == null)
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            if (!string.IsNullOrEmpty(uri.UserInfo))
                return false;

            return true;
        }

        private static async Task<bool> IsPublicHttpUrlAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            if (uri.IsLoopback)
                return false;

            var host = uri.Host?.Trim().Trim('[', ']') ?? "";
            if (host.Length == 0)
                return false;

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("metadata", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            IPAddress[] addresses;
            try
            {
                if (IPAddress.TryParse(host, out var parsed))
                    addresses = new[] { parsed };
                else
                    addresses = await Dns.GetHostAddressesAsync(host);
            }
            catch
            {
                return false;
            }

            if (addresses.Length == 0)
                return false;

            foreach (var address in addresses)
            {
                if (IsBlockedAddress(address))
                    return false;
            }

            return true;
        }

        private static bool IsBlockedAddress(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            if (IPAddress.IsLoopback(ip) || Equals(ip, IPAddress.Any) || Equals(ip, IPAddress.IPv6Any))
                return true;

            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                return true;

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var bytes = ip.GetAddressBytes();
                return (bytes[0] & 0xFE) == 0xFC;
            }

            var v4 = ip.GetAddressBytes();
            if (v4[0] == 0 || v4[0] == 10 || v4[0] == 127)
                return true;
            if (v4[0] == 169 && v4[1] == 254)
                return true;
            if (v4[0] == 172 && v4[1] >= 16 && v4[1] <= 31)
                return true;
            if (v4[0] == 192 && v4[1] == 168)
                return true;
            if (v4[0] == 100 && v4[1] >= 64 && v4[1] <= 127)
                return true;
            if (v4[0] >= 224)
                return true;

            return false;
        }

        private static bool IsRedirect(HttpStatusCode status) =>
            status is HttpStatusCode.Moved or
                HttpStatusCode.Redirect or
                HttpStatusCode.RedirectMethod or
                HttpStatusCode.RedirectKeepVerb or
                HttpStatusCode.PermanentRedirect;

        private static async Task<string?> ReadLimitedStringAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.Content.Headers.ContentLength is > MaxBytes)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            long total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                total += read;
                if (total > MaxBytes)
                    return null;
                memory.Write(buffer, 0, read);
            }

            var charset = response.Content.Headers.ContentType?.CharSet;
            Encoding encoding;
            try
            {
                encoding = string.IsNullOrWhiteSpace(charset)
                    ? Encoding.UTF8
                    : Encoding.GetEncoding(charset);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(memory.ToArray());
        }

        private static WebPageAnalysis Analyze(string html, Uri baseUrl)
        {
            var analysis = new WebPageAnalysis
            {
                Title = Decode(FirstGroup(TitleRegex, html)),
                Description = Decode(MetaContent(html))
            };

            foreach (Match match in HeadingRegex.Matches(html))
            {
                if (analysis.Headings.Count >= MaxListItems)
                    break;
                var text = VisiblePlain(match.Groups[2].Value);
                if (!string.IsNullOrWhiteSpace(text))
                    analysis.Headings.Add("H" + match.Groups[1].Value + " " + text);
            }

            foreach (Match match in LinkRegex.Matches(html))
            {
                if (analysis.Links.Count >= MaxListItems)
                    break;
                var href = ResolveUrl(baseUrl, ReadAttr(match.Groups[1].Value, "href"));
                var text = VisiblePlain(match.Groups[2].Value);
                if (string.IsNullOrWhiteSpace(href))
                    continue;
                analysis.Links.Add(new WebPageLinkItem { Href = href, Text = text });
            }

            foreach (Match match in ImageRegex.Matches(html))
            {
                if (analysis.Images.Count >= MaxListItems)
                    break;
                var src = ResolveUrl(baseUrl, ReadAttr(match.Groups[1].Value, "src"));
                if (string.IsNullOrWhiteSpace(src))
                    continue;
                analysis.Images.Add(new WebPageImageItem
                {
                    Src = src,
                    Alt = Decode(ReadAttr(match.Groups[1].Value, "alt"))
                });
            }

            foreach (Match match in FormRegex.Matches(html))
            {
                if (analysis.Forms.Count >= MaxListItems)
                    break;
                var fields = new List<string>();
                foreach (Match input in InputRegex.Matches(match.Groups[2].Value))
                {
                    if (fields.Count >= 40)
                        break;
                    var attrs = input.Groups[2].Value;
                    var name = ReadAttr(attrs, "name");
                    var type = ReadAttr(attrs, "type");
                    var id = ReadAttr(attrs, "id");
                    var label = string.IsNullOrWhiteSpace(name) ? id : name;
                    if (string.IsNullOrWhiteSpace(label))
                        continue;
                    fields.Add(string.IsNullOrWhiteSpace(type) ? label : label + " (" + type + ")");
                }

                analysis.Forms.Add(new WebPageFormItem
                {
                    Action = ResolveUrl(baseUrl, ReadAttr(match.Groups[1].Value, "action")) ?? "",
                    Method = (ReadAttr(match.Groups[1].Value, "method") ?? "get").ToUpperInvariant(),
                    Fields = fields
                });
            }

            foreach (Match match in TableRegex.Matches(html))
            {
                if (analysis.Tables.Count >= 80)
                    break;
                var rows = RowRegex.Matches(match.Groups[1].Value);
                var table = new WebPageTableItem { RowCount = rows.Count };
                if (rows.Count > 0)
                {
                    table.Headers = CellRegex.Matches(rows[0].Groups[1].Value)
                        .Cast<Match>()
                        .Select(c => VisiblePlain(c.Groups[1].Value))
                        .Where(v => v.Length > 0)
                        .Take(20)
                        .ToList();

                    for (var i = 1; i < rows.Count && table.SampleRows.Count < 8; i++)
                    {
                        var cells = CellRegex.Matches(rows[i].Groups[1].Value)
                            .Cast<Match>()
                            .Select(c => VisiblePlain(c.Groups[1].Value))
                            .Take(20)
                            .ToList();
                        if (cells.Count > 0)
                            table.SampleRows.Add(cells);
                    }
                }

                analysis.Tables.Add(table);
            }

            if (!string.IsNullOrWhiteSpace(analysis.Title))
                analysis.Meta.Add("title: " + analysis.Title);
            if (!string.IsNullOrWhiteSpace(analysis.Description))
                analysis.Meta.Add("description: " + analysis.Description);

            analysis.VisibleText = VisiblePlain(html);
            if (analysis.VisibleText.Length > MaxVisibleChars)
                analysis.VisibleText = analysis.VisibleText[..MaxVisibleChars];

            return analysis;
        }

        private static string MetaContent(string html)
        {
            var meta = MetaDescRegex.Match(html);
            if (!meta.Success)
                meta = MetaOgRegex.Match(html);
            if (!meta.Success)
                return "";
            var content = ContentAttrRegex.Match(meta.Value);
            return content.Success ? content.Groups[1].Value : "";
        }

        private static string FirstGroup(Regex regex, string html)
        {
            var match = regex.Match(html);
            return match.Success ? match.Groups[1].Value : "";
        }

        private static string ReadAttr(string attrs, string name)
        {
            foreach (Match match in AttrRegex.Matches(attrs))
            {
                if (!match.Groups[1].Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = match.Groups[2].Value;
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value[1..^1];
                }
                return value;
            }
            return "";
        }

        private static string? ResolveUrl(Uri baseUrl, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                return null;
            if (Uri.TryCreate(baseUrl, value, out var resolved))
                return resolved.ToString();
            return value;
        }

        private static string VisiblePlain(string html)
        {
            var text = CommentRegex.Replace(html, " ");
            text = ScriptStyleRegex.Replace(text, " ");
            text = Regex.Replace(text, @"<(br|p|div|li|tr|h[1-6])\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = TagRegex.Replace(text, " ");
            text = Decode(text);
            text = Regex.Replace(text, @"[ \t\f\r]+", " ");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            return text.Trim();
        }

        private static string Decode(string value) =>
            string.IsNullOrEmpty(value) ? "" : WebUtility.HtmlDecode(value).Trim();
    }
}
