(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.inline = {

        setup: function (ctx) {

            var options =
                ctx.options;


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
                                    function () {

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


                            if (
                                column.type ===
                                "Boolean"
                            ) {

                                var checked =
                                    value ===
                                    "true" ||
                                    value ===
                                    "True" ||
                                    value ===
                                    "1";


                                cell.innerHTML =
                                    '<input type="checkbox" ' +
                                    'class="form-check-input data-table-inline-input" ' +
                                    'data-field="' +
                                    ctx.escapeHtml(
                                        column.field
                                    ) +
                                    '" ' +
                                    (
                                        checked
                                            ? "checked"
                                            : ""
                                    ) +
                                    '>';
                            }
                            else {

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

                                    inputType =
                                        "text";

                                    inputMode =
                                        "numeric";
                                }


                                cell.innerHTML =
                                    '<input type="' +
                                    inputType +
                                    '" class="form-control form-control-sm data-table-inline-input" ' +
                                    'data-field="' +
                                    ctx.escapeHtml(
                                        column.field
                                    ) +
                                    '" ' +
                                    (
                                        inputMode
                                            ? 'inputmode="' +
                                            inputMode +
                                            '" '
                                            : ""
                                    ) +
                                    'value="' +
                                    ctx.escapeHtml(
                                        value
                                    ) +
                                    '">';


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
                                                        /[^0-9.]/g,
                                                        ""
                                                    );
                                            }
                                        );


                                        numericInput.addEventListener(
                                            "keydown",
                                            function (event) {

                                                var allowedKeys =
                                                    [
                                                        "Backspace",
                                                        "Delete",
                                                        "Tab",
                                                        "ArrowLeft",
                                                        "ArrowRight",
                                                        "Home",
                                                        "End"
                                                    ];


                                                if (
                                                    allowedKeys.indexOf(
                                                        event.key
                                                    ) !==
                                                    -1
                                                ) {
                                                    return;
                                                }


                                                if (
                                                    event.ctrlKey ||
                                                    event.metaKey
                                                ) {
                                                    return;
                                                }


                                                if (
                                                    event.key >=
                                                    "0" &&
                                                    event.key <=
                                                    "9"
                                                ) {
                                                    return;
                                                }


                                                if (
                                                    event.key ===
                                                    "." &&
                                                    this.value.indexOf(
                                                        "."
                                                    ) ===
                                                    -1
                                                ) {
                                                    return;
                                                }


                                                event.preventDefault();
                                            }
                                        );
                                    }
                                }
                            }
                        }
                    );


                    var actions =
                        row.querySelector(
                            ".data-table-actions"
                        );


                    if (!actions) {
                        return;
                    }


                    actions.innerHTML =
                        '<button type="button" class="btn btn-sm btn-success data-table-inline-save" ' +
                        'title="Save" aria-label="Save">' +
                        '<i class="bx bx-check"></i>' +
                        '</button> ' +
                        '<button type="button" class="btn btn-sm btn-outline-secondary data-table-inline-cancel" ' +
                        'title="Cancel" aria-label="Cancel">' +
                        '<i class="bx bx-x"></i>' +
                        '</button>';


                    ctx.bindInlineSaveCancel(
                        row
                    );
                };


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
                            function () {

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
                            function () {

                                ctx.load();
                            }
                        );
                    }
                };


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


                    var fields =
                        {};


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
                            id,

                        fields:
                            fields
                    };


                    var saveButton =
                        row.querySelector(
                            ".data-table-inline-save"
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
                            function (response) {

                                return response
                                    .json()
                                    .then(
                                        function (
                                            result
                                        ) {

                                            if (
                                                !response.ok
                                            ) {

                                                throw new Error(
                                                    result.message ||
                                                    "Update failed."
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
                                    !result.success
                                ) {

                                    throw new Error(
                                        result.message ||
                                        "Update failed."
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
                            }
                        );
                };
        }
    };

})(window);