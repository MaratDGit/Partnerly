(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.resize = {

        setup: function (ctx) {

            var options =
                ctx.options;

            /*
             * =========================================================
             * CHECK
             * =========================================================
             */

            if (
                options.columnResizeEnabled === false
            ) {
                return;
            }

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

            var scroller =
                ctx.root.querySelector(
                    ".table-responsive"
                ) ||
                table.parentElement;

            var headers =
                headerRow.querySelectorAll("th");

            if (!headers.length) {
                return;
            }


            /*
             * =========================================================
             * SETTINGS
             * =========================================================
             */

            var minWidth =
                parseInt(
                    options.columnResizeMinWidth,
                    10
                );

            if (
                isNaN(minWidth) ||
                minWidth < 20
            ) {
                minWidth = 60;
            }


            /*
             * =========================================================
             * STORAGE
             * =========================================================
             */

            var storageKey =
                "dataTable:" +
                String(options.id || "dataTable") +
                ":columnWidths";


            function canRemember() {

                return (
                    options.rememberState !== false
                );
            }


            function loadSavedWidths() {

                if (!canRemember()) {
                    return {};
                }

                try {

                    var value =
                        window.localStorage.getItem(
                            storageKey
                        );

                    if (!value) {
                        return {};
                    }

                    var parsed =
                        JSON.parse(value);

                    if (
                        !parsed ||
                        typeof parsed !== "object"
                    ) {
                        return {};
                    }

                    return parsed;

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to load column widths.",
                        error
                    );

                    return {};
                }
            }


            function saveWidths(widths) {

                if (!canRemember()) {
                    return;
                }

                try {

                    window.localStorage.setItem(
                        storageKey,
                        JSON.stringify(widths)
                    );

                }
                catch (error) {

                    console.warn(
                        "DataTable: unable to save column widths.",
                        error
                    );
                }
            }


            /*
             * =========================================================
             * HELPERS
             * =========================================================
             */

            function getHeaderField(
                header,
                index
            ) {

                var field =
                    header.getAttribute(
                        "data-field"
                    );

                if (field) {
                    return String(field);
                }

                return "__column_" + index;
            }


            function isSelectedField(
                field
            ) {

                return String(field || "")
                    .toLowerCase() ===
                    "selected";
            }


            function isColumnHidden(
                column
            ) {

                return Boolean(
                    column.header &&
                    column.header.classList.contains(
                        "data-table-col-hidden"
                    )
                );
            }


            function getColumnConfig(
                field
            ) {

                if (
                    options.columns &&
                    Array.isArray(options.columns)
                ) {

                    for (
                        var i = 0;
                        i < options.columns.length;
                        i++
                    ) {

                        var column =
                            options.columns[i];

                        if (!column) {
                            continue;
                        }

                        if (
                            String(column.field) ===
                            String(field)
                        ) {
                            return column;
                        }
                    }
                }

                /*
                 * Actions column.
                 */

                if (
                    field === "__actions"
                ) {

                    return {
                        resizable: true
                    };
                }

                return null;
            }


            function parseWidth(
                width
            ) {

                if (
                    width === null ||
                    width === undefined
                ) {
                    return null;
                }

                var value =
                    String(width)
                        .trim()
                        .toLowerCase();

                if (!value) {
                    return null;
                }

                if (
                    value.indexOf("px") !== -1
                ) {

                    var px =
                        parseFloat(value);

                    if (!isNaN(px)) {
                        return px;
                    }
                }

                /*
                 * Percentages are intentionally
                 * not converted here.
                 */

                if (
                    value.indexOf("%") !== -1
                ) {
                    return null;
                }

                var numeric =
                    parseFloat(value);

                if (!isNaN(numeric)) {
                    return numeric;
                }

                return null;
            }


            function normalizeWidth(
                width,
                field
            ) {

                width =
                    parseFloat(width);

                if (
                    isNaN(width)
                ) {
                    width = minWidth;
                }

                width = Math.max(
                    minWidth,
                    Math.round(width)
                );

                if (
                    isSelectedField(field)
                ) {
                    width = Math.min(width, 60);
                }

                return width;
            }


            function setColumnWidth(
                column,
                width,
                skipLayout
            ) {

                column.width =
                    normalizeWidth(
                        width,
                        column.field
                    );

                if (!skipLayout) {
                    applyLayout();
                }
            }


            function applyDisplayWidth(
                column,
                width
            ) {

                width =
                    normalizeWidth(
                        width,
                        column.field
                    );

                column.col.style.width =
                    width + "px";

                column.header.style.width =
                    width + "px";

                if (
                    isSelectedField(
                        column.field
                    )
                ) {

                    column.col.style.minWidth =
                        "60px";
                    column.col.style.maxWidth =
                        "60px";
                    column.header.style.minWidth =
                        "60px";
                    column.header.style.maxWidth =
                        "60px";
                }
                else {

                    column.col.style.minWidth =
                        "";
                    column.col.style.maxWidth =
                        "";
                    column.header.style.minWidth =
                        "";
                    column.header.style.maxWidth =
                        "";
                }
            }


            function collectWidths() {

                var result = {};

                columns.forEach(
                    function (column) {

                        result[column.field] =
                            normalizeWidth(
                                column.width,
                                column.field
                            );
                    }
                );

                return result;
            }


            /*
             * =========================================================
             * COLGROUP
             * =========================================================
             */

            var oldColGroup =
                table.querySelector(
                    "colgroup[data-table-resize-colgroup]"
                );

            if (oldColGroup) {
                oldColGroup.remove();
            }


            var colGroup =
                document.createElement(
                    "colgroup"
                );

            colGroup.setAttribute(
                "data-table-resize-colgroup",
                "true"
            );


            var columns = [];


            /*
             * Read the current widths BEFORE
             * enabling table-layout: fixed.
             */

            for (
                var i = 0;
                i < headers.length;
                i++
            ) {

                var header =
                    headers[i];

                var field =
                    getHeaderField(
                        header,
                        i
                    );

                var config =
                    getColumnConfig(
                        field
                    );

                var isHidden =
                    header.classList.contains(
                        "data-table-col-hidden"
                    );

                var width =
                    isHidden
                        ? minWidth
                        : header
                            .getBoundingClientRect()
                            .width;

                width =
                    normalizeWidth(
                        width,
                        field
                    );


                /*
                 * If column has explicit Width,
                 * use it as initial width.
                 */

                var configuredWidth =
                    config
                        ? parseWidth(
                            config.width
                        )
                        : null;

                if (
                    configuredWidth &&
                    configuredWidth > 0
                ) {

                    width =
                        normalizeWidth(
                            configuredWidth,
                            field
                        );
                }


                var col =
                    document.createElement(
                        "col"
                    );

                col.setAttribute(
                    "data-field",
                    field
                );

                if (isHidden) {

                    col.classList.add(
                        "data-table-col-hidden"
                    );
                }

                col.style.width =
                    width + "px";


                colGroup.appendChild(
                    col
                );


                columns.push({

                    field: field,

                    header: header,

                    col: col,

                    width: width,

                    config: config

                });
            }


            table.insertBefore(
                colGroup,
                thead
            );


            /*
             * =========================================================
             * TABLE LAYOUT
             * =========================================================
             */

            table.classList.add(
                "data-table-resizable"
            );

            table.style.tableLayout =
                "fixed";


            /*
             * =========================================================
             * RESTORE SAVED WIDTHS
             * =========================================================
             */

            var savedWidths =
                loadSavedWidths();


            columns.forEach(
                function (column) {

                    var savedWidth =
                        savedWidths[
                        column.field
                        ];

                    if (
                        savedWidth !== undefined &&
                        savedWidth !== null
                    ) {

                        savedWidth =
                            parseFloat(
                                savedWidth
                            );

                        if (
                            !isNaN(savedWidth) &&
                            savedWidth > 0
                        ) {

                            column.width =
                                normalizeWidth(
                                    savedWidth,
                                    column.field
                                );
                        }
                    }


                    column.width =
                        normalizeWidth(
                            column.width,
                            column.field
                        );
                }
            );

            applyLayout();


            /*
             * =========================================================
             * CREATE RESIZERS
             * =========================================================
             */

            columns.forEach(
                function (column) {

                    var config =
                        column.config;


                    /*
                     * If a column explicitly says
                     * resizable = false, skip it.
                     */

                    if (
                        (config && config.resizable === false) ||
                        isSelectedField(column.field)
                    ) {
                        return;
                    }


                    var resizer =
                        document.createElement(
                            "span"
                        );

                    resizer.className =
                        "data-table-column-resizer";

                    resizer.setAttribute(
                        "data-column-resizer",
                        "true"
                    );

                    resizer.setAttribute(
                        "data-column-field",
                        column.field
                    );

                    resizer.setAttribute(
                        "aria-hidden",
                        "true"
                    );


                    column.header.appendChild(
                        resizer
                    );


                    /*
                     * =================================================
                     * RESIZE
                     * =================================================
                     */

                    resizer.addEventListener(
                        "mousedown",
                        function (event) {

                            if (
                                event.button !== 0
                            ) {
                                return;
                            }


                            event.preventDefault();
                            event.stopPropagation();


                            /*
                             * IMPORTANT:
                             *
                             * Everything is captured ONCE
                             * at the beginning.
                             *
                             * We do not recalculate the
                             * start position during dragging.
                             */

                            var startX =
                                event.clientX;

                            var startWidth =
                                column.width;


                            /*
                             * Prevent browser text
                             * selection while dragging.
                             */

                            document.body.classList.add(
                                "data-table-column-resizing"
                            );

                            resizer.classList.add(
                                "is-resizing"
                            );


                            /*
                             * Capture pointer/mouse.
                             */

                            function onMouseMove(
                                moveEvent
                            ) {

                                var delta =
                                    moveEvent.clientX -
                                    startX;


                                var newWidth =
                                    startWidth +
                                    delta;


                                newWidth =
                                    normalizeWidth(
                                        newWidth,
                                        column.field
                                    );


                                /*
                                 * ONLY this column
                                 * is changed.
                                 *
                                 * We do NOT change
                                 * table.style.width.
                                 */

                                setColumnWidth(
                                    column,
                                    newWidth
                                );
                            }


                            function finishResize() {

                                resizer.classList.remove(
                                    "is-resizing"
                                );

                                document.body.classList.remove(
                                    "data-table-column-resizing"
                                );


                                document.removeEventListener(
                                    "mousemove",
                                    onMouseMove
                                );

                                document.removeEventListener(
                                    "mouseup",
                                    finishResize
                                );


                                /*
                                 * Save exact widths
                                 * after resize.
                                 */

                                applyLayout();

                                saveWidths(
                                    collectWidths()
                                );
                            }


                            document.addEventListener(
                                "mousemove",
                                onMouseMove
                            );

                            document.addEventListener(
                                "mouseup",
                                finishResize
                            );
                        }
                    );
                }
            );


            /*
             * =========================================================
             * FIT HEADERS
             * =========================================================
             */

            function getFilterFields() {

                var fields = {};
                var filters =
                    options.filters &&
                    options.filters.items;

                if (filters && Array.isArray(filters)) {

                    for (var i = 0; i < filters.length; i++) {

                        if (filters[i] && filters[i].field) {
                            fields[String(filters[i].field)] = true;
                        }
                    }
                }

                return fields;
            }


            function measureTextWidth(text, font) {

                var canvas =
                    measureTextWidth.canvas ||
                    (measureTextWidth.canvas =
                        document.createElement("canvas"));

                var context =
                    canvas.getContext("2d");

                context.font = font;

                return context.measureText(
                    text || ""
                ).width;
            }


            function getHeaderTitle(column) {

                if (
                    column.config &&
                    column.config.title
                ) {
                    return String(column.config.title);
                }

                var titleSpan =
                    column.header.querySelector(
                        ".data-table-sort > span:not(.data-table-sort-icon):not(.data-table-column-filter-icon)"
                    );

                if (titleSpan) {
                    return (titleSpan.textContent || "").trim();
                }

                if (
                    column.header.querySelector(
                        ".data-table-select-all"
                    )
                ) {
                    return "";
                }

                return (column.header.textContent || "").trim();
            }


            function measureHeaderTitleWidth(column) {

                var source =
                    column.header.querySelector(
                        ".data-table-sort"
                    ) ||
                    column.header;

                var style =
                    window.getComputedStyle(
                        source
                    );

                var title =
                    getHeaderTitle(column);

                if (style.textTransform === "uppercase") {
                    title = title.toUpperCase();
                }

                var font =
                    style.font ||
                    [
                        style.fontWeight,
                        style.fontSize,
                        style.fontFamily
                    ].join(" ");

                var width =
                    measureTextWidth(
                        title,
                        font
                    );

                var letterSpacing =
                    parseFloat(
                        style.letterSpacing
                    );

                if (
                    !isNaN(letterSpacing) &&
                    title.length > 1
                ) {
                    width +=
                        letterSpacing *
                        (title.length - 1);
                }

                return width;
            }


            var layoutLock = false;


            function applyLayout() {

                if (layoutLock) {
                    return;
                }

                var available = 0;

                if (scroller) {
                    available =
                        scroller.clientWidth;
                }

                var visible = [];
                var logicalTotal = 0;

                columns.forEach(
                    function (column) {

                        if (
                            isColumnHidden(
                                column
                            )
                        ) {

                            applyDisplayWidth(
                                column,
                                column.width
                            );

                            return;
                        }

                        if (
                            isSelectedField(
                                column.field
                            )
                        ) {
                            column.width = 60;
                        }

                        var logical =
                            normalizeWidth(
                                column.width,
                                column.field
                            );

                        column.width =
                            logical;

                        visible.push({
                            column: column,
                            logical: logical
                        });

                        logicalTotal +=
                            logical;
                    }
                );

                var extra = 0;

                if (
                    available > 0 &&
                    logicalTotal > 0
                ) {
                    extra =
                        available -
                        logicalTotal;
                }

                var extraTargets = [];
                var extraBase = 0;

                if (extra > 0) {

                    for (
                        var v = 0;
                        v < visible.length;
                        v++
                    ) {

                        var candidate =
                            visible[v];

                        if (
                            isSelectedField(
                                candidate.column.field
                            ) ||
                            candidate.column.field ===
                            "__actions"
                        ) {
                            continue;
                        }

                        extraTargets.push(
                            candidate
                        );

                        extraBase +=
                            candidate.logical;
                    }

                    if (
                        !extraTargets.length
                    ) {

                        for (
                            var a = 0;
                            a < visible.length;
                            a++
                        ) {

                            if (
                                !isSelectedField(
                                    visible[a]
                                        .column
                                        .field
                                )
                            ) {

                                extraTargets.push(
                                    visible[a]
                                );

                                extraBase +=
                                    visible[a]
                                        .logical;
                            }
                        }
                    }
                }

                var remainingExtra =
                    extra > 0
                        ? extra
                        : 0;

                visible.forEach(
                    function (item) {

                        var display =
                            item.logical;

                        var targetIndex =
                            extraTargets.indexOf(
                                item
                            );

                        if (
                            extra > 0 &&
                            targetIndex !== -1 &&
                            extraBase > 0
                        ) {

                            var share;

                            if (
                                targetIndex ===
                                extraTargets.length - 1
                            ) {
                                share =
                                    remainingExtra;
                            }
                            else {

                                share =
                                    Math.floor(
                                        extra *
                                        (
                                            item.logical /
                                            extraBase
                                        )
                                    );

                                remainingExtra -=
                                    share;
                            }

                            display =
                                item.logical +
                                share;
                        }

                        applyDisplayWidth(
                            item.column,
                            display
                        );
                    }
                );

                var tableWidth =
                    logicalTotal;

                if (
                    extra > 0 &&
                    extraTargets.length &&
                    available > 0
                ) {
                    tableWidth =
                        available;
                }

                if (tableWidth > 0) {

                    table.style.width =
                        tableWidth + "px";

                    table.style.minWidth =
                        tableWidth + "px";

                    table.style.maxWidth =
                        "none";
                }
            }


            function estimateActionsFallbackWidth() {

                var count = 0;
                var hasExplicitInlineEdit =
                    false;

                if (
                    typeof ctx.getRowActions ===
                    "function"
                ) {

                    var rowActions =
                        ctx.getRowActions();

                    count =
                        rowActions.length;

                    for (
                        var i = 0;
                        i < rowActions.length;
                        i++
                    ) {

                        if (
                            rowActions[i] &&
                            rowActions[i].type ===
                            "InlineEdit"
                        ) {
                            hasExplicitInlineEdit =
                                true;
                            break;
                        }
                    }
                }

                if (
                    !hasExplicitInlineEdit &&
                    options.inlineEditing &&
                    typeof ctx.hasEditableColumns ===
                    "function" &&
                    ctx.hasEditableColumns()
                ) {
                    count += 1;
                }

                if (count < 1) {
                    count = 2;
                }

                return (
                    count * 33 +
                    28
                );
            }


            function measureActionsColumnWidth(
                column
            ) {

                layoutLock = true;

                try {

                var extra = 10;

                if (
                    column.header.querySelector(
                        "[data-column-resizer]"
                    )
                ) {
                    extra += 8;
                }

                var cells =
                    table.querySelectorAll(
                        "tbody td.data-table-actions"
                    );

                if (!cells.length) {

                    return (
                        estimateActionsFallbackWidth() +
                        extra
                    );
                }

                var prevColWidth =
                    column.col.style.width;

                var prevHeaderWidth =
                    column.header.style.width;

                var prevTableWidth =
                    table.style.width;

                var prevTableMax =
                    table.style.maxWidth;

                var prevTableMin =
                    table.style.minWidth;

                var otherWidth = 0;

                columns.forEach(
                    function (item) {

                        if (item !== column) {
                            otherWidth +=
                                item.width;
                        }
                    }
                );

                var wide = 2000;

                column.col.style.width =
                    wide + "px";

                column.header.style.width =
                    wide + "px";

                table.style.maxWidth =
                    "none";

                table.style.width =
                    (otherWidth + wide) + "px";

                table.style.minWidth =
                    (otherWidth + wide) + "px";

                var maxWidth = 0;

                for (
                    var i = 0;
                    i < cells.length;
                    i++
                ) {

                    var cell =
                        cells[i];

                    var prevOverflow =
                        cell.style.overflow;

                    var prevMaxWidth =
                        cell.style.maxWidth;

                    var prevWhiteSpace =
                        cell.style.whiteSpace;

                    cell.style.overflow =
                        "visible";

                    cell.style.maxWidth =
                        "none";

                    cell.style.whiteSpace =
                        "nowrap";

                    var content = 0;
                    var children =
                        cell.children;

                    for (
                        var j = 0;
                        j < children.length;
                        j++
                    ) {

                        var child =
                            children[j];

                        var childPrevMax =
                            child.style.maxWidth;

                        var childPrevWhite =
                            child.style.whiteSpace;

                        child.style.maxWidth =
                            "none";

                        child.style.whiteSpace =
                            "nowrap";

                        var childStyle =
                            window.getComputedStyle(
                                child
                            );

                        content +=
                            child.getBoundingClientRect()
                                .width +
                            (parseFloat(childStyle.marginLeft) || 0) +
                            (parseFloat(childStyle.marginRight) || 0);

                        child.style.maxWidth =
                            childPrevMax;

                        child.style.whiteSpace =
                            childPrevWhite;
                    }

                    var cellStyle =
                        window.getComputedStyle(
                            cell
                        );

                    content +=
                        (parseFloat(cellStyle.paddingLeft) || 0) +
                        (parseFloat(cellStyle.paddingRight) || 0);

                    cell.style.overflow =
                        prevOverflow;

                    cell.style.maxWidth =
                        prevMaxWidth;

                    cell.style.whiteSpace =
                        prevWhiteSpace;

                    if (content > maxWidth) {
                        maxWidth = content;
                    }
                }

                column.col.style.width =
                    prevColWidth;

                column.header.style.width =
                    prevHeaderWidth;

                table.style.width =
                    prevTableWidth;

                table.style.maxWidth =
                    prevTableMax;

                table.style.minWidth =
                    prevTableMin;

                if (maxWidth <= 0) {
                    maxWidth =
                        estimateActionsFallbackWidth();
                }

                return maxWidth + extra;

                }
                finally {
                    layoutLock = false;
                }
            }


            ctx.fitHeaderColumns =
                function () {

                    var filterFields =
                        getFilterFields();

                    columns.forEach(
                        function (column) {

                            if (
                                column.header &&
                                column.header.classList.contains(
                                    "data-table-col-hidden"
                                )
                            ) {
                                return;
                            }

                            if (
                                column.config &&
                                column.config.resizable === false
                            ) {
                                return;
                            }

                            if (
                                column.field ===
                                "__actions"
                            ) {

                                setColumnWidth(
                                    column,
                                    measureActionsColumnWidth(
                                        column
                                    ),
                                    true
                                );

                                return;
                            }

                            var title =
                                getHeaderTitle(column);

                            if (
                                !title &&
                                column.header.querySelector(
                                    ".data-table-select-all"
                                )
                            ) {
                                return;
                            }

                            var headerStyle =
                                window.getComputedStyle(
                                    column.header
                                );

                            var padding =
                                (parseFloat(headerStyle.paddingLeft) || 0) +
                                (parseFloat(headerStyle.paddingRight) || 0);

                            var titleWidth =
                                measureHeaderTitleWidth(
                                    column
                                );

                            /*
                             * Extra space so the full
                             * header name stays visible
                             * together with sort / filter
                             * icons when they are present.
                             */

                            var extra = 14;

                            if (
                                column.header.querySelector(
                                    ".data-table-sort"
                                )
                            ) {
                                extra += 16;
                            }

                            if (
                                filterFields[column.field] ||
                                column.header.querySelector(
                                    ".data-table-column-filter-icon"
                                )
                            ) {
                                extra += 16;
                            }

                            if (
                                column.header.querySelector(
                                    "[data-column-resizer]"
                                )
                            ) {
                                extra += 8;
                            }

                            setColumnWidth(
                                column,
                                titleWidth + padding + extra,
                                true
                            );
                        }
                    );

                    applyLayout();

                    saveWidths(
                        collectWidths()
                    );
                };


            var fitButton =
                ctx.root.querySelector(
                    "[data-table-fit-columns]"
                );

            if (fitButton) {

                fitButton.addEventListener(
                    "click",
                    function (event) {

                        event.preventDefault();
                        ctx.fitHeaderColumns();
                    }
                );
            }


            /*
             * =========================================================
             * PUBLIC API
             * =========================================================
             */

            ctx.applyColumnLayout =
                applyLayout;


            ctx.resetColumnWidths =
                function () {

                    try {

                        window.localStorage.removeItem(
                            storageKey
                        );

                    }
                    catch (error) {

                        console.warn(
                            "DataTable: unable to reset column widths.",
                            error
                        );
                    }


                    window.location.reload();
                };


            if (
                window.ResizeObserver &&
                scroller
            ) {

                var layoutObserver =
                    new window.ResizeObserver(
                        function () {
                            applyLayout();
                        }
                    );

                layoutObserver.observe(
                    scroller
                );
            }
            else {

                window.addEventListener(
                    "resize",
                    applyLayout
                );
            }
        }
    };

})(window, document);