using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Partnerly.Integrations.Common;

namespace Partnerly.Integrations.Google.Gmail
{
    // Reading a whole message, replying in the thread, drafts, labels, archive, trash.
    public partial class GmailApi
    {
        // Full message: headers, readable text (plain part, else stripped HTML) and attachment names.
        public async Task<JsonObject> GetMessageAsync(IntegrationConnection connection, string id, CancellationToken cancellationToken)
        {
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailRead, GoogleOAuthService.ScopeGmailModify);
            var url = $"{BaseUrl}/messages/{Uri.EscapeDataString(id)}?format=full";
            var json = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Gmail", cancellationToken);
            var payload = json["payload"] as JsonObject ?? new JsonObject();
            var headers = HeaderMap(payload);
            var attachments = new JsonArray();
            CollectAttachments(payload, attachments);
            return new JsonObject
            {
                ["id"] = id,
                ["threadId"] = json["threadId"]?.ToString(),
                ["from"] = Header(headers, "From"),
                ["to"] = Header(headers, "To"),
                ["subject"] = Header(headers, "Subject"),
                ["date"] = Header(headers, "Date"),
                ["snippet"] = WebUtility.HtmlDecode(json["snippet"]?.ToString() ?? string.Empty),
                ["body"] = ExtractText(payload),
                ["attachmentCount"] = attachments.Count,
                ["attachments"] = attachments,
                ["labels"] = json["labelIds"]?.DeepClone()
            };
        }

        // Replies inside the same conversation (In-Reply-To / References / threadId).
        public async Task<JsonObject> ReplyAsync(
            IntegrationConnection connection,
            string messageId,
            string body,
            bool html,
            CancellationToken cancellationToken)
        {
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailSend);
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailRead, GoogleOAuthService.ScopeGmailModify);
            var url = $"{BaseUrl}/messages/{Uri.EscapeDataString(messageId)}?format=metadata" +
                      "&metadataHeaders=Message-ID&metadataHeaders=References&metadataHeaders=Subject&metadataHeaders=From&metadataHeaders=Reply-To";
            var original = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, url), "Gmail", cancellationToken);
            var headers = HeaderMap(original["payload"] as JsonObject ?? new JsonObject());
            var to = Header(headers, "Reply-To");
            if (string.IsNullOrWhiteSpace(to))
            {
                to = Header(headers, "From");
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                throw new IntegrationException("Gmail: the message has no sender to reply to.", retryable: false);
            }

            var subject = Header(headers, "Subject");
            if (!subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase))
            {
                subject = "Re: " + subject;
            }

            var extra = new List<(string name, string value)>();
            var originalId = Header(headers, "Message-ID");
            if (!string.IsNullOrWhiteSpace(originalId))
            {
                extra.Add(("In-Reply-To", originalId));
                extra.Add(("References", (Header(headers, "References") + " " + originalId).Trim()));
            }

            var raw = new JsonObject
            {
                ["raw"] = Base64Url(BuildMime(to, subject, body, html, extra)),
                ["threadId"] = original["threadId"]?.ToString()
            };
            var sent = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/messages/send")
            {
                Content = IntegrationHttp.Json(raw)
            }, "Gmail", cancellationToken);
            sent["to"] = to;
            return sent;
        }

        public async Task<JsonObject> CreateDraftAsync(
            IntegrationConnection connection,
            string to,
            string subject,
            string body,
            bool html,
            CancellationToken cancellationToken)
        {
            RequireModify(connection);
            var payload = new JsonObject
            {
                ["message"] = new JsonObject { ["raw"] = Base64Url(BuildMime(to, subject, body, html)) }
            };
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/drafts")
            {
                Content = IntegrationHttp.Json(payload)
            }, "Gmail", cancellationToken);
        }

        public async Task<JsonObject> ModifyAsync(
            IntegrationConnection connection,
            string messageId,
            IEnumerable<string> addLabelIds,
            IEnumerable<string> removeLabelIds,
            CancellationToken cancellationToken)
        {
            RequireModify(connection);
            var body = new JsonObject
            {
                ["addLabelIds"] = new JsonArray(addLabelIds.Select(l => (JsonNode?)JsonValue.Create(l)).ToArray()),
                ["removeLabelIds"] = new JsonArray(removeLabelIds.Select(l => (JsonNode?)JsonValue.Create(l)).ToArray())
            };
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages/{Uri.EscapeDataString(messageId)}/modify")
            {
                Content = IntegrationHttp.Json(body)
            }, "Gmail", cancellationToken);
        }

        public async Task<JsonObject> TrashAsync(IntegrationConnection connection, string messageId, CancellationToken cancellationToken)
        {
            RequireModify(connection);
            return await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages/{Uri.EscapeDataString(messageId)}/trash")
            {
                Content = new StringContent(string.Empty)
            }, "Gmail", cancellationToken);
        }

        // Finds a label by name (case-insensitive) and creates it when missing.
        public async Task<string> LabelIdAsync(IntegrationConnection connection, string name, bool createIfMissing, CancellationToken cancellationToken)
        {
            RequireModify(connection);
            var list = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/labels"), "Gmail", cancellationToken);
            var found = (list["labels"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .FirstOrDefault(l => string.Equals(l["name"]?.ToString(), name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                return found["id"]?.ToString() ?? string.Empty;
            }

            if (!createIfMissing)
            {
                throw new IntegrationException($"Gmail: label \"{name}\" was not found.", retryable: false);
            }

            var created = await _google.SendAsync(connection, () => new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/labels")
            {
                Content = IntegrationHttp.Json(new JsonObject { ["name"] = name })
            }, "Gmail", cancellationToken);
            return created["id"]?.ToString() ?? string.Empty;
        }

        private void RequireModify(IntegrationConnection connection) =>
            _google.RequireScope(connection, GoogleOAuthService.ScopeGmailModify);

        private static Dictionary<string, string> HeaderMap(JsonObject payload) =>
            (payload["headers"] as JsonArray ?? new JsonArray())
                .OfType<JsonObject>()
                .GroupBy(h => h["name"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First()["value"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        private static string Header(Dictionary<string, string> headers, string name) =>
            headers.TryGetValue(name, out var value) ? value : string.Empty;

        private static void CollectAttachments(JsonObject part, JsonArray into)
        {
            var file = part["filename"]?.ToString();
            if (!string.IsNullOrEmpty(file))
            {
                into.Add(new JsonObject
                {
                    ["name"] = file,
                    ["mimeType"] = part["mimeType"]?.ToString(),
                    ["size"] = (part["body"] as JsonObject)?["size"]?.DeepClone()
                });
            }

            foreach (var child in (part["parts"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
            {
                CollectAttachments(child, into);
            }
        }

        public static string ExtractText(JsonObject payload)
        {
            string? plain = null;
            string? html = null;

            void Walk(JsonObject part)
            {
                var mime = part["mimeType"]?.ToString() ?? string.Empty;
                var data = (part["body"] as JsonObject)?["data"]?.ToString();
                if (!string.IsNullOrEmpty(data) && string.IsNullOrEmpty(part["filename"]?.ToString()))
                {
                    if (mime == "text/plain" && plain == null)
                    {
                        plain = DecodeBase64Url(data);
                    }
                    else if (mime == "text/html" && html == null)
                    {
                        html = DecodeBase64Url(data);
                    }
                }

                foreach (var child in (part["parts"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                {
                    Walk(child);
                }
            }

            Walk(payload);
            var text = plain ?? StripHtml(html ?? string.Empty);
            return text.Length > 20000 ? text.Substring(0, 20000) : text;
        }

        public static string DecodeBase64Url(string data)
        {
            var padded = data.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        public static string StripHtml(string html)
        {
            var text = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<[^>]+>", " ");
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, @"[ \t]+", " ").Trim();
        }
    }
}
