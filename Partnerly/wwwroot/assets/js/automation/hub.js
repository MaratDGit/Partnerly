(function () {
    "use strict";

    var root = document.getElementById("automationHubRoot");
    var overlay = document.getElementById("automationHub");
    var nested = document.getElementById("automationHubNested");
    if (!overlay || !nested) {
        return;
    }

    var mainCard = overlay.querySelector("[data-hub-card]");
    var nestedCard = nested.querySelector("[data-hub-card]");
    var mainBody = overlay.querySelector("[data-hub-body]");
    var nestedBody = nested.querySelector("[data-hub-body]");
    var rowsHost = root ? root.querySelector("[data-hub-rows]") : null;
    var deleteTemplate = document.getElementById("automationDeleteTemplate");
    var loading = false;

    function attr(name, fallback) {
        return (root && root.getAttribute(name)) || fallback || "";
    }

    function can(name) {
        return attr(name) === "true";
    }

    function tokenFrom(scope) {
        var input = (scope || document).querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function parseJson(response) {
        return response.text().then(function (text) {
            if (!text) {
                return {};
            }
            try {
                return JSON.parse(text);
            } catch (err) {
                return { message: text };
            }
        });
    }

    function toast(message, type) {
        if (!message) {
            return;
        }
        var container = document.querySelector(".toast-container");
        if (!container) {
            container = document.createElement("div");
            container.className = "toast-container position-fixed bottom-0 end-0 p-3";
            document.body.appendChild(container);
        }
        var el = document.createElement("div");
        var failed = type === "error";
        // glass card in the style of the Agents pages (see .automation-toast in automation-builder.css)
        el.className = "toast automation-toast border-0 " + (failed ? "is-error" : "is-ok");
        el.setAttribute("role", "alert");
        el.innerHTML = '<div class="automation-toast-icon"><i class="bx ' + (failed ? "bx-error-circle" : "bx-check-circle") + '" aria-hidden="true"></i></div>' +
            '<div class="toast-body"></div>' +
            '<button type="button" class="automation-toast-close" data-bs-dismiss="toast" aria-label="Close"><i class="bx bx-x" aria-hidden="true"></i></button>';
        el.querySelector(".toast-body").textContent = message;
        container.appendChild(el);
        if (window.bootstrap && bootstrap.Toast) {
            new bootstrap.Toast(el, { delay: 3000 }).show();
            el.addEventListener("hidden.bs.toast", function () { el.remove(); });
        } else {
            window.setTimeout(function () { el.remove(); }, 3000);
        }
    }

    function setOpen(el, open) {
        if (!el) {
            return;
        }
        el.hidden = !open;
        el.classList.toggle("is-open", open);
        el.setAttribute("aria-hidden", open ? "false" : "true");
        document.body.classList.toggle("is-automation-create-open", isAnyOpen());
    }

    function isOpen(el) {
        return el && el.classList.contains("is-open");
    }

    function isAnyOpen() {
        var createEl = document.getElementById("automationCreate");
        return isOpen(overlay) || isOpen(nested) || (createEl && createEl.classList.contains("is-open"));
    }

    function slot(useNested) {
        return useNested
            ? { overlay: nested, card: nestedCard, body: nestedBody }
            : { overlay: overlay, card: mainCard, body: mainBody };
    }

    function closeSlot(useNested) {
        var target = slot(useNested);
        if (!useNested && target.body && target.body.querySelector("#automationBuilderForm")
            && typeof window.destroyAutomationBuilder === "function") {
            window.destroyAutomationBuilder();
        }
        if (target.body) {
            target.body.innerHTML = "";
        }
        if (target.card) {
            target.card.classList.remove("is-wide", "is-confirm");
        }
        setOpen(target.overlay, false);
        if (!useNested) {
            closeSlot(true);
        }
    }

    function closeTop() {
        if (isOpen(nested)) {
            closeSlot(true);
            return;
        }
        if (isOpen(overlay)) {
            closeSlot(false);
        }
    }

    function showHtml(html, options) {
        options = options || {};
        var target = slot(!!options.nested);
        if (!options.nested && target.body && target.body.querySelector("#automationBuilderForm")
            && typeof window.destroyAutomationBuilder === "function") {
            window.destroyAutomationBuilder();
        }
        if (target.card) {
            target.card.classList.toggle("is-wide", options.size === "wide");
            target.card.classList.toggle("is-confirm", options.size === "confirm");
        }
        target.body.innerHTML = html;
        setOpen(target.overlay, true);
        bootMonitor(target.body);
        bootRunLive(target.body);
        if (options.size === "wide") {
            window.requestAnimationFrame(function () {
                bootBuilder(target.body);
            });
        }
    }

    // Details of a run that is still going: re-read the panel every 2 s until it ends,
    // so a freshly started run does not stay on "no steps".
    function bootRunLive(scope) {
        var panel = scope.querySelector("[data-run-live]");
        if (!panel) {
            return;
        }
        var id = panel.getAttribute("data-run-id");
        window.setTimeout(function () {
            if (!document.body.contains(panel)) {
                return;
            }
            fetch("/Automation/RunDetailsPanel/" + id, {
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            }).then(function (response) {
                return response.ok ? response.text() : null;
            }).then(function (html) {
                if (!html || !document.body.contains(panel)) {
                    return;
                }
                scope.innerHTML = html;
                bootRunLive(scope);
            }).catch(function () { /* the popup may be closed */ });
        }, 2000);
    }

    // Live run monitor in the History popup: polls /Automation/RunsData/{id} every 3 s and
    // counts down locally each second. Stops by itself once the popup is gone.
    function bootMonitor(scope) {
        var root = scope.querySelector("[data-run-monitor]");
        if (!root) {
            return;
        }
        var url = root.getAttribute("data-url");
        var rows = root.querySelector("[data-monitor-rows]");
        var els = {
            next: root.querySelector("[data-monitor-next]"),
            nextAt: root.querySelector("[data-monitor-next-at]"),
            active: root.querySelector("[data-monitor-active]"),
            limit: root.querySelector("[data-monitor-limit]"),
            zone: root.querySelector("[data-monitor-zone]"),
            dot: root.querySelector("[data-monitor-dot]"),
            live: root.querySelector("[data-monitor-live]")
        };
        var data = null;
        var offset = 0;
        var failed = false;
        var tick = 0;
        var timer = null;

        function label(name) {
            return root.getAttribute("data-l-" + name) || "";
        }

        function pad(value) {
            return value < 10 ? "0" + value : String(value);
        }

        function span(ms) {
            var total = Math.max(0, Math.floor(ms / 1000));
            var days = Math.floor(total / 86400);
            total -= days * 86400;
            var hours = Math.floor(total / 3600);
            total -= hours * 3600;
            var text = pad(hours) + ":" + pad(Math.floor(total / 60)) + ":" + pad(total % 60);
            return days > 0 ? days + "d " + text : text;
        }

        function nowMs() {
            return Date.now() + offset;
        }

        function cell(text, className) {
            var td = document.createElement("td");
            if (className) {
                td.className = className;
            }
            td.textContent = text;
            return td;
        }

        var expanded = {};

        // Statuses are stored as English codes; show them in the language of the site.
        function statusText(code) {
            return label("s-" + code) || code;
        }

        function chip(kind, icon, count, title) {
            var node = document.createElement("span");
            node.className = "run-chip is-" + kind + (count === 0 ? " is-zero" : "");
            node.title = title;
            var i = document.createElement("i");
            i.className = "bx " + icon;
            i.setAttribute("aria-hidden", "true");
            node.appendChild(i);
            node.appendChild(document.createTextNode(" " + count));
            return node;
        }

        // Counts per process: how many cards finished well, failed, were skipped or are still going.
        function chips(run) {
            var box = document.createElement("div");
            box.className = "run-chips";
            box.appendChild(chip("ok", "bx-check-circle", run.succeeded, label("done")));
            box.appendChild(chip("bad", "bx-error-circle", run.failed, label("problems")));
            if (run.skipped > 0) { box.appendChild(chip("skip", "bx-skip-next-circle", run.skipped, label("skipped"))); }
            if (run.pending > 0) { box.appendChild(chip("wait", "bx-time-five", run.pending, label("pending"))); }
            if (run.totalSteps > 0) {
                var total = document.createElement("span");
                total.className = "run-monitor-sub run-chips-total";
                total.textContent = " / " + run.totalSteps;
                box.appendChild(total);
            }
            return box;
        }

        // Every card of the process with its own status and error, opened by clicking the row.
        function detailsRow(run) {
            var tr = document.createElement("tr");
            tr.className = "run-details";
            var td = document.createElement("td");
            td.colSpan = 5;
            if (!run.steps || !run.steps.length) {
                td.textContent = label("nosteps");
            } else {
                run.steps.forEach(function (step) {
                    var line = document.createElement("div");
                    line.className = "run-step is-" + step.status;
                    var name = document.createElement("strong");
                    name.textContent = step.name;
                    var meta = document.createElement("span");
                    meta.className = "run-monitor-sub";
                    meta.textContent = " " + step.type + " · " + statusText(step.status);
                    line.appendChild(name);
                    line.appendChild(meta);
                    if (step.error) {
                        var error = document.createElement("div");
                        error.className = "run-step-error";
                        error.textContent = step.error;
                        line.appendChild(error);
                    }
                    td.appendChild(line);
                });
            }
            tr.appendChild(td);
            return tr;
        }

        function buildRows() {
            rows.innerHTML = "";
            if (!data.runs.length) {
                var empty = document.createElement("tr");
                var td = cell(label("empty"), "automation-hub-empty");
                td.colSpan = 5;
                empty.appendChild(td);
                rows.appendChild(empty);
                return;
            }
            data.runs.forEach(function (run) {
                var tr = document.createElement("tr");

                var status = document.createElement("td");
                var button = document.createElement("button");
                button.type = "button";
                button.className = "automation-hub-link run-status is-" + run.status;
                button.setAttribute("data-hub", "run");
                button.setAttribute("data-id", run.id);
                button.textContent = statusText(run.status);
                if (run.error) {
                    button.title = run.error;
                }
                status.appendChild(button);
                if (run.failed > 0 && run.succeeded > 0) {
                    var partial = document.createElement("div");
                    partial.className = "run-monitor-sub";
                    partial.textContent = label("partial");
                    status.appendChild(partial);
                }
                if (run.resumeAtUtc) {
                    var resume = document.createElement("div");
                    resume.className = "run-monitor-sub";
                    resume.setAttribute("data-resume", run.resumeAtUtc);
                    status.appendChild(resume);
                }
                tr.appendChild(status);

                tr.appendChild(cell(run.triggerType + (run.isDryRun ? " · test" : "")));
                tr.appendChild(cell(run.startedText));

                var duration = cell("", "run-monitor-duration");
                duration.setAttribute("data-start", run.startedAtUtc);
                if (run.endedAtUtc) {
                    duration.setAttribute("data-end", run.endedAtUtc);
                }
                tr.appendChild(duration);

                var steps = document.createElement("td");
                steps.appendChild(chips(run));
                tr.appendChild(steps);
                rows.appendChild(tr);
                var more = detailsRow(run);
                tr.classList.add("run-row");
                if (run.steps && run.steps.length) {
                    tr.classList.add("has-details");
                    tr.addEventListener("click", function (event) {
                        if (event.target.closest("button, a")) { return; }
                        if (expanded[run.id]) { delete expanded[run.id]; } else { expanded[run.id] = true; }
                        more.hidden = !expanded[run.id];
                        tr.classList.toggle("is-open", !!expanded[run.id]);
                    });
                    more.hidden = !expanded[run.id];
                    tr.classList.toggle("is-open", !!expanded[run.id]);
                }
                rows.appendChild(more);
            });
        }

        function render() {
            if (!document.body.contains(root)) {
                window.clearInterval(timer);
                return;
            }
            els.dot.classList.toggle("is-off", failed);
            els.live.textContent = failed ? label("offline") : label("live");
            if (!data) {
                return;
            }
            var now = nowMs();
            els.zone.textContent = data.timeZoneId;

            if (!data.isPublished) {
                els.next.textContent = label("unpublished");
                els.nextAt.textContent = "";
            } else if (!data.isActive) {
                els.next.textContent = label("off");
                els.nextAt.textContent = "";
            } else if (data.next) {
                var left = Date.parse(data.next.atUtc) - now;
                els.next.textContent = left <= 0 ? label("now") : label("in") + " " + span(left);
                els.nextAt.textContent = data.next.atText;
            } else {
                els.next.textContent = label("event");
                els.nextAt.textContent = data.triggerTypes.join(", ");
            }

            els.active.textContent = data.activeRuns + " / " + data.maxConcurrentRuns;
            els.limit.textContent = label("lasthour") + ": " + data.runsLastHour + " / " + data.maxRunsPerHour;

            rows.querySelectorAll(".run-monitor-duration").forEach(function (td) {
                var end = td.getAttribute("data-end");
                var to = end ? Date.parse(end) : now;
                td.textContent = span(to - Date.parse(td.getAttribute("data-start")));
            });
            rows.querySelectorAll("[data-resume]").forEach(function (node) {
                var left = Date.parse(node.getAttribute("data-resume")) - now;
                node.textContent = label("resumes") + " " + (left <= 0 ? "00:00:00" : span(left));
            });
        }

        function poll() {
            fetch(url, {
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" }
            }).then(function (response) {
                if (!response.ok) {
                    throw new Error(response.statusText || "Error");
                }
                return response.json();
            }).then(function (payload) {
                data = payload;
                offset = Date.parse(payload.nowUtc) - Date.now();
                failed = false;
                buildRows();
                render();
            }).catch(function () {
                failed = true;
                render();
            });
        }

        timer = window.setInterval(function () {
            tick += 1;
            if (tick % 3 === 0) {
                poll();
            }
            render();
        }, 1000);
        poll();
    }

    function bootBuilder(scope) {
        var form = scope.querySelector("#automationBuilderForm");
        if (!form || typeof window.initAutomationBuilder !== "function") {
            return;
        }
        var graphEl = scope.querySelector("#automationBuilderGraph");
        var i18nEl = scope.querySelector("#automationBuilderI18n");
        var graph = {};
        var i18n = {};
        try {
            graph = JSON.parse(graphEl ? graphEl.textContent || "{}" : "{}");
        } catch (err) {
            graph = {};
        }
        try {
            i18n = JSON.parse(i18nEl ? i18nEl.textContent || "{}" : "{}");
        } catch (err) {
            i18n = {};
        }
        window.initAutomationBuilder({
            id: form.getAttribute("data-builder-id"),
            graph: graph,
            catalogUrl: form.getAttribute("data-catalog-url"),
            telegramChatsUrl: form.getAttribute("data-telegram-chats-url"),
            saveUrl: form.getAttribute("data-save-url"),
            i18n: i18n
        });
    }

    function loadPanel(url, options) {
        options = options || {};
        if (loading) {
            return Promise.resolve();
        }
        loading = true;
        return fetch(url, {
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "text/html" }
        }).then(function (response) {
            if (!response.ok) {
                throw new Error(response.statusText || "Error");
            }
            return response.text();
        }).then(function (html) {
            showHtml(html, options);
        }).catch(function (err) {
            toast(err.message || "Error", "error");
        }).then(function () {
            loading = false;
        });
    }

    function openBuilder(id) {
        if (!id) {
            return Promise.resolve();
        }
        window.location.href = "/Automation/Builder/" + encodeURIComponent(id);
        return Promise.resolve();
    }

    function openHistory(id) {
        return loadPanel("/Automation/HistoryPanel/" + id, { size: "default" });
    }

    function openSettings(id, nestedFlag) {
        return loadPanel("/Automation/SettingsPanel/" + id, { size: "default", nested: !!nestedFlag });
    }

    function openConnections(nestedFlag) {
        return loadPanel("/Automation/ConnectionsPanel", { size: "default", nested: !!nestedFlag });
    }

    function openRun(id) {
        return loadPanel("/Automation/RunDetailsPanel/" + id, { size: "default" });
    }

    function openDelete(id, name) {
        if (!deleteTemplate) {
            return;
        }
        var html = deleteTemplate.innerHTML;
        showHtml(html, { size: "confirm" });
        var form = mainBody.querySelector("form");
        var nameEl = mainBody.querySelector("[data-delete-name]");
        var idInput = mainBody.querySelector('input[name="id"]');
        if (idInput) {
            idInput.value = id;
        }
        if (nameEl) {
            nameEl.textContent = name || "";
        }
        if (form) {
            form.setAttribute("data-delete-id", id);
        }
    }

    function emptyRow() {
        var tr = document.createElement("tr");
        tr.setAttribute("data-hub-empty", "");
        tr.innerHTML = '<td colspan="4" class="automation-hub-empty"></td>';
        tr.querySelector("td").textContent = attr("data-empty");
        return tr;
    }

    function iconButton(action, id, title, icon, extraClass, name) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "automation-hub-icon" + (extraClass ? " " + extraClass : "");
        button.setAttribute("data-hub", action);
        button.setAttribute("data-id", id);
        if (name) {
            button.setAttribute("data-name", name);
        }
        button.title = title;
        button.setAttribute("aria-label", title);
        button.innerHTML = '<i class="' + icon + '"></i>';
        return button;
    }

    function iconLink(href, title, icon) {
        var link = document.createElement("a");
        link.className = "automation-hub-icon";
        link.href = href;
        link.title = title;
        link.setAttribute("aria-label", title);
        link.innerHTML = '<i class="' + icon + '"></i>';
        return link;
    }

    function renderRow(item) {
        var tr = document.createElement("tr");
        tr.setAttribute("data-automation-row", item.id);
        tr.setAttribute("data-icon", item.icon || "bx-bot");
        var desc = item.description ? '<div class="automation-hub-desc"></div>' : "";
        tr.innerHTML =
            '<td class="automation-hub-switch-cell"><div class="form-check form-switch automation-hub-switch"><input class="form-check-input" type="checkbox" data-hub-toggle aria-label="' + attr("data-col-status") + '"></div></td>' +
            '<td class="automation-hub-cell-main"><div class="automation-hub-row-main">' +
            '<span class="automation-hub-avatar ' + (item.isActive ? "is-on" : "is-off") + '" aria-hidden="true"><i class="bx ' + (item.icon || "bx-bot") + '"></i></span>' +
            '<div class="automation-hub-row-text"><div class="automation-hub-name"></div>' + desc + "</div></div></td>" +
            '<td><span class="automation-hub-status"></span></td>' +
            '<td class="automation-hub-date"></td>' +
            '<td class="automation-hub-actions"></td>';
        // Labels for the stacked (phone) layout.
        tr.children[2].setAttribute("data-label", attr("data-col-status"));
        tr.children[3].setAttribute("data-label", attr("data-col-updated"));
        var toggle = tr.querySelector("[data-hub-toggle]");
        toggle.setAttribute("data-id", item.id);
        toggle.checked = !!item.isActive;
        toggle.disabled = !can("data-can-edit");
        tr.querySelector(".automation-hub-name").textContent = item.name || "";
        var descEl = tr.querySelector(".automation-hub-desc");
        if (descEl) {
            descEl.textContent = item.description;
        }
        var status = tr.querySelector(".automation-hub-status");
        status.classList.add(item.isActive ? "is-on" : "is-off");
        status.textContent = item.isActive ? attr("data-active") : attr("data-inactive");
        tr.querySelector(".automation-hub-date").textContent = item.updatedDate || "";
        var actions = tr.querySelector(".automation-hub-actions");
        if (can("data-can-history")) {
            actions.appendChild(iconButton("history", item.id, attr("data-history"), "bx bx-history"));
        }
        if (can("data-can-edit")) {
            actions.appendChild(iconButton("settings", item.id, attr("data-settings"), "bx bx-cog"));
            var edit = iconLink("/Automation/Builder/" + item.id, attr("data-edit"), "bx bx-edit");
            edit.classList.add("is-primary");
            actions.appendChild(edit);
        }
        if (can("data-can-delete")) {
            actions.appendChild(iconButton("delete", item.id, attr("data-delete"), "bx bx-trash", "is-danger", item.name));
        }
        return tr;
    }

    function addRow(item) {
        if (!rowsHost || !item || !item.id) {
            return;
        }
        var empty = rowsHost.querySelector("[data-hub-empty]");
        if (empty) {
            empty.remove();
        }
        var existing = rowsHost.querySelector('[data-automation-row="' + item.id + '"]');
        var row = renderRow(item);
        if (existing) {
            existing.replaceWith(row);
        } else {
            rowsHost.insertBefore(row, rowsHost.firstChild);
        }
        if (window.PartnerlyAgentsMenu) {
            window.PartnerlyAgentsMenu.add(item);
        }
    }

    function removeRow(id) {
        if (!rowsHost || !id) {
            return;
        }
        var row = rowsHost.querySelector('[data-automation-row="' + id + '"]');
        if (row) {
            row.remove();
        }
        if (!rowsHost.querySelector("[data-automation-row]")) {
            rowsHost.appendChild(emptyRow());
        }
        if (window.PartnerlyAgentsMenu) {
            window.PartnerlyAgentsMenu.remove(id);
        }
    }

    function updateRowName(form) {
        if (!form) {
            return;
        }
        var id = form.getAttribute("data-builder-id");
        var nameInput = form.querySelector('input[name="name"]');
        var descInput = form.querySelector('input[name="description"]');
        if (id && nameInput && window.PartnerlyAgentsMenu) {
            window.PartnerlyAgentsMenu.rename(id, nameInput.value);
        }
        var row = rowsHost && id ? rowsHost.querySelector('[data-automation-row="' + id + '"]') : null;
        if (!row || !nameInput) {
            return;
        }
        row.querySelector(".automation-hub-name").textContent = nameInput.value;
        var descEl = row.querySelector(".automation-hub-desc");
        if (descInput && descInput.value) {
            if (!descEl) {
                descEl = document.createElement("div");
                descEl.className = "automation-hub-desc";
                row.querySelector("td").appendChild(descEl);
            }
            descEl.textContent = descInput.value;
        } else if (descEl) {
            descEl.remove();
        }
    }

    function postForm(form) {
        if (typeof window.syncAutomationBuilder === "function" && form.id === "automationBuilderForm") {
            window.syncAutomationBuilder();
        }
        var action = form.getAttribute("action") || window.location.href;
        var submitter = form.getAttribute("data-submit-action");
        var url = submitter || action;
        var data = new FormData(form);
        return fetch(url, {
            method: "POST",
            body: data,
            credentials: "same-origin",
            headers: {
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": tokenFrom(form)
            }
        }).then(function (response) {
            return parseJson(response).then(function (payload) {
                payload = payload || {};
                if (!response.ok || payload.success === false) {
                    throw new Error(payload.message || response.statusText || "Error");
                }
                return payload;
            });
        }).then(function (payload) {
            toast(payload.message);
            window.dispatchEvent(new CustomEvent("partnerly:automation-changed", { detail: { url: url } }));
            if (form.hasAttribute("data-hub-remove-row")) {
                removeRow((form.querySelector('input[name="id"]') || {}).value);
            }
            updateRowName(form);
            if (payload.reload === "connections") {
                return openConnections(isOpen(nested) || form.closest("#automationHubNested"));
            }
            if (form.getAttribute("data-hub-reload-run")) {
                return openRun(form.getAttribute("data-hub-reload-run"));
            }
            if (payload.runId) {
                return openRun(payload.runId);
            }
            if (!form.hasAttribute("data-hub-stay")) {
                closeTop();
            }
            return payload;
        }).catch(function (err) {
            toast(err.message || "Error", "error");
        });
    }

    function handleHubClick(event) {
        var closer = event.target.closest("[data-automation-hub-close]");
        if (closer) {
            event.preventDefault();
            closeTop();
            return true;
        }
        if (event.target === overlay) {
            closeSlot(false);
            return true;
        }
        if (event.target === nested) {
            closeSlot(true);
            return true;
        }
        var opener = event.target.closest("[data-hub]");
        if (!opener) {
            return false;
        }
        event.preventDefault();
        var action = opener.getAttribute("data-hub");
        var id = opener.getAttribute("data-id");
        var nestedFlag = opener.hasAttribute("data-hub-nested") || isOpen(overlay) && action !== "edit" && action !== "history" && action !== "run" && action !== "delete";
        if (action === "edit") {
            openBuilder(id);
        } else if (action === "history") {
            openHistory(id);
        } else if (action === "settings") {
            openSettings(id, nestedFlag);
        } else if (action === "connections") {
            openConnections(true);
        } else if (action === "node-history") {
            loadPanel("/Automation/NodeHistoryPanel/" + id
                + "?nodeId=" + encodeURIComponent(opener.getAttribute("data-node") || "")
                + "&name=" + encodeURIComponent(opener.getAttribute("data-name") || ""), { size: "default" });
        } else if (action === "run") {
            openRun(id);
        } else if (action === "delete") {
            openDelete(id, opener.getAttribute("data-name"));
        }
        return true;
    }

    // Connections popup: "+" opens the add form, copy icon copies the neighbouring value.
    document.addEventListener("click", function (event) {
        var toggle = event.target.closest("[data-connection-toggle]");
        if (toggle) {
            var form = document.querySelector(toggle.getAttribute("data-connection-toggle"));
            if (form) {
                form.hidden = !form.hidden;
                toggle.setAttribute("aria-expanded", form.hidden ? "false" : "true");
                toggle.classList.toggle("is-open", !form.hidden);
                if (!form.hidden) {
                    var first = form.querySelector("input");
                    if (first) {
                        first.focus();
                    }
                }
            }
            return;
        }
        var copy = event.target.closest("[data-copy]");
        if (copy) {
            var holder = copy.closest(".automation-copy-row");
            var source = holder ? holder.querySelector("[data-copy-text]") : null;
            if (!source) {
                return;
            }
            var text = source.textContent.trim();
            var done = function () {
                var icon = copy.querySelector("i");
                copy.classList.add("is-done");
                if (icon) {
                    icon.className = "bx bx-check";
                }
                window.setTimeout(function () {
                    copy.classList.remove("is-done");
                    if (icon) {
                        icon.className = "bx bx-copy";
                    }
                }, 1500);
            };
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(done, function () { /* clipboard blocked */ });
            }
        }
    });

    document.addEventListener("click", handleHubClick);

    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!form || !form.hasAttribute("data-hub-ajax")) {
            return;
        }
        event.preventDefault();
        var submitter = event.submitter;
        if (submitter && submitter.getAttribute("formaction")) {
            form.setAttribute("data-submit-action", submitter.getAttribute("formaction"));
        } else {
            form.removeAttribute("data-submit-action");
        }
        postForm(form);
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && (isOpen(nested) || isOpen(overlay))) {
            closeTop();
        }
    });

    function setActive(id, isActive) {
        if (!rowsHost || !id) {
            return;
        }
        var row = rowsHost.querySelector('[data-automation-row="' + id + '"]');
        if (!row) {
            return;
        }
        var status = row.querySelector(".automation-hub-status");
        if (!status) {
            return;
        }
        status.classList.toggle("is-on", !!isActive);
        status.classList.toggle("is-off", !isActive);
        status.textContent = isActive ? attr("data-active") : attr("data-inactive");
        var avatar = row.querySelector(".automation-hub-avatar");
        if (avatar) {
            avatar.classList.toggle("is-on", !!isActive);
            avatar.classList.toggle("is-off", !isActive);
        }
        var sw = row.querySelector("[data-hub-toggle]");
        if (sw) {
            sw.checked = !!isActive;
        }
    }

    // Switch at the start of a grid row: turn the agent on / off right away.
    if (rowsHost) {
        rowsHost.addEventListener("change", function (event) {
            var sw = event.target.closest("[data-hub-toggle]");
            if (!sw) {
                return;
            }
            var id = sw.getAttribute("data-id");
            var want = sw.checked;
            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            sw.disabled = true;
            fetch("/Automation/ToggleActive/" + encodeURIComponent(id) + "?isActive=" + (want ? "true" : "false"), {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "application/json",
                    "RequestVerificationToken": token ? token.value : ""
                }
            }).then(function (response) {
                return response.json().catch(function () { return {}; }).then(function (payload) {
                    if (!response.ok || payload.success === false) {
                        throw new Error(payload.message || response.statusText || "Error");
                    }
                    return payload;
                });
            }).then(function (payload) {
                setActive(id, payload.isActive);
                if (window.PartnerlyAgentsMenu && typeof window.PartnerlyAgentsMenu.setActive === "function") {
                    window.PartnerlyAgentsMenu.setActive(id, !!payload.isLive, !!payload.hasChanges);
                }
            }).catch(function (err) {
                sw.checked = !want;
                toast(err.message || "Error", "error");
            }).then(function () {
                sw.disabled = !can("data-can-edit");
            });
        });
    }

    // Template filter chips (All / Partnerly / Telegram / Google / WhatsApp).
    if (root) {
        root.addEventListener("click", function (event) {
            var chip = event.target.closest("[data-template-filter]");
            if (!chip) {
                return;
            }
            var value = chip.getAttribute("data-template-filter");
            root.querySelectorAll("[data-template-filter]").forEach(function (item) {
                var on = item === chip;
                item.classList.toggle("is-active", on);
                item.setAttribute("aria-selected", on ? "true" : "false");
            });
            root.querySelectorAll("[data-template-category]").forEach(function (card) {
                card.hidden = value !== "all" && card.getAttribute("data-template-category") !== value;
            });
        });
    }

    window.PartnerlyAutomationHub = {
        openBuilder: openBuilder,
        openHistory: openHistory,
        openSettings: openSettings,
        openConnections: openConnections,
        addRow: addRow,
        removeRow: removeRow,
        setActive: setActive,
        toast: toast,
        close: closeTop
    };

    if (root) {
        var params = new URLSearchParams(window.location.search);
        var editId = params.get("edit");
        var historyId = params.get("history");
        var settingsId = params.get("settings");
        var runId = params.get("run");
        var deleteId = params.get("delete");
        if (editId) {
            openBuilder(editId);
        } else if (historyId) {
            openHistory(historyId);
        } else if (settingsId) {
            openSettings(settingsId);
        } else if (runId) {
            openRun(runId);
        } else if (params.get("connections")) {
            openConnections();
        } else if (deleteId) {
            var row = rowsHost.querySelector('[data-automation-row="' + deleteId + '"]');
            var name = row ? (row.querySelector(".automation-hub-name") || {}).textContent : "";
            openDelete(deleteId, name);
        }
    }
})();
