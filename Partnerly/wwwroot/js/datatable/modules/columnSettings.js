(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.columnSettings = {

        setup: function (ctx) {

            var options =
                ctx.options;

            if (
                options.columnSettingsEnabled === false
            ) {
                return;
            }

            var button =
                ctx.root.querySelector(
                    "[data-table-column-settings]"
                );

            var panel =
                ctx.root.querySelector(
                    "[data-table-columns-panel]"
                );

            var list =
                ctx.root.querySelector(
                    "[data-table-columns-list]"
                );

            if (
                !button ||
                !panel ||
                !list
            ) {
                return;
            }

            var closeButton =
                panel.querySelector(
                    "[data-table-columns-close]"
                );

            var resetButton =
                panel.querySelector(
                    "[data-table-columns-reset]"
                );

            var storageKey =
                "dataTable:" +
                String(options.id || "dataTable") +
                ":columnVisibility";

            var dragThreshold = 6;


            function canRemember() {

                return (
                    options.rememberState !== false
                );
            }


            function loadSavedVisibility() {

                if (!canRemember()) {
                    return null;
                }

                try {

                    var value =
                        window.localStorage.getItem(
                            storageKey
                        );

                    if (!value) {
                        return null;
                    }

                    var parsed =
                        JSON.parse(value);

                    if (
                        !parsed ||
                        typeof parsed !== "object"
                    ) {
                        return null;
                    }

                    return parsed;

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to load column visibility.",
                        error
                    );

                    return null;
                }
            }


            function saveVisibility() {

                if (!canRemember()) {
                    return;
                }

                if (
                    !options.columns ||
                    !Array.isArray(options.columns)
                ) {
                    return;
                }

                var map = {};

                for (
                    var i = 0;
                    i < options.columns.length;
                    i++
                ) {

                    var column =
                        options.columns[i];

                    if (
                        !column ||
                        column.field == null
                    ) {
                        continue;
                    }

                    map[String(column.field)] =
                        column.visible !== false;
                }

                try {

                    window.localStorage.setItem(
                        storageKey,
                        JSON.stringify(map)
                    );

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to save column visibility.",
                        error
                    );
                }
            }


            function clearVisibility() {

                try {

                    window.localStorage.removeItem(
                        storageKey
                    );

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to reset column visibility.",
                        error
                    );
                }
            }


            function visibleCount() {

                var count = 0;

                if (
                    !options.columns ||
                    !Array.isArray(options.columns)
                ) {
                    return 0;
                }

                for (
                    var i = 0;
                    i < options.columns.length;
                    i++
                ) {

                    if (
                        options.columns[i] &&
                        options.columns[i].visible
                    ) {
                        count += 1;
                    }
                }

                return count;
            }


            function getColumn(field) {

                if (
                    !options.columns ||
                    !Array.isArray(options.columns)
                ) {
                    return null;
                }

                for (
                    var i = 0;
                    i < options.columns.length;
                    i++
                ) {

                    var column =
                        options.columns[i];

                    if (
                        column &&
                        String(column.field) ===
                        String(field)
                    ) {
                        return column;
                    }
                }

                return null;
            }


            function applyVisibilityToDom() {

                if (
                    !options.columns ||
                    !Array.isArray(options.columns)
                ) {
                    return;
                }

                for (
                    var i = 0;
                    i < options.columns.length;
                    i++
                ) {

                    var column =
                        options.columns[i];

                    if (
                        !column ||
                        column.field == null
                    ) {
                        continue;
                    }

                    var hidden =
                        column.visible === false;

                    var nodes =
                        ctx.root.querySelectorAll(
                            '[data-field="' +
                            String(column.field)
                                .replace(/\\/g, "\\\\")
                                .replace(/"/g, '\\"') +
                            '"]'
                        );

                    for (
                        var n = 0;
                        n < nodes.length;
                        n++
                    ) {

                        var tag =
                            nodes[n].tagName;

                        if (
                            tag !== "TH" &&
                            tag !== "COL"
                        ) {
                            continue;
                        }

                        nodes[n].classList.toggle(
                            "data-table-col-hidden",
                            hidden
                        );
                    }
                }

                if (
                    typeof ctx.refreshColumnCount ===
                    "function"
                ) {
                    ctx.refreshColumnCount();
                }
            }


            function refreshRows() {

                if (
                    typeof ctx.renderRows ===
                        "function" &&
                    ctx.lastItems
                ) {

                    ctx.renderRows(
                        ctx.lastItems
                    );
                }

                if (
                    typeof ctx.updateFilterColumnIndicators ===
                    "function"
                ) {
                    ctx.updateFilterColumnIndicators();
                }

                if (
                    typeof ctx.updateSortIcons ===
                    "function"
                ) {
                    ctx.updateSortIcons();
                }
            }


            function applySavedVisibility() {

                var saved =
                    loadSavedVisibility();

                if (
                    !saved ||
                    !options.columns
                ) {
                    applyVisibilityToDom();
                    return;
                }

                for (
                    var i = 0;
                    i < options.columns.length;
                    i++
                ) {

                    var column =
                        options.columns[i];

                    if (
                        !column ||
                        column.field == null
                    ) {
                        continue;
                    }

                    var key =
                        String(column.field);

                    if (
                        Object.prototype.hasOwnProperty.call(
                            saved,
                            key
                        )
                    ) {
                        column.visible =
                            saved[key] !== false;
                    }
                }

                if (visibleCount() === 0) {

                    for (
                        var c = 0;
                        c < options.columns.length;
                        c++
                    ) {

                        if (options.columns[c]) {
                            options.columns[c].visible =
                                true;
                            break;
                        }
                    }
                }

                applyVisibilityToDom();
            }


            function columnTitle(column) {

                if (
                    column &&
                    column.title
                ) {
                    return String(column.title);
                }

                if (
                    column &&
                    column.field
                ) {
                    return String(column.field);
                }

                return "";
            }


            function renderList() {

                list.innerHTML = "";

                if (
                    !options.columns ||
                    !options.columns.length
                ) {
                    return;
                }

                for (
                    var i = 0;
                    i < options.columns.length;
                    i++
                ) {

                    var column =
                        options.columns[i];

                    if (
                        !column ||
                        column.field == null
                    ) {
                        continue;
                    }

                    var item =
                        document.createElement("div");

                    item.className =
                        "data-table-columns-item";

                    item.setAttribute(
                        "data-field",
                        String(column.field)
                    );

                    if (column.reorderable !== false) {
                        item.classList.add(
                            "is-reorderable"
                        );
                    }

                    var handle =
                        document.createElement("span");

                    handle.className =
                        "data-table-columns-item-handle";

                    handle.setAttribute(
                        "aria-hidden",
                        "true"
                    );

                    var label =
                        document.createElement("label");

                    label.className =
                        "data-table-columns-item-label";

                    var checkbox =
                        document.createElement("input");

                    checkbox.type =
                        "checkbox";

                    checkbox.checked =
                        column.visible !== false;

                    checkbox.setAttribute(
                        "data-field",
                        String(column.field)
                    );

                    var text =
                        document.createElement("span");

                    text.className =
                        "data-table-columns-item-title";

                    text.textContent =
                        columnTitle(column);

                    label.appendChild(checkbox);
                    label.appendChild(text);
                    item.appendChild(handle);
                    item.appendChild(label);
                    list.appendChild(item);
                }
            }


            function isOpen() {

                return !panel.hasAttribute("hidden");
            }


            function openPanel() {

                renderList();
                panel.removeAttribute("hidden");
                button.classList.add("is-open");
                button.setAttribute(
                    "aria-expanded",
                    "true"
                );
            }


            function closePanel() {

                panel.setAttribute("hidden", "");
                button.classList.remove("is-open");
                button.setAttribute(
                    "aria-expanded",
                    "false"
                );
            }


            function togglePanel() {

                if (isOpen()) {
                    closePanel();
                }
                else {
                    openPanel();
                }
            }


            function setColumnVisible(field, visible) {

                var column =
                    getColumn(field);

                if (!column) {
                    return;
                }

                if (
                    !visible &&
                    visibleCount() <= 1 &&
                    column.visible
                ) {
                    return;
                }

                column.visible =
                    visible;

                applyVisibilityToDom();
                saveVisibility();
                refreshRows();
            }


            function applyListOrder() {

                var items =
                    list.querySelectorAll(
                        ".data-table-columns-item[data-field]"
                    );

                var fields = [];

                for (
                    var i = 0;
                    i < items.length;
                    i++
                ) {

                    fields.push(
                        String(
                            items[i].getAttribute(
                                "data-field"
                            )
                        )
                    );
                }

                if (
                    typeof ctx.applyColumnOrder ===
                    "function"
                ) {
                    ctx.applyColumnOrder(fields);
                }

                if (
                    typeof ctx.saveColumnOrder ===
                    "function"
                ) {
                    ctx.saveColumnOrder();
                }

                refreshRows();
            }


            function restoreDefaults() {

                var layout =
                    ctx.defaultColumnLayout || [];

                var fields = [];
                var visibleMap = {};

                for (
                    var i = 0;
                    i < layout.length;
                    i++
                ) {

                    var item =
                        layout[i];

                    if (
                        !item ||
                        !item.field
                    ) {
                        continue;
                    }

                    fields.push(item.field);
                    visibleMap[item.field] =
                        item.visible !== false;
                }

                if (
                    options.columns &&
                    Array.isArray(options.columns)
                ) {

                    var byField = {};

                    for (
                        var c = 0;
                        c < options.columns.length;
                        c++
                    ) {

                        var column =
                            options.columns[c];

                        if (
                            column &&
                            column.field != null
                        ) {
                            byField[String(column.field)] =
                                column;
                        }
                    }

                    var next = [];
                    var used = {};

                    for (
                        var f = 0;
                        f < fields.length;
                        f++
                    ) {

                        var key =
                            fields[f];

                        if (
                            byField[key] &&
                            !used[key]
                        ) {

                            byField[key].visible =
                                visibleMap[key] !== false;

                            next.push(byField[key]);
                            used[key] = true;
                        }
                    }

                    for (
                        var r = 0;
                        r < options.columns.length;
                        r++
                    ) {

                        var rest =
                            options.columns[r];

                        if (
                            !rest ||
                            rest.field == null
                        ) {
                            continue;
                        }

                        var restKey =
                            String(rest.field);

                        if (!used[restKey]) {
                            next.push(rest);
                        }
                    }

                    options.columns =
                        next;
                }

                if (
                    typeof ctx.applyColumnOrder ===
                    "function"
                ) {
                    ctx.applyColumnOrder(fields);
                }

                try {

                    window.localStorage.removeItem(
                        "dataTable:" +
                        String(options.id || "dataTable") +
                        ":columnOrder"
                    );

                }
                catch (error) {
                    /* ignore */
                }

                clearVisibility();
                applyVisibilityToDom();
                refreshRows();
                renderList();
            }


            /*
             * =========================================================
             * INIT VISIBILITY
             * =========================================================
             */

            applySavedVisibility();


            /*
             * =========================================================
             * PANEL DRAG
             * =========================================================
             */

            var drag = null;


            function findItem(target) {

                if (!target || !target.closest) {
                    return null;
                }

                return target.closest(
                    ".data-table-columns-item"
                );
            }


            function createItemGhost(item, clientX, clientY) {

                var rect =
                    item.getBoundingClientRect();

                var ghost =
                    document.createElement("div");

                ghost.className =
                    "data-table-columns-item-ghost";

                ghost.setAttribute(
                    "aria-hidden",
                    "true"
                );

                ghost.textContent =
                    (
                        item.querySelector(
                            ".data-table-columns-item-title"
                        ) || item
                    ).textContent;

                ghost.style.width =
                    Math.round(rect.width) + "px";

                document.body.appendChild(ghost);

                var state = {
                    el: ghost,
                    offsetX:
                        clientX - rect.left,
                    offsetY:
                        clientY - rect.top
                };

                positionItemGhost(
                    state,
                    clientX,
                    clientY
                );

                return state;
            }


            function positionItemGhost(ghostState, clientX, clientY) {

                if (
                    !ghostState ||
                    !ghostState.el
                ) {
                    return;
                }

                ghostState.el.style.left =
                    (clientX - ghostState.offsetX) +
                    "px";

                ghostState.el.style.top =
                    (clientY - ghostState.offsetY) +
                    "px";
            }


            function removeItemGhost() {

                if (
                    drag &&
                    drag.ghost &&
                    drag.ghost.el &&
                    drag.ghost.el.parentNode
                ) {

                    drag.ghost.el.parentNode.removeChild(
                        drag.ghost.el
                    );
                }
            }


            function listInsertBefore(clientY) {

                if (!drag || !drag.item) {
                    return;
                }

                var items =
                    list.querySelectorAll(
                        ".data-table-columns-item"
                    );

                var before = null;

                for (
                    var i = 0;
                    i < items.length;
                    i++
                ) {

                    var item =
                        items[i];

                    if (item === drag.item) {
                        continue;
                    }

                    var rect =
                        item.getBoundingClientRect();

                    if (
                        clientY <
                        rect.top +
                        rect.height / 2
                    ) {
                        before = item;
                        break;
                    }
                }

                if (
                    drag.item === before ||
                    drag.item.nextElementSibling ===
                    before
                ) {
                    return;
                }

                list.insertBefore(
                    drag.item,
                    before
                );
            }


            function finishItemDrag(moved) {

                if (!drag) {
                    return;
                }

                removeItemGhost();

                if (drag.item) {
                    drag.item.classList.remove(
                        "is-dragging"
                    );
                }

                document.body.classList.remove(
                    "data-table-columns-reordering"
                );

                document.removeEventListener(
                    "pointermove",
                    onItemMove
                );

                document.removeEventListener(
                    "pointerup",
                    onItemUp
                );

                document.removeEventListener(
                    "pointercancel",
                    onItemUp
                );

                if (moved) {
                    applyListOrder();
                }

                drag = null;
            }


            function onItemMove(event) {

                if (!drag) {
                    return;
                }

                var dx =
                    event.clientX -
                    drag.startX;

                var dy =
                    event.clientY -
                    drag.startY;

                if (!drag.active) {

                    if (
                        (dx * dx) + (dy * dy) <
                        dragThreshold * dragThreshold
                    ) {
                        return;
                    }

                    drag.active = true;
                    drag.item.classList.add(
                        "is-dragging"
                    );

                    document.body.classList.add(
                        "data-table-columns-reordering"
                    );

                    drag.ghost =
                        createItemGhost(
                            drag.item,
                            drag.startX,
                            drag.startY
                        );
                }

                event.preventDefault();

                positionItemGhost(
                    drag.ghost,
                    event.clientX,
                    event.clientY
                );

                listInsertBefore(
                    event.clientY
                );
            }


            function onItemUp(event) {

                if (!drag) {
                    return;
                }

                var moved =
                    drag.active;

                if (moved) {
                    event.preventDefault();
                }

                finishItemDrag(moved);
            }


            list.addEventListener(
                "pointerdown",
                function (event) {

                    if (event.button !== 0) {
                        return;
                    }

                    if (
                        event.target.closest(
                            "input, label"
                        )
                    ) {
                        return;
                    }

                    var item =
                        findItem(event.target);

                    if (
                        !item ||
                        !item.classList.contains(
                            "is-reorderable"
                        )
                    ) {
                        return;
                    }

                    drag = {
                        item: item,
                        startX: event.clientX,
                        startY: event.clientY,
                        active: false,
                        ghost: null
                    };

                    document.addEventListener(
                        "pointermove",
                        onItemMove
                    );

                    document.addEventListener(
                        "pointerup",
                        onItemUp
                    );

                    document.addEventListener(
                        "pointercancel",
                        onItemUp
                    );
                }
            );


            list.addEventListener(
                "change",
                function (event) {

                    var checkbox =
                        event.target;

                    if (
                        !checkbox ||
                        checkbox.type !== "checkbox"
                    ) {
                        return;
                    }

                    var field =
                        checkbox.getAttribute(
                            "data-field"
                        );

                    if (!field) {
                        return;
                    }

                    if (
                        !checkbox.checked &&
                        visibleCount() <= 1
                    ) {

                        checkbox.checked =
                            true;

                        return;
                    }

                    setColumnVisible(
                        field,
                        checkbox.checked
                    );
                }
            );


            button.addEventListener(
                "click",
                function (event) {

                    event.preventDefault();
                    event.stopPropagation();
                    togglePanel();
                }
            );


            if (closeButton) {

                closeButton.addEventListener(
                    "click",
                    function (event) {

                        event.preventDefault();
                        closePanel();
                    }
                );
            }


            if (resetButton) {

                resetButton.addEventListener(
                    "click",
                    function (event) {

                        event.preventDefault();
                        restoreDefaults();
                    }
                );
            }


            document.addEventListener(
                "click",
                function (event) {

                    if (!isOpen()) {
                        return;
                    }

                    if (
                        panel.contains(event.target) ||
                        button.contains(event.target)
                    ) {
                        return;
                    }

                    closePanel();
                }
            );


            document.addEventListener(
                "keydown",
                function (event) {

                    if (
                        event.key === "Escape" &&
                        isOpen()
                    ) {
                        closePanel();
                    }
                }
            );
        }
    };

})(window, document);
