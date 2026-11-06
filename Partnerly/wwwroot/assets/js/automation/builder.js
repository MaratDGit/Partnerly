(function () {
    "use strict";

    var editor = null;
    var bootId = 0;
    var cleanupFns = [];
    // Copied card (survives switching between agents in the same page session).
    var clipboard = null;

    function q(selector) {
        var form = document.getElementById("automationBuilderForm");
        return (form || document).querySelector(selector);
    }

    window.destroyAutomationBuilder = function () {
        bootId += 1;
        cleanupFns.forEach(function (fn) {
            try { fn(); } catch (err) { }
        });
        cleanupFns = [];
        if (editor) {
            try { editor.clear(); } catch (err) { }
            editor = null;
        }
    };

    window.syncAutomationBuilder = function () { };

    window.initAutomationBuilder = function (passed) {
        window.destroyAutomationBuilder();
        var myBoot = bootId;
        var config = passed || window.automationBuilder || {};
        window.automationBuilder = config;
        var i18n = config.i18n || {};
        var root = document.getElementById("drawflow");
        if (!root || typeof Drawflow === "undefined") {
            return;
        }

        editor = new Drawflow(root);
        editor.reroute = true;
        editor.zoom = 1;
        editor.zoom_min = 0.2;
        editor.zoom_max = 1.8;
        editor.zoom_value = 0.08;
        editor.start();

    var selectedId = null;
    var FALLBACK_ENTITIES = [
        { value: "user", label: "User" },
        { value: "support_ticket", label: "Support ticket" },
        { value: "notification", label: "Notification" },
        { value: "user_group", label: "User group" },
        { value: "email_template", label: "Email template" }
    ];
    var lookups = {};
    var FAVORITES_KEY = "partnerly.builder.favorites";
    var favorites = [];
    try {
        var storedFavorites = JSON.parse(localStorage.getItem(FAVORITES_KEY) || "[]");
        favorites = Array.isArray(storedFavorites) ? storedFavorites : [];
    } catch (e) { favorites = []; }
    // Favorites are stored per user on the server (shared by every editor and
    // browser). Browser storage is only the fallback while the server list is
    // unavailable and the source of a one-time import.
    var favoritesRemote = false;
    function saveFavorites() {
        try { localStorage.setItem(FAVORITES_KEY, JSON.stringify(favorites)); } catch (e) { /* storage unavailable */ }
    }
    function postForm(url, fields) {
        var form = document.getElementById("automationBuilderForm");
        var tokenInput = form ? form.querySelector('input[name="__RequestVerificationToken"]') : null;
        var body = Object.keys(fields).map(function (name) {
            return encodeURIComponent(name) + "=" + encodeURIComponent(fields[name]);
        }).join("&");
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": tokenInput ? tokenInput.value : ""
            },
            body: body
        }).then(function (response) {
            if (!response.ok) { throw new Error(response.statusText || "Error"); }
            return response.json();
        });
    }
    function setFavorite(key, on) {
        var at = favorites.indexOf(key);
        if (on && at === -1) { favorites.push(key); }
        if (!on && at !== -1) { favorites.splice(at, 1); }
        if (!favoritesRemote || !config.favoriteUrl) {
            saveFavorites();
            return;
        }
        postForm(config.favoriteUrl, { key: key, on: on ? "true" : "false" }).catch(function () {
            // Not saved on the server: undo the change and repaint.
            var back = favorites.indexOf(key);
            if (on && back !== -1) { favorites.splice(back, 1); }
            if (!on && back === -1) { favorites.push(key); }
            refreshFavorites();
        });
    }
    var favRepaint = [];
    function refreshFavorites() {
        favRepaint.forEach(function (repaint) { repaint(); });
    }
    // Called once the catalog is loaded: take the server list and move any
    // old browser-only favorites into the account.
    function adoptServerFavorites(serverList) {
        if (!Array.isArray(serverList)) {
            return;
        }
        favoritesRemote = true;
        var legacy = [];
        try {
            var stored = JSON.parse(localStorage.getItem(FAVORITES_KEY) || "[]");
            legacy = Array.isArray(stored) ? stored : [];
        } catch (e) { legacy = []; }
        favorites = serverList.slice();
        var missing = legacy.filter(function (key) { return typeof key === "string" && favorites.indexOf(key) === -1; });
        if (missing.length && config.favoriteImportUrl) {
            postForm(config.favoriteImportUrl, { keys: missing.join(",") }).then(function (payload) {
                if (payload && Array.isArray(payload.favorites)) {
                    favorites = payload.favorites;
                    try { localStorage.removeItem(FAVORITES_KEY); } catch (e) { /* ignore */ }
                    refreshFavorites();
                }
            }).catch(function () { /* keep browser copy, try again next time */ });
        } else if (legacy.length) {
            try { localStorage.removeItem(FAVORITES_KEY); } catch (e) { /* ignore */ }
        }
    }
    var GROUP_ICONS = {
        "Telegram": "bxl-telegram",
        "Gmail": "bxl-gmail",
        "Outlook": "bxl-microsoft",
        "Outlook Calendar": "bx-calendar",
        "Google Sheets": "bx-spreadsheet",
        "Google Drive": "bxl-google-cloud",
        "WhatsApp": "bxl-whatsapp",
        "Computer": "bx-desktop"
    };
    var TYPE_ICONS = {
        manual: "bx-pointer",
        schedule: "bx-calendar",
        interval: "bx-timer",
        "entity.created": "bx-plus-circle",
        "entity.updated": "bx-edit",
        "entity.deleted": "bx-trash",
        webhook: "bx-link-alt",
        webhook_action: "bx-send",
        event: "bx-bell",
        delay: "bx-time",
        set_variable: "bx-code-alt",
        send_email: "bx-envelope",
        send_notification: "bx-bell",
        http_request: "bx-globe",
        execute_workflow: "bx-play-circle",
        loop: "bx-repost",
        store_set: "bx-save",
        store_get: "bx-data",
        store_delete: "bx-eraser",
        once: "bx-check-double",
        filter: "bx-filter-alt",
        branch: "bx-git-branch",
        wait_until: "bx-hourglass",
        "switch": "bx-shuffle",
        create_record: "bx-plus-circle",
        update_record: "bx-edit",
        find_record: "bx-search",
        delete_record: "bx-trash",
        "computer.notify": "bx-bell",
        "computer.message_box": "bx-message-square-detail",
        "computer.speak": "bx-volume-full",
        "computer.open": "bx-link-external",
        "computer.clipboard_set": "bx-clipboard",
        "computer.clipboard_get": "bx-paste",
        "computer.system_info": "bx-chip",
        "computer.screenshot": "bx-camera",
        "computer.process_running": "bx-pulse",
        "computer.read_file": "bx-file",
        "computer.write_file": "bx-edit-alt",
        "computer.list_folder": "bx-folder-open",
        "computer.copy_file": "bx-copy",
        "computer.move_file": "bx-move",
        "computer.zip_folder": "bx-archive",
        "computer.save_attachment": "bx-download",
        "computer.get_file": "bx-upload",
        "computer.run_program": "bx-play-circle",
        "computer.run_script": "bx-terminal",
        "computer.close_program": "bx-window-close",
        "computer.lock": "bx-lock-alt",
        "computer.sleep": "bx-moon",
        "computer.file_created": "bx-file-blank",
        "computer.status_changed": "bx-wifi",
        "computer.program_started": "bx-rocket",
        "computer.file_changed": "bx-file-find",
        "computer.file_deleted": "bx-file-blank",
        "computer.program_stopped": "bx-stop-circle",
        "computer.session_changed": "bx-log-in-circle",
        "computer.clipboard_changed": "bx-clipboard",
        "computer.power_changed": "bx-battery",
        "computer.drive_changed": "bx-usb",
        "computer.idle_changed": "bx-time-five",
        "computer.disk_low": "bx-hdd",
        "computer.create_folder": "bx-folder-plus",
        "computer.delete_file": "bx-trash",
        "computer.file_info": "bx-search-alt",
        "computer.unzip": "bx-package",
        "computer.list_programs": "bx-list-ul",
        "computer.volume": "bx-volume-low",
        "computer.media": "bx-play",
        "computer.display_off": "bx-desktop",
        "computer.shutdown": "bx-power-off",
        "computer.cancel_shutdown": "bx-undo",
        "computer.active_window": "bx-window",
        "computer.network_info": "bx-network-chart",
        "computer.ping_host": "bx-broadcast",
        "computer.download_url": "bx-cloud-download"
    };
    var ui = typeof window.partnerlyBuilderFields === "function" ? window.partnerlyBuilderFields(t) : null;

    // Entities come from the server (connectors); the static list is a fallback.
    function ENTITIES() {
        var list = lookups.entities || [];
        return list.length
            ? list.map(function (item) { return { value: item.key, label: item.name || item.key }; })
            : FALLBACK_ENTITIES;
    }

    function t(key, fallback) {
        var value = i18n[key];
        return value == null || value === "" ? (fallback || key) : value;
    }

    function typeLabel(type, fallback) {
        var types = i18n.types || {};
        return types[type] || fallback || type;
    }

    function delayUnits() {
        return [
            { value: "seconds", label: t("seconds", "Seconds") },
            { value: "minutes", label: t("minutes", "Minutes") },
            { value: "hours", label: t("hours", "Hours") },
            { value: "days", label: t("days", "Days") }
        ];
    }

    function delayFields(helpKey, required) {
        return [
            { key: "amount", type: "number", min: 1, label: t("delayAmount", "Wait for"), help: t(helpKey, ""), required: !!required },
            { key: "unit", type: "select", label: t("delayUnit", "Unit"), options: delayUnits() }
        ];
    }

    var SCHEMAS = {
        manual: [{ type: "note", help: t("manualHelp") }],
        schedule: [
            {
                key: "scheduleKind",
                type: "select",
                label: t("scheduleKind"),
                help: t("scheduleKindHelp"),
                options: [
                    { value: "once", label: t("kindOnce") },
                    { value: "hourly", label: t("kindHourly") },
                    { value: "daily", label: t("kindDaily") },
                    { value: "weekly", label: t("kindWeekly") },
                    { value: "monthly", label: t("kindMonthly") },
                    { value: "cron", label: t("kindCron") }
                ]
            },
            { key: "time", type: "time", label: t("scheduleTime"), help: t("scheduleTimeHelp"), showIf: { scheduleKind: ["hourly", "daily", "weekly", "monthly"] } },
            { key: "at", type: "datetime", label: t("scheduleDate"), showIf: { scheduleKind: ["once"] } },
            { key: "expression", type: "text", plain: true, label: t("cronExpression"), help: t("cronExpressionHelp"), showIf: { scheduleKind: ["cron"] } }
        ],
        interval: [
            { key: "minutes", type: "number", min: 1, label: t("intervalMinutes"), help: t("intervalMinutesHelp") }
        ],
        "entity.created": entityFields(),
        "entity.updated": entityFields(),
        "entity.deleted": entityFields(),
        event: [
            { key: "event", type: "text", source: "events", label: t("eventName"), help: t("eventNameHelp") }
        ].concat(waitBeforeFields()),
        webhook: [
            { type: "note", help: t("webhookTriggerHelp") }
        ].concat(waitBeforeFields()),
        delay: delayFields("delayHelp", true),
        set_variable: [
            { key: "name", type: "text", plain: true, label: t("variableName"), help: t("variableNameHelp") },
            { key: "value", type: "text", label: t("variableValue"), help: t("variableValueHelp") }
        ],
        send_email: [
            { key: "to", type: "text", source: "userEmails", label: t("emailTo"), help: t("emailToHelp") },
            { key: "template", type: "text", source: "templates", strict: true, label: t("emailTemplate"), help: t("emailTemplateHelp") },
            { key: "subject", type: "text", label: t("emailSubject") },
            { key: "body", type: "textarea", label: t("emailBody") }
        ],
        send_notification: [
            { key: "message", type: "textarea", label: t("notificationMessage") },
            { key: "userId", type: "text", source: "userIds", label: t("notificationUser"), help: t("notificationUserHelp") },
            { key: "notificationType", type: "text", source: "notifTypes", label: t("notificationType") },
            { key: "link", type: "text", source: "links", label: t("notificationLink") }
        ],
        http_request: [
            {
                key: "method",
                type: "select",
                label: t("httpMethod"),
                options: ["GET", "POST", "PUT", "PATCH", "DELETE"].map(function (item) {
                    return { value: item, label: item };
                })
            },
            { key: "url", type: "text", label: t("httpUrl"), help: t("httpUrlHelp") },
            { key: "connectionId", type: "text", source: "httpConnections", strict: true, label: t("httpConnection"), help: t("httpConnectionHelp") },
            { key: "body", type: "textarea", label: t("httpBody"), help: t("httpBodyHelp") }
        ],
        webhook_action: [
            { key: "url", type: "text", label: t("webhookUrl"), help: t("webhookUrlHelp") },
            { key: "body", type: "textarea", label: t("webhookBody"), help: t("webhookBodyHelp") },
            { key: "secret", type: "text", plain: true, label: t("webhookSecret"), help: t("webhookSecretHelp") }
        ],
        execute_workflow: [
            { key: "automationId", type: "text", source: "automations", strict: true, label: t("childAutomation"), help: t("childAutomationHelp") },
            {
                key: "wait",
                type: "select",
                label: t("execWait", "Wait for the result"),
                help: t("execWaitHelp", ""),
                options: [
                    { value: "", label: t("execWaitNo", "No, just start it") },
                    { value: "true", label: t("execWaitYes", "Yes, wait until it finishes") }
                ]
            }
        ],
        store_set: [
            { key: "key", type: "text", label: t("storeKey", "Name (key)"), help: t("storeKeyHelp", "") },
            { key: "value", type: "textarea", label: t("storeValue", "Value") },
            { key: "ttlHours", type: "number", min: 1, label: t("storeTtl", "Forget after (hours)"), help: t("storeTtlHelp", "") }
        ],
        store_get: [
            { key: "key", type: "text", label: t("storeKey", "Name (key)"), help: t("storeKeyHelp", "") },
            { key: "default", type: "text", label: t("storeDefault", "Value when nothing is saved") }
        ],
        store_delete: [
            { key: "key", type: "text", label: t("storeKey", "Name (key)"), help: t("storeKeyHelp", "") }
        ],
        once: [
            { key: "key", type: "text", label: t("storeKey", "Name (key)"), help: t("onceKeyHelp", "") },
            { key: "ttlHours", type: "number", min: 1, label: t("storeTtl", "Forget after (hours)"), help: t("storeTtlHelp", "") }
        ],
        loop: [
            { key: "items", type: "textarea", label: t("loopItems", "List to go through"), help: t("loopItemsHelp", "") },
            { key: "maxItems", type: "number", min: 1, label: t("loopMax", "Most items"), help: t("loopMaxHelp", "") }
        ],
        filter: [{ type: "note", help: t("filterHelp", "") }].concat(conditionFields(false).slice(1)),
        branch: conditionFields(false),
        wait_until: conditionFields(true),
        "switch": [
            { key: "value", type: "text", label: t("switchValue"), help: t("switchValueHelp") }
        ],
        create_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "fields", type: "pairs", mode: "create", label: t("recordFields"), help: t("recordFieldsHelp") }
        ],
        update_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "key", type: "pairs", mode: "key", label: t("recordKey"), help: t("recordKeyHelp") },
            { key: "fields", type: "pairs", mode: "update", label: t("recordFields"), help: t("recordFieldsHelp") }
        ],
        find_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "filters", type: "pairs", mode: "filter", label: t("recordFilters"), help: t("recordFieldsHelp") }
        ],
        delete_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "key", type: "pairs", mode: "key", label: t("recordKey"), help: t("recordKeyHelp") }
        ]
    };

    function entityFields() {
        return [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES }
        ].concat(waitBeforeFields());
    }

    function waitBeforeFields() {
        return [
            { key: "amount", type: "number", min: 0, label: t("waitBeforeStart"), help: t("waitBeforeStartHelp") },
            { key: "unit", type: "select", label: t("delayUnit"), options: delayUnits() }
        ];
    }

    function conditionFields(withUntil) {
        var fields = [
            { type: "note", help: t("conditionHelp") },
            { key: "condition.left", type: "text", label: t("conditionLeft") },
            {
                key: "condition.op",
                type: "select",
                label: t("conditionOp"),
                options: [
                    { value: "equals", label: "equals" },
                    { value: "not_equals", label: "not equals" },
                    { value: "contains", label: "contains" },
                    { value: "greater_than", label: "greater than" },
                    { value: "less_than", label: "less than" },
                    { value: "is_empty", label: "is empty" },
                    { value: "is_not_empty", label: "is not empty" },
                    { value: "changed", label: "changed" }
                ]
            },
            { key: "condition.right", type: "text", label: t("conditionRight") }
        ];
        if (withUntil) {
            fields.push({
                key: "until",
                type: "datetime",
                label: t("waitUntilDeadline"),
                help: t("waitUntilDeadlineHelp")
            });
        }
        return fields;
    }

    // Nodes from a template have no connectionId. When the user has exactly one
    // connection of the needed kind, pick it so the agent is ready to publish.
    function fillTemplateConnections(descriptors) {
        var kindByType = {};
        descriptors.forEach(function (item) {
            if (item.connectionKind) {
                kindByType[item.type] = item.connectionKind;
            }
        });
        var home = (editor.export().drawflow || {}).Home || {};
        var changed = false;
        Object.keys(home.data || {}).forEach(function (key) {
            var data = home.data[key].data || {};
            var kind = kindByType[data.type];
            if (!kind || (data.config && data.config.connectionId)) {
                return;
            }
            var matching = catalogConnections.filter(function (c) { return c.kind === kind; });
            if (matching.length !== 1) {
                return;
            }
            data.config = data.config || {};
            data.config.connectionId = matching[0].id;
            editor.updateNodeDataFromId(home.data[key].id, data);
            changed = true;
        });
        if (changed) {
            syncHidden();
        }
    }

    // Integration node forms live in /assets/js/integrations/builder-forms.js.
    var EXTRA_SCHEMAS = typeof window.partnerlyIntegrationNodeForms === "function"
        ? (window.partnerlyIntegrationNodeForms(t) || {})
        : {};
    var catalogConnections = [];
    var integrationsUrl = "";

    function onErrorField() {
        return {
            key: "onError",
            type: "select",
            label: t("onError", "If this card fails"),
            help: t("onErrorHelp", ""),
            options: [
                { value: "", label: t("onErrorStop", "Stop this branch (default)") },
                { value: "continue", label: t("onErrorContinue", "Ignore the error and go on") },
                { value: "route", label: t("onErrorRoute", "Go to the error path (red output)") }
            ]
        };
    }

    function baseSchemaFor(data) {
        if (EXTRA_SCHEMAS[data.type]) {
            return EXTRA_SCHEMAS[data.type];
        }
        if (data.type === "delay" && data.kind === "trigger") {
            return delayFields("delayTriggerHelp", true);
        }
        if (data.type === "webhook" && data.kind === "action") {
            return SCHEMAS.webhook_action;
        }
        return SCHEMAS[data.type] || [];
    }

    function schemaFor(data) {
        if (!data) {
            return [];
        }
        var base = baseSchemaFor(data);
        return data.kind === "action" ? base.concat([onErrorField()]) : base;
    }

    function defaultConfig(type, kind) {
        if (type === "delay") {
            return kind === "trigger"
                ? { amount: 1, unit: "hours" }
                : { amount: 5, unit: "minutes" };
        }
        if (type === "schedule") {
            return { scheduleKind: "daily", time: "09:00" };
        }
        if (type === "interval") {
            return { minutes: 15 };
        }
        if (type === "set_variable") {
            return { name: "flag", value: "1" };
        }
        if (type === "http_request") {
            return { method: "GET" };
        }
        if (type === "loop") {
            return { items: "", maxItems: 100 };
        }
        if (type === "branch" || type === "wait_until" || type === "filter") {
            return { condition: { op: "equals", left: "{{vars.ready}}", right: "yes" } };
        }
        return {};
    }

    function slug(type) {
        return String(type || "node").replace(/[^A-Za-z0-9_]/g, "_");
    }

    // Cards of an integration show a small icon of the service next to the card title.
    var SERVICE_CHIPS = {
        telegram: { cls: "telegram", icon: "bxl-telegram", label: "Telegram" },
        sheets: { cls: "sheets", icon: "bx-spreadsheet", label: "Google Sheets" },
        gmail: { cls: "gmail", icon: "bxl-gmail", label: "Gmail" },
        drive: { cls: "drive", icon: "bxl-google-cloud", label: "Google Drive" },
        whatsapp: { cls: "whatsapp", icon: "bxl-whatsapp", label: "WhatsApp" },
        outlook: { cls: "outlook", icon: "bxl-microsoft", label: "Outlook" },
        outlookCalendar: { cls: "outlook", icon: "bx-calendar", label: "Outlook Calendar" },
        computer: { cls: "computer", icon: "bx-desktop", label: "Computer" }
    };

    function serviceOf(type) {
        var match = /^(telegram|sheets|gmail|drive|whatsapp|outlook|computer)\./.exec(String(type || ""));
        if (!match) { return null; }
        if (match[1] === "outlook" && /\.(create_event|find_events|event_added)$/.test(type)) {
            return SERVICE_CHIPS.outlookCalendar;
        }
        return SERVICE_CHIPS[match[1]];
    }

    function serviceChipHtml(type) {
        var service = serviceOf(type);
        return service
            ? '<span class="auto-node-service is-' + service.cls + '" title="' + service.label + '"><i class="bx ' + service.icon + '" aria-hidden="true"></i></span>'
            : "";
    }

    function nodeHtml(data) {
        return '<div class="auto-node auto-node--' + (data.kind || "action") + '">' +
            '<span class="auto-node-badge" aria-hidden="true"><i class="bx ' + (data.kind === "trigger" ? "bxs-bolt" : "bx-cube-alt") + '"></i></span>' +
            '<span class="auto-node-kind">' + (data.kind === "trigger" ? t("triggerBlocks", "When") : t("actionBlocks", "Then")) + "</span>" +
            '<strong class="auto-node-type">' + serviceChipHtml(data.type) + "<span>" + typeLabel(data.type, data.type) + "</span></strong>" +
            '<span class="auto-node-name">' + (data.name || "") + "</span>" +
            '<span class="auto-node-missing" hidden></span>' +
            '<span class="auto-node-off" title="' + t("builderDisabledHint", "This card is turned off: the flow skips it") + '"><i class="bx bx-power-off" aria-hidden="true"></i> ' + t("builderDisabledBadge", "Off") + "</span></div>";
    }

    // ---- "configured" check on cards -------------------------------------
    function filled(value) {
        if (value == null) { return false; }
        if (Array.isArray(value)) { return value.length > 0; }
        if (typeof value === "number") { return isFinite(value); }
        if (typeof value === "object") { return Object.keys(value).length > 0; }
        return String(value).trim() !== "";
    }

    var CONFIG_RULES = {
        manual: function () { return true; },
        webhook: function () { return true; },
        schedule: function (c) {
            var kind = c.scheduleKind || "daily";
            if (kind === "once") { return filled(c.at); }
            if (kind === "cron") { return filled(c.expression); }
            return filled(c.time);
        },
        interval: function (c) { return Number(c.minutes) > 0; },
        event: function (c) { return filled(c.event); },
        "entity.created": function (c) { return filled(c.entity); },
        "entity.updated": function (c) { return filled(c.entity); },
        "entity.deleted": function (c) { return filled(c.entity); },
        delay: function (c) { return Number(c.amount) > 0; },
        set_variable: function (c) { return filled(c.name); },
        send_email: function (c) { return filled(c.to) && (filled(c.template) || (filled(c.subject) && filled(c.body))); },
        send_notification: function (c) { return filled(c.message); },
        http_request: function (c) { return filled(c.url); },
        webhook_action: function (c) { return filled(c.url); },
        execute_workflow: function (c) { return filled(c.automationId); },
        loop: function (c) { return filled(c.items); },
        store_set: function (c) { return filled(c.key); },
        store_get: function (c) { return filled(c.key); },
        store_delete: function (c) { return filled(c.key); },
        once: function (c) { return filled(c.key); },
        filter: function (c) { return !!c.condition && filled(c.condition.left); },
        branch: function (c) { return !!c.condition && filled(c.condition.left); },
        wait_until: function (c) { return !!c.condition && filled(c.condition.left); },
        "switch": function (c) { return filled(c.value); },
        create_record: function (c) { return filled(c.entity) && filled(c.fields); },
        update_record: function (c) { return filled(c.entity) && filled(c.key) && filled(c.fields); },
        find_record: function (c) { return filled(c.entity); },
        delete_record: function (c) { return filled(c.entity) && filled(c.key); }
    };

    var INTEGRATION_PREFIX = /^(telegram|sheets|gmail|outlook|drive|whatsapp|computer)\./;
    // Settings (besides the connection) a card needs before it can work.
    var INTEGRATION_REQUIRED = {
        "telegram.send_message": ["chatId", "text"],
        "telegram.send_document": ["chatId", "attachmentId"],
        "sheets.append_row": ["spreadsheetId", "values"],
        "sheets.update_row": ["spreadsheetId", "rowNumber", "values"],
        "sheets.find_rows": ["spreadsheetId", "value"],
        "sheets.row_added": ["spreadsheetId"],
        "gmail.send": ["to", "subject", "body"],
        "outlook.send": ["to", "subject", "body"],
        "outlook.create_event": ["subject", "start"],
        "gmail.reply": ["messageId", "body"],
        "gmail.get_message": ["messageId"],
        "gmail.create_draft": ["to", "subject", "body"],
        "gmail.mark_read": ["messageId"],
        "gmail.archive": ["messageId"],
        "gmail.trash": ["messageId"],
        "gmail.label": ["messageId", "label"],
        "sheets.get_row": ["spreadsheetId", "rowNumber"],
        "sheets.read_range": ["spreadsheetId", "range"],
        "sheets.update_cell": ["spreadsheetId", "cell"],
        "sheets.clear_range": ["spreadsheetId", "range"],
        "sheets.delete_row": ["spreadsheetId", "rowNumber"],
        "sheets.create_spreadsheet": ["title"],
        "sheets.add_sheet": ["spreadsheetId", "sheet"],
        "drive.get_file": ["fileId"],
        "drive.copy_file": ["fileId"],
        "drive.move_file": ["fileId", "folderId"],
        "drive.rename_file": ["fileId", "name"],
        "drive.trash_file": ["fileId"],
        "drive.share_file": ["fileId", "email"],
        "drive.create_text_file": ["name"],
        "drive.upload_file": ["attachmentId"],
        "drive.create_folder": ["name"],
        "whatsapp.send_text": ["to", "text"],
        "whatsapp.send_template": ["to", "template", "language"],
        "computer.notify": ["text"],
        "computer.message_box": ["text"],
        "computer.speak": ["text"],
        "computer.open": ["target"],
        "computer.clipboard_set": ["text"],
        "computer.process_running": ["name"],
        "computer.read_file": ["path"],
        "computer.write_file": ["path", "text"],
        "computer.list_folder": ["path"],
        "computer.copy_file": ["source", "destination"],
        "computer.move_file": ["source", "destination"],
        "computer.zip_folder": ["source", "destination"],
        "computer.save_attachment": ["attachmentId", "folder"],
        "computer.get_file": ["path"],
        "computer.run_program": ["path"],
        "computer.run_script": ["path"],
        "computer.close_program": ["name"],
        "computer.create_folder": ["path"],
        "computer.delete_file": ["path"],
        "computer.file_info": ["path"],
        "computer.unzip": ["source", "destination"],
        "computer.ping_host": ["host"],
        "computer.download_url": ["url", "folder"]
    };

    function settingsReady(data) {
        if (data.disabled) { return true; }
        var config = normalizeDelay(JSON.parse(JSON.stringify(data.config || {})));
        var type = data.type;
        if (INTEGRATION_PREFIX.test(type)) {
            if (!filled(config.connectionId)) { return false; }
            return (INTEGRATION_REQUIRED[type] || []).every(function (key) { return filled(config[key]); });
        }
        var rule = CONFIG_RULES[type];
        return rule ? !!rule(config) : true;
    }

    function connectionCount(map) {
        var total = 0;
        Object.keys(map || {}).forEach(function (key) {
            total += (map[key].connections || []).length;
        });
        return total;
    }

    // Settings filled in AND the card is wired into the flow.
    function refreshChecks() {
        if (!editor || importing) { return; }
        var exported = editor.export();
        var home = exported.drawflow && exported.drawflow.Home && exported.drawflow.Home.data ? exported.drawflow.Home.data : {};
        Array.prototype.forEach.call(root.querySelectorAll(".connection.is-muted"), function (line) {
            line.classList.remove("is-muted");
        });
        Object.keys(home).forEach(function (key) {
            var node = home[key];
            var data = node.data || {};
            var off = root.querySelector("#node-" + node.id + " .auto-node");
            if (off) { off.classList.toggle("is-disabled", !!data.disabled); }
            if (data.disabled) {
                Array.prototype.forEach.call(root.querySelectorAll(".connection.node_in_node-" + node.id + ", .connection.node_out_node-" + node.id), function (line) {
                    line.classList.add("is-muted");
                });
            }
            var wired = data.kind === "trigger" ? connectionCount(node.outputs) > 0 : connectionCount(node.inputs) > 0;
            var inner = root.querySelector("#node-" + node.id + " .auto-node");
            if (inner) {
                var fieldsReady = settingsReady(data);
                var ready = wired && fieldsReady;
                inner.classList.toggle("is-configured", ready);
                // Fields that still need a value are listed on the card in the colour of its state line.
                var missing = [];
                if (!data.disabled && !fieldsReady) {
                    try { missing = missingFor(data); } catch (e) { missing = []; }
                }
                var hint = inner.querySelector(".auto-node-missing");
                var hintText = missing.length
                    ? t("nodeFillIn", "Fill in") + ": " + missing.slice(0, 3).join(", ") + (missing.length > 3 ? " +" + (missing.length - 3) : "")
                    : "";
                if (hint && (hint.textContent !== hintText || hint.hidden === !!hintText)) {
                    hint.textContent = hintText;
                    hint.hidden = !hintText;
                    hint.title = missing.join(", ");
                    // The card changed height: re-draw its lines.
                    try { editor.updateConnectionNodes("node-" + node.id); } catch (e) { /* card is going away */ }
                }
                // The colour line on the left edge tells the state; the tooltip spells it out.
                inner.title = data.disabled
                    ? t("builderDisabledHint", "This card is turned off: the flow skips it")
                    : (ready ? t("nodeConfigured", "Configured")
                        : (missing.length ? t("nodeFillIn", "Fill in") + ": " + missing.join(", ")
                            : (fieldsReady ? t("nodeNotConnected", "Not connected to the flow") : t("nodeNeedsSetup", "Needs setup"))));
            }
        });
    }

    function baseOutputs(type) {
        return type === "branch" || type === "switch" || type === "loop" ? 2 : 1;
    }

    // A card that sends its errors down their own path has one more (red) output.
    function hasErrorOutput(data) {
        return !!data && data.kind === "action" && !!data.config && data.config.onError === "route";
    }

    function outputsFor(type, data) {
        return baseOutputs(type) + (hasErrorOutput(data) ? 1 : 0);
    }

    function portName(type, outputKey, data) {
        var index = parseInt(String(outputKey).replace("output_", ""), 10) || 1;
        if (hasErrorOutput(data) && index === baseOutputs(type) + 1) {
            return "error";
        }
        if (type === "branch") {
            return index === 2 ? "false" : "true";
        }
        if (type === "switch") {
            return index === 2 ? "case:0" : "default";
        }
        if (type === "loop") {
            return index === 2 ? "done" : "each";
        }
        return null;
    }

    function outputForPort(type, port, data) {
        if (port === "error") {
            return "output_" + (baseOutputs(type) + 1);
        }
        if (port === "false" || port === "done" || (port && port.indexOf("case:") === 0)) {
            return "output_2";
        }
        return "output_1";
    }

    // Tooltips and colour of the outputs that mean something (each / done / error).
    function decoratePorts(id) {
        var node = nodeOrNull(id);
        var el = document.getElementById("node-" + id);
        if (!node || !el) { return; }
        var data = node.data || {};
        var outs = el.querySelectorAll(".outputs .output");
        Array.prototype.forEach.call(outs, function (out, i) {
            var key = "output_" + (i + 1);
            var port = portName(data.type, key, data);
            out.classList.toggle("is-error-port", port === "error");
            out.classList.toggle("is-loop-each", port === "each");
            out.classList.toggle("is-loop-done", port === "done");
            var label = port === "error" ? t("portError", "If it fails") : port === "each" ? t("portEach", "Each item") : port === "done" ? t("portDone", "When finished") : "";
            if (label) { out.title = label; } else { out.removeAttribute("title"); }
        });
    }

    // Keep the red output in step with the "If this card fails" choice.
    function syncErrorOutput(id, data) {
        var node = nodeOrNull(id);
        if (!node || !data || data.kind !== "action") { return; }
        var base = baseOutputs(data.type);
        var count = Object.keys(node.outputs || {}).length;
        if (hasErrorOutput(data) && count === base) {
            editor.addNodeOutput(id);
        } else if (!hasErrorOutput(data) && count > base) {
            editor.removeNodeOutput(id, "output_" + count);
        } else {
            decoratePorts(id);
            return;
        }
        decoratePorts(id);
        if (!importing) { refreshChecks(); }
    }

    function dropPoint() {
        var rect = root.getBoundingClientRect();
        var zoom = editor.zoom || 1;
        return {
            x: ((rect.width * 0.42) - (editor.canvas_x || 0)) / zoom,
            y: ((rect.height * 0.36) - (editor.canvas_y || 0)) / zoom
        };
    }

    function addNode(data, x, y) {
        var inputs = data.kind === "trigger" ? 0 : 1;
        var outputs = outputsFor(data.type, data);
        var point = (x == null || y == null) ? dropPoint() : null;
        var created = editor.addNode(
            slug(data.type),
            inputs,
            outputs,
            x == null ? point.x : x,
            y == null ? point.y : y,
            slug(data.kind + "-" + data.type),
            data,
            nodeHtml(data));
        decoratePorts(created);
        return created;
    }

    // New card from a catalog item (toolbar palette or the "+" port picker).
    function createFromItem(item, x, y) {
        var config = defaultConfig(item.type, item.kind);
        if (item.connectionKind) {
            var first = catalogConnections.filter(function (c) { return c.kind === item.connectionKind; })[0];
            if (first) {
                config.connectionId = first.id;
            }
        }
        var id = addNode({
            id: "n" + Date.now() + Math.floor(Math.random() * 1000),
            kind: item.kind,
            type: item.type,
            name: slug(item.type) + editor.getNodesFromName(slug(item.type)).length,
            config: config
        }, x, y);
        syncHidden();
        return id;
    }

    function exportGraph() {
        var exported = editor.export();
        var home = exported.drawflow && exported.drawflow.Home && exported.drawflow.Home.data
            ? exported.drawflow.Home.data
            : {};
        var idMap = {};
        var nodes = [];
        Object.keys(home).forEach(function (key) {
            var node = home[key];
            var data = node.data || {};
            var id = data.id || ("n" + node.id);
            data.id = id;
            idMap[String(node.id)] = id;
            nodes.push({
                id: id,
                kind: data.kind,
                type: data.type,
                name: data.name,
                x: node.pos_x,
                y: node.pos_y,
                config: data.config || {},
                disabled: data.disabled ? true : undefined
            });
        });
        var edges = [];
        Object.keys(home).forEach(function (key) {
            var node = home[key];
            var fromId = idMap[String(node.id)];
            var type = (node.data || {}).type;
            Object.keys(node.outputs || {}).forEach(function (outputKey) {
                (node.outputs[outputKey].connections || []).forEach(function (connection, index) {
                    edges.push({
                        id: "e-" + fromId + "-" + connection.node + "-" + index,
                        from: fromId,
                        to: idMap[String(connection.node)],
                        port: portName(type, outputKey, node.data)
                    });
                });
            });
        });
        var out = { schemaVersion: 1, nodes: nodes, edges: edges };
        if (boardNotes.length) {
            out.notes = JSON.parse(JSON.stringify(boardNotes));
        }
        var settings = cleanSettings(boardSettings);
        if (settings) {
            out.settings = settings;
        }
        return out;
    }

    // Agent variables ({{vars.name}} in every run) and the failure alert switch.
    var boardSettings = null;
    function cleanSettings(source) {
        if (!source) { return null; }
        var result = {};
        var names = source.variables ? Object.keys(source.variables).filter(function (name) { return /^[A-Za-z0-9_]+$/.test(name); }) : [];
        if (names.length) {
            result.variables = {};
            names.forEach(function (name) { result.variables[name] = String(source.variables[name]); });
        }
        if (source.notifyOnFailure === false) { result.notifyOnFailure = false; }
        return Object.keys(result).length ? result : null;
    }

    // Sticky notes on the canvas (drawn by builder-tools.js); they travel with the graph.
    var boardNotes = [];

    function importGraph(graph) {
        importing = true;
        try {
            importGraphInner(graph);
            boardNotes = graph && Array.isArray(graph.notes) ? JSON.parse(JSON.stringify(graph.notes)) : [];
            boardSettings = graph && graph.settings ? cleanSettings(graph.settings) : null;
        } finally {
            importing = false;
        }
        window.dispatchEvent(new CustomEvent("partnerly:notes-changed"));
    }

    function importGraphInner(graph) {
        editor.clear();
        if (!graph || !graph.nodes) {
            return;
        }
        var dfIds = {};
        // an error path in the graph means the card sends its errors there
        var withErrorPath = {};
        (graph.edges || []).forEach(function (edge) { if (edge.port === "error") { withErrorPath[edge.from] = true; } });
        graph.nodes.forEach(function (node) {
            if (withErrorPath[node.id] && node.kind === "action") {
                node.config = node.config || {};
                node.config.onError = "route";
            }
        });
        graph.nodes.forEach(function (node, index) {
            // 0 is a real saved coordinate: only a missing one gets a default place.
            var px = typeof node.x === "number" && isFinite(node.x) ? node.x : 80 + index * 40;
            var py = typeof node.y === "number" && isFinite(node.y) ? node.y : 80 + index * 30;
            dfIds[node.id] = addNode(node, px, py);
        });
        (graph.edges || []).forEach(function (edge) {
            var from = dfIds[edge.from];
            var to = dfIds[edge.to];
            if (!from || !to) {
                return;
            }
            var fromNode = graph.nodes.filter(function (item) { return item.id === edge.from; })[0] || {};
            var output = outputForPort(fromNode.type, edge.port, fromNode);
            try {
                editor.addConnection(from, to, output, "input_1");
            } catch (e) { }
        });
    }

    var importing = false;
    var savedFull = "";
    var savedLogic = "";

    function logicJson(graph) {
        return JSON.stringify({
            nodes: graph.nodes.map(function (n) { return { id: n.id, kind: n.kind, type: n.type, name: n.name, config: n.config, disabled: !!n.disabled }; }),
            edges: graph.edges.map(function (e) { return { from: e.from, to: e.to, port: e.port }; }),
            variables: graph.settings && graph.settings.variables ? graph.settings.variables : null
        });
    }

    // ---- session history (undo / redo / jump) ----------------------------
    var HISTORY_LIMIT = 100;
    var hist = { stack: [], index: -1 };
    var restoring = false;

    function nodeTitle(node) {
        return node.name || typeLabel(node.type, node.type);
    }

    // What changed between two graphs -> { label, key } or null when identical.
    function describeChange(prev, next) {
        var before = {};
        var after = {};
        prev.nodes.forEach(function (n) { before[n.id] = n; });
        next.nodes.forEach(function (n) { after[n.id] = n; });
        var added = next.nodes.filter(function (n) { return !before[n.id]; });
        var removed = prev.nodes.filter(function (n) { return !after[n.id]; });
        if (added.length || removed.length) {
            if (added.length) {
                return { label: t("historyAdded", "Added") + " " + nodeTitle(added[0]) + (added.length > 1 ? " +" + (added.length - 1) : "") };
            }
            return { label: t("historyRemoved", "Removed") + " " + nodeTitle(removed[0]) + (removed.length > 1 ? " +" + (removed.length - 1) : "") };
        }
        var edgeKey = function (e) { return e.from + ">" + e.to + ":" + (e.port || ""); };
        var prevEdges = prev.edges.map(edgeKey).sort().join("|");
        var nextEdges = next.edges.map(edgeKey).sort().join("|");
        if (prevEdges !== nextEdges) {
            return { label: next.edges.length >= prev.edges.length ? t("historyConnected", "Connected cards") : t("historyDisconnected", "Disconnected cards") };
        }
        for (var k = 0; k < next.nodes.length; k++) {
            var turned = next.nodes[k];
            if (!!before[turned.id].disabled !== !!turned.disabled) {
                return { label: (turned.disabled ? t("historyDisabled", "Turned off") : t("historyEnabled", "Turned on")) + " " + nodeTitle(turned) };
            }
        }
        for (var i = 0; i < next.nodes.length; i++) {
            var n = next.nodes[i];
            var o = before[n.id];
            if (JSON.stringify([o.name, o.config]) !== JSON.stringify([n.name, n.config])) {
                return { label: t("historyEdited", "Edited") + " " + nodeTitle(n), key: "edit:" + n.id };
            }
        }
        for (var j = 0; j < next.nodes.length; j++) {
            var m = next.nodes[j];
            var p = before[m.id];
            if (p.x !== m.x || p.y !== m.y) {
                return { label: t("historyMoved", "Moved") + " " + nodeTitle(m), key: "move:" + m.id };
            }
        }
        if (JSON.stringify(prev.notes || []) !== JSON.stringify(next.notes || [])) {
            return { label: t("historyNotes", "Edited a note"), key: "notes" };
        }
        if (JSON.stringify(prev.settings || null) !== JSON.stringify(next.settings || null)) {
            return { label: t("toolsVars", "Edited agent variables"), key: "settings" };
        }
        return null;
    }

    function recordHistory(graph) {
        if (importing || restoring) { return; }
        var json = JSON.stringify(graph);
        var top = hist.stack[hist.index];
        if (!top) {
            hist.stack = [{ json: json, label: t("historyOpened", "Opened"), time: Date.now() }];
            hist.index = 0;
            renderHistory();
            return;
        }
        if (top.json === json) { return; }
        var change = describeChange(JSON.parse(top.json), graph);
        if (!change) { return; }
        var now = Date.now();
        // Typing or dragging produces many tiny changes: merge them into one step.
        if (change.key && top.key === change.key && now - top.time < 1500 && hist.index === hist.stack.length - 1) {
            top.json = json;
            top.time = now;
        } else {
            hist.stack = hist.stack.slice(0, hist.index + 1);
            hist.stack.push({ json: json, label: change.label, key: change.key, time: now });
            if (hist.stack.length > HISTORY_LIMIT) { hist.stack.shift(); }
            hist.index = hist.stack.length - 1;
        }
        renderHistory();
    }

    function pad2(n) { return n < 10 ? "0" + n : String(n); }

    function renderHistory() {
        var list = q("[data-history-list]");
        var undo = q("[data-undo]");
        var redo = q("[data-redo]");
        var editable = canEditNow();
        if (undo) { undo.disabled = !editable || hist.index <= 0; }
        if (redo) { redo.disabled = !editable || hist.index >= hist.stack.length - 1; }
        if (!list) { return; }
        list.innerHTML = "";
        for (var i = hist.stack.length - 1; i >= 0; i--) {
            var entry = hist.stack[i];
            var d = new Date(entry.time);
            var row = document.createElement("button");
            row.type = "button";
            row.className = "automation-history-item" + (i === hist.index ? " is-current" : "") + (i > hist.index ? " is-future" : "");
            row.setAttribute("data-history-index", String(i));
            row.disabled = !editable;
            var time = document.createElement("span");
            time.className = "automation-history-time";
            time.textContent = pad2(d.getHours()) + ":" + pad2(d.getMinutes()) + ":" + pad2(d.getSeconds());
            var text = document.createElement("span");
            text.textContent = entry.label;
            row.appendChild(time);
            row.appendChild(text);
            list.appendChild(row);
        }
    }

    function restoreSnapshot(index) {
        var snap = hist.stack[index];
        if (!snap || !canEditNow()) { return; }
        hist.index = index;
        restoring = true;
        try {
            selectedId = null;
            markSelected(null);
            renderFields(null);
            hideProps();
            hideNodeMenu();
            importGraph(JSON.parse(snap.json));
            syncHidden();
        } finally {
            restoring = false;
        }
        renderHistory();
    }

    function undoStep() { if (hist.index > 0) { restoreSnapshot(hist.index - 1); } }
    function redoStep() { if (hist.index < hist.stack.length - 1) { restoreSnapshot(hist.index + 1); } }

    function syncHidden() {
        var input = q("#graphJson") || document.getElementById("graphJson");
        var graph = exportGraph();
        if (input) {
            input.value = JSON.stringify(graph);
        }
        recordHistory(graph);
        refreshChecks();
        window.dispatchEvent(new CustomEvent("partnerly:builder-changed"));
    }

    // ---- process check (used by the chat): which values a card still lacks ----
    var BUILTIN_REQUIRED = {
        schedule: function (c) {
            var kind = c.scheduleKind || "daily";
            return kind === "once" ? ["at"] : (kind === "cron" ? ["expression"] : ["time"]);
        },
        interval: function () { return ["minutes"]; },
        event: function () { return ["event"]; },
        "entity.created": function () { return ["entity"]; },
        "entity.updated": function () { return ["entity"]; },
        "entity.deleted": function () { return ["entity"]; },
        delay: function () { return ["amount"]; },
        set_variable: function () { return ["name"]; },
        send_email: function (c) { return ["to"].concat(filled(c.template) ? [] : ["subject", "body"]); },
        send_notification: function () { return ["message"]; },
        http_request: function () { return ["url"]; },
        webhook_action: function () { return ["url"]; },
        execute_workflow: function () { return ["automationId"]; },
        loop: function () { return ["items"]; },
        store_set: function () { return ["key"]; },
        store_get: function () { return ["key"]; },
        store_delete: function () { return ["key"]; },
        once: function () { return ["key"]; },
        filter: function () { return ["condition.left"]; },
        branch: function () { return ["condition.left"]; },
        wait_until: function () { return ["condition.left"]; },
        "switch": function () { return ["value"]; },
        create_record: function () { return ["entity", "fields"]; },
        update_record: function () { return ["entity", "key", "fields"]; },
        find_record: function () { return ["entity"]; },
        delete_record: function () { return ["entity", "key"]; }
    };

    function fieldLabelFor(data, key) {
        if (key === "condition.left") { return t("conditionLeft", "Condition"); }
        if (key === "connectionId") { return t("intConnection", "Connection"); }
        var schema = [];
        try { schema = schemaFor(data) || []; } catch (e) { schema = []; }
        for (var i = 0; i < schema.length; i++) {
            if (schema[i].key === key) { return schema[i].label || key; }
        }
        return key;
    }

    function missingKeysFor(data) {
        var config = normalizeDelay(JSON.parse(JSON.stringify(data.config || {})));
        var keys;
        if (INTEGRATION_PREFIX.test(data.type)) {
            keys = ["connectionId"].concat(INTEGRATION_REQUIRED[data.type] || []);
        } else {
            var rule = BUILTIN_REQUIRED[data.type];
            keys = rule ? rule(config) : [];
        }
        return keys.filter(function (key) {
            var value = getByPath(config, key);
            if (key === "amount" || key === "minutes") { return !(Number(value) > 0); }
            if (key === "chatId" && /^telegram\./.test(data.type) && !filled(value)) {
                // An empty chat is fine when the connection has a default chat (the server uses it).
                telegramChatOptions({ config: config });
                var entry = telegramChats[config.connectionId];
                if (entry && entry.defaultChatId) { return false; }
            }
            return !filled(value);
        });
    }

    function missingFor(data) {
        return missingKeysFor(data).map(function (key) { return fieldLabelFor(data, key); });
    }

    // ---- readable settings of a card (used by the agent chat) ----------------
    var SECRET_KEY = /secret|token|password|authorization|api[_-]?key/i;

    function shortText(value, max) {
        var text = String(value).replace(/\s+/g, " ").trim();
        return text.length > max ? text.slice(0, max - 1) + "…" : text;
    }

    function displayValue(field, value) {
        if (SECRET_KEY.test(field.key || "")) { return "••••••"; }
        if (field.type === "select") {
            var options = typeof field.options === "function" ? field.options() : (field.options || []);
            for (var i = 0; i < options.length; i++) {
                if (String(options[i].value) === String(value)) { return options[i].label || String(value); }
            }
        }
        if (field.type === "connection") {
            var found = catalogConnections.filter(function (c) { return String(c.id) === String(value); })[0];
            return found ? found.name : String(value);
        }
        if (value && typeof value === "object") {
            if (Array.isArray(value)) { return shortText(value.join(", "), 110); }
            return shortText(Object.keys(value).map(function (k) {
                return k + " = " + (SECRET_KEY.test(k) ? "••••••" : (value[k] && typeof value[k] === "object" ? JSON.stringify(value[k]) : value[k]));
            }).join("; "), 110);
        }
        if (typeof value === "boolean") { return value ? "✓" : "✗"; }
        return shortText(value, 110);
    }

    function settingsList(data) {
        var config = normalizeDelay(JSON.parse(JSON.stringify(data.config || {})));
        var schema = [];
        try { schema = schemaFor(data) || []; } catch (e) { schema = []; }
        var out = [];
        schema.forEach(function (field) {
            if (!field.key || field.type === "note" || field.type === "connection") { return; }
            if (!fieldVisible(field, config)) { return; }
            var value = getByPath(config, field.key);
            if (!filled(value) && typeof value !== "boolean") { return; }
            out.push({ label: field.label || field.key, value: displayValue(field, value) });
        });
        return out;
    }

    function connectionName(data) {
        var id = data && data.config ? data.config.connectionId : null;
        if (!filled(id)) { return ""; }
        var found = catalogConnections.filter(function (c) { return String(c.id) === String(id); })[0];
        return found ? found.name : "";
    }

    window.PartnerlyBuilder = {
        analyze: function () {
            persistSelected();
            var exported = editor.export();
            var home = exported.drawflow && exported.drawflow.Home && exported.drawflow.Home.data ? exported.drawflow.Home.data : {};
            var keys = Object.keys(home);
            var byId = {};
            keys.forEach(function (key) { byId[String(home[key].id)] = home[key]; });

            function nodeLabel(node) {
                var d = node.data || {};
                return d.name || typeLabel(d.type, d.type);
            }

            // links: who leads to whom (and through which exit)
            var incoming = {};
            var outgoing = {};
            keys.forEach(function (key) {
                var node = home[key];
                outgoing[String(node.id)] = [];
                Object.keys(node.outputs || {}).forEach(function (outKey) {
                    ((node.outputs[outKey].connections) || []).forEach(function (link) {
                        var target = String(link.node);
                        if (!byId[target]) { return; }
                        outgoing[String(node.id)].push({ id: target, port: portName((node.data || {}).type, outKey) });
                        (incoming[target] = incoming[target] || []).push(String(node.id));
                    });
                });
            });

            // separate chains (groups of cards joined to each other)
            var group = {};
            function find(a) { while (group[a] !== a) { group[a] = group[group[a]]; a = group[a]; } return a; }
            keys.forEach(function (key) { group[String(home[key].id)] = String(home[key].id); });
            Object.keys(outgoing).forEach(function (from) {
                outgoing[from].forEach(function (link) {
                    var a = find(from), b = find(link.id);
                    if (a !== b) { group[a] = b; }
                });
            });

            var cards = keys.map(function (key) {
                var node = home[key];
                var data = node.data || {};
                var id = String(node.id);
                var wired = data.kind === "trigger" ? connectionCount(node.outputs) > 0 : connectionCount(node.inputs) > 0;
                var missing = data.disabled ? [] : missingFor(data);
                return {
                    domId: node.id,
                    kind: data.kind,
                    type: data.type,
                    typeLabel: typeLabel(data.type, data.type),
                    name: data.name || "",
                    disabled: !!data.disabled,
                    wired: wired,
                    missing: missing,
                    settings: settingsList(data),
                    connection: connectionName(data),
                    from: (incoming[id] || []).map(function (fromId) { return nodeLabel(byId[fromId]); }),
                    fromIds: incoming[id] || [],
                    to: outgoing[id].map(function (link) { return { id: link.id, name: nodeLabel(byId[link.id]), port: link.port }; }),
                    chainKey: find(id),
                    x: node.pos_x,
                    y: node.pos_y
                };
            });
            cards.sort(function (a, b) {
                if (a.kind !== b.kind) { return a.kind === "trigger" ? -1 : 1; }
                return a.x - b.x || a.y - b.y;
            });
            var chainKeys = [];
            cards.forEach(function (card) {
                if (chainKeys.indexOf(card.chainKey) === -1) { chainKeys.push(card.chainKey); }
                card.chain = chainKeys.indexOf(card.chainKey) + 1;
            });
            return { cards: cards, chains: chainKeys.length };
        },
        markSaved: function () {
            var graph = exportGraph();
            savedFull = JSON.stringify(graph);
            savedLogic = logicJson(graph);
        },
        isDirty: function () { return JSON.stringify(exportGraph()) !== savedFull; },
        isLogicDirty: function () { return logicJson(exportGraph()) !== savedLogic; },
        stats: function () {
            var graph = exportGraph();
            return {
                hasTrigger: graph.nodes.some(function (n) { return n.kind === "trigger"; }),
                hasAction: graph.nodes.some(function (n) { return n.kind === "action"; })
            };
        }
    };

    function getByPath(object, path) {
        if (!path) {
            return object;
        }
        var parts = path.split(".");
        var current = object;
        for (var i = 0; i < parts.length; i++) {
            if (current == null) {
                return undefined;
            }
            current = current[parts[i]];
        }
        return current;
    }

    function setByPath(object, path, value) {
        var parts = path.split(".");
        var current = object;
        for (var i = 0; i < parts.length - 1; i++) {
            if (!current[parts[i]] || typeof current[parts[i]] !== "object") {
                current[parts[i]] = {};
            }
            current = current[parts[i]];
        }
        var last = parts[parts.length - 1];
        if (value === "" || value == null) {
            delete current[last];
        } else {
            current[last] = value;
        }
    }

    function pairsToText(value) {
        if (!value) {
            return "";
        }
        if (Array.isArray(value)) {
            return value.map(function (item) {
                return (item.field || "") + "=" + (item.value || "");
            }).join("\n");
        }
        return Object.keys(value).map(function (key) {
            return key + "=" + value[key];
        }).join("\n");
    }

    function textToPairs(text) {
        var result = {};
        String(text || "").split(/\r?\n/).forEach(function (line) {
            var trimmed = line.trim();
            if (!trimmed) {
                return;
            }
            var index = trimmed.indexOf("=");
            if (index <= 0) {
                return;
            }
            result[trimmed.slice(0, index).trim()] = trimmed.slice(index + 1).trim();
        });
        return result;
    }

    function normalizeDelay(config) {
        if (!config) {
            return { amount: 5, unit: "minutes" };
        }
        if (config.amount != null && config.amount !== "") {
            return config;
        }
        var legacy = config.delay || config.startAfter;
        if (legacy == null || legacy === "") {
            return config;
        }
        var raw = String(legacy);
        var match = raw.match(/^(\d+):(\d+):(\d+)$/);
        if (match) {
            var total = (+match[1] * 3600) + (+match[2] * 60) + (+match[3]);
            if (total % 86400 === 0 && total > 0) {
                config.amount = total / 86400;
                config.unit = "days";
            } else if (total % 3600 === 0 && total > 0) {
                config.amount = total / 3600;
                config.unit = "hours";
            } else if (total % 60 === 0 && total > 0) {
                config.amount = total / 60;
                config.unit = "minutes";
            } else {
                config.amount = total;
                config.unit = "seconds";
            }
            return config;
        }
        if (/^\d+(\.\d+)?$/.test(raw)) {
            config.amount = raw;
            config.unit = config.unit || "seconds";
        }
        return config;
    }

    // ---- values offered in dropdowns --------------------------------------
    var EVENT_VARS = {
        UserRegistered: ["id", "email", "firstName", "lastName"],
        SupportTicketCreated: ["id", "ticketId", "subject", "status", "type"],
        SupportTicketStatusChanged: ["id", "ticketId", "status"],
        UserLoggedIn: ["id", "email", "firstName", "lastName"],
        UserLoginFailed: ["id", "email", "firstName", "lastName", "attempts", "lockedOut"],
        UserLockedOut: ["id", "email", "firstName", "lastName", "lockedUntil"],
        UserEmailConfirmed: ["id", "email", "firstName", "lastName"],
        PasswordResetRequested: ["id", "email", "firstName", "lastName"],
        PasswordChanged: ["id", "email", "firstName", "lastName"],
        UserDeleted: ["id", "email", "firstName", "lastName"],
        SupportTicketAssigned: ["id", "ticketId", "subject", "status", "assignedTo", "previousAssignedTo"],
        SupportTicketDeleted: ["id", "ticketId", "subject", "status"]
    };

    var TRIGGER_VARS = {
        "telegram.message_received": ["messageId", "chatId", "chatType", "text", "fromId", "fromUsername", "fromName", "date"],
        "gmail.message_received": ["id", "threadId", "from", "to", "subject", "date", "snippet"],
        "outlook.message_received": ["id", "conversationId", "from", "fromName", "to", "subject", "date", "snippet", "webLink"],
        "outlook.event_added": ["id", "subject", "start", "end", "location", "organizer", "webLink", "created"],
        "sheets.row_added": ["rowNumber", "sheet", "spreadsheetId"],
        "drive.file_added": ["id", "name", "mimeType", "webViewLink", "createdTime", "modifiedTime", "size"],
        "whatsapp.message_received": ["messageId", "from", "name", "text"],
        "computer.file_created": ["path", "fileName", "folder", "extension", "size", "machine"],
        "computer.status_changed": ["state", "machine"],
        "computer.program_started": ["name", "machine"],
        "computer.program_stopped": ["name", "machine"],
        "computer.file_changed": ["path", "fileName", "folder", "extension", "size", "machine"],
        "computer.file_deleted": ["path", "fileName", "folder", "extension", "machine"],
        "computer.session_changed": ["state", "machine"],
        "computer.clipboard_changed": ["text", "length", "machine"],
        "computer.power_changed": ["state", "percent", "machine"],
        "computer.drive_changed": ["state", "drive", "label", "kind", "machine"],
        "computer.idle_changed": ["state", "idleMinutes", "machine"],
        "computer.disk_low": ["drive", "freeGb", "freePercent", "machine"]
    };

    var STEP_VARS = {
        store_get: ["result.value", "result.found"],
        once: ["result.firstTime"],
        find_record: function (cfg) { return entityFieldNames(cfg.entity).map(function (n) { return "result.result." + n; }); },
        create_record: function (cfg) { return entityFieldNames(cfg.entity).map(function (n) { return "result." + n; }); },
        "telegram.send_message": ["result.messageId", "result.chatId"],
        "telegram.send_document": ["result.messageId", "result.chatId", "result.fileName"],
        "sheets.append_row": ["result.spreadsheetId", "result.range"],
        "sheets.update_row": ["result.spreadsheetId", "result.rowNumber", "result.range"],
        "sheets.find_rows": ["result.count", "result.first.rowNumber"],
        "gmail.send": ["result.messageId", "result.threadId", "result.to"],
        "gmail.find_messages": ["result.count", "result.first.id", "result.first.from", "result.first.subject", "result.first.snippet"],
        "outlook.send": ["result.sent", "result.to"],
        "outlook.find_messages": ["result.count", "result.first.id", "result.first.from", "result.first.subject", "result.first.snippet"],
        "outlook.create_event": ["result.id", "result.subject", "result.webLink"],
        "outlook.find_events": ["result.count", "result.first.id", "result.first.subject", "result.first.start", "result.first.end"],
        "gmail.reply": ["result.messageId", "result.threadId", "result.to"],
        "gmail.get_message": ["result.id", "result.threadId", "result.from", "result.to", "result.subject", "result.date", "result.body", "result.snippet", "result.attachmentCount"],
        "gmail.create_draft": ["result.draftId", "result.messageId", "result.to"],
        "gmail.mark_read": ["result.messageId", "result.state"],
        "gmail.archive": ["result.messageId"],
        "gmail.trash": ["result.messageId"],
        "gmail.label": ["result.messageId", "result.label"],
        "sheets.get_row": ["result.found", "result.rowNumber", "result.values"],
        "sheets.read_range": ["result.count", "result.first", "result.values"],
        "sheets.update_cell": ["result.spreadsheetId", "result.range"],
        "sheets.clear_range": ["result.spreadsheetId", "result.range"],
        "sheets.delete_row": ["result.spreadsheetId", "result.rowNumber"],
        "sheets.create_spreadsheet": ["result.spreadsheetId", "result.title", "result.url"],
        "sheets.add_sheet": ["result.spreadsheetId", "result.sheetId", "result.title"],
        "drive.get_file": ["result.id", "result.name", "result.mimeType", "result.webViewLink", "result.size", "result.modifiedTime"],
        "drive.copy_file": ["result.id", "result.name", "result.webViewLink"],
        "drive.move_file": ["result.id", "result.name", "result.webViewLink"],
        "drive.rename_file": ["result.id", "result.name", "result.webViewLink"],
        "drive.trash_file": ["result.id", "result.name"],
        "drive.share_file": ["result.fileId", "result.email", "result.role", "result.webViewLink"],
        "drive.create_text_file": ["result.id", "result.name", "result.webViewLink"],
        "drive.upload_file": ["result.id", "result.name", "result.webViewLink"],
        "drive.create_folder": ["result.id", "result.name", "result.webViewLink"],
        "drive.find_files": ["result.count", "result.first.id", "result.first.name", "result.first.webViewLink"],
        "whatsapp.send_text": ["result.messageId"],
        "whatsapp.send_template": ["result.messageId"],
        "computer.clipboard_get": ["result.text"],
        "computer.system_info": ["result.machine", "result.os", "result.cpuCount", "result.memoryGb", "result.uptimeMinutes", "result.onBattery", "result.batteryPercent", "result.drives"],
        "computer.screenshot": ["result.attachmentId", "result.fileName", "result.size"],
        "computer.get_file": ["result.attachmentId", "result.fileName", "result.size"],
        "computer.process_running": ["result.running", "result.count"],
        "computer.read_file": ["result.text", "result.size", "result.fileName"],
        "computer.write_file": ["result.path", "result.size"],
        "computer.list_folder": ["result.count", "result.items"],
        "computer.copy_file": ["result.path"],
        "computer.move_file": ["result.path"],
        "computer.zip_folder": ["result.path", "result.size"],
        "computer.save_attachment": ["result.path", "result.size"],
        "computer.message_box": ["result.button", "result.confirmed"],
        "computer.run_program": ["result.exitCode", "result.output", "result.errors", "result.processId"],
        "computer.run_script": ["result.exitCode", "result.output", "result.errors", "result.processId"],
        "computer.close_program": ["result.closed"],
        "computer.create_folder": ["result.path", "result.existed"],
        "computer.delete_file": ["result.deleted", "result.path"],
        "computer.file_info": ["result.exists", "result.isFolder", "result.size", "result.modified", "result.created", "result.extension"],
        "computer.unzip": ["result.path", "result.files"],
        "computer.list_programs": ["result.count", "result.items"],
        "computer.active_window": ["result.title", "result.program"],
        "computer.network_info": ["result.hostname", "result.online", "result.addresses"],
        "computer.ping_host": ["result.reachable", "result.roundTripMs", "result.status"],
        "computer.download_url": ["result.path", "result.size"],
        "computer.shutdown": ["result.mode", "result.delaySeconds"]
    };

    function variable(path, group) {
        return { value: "{{" + path + "}}", label: path, group: group };
    }

    function entityFieldNames(key, only) {
        var entity = (lookups.entities || []).filter(function (item) { return item.key === key; })[0];
        if (!entity) { return []; }
        return entity.fields.filter(function (field) {
            return only ? field[only] : true;
        }).map(function (field) { return field.name; });
    }

    function nodeOrNull(id) {
        try { return editor.getNodeFromId(id); } catch (e) { return null; }
    }

    function upstreamIds(id) {
        var seen = {};
        var order = [];
        var queue = [String(id)];
        while (queue.length) {
            var node = nodeOrNull(queue.shift());
            if (!node) { continue; }
            Object.keys(node.inputs || {}).forEach(function (key) {
                (node.inputs[key].connections || []).forEach(function (link) {
                    var from = String(link.node);
                    if (!seen[from]) {
                        seen[from] = true;
                        order.push(from);
                        queue.push(from);
                    }
                });
            });
        }
        return order;
    }

    function nodeVariables(node) {
        var data = node.data || {};
        var cfg = data.config || {};
        var list = [];
        var groupTrigger = t("builderVarsTrigger", "Trigger data");
        var groupSteps = t("builderVarsSteps", "Previous steps");
        if (data.kind === "trigger") {
            var add = function (paths) {
                paths.forEach(function (p) { list.push(variable("trigger." + p, groupTrigger)); });
            };
            if (/^entity\./.test(data.type)) {
                add(["id", "entity"]);
                add(entityFieldNames(cfg.entity).map(function (n) { return "after." + n; }));
                if (data.type !== "entity.created") {
                    add(entityFieldNames(cfg.entity).map(function (n) { return "before." + n; }));
                }
            } else if (data.type === "event") {
                add(EVENT_VARS[cfg.event] || ["id"]);
            } else if (TRIGGER_VARS[data.type]) {
                add(TRIGGER_VARS[data.type]);
            }
            return list;
        }
        var name = data.name || "";
        if (data.type === "loop" && name) {
            list.push(variable("steps." + name + ".result.count", groupSteps));
        }
        if (cfg.onError === "route" || cfg.onError === "continue") {
            list.push(variable("steps." + name + ".error", groupSteps));
        }
        if (data.type === "set_variable" && cfg.name) {
            list.push(variable("vars." + cfg.name, groupSteps));
        }
        var spec = STEP_VARS[data.type];
        if (spec && name) {
            (typeof spec === "function" ? spec(cfg) : spec).forEach(function (p) {
                list.push(variable("steps." + name + "." + p, groupSteps));
            });
        }
        return list;
    }

    // Everything the selected card can reference: its triggers, the steps
    // before it and run info. An unwired card sees the whole flow.
    function variablesFor(id) {
        var ids = upstreamIds(id);
        if (!ids.length) {
            var home = (editor.export().drawflow || {}).Home || {};
            ids = Object.keys(home.data || {}).filter(function (key) { return String(key) !== String(id); });
        }
        var triggers = [];
        var steps = [];
        ids.forEach(function (key) {
            var node = nodeOrNull(key);
            if (!node) { return; }
            var target = node.data && node.data.kind === "trigger" ? triggers : steps;
            nodeVariables(node).forEach(function (item) { target.push(item); });
        });
        var run = t("builderVarsRun", "Run info");
        var extra = [];
        var insideLoop = ids.some(function (key) {
            var node = nodeOrNull(key);
            return node && node.data && node.data.type === "loop";
        });
        if (insideLoop) {
            var loopGroup = t("builderVarsLoop", "Current item");
            ["item", "number", "index", "count"].forEach(function (part) { extra.push(variable("loop." + part, loopGroup)); });
        }
        var constants = boardSettings && boardSettings.variables ? Object.keys(boardSettings.variables) : [];
        constants.forEach(function (name) { extra.push(variable("vars." + name, t("builderVarsAgent", "Agent variables"))); });
        return triggers.concat(steps, extra, [variable("execution.runId", run), variable("execution.startedAt", run)]);
    }

    var EVENT_OPTIONS = Object.keys(EVENT_VARS).map(function (key) { return { value: key, label: key }; });
    var LINK_OPTIONS = [
        { value: "/Dashboard", label: "/Dashboard" },
        { value: "/SupportTickets", label: "/SupportTickets" },
        { value: "/Automation", label: "/Automation" }
    ];

    function userOptions(valueKey) {
        var group = t("builderUsers", "Users");
        return (lookups.users || []).map(function (user) {
            var label = user.name ? user.name + " (" + user.email + ")" : user.email;
            return { value: valueKey === "id" ? user.id : user.email, label: label, group: group };
        });
    }

    // Chats of the chosen Telegram connection (people who wrote to the bot + its default
    // chat). Loaded once per connection, then the open card is drawn again.
    var telegramChats = {};

    function telegramChatOptions(data) {
        var id = data && data.config ? data.config.connectionId : "";
        if (!id) { return []; }
        var group = t("builderTelegramChats", "Chats of this bot");
        var entry = telegramChats[id];
        if (entry === undefined && config.telegramChatsUrl) {
            telegramChats[id] = { loading: true, chats: [] };
            fetch(config.telegramChatsUrl + "?id=" + encodeURIComponent(id), {
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" }
            }).then(function (response) { return response.json(); }).then(function (json) {
                telegramChats[id] = { chats: json && json.success ? (json.chats || []) : [], defaultChatId: json ? json.defaultChatId : "" };
            }).catch(function () {
                telegramChats[id] = { chats: [] };
            }).then(function () {
                var current = selectedId ? nodeOrNull(selectedId) : null;
                if (current && current.data.config && current.data.config.connectionId === id) {
                    renderFields(current.data);
                }
                refreshChecks();
            });
            return [];
        }
        if (!entry) { return []; }
        var options = [];
        if (entry.defaultChatId) {
            options.push({
                value: String(entry.defaultChatId),
                label: t("builderDefaultChat", "Default chat of the connection") + " · " + entry.defaultChatId,
                group: group
            });
        }
        (entry.chats || []).forEach(function (chat) {
            if (!chat.chatId || String(chat.chatId) === String(entry.defaultChatId)) { return; }
            var who = [chat.name, chat.username ? "@" + chat.username : ""].filter(Boolean).join(" ") || chat.type || "";
            options.push({ value: String(chat.chatId), label: (who ? who + " · " : "") + chat.chatId, group: group });
        });
        return options;
    }

    // Folders, programs and script folders the chosen computer allows: the program on the PC tells
    // the site, the editor offers them in a dropdown (a hint; the PC still decides when the card runs).
    var computerInfo = {};

    function computerOptions(data, kind) {
        var id = data && data.config ? data.config.connectionId : "";
        if (!id) { return []; }
        var entry = computerInfo[id];
        if (entry === undefined && config.computerSettingsUrl) {
            computerInfo[id] = { loading: true };
            fetch(config.computerSettingsUrl + "?id=" + encodeURIComponent(id), {
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest", "Accept": "application/json" }
            }).then(function (response) { return response.json(); }).then(function (json) {
                computerInfo[id] = json && json.success ? json : {};
            }).catch(function () {
                computerInfo[id] = {};
            }).then(function () {
                var current = selectedId ? nodeOrNull(selectedId) : null;
                if (current && current.data.config && current.data.config.connectionId === id) {
                    renderFields(current.data);
                }
            });
            return [];
        }
        if (!entry || !entry[kind]) { return []; }
        var group = kind === "programs"
            ? t("builderPcAllowedPrograms", "Programs this PC allows")
            : t("builderPcAllowedFolders", "Folders this PC allows");
        return entry[kind].map(function (path) { return { value: String(path), label: String(path), group: group }; });
    }

    function sourceOptions(source, vars, data) {
        switch (source) {
            case "computerFolders": return computerOptions(data, "folders").concat(vars);
            case "computerPrograms": return computerOptions(data, "programs").concat(vars);
            case "computerScripts": return computerOptions(data, "scripts").concat(vars);
            case "telegramChats": return telegramChatOptions(data).concat(vars);
            case "events": return EVENT_OPTIONS;
            case "userEmails": return vars.concat(userOptions("email"));
            case "userIds": return vars.concat(userOptions("id"));
            case "templates":
                return (lookups.emailTemplates || []).map(function (name) { return { value: name, label: name }; });
            case "automations":
                return (lookups.automations || [])
                    .filter(function (item) { return String(item.id) !== String(config.id); })
                    .map(function (item) { return { value: item.id, label: item.name }; });
            case "httpConnections":
                return (lookups.httpConnections || []).map(function (item) { return { value: item.id, label: item.name }; });
            case "notifTypes":
                return [{ value: "System", label: "System" }, { value: "Support", label: "Support" }];
            case "links": return vars.concat(LINK_OPTIONS);
            default: return vars;
        }
    }

    function fieldVisible(field, config) {
        if (!field.showIf) {
            return true;
        }
        var key = Object.keys(field.showIf)[0];
        var allowed = field.showIf[key];
        var current = String(getByPath(config, key) || "");
        return allowed.indexOf(current) !== -1;
    }

    function renderFields(data) {
        var host = q("[data-prop-fields]");
        var empty = q("[data-prop-empty]");
        var form = q("[data-prop-form]");
        var nameInput = q("[data-prop-name]");
        if (!host) {
            return;
        }
        host.innerHTML = "";
        if (!data) {
            if (empty) empty.hidden = false;
            if (form) form.hidden = true;
            if (nameInput) nameInput.value = "";
            updateDone();
            return;
        }
        if (empty) empty.hidden = true;
        if (form) form.hidden = false;
        if (nameInput) nameInput.value = data.name || "";
        var enabledBox = q("[data-prop-enabled]");
        if (enabledBox) { enabledBox.checked = !data.disabled; enabledBox.disabled = !canEditNow(); }
        data.config = normalizeDelay(data.config || {});
        var actionVars = data.kind === "action" ? variablesFor(selectedId) : [];
        var schema = schemaFor(data);
        schema.forEach(function (field) {
            if (!fieldVisible(field, data.config)) {
                return;
            }
            if (field.type === "note") {
                var note = document.createElement("p");
                note.className = "automation-hint";
                note.textContent = field.help || "";
                host.appendChild(note);
                return;
            }
            var wrap = document.createElement("div");
            wrap.className = "mb-3";
            wrap.setAttribute("data-field-wrap", field.key);
            var label = document.createElement("label");
            label.className = "form-label";
            label.textContent = field.label || field.key;
            wrap.appendChild(label);
            var control;
            var built = null;
            var insertHelper = null;
            var value = getByPath(data.config, field.key);
            if (field.type === "connection") {
                control = document.createElement("select");
                control.className = "form-select";
                var matching = catalogConnections.filter(function (item) { return item.kind === field.kind; });
                var placeholder = document.createElement("option");
                placeholder.value = "";
                placeholder.textContent = matching.length ? t("chooseConnection", "Choose…") : t("noConnections", "No connections yet");
                control.appendChild(placeholder);
                matching.forEach(function (item) {
                    var option = document.createElement("option");
                    option.value = item.id;
                    option.textContent = item.name;
                    if (String(value || "") === String(item.id)) {
                        option.selected = true;
                    }
                    control.appendChild(option);
                });
                if (integrationsUrl) {
                    var manage = document.createElement("a");
                    manage.className = "automation-docs-link d-inline-block mt-1";
                    manage.href = integrationsUrl;
                    manage.target = "_blank";
                    manage.rel = "noopener";
                    manage.textContent = t("manageConnections", "Manage connections");
                    window.setTimeout(function () { wrap.appendChild(manage); }, 0);
                }
            } else if (field.type === "pairs") {
                var entityKey = getByPath(data.config, "entity");
                var mode = field.mode || "fields";
                var names = entityFieldNames(entityKey, mode === "update" ? "updatable" : (mode === "filter" || mode === "key" ? "filterable" : null));
                if (mode === "key") { names = names.filter(function (n) { return n === "id" || n === "email"; }).concat(names.filter(function (n) { return n !== "id" && n !== "email"; })); }
                built = ui.pairs({
                    key: field.key,
                    value: value,
                    fieldOptions: names.map(function (n) { return { value: n, label: n }; }),
                    valueOptions: actionVars
                });
                control = built.control;
            } else if (field.source && ui) {
                built = ui.combo({
                    key: field.key,
                    value: value,
                    strict: !!field.strict,
                    options: sourceOptions(field.source, actionVars, data)
                });
                control = built.control;
            } else if (field.type === "text" && ui && data.kind === "action" && !field.plain && actionVars.length) {
                built = ui.combo({ key: field.key, value: value, options: actionVars });
                control = built.control;
            } else if (field.type === "select") {
                control = document.createElement("select");
                control.className = "form-select";
                (typeof field.options === "function" ? field.options() : (field.options || [])).forEach(function (option) {
                    var item = document.createElement("option");
                    item.value = option.value;
                    item.textContent = option.label;
                    if (String(value || "") === String(option.value)) {
                        item.selected = true;
                    }
                    control.appendChild(item);
                });
            } else if (field.type === "textarea") {
                control = document.createElement("textarea");
                control.className = "form-control";
                control.rows = 4;
                control.value = value || "";
                if (ui && data.kind === "action" && actionVars.length) {
                    insertHelper = ui.insertVariable(control, actionVars);
                }
            } else {
                control = document.createElement("input");
                control.className = "form-control";
                control.type = field.type === "number" ? "number" : field.type === "time" ? "time" : field.type === "datetime" ? "datetime-local" : "text";
                if (field.min != null) {
                    control.min = field.min;
                }
                if (field.type === "datetime" && value) {
                    control.value = String(value).replace("Z", "").slice(0, 16);
                } else {
                    control.value = value == null ? "" : value;
                }
            }
            if (built) {
                wrap.appendChild(built.el);
            } else {
                control.setAttribute("data-config-key", field.key);
                wrap.appendChild(control);
                if (insertHelper) { wrap.appendChild(insertHelper); }
            }
            if (field.help) {
                var hint = document.createElement("div");
                hint.className = "form-text";
                hint.textContent = field.help;
                wrap.appendChild(hint);
            }
            host.appendChild(wrap);
        });
        appendDocs(host, data);
        appendTestBox(host, data);
        updateDone();
    }

    // "Test this card": runs it in test mode with the values of the last run.
    function appendTestBox(host, data) {
        if (!data || data.kind !== "action") { return; }
        var form = document.getElementById("automationBuilderForm");
        var url = form ? form.getAttribute("data-test-node-url") : "";
        if (!url) { return; }
        var box = document.createElement("div");
        box.className = "automation-testcard";
        var button = document.createElement("button");
        button.type = "button";
        button.className = "btn btn-outline-primary btn-sm";
        button.innerHTML = '<i class="bx bx-test-tube" aria-hidden="true"></i> ';
        button.appendChild(document.createTextNode(t("testCard", "Test this card")));
        var note = document.createElement("div");
        note.className = "form-text";
        note.textContent = t("testNote", "");
        var out = document.createElement("div");
        out.className = "automation-testcard-out";
        out.hidden = true;
        box.appendChild(button);
        box.appendChild(note);
        box.appendChild(out);
        host.appendChild(box);
        button.disabled = !canEditNow() && false;
        button.addEventListener("click", function () {
            var id = selectedId;
            var node = id ? nodeOrNull(id) : null;
            if (!node) { return; }
            persistSelected();
            var d = node.data;
            var payload = JSON.stringify({ id: d.id, kind: d.kind, type: d.type, name: d.name, config: d.config || {} });
            button.disabled = true;
            out.hidden = false;
            out.textContent = t("testRunning", "Testing...");
            out.className = "automation-testcard-out";
            postForm(url, { nodeJson: payload }).then(function (result) {
                out.textContent = "";
                out.classList.toggle("is-failed", result.status === "failed");
                var head = document.createElement("strong");
                head.textContent = t("testResult", "Result") + ": " + result.status + (result.port ? " (" + result.port + ")" : "");
                out.appendChild(head);
                [
                    [result.error ? "Error" : null, result.error],
                    [result.output != null ? t("testResult", "Result") : null, result.output != null ? JSON.stringify(result.output, null, 2) : null],
                    [result.config ? t("testSettings", "Settings with values filled in") : null, result.config ? JSON.stringify(result.config, null, 2) : null]
                ].forEach(function (part) {
                    if (!part[0] || part[1] == null) { return; }
                    var label = document.createElement("div");
                    label.className = "automation-testcard-label";
                    label.textContent = part[0];
                    var pre = document.createElement("pre");
                    pre.textContent = part[1];
                    out.appendChild(label);
                    out.appendChild(pre);
                });
            }).catch(function (err) {
                out.textContent = (err && err.message) || t("testFailed", "Could not test the card.");
                out.classList.add("is-failed");
            }).then(function () { button.disabled = false; });
        });
    }

    function docsKey(data) {
        if (!data) {
            return "";
        }
        if (data.type === "delay" && data.kind === "trigger") {
            return "delay_trigger";
        }
        if (data.type === "webhook" && data.kind === "action") {
            return "webhook_action";
        }
        return data.type;
    }

    function appendDocs(host, data) {
        var docs = i18n.docs || {};
        var key = docsKey(data);
        var text = docs[key] || "";
        if (text) {
            var box = document.createElement("p");
            box.className = "automation-docs";
            box.textContent = text;
            host.appendChild(box);
        }
        if (data && data.kind === "action" && i18n.functionsHint) {
            var tip = document.createElement("p");
            tip.className = "automation-docs";
            tip.textContent = i18n.functionsHint;
            host.appendChild(tip);
        }
        var url = i18n.docsUrl;
        if (url) {
            var link = document.createElement("a");
            link.className = "automation-docs-link";
            link.href = url + (key ? "#docs-" + encodeURIComponent(key) : "");
            link.textContent = t("docsLink", "How to fill these fields");
            host.appendChild(link);
        }
    }

    function readFieldsInto(data) {
        var host = q("[data-prop-fields]");
        if (!host || !data) {
            return;
        }
        data.config = data.config || {};
        var controls = host.querySelectorAll("[data-config-key]");
        Array.prototype.forEach.call(controls, function (control) {
            var key = control.getAttribute("data-config-key");
            var schema = schemaFor(data).filter(function (item) { return item.key === key; })[0];
            var value = control.value;
            if (schema && schema.type === "pairs") {
                setByPath(data.config, key, textToPairs(value));
                return;
            }
            if (schema && schema.type === "number") {
                if (value === "") {
                    setByPath(data.config, key, "");
                } else {
                    setByPath(data.config, key, Number(value));
                }
                return;
            }
            if (schema && schema.type === "datetime" && value) {
                setByPath(data.config, key, value.length === 16 ? value + ":00Z" : value);
                return;
            }
            setByPath(data.config, key, value);
        });
        if (data.type === "delay") {
            delete data.config.delay;
            delete data.config.startAfter;
        }
    }

    function persistSelected(event) {
        if (!selectedId) {
            return;
        }
        var node = editor.getNodeFromId(selectedId);
        var nameInput = q("[data-prop-name]");
        if (nameInput) {
            node.data.name = nameInput.value;
        }
        var previousEntity = node.data.config && node.data.config.entity;
        readFieldsInto(node.data);
        var key = event && event.target ? fieldKeyOf(event.target) : "";
        if (key === "entity" && previousEntity !== node.data.config.entity) {
            // Fields of another record type no longer apply.
            ["fields", "key", "filters"].forEach(function (name) { delete node.data.config[name]; });
        }
        editor.updateNodeDataFromId(selectedId, node.data);
        var htmlNode = root.querySelector("#node-" + selectedId + " .auto-node-name");
        if (htmlNode) {
            htmlNode.textContent = node.data.name || "";
        }
        var typeNode = root.querySelector("#node-" + selectedId + " .auto-node-type");
        if (typeNode) {
            // The service icon stays; only the text part is refreshed.
            var typeText = typeNode.querySelector("span:not(.auto-node-service)");
            (typeText || typeNode).textContent = typeLabel(node.data.type, node.data.type);
        }
        syncErrorOutput(selectedId, node.data);
        syncHidden();
        if (event && event.type === "change" && RERENDER_KEYS.indexOf(key) !== -1) {
            renderFields(node.data);
        }
        updateDone();
    }

    // Changing these fields changes which other fields (or choices) apply.
    var RERENDER_KEYS = ["scheduleKind", "entity", "event", "connectionId"];

    function fieldKeyOf(target) {
        var own = target.getAttribute && target.getAttribute("data-config-key");
        if (own) { return own; }
        var holder = target.closest ? target.closest("[data-combo-key]") : null;
        return holder ? holder.getAttribute("data-combo-key") : "";
    }

    // "Done" is available once the card's settings are complete.
    function updateDone() {
        var button = q("[data-props-done]");
        if (!button) { return; }
        var node = selectedId ? nodeOrNull(selectedId) : null;
        var ready = !!node && settingsReady(node.data);
        button.disabled = !ready;
        var hint = q("[data-props-done-hint]");
        if (hint) { hint.hidden = ready; }
        updatePropsWarning(node && node.data, ready);
    }

    // Warning on top of the properties window: which fields still need a value (and the fields themselves are marked).
    function updatePropsWarning(data, ready) {
        var box = q("[data-props-warning]");
        var host = q("[data-prop-fields]");
        if (!box) { return; }
        var keys = [];
        if (data && !data.disabled && !ready) {
            try { keys = missingKeysFor(data); } catch (e) { keys = []; }
        }
        if (host) {
            Array.prototype.forEach.call(host.querySelectorAll("[data-field-wrap]"), function (wrap) {
                wrap.classList.toggle("is-required-missing", keys.indexOf(wrap.getAttribute("data-field-wrap")) !== -1);
            });
        }
        box.textContent = "";
        if (!keys.length) { box.hidden = true; return; }
        var icon = document.createElement("i");
        icon.className = "bx bx-error";
        icon.setAttribute("aria-hidden", "true");
        var title = document.createElement("strong");
        title.textContent = t("nodeFillIn", "Fill in") + ":";
        box.appendChild(icon);
        box.appendChild(title);
        keys.forEach(function (key) {
            var chip = document.createElement("button");
            chip.type = "button";
            chip.className = "automation-props-warning-chip";
            chip.textContent = fieldLabelFor(data, key);
            chip.addEventListener("click", function () {
                var wrap = host ? host.querySelector('[data-field-wrap="' + key.replace(/"/g, "") + '"]') : null;
                if (!wrap) { return; }
                wrap.scrollIntoView({ block: "center", behavior: "smooth" });
                var control = wrap.querySelector("input, select, textarea, [contenteditable]");
                if (control && control.focus) { control.focus({ preventScroll: true }); }
            });
            box.appendChild(chip);
        });
        box.hidden = false;
    }

    function markSelected(id) {
        Array.prototype.forEach.call(root.querySelectorAll(".auto-node.is-selected"), function (el) {
            el.classList.remove("is-selected");
        });
        if (!id) {
            return;
        }
        var inner = root.querySelector("#node-" + id + " .auto-node");
        if (inner) {
            inner.classList.add("is-selected");
        }
    }

    function isCompactProps() {
        return window.matchMedia("(max-width: 991.98px)").matches;
    }

    function propsPanel() {
        return q("[data-props-panel]");
    }

    function showProps() {
        var panel = propsPanel();
        if (!panel) {
            return;
        }
        panel.hidden = false;
        panel.classList.add("is-open");
    }

    function hideProps() {
        var panel = propsPanel();
        if (!panel) {
            return;
        }
        panel.hidden = true;
        panel.classList.remove("is-open");
    }

    function clearEditorSelection() {
        if (editor.node_selected) {
            editor.node_selected.classList.remove("selected");
            editor.node_selected = null;
        }
        selectedId = null;
        markSelected(null);
        renderFields(null);
        hideProps();
    }

    var nodeMenu = document.createElement("div");
    nodeMenu.className = "automation-node-menu";
    nodeMenu.hidden = true;
    // Menu entry: icon + text (the text span can be changed later).
    function menuItem(icon, text, danger) {
        var button = document.createElement("button");
        button.type = "button";
        if (danger) { button.className = "is-danger"; }
        var i = document.createElement("i");
        i.className = "bx " + icon;
        i.setAttribute("aria-hidden", "true");
        var span = document.createElement("span");
        span.textContent = text;
        button.appendChild(i);
        button.appendChild(span);
        return button;
    }
    function setMenuText(button, text) {
        var span = button.querySelector("span");
        if (span) { span.textContent = text; }
    }
    var propsItem = menuItem("bx-slider-alt", t("nodeProperties", "Properties"));
    var deleteItem = menuItem("bx-trash", t("delete", "Delete"), true);
    var copyItem = menuItem("bx-copy", t("builderCopy", "Copy"));
    var pasteItem = menuItem("bx-paste", t("builderPaste", "Paste"));
    var toggleItem = menuItem("bx-power-off", t("builderDisable", "Turn off"));
    var historyItem = menuItem("bx-history", t("builderCardHistory", "History"));
    // empty canvas: opens the side section of the agent (history, versions, settings, connections)
    var inspectorItem = menuItem("bx-dock-right", t("builderInspector", "Inspector"));
    nodeMenu.appendChild(propsItem);
    nodeMenu.appendChild(toggleItem);
    nodeMenu.appendChild(historyItem);
    nodeMenu.appendChild(copyItem);
    nodeMenu.appendChild(pasteItem);
    nodeMenu.appendChild(inspectorItem);
    nodeMenu.appendChild(deleteItem);
    var menuPoint = null;
    var menuHost = root.closest(".automation-canvas-wrap") || root;
    menuHost.appendChild(nodeMenu);
    var menuNodeId = null;

    function hideNodeMenu() {
        nodeMenu.hidden = true;
        menuNodeId = null;
    }

    function canEditNow() {
        var form = document.getElementById("automationBuilderForm");
        return !form || form.getAttribute("data-can-edit") !== "false";
    }

    // id = card under the cursor, or null for the empty canvas (Paste only).
    function showNodeMenu(clientX, clientY, id) {
        var canEdit = canEditNow();
        var onCard = !!id;
        propsItem.hidden = !onCard;
        copyItem.hidden = !onCard;
        deleteItem.hidden = !onCard || !canEdit;
        var hoverNode = onCard ? nodeOrNull(id) : null;
        // Triggers do not run as steps, so only action cards have a history.
        historyItem.hidden = !onCard || !config.id || !hoverNode || hoverNode.data.kind === "trigger";
        toggleItem.hidden = !onCard || !canEdit;
        var current = onCard ? nodeOrNull(id) : null;
        setMenuText(toggleItem, current && current.data.disabled ? t("builderEnable", "Turn on") : t("builderDisable", "Turn off"));
        pasteItem.hidden = onCard || !canEdit;
        inspectorItem.hidden = onCard;
        pasteItem.disabled = !clipboard;
        menuPoint = { x: clientX, y: clientY };
        menuNodeId = id;
        nodeMenu.hidden = false;
        var hostRect = menuHost.getBoundingClientRect();
        var left = clientX - hostRect.left;
        var top = clientY - hostRect.top;
        nodeMenu.style.left = "0px";
        nodeMenu.style.top = "0px";
        var menuRect = nodeMenu.getBoundingClientRect();
        left = Math.max(8, Math.min(left, hostRect.width - menuRect.width - 8));
        top = Math.max(8, Math.min(top, hostRect.height - menuRect.height - 8));
        nodeMenu.style.left = left + "px";
        nodeMenu.style.top = top + "px";
    }

    function selectNodeElement(id, nodeEl) {
        selectedId = id;
        markSelected(id);
        if (editor.node_selected && editor.node_selected !== nodeEl) {
            editor.node_selected.classList.remove("selected");
        }
        editor.node_selected = nodeEl;
        nodeEl.classList.add("selected");
        var node = editor.getNodeFromId(id);
        renderFields(node.data);
    }

    editor.on("nodeSelected", function (id) {
        hideNodeMenu();
        selectedId = id;
        markSelected(id);
        var node = editor.getNodeFromId(id);
        renderFields(node.data);
        if (!isCompactProps()) {
            showProps();
        }
    });

    editor.on("nodeUnselected", function () {
        hideNodeMenu();
        selectedId = null;
        markSelected(null);
        renderFields(null);
        hideProps();
    });

    function onPropsClose(event) {
        event.preventDefault();
        event.stopPropagation();
        persistSelected();
        clearEditorSelection();
    }

    function onNodeContext(event) {
        var nodeEl = event.target.closest(".drawflow-node");
        if (!nodeEl || !root.contains(nodeEl)) {
            // Empty canvas: Paste and Inspector (connections keep their own menu).
            if (!root.contains(event.target) || event.target.closest(".connection, .drawflow-delete, .point")) {
                return;
            }
            event.preventDefault();
            event.stopPropagation();
            clearPress();
            showNodeMenu(event.clientX, event.clientY, null);
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        if (typeof event.stopImmediatePropagation === "function") {
            event.stopImmediatePropagation();
        }
        clearPress();
        var id = nodeEl.id.replace("node-", "");
        selectNodeElement(id, nodeEl);
        showNodeMenu(event.clientX, event.clientY, id);
    }
    root.addEventListener("contextmenu", onNodeContext, true);
    cleanupFns.push(function () {
        root.removeEventListener("contextmenu", onNodeContext, true);
    });

    inspectorItem.addEventListener("click", function (event) {
        event.preventDefault();
        hideNodeMenu();
        if (window.PartnerlyBuilderSide) { window.PartnerlyBuilderSide.open(); }
    });
    propsItem.addEventListener("click", function (event) {
        event.preventDefault();
        hideNodeMenu();
        showProps();
    });
    function uniqueName(base) {
        var home = (editor.export().drawflow || {}).Home || {};
        var used = {};
        Object.keys(home.data || {}).forEach(function (key) {
            used[(home.data[key].data || {}).name] = true;
        });
        var n = editor.getNodesFromName(base).length;
        while (used[base + n]) { n += 1; }
        return base + n;
    }

    function copySelected(id) {
        var node = nodeOrNull(id);
        if (!node) { return; }
        persistSelected();
        node = nodeOrNull(id);
        clipboard = JSON.parse(JSON.stringify(node.data));
    }

    // point = {x, y} in client pixels, or null for the middle of the view.
    function pasteClipboard(point) {
        if (!clipboard || !canEditNow()) { return; }
        var data = JSON.parse(JSON.stringify(clipboard));
        data.id = "n" + Date.now() + Math.floor(Math.random() * 1000);
        data.name = uniqueName(slug(data.type));
        var x = null;
        var y = null;
        if (point) {
            var rect = root.getBoundingClientRect();
            var zoom = editor.zoom || 1;
            x = (point.x - rect.left - (editor.canvas_x || 0)) / zoom;
            y = (point.y - rect.top - (editor.canvas_y || 0)) / zoom;
        }
        addNode(data, x, y);
        syncHidden();
    }

    // Turn a card off / on. Cards before and after it keep their connections;
    // a run passes through an off card without doing anything.
    function setCardDisabled(id, off) {
        if (!id || !canEditNow()) { return; }
        persistSelected();
        var node = nodeOrNull(id);
        if (!node) { return; }
        if (off) { node.data.disabled = true; } else { delete node.data.disabled; }
        editor.updateNodeDataFromId(id, node.data);
        syncHidden();
        var sw = q("[data-prop-enabled]");
        if (sw && String(selectedId) === String(id)) { sw.checked = !off; }
        updateDone();
    }

    toggleItem.addEventListener("click", function (event) {
        event.preventDefault();
        var id = menuNodeId;
        hideNodeMenu();
        var node = id ? nodeOrNull(id) : null;
        if (node) { setCardDisabled(id, !node.data.disabled); }
    });
    // Runs of this one card (its steps in the agent's past runs) in the shared popup;
    // hub.js opens it for a [data-hub="node-history"] element.
    historyItem.addEventListener("click", function (event) {
        event.preventDefault();
        var id = menuNodeId;
        hideNodeMenu();
        var node = id ? nodeOrNull(id) : null;
        if (!node || !config.id) { return; }
        var opener = document.createElement("button");
        opener.type = "button";
        opener.hidden = true;
        opener.setAttribute("data-hub", "node-history");
        opener.setAttribute("data-id", config.id);
        opener.setAttribute("data-node", node.data.id || ("n" + id));
        opener.setAttribute("data-name", node.data.name || "");
        document.body.appendChild(opener);
        opener.click();
        document.body.removeChild(opener);
    });
    var enabledSwitch = q("[data-prop-enabled]");
    if (enabledSwitch) {
        var onSwitch = function () { setCardDisabled(selectedId, !enabledSwitch.checked); };
        enabledSwitch.addEventListener("change", onSwitch);
        cleanupFns.push(function () { enabledSwitch.removeEventListener("change", onSwitch); });
    }

    copyItem.addEventListener("click", function (event) {
        event.preventDefault();
        var id = menuNodeId;
        hideNodeMenu();
        if (id) { copySelected(id); }
    });
    pasteItem.addEventListener("click", function (event) {
        event.preventDefault();
        var point = menuPoint;
        hideNodeMenu();
        pasteClipboard(point);
    });
    deleteItem.addEventListener("click", function (event) {
        event.preventDefault();
        var id = menuNodeId;
        hideNodeMenu();
        if (!id || !editor) {
            return;
        }
        persistSelected();
        try {
            editor.removeNodeId("node-" + id);
        } catch (err) { }
        selectedId = null;
        markSelected(null);
        renderFields(null);
        hideProps();
        syncHidden();
    });
    function onDocHideMenu(event) {
        if (nodeMenu.hidden) {
            return;
        }
        if (event.target && nodeMenu.contains(event.target)) {
            return;
        }
        hideNodeMenu();
    }
    function typingInField() {
        var active = document.activeElement;
        if (!active) { return false; }
        // A switch / checkbox / button does not type text, so Ctrl+Z / Ctrl+Y still reach the canvas.
        if (active.tagName === "INPUT") { return !/^(checkbox|radio|button|submit|range)$/i.test(active.type); }
        return /^(TEXTAREA|SELECT)$/.test(active.tagName) || active.isContentEditable;
    }

    function onKeyHideMenu(event) {
        if (event.key === "Escape") {
            hideNodeMenu();
            hidePortPicker();
            return;
        }
        // Ctrl/Cmd+S saves (also while typing in a field; the browser's own "save page" is blocked).
        if ((event.ctrlKey || event.metaKey) && !event.shiftKey && !event.altKey && String(event.key).toLowerCase() === "s" && document.body.contains(root)) {
            event.preventDefault();
            var saveButton = document.querySelector('#automationBuilderForm [data-act="save"]');
            if (saveButton && !saveButton.disabled) { saveButton.click(); }
            return;
        }
        // Ctrl/Cmd+Z undo, Ctrl/Cmd+Y (or Shift+Z) redo, Ctrl/Cmd+C copies the selected card, Ctrl/Cmd+V pastes it.
        if (!(event.ctrlKey || event.metaKey) || typingInField() || !document.body.contains(root)) {
            return;
        }
        var window_selection = window.getSelection && window.getSelection().toString();
        if (window_selection) { return; }
        var k = String(event.key).toLowerCase();
        if (k === "z") {
            event.preventDefault();
            if (event.shiftKey) { redoStep(); } else { undoStep(); }
        } else if (k === "y") {
            event.preventDefault();
            redoStep();
        } else if (k === "c" && selectedId) {
            copySelected(selectedId);
        } else if (k === "v" && clipboard) {
            event.preventDefault();
            pasteClipboard(null);
        }
    }
    document.addEventListener("click", onDocHideMenu);
    document.addEventListener("keydown", onKeyHideMenu);
    menuHost.addEventListener("wheel", hideNodeMenu, { passive: true });
    cleanupFns.push(function () {
        document.removeEventListener("click", onDocHideMenu);
        document.removeEventListener("keydown", onKeyHideMenu);
        menuHost.removeEventListener("wheel", hideNodeMenu);
        hideNodeMenu();
        if (nodeMenu.parentNode) {
            nodeMenu.parentNode.removeChild(nodeMenu);
        }
    });

    var pressTimer = null;
    function clearPress() {
        if (pressTimer) {
            window.clearTimeout(pressTimer);
            pressTimer = null;
        }
    }
    function onTouchStart(event) {
        if (!isCompactProps()) {
            return;
        }
        var nodeEl = event.target.closest(".drawflow-node");
        if (!nodeEl) {
            return;
        }
        var touch = event.touches && event.touches[0];
        var clientX = touch ? touch.clientX : 0;
        var clientY = touch ? touch.clientY : 0;
        var id = nodeEl.id.replace("node-", "");
        pressTimer = window.setTimeout(function () {
            pressTimer = null;
            selectNodeElement(id, nodeEl);
            showNodeMenu(clientX, clientY, id);
        }, 520);
    }
    root.addEventListener("touchstart", onTouchStart, { passive: true });
    root.addEventListener("touchmove", clearPress, { passive: true });
    root.addEventListener("touchend", clearPress);
    root.addEventListener("touchcancel", clearPress);
    cleanupFns.push(function () {
        clearPress();
        root.removeEventListener("touchstart", onTouchStart);
        root.removeEventListener("touchmove", clearPress);
        root.removeEventListener("touchend", clearPress);
        root.removeEventListener("touchcancel", clearPress);
    });

    function onPanStart(event) {
        var target = event.target;
        if (!target) {
            return;
        }
        var first = target.classList && target.classList[0];
        if (first === "parent-drawflow" || first === "drawflow" || first === "automation-canvas") {
            root.classList.add("is-panning");
        }
    }
    function onPanEnd() {
        root.classList.remove("is-panning");
    }
    root.addEventListener("mousedown", onPanStart);
    window.addEventListener("mouseup", onPanEnd);
    cleanupFns.push(function () {
        root.removeEventListener("mousedown", onPanStart);
        window.removeEventListener("mouseup", onPanEnd);
        onPanEnd();
    });

    var statusCard = q("[data-status-card]");
    if (statusCard) {
        var onStatusClick = function (event) {
            var target = event.target;
            if (!target || !target.closest) { return; }
            if (target.closest("[data-undo]")) { undoStep(); return; }
            if (target.closest("[data-redo]")) { redoStep(); return; }
            var toggle = target.closest("[data-history-toggle]");
            if (toggle) {
                var panel = q("[data-history-list]");
                if (panel) {
                    panel.hidden = !panel.hidden;
                    toggle.setAttribute("aria-expanded", panel.hidden ? "false" : "true");
                    var current = panel.querySelector(".is-current");
                    if (current && !panel.hidden) { current.scrollIntoView({ block: "nearest" }); }
                }
                return;
            }
            var item = target.closest("[data-history-index]");
            if (item) { restoreSnapshot(parseInt(item.getAttribute("data-history-index"), 10)); }
        };
        statusCard.addEventListener("click", onStatusClick);
        cleanupFns.push(function () { statusCard.removeEventListener("click", onStatusClick); });
    }
    renderHistory();

    var props = q(".automation-props");
    if (props) {
        function stopCanvasPointer(event) {
            if (event.target && event.target.closest && event.target.closest("[data-props-close]")) {
                return;
            }
            event.stopPropagation();
        }
        ["pointerdown", "mousedown", "touchstart"].forEach(function (name) {
            props.addEventListener(name, stopCanvasPointer);
            cleanupFns.push(function () {
                props.removeEventListener(name, stopCanvasPointer);
            });
        });
        props.addEventListener("click", function (event) {
            if (event.target && event.target.closest && event.target.closest("[data-props-close]")) {
                onPropsClose(event);
                return;
            }
            var done = event.target && event.target.closest && event.target.closest("[data-props-done]");
            if (done && !done.disabled) {
                onPropsClose(event);
            }
        });
        props.addEventListener("input", persistSelected);
        props.addEventListener("change", persistSelected);
        props.addEventListener("blur", persistSelected, true);
        cleanupFns.push(function () {
            props.removeEventListener("input", persistSelected);
            props.removeEventListener("change", persistSelected);
            props.removeEventListener("blur", persistSelected, true);
        });
    }

    var builderForm = document.getElementById("automationBuilderForm");
    function onSubmit() {
        persistSelected();
        syncHidden();
    }
    if (builderForm) {
        builderForm.addEventListener("submit", onSubmit);
        cleanupFns.push(function () {
            builderForm.removeEventListener("submit", onSubmit);
        });
    }

    window.syncAutomationBuilder = function () {
        persistSelected();
        syncHidden();
    };

    function closeAddMenus() {
        Array.prototype.forEach.call(document.querySelectorAll("[data-add-menu].is-open"), function (menu) {
            menu.classList.remove("is-open");
            var panel = menu.querySelector(".automation-add-panel");
            var toggle = menu.querySelector("[data-add-toggle]");
            if (panel) {
                panel.hidden = true;
            }
            if (toggle) {
                toggle.setAttribute("aria-expanded", "false");
            }
        });
    }

    function onAddToggle(event) {
        var toggle = event.target.closest("[data-add-toggle]");
        if (!toggle) {
            // Picking any item of the dropdown closes it.
            if (event.target.closest(".automation-add-panel .automation-add-item")) {
                closeAddMenus();
            }
            return;
        }
        event.preventDefault();
        var menu = toggle.closest("[data-add-menu]");
        if (!menu) {
            return;
        }
        var willOpen = !menu.classList.contains("is-open");
        closeAddMenus();
        if (!willOpen) {
            return;
        }
        menu.classList.add("is-open");
        var panel = menu.querySelector(".automation-add-panel");
        if (panel) {
            panel.hidden = false;
        }
        toggle.setAttribute("aria-expanded", "true");
    }

    function onDocumentPointer(event) {
        if (event.target.closest("[data-add-menu]")) {
            return;
        }
        closeAddMenus();
    }

    var toolbar = q(".automation-toolbar");
    if (toolbar) {
        toolbar.addEventListener("click", onAddToggle);
        cleanupFns.push(function () {
            toolbar.removeEventListener("click", onAddToggle);
        });
    }
    document.addEventListener("click", onDocumentPointer);
    cleanupFns.push(function () {
        document.removeEventListener("click", onDocumentPointer);
        closeAddMenus();
    });

    // ---- "+" on a card's output: pick the next card and wire it ------------
    var portPicker = null;
    var bindPalette = null; // set once the catalog is loaded
    var portSource = null;

    function hidePortPicker() {
        if (portPicker) { portPicker.hidden = true; }
        portSource = null;
    }

    function pickForPort(item) {
        var src = portSource;
        hidePortPicker();
        if (!src || !nodeOrNull(src.id)) { return; }
        var from = nodeOrNull(src.id);
        var el = root.querySelector("#node-" + src.id);
        var siblings = ((from.outputs[src.output] || {}).connections || []).length;
        var side = src.output === "output_2" ? 1 : 0;
        var x = from.pos_x + (el ? el.offsetWidth : 200) + 90;
        var y = from.pos_y + (side * 120) + (siblings * 110);
        var newId = createFromItem(item, x, y);
        if (item.kind !== "trigger") {
            try { editor.addConnection(src.id, newId, src.output, "input_1"); } catch (e) { /* not connectable */ }
            syncHidden();
        }
        var newEl = root.querySelector("#node-" + newId);
        if (newEl && !isCompactProps()) {
            selectNodeElement(String(newId), newEl);
            showProps();
        }
    }

    function buildPortPicker(catalog) {
        if (portPicker && portPicker.parentNode) { portPicker.parentNode.removeChild(portPicker); }
        portPicker = document.createElement("div");
        portPicker.className = "automation-add-panel automation-port-picker";
        portPicker.hidden = true;
        var switcher = document.createElement("div");
        switcher.className = "automation-port-switch";
        var panes = {};
        [["action", t("addAction", "Add action")], ["trigger", t("addTrigger", "Add trigger")]].forEach(function (pair) {
            var button = document.createElement("button");
            button.type = "button";
            button.className = "automation-port-switch-btn";
            button.setAttribute("data-pane", pair[0]);
            button.textContent = pair[1];
            button.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                showPane(pair[0]);
            });
            switcher.appendChild(button);
            var pane = document.createElement("div");
            pane.setAttribute("data-pane-body", pair[0]);
            panes[pair[0]] = pane;
        });
        function showPane(name) {
            Array.prototype.forEach.call(switcher.children, function (button) {
                button.classList.toggle("is-active", button.getAttribute("data-pane") === name);
            });
            Object.keys(panes).forEach(function (key) { panes[key].hidden = key !== name; });
        }
        portPicker.appendChild(switcher);
        portPicker.appendChild(panes.action);
        portPicker.appendChild(panes.trigger);
        bindPalette(catalog.actions, panes.action, pickForPort);
        bindPalette(catalog.triggers, panes.trigger, pickForPort);
        showPane("action");
        menuHost.appendChild(portPicker);
    }

    function openPortPicker(outEl) {
        if (!portPicker || !canEditNow()) { return; }
        var nodeEl = outEl.closest(".drawflow-node");
        if (!nodeEl) { return; }
        var outputClass = Array.prototype.filter.call(outEl.classList, function (c) { return /^output_\d+$/.test(c); })[0] || "output_1";
        hideNodeMenu();
        portSource = { id: nodeEl.id.replace("node-", ""), output: outputClass };
        portPicker.hidden = false;
        // The window is fixed to the screen (nothing can clip it) and opens on the side
        // of the "+" where it fits: right or left, below or above.
        var portRect = outEl.getBoundingClientRect();
        var gap = 10;
        var edge = 8;
        var vw = window.innerWidth;
        var vh = window.innerHeight;
        portPicker.style.left = "0px";
        portPicker.style.top = "0px";
        var box = portPicker.getBoundingClientRect();
        var left = portRect.right + gap;
        if (left + box.width > vw - edge) {
            var leftSide = portRect.left - gap - box.width;
            left = leftSide >= edge ? leftSide : vw - box.width - edge;
        }
        var top = portRect.top - 20;
        if (top + box.height > vh - edge) {
            var above = portRect.bottom + 20 - box.height;
            top = above >= edge ? above : vh - box.height - edge;
        }
        left = Math.max(edge, left);
        top = Math.max(edge, top);
        portPicker.style.left = left + "px";
        portPicker.style.top = top + "px";
        // A transformed ancestor would shift a fixed box: correct by what we measure.
        var placed = portPicker.getBoundingClientRect();
        if (Math.abs(placed.left - left) > 1 || Math.abs(placed.top - top) > 1) {
            portPicker.style.left = (left - (placed.left - left)) + "px";
            portPicker.style.top = (top - (placed.top - top)) + "px";
        }
    }

    var portDown = null;
    function onPortDown(event) {
        var out = event.target.closest && event.target.closest(".output");
        portDown = out ? { el: out, x: event.clientX, y: event.clientY } : null;
    }
    function onPortClick(event) {
        var out = event.target.closest && event.target.closest(".output");
        var down = portDown;
        portDown = null;
        if (!out || !down || down.el !== out) { return; }
        if (Math.abs(event.clientX - down.x) > 5 || Math.abs(event.clientY - down.y) > 5) { return; }
        event.stopPropagation();
        openPortPicker(out);
    }
    function onDocHidePicker(event) {
        if (!portPicker || portPicker.hidden) { return; }
        if (portPicker.contains(event.target) || (event.target.closest && event.target.closest(".output"))) { return; }
        hidePortPicker();
    }
    root.addEventListener("mousedown", onPortDown, true);
    root.addEventListener("click", onPortClick, true);
    document.addEventListener("click", onDocHidePicker);
    cleanupFns.push(function () {
        root.removeEventListener("mousedown", onPortDown, true);
        root.removeEventListener("click", onPortClick, true);
        document.removeEventListener("click", onDocHidePicker);
        if (portPicker && portPicker.parentNode) { portPicker.parentNode.removeChild(portPicker); }
        portPicker = null;
    });

    var triggersHost = q("[data-palette-triggers]");
    var actionsHost = q("[data-palette-actions]");
    if (triggersHost) {
        triggersHost.innerHTML = "";
    }
    if (actionsHost) {
        actionsHost.innerHTML = "";
    }

    fetch(config.catalogUrl, { credentials: "same-origin" })
        .then(function (response) { return response.json(); })
        .then(function (catalog) {
            if (myBoot !== bootId) {
                return;
            }
            catalogConnections = catalog.connections || [];
            integrationsUrl = catalog.integrationsUrl || "";
            lookups = catalog.lookups || {};
            adoptServerFavorites(catalog.favorites);
            if (selectedId && nodeOrNull(selectedId)) {
                renderFields(nodeOrNull(selectedId).data);
            }
            function bind(list, container, onPick) {
                if (!container) {
                    return;
                }
                var canEdit = !builderForm || builderForm.getAttribute("data-can-edit") !== "false";
                var general = t("builderGeneral", "General");
                // Built-in blocks first, then one section per integration.
                var allItems = (list || []).slice().sort(function (a, b) {
                    return (a.group ? 1 : 0) - (b.group ? 1 : 0);
                });
                // A service nobody has turned on (no connection of its kind): its blocks
                // are replaced by one notice with a button that opens Integrations.
                function serviceOff(item) {
                    return !!item.connectionKind && !catalogConnections.some(function (c) { return c.kind === item.connectionKind; });
                }
                var items = allItems.filter(function (item) { return !serviceOff(item); });
                var offGroups = [];
                allItems.forEach(function (item) {
                    if (serviceOff(item) && item.group && !offGroups.some(function (g) { return g.name === item.group; })) {
                        offGroups.push({ name: item.group, kind: item.connectionKind });
                    }
                });
                var groups = [];
                items.forEach(function (item) {
                    var name = item.group || general;
                    if (groups.indexOf(name) === -1) { groups.push(name); }
                });
                offGroups.forEach(function (g) {
                    if (groups.indexOf(g.name) === -1) { groups.push(g.name); }
                });

                function mk(tag, className, text) {
                    var node = document.createElement(tag);
                    if (className) { node.className = className; }
                    if (text != null) { node.textContent = text; }
                    return node;
                }
                function icon(name) {
                    var i = mk("i", "bx " + name);
                    i.setAttribute("aria-hidden", "true");
                    return i;
                }

                var pal = mk("div", "automation-pal");
                var nav = mk("div", "automation-pal-nav");
                var listEl = mk("div", "automation-pal-list");
                var empty = mk("p", "automation-pal-empty", t("builderNoFavorites", "No favorites yet. Click the star next to any item to pin it here."));
                empty.hidden = true;
                pal.appendChild(nav);
                pal.appendChild(listEl);
                container.appendChild(pal);

                var tabs = [{ id: "__fav", label: t("builderFavorites", "Favorites"), icon: "bxs-star" }, { id: "", label: t("builderAll", "All"), icon: "bx-grid-alt" }]
                    .concat(groups.map(function (name) {
                        return { id: name, label: name, icon: name === general ? "bx-cube-alt" : (GROUP_ICONS[name] || "bx-plug") };
                    }));
                var active = "";

                function isFav(key) { return favorites.indexOf(key) !== -1; }

                function applyFilter() {
                    var favCount = 0;
                    Array.prototype.forEach.call(listEl.children, function (node) {
                        if (node === empty) { return; }
                        var group = node.getAttribute("data-group");
                        if (node.classList.contains("automation-add-group")) {
                            node.hidden = active !== "";
                            return;
                        }
                        var visible = active === "__fav" ? isFav(node.getAttribute("data-key")) : (active === "" || group === active);
                        node.hidden = !visible;
                        if (visible) { favCount += 1; }
                    });
                    empty.hidden = !(active === "__fav" && favCount === 0);
                    Array.prototype.forEach.call(nav.children, function (tab) {
                        tab.classList.toggle("is-active", tab.getAttribute("data-tab") === active);
                    });
                }

                tabs.forEach(function (tab) {
                    var button = mk("button", "automation-pal-tab");
                    button.type = "button";
                    button.setAttribute("data-tab", tab.id);
                    button.appendChild(icon(tab.icon));
                    button.appendChild(mk("span", null, tab.label));
                    button.addEventListener("click", function (event) {
                        event.preventDefault();
                        event.stopPropagation();
                        active = tab.id;
                        applyFilter();
                    });
                    nav.appendChild(button);
                });

                listEl.appendChild(empty);
                var lastGroup = null;
                items.forEach(function (item) {
                    var name = item.group || general;
                    var key = item.kind + ":" + item.type;
                    if (name !== lastGroup) {
                        var heading = mk("div", "automation-add-group", name);
                        heading.setAttribute("data-group", name);
                        listEl.appendChild(heading);
                    }
                    lastGroup = name;
                    var row = mk("div", "automation-pal-row");
                    row.setAttribute("data-group", name);
                    row.setAttribute("data-key", key);
                    var button = mk("button", "automation-add-item");
                    button.type = "button";
                    button.appendChild(icon(TYPE_ICONS[item.type] || GROUP_ICONS[item.group] || "bx-cube-alt"));
                    button.appendChild(mk("span", null, typeLabel(item.type, item.displayName || item.type)));
                    button.disabled = !canEdit;
                    button.addEventListener("click", function () {
                        if (!canEdit) {
                            return;
                        }
                        if (onPick) {
                            onPick(item);
                            return;
                        }
                        createFromItem(item);
                        closeAddMenus();
                    });
                    var star = mk("button", "automation-fav");
                    star.type = "button";
                    function paintStar() {
                        var on = isFav(key);
                        star.classList.toggle("is-on", on);
                        star.setAttribute("aria-pressed", on ? "true" : "false");
                        star.title = on ? t("builderRemoveFavorite", "Remove from favorites") : t("builderAddFavorite", "Add to favorites");
                        star.innerHTML = "";
                        star.appendChild(icon(on ? "bxs-star" : "bx-star"));
                    }
                    paintStar();
                    favRepaint.push(function () { paintStar(); applyFilter(); });
                    star.addEventListener("click", function (event) {
                        event.preventDefault();
                        event.stopPropagation();
                        setFavorite(key, !isFav(key));
                        paintStar();
                        applyFilter();
                    });
                    row.appendChild(button);
                    row.appendChild(star);
                    listEl.appendChild(row);
                });

                offGroups.forEach(function (g) {
                    var heading = mk("div", "automation-add-group", g.name);
                    heading.setAttribute("data-group", g.name);
                    listEl.appendChild(heading);
                    var notice = mk("div", "automation-pal-notice");
                    notice.setAttribute("data-group", g.name);
                    notice.appendChild(icon(GROUP_ICONS[g.name] || "bx-plug"));
                    notice.appendChild(mk("p", null, t("builderServiceOff", "{0} is not turned on yet. Turn it on in Integrations to use these blocks.").replace("{0}", g.name)));
                    var enable = mk("a", "btn btn-primary btn-sm", t("builderServiceEnable", "Turn on"));
                    enable.href = (integrationsUrl || "/Integrations") + "?service=" + encodeURIComponent(g.kind);
                    enable.target = "_blank";
                    enable.rel = "noopener";
                    notice.appendChild(enable);
                    listEl.appendChild(notice);
                });

                active = items.some(function (item) { return isFav(item.kind + ":" + item.type); }) ? "__fav" : "";
                applyFilter();
            }
            bindPalette = bind;
            bind(catalog.triggers, triggersHost);
            bind(catalog.actions, actionsHost);
            buildPortPicker(catalog);
            fillTemplateConnections((catalog.triggers || []).concat(catalog.actions || []));
        });

    function zoomLabel() {
        var label = q("[data-zoom-label]");
        if (label) {
            label.textContent = Math.round((editor.zoom || 1) * 100) + "%";
        }
    }

    function applyCanvasTransform() {
        if (!editor.precanvas) {
            return;
        }
        editor.precanvas.style.transform = "translate(" + (editor.canvas_x || 0) + "px, " + (editor.canvas_y || 0) + "px) scale(" + (editor.zoom || 1) + ")";
    }

    function applyZoom(next) {
        var current = editor.zoom || 1;
        // Steps never jump over 100%: crossing it lands exactly on it.
        if ((current < 0.9995 && next > 1.0005) || (current > 1.0005 && next < 0.9995)) {
            next = 1;
        }
        var min = editor.zoom_min || 0.2;
        var max = editor.zoom_max || 1.8;
        editor.zoom = Math.max(min, Math.min(max, next));
        if (typeof editor.zoom_refresh === "function") {
            editor.zoom_refresh();
        } else {
            applyCanvasTransform();
        }
        zoomLabel();
    }

    // bounds (optional): where the cards are going to be; otherwise the cards are measured as they are.
    function fitView(bounds) {
        var nodes = root.querySelectorAll(".drawflow-node");
        if (!nodes.length) {
            applyZoom(1);
            editor.canvas_x = 0;
            editor.canvas_y = 0;
            applyCanvasTransform();
            return;
        }
        var minX = Infinity;
        var minY = Infinity;
        var maxX = -Infinity;
        var maxY = -Infinity;
        if (bounds) {
            minX = bounds.minX; minY = bounds.minY; maxX = bounds.maxX; maxY = bounds.maxY;
        } else {
            Array.prototype.forEach.call(nodes, function (el) {
                minX = Math.min(minX, el.offsetLeft);
                minY = Math.min(minY, el.offsetTop);
                maxX = Math.max(maxX, el.offsetLeft + el.offsetWidth);
                maxY = Math.max(maxY, el.offsetTop + el.offsetHeight);
            });
        }
        var pad = 56;
        var width = Math.max(maxX - minX + pad * 2, 1);
        var height = Math.max(maxY - minY + pad * 2, 1);
        var area = root.getBoundingClientRect();
        var zoom = Math.min(area.width / width, area.height / height, 1);
        applyZoom(zoom);
        // The canvas scales around its own middle (transform-origin), not its corner.
        var origin = (getComputedStyle(editor.precanvas).transformOrigin || "0px 0px").split(" ").map(parseFloat);
        var ox = origin[0] || 0, oy = origin[1] || 0;
        editor.canvas_x = (area.width - width * editor.zoom) / 2 - (minX - pad) * editor.zoom - ox * (1 - editor.zoom);
        editor.canvas_y = (area.height - height * editor.zoom) / 2 - (minY - pad) * editor.zoom - oy * (1 - editor.zoom);
        applyCanvasTransform();
    }

    // ---- "Tidy up": the user picks one of several layouts (chains by the shortest path) ----
    var ARRANGE_VARIANTS = [
        { key: "rows", dir: "h", wrap: true, gap: 110, lane: 44, minZoom: 0.6 },
        { key: "line", dir: "h", wrap: false, gap: 110, lane: 44, minZoom: 0 },
        { key: "tight", dir: "h", wrap: true, gap: 56, lane: 22, minZoom: 0.75 },
        { key: "down", dir: "v", wrap: true, gap: 56, lane: 50, minZoom: 0.6 },
        { key: "column", dir: "v", wrap: false, gap: 56, lane: 50, minZoom: 0 },
        { key: "spacious", dir: "h", wrap: true, gap: 200, lane: 90, minZoom: 0.5 },
        { key: "tree", dir: "h", wrap: true, gap: 110, lane: 44, minZoom: 0.6, center: true },
        { key: "stairs", dir: "h", wrap: true, gap: 70, lane: 30, minZoom: 0.6, stairs: 0.9 },
        { key: "snake", dir: "h", wrap: true, gap: 110, lane: 60, minZoom: 0.6, snake: true },
        { key: "grid", dir: "h", wrap: false, gap: 80, lane: 40, minZoom: 0, grid: true }
    ];

    function readArrangeModel() {
        var exported = editor.export();
        var home = exported.drawflow && exported.drawflow.Home && exported.drawflow.Home.data ? exported.drawflow.Home.data : {};
        var ids = Object.keys(home);
        var size = {};
        var maxW = 0, maxH = 0;
        ids.forEach(function (id) {
            var el = document.getElementById("node-" + id);
            size[id] = { w: el ? el.offsetWidth : 176, h: el ? el.offsetHeight : 76 };
            maxW = Math.max(maxW, size[id].w);
            maxH = Math.max(maxH, size[id].h);
        });
        var next = {}, prev = {};
        ids.forEach(function (id) { next[id] = []; prev[id] = []; });
        ids.forEach(function (id) {
            Object.keys(home[id].outputs || {}).forEach(function (outKey) {
                (home[id].outputs[outKey].connections || []).forEach(function (link) {
                    var to = String(link.node);
                    if (next[to] && next[id].indexOf(to) === -1) { next[id].push(to); prev[to].push(id); }
                });
            });
        });
        // chains = groups of cards joined to each other
        var seen = {}, chains = [];
        var byPos = ids.slice().sort(function (a, b) { return home[a].pos_x - home[b].pos_x || home[a].pos_y - home[b].pos_y; });
        byPos.forEach(function (start) {
            if (seen[start]) { return; }
            var members = [], queue = [start];
            seen[start] = true;
            while (queue.length) {
                var cur = queue.shift();
                members.push(cur);
                next[cur].concat(prev[cur]).forEach(function (n) { if (!seen[n]) { seen[n] = true; queue.push(n); } });
            }
            chains.push(members);
        });
        return { home: home, ids: ids, size: size, maxW: maxW, maxH: maxH, next: next, prev: prev, chains: chains };
    }

    function computeArrange(model, v) {
        var home = model.home, next = model.next, prev = model.prev;
        var horizontal = v.dir === "h";
        var depthStep = (horizontal ? model.maxW : model.maxH) + v.gap;
        var laneStep = (horizontal ? model.maxH : model.maxW) + v.lane;
        var areaBox = root.getBoundingClientRect();
        var along = horizontal ? areaBox.width : areaBox.height;
        var maxCols = v.wrap ? Math.max(2, Math.floor((along / v.minZoom - 112) / depthStep)) : Infinity;
        var target = {};
        var top = 0;

        // grid: every card in a tidy grid shaped like the screen, chain after chain
        if (v.grid) {
            var order = [];
            model.chains.forEach(function (members) {
                var rootsG = members.filter(function (id) { return !prev[id].length; });
                var dep = {}, qG = rootsG.slice();
                rootsG.forEach(function (id) { dep[id] = 0; });
                while (qG.length) {
                    var c = qG.shift();
                    next[c].forEach(function (n) { if (dep[n] == null) { dep[n] = dep[c] + 1; qG.push(n); } });
                }
                members.slice().sort(function (a, b) { return (dep[a] || 0) - (dep[b] || 0); }).forEach(function (id) { order.push(id); });
            });
            var ratio = areaBox.width / Math.max(areaBox.height, 1);
            var cols = Math.max(1, Math.round(Math.sqrt(order.length * ratio * (laneStep / depthStep))));
            order.forEach(function (id, i) {
                target[id] = { x: (i % cols) * depthStep, y: Math.floor(i / cols) * laneStep };
            });
            return target;
        }

        model.chains.forEach(function (members) {
            // shortest distance from the start cards (cards nobody leads to; triggers first)
            var roots = members.filter(function (id) { return !prev[id].length; });
            roots.sort(function (a, b) {
                var ta = home[a].data && home[a].data.kind === "trigger" ? 0 : 1;
                var tb = home[b].data && home[b].data.kind === "trigger" ? 0 : 1;
                return ta - tb || home[a].pos_y - home[b].pos_y;
            });
            if (!roots.length) { roots = [members[0]]; }
            var depth = {};
            var queue = roots.slice();
            roots.forEach(function (id) { depth[id] = 0; });
            while (queue.length) {
                var cur = queue.shift();
                next[cur].forEach(function (n) {
                    if (depth[n] == null) { depth[n] = depth[cur] + 1; queue.push(n); }
                });
            }
            members.forEach(function (id) { if (depth[id] == null) { depth[id] = 0; } });

            var layers = [];
            members.forEach(function (id) { (layers[depth[id]] = layers[depth[id]] || []).push(id); });
            var rowOf = {};
            var bottom = 0;
            var maxLayer = 1;
            layers.forEach(function (layer) { if (layer) { maxLayer = Math.max(maxLayer, layer.length); } });
            layers.forEach(function (layer, d) {
                if (!layer) { return; }
                var wanted = {};
                layer.forEach(function (id) {
                    var parents = prev[id].filter(function (p) { return rowOf[p] != null && depth[p] < d; });
                    var own = (horizontal ? home[id].pos_y : home[id].pos_x) / laneStep;
                    wanted[id] = parents.length
                        ? parents.reduce(function (sum, p) { return sum + rowOf[p]; }, 0) / parents.length
                        : own;
                });
                layer.sort(function (a, b) { return wanted[a] - wanted[b]; });
                if (v.center) {
                    // centred tree: every column is centred on the middle line of the chain
                    layer.forEach(function (id, i) {
                        rowOf[id] = (maxLayer - 1) / 2 + i - (layer.length - 1) / 2;
                        bottom = Math.max(bottom, rowOf[id]);
                    });
                    return;
                }
                var last = -Infinity;
                layer.forEach(function (id) {
                    var row = Math.max(wanted[id], last + 1);
                    rowOf[id] = row;
                    last = row;
                    bottom = Math.max(bottom, row);
                });
            });
            var minRow = Infinity;
            members.forEach(function (id) { minRow = Math.min(minRow, rowOf[id]); });

            // a long chain wraps to the next band so the cards stay big enough to read
            var stairs = v.stairs || 0;
            var colsUsed = Math.min(layers.length, maxCols);
            var span = bottom - minRow + 1 + (colsUsed - 1) * stairs;
            var bands = Math.ceil(layers.length / maxCols);
            members.forEach(function (id) {
                var band = Math.floor(depth[id] / maxCols);
                var col = depth[id] % maxCols;
                if (v.snake && band % 2 === 1) { col = colsUsed - 1 - col; }
                var depthPos = col * depthStep;
                var lanePos = top + (rowOf[id] - minRow + col * stairs + band * (span + 0.6)) * laneStep;
                target[id] = horizontal ? { x: depthPos, y: lanePos } : { x: lanePos, y: depthPos };
            });
            top += bands * (span + 0.6) * laneStep + 30;
        });
        return target;
    }

    function applyArrange(model, target) {
        var ids = model.ids;
        root.classList.add("is-arranging");
        var live = editor.drawflow.drawflow.Home.data;
        ids.forEach(function (id) {
            var el = document.getElementById("node-" + id);
            live[id].pos_x = target[id].x;
            live[id].pos_y = target[id].y;
            if (el) { el.style.left = target[id].x + "px"; el.style.top = target[id].y + "px"; }
        });
        var bounds = { minX: Infinity, minY: Infinity, maxX: -Infinity, maxY: -Infinity };
        ids.forEach(function (id) {
            bounds.minX = Math.min(bounds.minX, target[id].x);
            bounds.minY = Math.min(bounds.minY, target[id].y);
            bounds.maxX = Math.max(bounds.maxX, target[id].x + model.size[id].w);
            bounds.maxY = Math.max(bounds.maxY, target[id].y + model.size[id].h);
        });
        var begin = performance.now();
        (function follow() {
            ids.forEach(function (id) { editor.updateConnectionNodes("node-" + id); });
            if (performance.now() - begin < 900) { window.setTimeout(follow, 16); }
        })();
        window.setTimeout(function () {
            root.classList.remove("is-arranging");
            ids.forEach(function (id) { editor.updateConnectionNodes("node-" + id); });
            fitView(bounds);
            syncHidden();
        }, 950);
    }

    // small picture of a layout (cards as rectangles, links as lines)
    function arrangePreview(model, target) {
        var NS = "http://www.w3.org/2000/svg";
        var minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
        model.ids.forEach(function (id) {
            minX = Math.min(minX, target[id].x);
            minY = Math.min(minY, target[id].y);
            maxX = Math.max(maxX, target[id].x + model.size[id].w);
            maxY = Math.max(maxY, target[id].y + model.size[id].h);
        });
        var W = 168, H = 92, pad = 5;
        var scale = Math.min((W - pad * 2) / Math.max(maxX - minX, 1), (H - pad * 2) / Math.max(maxY - minY, 1));
        var offX = (W - (maxX - minX) * scale) / 2 - minX * scale;
        var offY = (H - (maxY - minY) * scale) / 2 - minY * scale;
        var svg = document.createElementNS(NS, "svg");
        svg.setAttribute("viewBox", "0 0 " + W + " " + H);
        svg.setAttribute("class", "automation-arrange-svg");
        svg.setAttribute("aria-hidden", "true");
        model.ids.forEach(function (id) {
            model.next[id].forEach(function (to) {
                var line = document.createElementNS(NS, "line");
                line.setAttribute("x1", offX + (target[id].x + model.size[id].w) * scale);
                line.setAttribute("y1", offY + (target[id].y + model.size[id].h / 2) * scale);
                line.setAttribute("x2", offX + target[to].x * scale);
                line.setAttribute("y2", offY + (target[to].y + model.size[to].h / 2) * scale);
                line.setAttribute("class", "automation-arrange-link");
                svg.appendChild(line);
            });
        });
        model.ids.forEach(function (id) {
            var rect = document.createElementNS(NS, "rect");
            rect.setAttribute("x", offX + target[id].x * scale);
            rect.setAttribute("y", offY + target[id].y * scale);
            rect.setAttribute("width", Math.max(model.size[id].w * scale, 3));
            rect.setAttribute("height", Math.max(model.size[id].h * scale, 2));
            rect.setAttribute("rx", 1.5);
            var data = model.home[id].data || {};
            rect.setAttribute("class", "automation-arrange-card" + (data.kind === "trigger" ? " is-trigger" : "") + (data.disabled ? " is-off" : ""));
            svg.appendChild(rect);
        });
        return svg;
    }

    var arrangePanel = q("[data-arrange-panel]");

    function closeArrange() {
        if (arrangePanel) { arrangePanel.hidden = true; }
        if (zoomArrangeBtn) { zoomArrangeBtn.setAttribute("aria-expanded", "false"); }
    }

    function arrangeText(name) {
        return (arrangePanel && arrangePanel.getAttribute("data-l-" + name)) || name;
    }

    function openArrange() {
        if (!arrangePanel || !canEditNow() || importing) { return; }
        var model = readArrangeModel();
        if (!model.ids.length) { return; }
        var host = arrangePanel.querySelector("[data-arrange-list]");
        host.innerHTML = "";
        ARRANGE_VARIANTS.forEach(function (variant) {
            var target = computeArrange(model, variant);
            var option = document.createElement("button");
            option.type = "button";
            option.className = "automation-arrange-option";
            option.appendChild(arrangePreview(model, target));
            var title = document.createElement("strong");
            title.textContent = arrangeText(variant.key);
            var desc = document.createElement("span");
            desc.textContent = arrangeText(variant.key + "-d");
            option.appendChild(title);
            option.appendChild(desc);
            option.addEventListener("click", function () {
                closeArrange();
                applyArrange(model, target);
            });
            host.appendChild(option);
        });
        arrangePanel.hidden = false;
        if (zoomArrangeBtn) { zoomArrangeBtn.setAttribute("aria-expanded", "true"); }
    }

    var arrangeClose = arrangePanel ? arrangePanel.querySelector("[data-arrange-close]") : null;
    if (arrangeClose) { arrangeClose.addEventListener("click", closeArrange); }
    // a click on the empty canvas or anywhere else closes the window
    function onArrangeOutside(event) {
        if (!arrangePanel || arrangePanel.hidden) { return; }
        if (arrangePanel.contains(event.target) || (event.target.closest && event.target.closest("[data-zoom-arrange]"))) { return; }
        closeArrange();
    }
    document.addEventListener("pointerdown", onArrangeOutside, true);
    cleanupFns.push(function () { document.removeEventListener("pointerdown", onArrangeOutside, true); });

    editor.on("zoom", zoomLabel);

    var zoomInBtn = q("[data-zoom-in]");
    var zoomOutBtn = q("[data-zoom-out]");
    var zoomFitBtn = q("[data-zoom-fit]");
    function onZoomIn(event) {
        event.preventDefault();
        applyZoom((editor.zoom || 1) + (editor.zoom_value || 0.08));
    }
    function onZoomOut(event) {
        event.preventDefault();
        applyZoom((editor.zoom || 1) - (editor.zoom_value || 0.08));
    }
    function onZoomFit(event) {
        event.preventDefault();
        fitView();
    }
    if (zoomInBtn) {
        zoomInBtn.addEventListener("click", onZoomIn);
        cleanupFns.push(function () { zoomInBtn.removeEventListener("click", onZoomIn); });
    }
    if (zoomOutBtn) {
        zoomOutBtn.addEventListener("click", onZoomOut);
        cleanupFns.push(function () { zoomOutBtn.removeEventListener("click", onZoomOut); });
    }
    var zoomArrangeBtn = q("[data-zoom-arrange]");
    function onZoomArrange(event) {
        event.preventDefault();
        if (arrangePanel && !arrangePanel.hidden) { closeArrange(); } else { openArrange(); }
    }
    if (zoomArrangeBtn) {
        zoomArrangeBtn.addEventListener("click", onZoomArrange);
        cleanupFns.push(function () { zoomArrangeBtn.removeEventListener("click", onZoomArrange); });
    }
    if (zoomFitBtn) {
        zoomFitBtn.addEventListener("click", onZoomFit);
        cleanupFns.push(function () { zoomFitBtn.removeEventListener("click", onZoomFit); });
    }

    var wrap = root.closest(".automation-canvas-wrap") || root;
    function onWheel(event) {
        if (event.target && event.target.closest && event.target.closest(".automation-props, .automation-zoom, .automation-add-panel, .automation-status, .agent-chat, .automation-arrange, .automation-mapbox, .automation-multibar, .automation-note")) {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        var step = editor.zoom_value || 0.08;
        applyZoom((editor.zoom || 1) + (event.deltaY < 0 ? step : -step));
    }
    wrap.addEventListener("wheel", onWheel, { passive: false, capture: true });
    cleanupFns.push(function () {
        wrap.removeEventListener("wheel", onWheel, true);
    });

    if (config.graph) {
        importGraph(typeof config.graph === "string" ? JSON.parse(config.graph) : config.graph);
        window.setTimeout(fitView, 0);
    }
    ["nodeCreated", "nodeRemoved", "nodeMoved", "connectionCreated", "connectionRemoved"].forEach(function (name) {
        editor.on(name, function () {
            if (!importing) { syncHidden(); }
        });
    });
    syncHidden();
    window.PartnerlyBuilder.markSaved();
    window.dispatchEvent(new CustomEvent("partnerly:builder-changed"));
    renderFields(null);
    zoomLabel();

    // ---- pieces the extra tools (builder-tools.js) build on ----------------
    // Centre the view on a point of the canvas (card coordinates).
    function centerOn(px, py, smooth) {
        var area = root.getBoundingClientRect();
        var z = editor.zoom || 1;
        var origin = (getComputedStyle(editor.precanvas).transformOrigin || "0px 0px").split(" ").map(parseFloat);
        var ox = origin[0] || 0, oy = origin[1] || 0;
        editor.canvas_x = area.width / 2 - ox - (px - ox) * z;
        editor.canvas_y = area.height / 2 - oy - (py - oy) * z;
        if (smooth) {
            editor.precanvas.classList.add("is-gliding");
            window.setTimeout(function () { editor.precanvas.classList.remove("is-gliding"); }, 480);
        }
        applyCanvasTransform();
    }

    // Select a card, open its settings and bring it to the middle of the view.
    function focusCard(domId) {
        var el = document.getElementById("node-" + domId);
        if (!el) { return false; }
        selectNodeElement(String(domId), el);
        if (!isCompactProps()) { showProps(); }
        var left = parseFloat(el.style.left) || 0, top = parseFloat(el.style.top) || 0;
        centerOn(left + el.offsetWidth / 2, top + el.offsetHeight / 2, true);
        return true;
    }

    window.PartnerlyBuilder.focusCard = focusCard;
    window.PartnerlyBuilder.internals = {
        editor: editor, root: root, syncHidden: syncHidden, persistSelected: persistSelected, selectNodeElement: selectNodeElement,
        showProps: showProps, fitView: fitView, applyZoom: applyZoom, applyCanvasTransform: applyCanvasTransform, centerOn: centerOn,
        canEditNow: canEditNow, addNode: addNode, exportGraph: exportGraph, uniqueName: uniqueName, slug: slug,
        typingInField: typingInField, clearEditorSelection: clearEditorSelection, t: t, cleanupFns: cleanupFns,
        isImporting: function () { return importing; },
        getSettings: function () { return JSON.parse(JSON.stringify(boardSettings || {})); },
        setSettings: function (value) { boardSettings = cleanSettings(value); syncHidden(); },
        getNotes: function () { return JSON.parse(JSON.stringify(boardNotes)); },
        setNotes: function (list) { boardNotes = JSON.parse(JSON.stringify(list || [])); syncHidden(); }
    };
    window.dispatchEvent(new CustomEvent("partnerly:builder-ready"));
    };

    function configFromDom() {
        var form = document.getElementById("automationBuilderForm");
        if (!form) {
            return window.automationBuilder || {};
        }
        var graphEl = document.getElementById("automationBuilderGraph");
        var i18nEl = document.getElementById("automationBuilderI18n");
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
        return {
            id: form.getAttribute("data-builder-id"),
            graph: graph,
            catalogUrl: form.getAttribute("data-catalog-url"),
            telegramChatsUrl: form.getAttribute("data-telegram-chats-url"),
            computerSettingsUrl: form.getAttribute("data-computer-settings-url"),
            saveUrl: form.getAttribute("data-save-url"),
            favoriteUrl: form.getAttribute("data-favorite-url"),
            favoriteImportUrl: form.getAttribute("data-favorite-import-url"),
            i18n: i18n
        };
    }

    if (document.getElementById("drawflow") && !document.getElementById("automationHubRoot")) {
        window.initAutomationBuilder(configFromDom());
    }
})();
