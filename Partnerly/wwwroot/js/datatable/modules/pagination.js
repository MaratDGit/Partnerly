(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.pagination = {

        setup: function (ctx) {

            ctx.renderPagination =
                function (result) {

                    if (!ctx.pagination) {
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

                        ctx.pagination.innerHTML = "";

                        return;
                    }


                    var totalPages =
                        Math.ceil(
                            result.total /
                            result.pageSize
                        );


                    /*
                     * =========================================================
                     * ONLY ONE PAGE
                     * =========================================================
                     *
                     * Даже если только одна страница,
                     * показываем pagination с текущей страницей.
                     */

                    if (totalPages < 1) {
                        totalPages = 1;
                    }


                    var currentPage =
                        result.page;


                    /*
                     * =========================================================
                     * PAGINATION HTML
                     * =========================================================
                     */

                    var html = "";


                    /*
                     * =========================================================
                     * FIRST PAGE
                     * =========================================================
                     */

                    html +=
                        '<button type="button" ' +
                        'class="data-table-page-button" ' +
                        'data-page="1" ' +
                        'title="First page" ' +
                        (
                            currentPage <= 1
                                ? " disabled"
                                : ""
                        ) +
                        '>' +
                        '<i class="bx bx-chevrons-left"></i>' +
                        '</button>';


                    /*
                     * =========================================================
                     * PREVIOUS PAGE
                     * =========================================================
                     */

                    html +=
                        '<button type="button" ' +
                        'class="data-table-page-button" ' +
                        'data-page="' +
                        (
                            currentPage - 1
                        ) +
                        '" ' +
                        'title="Previous page" ' +
                        (
                            currentPage <= 1
                                ? " disabled"
                                : ""
                        ) +
                        '>' +
                        '<i class="bx bx-chevron-left"></i>' +
                        '</button>';


                    /*
                     * =========================================================
                     * CURRENT PAGE
                     * =========================================================
                     */

                    html +=
                        '<input type="number" ' +
                        'class="data-table-page-input" ' +
                        'value="' +
                        currentPage +
                        '" ' +
                        'min="1" ' +
                        'max="' +
                        totalPages +
                        '" ' +
                        'data-page-input ' +
                        'aria-label="Current page" ' +
                        'title="Page ' +
                        currentPage +
                        ' of ' +
                        totalPages +
                        '" />';


                    /*
                     * =========================================================
                     * NEXT PAGE
                     * =========================================================
                     */

                    html +=
                        '<button type="button" ' +
                        'class="data-table-page-button" ' +
                        'data-page="' +
                        (
                            currentPage + 1
                        ) +
                        '" ' +
                        'title="Next page" ' +
                        (
                            currentPage >= totalPages
                                ? " disabled"
                                : ""
                        ) +
                        '>' +
                        '<i class="bx bx-chevron-right"></i>' +
                        '</button>';


                    /*
                     * =========================================================
                     * LAST PAGE
                     * =========================================================
                     */

                    html +=
                        '<button type="button" ' +
                        'class="data-table-page-button" ' +
                        'data-page="' +
                        totalPages +
                        '" ' +
                        'title="Last page" ' +
                        (
                            currentPage >= totalPages
                                ? " disabled"
                                : ""
                        ) +
                        '>' +
                        '<i class="bx bx-chevrons-right"></i>' +
                        '</button>';


                    ctx.pagination.innerHTML =
                        html;


                    /*
                     * =========================================================
                     * PAGE BUTTONS
                     * =========================================================
                     */

                    ctx.pagination
                        .querySelectorAll(
                            "[data-page]"
                        )
                        .forEach(
                            function (button) {

                                button.addEventListener(
                                    "click",
                                    function () {

                                        if (
                                            button.disabled
                                        ) {
                                            return;
                                        }


                                        var page =
                                            parseInt(
                                                button.getAttribute(
                                                    "data-page"
                                                ),
                                                10
                                            );


                                        if (
                                            !page ||
                                            page < 1 ||
                                            page > totalPages ||
                                            page === ctx.state.page
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


                    /*
                     * =========================================================
                     * PAGE INPUT
                     * =========================================================
                     */

                    var pageInput =
                        ctx.pagination.querySelector(
                            "[data-page-input]"
                        );


                    if (!pageInput) {
                        return;
                    }


                    /*
                     * ---------------------------------------------------------
                     * GO TO PAGE
                     * ---------------------------------------------------------
                     */

                    function goToPage() {

                        var value =
                            parseInt(
                                pageInput.value,
                                10
                            );


                        /*
                         * Если введено не число —
                         * возвращаем текущую страницу.
                         */

                        if (isNaN(value)) {

                            pageInput.value =
                                currentPage;

                            return;
                        }


                        /*
                         * Ограничиваем диапазон:
                         *
                         * 0      -> 1
                         * -5     -> 1
                         * 999999 -> totalPages
                         */

                        value =
                            Math.max(
                                1,
                                Math.min(
                                    value,
                                    totalPages
                                )
                            );


                        pageInput.value =
                            value;


                        /*
                         * Ничего не делаем,
                         * если это уже текущая страница.
                         */

                        if (
                            value ===
                            ctx.state.page
                        ) {
                            return;
                        }


                        ctx.state.page =
                            value;


                        ctx.load();
                    }


                    /*
                     * ---------------------------------------------------------
                     * ENTER
                     * ---------------------------------------------------------
                     */

                    pageInput.addEventListener(
                        "keydown",
                        function (event) {

                            if (
                                event.key === "Enter"
                            ) {

                                event.preventDefault();

                                goToPage();
                            }
                        }
                    );


                    /*
                     * ---------------------------------------------------------
                     * BLUR
                     * ---------------------------------------------------------
                     */

                    pageInput.addEventListener(
                        "blur",
                        function () {

                            goToPage();
                        }
                    );


                    /*
                     * ---------------------------------------------------------
                     * SELECT ALL ON FOCUS
                     * ---------------------------------------------------------
                     */

                    pageInput.addEventListener(
                        "focus",
                        function () {

                            pageInput.select();
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

            if (!ctx.pageSizeSelect) {
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