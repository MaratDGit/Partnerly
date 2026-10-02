(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.render = {

        setup: function (ctx) {

            var options =
                ctx.options;


            /*
             * =========================================================
             * ROW SELECTION
             * =========================================================
             */

            ctx.bindRowSelection =
                function () {

                    if (
                        !options.rows ||
                        !options.rows.selectable
                    ) {
                        return;
                    }

                    if (!ctx.body) {
                        return;
                    }

                    ctx.body.addEventListener(
                        "change",
                        function (event) {

                            var target =
                                event.target;

                            if (
                                !target.classList.contains(
                                    "data-table-row-select"
                                )
                            ) {
                                return;
                            }

                            var id =
                                target.getAttribute(
                                    "data-row-id"
                                );

                            if (!id) {
                                return;
                            }

                            id =
                                String(id);


                            if (target.checked) {

                                if (
                                    ctx.state.selectedRows.indexOf(id) === -1
                                ) {

                                    ctx.state.selectedRows.push(id);
                                }

                            } else {

                                ctx.state.selectedRows =
                                    ctx.state.selectedRows.filter(
                                        function (selectedId) {

                                            return selectedId !== id;
                                        }
                                    );
                            }


                            ctx.updateSelectAllState();
                        }
                    );
                };


            ctx.bindSelectAll =
                function () {

                    if (
                        !options.rows ||
                        !options.rows.selectable
                    ) {
                        return;
                    }

                    var selectAll =
                        ctx.root.querySelector(
                            ".data-table-select-all"
                        );

                    if (!selectAll) {
                        return;
                    }


                    selectAll.addEventListener(
                        "change",
                        function () {

                            var checked =
                                selectAll.checked;

                            var checkboxes =
                                ctx.body.querySelectorAll(
                                    ".data-table-row-select"
                                );


                            for (
                                var i = 0;
                                i < checkboxes.length;
                                i++
                            ) {

                                var checkbox =
                                    checkboxes[i];

                                var id =
                                    checkbox.getAttribute(
                                        "data-row-id"
                                    );

                                if (!id) {
                                    continue;
                                }

                                id =
                                    String(id);


                                checkbox.checked =
                                    checked;


                                if (checked) {

                                    if (
                                        ctx.state.selectedRows.indexOf(id) === -1
                                    ) {

                                        ctx.state.selectedRows.push(id);
                                    }

                                } else {

                                    ctx.state.selectedRows =
                                        ctx.state.selectedRows.filter(
                                            function (selectedId) {

                                                return selectedId !== id;
                                            }
                                        );
                                }
                            }


                            ctx.updateSelectAllState();
                        }
                    );
                };


            ctx.updateSelectAllState =
                function () {

                    if (
                        !options.rows ||
                        !options.rows.selectable
                    ) {
                        return;
                    }

                    var selectAll =
                        ctx.root.querySelector(
                            ".data-table-select-all"
                        );

                    if (!selectAll) {
                        return;
                    }


                    var checkboxes =
                        ctx.body.querySelectorAll(
                            ".data-table-row-select"
                        );


                    if (checkboxes.length === 0) {

                        selectAll.checked =
                            false;

                        selectAll.indeterminate =
                            false;

                        return;
                    }


                    var checkedCount =
                        0;


                    for (
                        var i = 0;
                        i < checkboxes.length;
                        i++
                    ) {

                        if (
                            checkboxes[i].checked
                        ) {

                            checkedCount++;
                        }
                    }


                    selectAll.checked =
                        checkedCount ===
                        checkboxes.length;


                    selectAll.indeterminate =
                        checkedCount > 0 &&
                        checkedCount < checkboxes.length;
                };

            /*
             * =========================================================
             * ROW CLICK
             * =========================================================
             */

            ctx.bindRowClick =
                function () {

                    if (
                        !options.rows ||
                        !options.rows.clickable
                    ) {
                        return;
                    }

                    if (!ctx.body) {
                        return;
                    }


                    ctx.body.addEventListener(
                        "click",
                        function (event) {

                            var target =
                                event.target;


                            /*
                             * Не считаем кликом строки:
                             * checkbox
                             * button
                             * link
                             * input
                             * select
                             * textarea
                             */

                            if (
                                target.closest(
                                    "input, button, a, select, textarea"
                                )
                            ) {
                                return;
                            }


                            var row =
                                target.closest(
                                    "tr[data-row-id]"
                                );

                            if (!row) {
                                return;
                            }


                            var id =
                                row.getAttribute(
                                    "data-row-id"
                                );

                            if (!id) {
                                return;
                            }


                            ctx.handleRowClick(
                                String(id),
                                row,
                                event
                            );
                        }
                    );
                };


            ctx.handleRowClick =
                function (
                    id,
                    row,
                    event
                ) {

                    /*
                     * Пока только событие.
                     *
                     * Позже сюда можно подключить:
                     * - ViewUrl
                     * - callback
                     * - custom row action
                     *
                     * Но сейчас не привязываем
                     * Clickable к View/Edit/Delete.
                     */

                    if (
                        window.console &&
                        console.debug
                    ) {

                        console.debug(
                            "DataTable row clicked:",
                            id
                        );
                    }
                };


            /*
             * =========================================================
             * RENDER ROWS
             * =========================================================
             */

            ctx.renderRows =
                function (items) {

                    if (
                        !items ||
                        items.length === 0
                    ) {

                        ctx.body.innerHTML =
                            '<tr><td colspan="' +
                            options.columnCount +
                            '">No data</td></tr>';

                        ctx.updateSelectAllState();

                        return;
                    }


                    var html =
                        "";


                    items.forEach(
                        function (item) {

                            var id =
                                ctx.getValue(
                                    item,
                                    options.keyField ||
                                    "Id"
                                );


                            var rowClass =
                                "";

                            if (
                                options.rows &&
                                options.rows.hover
                            ) {
                                rowClass =
                                    ' class="data-table-row-hover"';
                            }

                            html +=
                                '<tr' +
                                rowClass +
                                ' data-row-id="' +
                                ctx.escapeHtml(
                                    id
                                ) +
                                '">';

                            /*
                             * =================================================
                             * COLUMNS
                             * =================================================
                             */

                            options.columns.forEach(
                                function (column) {

                                    if (!column.visible) {
                                        return;
                                    }

                                    /*
                                     * =================================================
                                     * SELECTED COLUMN
                                     * =================================================
                                     */

                                    if (
                                        options.rows &&
                                        options.rows.selectable &&
                                        column.field.toLowerCase() === "selected"
                                    ) {

                                        var isSelected =
                                            ctx.state.selectedRows.indexOf(
                                                String(id)
                                            ) !== -1;

                                        html +=
                                            '<td class="data-table-row-select-cell" ' +
                                            'data-field="' +
                                            ctx.escapeHtml(column.field) +
                                            '">' +

                                            '<input type="checkbox" ' +
                                            'class="data-table-row-select" ' +
                                            'data-row-id="' +
                                            ctx.escapeHtml(id) +
                                            '"' +
                                            (
                                                isSelected
                                                    ? ' checked'
                                                    : ''
                                            ) +
                                            '>' +

                                            '</td>';

                                        return;
                                    }

                                    /*
                                     * =================================================
                                     * NORMAL COLUMN
                                     * =================================================
                                     */

                                    var value =
                                        ctx.getValue(
                                            item,
                                            column.field
                                        );

                                    var formatted =
                                        ctx.formatCellValue(
                                            value,
                                            column,
                                            item,
                                            id
                                        );

                                    var cellClasses = [];

                                    var alignmentClass =
                                        ctx.getColumnAlignmentClass(
                                            column
                                        );

                                    if (alignmentClass) {
                                        cellClasses.push(
                                            alignmentClass
                                        );
                                    }

                                    if (column.cssClass) {
                                        cellClasses.push(
                                            column.cssClass
                                        );
                                    }

                                    html +=
                                        '<td' +
                                        ' data-field="' +
                                        ctx.escapeHtml(
                                            column.field
                                        ) +
                                        '"' +
                                        ' data-original-value="' +
                                        ctx.escapeHtml(
                                            formatted.rawValue
                                        ) +
                                        '"' +
                                        (
                                            cellClasses.length > 0
                                                ? ' class="' +
                                                ctx.escapeHtml(
                                                    cellClasses.join(" ")
                                                ) +
                                                '"'
                                                : ""
                                        ) +
                                        '>' +
                                        formatted.html +
                                        "</td>";
                                }
                            );


                            /*
                             * =================================================
                             * ACTIONS
                             * =================================================
                             */

                            if (
                                ctx.hasActionsColumn()
                            ) {

                                html +=
                                    '<td class="data-table-actions">';


                                /*
                                 * INLINE EDIT
                                 */

                                if (
                                    options.inlineEditing &&
                                    ctx.hasEditableColumns()
                                ) {

                                    html +=
                                        '<button type="button" ' +
                                        'class="btn btn-sm btn-outline-primary data-table-inline-edit" ' +
                                        'data-id="' +
                                        ctx.escapeHtml(
                                            id
                                        ) +
                                        '" ' +
                                        'title="Edit row" ' +
                                        'aria-label="Edit row">' +
                                        '<i class="bx bx-edit-alt"></i>' +
                                        '</button>';
                                }


                                /*
                                 * VIEW
                                 */

                                if (
                                    options.actions &&
                                    options.actions.enabled &&
                                    options.actions.view &&
                                    options.actions.viewUrl
                                ) {

                                    html +=
                                        '<a href="' +
                                        ctx.escapeHtml(
                                            options.actions.viewUrl +
                                            "/" +
                                            encodeURIComponent(
                                                id
                                            )
                                        ) +
                                        '" ' +
                                        'class="btn btn-sm btn-outline-secondary" ' +
                                        'title="View" ' +
                                        'aria-label="View">' +
                                        '<i class="bx bx-show"></i>' +
                                        '</a>';
                                }


                                /*
                                 * EDIT
                                 */

                                if (
                                    options.actions &&
                                    options.actions.enabled &&
                                    options.actions.edit &&
                                    options.actions.editUrl
                                ) {

                                    html +=
                                        '<a href="' +
                                        ctx.escapeHtml(
                                            options.actions.editUrl +
                                            "/" +
                                            encodeURIComponent(
                                                id
                                            )
                                        ) +
                                        '" ' +
                                        'class="btn btn-sm btn-outline-primary" ' +
                                        'title="Edit" ' +
                                        'aria-label="Edit">' +
                                        '<i class="bx bx-edit"></i>' +
                                        '</a>';
                                }


                                /*
                                 * DELETE
                                 */

                                if (
                                    options.actions &&
                                    options.actions.enabled &&
                                    options.actions.delete &&
                                    options.actions.deleteUrl
                                ) {

                                    html +=
                                        '<a href="' +
                                        ctx.escapeHtml(
                                            options.actions.deleteUrl +
                                            "/" +
                                            encodeURIComponent(
                                                id
                                            )
                                        ) +
                                        '" ' +
                                        'class="btn btn-sm btn-outline-danger" ' +
                                        'title="Delete" ' +
                                        'aria-label="Delete">' +
                                        '<i class="bx bx-trash"></i>' +
                                        '</a>';
                                }


                                html +=
                                    "</td>";
                            }


                            html +=
                                "</tr>";
                        }
                    );


                    /*
                     * =========================================================
                     * INSERT HTML
                     * =========================================================
                     */

                    ctx.body.innerHTML =
                        html;


                    /*
                     * =========================================================
                     * UPDATE SELECT ALL
                     * =========================================================
                     */

                    ctx.updateSelectAllState();


                    /*
                     * =========================================================
                     * EXISTING BEHAVIOR
                     * =========================================================
                     */

                    ctx.bindInlineEditButtons();

                    ctx.highlightSearchText();
                };


            /*
             * =========================================================
             * INFO
             * =========================================================
             */

            ctx.renderInfo =
                function (result) {

                    if (!ctx.info) {
                        return;
                    }

                    if (
                        !ctx.options.paging ||
                        ctx.options.paging.showInfo === false
                    ) {
                        ctx.info.textContent = "";
                        return;
                    }

                    if (
                        result.total === 0
                    ) {

                        ctx.info.textContent =
                            "No records";

                        return;
                    }


                    var start =
                        (
                            (
                                result.page - 1
                            ) *
                            result.pageSize
                        ) + 1;


                    var end =
                        Math.min(
                            result.page *
                            result.pageSize,
                            result.total
                        );


                    ctx.info.textContent =
                        "Showing " +
                        start +
                        "-" +
                        end +
                        " of " +
                        result.total;
                };


            /*
             * =========================================================
             * INITIALIZE SELECTION EVENTS
             * =========================================================
             */

            ctx.bindRowSelection();

            ctx.bindSelectAll();

            ctx.bindRowClick();

        }
    };

})(window);