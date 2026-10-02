(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.columns = {

        setup: function (ctx) {

            var options =
                ctx.options;


            /*
             * =========================================================
             * COLUMN ALIGNMENT
             * =========================================================
             */

            ctx.getColumnAlignmentClass =
                function (column) {

                    if (!column || !column.alignment) {
                        return "";
                    }

                    switch (column.alignment) {

                        case "Center":
                            return "data-table-align-center";

                        case "Right":
                            return "data-table-align-right";

                        case "Left":
                        default:
                            return "data-table-align-left";
                    }
                };


            /*
             * =========================================================
             * CONDITIONAL STYLES
             * =========================================================
             *
             * One ConditionalStyle = one style group.
             *
             * All conditions inside the group must match.
             *
             * Example:
             *
             * Balance > 100 AND Balance < 500
             *
             * Multiple groups are checked in order.
             * The first matching group is returned.
             *
             * =========================================================
             */

            ctx.evaluateConditionalCondition =
                function (
                    value,
                    operatorName,
                    expectedValue
                ) {

                    var actual =
                        value === null ||
                            value === undefined
                            ? ""
                            : String(value);

                    var expected =
                        expectedValue === null ||
                            expectedValue === undefined
                            ? ""
                            : String(expectedValue);


                    switch (operatorName) {

                        case "Equals":
                            return actual === expected;


                        case "NotEquals":
                            return actual !== expected;


                        case "GreaterThan":
                            return (
                                Number(actual) >
                                Number(expected)
                            );


                        case "GreaterThanOrEqual":
                            return (
                                Number(actual) >=
                                Number(expected)
                            );


                        case "LessThan":
                            return (
                                Number(actual) <
                                Number(expected)
                            );


                        case "LessThanOrEqual":
                            return (
                                Number(actual) <=
                                Number(expected)
                            );


                        case "Contains":
                            return actual
                                .toLowerCase()
                                .indexOf(
                                    expected.toLowerCase()
                                ) !== -1;


                        case "StartsWith":
                            return actual
                                .toLowerCase()
                                .indexOf(
                                    expected.toLowerCase()
                                ) === 0;


                        case "EndsWith":
                            return (
                                actual
                                    .toLowerCase()
                                    .slice(
                                        -expected.length
                                    ) ===
                                expected.toLowerCase()
                            );


                        default:
                            return false;
                    }
                };


            ctx.getConditionalStyle =
                function (
                    value,
                    column
                ) {

                    if (
                        !column ||
                        !column.conditionalStyles ||
                        !Array.isArray(
                            column.conditionalStyles
                        ) ||
                        value === null ||
                        value === undefined
                    ) {
                        return null;
                    }


                    var conditionalStyles =
                        column.conditionalStyles;


                    /*
                     * =================================================
                     * CHECK GROUPS
                     * =================================================
                     */

                    for (
                        var i = 0;
                        i < conditionalStyles.length;
                        i++
                    ) {

                        var conditionalStyle =
                            conditionalStyles[i];


                        if (
                            !conditionalStyle ||
                            !conditionalStyle.conditions ||
                            !Array.isArray(
                                conditionalStyle.conditions
                            ) ||
                            conditionalStyle.conditions.length === 0
                        ) {
                            continue;
                        }


                        /*
                         * =============================================
                         * ALL CONDITIONS MUST MATCH
                         * =============================================
                         */

                        var matches =
                            true;


                        for (
                            var j = 0;
                            j < conditionalStyle.conditions.length;
                            j++
                        ) {

                            var condition =
                                conditionalStyle.conditions[j];


                            if (
                                !condition ||
                                !condition.operatorName
                            ) {

                                matches =
                                    false;

                                break;
                            }


                            var conditionMatches =
                                ctx.evaluateConditionalCondition(
                                    value,
                                    condition.operatorName,
                                    condition.value
                                );


                            if (!conditionMatches) {

                                matches =
                                    false;

                                break;
                            }
                        }


                        /*
                         * =============================================
                         * FIRST MATCHING GROUP WINS
                         * =============================================
                         */

                        if (matches) {
                            return conditionalStyle;
                        }
                    }


                    return null;
                };


            ctx.getConditionalStyleClass =
                function (
                    value,
                    column
                ) {

                    var conditionalStyle =
                        ctx.getConditionalStyle(
                            value,
                            column
                        );


                    if (
                        !conditionalStyle ||
                        !conditionalStyle.cssClass
                    ) {
                        return "";
                    }


                    return String(
                        conditionalStyle.cssClass
                    );
                };


            ctx.getConditionalStyleAttribute =
                function (
                    value,
                    column
                ) {

                    var conditionalStyle =
                        ctx.getConditionalStyle(
                            value,
                            column
                        );


                    if (
                        !conditionalStyle ||
                        !conditionalStyle.style
                    ) {
                        return "";
                    }


                    return String(
                        conditionalStyle.style
                    );
                };


            /*
             * =========================================================
             * APPLY CONDITIONAL STYLE
             * =========================================================
             */

            ctx.applyConditionalStyle =
                function (
                    html,
                    value,
                    column
                ) {

                    var conditionalStyle =
                        ctx.getConditionalStyle(
                            value,
                            column
                        );


                    if (!conditionalStyle) {
                        return html;
                    }


                    var cssClass =
                        conditionalStyle.cssClass || "";


                    var style =
                        conditionalStyle.style || "";


                    if (
                        !cssClass &&
                        !style
                    ) {
                        return html;
                    }


                    return (
                        '<span' +
                        (
                            cssClass
                                ? ' class="' +
                                ctx.escapeHtml(
                                    cssClass
                                ) +
                                '"'
                                : ""
                        ) +
                        (
                            style
                                ? ' style="' +
                                ctx.escapeHtml(
                                    style
                                ) +
                                '"'
                                : ""
                        ) +
                        ">" +
                        html +
                        "</span>"
                    );
                };


            /*
             * =========================================================
             * BUILD COLUMN URL
             * =========================================================
             */

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


            /*
             * =========================================================
             * FORMAT CELL VALUE
             * =========================================================
             */

            ctx.formatCellValue =
                function (
                    value,
                    column,
                    row,
                    rowId
                ) {

                    /*
                     * =================================================
                     * EMPTY
                     * =================================================
                     */

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


                    /*
                     * =================================================
                     * BOOLEAN
                     * =================================================
                     */

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


                    /*
                     * =================================================
                     * NUMBER
                     * =================================================
                     */

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

                            var numberHtml =
                                ctx.escapeHtml(
                                    numberValue.toLocaleString()
                                );

                            numberHtml =
                                ctx.applyConditionalStyle(
                                    numberHtml,
                                    value,
                                    column
                                );

                            return {

                                html:
                                    numberHtml,

                                rawValue:
                                    String(value)
                            };
                        }


                        var invalidNumberHtml =
                            ctx.escapeHtml(
                                value
                            );

                        invalidNumberHtml =
                            ctx.applyConditionalStyle(
                                invalidNumberHtml,
                                value,
                                column
                            );

                        return {

                            html:
                                invalidNumberHtml,

                            rawValue:
                                String(value)
                        };
                    }


                    /*
                     * =================================================
                     * CURRENCY
                     * =================================================
                     */

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

                            var currencyHtml =
                                ctx.escapeHtml(
                                    currencyValue.toLocaleString(
                                        undefined,
                                        {
                                            minimumFractionDigits: 2,
                                            maximumFractionDigits: 2
                                        }
                                    )
                                );

                            currencyHtml =
                                ctx.applyConditionalStyle(
                                    currencyHtml,
                                    value,
                                    column
                                );

                            return {

                                html:
                                    currencyHtml,

                                rawValue:
                                    String(value)
                            };
                        }


                        var invalidCurrencyHtml =
                            ctx.escapeHtml(
                                value
                            );

                        invalidCurrencyHtml =
                            ctx.applyConditionalStyle(
                                invalidCurrencyHtml,
                                value,
                                column
                            );

                        return {

                            html:
                                invalidCurrencyHtml,

                            rawValue:
                                String(value)
                        };
                    }


                    /*
                     * =================================================
                     * DATE
                     * =================================================
                     */

                    if (
                        column.type ===
                        "Date"
                    ) {

                        var dateHtml =
                            ctx.escapeHtml(
                                ctx.formatDate(
                                    value,
                                    column.format
                                )
                            );

                        dateHtml =
                            ctx.applyConditionalStyle(
                                dateHtml,
                                value,
                                column
                            );

                        return {

                            html:
                                dateHtml,

                            rawValue:
                                String(value)
                        };
                    }


                    /*
                     * =================================================
                     * DATETIME
                     * =================================================
                     */

                    if (
                        column.type ===
                        "DateTime"
                    ) {

                        var dateTimeHtml =
                            ctx.escapeHtml(
                                ctx.formatDateTime(
                                    value,
                                    column.format
                                )
                            );

                        dateTimeHtml =
                            ctx.applyConditionalStyle(
                                dateTimeHtml,
                                value,
                                column
                            );

                        return {

                            html:
                                dateTimeHtml,

                            rawValue:
                                String(value)
                        };
                    }


                    /*
                     * =================================================
                     * EMAIL
                     * =================================================
                     */

                    if (
                        column.type ===
                        "Email"
                    ) {

                        var email =
                            String(value);


                        var emailClass =
                            "data-table-link";


                        var emailConditionalClass =
                            ctx.getConditionalStyleClass(
                                value,
                                column
                            );


                        if (emailConditionalClass) {

                            emailClass +=
                                " " +
                                emailConditionalClass;
                        }


                        var emailConditionalStyle =
                            ctx.getConditionalStyleAttribute(
                                value,
                                column
                            );


                        return {

                            html:
                                '<a href="mailto:' +
                                ctx.escapeHtml(
                                    email
                                ) +
                                '"' +
                                ' class="' +
                                ctx.escapeHtml(
                                    emailClass
                                ) +
                                '"' +
                                (
                                    emailConditionalStyle
                                        ? ' style="' +
                                        ctx.escapeHtml(
                                            emailConditionalStyle
                                        ) +
                                        '"'
                                        : ""
                                ) +
                                '>' +
                                ctx.escapeHtml(
                                    email
                                ) +
                                "</a>",

                            rawValue:
                                email
                        };
                    }


                    /*
                     * =================================================
                     * LINK
                     * =================================================
                     */

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

                            var plainLinkHtml =
                                ctx.escapeHtml(
                                    String(
                                        linkValue
                                    )
                                );

                            plainLinkHtml =
                                ctx.applyConditionalStyle(
                                    plainLinkHtml,
                                    linkValue,
                                    column
                                );

                            return {

                                html:
                                    plainLinkHtml,

                                rawValue:
                                    String(
                                        linkValue
                                    )
                            };
                        }


                        var linkClass =
                            "data-table-link";


                        var linkConditionalClass =
                            ctx.getConditionalStyleClass(
                                linkValue,
                                column
                            );


                        if (linkConditionalClass) {

                            linkClass +=
                                " " +
                                linkConditionalClass;
                        }


                        var linkConditionalStyle =
                            ctx.getConditionalStyleAttribute(
                                linkValue,
                                column
                            );


                        return {

                            html:
                                '<a href="' +
                                ctx.escapeHtml(
                                    href
                                ) +
                                '"' +
                                ' class="' +
                                ctx.escapeHtml(
                                    linkClass
                                ) +
                                '"' +
                                (
                                    linkConditionalStyle
                                        ? ' style="' +
                                        ctx.escapeHtml(
                                            linkConditionalStyle
                                        ) +
                                        '"'
                                        : ""
                                ) +
                                '>' +
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


                    /*
                     * =================================================
                     * BADGE
                     * =================================================
                     */

                    if (
                        column.type ===
                        "Badge"
                    ) {

                        var badgeClass =
                            "data-table-badge";


                        var badgeConditionalClass =
                            ctx.getConditionalStyleClass(
                                value,
                                column
                            );


                        if (badgeConditionalClass) {

                            badgeClass +=
                                " " +
                                badgeConditionalClass;
                        }


                        var badgeConditionalStyle =
                            ctx.getConditionalStyleAttribute(
                                value,
                                column
                            );


                        return {

                            html:
                                '<span class="' +
                                ctx.escapeHtml(
                                    badgeClass
                                ) +
                                '"' +
                                (
                                    badgeConditionalStyle
                                        ? ' style="' +
                                        ctx.escapeHtml(
                                            badgeConditionalStyle
                                        ) +
                                        '"'
                                        : ""
                                ) +
                                '>' +
                                ctx.escapeHtml(
                                    value
                                ) +
                                "</span>",

                            rawValue:
                                String(value)
                        };
                    }


                    /*
                     * =================================================
                     * DEFAULT TEXT
                     * =================================================
                     */

                    var textHtml =
                        ctx.escapeHtml(
                            value
                        );

                    textHtml =
                        ctx.applyConditionalStyle(
                            textHtml,
                            value,
                            column
                        );

                    return {

                        html:
                            textHtml,

                        rawValue:
                            String(value)
                    };
                };


            /*
             * =========================================================
             * EDITABLE COLUMNS
             * =========================================================
             */

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


            /*
             * =========================================================
             * ACTIONS COLUMN
             * =========================================================
             */

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