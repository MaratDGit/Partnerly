(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.export = {

        setup: function (ctx) {

            ctx.exportButtons =
                ctx.root.querySelectorAll(
                    "[data-table-export]"
                );


            ctx.exportData =
                function (format) {

                    if (!format) {
                        return;
                    }

                    if (
                        !ctx.options.exportUrl
                    ) {

                        console.error(
                            "DataTable exportUrl is not configured."
                        );

                        return;
                    }


                    var url =
                        new URL(
                            ctx.options.exportUrl,
                            window.location.origin
                        );


                    var state =
                        ctx.state;


                    if (
                        state.search &&
                        String(state.search).length > 0
                    ) {

                        url.searchParams.set(
                            "Search",
                            state.search
                        );
                    }


                    if (
                        state.sortField
                    ) {

                        url.searchParams.set(
                            "SortField",
                            state.sortField
                        );

                        url.searchParams.set(
                            "SortDirection",
                            state.sortDirection || "asc"
                        );
                    }


                    if (
                        state.filters
                    ) {

                        Object.keys(
                            state.filters
                        ).forEach(
                            function (field) {

                                var values =
                                    state.filters[field];

                                if (!values) {
                                    return;
                                }

                                if (!Array.isArray(values)) {
                                    values = [values];
                                }

                                values.forEach(
                                    function (value) {

                                        if (
                                            value === null ||
                                            value === undefined ||
                                            value === ""
                                        ) {
                                            return;
                                        }

                                        url.searchParams.append(
                                            "Filters[" +
                                            field +
                                            "]",
                                            value
                                        );
                                    }
                                );
                            }
                        );
                    }


                    if (
                        state.selectedRows &&
                        state.selectedRows.length > 0
                    ) {

                        state.selectedRows.forEach(
                            function (id) {

                                url.searchParams.append(
                                    "SelectedRows",
                                    id
                                );
                            }
                        );
                    }


                    url.searchParams.set(
                        "Format",
                        format
                    );


                    window.location.href =
                        url.toString();
                };


            ctx.exportButtons.forEach(
                function (button) {

                    button.addEventListener(
                        "click",
                        function () {

                            var format =
                                button.getAttribute(
                                    "data-table-export"
                                );

                            ctx.exportData(
                                format
                            );
                        }
                    );
                }
            );
        }
    };

})(window);