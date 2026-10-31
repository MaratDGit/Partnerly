(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.reorder = {

        setup: function (ctx) {

            var options =
                ctx.options;

            var headerDragEnabled =
                options.columnReorderEnabled !== false;

            var table =
                ctx.root.querySelector("table");

            if (!table) {
                return;
            }

            var thead =
                table.querySelector("thead");

            if (!thead) {
                return;
            }

            var headerRow =
                thead.querySelector("tr");

            if (!headerRow) {
                return;
            }

            var tbody =
                ctx.body ||
                table.querySelector("tbody");

            var scroller =
                ctx.root.querySelector(
                    ".table-responsive"
                ) ||
                table.parentElement;


            var storageKey =
                "dataTable:" +
                String(options.id || "dataTable") +
                ":columnOrder";

            var dragThreshold = 6;
            var flipMs = 220;
            var reduceMotion =
                window.matchMedia &&
                window.matchMedia(
                    "(prefers-reduced-motion: reduce)"
                ).matches;


            /*
             * =========================================================
             * STORAGE
             * =========================================================
             */

            function canRemember() {

                return (
                    options.rememberState !== false
                );
            }


            function loadSavedOrder() {

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
                        !Array.isArray(parsed)
                    ) {
                        return null;
                    }

                    return parsed.map(
                        function (field) {
                            return String(field);
                        }
                    );

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to load column order.",
                        error
                    );

                    return null;
                }
            }


            function saveOrder(fields) {

                if (!canRemember()) {
                    return;
                }

                try {

                    window.localStorage.setItem(
                        storageKey,
                        JSON.stringify(fields)
                    );

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to save column order.",
                        error
                    );
                }
            }


            /*
             * =========================================================
             * HELPERS
             * =========================================================
             */

            function fieldSelector(field) {

                return (
                    '[data-field="' +
                    String(field)
                        .replace(/\\/g, "\\\\")
                        .replace(/"/g, '\\"') +
                    '"]'
                );
            }


            function getColumnConfig(field) {

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


            function isActionsField(field) {

                return String(field) === "__actions";
            }


            function isReorderableHeader(header) {

                if (!header) {
                    return false;
                }

                var field =
                    header.getAttribute("data-field");

                if (
                    !field ||
                    isActionsField(field)
                ) {
                    return false;
                }

                var config =
                    getColumnConfig(field);

                if (
                    config &&
                    config.reorderable === false
                ) {
                    return false;
                }

                if (
                    header.classList.contains(
                        "data-table-col-hidden"
                    )
                ) {
                    return false;
                }

                return true;
            }


            function getHeaderCells() {

                return Array.prototype.slice.call(
                    headerRow.querySelectorAll("th")
                );
            }


            function getActionsHeader() {

                return headerRow.querySelector(
                    'th[data-field="__actions"]'
                );
            }


            function reorderArrayByFields(items, savedFields) {

                if (
                    !Array.isArray(items) ||
                    !savedFields ||
                    !savedFields.length
                ) {
                    return items;
                }

                var byField = {};
                var used = {};
                var next = [];

                for (
                    var i = 0;
                    i < items.length;
                    i++
                ) {

                    var item =
                        items[i];

                    if (
                        !item ||
                        item.field === null ||
                        item.field === undefined
                    ) {
                        continue;
                    }

                    byField[String(item.field)] =
                        item;
                }

                for (
                    var s = 0;
                    s < savedFields.length;
                    s++
                ) {

                    var key =
                        String(savedFields[s]);

                    if (
                        byField[key] &&
                        !used[key]
                    ) {

                        next.push(byField[key]);
                        used[key] = true;
                    }
                }

                for (
                    var c = 0;
                    c < items.length;
                    c++
                ) {

                    var column =
                        items[c];

                    if (!column) {
                        continue;
                    }

                    var field =
                        String(column.field);

                    if (!used[field]) {

                        next.push(column);
                        used[field] = true;
                    }
                }

                return next;
            }


            function collectVisibleFields() {

                var fields = [];
                var headers =
                    getHeaderCells();

                for (
                    var i = 0;
                    i < headers.length;
                    i++
                ) {

                    var field =
                        headers[i].getAttribute(
                            "data-field"
                        );

                    if (
                        !field ||
                        isActionsField(field)
                    ) {
                        continue;
                    }

                    fields.push(String(field));
                }

                return fields;
            }


            function applyHeaderOrder(savedFields) {

                var actions =
                    getActionsHeader();

                var byField = {};
                var current = [];
                var headers =
                    getHeaderCells();

                for (
                    var i = 0;
                    i < headers.length;
                    i++
                ) {

                    var field =
                        headers[i].getAttribute(
                            "data-field"
                        );

                    if (
                        !field ||
                        isActionsField(field)
                    ) {
                        continue;
                    }

                    current.push(field);
                    byField[field] =
                        headers[i];
                }

                var used = {};
                var next = [];

                if (
                    savedFields &&
                    savedFields.length
                ) {

                    for (
                        var s = 0;
                        s < savedFields.length;
                        s++
                    ) {

                        var key =
                            String(savedFields[s]);

                        if (
                            byField[key] &&
                            !used[key]
                        ) {

                            next.push(key);
                            used[key] = true;
                        }
                    }
                }

                for (
                    var c = 0;
                    c < current.length;
                    c++
                ) {

                    if (!used[current[c]]) {
                        next.push(current[c]);
                    }
                }

                for (
                    var n = 0;
                    n < next.length;
                    n++
                ) {

                    headerRow.insertBefore(
                        byField[next[n]],
                        actions
                    );
                }

                var colGroup =
                    table.querySelector(
                        "colgroup[data-table-resize-colgroup]"
                    );

                if (colGroup) {

                    var actionsCol =
                        colGroup.querySelector(
                            'col[data-field="__actions"]'
                        );

                    for (
                        var k = 0;
                        k < next.length;
                        k++
                    ) {

                        var col =
                            colGroup.querySelector(
                                "col" +
                                fieldSelector(
                                    next[k]
                                )
                            );

                        if (col) {

                            colGroup.insertBefore(
                                col,
                                actionsCol
                            );
                        }
                    }
                }
            }


            function syncOptionsFromDom() {

                if (
                    !options.columns ||
                    !Array.isArray(options.columns)
                ) {
                    return;
                }

                options.columns =
                    reorderArrayByFields(
                        options.columns,
                        collectVisibleFields()
                    );
            }


            function markReorderableHeaders() {

                var headers =
                    getHeaderCells();

                for (
                    var i = 0;
                    i < headers.length;
                    i++
                ) {

                    var header =
                        headers[i];

                    header.classList.toggle(
                        "data-table-col-reorderable",
                        isReorderableHeader(header)
                    );
                }
            }


            function getHeaderTitle(header) {

                var titleSpan =
                    header.querySelector(
                        ".data-table-sort > span:not(.data-table-sort-icon):not(.data-table-column-filter-icon)"
                    );

                if (titleSpan) {
                    return (
                        titleSpan.textContent || ""
                    ).trim();
                }

                if (
                    header.querySelector(
                        ".data-table-select-all"
                    )
                ) {
                    return "";
                }

                var clone =
                    header.cloneNode(true);

                var extras =
                    clone.querySelectorAll(
                        ".data-table-column-resizer, .data-table-sort-icon, .data-table-column-filter-icon"
                    );

                for (
                    var i = 0;
                    i < extras.length;
                    i++
                ) {
                    extras[i].remove();
                }

                return (
                    clone.textContent || ""
                ).trim();
            }


            function collectAnimatedCells() {

                var cells = [];
                var headers =
                    getHeaderCells();

                for (
                    var i = 0;
                    i < headers.length;
                    i++
                ) {
                    cells.push(headers[i]);
                }

                if (!tbody) {
                    return cells;
                }

                var rows =
                    tbody.querySelectorAll("tr");

                for (
                    var r = 0;
                    r < rows.length;
                    r++
                ) {

                    var rowCells =
                        rows[r].children;

                    for (
                        var c = 0;
                        c < rowCells.length;
                        c++
                    ) {
                        cells.push(rowCells[c]);
                    }
                }

                return cells;
            }


            function measureLefts(elements) {

                var lefts = [];

                for (
                    var i = 0;
                    i < elements.length;
                    i++
                ) {

                    lefts.push(
                        elements[i]
                            .getBoundingClientRect()
                            .left
                    );
                }

                return lefts;
            }


            function playFlip(elements, firstLefts) {

                if (
                    reduceMotion ||
                    !elements.length
                ) {
                    return;
                }

                var inverted = [];

                for (
                    var i = 0;
                    i < elements.length;
                    i++
                ) {

                    var el =
                        elements[i];

                    var dx =
                        firstLefts[i] -
                        el.getBoundingClientRect()
                            .left;

                    if (Math.abs(dx) < 1) {
                        continue;
                    }

                    el.classList.add(
                        "data-table-col-sliding"
                    );

                    el.style.transition =
                        "none";

                    el.style.transform =
                        "translateX(" +
                        dx +
                        "px)";

                    inverted.push(el);
                }

                if (!inverted.length) {
                    return;
                }

                void table.offsetWidth;

                window.requestAnimationFrame(
                    function () {

                        for (
                            var i = 0;
                            i < inverted.length;
                            i++
                        ) {

                            inverted[i].style.transition =
                                "transform " +
                                flipMs +
                                "ms cubic-bezier(0.22, 1, 0.36, 1)";

                            inverted[i].style.transform =
                                "";
                        }
                    }
                );

                window.setTimeout(
                    function () {

                        for (
                            var i = 0;
                            i < inverted.length;
                            i++
                        ) {

                            inverted[i].classList.remove(
                                "data-table-col-sliding"
                            );

                            inverted[i].style.transition =
                                "";

                            inverted[i].style.transform =
                                "";
                        }
                    },
                    flipMs + 40
                );
            }


            function setDraggingCells(field, on) {

                var selector =
                    fieldSelector(field);

                var nodes =
                    table.querySelectorAll(
                        "th" + selector +
                        ", td" + selector
                    );

                for (
                    var i = 0;
                    i < nodes.length;
                    i++
                ) {

                    nodes[i].classList.toggle(
                        "is-column-dragging",
                        on
                    );
                }
            }


            function getLayoutRect(el) {

                var rect =
                    el.getBoundingClientRect();

                var left =
                    rect.left;

                var transform =
                    el.style.transform;

                if (
                    transform &&
                    transform.indexOf("translateX") !== -1
                ) {

                    var dx =
                        parseFloat(
                            transform.replace(
                                /.*translateX\(/,
                                ""
                            )
                        );

                    if (!isNaN(dx)) {
                        left -= dx;
                    }
                }

                return {
                    left: left,
                    width: rect.width,
                    right: left + rect.width
                };
            }


            function getInsertBeforeCell(row, beforeHeader) {

                if (!beforeHeader) {
                    return null;
                }

                var field =
                    beforeHeader.getAttribute(
                        "data-field"
                    );

                if (isActionsField(field)) {

                    return row.querySelector(
                        "td.data-table-actions, td[data-field='__actions']"
                    );
                }

                return row.querySelector(
                    "td" + fieldSelector(field)
                );
            }


            function moveFieldBefore(field, beforeHeader) {

                var sourceHeader =
                    headerRow.querySelector(
                        "th" + fieldSelector(field)
                    );

                if (!sourceHeader) {
                    return false;
                }

                var beforeNode =
                    beforeHeader || null;

                if (sourceHeader === beforeNode) {
                    return false;
                }

                if (
                    sourceHeader.nextElementSibling ===
                    beforeNode
                ) {
                    return false;
                }

                var animated =
                    collectAnimatedCells();

                var firstLefts =
                    measureLefts(animated);

                headerRow.insertBefore(
                    sourceHeader,
                    beforeNode
                );

                var colGroup =
                    table.querySelector(
                        "colgroup[data-table-resize-colgroup]"
                    );

                if (colGroup) {

                    var sourceCol =
                        colGroup.querySelector(
                            "col" + fieldSelector(field)
                        );

                    var beforeCol = null;

                    if (beforeNode) {

                        beforeCol =
                            colGroup.querySelector(
                                "col" +
                                fieldSelector(
                                    beforeNode.getAttribute(
                                        "data-field"
                                    )
                                )
                            );
                    }

                    if (sourceCol) {

                        colGroup.insertBefore(
                            sourceCol,
                            beforeCol
                        );
                    }
                }

                if (tbody) {

                    var rows =
                        tbody.querySelectorAll("tr");

                    for (
                        var r = 0;
                        r < rows.length;
                        r++
                    ) {

                        var row =
                            rows[r];

                        var td =
                            row.querySelector(
                                "td" + fieldSelector(field)
                            );

                        if (!td) {
                            continue;
                        }

                        row.insertBefore(
                            td,
                            getInsertBeforeCell(
                                row,
                                beforeNode
                            )
                        );
                    }
                }

                playFlip(
                    animated,
                    firstLefts
                );

                return true;
            }


            function findDropTarget(clientX, dragField) {

                var headers =
                    getHeaderCells();

                var actions =
                    getActionsHeader();

                var columns = [];

                for (
                    var i = 0;
                    i < headers.length;
                    i++
                ) {

                    var field =
                        headers[i].getAttribute(
                            "data-field"
                        );

                    if (
                        !field ||
                        isActionsField(field)
                    ) {
                        continue;
                    }

                    if (
                        headers[i].classList.contains(
                            "data-table-col-hidden"
                        )
                    ) {
                        continue;
                    }

                    columns.push(headers[i]);
                }

                if (actions) {

                    var actionsRect =
                        getLayoutRect(actions);

                    if (
                        clientX >=
                        actionsRect.left - 8
                    ) {
                        return actions;
                    }
                }

                for (
                    var c = 0;
                    c < columns.length;
                    c++
                ) {

                    var rect =
                        getLayoutRect(
                            columns[c]
                        );

                    /*
                     * Last data column: start the
                     * "before Actions" slot earlier
                     * so a wide last column cannot
                     * swallow the drop zone.
                     */

                    var threshold =
                        c === columns.length - 1
                            ? rect.left +
                              rect.width * 0.35
                            : rect.left +
                              rect.width / 2;

                    if (clientX < threshold) {
                        return columns[c];
                    }
                }

                return actions;
            }


            /*
             * =========================================================
             * RESTORE SAVED ORDER
             * =========================================================
             */

            var savedOrder =
                loadSavedOrder();

            if (savedOrder) {

                if (
                    options.columns &&
                    Array.isArray(options.columns)
                ) {

                    options.columns =
                        reorderArrayByFields(
                            options.columns,
                            savedOrder
                        );
                }

                applyHeaderOrder(savedOrder);
            }

            if (headerDragEnabled) {

                markReorderableHeaders();
            }


            ctx.applyColumnOrder =
                function (fields) {

                    if (
                        !fields ||
                        !fields.length
                    ) {
                        return;
                    }

                    if (
                        options.columns &&
                        Array.isArray(options.columns)
                    ) {

                        options.columns =
                            reorderArrayByFields(
                                options.columns,
                                fields
                            );
                    }

                    applyHeaderOrder(
                        fields
                    );
                };


            ctx.saveColumnOrder =
                function () {

                    if (
                        !options.columns ||
                        !Array.isArray(options.columns)
                    ) {
                        return;
                    }

                    saveOrder(
                        options.columns.map(
                            function (column) {
                                return String(
                                    column.field
                                );
                            }
                        )
                    );
                };


            var reorderableCount = 0;
            var headersForBind =
                getHeaderCells();

            for (
                var h = 0;
                h < headersForBind.length;
                h++
            ) {

                if (
                    isReorderableHeader(
                        headersForBind[h]
                    )
                ) {
                    reorderableCount += 1;
                }
            }

            if (
                !headerDragEnabled ||
                reorderableCount < 2
            ) {
                return;
            }


            /*
             * =========================================================
             * DRAG
             * =========================================================
             */

            var drag = null;
            var suppressClick = false;
            var scrollFrame = 0;
            var lastClientX = 0;


            function stopAutoScroll() {

                if (scrollFrame) {

                    window.cancelAnimationFrame(
                        scrollFrame
                    );

                    scrollFrame = 0;
                }
            }


            function tickAutoScroll() {

                scrollFrame = 0;

                if (
                    !drag ||
                    !drag.active
                ) {
                    return;
                }

                autoScroll(lastClientX);
                applyDrop(lastClientX);

                scrollFrame =
                    window.requestAnimationFrame(
                        tickAutoScroll
                    );
            }


            function startAutoScroll() {

                if (scrollFrame) {
                    return;
                }

                scrollFrame =
                    window.requestAnimationFrame(
                        tickAutoScroll
                    );
            }


            function shouldIgnoreTarget(target) {

                if (!target || !target.closest) {
                    return true;
                }

                return Boolean(
                    target.closest(
                        "[data-column-resizer], input, select, textarea, a, .data-table-column-filter-icon, .data-table-select-all"
                    )
                );
            }


            function createGhost(header, clientX, clientY) {

                var rect =
                    header.getBoundingClientRect();

                var ghost =
                    document.createElement("div");

                ghost.className =
                    "data-table-column-ghost";

                ghost.setAttribute(
                    "aria-hidden",
                    "true"
                );

                var inner =
                    document.createElement("div");

                inner.className =
                    "data-table-column-ghost-inner";

                inner.style.minWidth =
                    Math.max(
                        88,
                        Math.round(rect.width)
                    ) + "px";

                inner.style.height =
                    Math.max(
                        34,
                        Math.round(rect.height)
                    ) + "px";

                var handle =
                    document.createElement("span");

                handle.className =
                    "data-table-column-ghost-handle";

                var title =
                    document.createElement("span");

                title.className =
                    "data-table-column-ghost-title";

                title.textContent =
                    getHeaderTitle(header) ||
                    header.getAttribute("data-field") ||
                    "";

                inner.appendChild(handle);
                inner.appendChild(title);
                ghost.appendChild(inner);
                document.body.appendChild(ghost);

                var ghostState = {
                    el: ghost,
                    offsetX:
                        clientX - rect.left,
                    offsetY:
                        clientY - rect.top,
                    headerTop:
                        rect.top
                };

                positionGhost(
                    ghostState,
                    clientX,
                    clientY
                );

                return ghostState;
            }


            function positionGhost(ghostState, clientX, clientY) {

                if (
                    !ghostState ||
                    !ghostState.el
                ) {
                    return;
                }

                var x =
                    clientX -
                    ghostState.offsetX;

                var y =
                    ghostState.headerTop - 10;

                if (
                    typeof clientY === "number"
                ) {

                    var lift =
                        Math.max(
                            -18,
                            Math.min(
                                18,
                                clientY -
                                ghostState.headerTop -
                                ghostState.offsetY
                            )
                        );

                    y =
                        ghostState.headerTop -
                        10 +
                        lift;
                }

                ghostState.el.style.left =
                    x + "px";

                ghostState.el.style.top =
                    y + "px";
            }


            function removeGhost() {

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


            function autoScroll(clientX) {

                if (!scroller) {
                    return;
                }

                var rect =
                    scroller.getBoundingClientRect();

                var edge = 56;
                var step = 22;

                if (
                    clientX <
                    rect.left + edge
                ) {

                    scroller.scrollLeft -=
                        step;
                }
                else if (
                    clientX >
                    rect.right - edge
                ) {

                    scroller.scrollLeft +=
                        step;
                }
            }


            function applyDrop(clientX) {

                if (!drag) {
                    return;
                }

                var target =
                    findDropTarget(
                        clientX,
                        drag.field
                    );

                moveFieldBefore(
                    drag.field,
                    target
                );
            }


            function finishDrag(moved) {

                if (!drag) {
                    return;
                }

                stopAutoScroll();
                removeGhost();

                setDraggingCells(
                    drag.field,
                    false
                );

                document.body.classList.remove(
                    "data-table-column-reordering"
                );

                if (
                    drag.header &&
                    drag.pointerId !== null &&
                    drag.header.releasePointerCapture
                ) {

                    try {

                        drag.header.releasePointerCapture(
                            drag.pointerId
                        );

                    }
                    catch (error) {
                        /* already released */
                    }
                }

                document.removeEventListener(
                    "pointermove",
                    onPointerMove
                );

                document.removeEventListener(
                    "pointerup",
                    onPointerUp
                );

                document.removeEventListener(
                    "pointercancel",
                    onPointerUp
                );

                if (moved) {

                    syncOptionsFromDom();

                    saveOrder(
                        options.columns.map(
                            function (column) {
                                return String(
                                    column.field
                                );
                            }
                        )
                    );

                    suppressClick = true;

                    window.setTimeout(
                        function () {
                            suppressClick = false;
                        },
                        0
                    );
                }

                drag = null;
            }


            function onPointerMove(event) {

                if (!drag) {
                    return;
                }

                if (
                    drag.pointerId !== null &&
                    event.pointerId !==
                    drag.pointerId
                ) {
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

                    document.body.classList.add(
                        "data-table-column-reordering"
                    );

                    setDraggingCells(
                        drag.field,
                        true
                    );

                    drag.ghost =
                        createGhost(
                            drag.header,
                            drag.startX,
                            drag.startY
                        );

                    if (
                        drag.header.setPointerCapture
                    ) {

                        try {

                            drag.header.setPointerCapture(
                                drag.pointerId
                            );

                        }
                        catch (error) {
                            /* ignore */
                        }
                    }
                }

                event.preventDefault();

                positionGhost(
                    drag.ghost,
                    event.clientX,
                    event.clientY
                );

                lastClientX =
                    event.clientX;

                autoScroll(event.clientX);
                applyDrop(event.clientX);
                startAutoScroll();
            }


            function onPointerUp(event) {

                if (!drag) {
                    return;
                }

                if (
                    drag.pointerId !== null &&
                    event.pointerId !==
                    drag.pointerId
                ) {
                    return;
                }

                var moved =
                    drag.active;

                if (moved) {
                    event.preventDefault();
                }

                finishDrag(moved);
            }


            headerRow.addEventListener(
                "pointerdown",
                function (event) {

                    if (event.button !== 0) {
                        return;
                    }

                    if (
                        document.body.classList.contains(
                            "data-table-column-resizing"
                        )
                    ) {
                        return;
                    }

                    if (shouldIgnoreTarget(event.target)) {
                        return;
                    }

                    var header =
                        event.target.closest("th");

                    if (
                        !header ||
                        !headerRow.contains(header) ||
                        !isReorderableHeader(header)
                    ) {
                        return;
                    }

                    var field =
                        header.getAttribute(
                            "data-field"
                        );

                    if (!field) {
                        return;
                    }

                    drag = {
                        header: header,
                        field: String(field),
                        startX: event.clientX,
                        startY: event.clientY,
                        pointerId: event.pointerId,
                        active: false,
                        ghost: null
                    };

                    document.addEventListener(
                        "pointermove",
                        onPointerMove
                    );

                    document.addEventListener(
                        "pointerup",
                        onPointerUp
                    );

                    document.addEventListener(
                        "pointercancel",
                        onPointerUp
                    );
                }
            );


            headerRow.addEventListener(
                "click",
                function (event) {

                    if (!suppressClick) {
                        return;
                    }

                    event.preventDefault();
                    event.stopPropagation();
                    event.stopImmediatePropagation();
                },
                true
            );


            ctx.resetColumnOrder =
                function () {

                    try {

                        window.localStorage.removeItem(
                            storageKey
                        );

                    }
                    catch (error) {

                        console.warn(
                            "DataTable: unable to reset column order.",
                            error
                        );
                    }

                    window.location.reload();
                };
        }
    };

})(window, document);
