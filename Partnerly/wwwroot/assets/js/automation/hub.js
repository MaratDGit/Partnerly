(function () {
    "use strict";

    var root = document.getElementById("automationHubRoot");
    var overlay = document.getElementById("automationHub");
    var nested = document.getElementById("automationHubNested");
    if (!root || !overlay || !nested) {
        return;
    }

    var mainCard = overlay.querySelector("[data-hub-card]");
    var nestedCard = nested.querySelector("[data-hub-card]");
    var mainBody = overlay.querySelector("[data-hub-body]");
    var nestedBody = nested.querySelector("[data-hub-body]");
    var rowsHost = root.querySelector("[data-hub-rows]");
    var deleteTemplate = document.getElementById("automationDeleteTemplate");
    var loading = false;

    function attr(name, fallback) {
        return root.getAttribute(name) || fallback || "";
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
        el.className = "toast align-items-center border-0 " + (type === "error" ? "text-bg-danger" : "text-bg-primary");
        el.setAttribute("role", "alert");
        el.innerHTML = '<div class="d-flex"><div class="toast-body"></div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
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
        if (!useNested && typeof window.destroyAutomationBuilder === "function") {
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
        if (!options.nested && typeof window.destroyAutomationBuilder === "function") {
            window.destroyAutomationBuilder();
        }
        if (target.card) {
            target.card.classList.toggle("is-wide", options.size === "wide");
            target.card.classList.toggle("is-confirm", options.size === "confirm");
        }
        target.body.innerHTML = html;
        setOpen(target.overlay, true);
        if (options.size === "wide") {
            window.requestAnimationFrame(function () {
                bootBuilder(target.body);
            });
        }
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
        return loadPanel("/Automation/BuilderPanel/" + id, { size: "wide" });
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

    function renderRow(item) {
        var tr = document.createElement("tr");
        tr.setAttribute("data-automation-row", item.id);
        var desc = item.description ? '<div class="automation-hub-desc"></div>' : "";
        tr.innerHTML =
            '<td><div class="automation-hub-name"></div>' + desc + "</td>" +
            '<td><span class="automation-hub-status"></span></td>' +
            '<td class="automation-hub-date"></td>' +
            '<td class="automation-hub-actions"></td>';
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
            actions.appendChild(iconButton("edit", item.id, attr("data-edit"), "bx bx-edit"));
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
        var row = rowsHost && id ? rowsHost.querySelector('[data-automation-row="' + id + '"]') : null;
        if (!row || !nameInput) {
            return;
        }
        row.querySelector(".automation-hub-name").textContent = nameInput.value;
        if (window.PartnerlyAgentsMenu) {
            window.PartnerlyAgentsMenu.rename(id, nameInput.value);
        }
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
        } else if (action === "run") {
            openRun(id);
        } else if (action === "delete") {
            openDelete(id, opener.getAttribute("data-name"));
        }
        return true;
    }

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

    window.PartnerlyAutomationHub = {
        openBuilder: openBuilder,
        openHistory: openHistory,
        openSettings: openSettings,
        openConnections: openConnections,
        addRow: addRow,
        removeRow: removeRow,
        toast: toast,
        close: closeTop
    };

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
})();
