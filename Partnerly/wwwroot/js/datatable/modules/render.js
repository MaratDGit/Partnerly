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


                            ctx.state.selectedRowKeys =
                                ctx.state.selectedRowKeys || {};

                            if (target.checked) {

                                if (
                                    ctx.state.selectedRows.indexOf(id) === -1
                                ) {

                                    ctx.state.selectedRows.push(id);
                                }

                                var row =
                                    target.closest("tr");

                                if (ctx.getRowKeys) {
                                    ctx.state.selectedRowKeys[id] =
                                        ctx.getRowKeys(row);
                                }

                            } else {

                                ctx.state.selectedRows =
                                    ctx.state.selectedRows.filter(
                                        function (selectedId) {

                                            return selectedId !== id;
                                        }
                                    );

                                delete ctx.state.selectedRowKeys[id];
                            }


                            ctx.updateSelectAllState();

                            if (ctx.updateBulkDeleteButton) {
                                ctx.updateBulkDeleteButton();
                            }
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

                                ctx.state.selectedRowKeys =
                                    ctx.state.selectedRowKeys || {};


                                if (checked) {

                                    if (
                                        ctx.state.selectedRows.indexOf(id) === -1
                                    ) {

                                        ctx.state.selectedRows.push(id);
                                    }

                                    var row =
                                        checkbox.closest("tr");

                                    if (ctx.getRowKeys) {
                                        ctx.state.selectedRowKeys[id] =
                                            ctx.getRowKeys(row);
                                    }

                                } else {

                                    ctx.state.selectedRows =
                                        ctx.state.selectedRows.filter(
                                            function (selectedId) {

                                                return selectedId !== id;
                                            }
                                        );

                                    delete ctx.state.selectedRowKeys[id];
                                }
                            }


                            ctx.updateSelectAllState();

                            if (ctx.updateBulkDeleteButton) {
                                ctx.updateBulkDeleteButton();
                            }
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


            function noteLocalization(key, fallback) {

                if (
                    typeof dataTableLocalization !==
                        "undefined" &&
                    dataTableLocalization[key]
                ) {
                    return dataTableLocalization[key];
                }

                return fallback;
            }


            ctx.ensureNoteModal =
                function () {

                    var modal =
                        document.getElementById(
                            "dataTableNoteModal"
                        );

                    if (modal) {
                        return modal;
                    }

                    modal =
                        document.createElement(
                            "div"
                        );

                    modal.id =
                        "dataTableNoteModal";
                    modal.className =
                        "modal fade partnerly-note-modal";
                    modal.tabIndex = -1;
                    modal.setAttribute(
                        "aria-labelledby",
                        "dataTableNoteModalLabel"
                    );
                    modal.setAttribute(
                        "aria-hidden",
                        "true"
                    );

                    modal.innerHTML =
                        '<div class="modal-dialog modal-dialog-centered">' +
                        '<div class="modal-content rounded-3 shadow-lg">' +
                        '<div class="modal-header py-2">' +
                        '<h6 class="modal-title" id="dataTableNoteModalLabel">' +
                        '<span class="note-modal-icon">' +
                        '<i class="bx bx-note"></i>' +
                        "</span>" +
                        "<span>" +
                        ctx.escapeHtml(
                            noteLocalization(
                                "noteTitle",
                                "Note"
                            )
                        ) +
                        "</span>" +
                        "</h6>" +
                        '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="' +
                        ctx.escapeHtml(
                            noteLocalization(
                                "close",
                                "Close"
                            )
                        ) +
                        '"></button>' +
                        "</div>" +
                        '<div class="modal-body py-2">' +
                        '<div class="form-floating">' +
                        '<textarea id="dataTableNoteText" class="form-control" placeholder="' +
                        ctx.escapeHtml(
                            noteLocalization(
                                "notePlaceholder",
                                ""
                            )
                        ) +
                        '" readonly style="height: 120px; font-size: 0.9rem;"></textarea>' +
                        "</div>" +
                        "</div>" +
                        '<div class="modal-footer py-2">' +
                        '<button type="button" class="btn btn-primary btn-sm" data-bs-dismiss="modal">' +
                        ctx.escapeHtml(
                            noteLocalization(
                                "ok",
                                "OK"
                            )
                        ) +
                        "</button>" +
                        "</div>" +
                        "</div>" +
                        "</div>";

                    document.body.appendChild(
                        modal
                    );

                    return modal;
                };


            ctx.openNotePopup =
                function (noteValue) {

                    var modal =
                        ctx.ensureNoteModal();

                    var textarea =
                        modal.querySelector(
                            "#dataTableNoteText"
                        );

                    if (textarea) {
                        textarea.value =
                            noteValue == null
                                ? ""
                                : String(noteValue);
                    }

                    if (
                        window.bootstrap &&
                        bootstrap.Modal
                    ) {
                        bootstrap.Modal.getOrCreateInstance(
                            modal
                        ).show();
                        return;
                    }

                    modal.classList.add(
                        "show"
                    );
                    modal.style.display =
                        "block";
                    modal.setAttribute(
                        "aria-hidden",
                        "false"
                    );
                };


            ctx.bindNotePopup =
                function () {

                    if (
                        ctx._notePopupBound ||
                        !ctx.body
                    ) {
                        return;
                    }

                    ctx._notePopupBound =
                        true;

                    ctx.body.addEventListener(
                        "click",
                        function (event) {

                            var button =
                                event.target.closest(
                                    ".data-table-note-btn"
                                );

                            if (!button) {
                                return;
                            }

                            event.preventDefault();
                            event.stopPropagation();

                            var cell =
                                button.closest(
                                    "td"
                                );

                            var noteValue =
                                cell
                                    ? cell.getAttribute(
                                        "data-original-value"
                                    )
                                    : "";

                            ctx.openNotePopup(
                                noteValue || ""
                            );
                        }
                    );
                };


            /*
             * =========================================================
             * RENDER ROWS
             * =========================================================
             */

            ctx.renderRows =
                function (items) {

                    ctx.lastItems =
                        items;

                    if (
                        !items ||
                        items.length === 0
                    ) {

                        ctx.body.innerHTML =
                            '<tr>' +
                            '<td colspan="' +
                            options.columnCount +
                            '">' +
                            (
                                lookupLocalization?.noData ||
                                "No Data"
                            ) +
                            '</td>' +
                            '</tr>';
                        
                        ctx.updateSelectAllState();

                        if (ctx.updateBulkDeleteButton) {
                            ctx.updateBulkDeleteButton();
                        }

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

                            var rowKeys = ctx.getKeysFromItem(item);

                            html +=
                                '<tr' +
                            rowClass +
                            ' data-row-id="' + ctx.escapeHtml(id) +
                            '"' +
                            ' data-row-keys="' + ctx.escapeHtml(JSON.stringify(rowKeys)) +
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

                                    if (
                                        column.type ===
                                        "Note"
                                    ) {
                                        cellClasses.push(
                                            "data-table-note-cell"
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
                              * =========================================================
                              * ACTIONS
                              * =========================================================
                              */

                            function getActionContent(action, defaultText) {
                                if (action.text) {
                                    return ctx.escapeHtml(action.text);
                                }

                                if (action.icon) {
                                    return '<i class="' +
                                        ctx.escapeHtml(action.icon) +
                                        '"></i>';
                                }

                                return ctx.escapeHtml(
                                    action.title ||
                                    defaultText ||
                                    action.name ||
                                    ""
                                );
                            }

                            if (
                                ctx.hasActionsColumn()
                            ) {

                                html +=
                                    '<td class="data-table-actions" data-field="__actions">';


                                /*
                                 * =====================================================
                                 * INLINE EDIT
                                 * =====================================================
                                 */

                                var rowActions =
                                    ctx.getRowActions();


                                var explicitInlineEditActions =
                                    rowActions.filter(
                                        function (action) {

                                            return (
                                                action &&
                                                action.type ===
                                                "InlineEdit"
                                            );
                                        }
                                    );


                                if (
                                    explicitInlineEditActions.length > 0
                                ) {

                                    explicitInlineEditActions.forEach(
                                        function (action) {

                                            html +=
                                                '<button type="button" ' +

                                                'class="' +
                                                ctx.escapeHtml(
                                                    action.cssClass ||
                                                    "btn btn-sm btn-outline-primary"
                                                ) +
                                                ' data-table-inline-edit" ' +

                                                'data-id="' +
                                                ctx.escapeHtml(id) +
                                                '" ' +

                                                'title="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    "Edit"
                                                ) +
                                                '" ' +

                                                'aria-label="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    "Edit"
                                                ) +
                                                '">' +

                                            getActionContent(action, "Edit") +

                                                '</button>';
                                        }
                                    );

                                }
                                else if (
                                    options.inlineEditing &&
                                    ctx.hasEditableColumns()
                                ) {

                                    html +=
                                        '<button type="button" ' +

                                        'class="btn btn-sm btn-outline-primary data-table-inline-edit" ' +

                                        'data-id="' +
                                        ctx.escapeHtml(id) +
                                        '" ' +

                                        'title="Edit row" ' +
                                        'aria-label="Edit row">' +

                                        '<i class="bx bx-edit-alt"></i>' +

                                        '</button>';
                                }


                                /*
                                 * =====================================================
                                 * OTHER ACTIONS
                                 * =====================================================
                                 */

                                rowActions.forEach(
                                    function (action) {

                                        if (
                                            !action ||
                                            action.type ===
                                            "InlineEdit"
                                        ) {
                                            return;
                                        }


                                        /*
                                         * =================================================
                                         * INLINE DELETE
                                         * =================================================
                                         */

                                        if (
                                            action.type ===
                                            "InlineDelete"
                                        ) {

                                            html +=
                                                '<button type="button" ' +

                                                'class="' +
                                                ctx.escapeHtml(
                                                    action.cssClass ||
                                                    "btn btn-sm btn-outline-danger"
                                                ) +
                                                ' data-table-action-inline-delete" ' +

                                                'data-action-name="' +
                                                ctx.escapeHtml(
                                                    action.name
                                                ) +
                                                '" ' +

                                                'data-id="' +
                                                ctx.escapeHtml(id) +
                                                '" ' +

                                                'title="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    "Delete"
                                                ) +
                                                '" ' +

                                                'aria-label="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    "Delete"
                                                ) +
                                                '">' +

                                            getActionContent(action, "Delete") +

                                                '</button>';

                                            return;
                                        }


                                        /*
                                         * =================================================
                                         * AJAX
                                         * =================================================
                                         */

                                        if (
                                            action.type ===
                                            "Ajax"
                                        ) {

                                            html +=
                                                '<button type="button" ' +

                                                'class="' +
                                                ctx.escapeHtml(
                                                    action.cssClass ||
                                                    "btn btn-sm btn-outline-secondary"
                                                ) +
                                                ' data-table-action-ajax" ' +

                                                'data-action-name="' +
                                                ctx.escapeHtml(
                                                    action.name
                                                ) +
                                                '" ' +

                                                'data-id="' +
                                                ctx.escapeHtml(id) +
                                                '" ' +

                                                'title="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    action.name
                                                ) +
                                                '" ' +

                                                'aria-label="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    action.name
                                                ) +
                                                '">' +

                                            getActionContent(action, action.name) +

                                                '</button>';

                                            return;
                                        }


                                        /*
                                         * =================================================
                                         * LINK
                                         * =================================================
                                         */

                                        if (
                                            action.type ===
                                            "Link"
                                        ) {

                                            var href =
                                                ctx.buildActionUrl(
                                                    action,
                                                    id
                                                );


                                            if (!href) {
                                                return;
                                            }


                                            html +=
                                                '<a href="' +
                                                ctx.escapeHtml(
                                                    href
                                                ) +
                                                '" ' +

                                                'class="' +
                                                ctx.escapeHtml(
                                                    action.cssClass ||
                                                    "btn btn-sm btn-outline-secondary"
                                                ) +
                                                '" ' +

                                                'title="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    action.name
                                                ) +
                                                '" ' +

                                                'aria-label="' +
                                                ctx.escapeHtml(
                                                    action.title ||
                                                    action.name
                                                ) +
                                                '">' +

                                            getActionContent(action, action.name) +

                                                '</a>';
                                        }
                                    }
                                );


                                html +=
                                    '</td>';
                            }
                        }
                    );

                    ctx.body.innerHTML =
                        html;

                    ctx.updateSelectAllState();

                    if (ctx.updateBulkDeleteButton) {
                        ctx.updateBulkDeleteButton();
                    }

                    if (ctx.bindInlineEditButtons) {
                        ctx.bindInlineEditButtons();
                    }

                    if (ctx.bindInlineDeleteButtons) {
                        ctx.bindInlineDeleteButtons();
                    }

                    if (ctx.bindActionButtons) {
                        ctx.bindActionButtons();
                    }

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
                            lookupLocalization?.noData ?? "No Records";

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


                 

                    ctx.info.textContent = dataTableLocalization.shownRecords
                        .replace("{0}", start)
                        .replace("{1}", end)
                        .replace("{2}", result.total);
                };


            /*
             * =========================================================
             * INITIALIZE SELECTION EVENTS
             * =========================================================
             */

            if (
                ctx.bindInlineActions
            ) {
                ctx.bindInlineActions();
            }

            ctx.bindRowSelection();

            ctx.bindSelectAll();

            ctx.bindRowClick();

            ctx.bindNotePopup();

        }
    };

})(window);