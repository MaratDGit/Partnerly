(function (window) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.state = {

        setup: function (ctx) {

            var options =
                ctx.options;

            ctx.state = {

                page: 1,

                pageSize:
                    options.paging &&
                        options.paging.pageSize
                        ? options.paging.pageSize
                        : 10,

                search: "",

                sortField:
                    options.sorting &&
                        options.sorting.defaultField
                        ? options.sorting.defaultField
                        : "",

                sortDirection:
                    options.sorting &&
                        options.sorting.defaultDirection
                        ? options.sorting.defaultDirection
                        : "asc",

                filters: {},

                selectedRows: []
            };
        },

        reset: function (ctx) {

            var options =
                ctx.options;

            ctx.state.page = 1;

            ctx.state.search = "";

            ctx.state.filters = {};

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
    };

})(window);