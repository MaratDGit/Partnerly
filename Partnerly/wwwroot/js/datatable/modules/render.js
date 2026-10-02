(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.render = {

        setup: function (ctx) {

            var options =
                ctx.options;


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

                            html +=
                                '<tr data-row-id="' +
                                ctx.escapeHtml(
                                    id
                                ) +
                                '">';


                            options.columns.forEach(
                                function (column) {

                                    if (
                                        !column.visible
                                    ) {
                                        return;
                                    }

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

                                    html +=
                                        '<td data-field="' +
                                        ctx.escapeHtml(
                                            column.field
                                        ) +
                                        '" data-original-value="' +
                                        ctx.escapeHtml(
                                            formatted.rawValue
                                        ) +
                                        '">' +
                                        formatted.html +
                                        "</td>";
                                }
                            );


                            if (
                                ctx.hasActionsColumn()
                            ) {

                                html +=
                                    '<td class="data-table-actions">';


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


                    ctx.body.innerHTML =
                        html;


                    ctx.bindInlineEditButtons();


                    ctx.highlightSearchText();
                };


            ctx.renderInfo =
                function (result) {

                    if (!ctx.info) {
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
        }
    };

})(window);