(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.pagination = {

        setup: function (ctx) {

            ctx.renderPagination =
                function (result) {

                    if (
                        !ctx.pagination
                    ) {
                        return;
                    }


                    /*
                     * =========================================================
                     * PAGING ENABLED
                     * =========================================================
                     */

                    if (
                        !ctx.options.paging ||
                        !ctx.options.paging.enabled
                    ) {

                        ctx.pagination.innerHTML =
                            "";

                        return;
                    }


                    var totalPages =
                        Math.ceil(
                            result.total /
                            result.pageSize
                        );


                    if (
                        totalPages <= 1
                    ) {

                        ctx.pagination.innerHTML =
                            "";

                        return;
                    }


                    var currentPage =
                        result.page;


                    var html =
                        "";


                    html +=
                        '<button type="button" class="btn btn-sm btn-outline-secondary" ' +
                        'data-page="1" ' +
                        (
                            currentPage === 1
                                ? "disabled"
                                : ""
                        ) +
                        '>&laquo;</button>';


                    html +=
                        '<button type="button" class="btn btn-sm btn-outline-secondary" ' +
                        'data-page="' +
                        (
                            currentPage - 1
                        ) +
                        '" ' +
                        (
                            currentPage === 1
                                ? "disabled"
                                : ""
                        ) +
                        '>&lsaquo;</button>';


                    var startPage =
                        Math.max(
                            1,
                            currentPage - 1
                        );


                    var endPage =
                        Math.min(
                            totalPages,
                            startPage + 2
                        );


                    if (
                        endPage -
                        startPage < 2
                    ) {

                        startPage =
                            Math.max(
                                1,
                                endPage - 2
                            );
                    }


                    for (
                        var page =
                            startPage;

                        page <=
                        endPage;

                        page++
                    ) {

                        html +=
                            '<button type="button" class="btn btn-sm ' +
                            (
                                page ===
                                    currentPage
                                    ? "btn-primary"
                                    : "btn-outline-secondary"
                            ) +
                            '" data-page="' +
                            page +
                            '">' +
                            page +
                            '</button>';
                    }


                    html +=
                        '<button type="button" class="btn btn-sm btn-outline-secondary" ' +
                        'data-page="' +
                        (
                            currentPage + 1
                        ) +
                        '" ' +
                        (
                            currentPage ===
                                totalPages
                                ? "disabled"
                                : ""
                        ) +
                        '>&rsaquo;</button>';


                    html +=
                        '<button type="button" class="btn btn-sm btn-outline-secondary" ' +
                        'data-page="' +
                        totalPages +
                        '" ' +
                        (
                            currentPage ===
                                totalPages
                                ? "disabled"
                                : ""
                        ) +
                        '>&raquo;</button>';


                    ctx.pagination.innerHTML =
                        html;


                    ctx.pagination
                        .querySelectorAll(
                            "[data-page]"
                        )
                        .forEach(
                            function (button) {

                                button.addEventListener(
                                    "click",
                                    function () {

                                        var page =
                                            parseInt(
                                                button.getAttribute(
                                                    "data-page"
                                                ),
                                                10
                                            );


                                        if (
                                            !page ||
                                            page ===
                                            ctx.state.page
                                        ) {
                                            return;
                                        }


                                        ctx.state.page =
                                            page;


                                        ctx.load();
                                    }
                                );
                            }
                        );
                };
        },


        initialize: function (ctx) {

            /*
             * =========================================================
             * PAGE SIZE
             * =========================================================
             */

            if (
                !ctx.pageSizeSelect
            ) {
                return;
            }


            if (
                !ctx.options.paging ||
                !ctx.options.paging.enabled
            ) {

                ctx.pageSizeSelect.style.display =
                    "none";

                return;
            }


            if (
                ctx.options.paging.showPageSize === false
            ) {

                ctx.pageSizeSelect.style.display =
                    "none";

                return;
            }


            ctx.pageSizeSelect.addEventListener(
                "change",
                function () {

                    ctx.state.pageSize =
                        parseInt(
                            ctx.pageSizeSelect.value,
                            10
                        );


                    ctx.state.page =
                        1;


                    ctx.load();
                }
            );
        }
    };

})(window);