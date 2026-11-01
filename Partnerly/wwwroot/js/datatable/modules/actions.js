(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.actions = {

        setup: function (ctx) {

            var options =
                ctx.options;


            /*
             * =========================================================
             * GET ACTIONS
             * =========================================================
             */

            ctx.getActions =
                function () {

                    if (
                        !options.actions ||
                        !options.actions.enabled
                    ) {
                        return [];
                    }

                    if (
                        !Array.isArray(
                            options.actions.items
                        )
                    ) {
                        return [];
                    }

                    return options.actions.items;
                };


            /*
             * =========================================================
             * TOOLBAR ACTIONS
             * =========================================================
             */

            ctx.getToolbarActions =
                function () {

                    return ctx.getActions()
                        .filter(
                            function (action) {

                                return (
                                    action &&
                                    action.placement ===
                                    "Toolbar"
                                );
                            }
                        );
                };


            /*
             * =========================================================
             * ROW ACTIONS
             * =========================================================
             */

            ctx.getRowActions =
                function () {

                    return ctx.getActions()
                        .filter(
                            function (action) {

                                return (
                                    action &&
                                    action.placement ===
                                    "Row"
                                );
                            }
                        );
                };


            /*
             * =========================================================
             * FIND ACTION
             * =========================================================
             */

            ctx.findAction =
                function (name) {

                    var actions =
                        ctx.getActions();

                    for (
                        var i = 0;
                        i < actions.length;
                        i++
                    ) {

                        if (
                            actions[i] &&
                            String(actions[i].name) ===
                            String(name)
                        ) {
                            return actions[i];
                        }
                    }

                    return null;
                };


            /*
             * =========================================================
             * REMOVE SELECTED ROW
             * =========================================================
             */

            ctx.removeSelectedRow =
                function (id) {

                    if (
                        !ctx.state ||
                        !ctx.state.selectedRows
                    ) {
                        return;
                    }

                    ctx.state.selectedRows =
                        ctx.state.selectedRows.filter(
                            function (selectedId) {

                                return (
                                    String(selectedId) !==
                                    String(id)
                                );
                            }
                        );

                    if (
                        ctx.state.selectedRowKeys
                    ) {
                        delete ctx.state.selectedRowKeys[String(id)];
                    }

                    if (
                        ctx.updateSelectAllState
                    ) {
                        ctx.updateSelectAllState();
                    }

                    if (
                        ctx.updateBulkDeleteButton
                    ) {
                        ctx.updateBulkDeleteButton();
                    }
                };


            /*
             * =========================================================
             * BUILD ACTION URL
             * =========================================================
             */

            ctx.buildActionUrl =
                function (
                    action,
                    id,
                    row
                ) {

                    if (
                        !action ||
                        !action.url
                    ) {
                        return "";
                    }

                    return String(
                        action.url
                    ).replace(
                        /\{([^}]+)\}/g,
                        function (
                            match,
                            fieldName
                        ) {

                            if (
                                fieldName.toLowerCase() ===
                                "id"
                            ) {

                                return encodeURIComponent(
                                    String(id)
                                );
                            }

                            if (
                                row &&
                                ctx.getRowValue
                            ) {

                                var value =
                                    ctx.getRowValue(
                                        row,
                                        fieldName
                                    );

                                if (
                                    value !== null &&
                                    value !== undefined
                                ) {

                                    return encodeURIComponent(
                                        String(value)
                                    );
                                }
                            }

                            return "";
                        }
                    );
                };


            /*
             * =========================================================
             * CONFIRM
             * =========================================================
             */

            ctx.confirmAction =
                function (action) {

                    if (
                        !action ||
                        !action.confirm
                    ) {
                        return true;
                    }

                    return window.confirm(
                        action.confirmMessage ||
                        "Are you sure?"
                    );
                };


            /*
             * =========================================================
             * RESPONSE HANDLER
             * =========================================================
             */

            ctx.handleActionResponse =
                function (response) {

                    return response
                        .json()
                        .catch(
                            function () {

                                return {};
                            }
                        )
                        .then(
                            function (result) {

                                if (
                                    !response.ok
                                ) {

                                    throw new Error(
                                        result.message ||
                                        "Action failed."
                                    );
                                }

                                if (
                                    result &&
                                    result.success === false
                                ) {

                                    throw new Error(
                                        result.message ||
                                        "Action failed."
                                    );
                                }

                                return result;
                            }
                        );
                };


            /*
             * =========================================================
             * AJAX ACTION
             * =========================================================
             */

            ctx.executeAjaxAction =
                function (
                    action,
                    id,
                    extraData,
                    rowElement
                ) {

                    if (
                        !action ||
                        !action.url
                    ) {
                        return;
                    }

                    if (
                        !ctx.confirmAction(
                            action
                        )
                    ) {
                        return;
                    }

                    var method =
                        (
                            action.method ||
                            "POST"
                        ).toUpperCase();

                    var payload = {
                        TableID: ctx.options.id
                    };


                    if (id !== undefined && id !== null) {
                        payload.id = String(id);
                    }

                    if (rowElement) {
                        payload.keys = ctx.getRowKeys(rowElement);
                    }

                    if (extraData) {
                        Object.assign(payload, extraData);
                    }

                    ctx.addParentKeysToPayload(payload);

                    var requestOptions = {

                        method:
                            method,

                        headers: {

                            "Content-Type":
                                "application/json",

                            "RequestVerificationToken":
                                ctx.getAntiForgeryToken()
                        }
                    };


                    if (
                        method !== "GET" &&
                        method !== "HEAD"
                    ) {

                        requestOptions.body =
                            JSON.stringify(
                                payload
                            );
                    }


                    fetch(
                        action.url,
                        requestOptions
                    )
                        .then(
                            ctx.handleActionResponse
                        )
                        .then(
                            function () {

                                if (
                                    id !== null &&
                                    id !== undefined
                                ) {

                                    ctx.removeSelectedRow(
                                        id
                                    );
                                }

                                ctx.load();
                            }
                        )
                        .catch(
                            function (error) {

                                console.error(
                                    error
                                );

                                alert(
                                    error.message ||
                                    "Unable to execute action."
                                );
                            }
                        );
                };


            /*
             * =========================================================
             * BIND ROW ACTIONS
             * =========================================================
             */

            ctx.bindActionButtons =
                function () {

                    if (!ctx.body) {
                        return;
                    }


                    /*
                     * IMPORTANT:
                     *
                     * Bind only once.
                     *
                     * Rows are recreated with innerHTML after every load,
                     * therefore event delegation is used here.
                     */

                    if (
                        ctx.body.dataset.actionEventsBound === "true"
                    ) {
                        return;
                    }


                    ctx.body.dataset.actionEventsBound =
                        "true";


                    ctx.body.addEventListener(
                        "click",
                        function (event) {

                            var target =
                                event.target;

                            /*
                             * =====================================================
                             * AJAX ACTION
                             * =====================================================
                             */

                            var ajaxButton =
                                target.closest(
                                    "[data-table-action-ajax]"
                                );


                            if (ajaxButton) {

                                event.preventDefault();
                                event.stopPropagation();


                                var name =
                                    ajaxButton.getAttribute(
                                        "data-action-name"
                                    );

                                var id =
                                    ajaxButton.getAttribute(
                                        "data-id"
                                    );


                                var action =
                                    ctx.findAction(
                                        name
                                    );


                                if (!action) {
                                    return;
                                }

                                var rowElement = button.closest("tr");

                                ctx.executeAjaxAction(
                                    action,
                                    id,
                                    null,
                                    rowElement
                                );
                            }
                        }
                    );
                };


            /*
             * =========================================================
             * CREATE INPUT
             * =========================================================
             */

            ctx.createInlineInput =
                function (column) {

                    var field =
                        ctx.escapeHtml(
                            column.field
                        );

                    /*
                     * LOOKUP field
                     */

                    var lookup = column.lookup;

                    if (lookup && lookup.url && lookup.idField) {
                        var idField = ctx.escapeHtml(lookup.idField);

                        return (
                            '<button type="button" ' +
                            'class="btn btn-outline-primary btn-sm data-table-lookup-select" ' +
                            'data-lookup-column="' + field + '" ' +
                            'data-lookup-id-field="' + idField + '">' +
                            '<i class="bx bx-search me-1"></i>' +
                            ctx.escapeHtml(lookup.title || "Lookup") +
                            '</button>' +
                            '<input type="hidden" ' +
                            'class="data-table-inline-new-input" ' +
                            'data-field="' + idField + '" value="">'
                        );
                    }


                    if (
                        column.type ===
                        "Note"
                    ) {

                        return (
                            '<button type="button" ' +
                            'class="data-table-note-btn" disabled>' +
                            '<i class="bx bx-note"></i>' +
                            "</button>"
                        );
                    }


                    /*
                     * BOOLEAN
                     */

                    if (
                        column.type ===
                        "Boolean"
                    ) {

                        return (
                            '<input type="checkbox" ' +
                            'class="form-check-input data-table-inline-new-input" ' +
                            'data-field="' +
                            field +
                            '">'
                        );
                    }


                    /*
                     * NUMBER / CURRENCY
                     */

                    var inputMode =
                        "";

                    if (
                        column.type ===
                        "Number" ||
                        column.type ===
                        "Currency"
                    ) {

                        inputMode =
                            ' inputmode="decimal" ';
                    }


                    return (
                        '<input type="text" ' +
                        'class="form-control form-control-sm data-table-inline-new-input" ' +
                        'data-field="' +
                        field +
                        '"' +
                        inputMode +
                        '>'
                    );
                };


            /*
             * =========================================================
             * INLINE ADD
             * =========================================================
             */

            ctx.beginInlineAdd =
                function (action) {

                    if (
                        !action ||
                        !action.url
                    ) {

                        alert(
                            "Inline add URL is not configured."
                        );

                        return;
                    }


                    /*
                     * Only one new row.
                     */

                    var existing =
                        ctx.body.querySelector(
                            ".data-table-inline-new"
                        );


                    if (existing) {

                        var firstInput =
                            existing.querySelector(
                                ".data-table-inline-new-input"
                            );

                        if (firstInput) {
                            firstInput.focus();
                        }

                        return;
                    }


                    var columns =
                        options.columns;


                    var html =
                        '<tr class="data-table-inline-new">';


                    columns.forEach(
                        function (column) {

                            if (
                                !column.visible
                            ) {
                                return;
                            }


                            /*
                             * SELECTED
                             */

                            if (
                                options.rows &&
                                options.rows.selectable &&
                                column.field &&
                                column.field.toLowerCase() ===
                                "selected"
                            ) {

                                html +=
                                    '<td></td>';

                                return;
                            }


                            /*
                             * EDITABLE
                             */

                            if (
                                column.editable
                            ) {

                                html +=
                                    '<td data-field="' +
                                    ctx.escapeHtml(
                                        column.field
                                    ) +
                                    '">' +

                                    ctx.createInlineInput(
                                        column
                                    ) +

                                    '</td>';

                                return;
                            }


                            /*
                             * NON EDITABLE
                             */

                            html +=
                                '<td data-field="' +
                                ctx.escapeHtml(column.field) +
                                '">' +
                                ctx.escapeHtml(column.emptyText || "—") +
                                '</td>';
                        }
                    );


                    /*
                     * ACTIONS
                     */

                    html +=
                        '<td class="data-table-actions">' +

                        '<button type="button" ' +
                        'class="btn btn-sm btn-success data-table-inline-new-save" ' +
                        'title="Save" ' +
                        'aria-label="Save">' +

                        '<i class="bx bx-check"></i>' +

                        '</button>' +

                        '<button type="button" ' +
                        'class="btn btn-sm btn-outline-secondary data-table-inline-new-cancel" ' +
                        'title="Cancel" ' +
                        'aria-label="Cancel">' +

                        '<i class="bx bx-x"></i>' +

                        '</button>' +

                        '</td>';


                    html +=
                        '</tr>';


                    ctx.body.insertAdjacentHTML(
                        "afterbegin",
                        html
                    );


                    var row =
                        ctx.body.querySelector(
                            ".data-table-inline-new"
                        );


                    if (!row) {
                        return;
                    }

                    row.querySelectorAll(".data-table-lookup-select")
                        .forEach(function (button) {
                            button.addEventListener("click", function (event) {
                                event.preventDefault();
                                event.stopPropagation();

                                const field = button.getAttribute("data-lookup-column");

                                const column = columns.find(function (item) {
                                    return item.field === field;
                                });

                                if (column && column.lookup) {
                                    ctx.openLookup(row, column);
                                }
                            });
                        });


                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-new-save"
                        );


                    var cancelButton =
                        row.querySelector(
                            ".data-table-inline-new-cancel"
                        );


                    /*
                     * Numeric validation
                     */

                    row.querySelectorAll(
                        ".data-table-inline-new-input"
                    ).forEach(
                        function (input) {

                            var field =
                                input.getAttribute(
                                    "data-field"
                                );

                            var column =
                                columns.find(
                                    function (x) {

                                        return (
                                            x.field ===
                                            field
                                        );
                                    }
                                );


                            if (
                                !column
                            ) {
                                return;
                            }


                            if (
                                column.type ===
                                "Number" ||
                                column.type ===
                                "Currency"
                            ) {

                                input.addEventListener(
                                    "input",
                                    function () {

                                        this.value =
                                            this.value.replace(
                                                /[^0-9.-]/g,
                                                ""
                                            );
                                    }
                                );
                            }
                        }
                    );


                    if (saveButton) {

                        saveButton.addEventListener(
                            "click",
                            function (event) {

                                event.preventDefault();

                                event.stopPropagation();

                                ctx.saveInlineNewRow(
                                    row,
                                    action
                                );
                            }
                        );
                    }


                    if (cancelButton) {

                        cancelButton.addEventListener(
                            "click",
                            function (event) {

                                event.preventDefault();

                                event.stopPropagation();

                                row.remove();
                            }
                        );
                    }


                    var firstInput =
                        row.querySelector(
                            ".data-table-inline-new-input:not([type=hidden])"
                        );

                    var lookupButton = row.querySelector(
                        ".data-table-lookup-select"
                    );

                    if (lookupButton) {
                        lookupButton.focus();
                    } else if (firstInput) {
                        firstInput.focus();
                    }

                    if (firstInput) {

                        firstInput.focus();
                    }
                };


            /*
             * =========================================================
             * SAVE INLINE NEW ROW
             * =========================================================
             */

            ctx.saveInlineNewRow =
                function (
                    row,
                    action
                ) {

                    if (
                        !row ||
                        !action ||
                        !action.url
                    ) {
                        return;
                    }


                    var fields = {};


                    row.querySelectorAll(
                        ".data-table-inline-new-input"
                    ).forEach(
                        function (input) {

                            var field =
                                input.getAttribute(
                                    "data-field"
                                );


                            if (!field) {
                                return;
                            }


                            if (
                                input.type ===
                                "checkbox"
                            ) {

                                fields[field] =
                                    input.checked
                                        ? "true"
                                        : "false";

                            }
                            else {

                                fields[field] =
                                    input.value;
                            }
                        }
                    );

                    var lookupButton = row.querySelector(".data-table-lookup-select");

                    if (lookupButton) {
                        var idField = lookupButton.getAttribute("data-lookup-id-field");

                        var idInput = row.querySelector(
                            '.data-table-inline-new-input[data-field="' +
                            CSS.escape(idField) + '"]'
                        );

                        if (!idInput || !idInput.value) {
                            alert("Please select a record first.");
                            return;
                        }

                        fields[idField] = idInput.value;
                    }

                    /*
                     * Required fields
                     *
                     * Сейчас проверяем только
                     * пустые editable поля.
                     *
                     * ASP.NET validation всё равно
                     * остаётся главным.
                     */

                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-new-save"
                        );


                    var cancelButton =
                        row.querySelector(
                            ".data-table-inline-new-cancel"
                        );


                    if (saveButton) {

                        saveButton.disabled =
                            true;

                        saveButton.innerHTML =
                            '<i class="bx bx-loader-alt bx-spin"></i>';

                        saveButton.setAttribute(
                            "title",
                            "Saving..."
                        );
                    }


                    if (cancelButton) {

                        cancelButton.disabled =
                            true;
                    }


                    var method =
                        (
                            action.method ||
                            "POST"
                        ).toUpperCase();


                    var requestOptions = {

                        method:
                            method,

                        headers: {

                            "Content-Type":
                                "application/json",

                            "RequestVerificationToken":
                                ctx.getAntiForgeryToken()
                        },

                        body: JSON.stringify(
                            ctx.addParentKeysToPayload({
                                TableID: ctx.options.id,
                                fields: fields
                            })
                        )
                    };


                    fetch(
                        action.url,
                        requestOptions
                    )
                        .then(
                            ctx.handleActionResponse
                        )
                        .then(
                            function () {

                                ctx.load();
                            }
                        )
                        .catch(
                            function (error) {

                                console.error(
                                    error
                                );

                                alert(
                                    error.message ||
                                    "Unable to create record."
                                );


                                if (saveButton) {

                                    saveButton.disabled =
                                        false;

                                    saveButton.innerHTML =
                                        '<i class="bx bx-check"></i>';

                                    saveButton.setAttribute(
                                        "title",
                                        "Save"
                                    );
                                }


                                if (cancelButton) {

                                    cancelButton.disabled =
                                        false;
                                }
                            }
                        );
                };


            /*
             * =========================================================
             * RESET TABLE
             * =========================================================
             */

            ctx.resetTableToDefaultState =
                function () {

                    var currentOptions =
                        ctx.options;


                    if (ctx.state) {

                        ctx.state.page =
                            1;

                        ctx.state.search =
                            "";

                        ctx.state.filters =
                            {};

                        ctx.state.sortField =
                            currentOptions.sorting &&
                                currentOptions.sorting.defaultField
                                ? currentOptions.sorting.defaultField
                                : "";

                        ctx.state.sortDirection =
                            currentOptions.sorting &&
                                currentOptions.sorting.defaultDirection
                                ? currentOptions.sorting.defaultDirection
                                : "asc";
                    }


                    if (ctx.searchInput) {

                        ctx.searchInput.value =
                            "";
                    }


                    if (
                        ctx.syncFilterModalFromState
                    ) {

                        ctx.syncFilterModalFromState();
                    }


                    if (
                        ctx.updateFilterButton
                    ) {

                        ctx.updateFilterButton();
                    }


                    if (
                        ctx.updateSortIcons
                    ) {

                        ctx.updateSortIcons();
                    }


                    ctx.load();
                };

            /*
             * =========================================================
             * GENERIC LOOKUP
             * =========================================================
             */

            ctx.openLookup =
                function (row, column) {
                const lookup = column.lookup;

                if (!lookup || !lookup.url) {
                    return;
                }

                const payload = typeof ctx.addParentKeysToPayload === "function"
                    ? ctx.addParentKeysToPayload({})
                    : {};

                const parentKeys = payload.parentKeys || payload.ParentKeys || {};
                const parameters = {};

                Object.entries(lookup.parentKeyParameters || {})
                    .forEach(function ([queryName, parentKeyName]) {
                        const key = Object.keys(parentKeys).find(
                            key => key.toLowerCase() === parentKeyName.toLowerCase()
                        );

                        if (key && parentKeys[key] != null) {
                            parameters[queryName] = parentKeys[key];
                        }
                    });

                openDataTableLookup({
                    url: lookup.url,
                    title: lookup.title,
                    searchPlaceholder: lookup.searchPlaceholder,
                    columns: lookup.columns,
                    parameters: parameters,

                    onSelect: function (record) {
                        const idField = lookup.idField;
                        const idValue = record[lookup.recordIdField || "id"];

                        const hiddenInput = row.querySelector(
                            '.data-table-inline-new-input[data-field="' +
                            CSS.escape(idField) + '"]'
                        );

                        if (!hiddenInput || idValue == null) {
                            console.error("Lookup ID field or input is missing.");
                            return;
                        }

                        hiddenInput.value = idValue;
                        hiddenInput.dispatchEvent(
                            new Event("change", { bubbles: true })
                        );

                        const button = row.querySelector(
                            '.data-table-lookup-select[data-lookup-column="' +
                            CSS.escape(column.field) + '"]'
                        );

                        if (button) {
                            button.textContent =
                                record[lookup.displayField] ?? String(idValue);
                            button.classList.add("has-selection");
                        }

                        (lookup.mappings || []).forEach(function (mapping) {
                            const target = row.querySelector(
                                '[data-field="' +
                                CSS.escape(mapping.targetField) + '"]'
                            );

                            if (!target || (button && target.contains(button))) {
                                return;
                            }

                            target.textContent = record[mapping.sourceField] ?? "";
                        });
                    }
                });
            };
        },


        initialize: function (ctx) {

            /*
             * =========================================================
             * RELOAD
             * =========================================================
             */

            var reloadButton =
                ctx.root.querySelector(
                    "[data-table-reload]"
                );


            if (reloadButton) {

                reloadButton.addEventListener(
                    "click",
                    function () {

                        ctx.resetTableToDefaultState();
                    }
                );
            }


            /*
             * =========================================================
             * TOOLBAR ACTIONS
             * =========================================================
             */

            ctx.getToolbarActions()
                .forEach(
                    function (action) {

                        var buttons =
                            ctx.root.querySelectorAll(
                                '[data-table-action="' +
                                String(action.name)
                                    .replace(
                                        /"/g,
                                        '\\"'
                                    ) +
                                '"]'
                            );


                        buttons.forEach(
                            function (button) {

                                /*
                                 * LINK
                                 */

                                if (
                                    action.type ===
                                    "Link"
                                ) {
                                    return;
                                }


                                button.addEventListener(
                                    "click",
                                    function (event) {

                                        event.preventDefault();

                                        event.stopPropagation();


                                        /*
                                         * SELECTION
                                         */

                                        if (
                                            action.requiresSelection &&
                                            (
                                                !ctx.state.selectedRows ||
                                                ctx.state.selectedRows.length === 0
                                            )
                                        ) {

                                            alert(
                                                "Please select at least one record."
                                            );

                                            return;
                                        }


                                        /*
                                         * INLINE ADD
                                         */

                                        if (
                                            action.type ===
                                            "InlineAdd"
                                        ) {

                                            ctx.beginInlineAdd(
                                                action
                                            );

                                            return;
                                        }


                                        /*
                                         * AJAX
                                         */

                                        if (
                                            action.type ===
                                            "Ajax"
                                        ) {

                                            ctx.executeAjaxAction(
                                                action,
                                                null,
                                                {
                                                    selectedRows:
                                                        ctx.state.selectedRows || []
                                                }
                                            );

                                            return;
                                        }
                                    }
                                );
                            }
                        );
                    }
                );
        }
    };

})(window);