// Mini chat with the agent (editor page).
// - The AI is not connected yet: replies come from a replaceable "responder"
//   (window.PartnerlyAgentChat.setResponder(fn); fn(text, context) -> string | Promise<string>).
// - "Analyze this process" is a local, automatic check of the editor (no AI): the chat grows
//   and writes the report step by step while the cards it mentions light up on the canvas.
(function () {
    "use strict";

    var root = document.getElementById("agentChat");
    if (!root) {
        return;
    }

    var launcher = root.querySelector("[data-chat-launcher]");
    var win = root.querySelector("[data-chat-window]");
    var list = root.querySelector("[data-chat-list]");
    var chips = root.querySelector("[data-chat-chips]");
    var input = root.querySelector("[data-chat-input]");
    var sendBtn = root.querySelector("[data-chat-send]");
    var clearBtn = root.querySelector("[data-chat-clear]");
    var closeBtn = root.querySelector("[data-chat-close]");
    var pinBtn = root.querySelector("[data-chat-pin]");
    var sizeBtn = root.querySelector("[data-chat-size]");

    var agentId = root.getAttribute("data-agent-id") || "";
    var agentName = root.getAttribute("data-agent-name") || "";
    var storageKey = "partnerly.agentChat." + agentId;
    var MAX_SAVED = 60;
    var MIN_THINK_MS = 900;

    var messages = [];
    var busy = false;
    var pinned = false;
    var hideTimer = null;
    // Bumped by "clear": an answer or report that is still being written is dropped.
    var epoch = 0;

    var text = {};
    try {
        text = JSON.parse((root.querySelector("[data-chat-i18n]") || {}).textContent || "{}");
    } catch (e) {
        text = {};
    }

    function label(name) {
        return root.getAttribute("data-l-" + name) || "";
    }

    function t(key, a, b) {
        var value = text[key] || key;
        return value.replace("{0}", a == null ? "" : a).replace("{1}", b == null ? "" : b);
    }

    // ---- replaceable answer source -------------------------------------
    var responder = function () {
        return label("offline-reply");
    };

    window.PartnerlyAgentChat = {
        setResponder: function (fn) {
            if (typeof fn === "function") { responder = fn; }
        },
        open: function () { openChat(); },
        close: function () { closeChat(); }
    };

    // ---- persistence (per agent, this browser tab only) ----------------
    function load() {
        try {
            var saved = JSON.parse(window.sessionStorage.getItem(storageKey) || "[]");
            return Array.isArray(saved) ? saved : [];
        } catch (e) {
            return [];
        }
    }

    function save() {
        try {
            window.sessionStorage.setItem(storageKey, JSON.stringify(messages.slice(-MAX_SAVED)));
        } catch (e) { /* storage unavailable */ }
    }

    // ---- helpers ---------------------------------------------------------
    function pad(n) { return n < 10 ? "0" + n : String(n); }

    function clock(ms) {
        var d = new Date(ms);
        return pad(d.getHours()) + ":" + pad(d.getMinutes());
    }

    function reduced() {
        return !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    }

    function sleep(ms) {
        return new Promise(function (resolve) { window.setTimeout(resolve, reduced() ? Math.min(ms, 30) : ms); });
    }

    function make(tag, className, content) {
        var node = document.createElement(tag);
        if (className) { node.className = className; }
        if (content != null) { node.textContent = content; }
        return node;
    }

    function icon(name) {
        var i = make("i", "bx " + name);
        i.setAttribute("aria-hidden", "true");
        return i;
    }

    function scrollDown(smooth) {
        window.requestAnimationFrame(function () {
            if (typeof list.scrollTo === "function") {
                list.scrollTo({ top: list.scrollHeight, behavior: smooth ? "smooth" : "auto" });
            } else {
                list.scrollTop = list.scrollHeight;
            }
        });
    }

    // ---- rendering ------------------------------------------------------
    function buildMessage(item) {
        var row = make("div", "agent-chat-msg " + (item.role === "user" ? "is-user" : "is-bot"));
        row.appendChild(make("div", "agent-chat-bubble", item.text));
        row.appendChild(make("span", "agent-chat-time", clock(item.at)));
        return row;
    }

    function addMessage(role, content, animate, skipDom) {
        var item = { role: role, text: content, at: Date.now() };
        messages.push(item);
        save();
        if (skipDom) { return; }
        var row = buildMessage(item);
        if (!animate) { row.classList.add("is-static"); }
        list.appendChild(row);
        scrollDown(true);
    }

    function renderAll() {
        list.innerHTML = "";
        messages.forEach(function (item) {
            var row = buildMessage(item);
            row.classList.add("is-static");
            list.appendChild(row);
        });
        scrollDown(false);
        syncChips();
    }

    function welcome() {
        messages = [{ role: "bot", text: label("welcome").replace("{0}", agentName), at: Date.now() }];
        save();
    }

    function syncChips() {
        // The suggestion buttons stay above the field all the time.
        chips.classList.remove("is-hidden");
    }

    // ---- typing indicator -----------------------------------------------
    var typingRow = null;

    function showTyping() {
        if (typingRow) { return; }
        typingRow = make("div", "agent-chat-msg is-bot is-typing");
        typingRow.innerHTML = '<div class="agent-chat-bubble"><span></span><span></span><span></span></div>';
        list.appendChild(typingRow);
        scrollDown(true);
    }

    function hideTyping() {
        if (typingRow && typingRow.parentNode) {
            typingRow.parentNode.removeChild(typingRow);
        }
        typingRow = null;
    }

    // ---- sending ---------------------------------------------------------
    function setBusy(value) {
        busy = value;
        syncSend();
    }

    function syncSend() {
        sendBtn.disabled = busy || input.value.trim() === "";
    }

    function autosize() {
        // One line to start with; grows with the text up to ~5 lines, then scrolls.
        input.style.height = "34px";
        input.style.height = Math.min(Math.max(input.scrollHeight, 34), 132) + "px";
    }

    function send(content) {
        content = String(content || "").trim();
        if (!content || busy) { return; }
        addMessage("user", content, true);
        syncChips();
        input.value = "";
        autosize();
        setBusy(true);
        var myEpoch = epoch;
        var started = Date.now();
        window.setTimeout(showTyping, 220);
        var answer;
        try {
            answer = Promise.resolve(responder(content, { agentId: agentId, agentName: agentName }));
        } catch (e) {
            answer = Promise.reject(e);
        }
        answer.then(function (reply) {
            return reply;
        }, function () {
            return label("offline-reply");
        }).then(function (reply) {
            var wait = Math.max(0, MIN_THINK_MS - (Date.now() - started));
            window.setTimeout(function () {
                if (myEpoch !== epoch) { return; }
                hideTyping();
                addMessage("bot", String(reply || label("offline-reply")), true);
                setBusy(false);
                input.focus();
            }, wait);
        });
    }

    // ---- keep the window inside the editor (canvas) area -------------------
    var stage = root.closest(".automation-canvas-wrap") || root.parentElement;

    function fit() {
        if (!stage) { return; }
        // room above the launcher, minus the top toolbar; width minus the side margins
        var maxH = Math.max(200, stage.clientHeight - 62 - 14 - 52);
        var maxW = Math.max(240, stage.clientWidth - 28);
        root.style.setProperty("--chat-max-h", maxH + "px");
        root.style.setProperty("--chat-max-w", maxW + "px");
    }

    window.addEventListener("resize", fit);
    if (typeof ResizeObserver === "function" && stage) {
        new ResizeObserver(fit).observe(stage);
    }
    fit();

    // ---- size ------------------------------------------------------------
    function setLarge(on) {
        root.classList.toggle("is-large", on);
        var name = on ? t("shrink") : t("enlarge");
        sizeBtn.title = name;
        sizeBtn.setAttribute("aria-label", name);
        var i = sizeBtn.querySelector("i");
        if (i) { i.className = "bx " + (on ? "bx-collapse-alt" : "bx-expand-alt"); }
        window.setTimeout(function () { scrollDown(false); }, 460);
    }

    sizeBtn.addEventListener("click", function () {
        setLarge(!root.classList.contains("is-large"));
    });

    // ---- pin: keep the chat open when clicking elsewhere ------------------
    function setPinned(on) {
        pinned = on;
        root.classList.toggle("is-pinned", on);
        pinBtn.setAttribute("aria-pressed", on ? "true" : "false");
        var name = on ? t("unpin") : t("pin");
        pinBtn.title = name;
        pinBtn.setAttribute("aria-label", name);
        var i = pinBtn.querySelector("i");
        if (i) { i.className = "bx " + (on ? "bxs-pin" : "bx-pin"); }
    }

    pinBtn.addEventListener("click", function () { setPinned(!pinned); });

    // A click anywhere outside the chat closes it, unless it is pinned.
    document.addEventListener("pointerdown", function (event) {
        if (!isOpen() || pinned || root.contains(event.target)) { return; }
        closeChat();
    });

    // ---- open / close ----------------------------------------------------
    function isOpen() {
        return root.classList.contains("is-open");
    }

    function openChat() {
        if (isOpen()) { return; }
        window.clearTimeout(hideTimer);
        win.hidden = false;
        fit();
        // Reflow so the closed -> open transition runs.
        void win.offsetWidth;
        root.classList.add("is-open");
        launcher.setAttribute("aria-expanded", "true");
        scrollDown(false);
        window.setTimeout(function () { input.focus(); }, 260);
    }

    function closeChat() {
        if (!isOpen()) { return; }
        root.classList.remove("is-open");
        launcher.setAttribute("aria-expanded", "false");
        hideTimer = window.setTimeout(function () { win.hidden = true; }, 340);
    }

    launcher.addEventListener("click", function () {
        if (isOpen()) { closeChat(); } else { openChat(); }
    });
    closeBtn.addEventListener("click", closeChat);

    // Clear starts the conversation over, even while an answer is being "typed".
    clearBtn.addEventListener("click", function () {
        epoch += 1;
        hideTyping();
        clearHits();
        setBusy(false);
        input.value = "";
        autosize();
        setLarge(false);
        welcome();
        renderAll();
        input.focus();
    });

    sendBtn.addEventListener("click", function () {
        send(input.value);
    });

    input.addEventListener("input", function () {
        autosize();
        syncSend();
    });

    input.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            send(input.value);
        } else if (event.key === "Escape") {
            event.stopPropagation();
            closeChat();
            launcher.focus();
        }
    });

    chips.addEventListener("click", function (event) {
        var chip = event.target.closest("[data-chat-suggest]");
        if (!chip) { return; }
        if (chip.hasAttribute("data-chat-analyze")) {
            analyzeProcess(chip.getAttribute("data-chat-suggest"));
        } else {
            send(chip.getAttribute("data-chat-suggest"));
        }
    });

    // =====================================================================
    //  Process check: the bot writes the report and marks the cards
    // =====================================================================
    var hits = [];

    function hitCard(domId, status) {
        var el = document.querySelector("#node-" + domId + " .auto-node");
        if (!el) { return; }
        el.classList.add("agent-scan-hit", "is-" + status);
        hits.push(el);
        window.setTimeout(function () {
            el.classList.remove("agent-scan-hit", "is-" + status);
        }, 3600);
    }

    function clearHits() {
        hits.forEach(function (el) {
            el.classList.remove("agent-scan-hit", "is-ready", "is-setup", "is-off", "is-unlinked");
        });
        hits = [];
    }

    // The bot "types" a line: characters appear a few at a time.
    function typeInto(el, content, myEpoch) {
        return new Promise(function (resolve, reject) {
            if (reduced() || content.length === 0) {
                el.textContent = content;
                resolve();
                return;
            }
            var caret = make("span", "agent-rp-caret");
            el.textContent = "";
            el.appendChild(caret);
            var i = 0;
            var step = Math.max(1, Math.round(content.length / 90));
            (function tick() {
                if (myEpoch !== epoch) { reject(new Error("cancelled")); return; }
                i = Math.min(content.length, i + step);
                el.firstChild === caret
                    ? el.insertBefore(document.createTextNode(content.slice(0, i)), caret)
                    : (el.firstChild.nodeValue = content.slice(0, i));
                scrollDown(false);
                if (i >= content.length) {
                    if (caret.parentNode) { caret.parentNode.removeChild(caret); }
                    resolve();
                } else {
                    window.setTimeout(tick, 16);
                }
            })();
        });
    }

    function countUp(el, to, myEpoch) {
        return new Promise(function (resolve) {
            if (reduced() || to === 0) { el.textContent = String(to); resolve(); return; }
            var n = 0;
            (function tick() {
                if (myEpoch !== epoch) { resolve(); return; }
                n += 1;
                el.textContent = String(n);
                if (n >= to) { resolve(); } else { window.setTimeout(tick, 70); }
            })();
        });
    }

    function guard(myEpoch) {
        if (myEpoch !== epoch) { throw new Error("cancelled"); }
    }

    function appear(parent, node) {
        node.classList.add("agent-rp-in");
        parent.appendChild(node);
        scrollDown(true);
    }

    function readButtons(status) {
        var form = document.getElementById("automationBuilderForm");
        var out = [];
        if (!form) { return out; }
        Array.prototype.forEach.call(form.querySelectorAll(".automation-add-panel [data-act]"), function (button) {
            var name = (button.querySelector("[data-label]") || button).textContent.trim();
            var act = button.getAttribute("data-act");
            var disabled = button.disabled || button.getAttribute("aria-disabled") === "true";
            var reason = "";
            if (disabled) {
                reason = (act === "run" || act === "toggle") && !status.isPublished ? t("reasonPublish") : t("reasonPermission");
            } else if (act === "run" && status.isPublished && status.hasChanges) {
                reason = t("reasonChanges");
            }
            out.push({ name: name, enabled: !disabled, reason: reason });
        });
        return out;
    }

    function classify(card) {
        if (card.disabled) { return "off"; }
        if (card.missing.length) { return "setup"; }
        if (!card.wired) { return "unlinked"; }
        return "ready";
    }

    // "A → B → (yes: C | no: D)" - the way a person would read the flow.
    function portText(port) {
        if (port === "true") { return t("flowYes"); }
        if (port === "false") { return t("flowNo"); }
        if (port === "default") { return t("flowOther"); }
        if (port && port.indexOf("case:") === 0) { return t("flowCase") + " " + (Number(port.slice(5)) + 1); }
        return "";
    }

    function storyLine(card, byId, path) {
        var label = card.name || card.typeLabel;
        if (card.disabled) { label += " (×)"; }
        if (path.indexOf(card.domId) !== -1) { return label + " " + t("flowRepeat"); }
        var next = path.concat([card.domId]);
        var outs = card.to.filter(function (o) { return byId[o.id]; });
        if (!outs.length) { return label; }
        if (outs.length === 1 && !outs[0].port) {
            return label + " → " + storyLine(byId[outs[0].id], byId, next);
        }
        return label + " → (" + outs.map(function (o) {
            var prefix = portText(o.port);
            return (prefix ? prefix + ": " : "") + storyLine(byId[o.id], byId, next);
        }).join("  |  ") + ")";
    }

    function buildStory(cards) {
        var byId = {};
        cards.forEach(function (c) { byId[String(c.domId)] = c; });
        var roots = cards.filter(function (c) { return !c.fromIds.length; });
        return roots.map(function (root) {
            return { chain: root.chain, text: storyLine(root, byId, []) };
        });
    }

    function addRows(host, rows) {
        rows.forEach(function (row) {
            var line = make("div", "agent-rp-detail");
            line.appendChild(make("span", "agent-rp-detail-k", row[0]));
            line.appendChild(make("span", "agent-rp-detail-v", row[1]));
            host.appendChild(line);
        });
    }

    // =====================================================================
    //  Lock + scan: while the check runs nothing in the editor can be used
    // =====================================================================
    var LOCK_EVENTS = ["pointerdown", "pointerup", "mousedown", "mouseup", "click", "dblclick", "auxclick",
        "contextmenu", "touchstart", "touchend", "keydown", "keyup", "wheel", "dragstart"];

    function blockOutsideChat(event) {
        if (root.contains(event.target)) { return; }
        event.stopPropagation();
        event.preventDefault();
    }

    function lockEditor() {
        LOCK_EVENTS.forEach(function (name) {
            window.addEventListener(name, blockOutsideChat, { capture: true, passive: false });
        });
        document.documentElement.classList.add("agent-scan-lock");
        document.documentElement.setAttribute("aria-busy", "true");
        if (document.activeElement && document.activeElement !== document.body && !root.contains(document.activeElement)) {
            document.activeElement.blur();
        }
    }

    function unlockEditor() {
        LOCK_EVENTS.forEach(function (name) {
            window.removeEventListener(name, blockOutsideChat, { capture: true });
        });
        document.documentElement.classList.remove("agent-scan-lock");
        document.documentElement.removeAttribute("aria-busy");
    }

    function moveTo(el, x, y, scale) {
        return "translate(" + x + "px," + y + "px) scale(" + scale + ")";
    }

    function flyTo(el, from, to, ms) {
        if (!el.animate) { el.style.transform = to; return Promise.resolve(); }
        var animation = el.animate([{ transform: from }, { transform: to }], {
            duration: ms, easing: "cubic-bezier(0.22, 1, 0.36, 1)", fill: "forwards"
        });
        // A hidden page does not run animations: never wait longer than the animation itself.
        return Promise.race([
            animation.finished.catch(function () { /* cancelled */ }),
            new Promise(function (resolve) { window.setTimeout(resolve, ms + 400); })
        ]);
    }

    // The agent's icon flies to the middle of the editor and grows, two light lines sweep the
    // screen (one down, one to the right) and wake each card they touch, then the icon goes home.
    async function playScan(cards, myEpoch) {
        var source = launcher.querySelector(".agent-chat-launcher-icon");
        var stageBox = (stage || root).getBoundingClientRect();
        var srcBox = (source || launcher).getBoundingClientRect();
        var items = cards.map(function (card) {
            var el = document.querySelector("#node-" + card.domId + " .auto-node");
            var box = el ? el.getBoundingClientRect() : null;
            return { card: card, kind: classify(card), box: box, done: false };
        });
        var ports = Array.prototype.map.call(document.querySelectorAll("#drawflow .drawflow-node .input, #drawflow .drawflow-node .output"), function (el) {
            return { el: el, box: el.getBoundingClientRect(), done: false };
        });
        function touchPort(port) {
            if (port.done) { return; }
            port.done = true;
            port.el.classList.add("agent-dot-hit");
            window.setTimeout(function () { port.el.classList.remove("agent-dot-hit"); }, 2600);
        }
        function touch(item) {
            if (item.done) { return; }
            item.done = true;
            hitCard(item.card.domId, item.kind);
        }

        if (reduced() || !source) {
            for (var r = 0; r < items.length; r++) { guard(myEpoch); touch(items[r]); await sleep(60); }
            return;
        }

        var size = srcBox.width;
        var fromX = srcBox.left - stageBox.left, fromY = srcBox.top - stageBox.top;
        var toX = stageBox.width / 2 - size / 2;
        var toY = stageBox.height / 2 - size / 2;
        var big = 3.4;

        var layer = make("div", "agent-scan-layer");
        var lineH = make("div", "agent-scan-h");
        var lineV = make("div", "agent-scan-v");
        var fly = make("div", "agent-fly");
        fly.style.width = size + "px";
        fly.style.height = size + "px";
        fly.appendChild(source.cloneNode(true));
        layer.style.left = stageBox.left + "px";
        layer.style.top = stageBox.top + "px";
        layer.style.width = stageBox.width + "px";
        layer.style.height = stageBox.height + "px";
        layer.style.right = "auto";
        layer.style.bottom = "auto";
        var dfBox = (document.getElementById("drawflow") || stage || root).getBoundingClientRect();
        var dotsPos = (dfBox.left - stageBox.left) + "px " + (dfBox.top - stageBox.top) + "px";
        var dotsH = make("div", "agent-scan-dots agent-scan-dots-h");
        var dotsV = make("div", "agent-scan-dots agent-scan-dots-v");
        dotsH.style.backgroundPosition = dotsPos;
        dotsV.style.backgroundPosition = dotsPos;
        layer.appendChild(dotsH);
        layer.appendChild(dotsV);
        layer.appendChild(lineH);
        layer.appendChild(lineV);
        layer.appendChild(fly);
        document.body.appendChild(layer);
        root.classList.add("is-scanning");

        var home = moveTo(fly, fromX, fromY, 1);
        var centre = moveTo(fly, toX, toY, big);
        fly.style.transform = home;
        try {
            // 3. to the middle, growing
            await flyTo(fly, home, centre, 1700);
            guard(myEpoch);
            fly.classList.add("is-pulsing");

            // 4. the lines sweep the whole screen; a card lights up when a line reaches it
            var W = stageBox.width, H = stageBox.height, total = 5600;
            lineH.style.opacity = "1";
            lineV.style.opacity = "1";
            dotsH.style.opacity = "1";
            dotsV.style.opacity = "1";
            await new Promise(function (resolve, reject) {
                var begin = performance.now();
                (function frame(now) {
                    if (myEpoch !== epoch) { reject(new Error("cancelled")); return; }
                    var p = Math.min(1, (now - begin) / total);
                    var eased = p < 0.5 ? 2 * p * p : 1 - Math.pow(-2 * p + 2, 2) / 2;
                    var y = eased * (H + 6) - 3;
                    var x = eased * (W + 6) - 3;
                    lineH.style.transform = "translateY(" + y + "px)";
                    lineV.style.transform = "translateX(" + x + "px)";
                    dotsH.style.setProperty("--p", y + "px");
                    dotsV.style.setProperty("--p", x + "px");
                    ports.forEach(function (port) {
                        if (!port.done && (y + stageBox.top >= port.box.top || x + stageBox.left >= port.box.left)) { touchPort(port); }
                    });
                    items.forEach(function (item) {
                        if (!item.done && item.box && (y + stageBox.top >= item.box.top || x + stageBox.left >= item.box.left)) { touch(item); }
                    });
                    if (p >= 1) { resolve(); } else { window.setTimeout(function () { frame(performance.now()); }, 16); }
                })(begin);
            });
            items.forEach(touch);
            ports.forEach(touchPort);
            lineH.style.opacity = "0";
            lineV.style.opacity = "0";
            dotsH.style.opacity = "0";
            dotsV.style.opacity = "0";
            await sleep(800);
            guard(myEpoch);

            // 5. back to its place, small again
            fly.classList.remove("is-pulsing");
            await flyTo(fly, centre, home, 1500);
        } finally {
            root.classList.remove("is-scanning");
            if (layer.parentNode) { layer.parentNode.removeChild(layer); }
        }
    }

    async function analyzeProcess(prompt) {
        if (busy) { return; }
        var myEpoch = epoch;
        addMessage("user", prompt, true);
        syncChips();
        setBusy(true);
        // 1-2. the chat closes and the editor is locked
        closeChat();
        lockEditor();
        try {
            await sleep(420);
            guard(myEpoch);
            var builder = window.PartnerlyBuilder;
            var data = builder && builder.analyze ? builder.analyze() : { cards: [] };
            var statusApi = window.PartnerlyBuilderStatus;
            var status = statusApi ? statusApi.get() : { isPublished: false, isActive: false, hasChanges: false, running: 0 };

            // 3-5. icon flight and screen scan
            await playScan(data.cards, myEpoch);
            guard(myEpoch);

            // 6. the chat opens and the agent writes the answer
            openChat();
            await sleep(520);
            guard(myEpoch);

            // --- the bot's message: a report that is written block by block
            var row = make("div", "agent-chat-msg is-bot is-report");
            var bubble = make("div", "agent-chat-bubble agent-rp");
            row.appendChild(bubble);
            row.appendChild(make("span", "agent-chat-time", clock(Date.now())));
            list.appendChild(row);
            scrollDown(true);

            // 1. title + the steps it goes through
            var title = make("div", "agent-rp-title");
            title.appendChild(icon("bx-scan"));
            var titleText = make("span");
            title.appendChild(titleText);
            bubble.appendChild(title);
            await typeInto(titleText, t("scanTitle"), myEpoch);

            var steps = make("ul", "agent-rp-steps");
            bubble.appendChild(steps);
            for (var s = 1; s <= 5; s++) {
                guard(myEpoch);
                var li = make("li", "agent-rp-step");
                li.appendChild(make("span", "agent-rp-spin"));
                li.appendChild(make("span", null, t("step" + s)));
                appear(steps, li);
                await sleep(520);
                li.classList.add("is-done");
                li.firstChild.className = "agent-rp-check";
                li.firstChild.appendChild(icon("bx-check"));
            }
            await sleep(240);
            guard(myEpoch);

            // 2. numbers
            var cards = data.cards;
            var counts = {
                all: cards.length,
                on: cards.filter(function (c) { return !c.disabled; }).length,
                off: cards.filter(function (c) { return c.disabled; }).length,
                setup: cards.filter(function (c) { return !c.disabled && c.missing.length; }).length,
                unlinked: cards.filter(function (c) { return !c.disabled && !c.wired; }).length
            };
            var intro = make("p", "agent-rp-intro");
            bubble.appendChild(intro);
            scrollDown(true);
            await typeInto(intro, t("intro", agentName), myEpoch);

            var stats = make("div", "agent-rp-stats");
            var statDefs = [
                ["statCards", counts.all, "all"], ["statOn", counts.on, "ready"], ["statOff", counts.off, "off"],
                ["statSetup", counts.setup, "setup"], ["statUnlinked", counts.unlinked, "unlinked"]
            ];
            var statNumbers = [];
            statDefs.forEach(function (def) {
                var box = make("div", "agent-rp-stat is-" + def[2]);
                var num = make("strong", null, "0");
                box.appendChild(num);
                box.appendChild(make("span", null, t(def[0])));
                appear(stats, box);
                statNumbers.push([num, def[1]]);
            });
            bubble.appendChild(stats);
            scrollDown(true);
            await Promise.all(statNumbers.map(function (pair) { return countUp(pair[0], pair[1], myEpoch); }));
            await sleep(300);
            guard(myEpoch);

            // 2b. overview in plain words + the flow as a sentence
            if (cards.length) {
                var servicesUsed = [];
                cards.forEach(function (c) {
                    var m = /^(telegram|sheets|gmail|outlook|github|drive|whatsapp)\./.exec(c.type || "");
                    if (m && servicesUsed.indexOf(m[1]) === -1) { servicesUsed.push(m[1]); }
                });
                var serviceNames = { telegram: "Telegram", sheets: "Google Sheets", gmail: "Gmail", outlook: "Outlook", github: "GitHub", drive: "Google Drive", whatsapp: "WhatsApp" };
                var overview = [
                    ["bx-bolt-circle", t("factTriggers", cards.filter(function (c) { return c.kind === "trigger"; }).length)],
                    ["bx-cube-alt", t("factActions", cards.filter(function (c) { return c.kind !== "trigger"; }).length)],
                    ["bx-git-branch", t("factChains", data.chains)],
                    ["bx-git-compare", t("factBranches", cards.filter(function (c) { return c.type === "branch" || c.type === "switch"; }).length)]
                ];
                var alone = cards.filter(function (c) { return !c.from.length && !c.to.length; }).length;
                if (alone) { overview.push(["bx-unlink", t("factAlone", alone)]); }
                if (servicesUsed.length) {
                    overview.push(["bx-plug", t("factServices", servicesUsed.map(function (k) { return serviceNames[k]; }).join(", "))]);
                }
                var ovHead = make("div", "agent-rp-section");
                ovHead.appendChild(icon("bx-bar-chart-alt-2"));
                ovHead.appendChild(make("span", null, t("secOverview")));
                appear(bubble, ovHead);
                var ovList = make("ul", "agent-rp-facts");
                bubble.appendChild(ovList);
                for (var o = 0; o < overview.length; o++) {
                    guard(myEpoch);
                    var ovItem = make("li", "agent-rp-fact is-info");
                    ovItem.appendChild(icon(overview[o][0]));
                    ovItem.appendChild(make("span", null, overview[o][1]));
                    appear(ovList, ovItem);
                    await sleep(160);
                }

                var story = buildStory(cards);
                if (story.length) {
                    var flowHead = make("div", "agent-rp-section");
                    flowHead.appendChild(icon("bx-trip"));
                    flowHead.appendChild(make("span", null, t("secFlow")));
                    appear(bubble, flowHead);
                    var flowList = make("div", "agent-rp-story");
                    bubble.appendChild(flowList);
                    for (var fl = 0; fl < story.length; fl++) {
                        guard(myEpoch);
                        var flowItem = make("div", "agent-rp-story-line");
                        flowItem.appendChild(make("strong", null, t("flowChain", story[fl].chain)));
                        flowItem.appendChild(make("span", null, story[fl].text));
                        appear(flowList, flowItem);
                        await sleep(260);
                    }
                }
                await sleep(200);
                guard(myEpoch);
            }

            // 3. every card, and the card lights up on the canvas
            if (cards.length) {
                var head = make("div", "agent-rp-section");
                head.appendChild(icon("bx-layer"));
                head.appendChild(make("span", null, t("secCards")));
                appear(bubble, head);
                var listEl = make("div", "agent-rp-cards");
                bubble.appendChild(listEl);
                // a click on a card of the report selects it on the canvas and opens its settings
                listEl.addEventListener("click", function (event) {
                    var hit = event.target.closest("[data-card-focus]");
                    if (hit && window.PartnerlyBuilder && window.PartnerlyBuilder.focusCard) {
                        window.PartnerlyBuilder.focusCard(hit.getAttribute("data-card-focus"));
                    }
                });
                var delay = cards.length > 14 ? 160 : 320;
                for (var c = 0; c < cards.length; c++) {
                    guard(myEpoch);
                    var card = cards[c];
                    var kind = classify(card);
                    var item = make("div", "agent-rp-card is-" + kind);
                    item.setAttribute("data-card-focus", String(card.domId));
                    var top = make("div", "agent-rp-card-top");
                    top.appendChild(icon(card.kind === "trigger" ? "bx-bolt-circle" : "bx-cube-alt"));
                    var names = make("div", "agent-rp-card-name");
                    names.appendChild(make("strong", null, card.name || card.typeLabel));
                    names.appendChild(make("span", null, (card.kind === "trigger" ? t("kindTrigger") : t("kindAction")) + " · " + card.typeLabel));
                    top.appendChild(names);
                    top.appendChild(make("span", "agent-rp-badge is-" + kind, t({ ready: "stReady", setup: "stSetup", off: "stOff", unlinked: "stUnlinked" }[kind])));
                    item.appendChild(top);
                    var details = make("div", "agent-rp-details");
                    var rows = [];
                    if (card.connection) { rows.push([t("cardAccount"), card.connection]); }
                    card.settings.forEach(function (st) { rows.push([st.label, st.value]); });
                    if (!rows.length && !card.missing.length && !card.disabled) { rows.push(["", t("cardEmpty")]); }
                    if (card.from.length) { rows.push([t("cardFrom"), card.from.join(", ")]); }
                    if (card.to.length) {
                        rows.push([t("cardTo"), card.to.map(function (o) {
                            var p = portText(o.port);
                            return (p ? p + " → " : "") + o.name;
                        }).join(", ")]);
                    } else if (card.kind !== "trigger" || card.wired) {
                        rows.push([t("cardTo"), t("cardNoNext")]);
                    }
                    rows.push(["", t("cardChain", card.chain)]);
                    addRows(details, rows);
                    item.appendChild(details);
                    if (card.missing.length) {

                        var miss = make("div", "agent-rp-card-note");
                        miss.appendChild(icon("bx-error-circle"));
                        miss.appendChild(make("span", null, t("notSet") + ": " + card.missing.join(", ")));
                        item.appendChild(miss);
                    }
                    if (!card.disabled && !card.wired) {
                        var link = make("div", "agent-rp-card-note");
                        link.appendChild(icon("bx-unlink"));
                        link.appendChild(make("span", null, t("notLinked")));
                        item.appendChild(link);
                    }
                    appear(listEl, item);
                    await sleep(delay);
                }
                await sleep(260);
            }
            guard(myEpoch);

            // 4. agent state
            var agentHead = make("div", "agent-rp-section");
            agentHead.appendChild(icon("bx-bot"));
            agentHead.appendChild(make("span", null, t("secAgent")));
            appear(bubble, agentHead);
            var facts = make("ul", "agent-rp-facts");
            bubble.appendChild(facts);
            var factDefs = [
                [status.isPublished ? "bx-check-circle" : "bx-x-circle", status.isPublished ? t("published") : t("notPublished"), status.isPublished],
                [status.isActive ? "bx-play-circle" : "bx-pause-circle", status.isActive ? t("active") : t("paused"), status.isActive],
                [status.hasChanges && status.isPublished ? "bx-edit" : "bx-check", status.hasChanges && status.isPublished ? t("changes") : t("noChanges"), !(status.hasChanges && status.isPublished)]
            ];
            if (status.running > 0) { factDefs.push(["bx-loader-circle", t("running", status.running), true]); }
            for (var f = 0; f < factDefs.length; f++) {
                guard(myEpoch);
                var fact = make("li", "agent-rp-fact " + (factDefs[f][2] ? "is-good" : "is-bad"));
                fact.appendChild(icon(factDefs[f][0]));
                fact.appendChild(make("span", null, factDefs[f][1]));
                appear(facts, fact);
                await sleep(260);
            }

            // 5. buttons
            var buttons = readButtons(status);
            if (buttons.length) {
                var btnHead = make("div", "agent-rp-section");
                btnHead.appendChild(icon("bx-pointer"));
                btnHead.appendChild(make("span", null, t("secButtons")));
                appear(bubble, btnHead);
                var btnList = make("ul", "agent-rp-buttons");
                bubble.appendChild(btnList);
                for (var b = 0; b < buttons.length; b++) {
                    guard(myEpoch);
                    var btn = buttons[b];
                    var line = make("li", "agent-rp-button " + (btn.enabled ? "is-good" : "is-bad"));
                    line.appendChild(icon(btn.enabled ? "bx-check-circle" : "bx-block"));
                    line.appendChild(make("strong", null, btn.name));
                    line.appendChild(make("span", null, "— " + (btn.enabled ? t("btnOn") : t("btnOff")) + (btn.reason ? " (" + btn.reason + ")" : "")));
                    appear(btnList, line);
                    await sleep(210);
                }
            }
            guard(myEpoch);

            // 6. conclusion
            var lines = [];
            var hasTrigger = cards.some(function (c) { return c.kind === "trigger" && !c.disabled; });
            if (!cards.length) {
                lines.push(t("conclEmpty"));
            } else {
                if (!hasTrigger) { lines.push(t("conclNoTrigger")); }
                if (counts.setup) { lines.push(t("conclSetup", counts.setup)); }
                if (counts.unlinked) { lines.push(t("conclUnlinked", counts.unlinked)); }
                if (hasTrigger && !counts.setup && !counts.unlinked) {
                    lines.push(t("conclReady"));
                    if (!status.isPublished) { lines.push(t("conclPublish")); }
                    else if (status.hasChanges) { lines.push(t("conclChanges")); }
                    else if (status.isActive) { lines.push(t("conclLive")); }
                    else { lines.push(t("conclPaused")); }
                }
            }
            var conclusion = make("div", "agent-rp-conclusion");
            conclusion.appendChild(icon("bx-bulb"));
            var concText = make("div", "agent-rp-conclusion-text");
            conclusion.appendChild(concText);
            appear(bubble, conclusion);
            for (var k = 0; k < lines.length; k++) {
                guard(myEpoch);
                var p = make("p");
                concText.appendChild(p);
                await typeInto(p, lines[k], myEpoch);
                await sleep(220);
            }
            scrollDown(true);

            // The report is finished: its background settles darker so the text is easy to read.
            bubble.classList.add("is-done");

            // The saved history keeps one plain line (the live report is not stored).
            addMessage("bot", t("scanSummary", counts.all, counts.setup), false, true);
        } catch (e) {
            if (!e || e.message !== "cancelled") { throw e; }
        } finally {
            // 7. everything works again (the user's permissions decide what is enabled)
            unlockEditor();
            if (myEpoch === epoch) {
                setBusy(false);
            }
        }
    }

    // =====================================================================
    //  A light dot runs around the outline of the agent's icon (chat button)
    //  The outline is read from the real glyph: it is drawn on a hidden canvas
    //  and its outer edge is traced pixel by pixel.
    // =====================================================================
    function traceOutline(mask, w, h) {
        function on(x, y) { return x >= 0 && y >= 0 && x < w && y < h && mask[y * w + x] === 1; }
        var sx = -1, sy = -1;
        for (var y = 0; y < h && sx < 0; y++) {
            for (var x = 0; x < w; x++) {
                if (on(x, y)) { sx = x; sy = y; break; }
            }
        }
        if (sx < 0) { return []; }
        // clockwise on screen, starting east
        var dirs = [[1, 0], [1, 1], [0, 1], [-1, 1], [-1, 0], [-1, -1], [0, -1], [1, -1]];
        var cx = sx, cy = sy, bi = 4; // came from the west (a background pixel)
        var pts = [[cx, cy]];
        for (var guard = 0; guard < w * h * 2; guard++) {
            var moved = false;
            for (var k = 1; k <= 8; k++) {
                var d = (bi + k) % 8;
                var nx = cx + dirs[d][0], ny = cy + dirs[d][1];
                if (on(nx, ny)) {
                    var pd = (bi + k - 1) % 8;
                    var bx = cx + dirs[pd][0], by = cy + dirs[pd][1];
                    cx = nx; cy = ny;
                    for (var q = 0; q < 8; q++) {
                        if (cx + dirs[q][0] === bx && cy + dirs[q][1] === by) { bi = q; break; }
                    }
                    moved = true;
                    break;
                }
            }
            if (!moved) { break; }
            if (cx === sx && cy === sy) { break; }
            pts.push([cx, cy]);
        }
        return pts;
    }

    function resample(pts, count) {
        if (pts.length < 3) { return pts; }
        var lengths = [0];
        for (var i = 1; i <= pts.length; i++) {
            var a = pts[i - 1], b = pts[i % pts.length];
            lengths.push(lengths[i - 1] + Math.hypot(b[0] - a[0], b[1] - a[1]));
        }
        var total = lengths[lengths.length - 1];
        var out = [];
        var seg = 1;
        for (var n = 0; n < count; n++) {
            var at = (n / count) * total;
            while (seg < lengths.length - 1 && lengths[seg] < at) { seg++; }
            var from = pts[(seg - 1) % pts.length], to = pts[seg % pts.length];
            var span = lengths[seg] - lengths[seg - 1] || 1;
            var f = (at - lengths[seg - 1]) / span;
            out.push([from[0] + (to[0] - from[0]) * f, from[1] + (to[1] - from[1]) * f]);
        }
        return out;
    }

    function startOrbit() {
        if (reduced() || !launcher) { return; }
        var iconEl = launcher.querySelector(".agent-chat-launcher-icon");
        if (!iconEl) { return; }
        var glyph = window.getComputedStyle(iconEl, "::before").content || "";
        glyph = glyph.replace(/^["']|["']$/g, "");
        if (!glyph || glyph === "none" || glyph === "normal") { return; }
        var family = window.getComputedStyle(iconEl).fontFamily;

        var SIZE = 160, FONT = 128;
        var probe = document.createElement("canvas");
        probe.width = SIZE; probe.height = SIZE;
        var pctx = probe.getContext("2d");
        pctx.font = FONT + "px " + family;
        pctx.textAlign = "center";
        pctx.textBaseline = "middle";
        pctx.fillStyle = "#000";
        pctx.fillText(glyph, SIZE / 2, SIZE / 2);
        var image = pctx.getImageData(0, 0, SIZE, SIZE).data;
        var mask = new Uint8Array(SIZE * SIZE);
        for (var i = 0; i < mask.length; i++) { mask[i] = image[i * 4 + 3] > 110 ? 1 : 0; }
        var outline = resample(traceOutline(mask, SIZE, SIZE), 260);
        if (outline.length < 20) { return; }

        var canvas = document.createElement("canvas");
        canvas.className = "agent-chat-orbit";
        canvas.setAttribute("aria-hidden", "true");
        launcher.appendChild(canvas);
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        var box = launcher.clientWidth || 50;
        canvas.width = box * dpr;
        canvas.height = box * dpr;
        var ctx = canvas.getContext("2d");

        // glyph pixels -> launcher pixels (the icon is FONT_PX tall, centred in the button)
        var fontPx = parseFloat(window.getComputedStyle(iconEl).fontSize) || 30;
        var k = fontPx / FONT;
        var path = outline.map(function (p) {
            return [(box / 2 + (p[0] - SIZE / 2) * k) * dpr, (box / 2 + (p[1] - SIZE / 2) * k) * dpr];
        });

        var LOOP_MS = 3600, TAIL = 46;
        function frame(now) {
            window.requestAnimationFrame(frame);
            var hidden = document.hidden || root.classList.contains("is-open") || root.classList.contains("is-scanning");
            canvas.style.opacity = hidden ? "0" : "1";
            if (hidden) { return; }
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            var dark = document.documentElement.getAttribute("data-theme") === "dark" || document.documentElement.classList.contains("dark-style");
            var rgb = dark ? "216,204,255" : "99,102,241";
            var head = Math.floor(((now % LOOP_MS) / LOOP_MS) * path.length);
            for (var t = TAIL; t >= 0; t--) {
                var p = path[(head - t + path.length * 2) % path.length];
                var fade = 1 - t / TAIL;
                ctx.beginPath();
                ctx.arc(p[0], p[1], (0.9 + fade * 1.5) * dpr, 0, Math.PI * 2);
                ctx.fillStyle = "rgba(" + rgb + "," + (fade * fade * 0.95).toFixed(3) + ")";
                ctx.shadowColor = "rgba(" + rgb + ",0.9)";
                ctx.shadowBlur = (2 + fade * 8) * dpr;
                ctx.fill();
            }
            ctx.shadowBlur = 0;
        }
        window.requestAnimationFrame(frame);
    }

    if (document.fonts && document.fonts.ready) {
        document.fonts.ready.then(startOrbit);
    } else {
        startOrbit();
    }

    // ---- start -------------------------------------------------------------
    // The (only) suggestion runs the process check instead of asking the (absent) AI.
    var analyzeChip = chips.querySelectorAll("[data-chat-suggest]")[0];
    if (analyzeChip) { analyzeChip.setAttribute("data-chat-analyze", ""); }
    setLarge(false);
    messages = load();
    if (!messages.length) { welcome(); }
    renderAll();
})();
