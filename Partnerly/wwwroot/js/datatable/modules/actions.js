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
             * ACTION TYPE
             * =========================================================
             */

            ctx.hasActionType =
                function (type) {

                    return ctx.getActions()
                        .some(
                            function (action) {

                                return (
                                    action &&
                                    action.type ===
                                    type
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
                            String(
                                actions[i].name
                            ) ===
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
             * AJAX ACTION
             * =========================================================
             */

            ctx.executeAjaxAction =
                function (
                    action,
                    id,
                    extraData
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

                    var payload = {};


                    if (
                        id !== null &&
                        id !== undefined
                    ) {

                        payload.id =
                            String(id);
                    }


                    if (extraData) {

                        Object.keys(
                            extraData
                        ).forEach(
                            function (key) {

                                payload[key] =
                                    extraData[key];
                            }
                        );
                    }


                    var requestOptions = {

                        method: method,

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
                            function (response) {

                                return response
                                    .json()
                                    .catch(
                                        function () {

                                            return {};
                                        }
                                    )
                                    .then(
                                        function (
                                            result
                                        ) {

                                            if (
                                                !response.ok
                                            ) {

                                                throw new Error(
                                                    result.message ||
                                                    "Action failed."
                                                );
                                            }

                                            return result;
                                        }
                                    );
                            }
                        )
                        .then(
                            function (result) {

                                if (
                                    result &&
                                    result.success === false
                                ) {

                                    throw new Error(
                                        result.message ||
                                        "Action failed."
                                    );
                                }


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
             * INLINE DELETE
             * =========================================================
             */

            ctx.executeInlineDelete =
                function (
                    action,
                    id
                ) {

                    if (
                        !action ||
                        !id
                    ) {
                        return;
                    }

                    ctx.executeAjaxAction(
                        action,
                        id
                    );
                };


            /*
             * =========================================================
             * BIND ROW ACTIONS
             * =========================================================
             */

            ctx.bindActionButtons =
                function () {

                    /*
                     * INLINE DELETE
                     */

                    ctx.body
                        .querySelectorAll(
                            "[data-table-action-inline-delete]"
                        )
                        .forEach(
                            function (button) {

                                button.addEventListener(
                                    "click",
                                    function () {

                                        var name =
                                            button.getAttribute(
                                                "data-action-name"
                                            );

                                        var id =
                                            button.getAttribute(
                                                "data-id"
                                            );

                                        var action =
                                            ctx.findAction(
                                                name
                                            );

                                        if (
                                            !action ||
                                            !id
                                        ) {
                                            return;
                                        }

                                        ctx.executeInlineDelete(
                                            action,
                                            id
                                        );
                                    }
                                );
                            }
                        );


                    /*
                     * AJAX
                     */

                    ctx.body
                        .querySelectorAll(
                            "[data-table-action-ajax]"
                        )
                        .forEach(
                            function (button) {

                                button.addEventListener(
                                    "click",
                                    function () {

                                        var name =
                                            button.getAttribute(
                                                "data-action-name"
                                            );

                                        var id =
                                            button.getAttribute(
                                                "data-id"
                                            );

                                        var action =
                                            ctx.findAction(
                                                name
                                            );

                                        if (!action) {
                                            return;
                                        }

                                        ctx.executeAjaxAction(
                                            action,
                                            id
                                        );
                                    }
                                );
                            }
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
                     * Не разрешаем несколько
                     * новых строк одновременно.
                     */

                    var existing =
                        ctx.body.querySelector(
                            ".data-table-inline-new"
                        );

                    if (existing) {
                        return;
                    }


                    var columns =
                        options.columns;

                    var html =
                        '<tr class="data-table-inline-new">';


                    columns.forEach(
                        function (column) {

                            if (!column.visible) {
                                return;
                            }


                            /*
                             * Selected
                             */

                            if (
                                options.rows &&
                                options.rows.selectable &&
                                column.field.toLowerCase() ===
                                "selected"
                            ) {

                                html +=
                                    "<td></td>";

                                return;
                            }


                            /*
                             * Editable
                             */

                            if (column.editable) {

                                var inputType =
                                    "text";

                                var inputMode =
                                    "";


                                if (
                                    column.type ===
                                    "Number" ||
                                    column.type ===
                                    "Currency"
                                ) {

                                    inputMode =
                                        "numeric";
                                }


                                if (
                                    column.type ===
                                    "Boolean"
                                ) {

                                    html +=
                                        '<td data-field="' +
                                        ctx.escapeHtml(
                                            column.field
                                        ) +
                                        '">' +

                                        '<input type="checkbox" ' +
                                        'class="form-check-input data-table-inline-new-input" ' +
                                        'data-field="' +
                                        ctx.escapeHtml(
                                            column.field
                                        ) +
                                        '">' +

                                        "</td>";

                                    return;
                                }


                                html +=
                                    '<td data-field="' +
                                    ctx.escapeHtml(
                                        column.field
                                    ) +
                                    '">' +

                                    '<input type="' +
                                    inputType +
                                    '" ' +
                                    'class="form-control form-control-sm data-table-inline-new-input" ' +
                                    'data-field="' +
                                    ctx.escapeHtml(
                                        column.field
                                    ) +
                                    '"' +
                                    (
                                        inputMode
                                            ? ' inputmode="' +
                                            inputMode +
                                            '"'
                                            : ""
                                    ) +
                                    ">" +

                                    "</td>";

                                return;
                            }


                            html +=
                                "<td>" +
                                ctx.escapeHtml(
                                    column.emptyText ||
                                    "—"
                                ) +
                                "</td>";
                        }
                    );


                    html +=
                        '<td class="data-table-actions">' +

                        '<button type="button" ' +
                        'class="btn btn-sm btn-success data-table-inline-new-save" ' +
                        'title="Save" aria-label="Save">' +
                        '<i class="bx bx-check"></i>' +
                        "</button> " +

                        '<button type="button" ' +
                        'class="btn btn-sm btn-outline-secondary data-table-inline-new-cancel" ' +
                        'title="Cancel" aria-label="Cancel">' +
                        '<i class="bx bx-x"></i>' +
                        "</button>" +

                        "</td>";


                    html +=
                        "</tr>";


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


                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-new-save"
                        );

                    var cancelButton =
                        row.querySelector(
                            ".data-table-inline-new-cancel"
                        );


                    if (saveButton) {

                        saveButton.addEventListener(
                            "click",
                            function () {

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
                            function () {

                                row.remove();
                            }
                        );
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


                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-new-save"
                        );


                    if (saveButton) {

                        saveButton.disabled =
                            true;

                        saveButton.innerHTML =
                            '<i class="bx bx-loader-alt bx-spin"></i>';
                    }


                    var method =
                        (
                            action.method ||
                            "POST"
                        ).toUpperCase();


                    fetch(
                        action.url,
                        {

                            method: method,

                            headers: {

                                "Content-Type":
                                    "application/json",

                                "RequestVerificationToken":
                                    ctx.getAntiForgeryToken()
                            },

                            body:
                                JSON.stringify({
                                    fields: fields
                                })
                        }
                    )
                        .then(
                            function (response) {

                                return response
                                    .json()
                                    .catch(
                                        function () {

                                            return {};
                                        }
                                    )
                                    .then(
                                        function (
                                            result
                                        ) {

                                            if (
                                                !response.ok
                                            ) {

                                                throw new Error(
                                                    result.message ||
                                                    "Create failed."
                                                );
                                            }

                                            return result;
                                        }
                                    );
                            }
                        )
                        .then(
                            function (result) {

                                if (
                                    result &&
                                    result.success === false
                                ) {

                                    throw new Error(
                                        result.message ||
                                        "Create failed."
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
                                    "Unable to create record."
                                );


                                if (saveButton) {

                                    saveButton.disabled =
                                        false;

                                    saveButton.innerHTML =
                                        '<i class="bx bx-check"></i>';
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
                                 * Link не перехватываем.
                                 *
                                 * Обычный <a href="...">
                                 * должен работать нативно.
                                 */

                                if (
                                    action.type ===
                                    "Link"
                                ) {
                                    return;
                                }


                                button.addEventListener(
                                    "click",
                                    function () {

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


                                        if (
                                            action.type ===
                                            "InlineAdd"
                                        ) {

                                            ctx.beginInlineAdd(
                                                action
                                            );

                                            return;
                                        }


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