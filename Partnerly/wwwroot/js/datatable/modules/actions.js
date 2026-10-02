(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.actions = {

        setup: function (ctx) {

            var options =
                ctx.options;


            ctx.resetTableToDefaultState =
                function () {

                    var options =
                        ctx.options;


                    if (
                        ctx.state
                    ) {

                        ctx.state.page =
                            1;


                        ctx.state.search =
                            "";


                        ctx.state.filters =
                            {};


                        ctx.state.sortField =
                            options.sorting &&
                                options.sorting.defaultField
                                ? options.sorting.defaultField
                                : "";


                        ctx.state.sortDirection =
                            options.sorting &&
                                options.sorting.defaultDirection
                                ? options.sorting.defaultDirection
                                : "asc";
                    }


                    if (
                        ctx.searchInput
                    ) {

                        ctx.searchInput.value =
                            "";
                    }


                    ctx.syncFilterModalFromState();


                    ctx.updateFilterButton();


                    ctx.updateSortIcons();


                    ctx.load();
                };
        },


        initialize: function (ctx) {

            var reloadButton =
                ctx.root.querySelector(
                    "[data-table-reload]"
                );


            if (
                reloadButton
            ) {

                reloadButton.addEventListener(
                    "click",
                    function () {

                        ctx.resetTableToDefaultState();
                    }
                );
            }
        }
    };

})(window);