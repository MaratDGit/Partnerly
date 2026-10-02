(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.sorting = {

        setup: function (ctx) {

            ctx.sortButtons =
                ctx.root.querySelectorAll(
                    ".data-table-sort"
                );


            ctx.updateSortIcons =
                function () {

                    ctx.sortButtons.forEach(
                        function (button) {

                            var field =
                                button.getAttribute(
                                    "data-field"
                                );


                            var icon =
                                button.querySelector(
                                    ".data-table-sort-icon"
                                );


                            if (!icon) {
                                return;
                            }


                            if (
                                field !==
                                ctx.state.sortField
                            ) {

                                icon.textContent =
                                    "";

                                return;
                            }


                            if (
                                ctx.state.sortDirection ===
                                "asc"
                            ) {

                                icon.innerHTML =
                                    "&#9650;";
                            }
                            else {

                                icon.innerHTML =
                                    "&#9660;";
                            }
                        }
                    );
                };
        },


        initialize: function (ctx) {

            ctx.sortButtons.forEach(
                function (button) {

                    button.addEventListener(
                        "click",
                        function () {

                            var field =
                                button.getAttribute(
                                    "data-field"
                                );


                            if (!field) {
                                return;
                            }


                            if (
                                ctx.state.sortField ===
                                field
                            ) {

                                ctx.state.sortDirection =
                                    ctx.state.sortDirection ===
                                        "asc"
                                        ? "desc"
                                        : "asc";
                            }
                            else {

                                ctx.state.sortField =
                                    field;

                                ctx.state.sortDirection =
                                    "asc";
                            }


                            ctx.state.page =
                                1;


                            ctx.updateSortIcons();


                            ctx.load();
                        }
                    );
                }
            );


            ctx.updateSortIcons();
        }
    };

})(window);