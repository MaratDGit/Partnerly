(function () {
    "use strict";

    var card = document.querySelector("[data-status-card]");
    if (!card) {
        return;
    }

    var form = document.getElementById("automationBuilderForm");
    var can = {
        edit: form && form.dataset.canEdit === "true",
        publish: form && form.dataset.canPublish === "true",
        run: form && form.dataset.canRun === "true",
        connections: form && form.dataset.canConnections === "true"
    };

    var state = {
        name: "",
        isPublished: card.dataset.published === "true",
        isActive: card.dataset.active === "true",
        hasChanges: card.dataset.changes === "true",
        running: parseInt(card.dataset.running || "0", 10) || 0,
        lastStatus: card.dataset.lastStatus || "",
        lastAt: card.dataset.lastAt || ""
    };

    function label(name) {
        return card.getAttribute("data-l-" + name) || "";
    }

    function ago(iso) {
        var at = Date.parse(iso);
        if (isNaN(at)) {
            return "";
        }
        var sec = Math.max(0, Math.round((Date.now() - at) / 1000));
        if (sec < 60) { return sec + "s"; }
        if (sec < 3600) { return Math.floor(sec / 60) + "m"; }
        if (sec < 86400) { return Math.floor(sec / 3600) + "h"; }
        return Math.floor(sec / 86400) + "d";
    }

    function chip(name, text, cls) {
        var el = card.querySelector('[data-chip="' + name + '"]');
        if (!el) { return; }
        el.textContent = text || "";
        el.hidden = !text;
        el.classList.remove("is-on", "is-off");
        if (cls) { el.classList.add(cls); }
    }

    function btn(act) {
        return form ? form.querySelector('[data-act="' + act + '"]') : null;
    }

    function setEnabled(act, on) {
        var el = btn(act);
        if (!el) { return; }
        el.disabled = !on;
        if (el.tagName === "A") {
            el.classList.toggle("is-disabled", !on);
            el.setAttribute("aria-disabled", on ? "false" : "true");
        }
    }

    // Each dropdown item is enabled only when its action makes sense right now.
    function applyButtons() {
        var b = window.PartnerlyBuilder;
        var dirty = b ? b.isDirty() : false;
        var logicDirty = b ? b.isLogicDirty() : false;
        var stats = b ? b.stats() : { hasTrigger: true, hasAction: true };
        var needsPublish = !state.isPublished || state.hasChanges || logicDirty;

        setEnabled("back", true);
        setEnabled("save", can.edit && dirty);
        setEnabled("publish", can.publish && stats.hasTrigger && stats.hasAction && needsPublish);
        setEnabled("test", can.run && stats.hasTrigger);
        setEnabled("run", can.run && state.isPublished && !state.hasChanges && !logicDirty);
        setEnabled("toggle", can.edit && state.isPublished);
        setEnabled("settings", can.edit);
        setEnabled("connections", can.connections);

        var toggle = btn("toggle");
        if (toggle) {
            var toggleLabel = toggle.querySelector("[data-label]");
            var toggleIcon = toggle.querySelector("i");
            (toggleLabel || toggle).textContent = state.isActive ? toggle.dataset.lPause : toggle.dataset.lStart;
            if (toggleIcon) {
                toggleIcon.className = "bx " + (state.isActive ? "bx-pause-circle" : "bx-play-circle");
            }
        }
        var run = btn("run");
        if (run) {
            run.title = !state.isPublished || state.hasChanges || logicDirty ? (card.dataset.lPublishFirst || "") : "";
        }
    }

    function applySideMenu() {
        var menu = window.PartnerlyAgentsMenu;
        var id = card.dataset.automationId;
        if (!menu || !id) { return; }
        menu.setActive(id, state.isPublished && state.isActive, !!state.hasChanges);
        if (state.name && menu.rename) { menu.rename(id, state.name); }
    }

    function render() {
        var mode = "draft";
        if (state.running > 0) { mode = "running"; }
        else if (!state.isPublished) { mode = "draft"; }
        else if (!state.isActive) { mode = "paused"; }
        else { mode = "live"; }

        card.setAttribute("data-state", mode);
        var title = label(mode);
        if (mode === "running" && state.running > 1) {
            title += " (" + state.running + ")";
        }
        card.querySelector("[data-status-label]").textContent = title;

        chip("published", state.isPublished ? label("published") : label("notpublished"), state.isPublished ? "is-on" : "is-off");
        chip("active", state.isActive ? label("active") : label("inactive"), state.isActive ? "is-on" : "is-off");
        chip("changes", state.isPublished && state.hasChanges ? label("changes") : "");

        var run = card.querySelector("[data-status-run]");
        if (!state.lastStatus) {
            run.textContent = label("noruns");
        } else {
            var key = state.lastStatus === "running" ? "runrunning" : state.lastStatus;
            run.innerHTML = "";
            run.appendChild(document.createTextNode(label("lastrun") + ": "));
            var span = document.createElement("span");
            span.className = "is-" + state.lastStatus;
            span.textContent = label(key) || state.lastStatus;
            run.appendChild(span);
            var when = ago(state.lastAt);
            if (when) {
                run.appendChild(document.createTextNode(" \u00b7 " + when));
            }
        }

        applyButtons();
    }

    var timer = null;
    function refresh() {
        clearTimeout(timer);
        timer = setTimeout(function () {
            fetch(card.dataset.stateUrl, { credentials: "same-origin", headers: { Accept: "application/json" } })
                .then(function (r) { return r.ok ? r.json() : null; })
                .then(function (d) {
                    if (!d) { return; }
                    state.name = d.name || state.name;
                    state.isPublished = !!d.isPublished;
                    state.isActive = !!d.isActive;
                    state.hasChanges = !!d.hasChanges;
                    state.running = d.running || 0;
                    state.lastStatus = d.lastStatus || "";
                    state.lastAt = d.lastAt || "";
                    render();
                    applySideMenu();
                })
                .catch(function () { /* keep the last known state */ });
        }, 250);
    }

    // Saving, publishing and running all store the draft, so the "unsaved" baseline moves.
    window.addEventListener("partnerly:automation-changed", function (event) {
        var url = event && event.detail && event.detail.url ? String(event.detail.url) : "";
        if (/BuilderSave|Publish|\/Run/i.test(url) && window.PartnerlyBuilder) {
            window.PartnerlyBuilder.markSaved();
        }
        applyButtons();
        refresh();
    });
    window.addEventListener("partnerly:builder-changed", applyButtons);

    // Start / Pause from the dropdown.
    var toggleBtn = btn("toggle");
    if (toggleBtn && form) {
        toggleBtn.addEventListener("click", function () {
            var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
            toggleBtn.disabled = true;
            fetch(form.dataset.toggleUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "X-Requested-With": "XMLHttpRequest",
                    "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                },
                body: "isActive=" + (state.isActive ? "false" : "true")
            }).then(function (response) {
                if (!response.ok) { throw new Error(response.statusText || "Error"); }
                return response.json();
            }).then(function (payload) {
                state.isActive = !!payload.isActive;
                render();
                applySideMenu();
                window.dispatchEvent(new CustomEvent("partnerly:automation-changed", { detail: { url: "/ToggleActive" } }));
            }).catch(function (err) {
                if (window.PartnerlyAutomationHub && window.PartnerlyAutomationHub.toast) {
                    window.PartnerlyAutomationHub.toast(err.message || "Error", "error");
                }
                applyButtons();
            });
        });
    }

    if (window.signalR) {
        var id = (card.dataset.automationId || "").toLowerCase();
        var conn = new signalR.HubConnectionBuilder()
            .withUrl("/automationHub")
            .withAutomaticReconnect()
            .build();
        [
            "automation.run.started", "automation.run.completed", "automation.run.failed",
            "automation.run.cancelled", "automation.step.started", "automation.step.completed",
            "automation.step.failed"
        ].forEach(function (name) {
            conn.on(name, function (e) {
                if (e && String(e.automationId || "").toLowerCase() === id) {
                    if (name === "automation.run.started") {
                        state.running = Math.max(1, state.running + 1);
                        state.lastStatus = "running";
                        state.lastAt = new Date().toISOString();
                        render();
                    }
                    refresh();
                }
            });
        });
        conn.onreconnected(refresh);
        conn.start().catch(function () { /* hub unavailable: manual refresh events still work */ });
    }

    render();
    setInterval(render, 30000);
    // Read-only view for the chat's process check.
    window.PartnerlyBuilderStatus = {
        get: function () { return { isPublished: state.isPublished, isActive: state.isActive, hasChanges: state.hasChanges, running: state.running }; }
    };
})();
