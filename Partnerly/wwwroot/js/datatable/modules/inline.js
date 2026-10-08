(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.inline = {

        setup: function (ctx) {

            var options =
                ctx.options;


            /*
             * =========================================================
             * BIND INLINE EDIT BUTTONS
             * =========================================================
             */

            ctx.bindInlineEditButtons =
                function () {

                    if (
                        !options.inlineEditing
                    ) {
                        return;
                    }


                    ctx.body
                        .querySelectorAll(
                            ".data-table-inline-edit"
                        )
                        .forEach(
                            function (button) {

                                button.addEventListener(
                                    "click",
                                    function (event) {

                                        event.preventDefault();

                                        event.stopPropagation();


                                        var id =
                                            button.getAttribute(
                                                "data-id"
                                            );


                                        if (!id) {
                                            return;
                                        }


                                        ctx.enterInlineEdit(
                                            id
                                        );
                                    }
                                );
                            }
                        );
                };

            /*
             * =========================================================
             * BIND INLINE DELETE BUTTONS
             * =========================================================
             */

            ctx.bindInlineDeleteButtons =
                function () {

                    if (!ctx.body) {
                        return;
                    }

                    /*
                     * Используем delegation.
                     *
                     * Строки таблицы пересоздаются через innerHTML,
                     * поэтому не нужно каждый раз навешивать
                     * отдельный event listener на каждую кнопку.
                     */

                    if (
                        ctx.body.dataset.inlineDeleteEventsBound === "true"
                    ) {
                        return;
                    }

                    ctx.body.dataset.inlineDeleteEventsBound =
                        "true";

                    ctx.body.addEventListener(
                        "click",
                        function (event) {

                            var target =
                                event.target;

                            var button =
                                target.closest(
                                    ".data-table-action-inline-delete"
                                );

                            if (!button) {
                                return;
                            }

                            event.preventDefault();
                            event.stopPropagation();

                            var actionName =
                                button.getAttribute(
                                    "data-action-name"
                                );

                            var id =
                                button.getAttribute(
                                    "data-id"
                                );

                            if (!actionName) {

                                console.error(
                                    "InlineDelete: action name is missing."
                                );

                                return;
                            }

                            if (!id) {

                                console.error(
                                    "InlineDelete: row id is missing."
                                );

                                return;
                            }

                            var action =
                                ctx.findAction(
                                    actionName
                                );

                            if (!action) {

                                console.error(
                                    "InlineDelete: action not found.",
                                    actionName
                                );

                                return;
                            }

                            ctx.executeInlineDelete(
                                action,
                                id,
                                button
                            );
                        }
                    );
                };

            /*
             * =========================================================
             * EXECUTE INLINE DELETE
             * =========================================================
             */

            ctx.executeInlineDelete =
                function (
                    action,
                    id,
                    button
                ) {

                    if (
                        !action ||
                        !action.url ||
                        !id
                    ) {

                        console.error(
                            "InlineDelete: invalid arguments.",
                            {
                                action: action,
                                id: id
                            }
                        );

                        return;
                    }


                    /*
                     * CONFIRM
                     */

                    if (
                        !ctx.confirmAction(
                            action
                        )
                    ) {
                        return;
                    }


                    /*
                     * BUTTON LOADING
                     */

                    if (button) {

                        button.disabled =
                            true;

                        button.innerHTML =
                            '<i class="bx bx-loader-alt bx-spin"></i>';

                        button.setAttribute(
                            "title",
                            "Deleting..."
                        );
                    }


                    var method =
                        (
                            action.method ||
                            "POST"
                        ).toUpperCase();


                    var payload = {

                        id:
                            String(id)
                    };


                    fetch(
                        action.url,
                        {

                            method:
                                method,

                            headers: {

                                "Content-Type":
                                    "application/json",

                                "RequestVerificationToken":
                                    ctx.getAntiForgeryToken()
                            },

                            body:
                                JSON.stringify(
                                    payload
                                )
                        }
                    )
                        .then(
                            function (response) {

                                return ctx.handleActionResponse(
                                    response
                                );
                            }
                        )
                        .then(
                            function () {

                                /*
                                 * Remove from selected rows
                                 */

                                if (
                                    ctx.removeSelectedRow
                                ) {

                                    ctx.removeSelectedRow(
                                        id
                                    );
                                }


                                /*
                                 * Reload table
                                 */

                                ctx.load();
                            }
                        )
                        .catch(
                            function (error) {

                                console.error(
                                    "InlineDelete error:",
                                    error
                                );


                                alert(
                                    error.message ||
                                    "Unable to delete record."
                                );


                                if (button) {

                                    button.disabled =
                                        false;

                                    button.innerHTML =
                                        '<i class="bx bx-trash"></i>';

                                    button.setAttribute(
                                        "title",
                                        action.title ||
                                        "Delete"
                                    );
                                }
                            }
                        );
                };

            /*
             * =========================================================
             * ENTER INLINE EDIT
             * =========================================================
             */

            ctx.enterInlineEdit =
                function (id) {

                    var rows =
                        ctx.body.querySelectorAll(
                            "tr[data-row-id]"
                        );


                    var row =
                        null;


                    rows.forEach(
                        function (item) {

                            if (
                                String(
                                    item.getAttribute(
                                        "data-row-id"
                                    )
                                ) ===
                                String(id)
                            ) {

                                row =
                                    item;
                            }
                        }
                    );


                    if (!row) {
                        return;
                    }


                    if (
                        row.classList.contains(
                            "data-table-editing"
                        )
                    ) {
                        return;
                    }


                    /*
                     * Don't allow editing
                     * while another new row exists.
                     */

                    var newRow =
                        ctx.body.querySelector(
                            ".data-table-inline-new"
                        );


                    if (newRow) {
                        return;
                    }


                    row.classList.add(
                        "data-table-editing"
                    );


                    options.columns.forEach(
                        function (column) {

                            if (
                                !column.visible ||
                                !column.editable
                            ) {
                                return;
                            }


                            var cell =
                                ctx.findCellByField(
                                    row,
                                    column.field
                                );


                            if (!cell) {
                                return;
                            }


                            var value =
                                cell.getAttribute(
                                    "data-original-value"
                                );


                            if (
                                value === null
                            ) {

                                value =
                                    cell.textContent.trim();


                                if (
                                    value ===
                                    column.emptyText
                                ) {

                                    value =
                                        "";
                                }


                                cell.setAttribute(
                                    "data-original-value",
                                    value
                                );
                            }


                            /*
                             * BOOLEAN
                             */

                            if (
                                column.type ===
                                "Boolean"
                            ) {

                                var checked =
                                    value === "true" ||
                                    value === "True" ||
                                    value === "TRUE" ||
                                    value === "1";


                                cell.innerHTML =
                                    '<input type="checkbox" ' +
                                    'class="form-check-input data-table-inline-input" ' +
                                    'data-field="' +
                                    ctx.escapeHtml(
                                        column.field
                                    ) +
                                    '"' +
                                    (
                                        checked
                                            ? " checked"
                                            : ""
                                    ) +
                                    '>';

                                return;
                            }


                            /*
                             * TEXT / NUMBER / CURRENCY
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


                            cell.innerHTML =
                                '<input type="text" ' +
                                'class="form-control form-control-sm data-table-inline-input" ' +
                                'data-field="' +
                                ctx.escapeHtml(
                                    column.field
                                ) +
                                '"' +
                                inputMode +
                                'value="' +
                                ctx.escapeHtml(
                                    value
                                ) +
                                '">';


                            /*
                             * NUMERIC VALIDATION
                             */

                            if (
                                column.type ===
                                "Number" ||
                                column.type ===
                                "Currency"
                            ) {

                                var numericInput =
                                    cell.querySelector(
                                        ".data-table-inline-input"
                                    );


                                if (
                                    numericInput
                                ) {

                                    numericInput.addEventListener(
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
                        }
                    );


                    /*
                     * ACTION BUTTONS
                     */

                    var actions =
                        row.querySelector(
                            ".data-table-actions"
                        );


                    if (!actions) {
                        return;
                    }


                    actions.innerHTML =

                        '<button type="button" ' +
                        'class="btn btn-sm btn-success data-table-inline-save" ' +
                        'title="Save" ' +
                        'aria-label="Save">' +

                        '<i class="bx bx-check"></i>' +

                        '</button>' +

                        '<button type="button" ' +
                        'class="btn btn-sm btn-outline-secondary data-table-inline-cancel" ' +
                        'title="Cancel" ' +
                        'aria-label="Cancel">' +

                        '<i class="bx bx-x"></i>' +

                        '</button>';


                    ctx.bindInlineSaveCancel(
                        row
                    );
                };


            /*
             * =========================================================
             * SAVE / CANCEL
             * =========================================================
             */

            ctx.bindInlineSaveCancel =
                function (row) {

                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-save"
                        );


                    var cancelButton =
                        row.querySelector(
                            ".data-table-inline-cancel"
                        );


                    if (
                        saveButton
                    ) {

                        saveButton.addEventListener(
                            "click",
                            function (event) {

                                event.preventDefault();

                                event.stopPropagation();

                                ctx.saveInlineRow(
                                    row
                                );
                            }
                        );
                    }


                    if (
                        cancelButton
                    ) {

                        cancelButton.addEventListener(
                            "click",
                            function (event) {

                                event.preventDefault();

                                event.stopPropagation();

                                ctx.cancelInlineRow(
                                    row
                                );
                            }
                        );
                    }
                };


            /*
             * =========================================================
             * CANCEL INLINE ROW
             * =========================================================
             */

            ctx.cancelInlineRow =
                function (row) {

                    if (!row) {
                        return;
                    }


                    /*
                     * Re-render current data.
                     *
                     * This is safer than trying to manually
                     * reconstruct every formatted cell.
                     */

                    ctx.load();
                };


            /*
             * =========================================================
             * SAVE INLINE ROW
             * =========================================================
             */

            ctx.saveInlineRow =
                function (row) {

                    var id =
                        row.getAttribute(
                            "data-row-id"
                        );


                    if (!id) {
                        return;
                    }


                    if (
                        !options.inlineUpdateUrl
                    ) {

                        alert(
                            "Inline update URL is not configured."
                        );

                        return;
                    }


                    var fields = {};


                    row.querySelectorAll(
                        ".data-table-inline-input"
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


                    var request = {

                        id:
                            String(id),

                        fields:
                            fields
                    };


                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-save"
                        );


                    var cancelButton =
                        row.querySelector(
                            ".data-table-inline-cancel"
                        );


                    if (
                        saveButton
                    ) {

                        saveButton.disabled =
                            true;

                        saveButton.innerHTML =
                            '<i class="bx bx-loader-alt bx-spin"></i>';

                        saveButton.setAttribute(
                            "title",
                            "Saving..."
                        );
                    }


                    if (
                        cancelButton
                    ) {

                        cancelButton.disabled =
                            true;
                    }


                    fetch(
                        options.inlineUpdateUrl,
                        {

                            method:
                                "POST",

                            headers: {

                                "Content-Type":
                                    "application/json",

                                "RequestVerificationToken":
                                    ctx.getAntiForgeryToken()
                            },

                            body:
                                JSON.stringify(
                                    request
                                )
                        }
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
                                    "Unable to save changes."
                                );


                                if (
                                    saveButton
                                ) {

                                    saveButton.disabled =
                                        false;

                                    saveButton.innerHTML =
                                        '<i class="bx bx-check"></i>';

                                    saveButton.setAttribute(
                                        "title",
                                        "Save"
                                    );
                                }


                                if (
                                    cancelButton
                                ) {

                                    cancelButton.disabled =
                                        false;
                                }
                            }
                        );
                };

            ctx.bindInlineDeleteButtons();
        }

        
    };

})(window);