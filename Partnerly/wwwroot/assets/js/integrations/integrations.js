(function () {
    "use strict";

    var root = document.getElementById("integrationsRoot");
    if (!root) {
        return;
    }

    var tokenInput = root.querySelector('input[name="__RequestVerificationToken"]');
    var toastHost = root.querySelector("[data-int-toast]");
    var toastTimer = null;

    function antiForgery() {
        return tokenInput ? tokenInput.value : "";
    }

    function toast(message, ok) {
        if (!toastHost || !message) {
            return;
        }
        toastHost.textContent = message;
        toastHost.className = "integrations-toast " + (ok ? "is-ok" : "is-error");
        toastHost.hidden = false;
        window.clearTimeout(toastTimer);
        toastTimer = window.setTimeout(function () { toastHost.hidden = true; }, 4200);
    }

    function post(url, data) {
        var body = data instanceof FormData ? data : new FormData();
        if (!(data instanceof FormData) && data) {
            Object.keys(data).forEach(function (key) { body.append(key, data[key]); });
        }
        if (!body.has("__RequestVerificationToken")) {
            body.append("__RequestVerificationToken", antiForgery());
        }
        return fetch(url, {
            method: "POST",
            body: body,
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).then(readJson);
    }

    function readJson(response) {
        return response.json().catch(function () { return {}; }).then(function (json) {
            json.ok = response.ok && json.success !== false;
            return json;
        });
    }

    function actionUrl(name) {
        return "/Integrations/" + name;
    }

    // ---------- dialogs ----------
    function openDialog(name) {
        var overlay = root.querySelector('[data-int-dialog="' + name + '"]');
        if (!overlay) {
            return null;
        }
        overlay.hidden = false;
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-integrations-dialog-open");
        var first = overlay.querySelector("input:not([type=hidden]):not([type=checkbox])");
        if (first) {
            window.setTimeout(function () { first.focus(); }, 40);
        }
        return overlay;
    }

    function closeDialogs() {
        root.querySelectorAll("[data-int-dialog]").forEach(function (overlay) {
            overlay.hidden = true;
            overlay.setAttribute("aria-hidden", "true");
        });
        document.body.classList.remove("is-integrations-dialog-open");
    }

    root.addEventListener("click", function (event) {
        var opener = event.target.closest("[data-int-open]");
        if (opener && !opener.disabled) {
            openDialog(opener.getAttribute("data-int-open"));
            return;
        }
        if (event.target.closest("[data-int-close]") || event.target.classList.contains("integrations-overlay")) {
            closeDialogs();
            return;
        }

        var copy = event.target.closest("[data-int-copy]");
        if (copy && navigator.clipboard) {
            navigator.clipboard.writeText(copy.textContent.trim()).then(function () {
                toast(root.getAttribute("data-copied") || "Copied", true);
            });
            return;
        }

        var test = event.target.closest("[data-int-test]");
        if (test) {
            busy(test, true);
            post(actionUrl("Test"), { id: test.getAttribute("data-int-test") }).then(function (json) {
                busy(test, false);
                toast(json.message, json.ok);
                var dot = test.closest(".integrations-item").querySelector(".integrations-dot");
                if (dot) {
                    dot.className = "integrations-dot " + (json.ok ? "is-ok" : "is-error");
                }
            });
            return;
        }

        var del = event.target.closest("[data-int-delete]");
        if (del) {
            if (!window.confirm(root.getAttribute("data-confirm-delete") || "Delete?")) {
                return;
            }
            busy(del, true);
            post(actionUrl("Delete"), { id: del.getAttribute("data-int-delete") }).then(function (json) {
                busy(del, false);
                if (json.ok) {
                    window.location.reload();
                } else {
                    toast(json.message, false);
                }
            });
            return;
        }

        var chats = event.target.closest("[data-int-chats-open]");
        if (chats) {
            loadChats(chats);
            return;
        }

        var useChat = event.target.closest("[data-use-chat]");
        if (useChat) {
            var input = root.querySelector("#chat-" + useChat.getAttribute("data-connection"));
            if (input) {
                input.value = useChat.getAttribute("data-use-chat");
                closeDialogs();
                input.focus();
            }
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeDialogs();
        }
    });

    function busy(button, on) {
        button.classList.toggle("is-busy", on);
        button.disabled = on;
    }

    // ---------- forms (connect / save) ----------
    root.querySelectorAll("[data-int-form]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            var submit = form.querySelector('[type="submit"]');
            if (submit) {
                busy(submit, true);
            }
            post(form.getAttribute("action"), new FormData(form)).then(function (json) {
                if (submit) {
                    busy(submit, false);
                }
                if (json.redirect) {
                    window.location.href = json.redirect;
                    return;
                }
                if (json.ok) {
                    toast(json.message, true);
                    if (!form.classList.contains("integrations-inline")) {
                        window.setTimeout(function () { window.location.reload(); }, 500);
                    }
                } else {
                    toast(json.message || "Error", false);
                }
            });
        });
    });

    // ---------- Telegram recent chats ----------
    function loadChats(button) {
        var id = button.getAttribute("data-int-chats-open");
        var overlay = openDialog("chats");
        var list = overlay ? overlay.querySelector("[data-int-chats]") : null;
        if (!list) {
            return;
        }
        list.innerHTML = '<li><i class="bx bx-loader-alt bx-spin"></i></li>';
        fetch(actionUrl("TelegramChats") + "?id=" + encodeURIComponent(id), { credentials: "same-origin" })
            .then(readJson)
            .then(function (json) {
                list.innerHTML = "";
                if (!json.ok) {
                    list.appendChild(row(json.message || "Error"));
                    return;
                }
                if (!json.chats || json.chats.length === 0) {
                    list.appendChild(row(root.getAttribute("data-no-chats")));
                    return;
                }
                json.chats.forEach(function (chat) {
                    var item = document.createElement("li");
                    var text = document.createElement("div");
                    var title = document.createElement("strong");
                    title.textContent = (chat.name || chat.username || chat.chatId) + (chat.type && chat.type !== "private" ? " · " + chat.type : "");
                    var meta = document.createElement("small");
                    meta.textContent = "chat id: " + chat.chatId + (chat.text ? " — " + chat.text.slice(0, 60) : "");
                    text.appendChild(title);
                    text.appendChild(meta);
                    var use = document.createElement("button");
                    use.type = "button";
                    use.className = "btn btn-sm btn-outline-primary";
                    use.textContent = root.getAttribute("data-use-chat") || "Use";
                    use.setAttribute("data-use-chat", chat.chatId);
                    use.setAttribute("data-connection", id);
                    item.appendChild(text);
                    item.appendChild(use);
                    list.appendChild(item);
                });
            });
    }

    function row(text) {
        var item = document.createElement("li");
        item.textContent = text;
        return item;
    }
})();
