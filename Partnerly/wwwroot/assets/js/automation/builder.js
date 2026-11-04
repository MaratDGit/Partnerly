(function () {
    "use strict";

    var editor = null;
    var bootId = 0;
    var cleanupFns = [];

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
    var ENTITIES = [
        { value: "user", label: "User" },
        { value: "support_ticket", label: "Support ticket" },
        { value: "notification", label: "Notification" },
        { value: "user_group", label: "User group" },
        { value: "email_template", label: "Email template" }
    ];

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
            { key: "expression", type: "text", label: t("cronExpression"), help: t("cronExpressionHelp"), showIf: { scheduleKind: ["cron"] } }
        ],
        interval: [
            { key: "minutes", type: "number", min: 1, label: t("intervalMinutes"), help: t("intervalMinutesHelp") }
        ],
        "entity.created": entityFields(),
        "entity.updated": entityFields(),
        "entity.deleted": entityFields(),
        event: [
            { key: "event", type: "text", label: t("eventName"), help: t("eventNameHelp") }
        ].concat(waitBeforeFields()),
        webhook: [
            { type: "note", help: t("webhookTriggerHelp") }
        ].concat(waitBeforeFields()),
        delay: delayFields("delayHelp", true),
        set_variable: [
            { key: "name", type: "text", label: t("variableName"), help: t("variableNameHelp") },
            { key: "value", type: "text", label: t("variableValue"), help: t("variableValueHelp") }
        ],
        send_email: [
            { key: "to", type: "text", label: t("emailTo"), help: t("emailToHelp") },
            { key: "template", type: "text", label: t("emailTemplate"), help: t("emailTemplateHelp") },
            { key: "subject", type: "text", label: t("emailSubject") },
            { key: "body", type: "textarea", label: t("emailBody") }
        ],
        send_notification: [
            { key: "message", type: "textarea", label: t("notificationMessage") },
            { key: "userId", type: "text", label: t("notificationUser"), help: t("notificationUserHelp") },
            { key: "notificationType", type: "text", label: t("notificationType") },
            { key: "link", type: "text", label: t("notificationLink") }
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
            { key: "connectionId", type: "text", label: t("httpConnection"), help: t("httpConnectionHelp") },
            { key: "body", type: "textarea", label: t("httpBody"), help: t("httpBodyHelp") }
        ],
        webhook_action: [
            { key: "url", type: "text", label: t("webhookUrl"), help: t("webhookUrlHelp") },
            { key: "body", type: "textarea", label: t("webhookBody"), help: t("webhookBodyHelp") },
            { key: "secret", type: "text", label: t("webhookSecret"), help: t("webhookSecretHelp") }
        ],
        execute_workflow: [
            { key: "automationId", type: "text", label: t("childAutomation"), help: t("childAutomationHelp") }
        ],
        branch: conditionFields(false),
        wait_until: conditionFields(true),
        "switch": [
            { key: "value", type: "text", label: t("switchValue"), help: t("switchValueHelp") }
        ],
        create_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "fields", type: "pairs", label: t("recordFields"), help: t("recordFieldsHelp") }
        ],
        update_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "key", type: "pairs", label: t("recordKey"), help: t("recordKeyHelp") },
            { key: "fields", type: "pairs", label: t("recordFields"), help: t("recordFieldsHelp") }
        ],
        find_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "filters", type: "pairs", label: t("recordFilters"), help: t("recordFieldsHelp") }
        ],
        delete_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "key", type: "pairs", label: t("recordKey"), help: t("recordKeyHelp") }
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

    function schemaFor(data) {
        if (!data) {
            return [];
        }
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
        if (type === "branch" || type === "wait_until") {
            return { condition: { op: "equals", left: "{{vars.ready}}", right: "yes" } };
        }
        return {};
    }

    function slug(type) {
        return String(type || "node").replace(/[^A-Za-z0-9_]/g, "_");
    }

    function nodeHtml(data) {
        return '<div class="auto-node auto-node--' + (data.kind || "action") + '">' +
            '<span class="auto-node-kind">' + (data.kind === "trigger" ? t("triggerBlocks", "When") : t("actionBlocks", "Then")) + "</span>" +
            '<strong class="auto-node-type">' + typeLabel(data.type, data.type) + "</strong>" +
            '<span class="auto-node-name">' + (data.name || "") + "</span>" +
            '<span class="auto-node-check" title="' + t("nodeConfigured", "Configured") + '" aria-hidden="true"><i class="bx bx-check"></i></span></div>';
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
        branch: function (c) { return !!c.condition && filled(c.condition.left); },
        wait_until: function (c) { return !!c.condition && filled(c.condition.left); },
        "switch": function (c) { return filled(c.value); },
        create_record: function (c) { return filled(c.entity) && filled(c.fields); },
        update_record: function (c) { return filled(c.entity) && filled(c.key) && filled(c.fields); },
        find_record: function (c) { return filled(c.entity); },
        delete_record: function (c) { return filled(c.entity) && filled(c.key); }
    };

    var INTEGRATION_PREFIX = /^(telegram|sheets|gmail|drive|whatsapp)\./;

    function settingsReady(data) {
        var config = normalizeDelay(JSON.parse(JSON.stringify(data.config || {})));
        var type = data.type;
        if (INTEGRATION_PREFIX.test(type)) {
            if (!filled(config.connectionId)) { return false; }
            if (/\.message_received$/.test(type)) { return true; }
            return Object.keys(config).some(function (key) {
                return key !== "connectionId" && key !== "parseMode" && key !== "format" && filled(config[key]);
            });
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
        Object.keys(home).forEach(function (key) {
            var node = home[key];
            var data = node.data || {};
            var wired = data.kind === "trigger" ? connectionCount(node.outputs) > 0 : connectionCount(node.inputs) > 0;
            var inner = root.querySelector("#node-" + node.id + " .auto-node");
            if (inner) {
                inner.classList.toggle("is-configured", wired && settingsReady(data));
            }
        });
    }

    function outputsFor(type) {
        if (type === "branch" || type === "switch") {
            return 2;
        }
        return 1;
    }

    function portName(type, outputKey) {
        if (type === "branch") {
            return outputKey === "output_2" ? "false" : "true";
        }
        if (type === "switch") {
            return outputKey === "output_2" ? "case:0" : "default";
        }
        return null;
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
        var outputs = outputsFor(data.type);
        var point = (x == null || y == null) ? dropPoint() : null;
        return editor.addNode(
            slug(data.type),
            inputs,
            outputs,
            x == null ? point.x : x,
            y == null ? point.y : y,
            slug(data.kind + "-" + data.type),
            data,
            nodeHtml(data));
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
                config: data.config || {}
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
                        port: portName(type, outputKey)
                    });
                });
            });
        });
        return { schemaVersion: 1, nodes: nodes, edges: edges };
    }

    function importGraph(graph) {
        importing = true;
        try {
            importGraphInner(graph);
        } finally {
            importing = false;
        }
    }

    function importGraphInner(graph) {
        editor.clear();
        if (!graph || !graph.nodes) {
            return;
        }
        var dfIds = {};
        graph.nodes.forEach(function (node, index) {
            dfIds[node.id] = addNode(node, node.x || 80 + index * 40, node.y || 80 + index * 30);
        });
        (graph.edges || []).forEach(function (edge) {
            var from = dfIds[edge.from];
            var to = dfIds[edge.to];
            if (!from || !to) {
                return;
            }
            var output = "output_1";
            if (edge.port === "false" || (edge.port && edge.port.indexOf("case:") === 0)) {
                output = "output_2";
            }
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
            nodes: graph.nodes.map(function (n) { return { id: n.id, kind: n.kind, type: n.type, name: n.name, config: n.config }; }),
            edges: graph.edges.map(function (e) { return { from: e.from, to: e.to, port: e.port }; })
        });
    }

    function syncHidden() {
        var input = q("#graphJson") || document.getElementById("graphJson");
        if (input) {
            input.value = JSON.stringify(exportGraph());
        }
        refreshChecks();
        window.dispatchEvent(new CustomEvent("partnerly:builder-changed"));
    }

    window.PartnerlyBuilder = {
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
            return;
        }
        if (empty) empty.hidden = true;
        if (form) form.hidden = false;
        if (nameInput) nameInput.value = data.name || "";
        data.config = normalizeDelay(data.config || {});
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
            var label = document.createElement("label");
            label.className = "form-label";
            label.textContent = field.label || field.key;
            wrap.appendChild(label);
            var control;
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
            } else if (field.type === "select") {
                control = document.createElement("select");
                control.className = "form-select";
                (field.options || []).forEach(function (option) {
                    var item = document.createElement("option");
                    item.value = option.value;
                    item.textContent = option.label;
                    if (String(value || "") === String(option.value)) {
                        item.selected = true;
                    }
                    control.appendChild(item);
                });
            } else if (field.type === "textarea" || field.type === "pairs") {
                control = document.createElement("textarea");
                control.className = "form-control";
                control.rows = 4;
                control.value = field.type === "pairs" ? pairsToText(value) : (value || "");
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
            control.setAttribute("data-config-key", field.key);
            wrap.appendChild(control);
            if (field.help) {
                var hint = document.createElement("div");
                hint.className = "form-text";
                hint.textContent = field.help;
                wrap.appendChild(hint);
            }
            host.appendChild(wrap);
        });
        appendDocs(host, data);
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
        readFieldsInto(node.data);
        editor.updateNodeDataFromId(selectedId, node.data);
        var htmlNode = root.querySelector("#node-" + selectedId + " .auto-node-name");
        if (htmlNode) {
            htmlNode.textContent = node.data.name || "";
        }
        var typeNode = root.querySelector("#node-" + selectedId + " .auto-node-type");
        if (typeNode) {
            typeNode.textContent = typeLabel(node.data.type, node.data.type);
        }
        syncHidden();
        var key = event && event.target ? event.target.getAttribute("data-config-key") : "";
        if (key === "scheduleKind") {
            renderFields(node.data);
        }
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
    var propsItem = document.createElement("button");
    propsItem.type = "button";
    propsItem.textContent = t("nodeProperties", "Properties");
    var deleteItem = document.createElement("button");
    deleteItem.type = "button";
    deleteItem.className = "is-danger";
    deleteItem.textContent = t("delete", "Delete");
    nodeMenu.appendChild(propsItem);
    nodeMenu.appendChild(deleteItem);
    var menuHost = root.closest(".automation-canvas-wrap") || root;
    menuHost.appendChild(nodeMenu);
    var menuNodeId = null;

    function hideNodeMenu() {
        nodeMenu.hidden = true;
        menuNodeId = null;
    }

    function showNodeMenu(clientX, clientY, id) {
        var form = document.getElementById("automationBuilderForm");
        var canEdit = !form || form.getAttribute("data-can-edit") !== "false";
        deleteItem.hidden = !canEdit;
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

    propsItem.addEventListener("click", function (event) {
        event.preventDefault();
        hideNodeMenu();
        showProps();
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
    function onKeyHideMenu(event) {
        if (event.key === "Escape") {
            hideNodeMenu();
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
            }
        });
        props.addEventListener("change", persistSelected);
        props.addEventListener("blur", persistSelected, true);
        cleanupFns.push(function () {
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
            function bind(list, container) {
                if (!container) {
                    return;
                }
                var canEdit = !builderForm || builderForm.getAttribute("data-can-edit") !== "false";
                var lastGroup = null;
                // Built-in blocks first, then one titled group per integration.
                (list || []).slice().sort(function (a, b) {
                    return (a.group ? 1 : 0) - (b.group ? 1 : 0);
                }).forEach(function (item) {
                    if (item.group && item.group !== lastGroup) {
                        var heading = document.createElement("div");
                        heading.className = "automation-add-group";
                        heading.textContent = item.group;
                        container.appendChild(heading);
                    }
                    lastGroup = item.group || null;
                    var button = document.createElement("button");
                    button.type = "button";
                    button.className = "automation-add-item";
                    button.textContent = typeLabel(item.type, item.displayName || item.type);
                    button.disabled = !canEdit;
                    button.addEventListener("click", function () {
                        if (!canEdit) {
                            return;
                        }
                        var config = defaultConfig(item.type, item.kind);
                        if (item.connectionKind) {
                            var first = catalogConnections.filter(function (c) { return c.kind === item.connectionKind; })[0];
                            if (first) {
                                config.connectionId = first.id;
                            }
                        }
                        addNode({
                            id: "n" + Date.now(),
                            kind: item.kind,
                            type: item.type,
                            name: slug(item.type) + editor.getNodesFromName(slug(item.type)).length,
                            config: config
                        });
                        syncHidden();
                        closeAddMenus();
                    });
                    container.appendChild(button);
                });
            }
            bind(catalog.triggers, triggersHost);
            bind(catalog.actions, actionsHost);
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

    function fitView() {
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
        Array.prototype.forEach.call(nodes, function (el) {
            minX = Math.min(minX, el.offsetLeft);
            minY = Math.min(minY, el.offsetTop);
            maxX = Math.max(maxX, el.offsetLeft + el.offsetWidth);
            maxY = Math.max(maxY, el.offsetTop + el.offsetHeight);
        });
        var pad = 56;
        var width = Math.max(maxX - minX + pad * 2, 1);
        var height = Math.max(maxY - minY + pad * 2, 1);
        var area = root.getBoundingClientRect();
        var zoom = Math.min(area.width / width, area.height / height, 1);
        applyZoom(zoom);
        editor.canvas_x = (area.width - width * editor.zoom) / 2 - (minX - pad) * editor.zoom;
        editor.canvas_y = (area.height - height * editor.zoom) / 2 - (minY - pad) * editor.zoom;
        applyCanvasTransform();
    }

    editor.on("zoom", zoomLabel);

    var zoomInBtn = q("[data-zoom-in]");
    var zoomOutBtn = q("[data-zoom-out]");
    var zoomFitBtn = q("[data-zoom-fit]");
    function onZoomIn(event) {
        event.preventDefault();
        if (typeof editor.zoom_in === "function") {
            editor.zoom_in();
        } else {
            applyZoom((editor.zoom || 1) + (editor.zoom_value || 0.08));
        }
        zoomLabel();
    }
    function onZoomOut(event) {
        event.preventDefault();
        if (typeof editor.zoom_out === "function") {
            editor.zoom_out();
        } else {
            applyZoom((editor.zoom || 1) - (editor.zoom_value || 0.08));
        }
        zoomLabel();
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
    if (zoomFitBtn) {
        zoomFitBtn.addEventListener("click", onZoomFit);
        cleanupFns.push(function () { zoomFitBtn.removeEventListener("click", onZoomFit); });
    }

    var wrap = root.closest(".automation-canvas-wrap") || root;
    function onWheel(event) {
        if (event.target && event.target.closest && event.target.closest(".automation-props, .automation-zoom, .automation-add-panel")) {
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
            saveUrl: form.getAttribute("data-save-url"),
            i18n: i18n
        };
    }

    if (document.getElementById("drawflow") && !document.getElementById("automationHubRoot")) {
        window.initAutomationBuilder(configFromDom());
    }
})();
