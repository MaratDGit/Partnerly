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
                width
            ) {

                width =
                    parseFloat(width);

                if (
                    isNaN(width)
                ) {
                    width = minWidth;
                }

                return Math.max(
                    minWidth,
                    Math.round(width)
                );
            }


            function setColumnWidth(
                column,
                width
            ) {

                width =
                    normalizeWidth(width);

                column.width =
                    width;

                column.col.style.width =
                    width + "px";

                /*
                 * Keep the header explicitly
                 * synchronized as well.
                 */

                column.header.style.width =
                    width + "px";
            }


            function getCurrentColumnWidth(
                column
            ) {

                var width =
                    column.col.getBoundingClientRect()
                        .width;

                if (
                    !width ||
                    width <= 0
                ) {

                    width =
                        column.header
                            .getBoundingClientRect()
                            .width;
                }

                return normalizeWidth(
                    width
                );
            }


            function collectWidths() {

                var result = {};

                columns.forEach(
                    function (column) {

                        result[column.field] =
                            normalizeWidth(
                                column.width
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

                var width =
                    header
                        .getBoundingClientRect()
                        .width;

                width =
                    normalizeWidth(
                        width
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
                            configuredWidth
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
                                    savedWidth
                                );
                        }
                    }


                    setColumnWidth(
                        column,
                        column.width
                    );
                }
            );


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
                        config &&
                        config.resizable === false
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
                                getCurrentColumnWidth(
                                    column
                                );


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
                                        newWidth
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
             * PUBLIC API
             * =========================================================
             */

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
        }
    };

})(window, document);