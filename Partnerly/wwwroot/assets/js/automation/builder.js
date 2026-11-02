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
            { key: "value", type: "text", label: t("variableValue") }
        ],
        send_email: [
            { key: "to", type: "text", label: t("emailTo") },
            { key: "template", type: "text", label: t("emailTemplate"), help: t("emailTemplateHelp") },
            { key: "subject", type: "text", label: t("emailSubject") },
            { key: "body", type: "textarea", label: t("emailBody") }
        ],
        send_notification: [
            { key: "message", type: "textarea", label: t("notificationMessage") },
            { key: "userId", type: "text", label: t("notificationUser") },
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
            { key: "url", type: "text", label: t("httpUrl") },
            { key: "connectionId", type: "text", label: t("httpConnection"), help: t("httpConnectionHelp") },
            { key: "body", type: "textarea", label: t("httpBody") }
        ],
        webhook_action: [
            { key: "url", type: "text", label: t("webhookUrl") },
            { key: "body", type: "textarea", label: t("webhookBody") },
            { key: "secret", type: "text", label: t("webhookSecret") }
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
            { key: "key", type: "pairs", label: t("recordKey") },
            { key: "fields", type: "pairs", label: t("recordFields"), help: t("recordFieldsHelp") }
        ],
        find_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "filters", type: "pairs", label: t("recordFilters"), help: t("recordFieldsHelp") }
        ],
        delete_record: [
            { key: "entity", type: "select", label: t("entity"), help: t("entityHelp"), options: ENTITIES },
            { key: "key", type: "pairs", label: t("recordKey") }
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

    function schemaFor(data) {
        if (!data) {
            return [];
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
            '<span class="auto-node-name d-block">' + (data.name || "") + "</span></div>";
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

    function addNode(data, x, y) {
        var inputs = data.kind === "trigger" ? 0 : 1;
        var outputs = outputsFor(data.type);
        return editor.addNode(
            slug(data.type),
            inputs,
            outputs,
            x || 80,
            y || 80,
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

    function syncHidden() {
        var input = q("#graphJson") || document.getElementById("graphJson");
        if (input) {
            input.value = JSON.stringify(exportGraph());
        }
    }

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
            if (field.type === "select") {
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

    editor.on("nodeSelected", function (id) {
        selectedId = id;
        var node = editor.getNodeFromId(id);
        renderFields(node.data);
    });

    editor.on("nodeUnselected", function () {
        selectedId = null;
        renderFields(null);
    });

    var props = q(".automation-props");
    if (props) {
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
            function bind(list, container) {
                if (!container) {
                    return;
                }
                (list || []).forEach(function (item) {
                    var button = document.createElement("button");
                    button.type = "button";
                    button.textContent = typeLabel(item.type, item.displayName || item.type);
                    button.addEventListener("click", function () {
                        addNode({
                            id: "n" + Date.now(),
                            kind: item.kind,
                            type: item.type,
                            name: slug(item.type) + editor.getNodesFromName(slug(item.type)).length,
                            config: defaultConfig(item.type, item.kind)
                        }, 160, 120);
                        syncHidden();
                    });
                    container.appendChild(button);
                });
            }
            bind(catalog.triggers, triggersHost);
            bind(catalog.actions, actionsHost);
        });

    if (config.graph) {
        importGraph(typeof config.graph === "string" ? JSON.parse(config.graph) : config.graph);
    }
    syncHidden();
    renderFields(null);
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
