(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.data = {

        setup: function (ctx) {

            ctx.load =
                function () {

                    var options =
                        ctx.options;

                    var state =
                        ctx.state;

                    var url =
                        new URL(
                            options.ajaxUrl,
                            window.location.origin
                        );

                    /*
                     * =========================================================
                     * TABLE ID
                     * =========================================================
                     */

                    url.searchParams.set(
                        "TableID",
                        options.id
                    );

                    /*
                     * =========================================================
                     * PAGING
                     * =========================================================
                     */

                    url.searchParams.set(
                        "page",
                        state.page
                    );

                    url.searchParams.set(
                        "pageSize",
                        state.pageSize
                    );


                    /*
                     * =========================================================
                     * SEARCH
                     * =========================================================
                     */

                    if (
                        state.search
                    ) {

                        url.searchParams.set(
                            "search",
                            state.search
                        );
                    }


                    /*
                     * =========================================================
                     * SORTING
                     * =========================================================
                     */

                    if (
                        state.sortField
                    ) {

                        url.searchParams.set(
                            "sortField",
                            state.sortField
                        );

                        url.searchParams.set(
                            "sortDirection",
                            state.sortDirection
                        );
                    }


                    /*
                     * =========================================================
                     * FILTERS
                     * =========================================================
                     */

                    Object.keys(
                        state.filters
                    ).forEach(
                        function (field) {

                            var value =
                                state.filters[
                                field
                                ];


                            if (
                                Array.isArray(
                                    value
                                )
                            ) {

                                value.forEach(
                                    function (item) {

                                        url.searchParams.append(
                                            "Filters[" +
                                            field +
                                            "]",
                                            item
                                        );
                                    }
                                );
                            }
                            else if (
                                value !== null &&
                                value !== undefined &&
                                value !== ""
                            ) {

                                url.searchParams.append(
                                    "Filters[" +
                                    field +
                                    "]",
                                    value
                                );
                            }
                        }
                    );


                    /*
                     * =========================================================
                     * SELECTED ROWS
                     * =========================================================
                     */

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


                    /*
                     * =========================================================
                     * LOADING
                     * =========================================================
                     */

                    const loading = lookupLocalization?.loading ?? "Loading...";
                    const loadingError = lookupLocalization?.loadError ?? "Error loading data";

                    ctx.body.innerHTML =
                        `<tr><td colspan="' +
                        ${options.columnCount} +
                        '">${loading}</td></tr>`;

                    /*
                     * =========================================================
                     * REQUEST
                     * =========================================================
                     */

                    fetch(
                        url.toString()
                    )
                        .then(
                            function (response) {

                                if (
                                    !response.ok
                                ) {

                                    throw new Error(
                                        "Request failed: " +
                                        response.status
                                    );
                                }

                                return response.json();
                            }
                        )
                        .then(
                            function (result) {

                                ctx.renderRows(
                                    result.items
                                );

                                ctx.renderInfo(
                                    result
                                );

                                ctx.renderPagination(
                                    result
                                );
                            }
                        )
                        .catch(
                            function (error) {

                                console.error(
                                    error
                                );

                                ctx.body.innerHTML =
                                    `<tr><td colspan="' +
                                    ${options.columnCount} +
                                    '">${loadingError}</td></tr>`;
                            }
                        );
                };
        }
    };

})(window);