(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.columns = {

        setup: function (ctx) {

            var options =
                ctx.options;


            ctx.buildColumnUrl =
                function (
                    template,
                    row,
                    rowId
                ) {

                    if (!template) {
                        return "";
                    }

                    return String(
                        template
                    ).replace(
                        /\{([^}]+)\}/g,
                        function (
                            match,
                            fieldName
                        ) {

                            if (
                                fieldName.toLowerCase() ===
                                "id" &&
                                rowId !== null &&
                                rowId !== undefined
                            ) {

                                return encodeURIComponent(
                                    String(rowId)
                                );
                            }

                            var value =
                                ctx.getRowValue(
                                    row,
                                    fieldName
                                );

                            return (
                                value === null ||
                                value === undefined
                            )
                                ? ""
                                : encodeURIComponent(
                                    String(value)
                                );
                        }
                    );
                };


            ctx.formatCellValue =
                function (
                    value,
                    column,
                    row,
                    rowId
                ) {

                    if (
                        value === null ||
                        value === undefined ||
                        value === ""
                    ) {

                        return {

                            html:
                                ctx.escapeHtml(
                                    column.emptyText
                                ),

                            rawValue:
                                ""
                        };
                    }


                    if (
                        column.type ===
                        "Boolean"
                    ) {

                        var checked =
                            value === true ||
                            value === "true" ||
                            value === "True" ||
                            value === 1 ||
                            value === "1";

                        return {

                            html:
                                '<input type="checkbox" ' +
                                'class="form-check-input data-table-boolean" ' +
                                (
                                    checked
                                        ? "checked "
                                        : ""
                                ) +
                                'disabled>',

                            rawValue:
                                checked
                                    ? "true"
                                    : "false"
                        };
                    }


                    if (
                        column.type ===
                        "Number"
                    ) {

                        var numberValue =
                            Number(value);

                        if (
                            !isNaN(
                                numberValue
                            )
                        ) {

                            return {

                                html:
                                    ctx.escapeHtml(
                                        numberValue.toLocaleString()
                                    ),

                                rawValue:
                                    String(value)
                            };
                        }

                        return {

                            html:
                                ctx.escapeHtml(
                                    value
                                ),

                            rawValue:
                                String(value)
                        };
                    }


                    if (
                        column.type ===
                        "Currency"
                    ) {

                        var currencyValue =
                            Number(value);

                        if (
                            !isNaN(
                                currencyValue
                            )
                        ) {

                            return {

                                html:
                                    ctx.escapeHtml(
                                        currencyValue.toLocaleString(
                                            undefined,
                                            {
                                                minimumFractionDigits: 2,
                                                maximumFractionDigits: 2
                                            }
                                        )
                                    ),

                                rawValue:
                                    String(value)
                            };
                        }

                        return {

                            html:
                                ctx.escapeHtml(
                                    value
                                ),

                            rawValue:
                                String(value)
                        };
                    }


                    if (
                        column.type ===
                        "Date"
                    ) {

                        return {

                            html:
                                ctx.escapeHtml(
                                    ctx.formatDate(
                                        value,
                                        column.format
                                    )
                                ),

                            rawValue:
                                String(value)
                        };
                    }


                    if (
                        column.type ===
                        "DateTime"
                    ) {

                        return {

                            html:
                                ctx.escapeHtml(
                                    ctx.formatDateTime(
                                        value,
                                        column.format
                                    )
                                ),

                            rawValue:
                                String(value)
                        };
                    }


                    if (
                        column.type ===
                        "Email"
                    ) {

                        var email =
                            String(value);

                        return {

                            html:
                                '<a href="mailto:' +
                                ctx.escapeHtml(
                                    email
                                ) +
                                '">' +
                                ctx.escapeHtml(
                                    email
                                ) +
                                "</a>",

                            rawValue:
                                email
                        };
                    }


                    if (
                        column.type ===
                        "Link"
                    ) {

                        var linkValue =
                            value;

                        if (
                            linkValue === null ||
                            linkValue === undefined ||
                            String(
                                linkValue
                            ).trim() === ""
                        ) {

                            return {

                                html:
                                    ctx.escapeHtml(
                                        column.emptyText ||
                                        "—"
                                    ),

                                rawValue:
                                    ""
                            };
                        }

                        var href =
                            ctx.buildColumnUrl(
                                column.urlTemplate,
                                row,
                                rowId
                            );

                        if (!href) {

                            return {

                                html:
                                    ctx.escapeHtml(
                                        String(
                                            linkValue
                                        )
                                    ),

                                rawValue:
                                    String(
                                        linkValue
                                    )
                            };
                        }

                        return {

                            html:
                                '<a href="' +
                                ctx.escapeHtml(
                                    href
                                ) +
                                '" class="data-table-link">' +
                                ctx.escapeHtml(
                                    String(
                                        linkValue
                                    )
                                ) +
                                "</a>",

                            rawValue:
                                String(
                                    linkValue
                                )
                        };
                    }


                    if (
                        column.type ===
                        "Badge"
                    ) {

                        return {

                            html:
                                '<span class="data-table-badge">' +
                                ctx.escapeHtml(
                                    value
                                ) +
                                "</span>",

                            rawValue:
                                String(value)
                        };
                    }


                    return {

                        html:
                            ctx.escapeHtml(
                                value
                            ),

                        rawValue:
                            String(value)
                    };
                };


            ctx.hasEditableColumns =
                function () {

                    if (
                        !options.inlineEditing
                    ) {
                        return false;
                    }

                    return options.columns.some(
                        function (column) {

                            return (
                                column.visible &&
                                column.editable
                            );
                        }
                    );
                };


            ctx.hasActionsColumn =
                function () {

                    if (
                        options.inlineEditing &&
                        ctx.hasEditableColumns()
                    ) {
                        return true;
                    }

                    if (
                        !options.actions ||
                        !options.actions.enabled
                    ) {
                        return false;
                    }

                    return (

                        (
                            options.actions.view &&
                            options.actions.viewUrl
                        ) ||

                        (
                            options.actions.edit &&
                            options.actions.editUrl
                        ) ||

                        (
                            options.actions.delete &&
                            options.actions.deleteUrl
                        )
                    );
                };
        }
    };

})(window);