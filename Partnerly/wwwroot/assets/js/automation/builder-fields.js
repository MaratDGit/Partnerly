// DOM helpers for the agent builder's property panel: dropdowns that offer
// values from the chosen trigger / previous steps, so people pick instead of type.
// builder.js calls window.partnerlyBuilderFields(t) with its translator.
(function () {
    "use strict";

    var CUSTOM = "__custom__";

    window.partnerlyBuilderFields = function (t) {
        function make(tag, className, text) {
            var node = document.createElement(tag);
            if (className) { node.className = className; }
            if (text != null) { node.textContent = text; }
            return node;
        }

        function addOptions(select, options, current) {
            var groups = {};
            var matched = false;
            options.forEach(function (option) {
                var parent = select;
                if (option.group) {
                    if (!groups[option.group]) {
                        groups[option.group] = make("optgroup");
                        groups[option.group].label = option.group;
                        select.appendChild(groups[option.group]);
                    }
                    parent = groups[option.group];
                }
                var item = make("option", null, option.label);
                item.value = option.value;
                if (String(current) === String(option.value) && current !== "") {
                    item.selected = true;
                    matched = true;
                }
                parent.appendChild(item);
            });
            return matched;
        }

        // A dropdown whose value can also be typed ("Custom value…").
        // strict: only the listed values. The element holding the value carries
        // data-config-key (select when strict, text input otherwise).
        function combo(opts) {
            var options = opts.options || [];
            var current = opts.value == null ? "" : String(opts.value);
            var wrap = make("div", "automation-combo");
            wrap.setAttribute("data-combo-key", opts.key || "");
            var select = make("select", "form-select");
            var empty = make("option", null, opts.placeholder || t("builderChoose", "Choose…"));
            empty.value = "";
            select.appendChild(empty);
            var matched = addOptions(select, options, current);

            if (opts.strict) {
                if (current !== "" && !matched) {
                    var keep = make("option", null, current);
                    keep.value = current;
                    keep.selected = true;
                    select.appendChild(keep);
                }
                if (opts.key) { select.setAttribute("data-config-key", opts.key); }
                wrap.appendChild(select);
                return { el: wrap, control: select };
            }

            var custom = make("option", null, t("builderCustomValue", "Custom value…"));
            custom.value = CUSTOM;
            select.appendChild(custom);
            var input = make("input", "form-control mt-1");
            input.type = "text";
            input.value = current;
            if (opts.key) { input.setAttribute("data-config-key", opts.key); }
            var isCustom = current !== "" && !matched;
            if (isCustom) { select.value = CUSTOM; }
            input.hidden = !isCustom;

            select.addEventListener("change", function () {
                if (select.value === CUSTOM) {
                    input.hidden = false;
                    input.focus();
                } else {
                    input.value = select.value;
                    input.hidden = true;
                }
                if (opts.onChange) { opts.onChange(input.value); }
            });
            input.addEventListener("input", function () {
                if (opts.onChange) { opts.onChange(input.value); }
            });
            wrap.appendChild(select);
            wrap.appendChild(input);
            return { el: wrap, control: input, get: function () { return input.value; } };
        }

        // "Insert variable…" helper under a text box: appends the token at the cursor.
        function insertVariable(target, options) {
            var select = make("select", "form-select form-select-sm mt-1 automation-insert-var");
            var first = make("option", null, t("builderInsertVariable", "Insert variable…"));
            first.value = "";
            select.appendChild(first);
            addOptions(select, options, "");
            select.addEventListener("change", function () {
                var token = select.value;
                if (!token) { return; }
                var start = typeof target.selectionStart === "number" ? target.selectionStart : target.value.length;
                var end = typeof target.selectionEnd === "number" ? target.selectionEnd : target.value.length;
                target.value = target.value.slice(0, start) + token + target.value.slice(end);
                var caret = start + token.length;
                try { target.setSelectionRange(caret, caret); } catch (e) { /* not a text control */ }
                select.value = "";
                target.dispatchEvent(new Event("input", { bubbles: true }));
                target.dispatchEvent(new Event("change", { bubbles: true }));
            });
            return select;
        }

        function parsePairs(value) {
            var rows = [];
            if (!value) { return rows; }
            if (Array.isArray(value)) {
                value.forEach(function (item) { rows.push([item.field || "", item.value || ""]); });
            } else {
                Object.keys(value).forEach(function (key) { rows.push([key, value[key]]); });
            }
            return rows;
        }

        // Rows of [field dropdown][value dropdown]. The hidden input carries
        // the "field=value" lines builder.js already knows how to save.
        function pairs(opts) {
            var wrap = make("div", "automation-pairs");
            var hidden = make("input");
            hidden.type = "hidden";
            hidden.setAttribute("data-config-key", opts.key);
            wrap.appendChild(hidden);
            var list = make("div", "automation-pairs-list");
            wrap.appendChild(list);
            var fieldOptions = opts.fieldOptions || [];

            function serialize() {
                var lines = [];
                Array.prototype.forEach.call(list.children, function (row) {
                    var field = row.querySelector(".automation-pair-field").value;
                    var value = row.querySelector(".automation-pair-value input").value;
                    if (field) { lines.push(field + "=" + value); }
                });
                hidden.value = lines.join("\n");
            }

            function emit() {
                serialize();
                hidden.dispatchEvent(new Event("change", { bubbles: true }));
            }

            function addRow(field, value) {
                var row = make("div", "automation-pair");
                var fieldSelect = make("select", "form-select automation-pair-field");
                var placeholder = make("option", null, t("builderFieldPlaceholder", "Field…"));
                placeholder.value = "";
                fieldSelect.appendChild(placeholder);
                var known = addOptions(fieldSelect, fieldOptions, field);
                if (field && !known) {
                    var keep = make("option", null, field);
                    keep.value = field;
                    keep.selected = true;
                    fieldSelect.appendChild(keep);
                }
                fieldSelect.addEventListener("change", emit);
                var valueBox = combo({
                    options: opts.valueOptions || [],
                    value: value,
                    onChange: serialize
                });
                valueBox.el.className += " automation-pair-value";
                valueBox.el.addEventListener("change", function (event) {
                    event.stopPropagation();
                    emit();
                });
                var remove = make("button", "automation-pair-remove");
                remove.type = "button";
                remove.title = t("builderRemove", "Remove");
                remove.innerHTML = '<i class="bx bx-x" aria-hidden="true"></i>';
                remove.addEventListener("click", function () {
                    row.parentNode.removeChild(row);
                    emit();
                });
                row.appendChild(fieldSelect);
                row.appendChild(valueBox.el);
                row.appendChild(remove);
                list.appendChild(row);
            }

            parsePairs(opts.value).forEach(function (pair) { addRow(pair[0], pair[1]); });
            if (!list.children.length) { addRow("", ""); }
            serialize();

            var add = make("button", "automation-pair-add", "+ " + t("builderAddField", "Add field"));
            add.type = "button";
            add.addEventListener("click", function () { addRow("", ""); });
            wrap.appendChild(add);
            return { el: wrap, control: hidden };
        }

        return { combo: combo, insertVariable: insertVariable, pairs: pairs, CUSTOM: CUSTOM };
    };
})();
