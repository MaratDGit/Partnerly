(function () {
    const form = document.querySelector("[data-inspector-form]");
    const input = document.getElementById("webInspectorUrl");
    const goBtn = document.querySelector("[data-inspector-go]");
    const schemeEl = document.querySelector("[data-url-scheme]");
    const banner = document.querySelector("[data-inspector-banner]");
    const hint = document.querySelector("[data-browser-hint]");
    const pulse = document.querySelector("[data-live-pulse]");
    const extBtn = document.querySelector("[data-enable-extension]");
    const extLabel = document.querySelector("[data-ext-label]");
    const textEl = document.querySelector("[data-visible-text]");
    const htmlEl = document.querySelector("[data-html-source]");
    const detailBody = document.querySelector("[data-detail-body]");
    const detailTitle = document.querySelector("[data-detail-title]");
    const detailTab = document.querySelector("[data-detail-tab]");
    const i18n = window.webInspectorTranslations || {};
    const config = window.webInspectorConfig || {};
    const VOID_TAGS = {
        area: 1, base: 1, br: 1, col: 1, embed: 1, hr: 1, img: 1, input: 1,
        link: 1, meta: 1, param: 1, source: 1, track: 1, wbr: 1
    };
    function cardLabel(card) {
        const el = document.querySelector('[data-card="' + card + '"] span');
        return (el && el.textContent) || card;
    }

    let extReady = false;
    let reqId = 0;
    let lastSnapshot = null;
    let pending = Object.create(null);
    let lastFlashAt = Object.create(null);
    let selectedCard = "title";
    let activeTab = "text";
    let detailCopy = "";

    function kpi(name) {
        return document.querySelector('[data-kpi="' + name + '"]');
    }

    function escapeHtml(value) {
        return String(value ?? "").replace(/[&<>"']/g, function (ch) {
            return ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[ch];
        });
    }

    function dropLoaders() {
        if (goBtn) {
            goBtn.disabled = false;
            goBtn.classList.remove("is-loading");
        }
        if (window.PartnerlyLoader && typeof window.PartnerlyLoader.hide === "function") {
            window.PartnerlyLoader.hide();
        }
        const globalLoader = document.getElementById("globalLoader");
        if (globalLoader) {
            globalLoader.style.display = "none";
            globalLoader.setAttribute("hidden", "");
            globalLoader.setAttribute("aria-hidden", "true");
        }
    }

    function setBanner(text, isError) {
        if (!banner) {
            return;
        }
        banner.hidden = !text;
        banner.textContent = text || "";
        banner.classList.toggle("is-error", !!isError);
        banner.classList.toggle("is-ok", !!text && !isError);
    }

    function setExtensionState(on) {
        extReady = !!on;
        if (extBtn) {
            extBtn.classList.toggle("is-on", extReady);
        }
        if (extLabel) {
            extLabel.textContent = extReady
                ? (i18n.extensionOn || "On")
                : (i18n.enableExtension || "Enable");
        }
        if (extReady) {
            setBanner("");
        }
    }

    function normalizeClientUrl(raw) {
        const trimmed = String(raw || "").trim();
        if (!trimmed) {
            return "";
        }
        const withScheme = trimmed.indexOf("://") === -1 ? "https://" + trimmed : trimmed;
        let parsed;
        try {
            parsed = new URL(withScheme);
        } catch (e) {
            return "";
        }
        if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
            return "";
        }
        if (parsed.username || parsed.password) {
            return "";
        }
        return parsed.href;
    }

    function screenLayout() {
        const left = window.screen.availLeft || 0;
        const top = window.screen.availTop || 0;
        const width = window.screen.availWidth || 1280;
        const height = window.screen.availHeight || 800;
        const siteWidth = Math.max(560, Math.floor(width * 0.52));
        return {
            site: { left: left, top: top, width: siteWidth, height: height, focused: true },
            partnerly: { left: left + siteWidth, top: top, width: width - siteWidth, height: height, focused: false }
        };
    }

    function sendToExtension(payload) {
        return new Promise(function (resolve) {
            const id = ++reqId;
            pending[id] = resolve;
            window.postMessage({
                source: "partnerly-inspector-page",
                id: id,
                payload: payload
            }, "*");
            setTimeout(function () {
                if (pending[id]) {
                    delete pending[id];
                    resolve({ ok: false, timeout: true });
                }
            }, 8000);
        });
    }

    function flash(name) {
        const card = document.querySelector('[data-flash="' + name + '"]');
        if (!card) {
            return;
        }
        const now = Date.now();
        if (lastFlashAt[name] && now - lastFlashAt[name] < 80) {
            return;
        }
        lastFlashAt[name] = now;
        card.classList.remove("is-flash");
        void card.offsetWidth;
        card.classList.add("is-flash");
    }

    function setKpi(name, value) {
        const el = kpi(name);
        if (!el) {
            return;
        }
        const next = value == null || value === "" ? "—" : String(value);
        if (el.textContent !== next) {
            el.textContent = next;
            flash(name);
        }
    }

    function setTab(name) {
        activeTab = name;
        document.querySelectorAll("[data-stream-tab]").forEach(function (btn) {
            btn.classList.toggle("is-active", btn.getAttribute("data-stream-tab") === name);
        });
        document.querySelectorAll("[data-stream-panel]").forEach(function (panel) {
            const on = panel.getAttribute("data-stream-panel") === name;
            panel.classList.toggle("is-active", on);
            panel.hidden = !on;
        });
    }

    function indentHtml(raw) {
        const src = String(raw || "").slice(0, 200000);
        const parts = src.split(/(<[^>]+>)/);
        let depth = 0;
        const lines = [];
        for (let i = 0; i < parts.length; i++) {
            const part = parts[i];
            if (!part) {
                continue;
            }
            if (part.charAt(0) !== "<") {
                const text = part.replace(/\s+/g, " ").trim();
                if (text) {
                    lines.push("  ".repeat(depth) + text);
                }
                continue;
            }
            const nameMatch = part.match(/^<\/?([a-zA-Z0-9:-]+)/);
            const name = nameMatch ? nameMatch[1].toLowerCase() : "";
            const isClose = part.indexOf("</") === 0;
            const isComment = part.indexOf("<!--") === 0 || part.indexOf("<!") === 0;
            const isVoid = VOID_TAGS[name] || /\/\s*>$/.test(part) || isComment;
            if (isClose) {
                depth = Math.max(0, depth - 1);
            }
            lines.push("  ".repeat(depth) + part);
            if (!isClose && !isVoid) {
                depth += 1;
            }
        }
        return lines.join("\n");
    }

    function paintHtml(src) {
        return escapeHtml(src).replace(/(&lt;\/?)([a-zA-Z0-9:-]+)([\s\S]*?)(\/?&gt;)/g, function (_, open, name, rest, close) {
            const attrs = rest.replace(/([a-zA-Z_:][\w:.-]*)(\s*=\s*)(&quot;[\s\S]*?&quot;|&#39;[\s\S]*?&#39;|[^\s&]+)?/g, function (__unused, key, eq, val) {
                return '<span class="h-attr">' + key + "</span>" + eq + (val ? '<span class="h-val">' + val + "</span>" : "");
            });
            return '<span class="h-tag">' + open + '</span><span class="h-name">' + name + "</span>" + attrs + '<span class="h-tag">' + close + "</span>";
        });
    }

    function renderText(text) {
        if (!textEl) {
            return;
        }
        const blocks = String(text || "").split(/\n+/).map(function (line) {
            return line.trim();
        }).filter(Boolean).slice(0, 400);
        if (!blocks.length) {
            textEl.innerHTML = '<p class="web-inspector-none">' + escapeHtml(i18n.none || "") + "</p>";
            return;
        }
        textEl.innerHTML = blocks.map(function (line) {
            const heading = line.length < 80 && line === line.toUpperCase() && /[A-ZА-Я]/.test(line);
            const tag = heading ? "h6" : "p";
            return "<" + tag + ">" + escapeHtml(line) + "</" + tag + ">";
        }).join("");
    }

    function renderHtml(html) {
        if (!htmlEl) {
            return;
        }
        const pretty = indentHtml(html || "");
        htmlEl.innerHTML = pretty ? paintHtml(pretty) : escapeHtml(i18n.none || "");
    }

    function emptyDetail() {
        return '<p class="web-inspector-none">' + escapeHtml(i18n.none || "") + "</p>";
    }

    function listHtml(items, renderItem) {
        if (!items || !items.length) {
            return emptyDetail();
        }
        return "<ol class=\"web-inspector-list\">" + items.map(renderItem).join("") + "</ol>";
    }

    function frameLine(item) {
        if (!item || !item.frame) {
            return "";
        }
        return "<span>" + escapeHtml(item.frame) + "</span>";
    }

    function renderDetail(card) {
        const data = lastSnapshot || {};
        const details = data.details || {};
        const counts = data.counts || {};
        const label = cardLabel(card);
        if (detailTitle) {
            detailTitle.textContent = label;
        }
        if (detailTab) {
            detailTab.textContent = label;
        }
        document.querySelectorAll("[data-card]").forEach(function (el) {
            el.classList.toggle("is-selected", el.getAttribute("data-card") === card);
        });

        let html = emptyDetail();
        let copy = "";

        if (card === "title") {
            html = '<dl class="web-inspector-kv">' +
                row("URL", data.url) +
                row("Title", data.title) +
                row("State", data.readyState) +
                "</dl>";
            copy = (data.title || "") + "\n" + (data.url || "");
        } else if (card === "ready") {
            html = '<dl class="web-inspector-kv">' + row("readyState", data.readyState) + "</dl>";
            copy = data.readyState || "";
        } else if (card === "mutations" || card === "reason" || card === "updated") {
            const events = data.events || [];
            html = listHtml(events.slice().reverse(), function (item) {
                const time = item.at ? new Date(item.at).toLocaleTimeString() : "";
                return "<li><strong>" + escapeHtml(item.reason || "") + "</strong><span>" + escapeHtml(time) + "</span></li>";
            });
            copy = events.map(function (item) { return (item.reason || "") + " " + (item.at || ""); }).join("\n");
        } else if (card === "nodes") {
            html = listHtml(details.tags, function (item) {
                return "<li><strong>" + escapeHtml(item.name) + "</strong><span>" + escapeHtml(item.count) + "</span></li>";
            });
            copy = (details.tags || []).map(function (item) { return item.name + "\t" + item.count; }).join("\n");
        } else if (card === "links") {
            html = listHtml(details.links, function (item) {
                return "<li><button type=\"button\" class=\"web-inspector-link\" data-open-url=\"" +
                    escapeHtml(item.href || "") + "\">" + escapeHtml(item.text || item.href || "") +
                    "</button><span>" + escapeHtml(item.href || "") + "</span>" + frameLine(item) + "</li>";
            });
            copy = (details.links || []).map(function (item) { return (item.text || "") + "\t" + (item.href || ""); }).join("\n");
        } else if (card === "images") {
            html = listHtml(details.images, function (item) {
                return "<li><code>" + escapeHtml(item.src || "") + "</code><span>" + escapeHtml(item.alt || "") + "</span>" + frameLine(item) + "</li>";
            });
            copy = (details.images || []).map(function (item) { return item.src || ""; }).join("\n");
        } else if (card === "forms") {
            html = listHtml(details.forms, function (item) {
                return "<li><strong>" + escapeHtml(item.method || "") + "</strong> " +
                    escapeHtml(item.action || "") +
                    "<span>" + escapeHtml((item.fields || []).join(", ")) + "</span>" + frameLine(item) + "</li>";
            });
            copy = (details.forms || []).map(function (item) { return (item.method || "") + " " + (item.action || ""); }).join("\n");
        } else if (card === "inputs") {
            html = listHtml(details.inputs, function (item) {
                return "<li><strong>" + escapeHtml(item.type || item.tag) + "</strong> " +
                    escapeHtml(item.name || item.id || "") +
                    "<span>" + escapeHtml(item.placeholder || item.value || "") + "</span>" + frameLine(item) + "</li>";
            });
            copy = (details.inputs || []).map(function (item) { return (item.type || "") + " " + (item.name || ""); }).join("\n");
        } else if (card === "buttons") {
            html = listHtml(details.buttons, function (item) {
                return "<li><strong>" + escapeHtml(item.type || "") + "</strong> " +
                    escapeHtml(item.text || "") +
                    (item.disabled ? "<span>disabled</span>" : "") +
                    frameLine(item) + "</li>";
            });
            copy = (details.buttons || []).map(function (item) { return item.text || ""; }).join("\n");
        } else if (card === "frames") {
            const frames = details.iframes && details.iframes.length
                ? details.iframes
                : (data.frames || []);
            html = listHtml(frames, function (item) {
                const url = item.innerUrl || item.url || item.src || "";
                const state = item.accessible === false ? "blocked" : (item.isTop ? "top" : "iframe");
                return "<li><strong>" + escapeHtml(state) + "</strong> " +
                    escapeHtml(item.name || "") +
                    "<span>" + escapeHtml(url) + "</span></li>";
            });
            copy = frames.map(function (item) {
                return (item.src || item.url || "") + "\t" + (item.innerUrl || "");
            }).join("\n");
        } else if (card === "headings") {
            html = listHtml(details.headings, function (item) {
                return "<li><strong>" + escapeHtml(item.level || "") + "</strong> " +
                    escapeHtml(item.text || "") + frameLine(item) + "</li>";
            });
            copy = (details.headings || []).map(function (item) { return (item.level || "") + " " + (item.text || ""); }).join("\n");
        }

        if (card === "nodes" && counts.nodes) {
            html = '<p class="web-inspector-detail-sum">' + escapeHtml(counts.nodes) + "</p>" + html;
        }

        if (detailBody) {
            detailBody.innerHTML = html;
        }
        detailCopy = copy;
    }

    function row(label, value) {
        return "<div><dt>" + escapeHtml(label) + "</dt><dd>" + escapeHtml(value || "—") + "</dd></div>";
    }

    function applySnapshot(data) {
        if (!data) {
            return;
        }
        lastSnapshot = data;
        const counts = data.counts || {};
        if (input && data.url) {
            input.value = data.url;
        }
        if (schemeEl && data.url) {
            schemeEl.textContent = (String(data.url).split(":")[0] || "https").replace("/", "");
        }
        setKpi("live", i18n.live || "LIVE");
        setKpi("url", data.url || "");
        setKpi("title", data.title || "—");
        setKpi("mutations", counts.mutations != null ? counts.mutations : data.mutations);
        setKpi("ready", data.readyState || "—");
        setKpi("nodes", counts.nodes || 0);
        setKpi("frames", counts.frames || (data.frames && data.frames.length) || 0);
        setKpi("links", counts.links || 0);
        setKpi("images", counts.images || 0);
        setKpi("forms", counts.forms || 0);
        setKpi("inputs", counts.inputs || 0);
        setKpi("buttons", counts.buttons || 0);
        setKpi("headings", counts.headings || 0);
        setKpi("reason", data.reason || "—");
        setKpi("updated", new Date(data.at || Date.now()).toLocaleTimeString());
        renderText(data.text || "");
        renderHtml(data.html || "");
        if (selectedCard) {
            renderDetail(selectedCard);
        }
        if (pulse) {
            pulse.classList.add("is-on");
        }
        if (hint) {
            hint.hidden = true;
        }
        setBanner("");
        dropLoaders();
    }

    function markDetached() {
        setKpi("live", i18n.detached || "—");
        if (pulse) {
            pulse.classList.remove("is-on");
        }
        if (hint) {
            hint.hidden = false;
            hint.textContent = i18n.detached || "";
        }
    }

    function copyValue(kind, button) {
        let value = "";
        if (kind === "html") {
            value = lastSnapshot && lastSnapshot.html || "";
        } else if (kind === "detail") {
            value = detailCopy;
        } else {
            value = lastSnapshot && lastSnapshot.text || "";
        }
        if (!value) {
            return;
        }
        navigator.clipboard.writeText(value).then(function () {
            const original = button.textContent;
            button.textContent = i18n.copied || "Copied";
            setTimeout(function () {
                button.textContent = original;
            }, 1100);
        });
    }

    async function openExtensionPage() {
        const ping = await sendToExtension({ type: "PING" });
        if (ping && ping.ready) {
            setExtensionState(true);
            await sendToExtension({ type: "OPEN_EXTENSIONS" });
            return;
        }
        const folder = config.extensionFolder || "tools/partnerly-web-inspector-extension";
        try {
            await navigator.clipboard.writeText(folder);
        } catch (e) {
            /* ignore */
        }
        setBanner((i18n.extensionCopied || "") + "  " + folder, false);
        const opened = window.open("chrome://extensions", "_blank");
        if (!opened) {
            setBanner(i18n.needExtension || "", true);
        }
    }

    window.addEventListener("message", function (event) {
        if (event.source !== window || !event.data || event.data.source !== "partnerly-inspector-ext") {
            return;
        }
        if (event.data.id && pending[event.data.id]) {
            pending[event.data.id](event.data.response || {});
            delete pending[event.data.id];
        }
        const type = event.data.type || (event.data.payload && event.data.payload.type);
        const payload = event.data.payload || event.data;
        if (type === "READY") {
            setExtensionState(true);
            return;
        }
        if (type === "SNAPSHOT") {
            applySnapshot(payload);
            return;
        }
        if (type === "NAV") {
            if (payload.url) {
                setKpi("url", payload.url);
                if (input) {
                    input.value = payload.url;
                }
            }
            if (payload.title) {
                setKpi("title", payload.title);
            }
            if (payload.status) {
                setKpi("ready", payload.status);
            }
            flash("reason");
            setKpi("reason", payload.status || "nav");
            return;
        }
        if (type === "DETACHED") {
            markDetached();
        }
    });

    async function openSite(rawUrl) {
        const url = normalizeClientUrl(rawUrl);
        if (!url) {
            dropLoaders();
            return;
        }
        if (input) {
            input.value = url;
        }
        if (goBtn) {
            goBtn.disabled = true;
            goBtn.classList.add("is-loading");
        }
        if (!extReady) {
            dropLoaders();
            setBanner(i18n.needExtension || "", true);
            return;
        }
        const result = await sendToExtension({
            type: "OPEN",
            url: url,
            layout: screenLayout()
        });
        dropLoaders();
        if (!result || !result.ok) {
            setBanner(i18n.needExtension || "", true);
            return;
        }
        setKpi("live", i18n.live || "LIVE");
        setKpi("url", url);
        if (hint) {
            hint.hidden = true;
        }
        if (pulse) {
            pulse.classList.add("is-on");
        }
    }

    if (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            openSite(input && input.value);
        });
    }

    if (extBtn) {
        extBtn.addEventListener("click", function () {
            openExtensionPage();
        });
    }

    const focusBtn = document.querySelector("[data-focus-live]");
    if (focusBtn) {
        focusBtn.addEventListener("click", function () {
            sendToExtension({ type: "FOCUS" });
        });
    }

    document.querySelectorAll("[data-stream-tab]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            setTab(btn.getAttribute("data-stream-tab"));
        });
    });

    document.querySelectorAll("[data-card]").forEach(function (card) {
        card.addEventListener("click", function () {
            selectedCard = card.getAttribute("data-card");
            renderDetail(selectedCard);
            setTab("detail");
        });
    });

    window.addEventListener("resize", function () {
        if (!extReady) {
            return;
        }
        sendToExtension({ type: "LAYOUT", layout: screenLayout() });
    });

    document.addEventListener("click", function (event) {
        const copy = event.target.closest("[data-copy]");
        if (copy) {
            copyValue(copy.getAttribute("data-copy"), copy);
            return;
        }
        const open = event.target.closest("[data-open-url]");
        if (open) {
            const url = open.getAttribute("data-open-url");
            if (url) {
                sendToExtension({ type: "NAVIGATE", url: url });
            }
        }
    });

    function pingExtension() {
        sendToExtension({ type: "PING" }).then(function (result) {
            if (result && result.ready) {
                setExtensionState(true);
            }
        });
    }

    pingExtension();
    setTimeout(pingExtension, 600);
    setTimeout(function () {
        if (!extReady) {
            setBanner(i18n.needExtension || "", true);
        }
    }, 1200);
})();
