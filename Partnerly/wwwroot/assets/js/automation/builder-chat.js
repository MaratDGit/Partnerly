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
        var started = messages.some(function (m) { return m.role === "user"; });
        chips.classList.toggle("is-hidden", started);
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
        }, 2600);
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

    async function analyzeProcess(prompt) {
        if (busy) { return; }
        var myEpoch = epoch;
        addMessage("user", prompt, true);
        syncChips();
        setBusy(true);
        await sleep(520);
        try {
            guard(myEpoch);
            var builder = window.PartnerlyBuilder;
            var data = builder && builder.analyze ? builder.analyze() : { cards: [] };
            var statusApi = window.PartnerlyBuilderStatus;
            var status = statusApi ? statusApi.get() : { isPublished: false, isActive: false, hasChanges: false, running: 0 };

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

            // 3. every card, and the card lights up on the canvas
            if (cards.length) {
                var head = make("div", "agent-rp-section");
                head.appendChild(icon("bx-layer"));
                head.appendChild(make("span", null, t("secCards")));
                appear(bubble, head);
                var listEl = make("div", "agent-rp-cards");
                bubble.appendChild(listEl);
                var delay = cards.length > 14 ? 180 : 420;
                for (var c = 0; c < cards.length; c++) {
                    guard(myEpoch);
                    var card = cards[c];
                    var kind = classify(card);
                    var item = make("div", "agent-rp-card is-" + kind);
                    var top = make("div", "agent-rp-card-top");
                    top.appendChild(icon(card.kind === "trigger" ? "bx-bolt-circle" : "bx-cube-alt"));
                    var names = make("div", "agent-rp-card-name");
                    names.appendChild(make("strong", null, card.name || card.typeLabel));
                    names.appendChild(make("span", null, (card.kind === "trigger" ? t("kindTrigger") : t("kindAction")) + " · " + card.typeLabel));
                    top.appendChild(names);
                    top.appendChild(make("span", "agent-rp-badge is-" + kind, t({ ready: "stReady", setup: "stSetup", off: "stOff", unlinked: "stUnlinked" }[kind])));
                    item.appendChild(top);
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
                    hitCard(card.domId, kind);
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
            if (myEpoch === epoch) {
                setBusy(false);
            }
        }
    }

    // ---- start -------------------------------------------------------------
    // The second suggestion runs the process check instead of asking the (absent) AI.
    var analyzeChip = chips.querySelectorAll("[data-chat-suggest]")[1];
    if (analyzeChip) { analyzeChip.setAttribute("data-chat-analyze", ""); }
    setLarge(false);
    messages = load();
    if (!messages.length) { welcome(); }
    renderAll();
})();
