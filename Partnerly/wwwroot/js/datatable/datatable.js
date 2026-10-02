//wwwroot /
//└── js /
//    └── datatable /
//        │
//        ├── datatable.js
//        ├── datatable.init.js
//        │
//        └── modules /
//            ├── core.js
//            ├── state.js
//            ├── utils.js
//            ├── columns.js
//            ├── render.js
//            ├── data.js
//            ├── search.js
//            ├── sorting.js
//            ├── filters.js
//            ├── pagination.js
//            ├── inline.js
//            └── actions.js

(function (window, document) {

    "use strict";

    function DataTable(options) {

        if (!options) {
            return;
        }

        var modules = window.DataTableModules;

        if (!modules || !modules.core) {

            console.error(
                "DataTable modules are not loaded."
            );

            return;
        }

        var ctx =
            modules.core.createContext(
                options
            );

        if (!ctx) {
            return;
        }

        var setupModules = [
            modules.state,
            modules.utils,
            modules.columns,
            modules.render,
            modules.data,
            modules.search,
            modules.sorting,
            modules.filters,
            modules.pagination,
            modules.inline,
            modules.actions
        ];

        for (
            var i = 0;
            i < setupModules.length;
            i++
        ) {

            if (
                setupModules[i] &&
                setupModules[i].setup
            ) {

                setupModules[i].setup(ctx);
            }
        }

        if (
            modules.search &&
            modules.search.initialize
        ) {

            modules.search.initialize(ctx);
        }

        if (
            modules.sorting &&
            modules.sorting.initialize
        ) {

            modules.sorting.initialize(ctx);
        }

        if (
            modules.filters &&
            modules.filters.initialize
        ) {

            modules.filters.initialize(ctx);
        }

        if (
            modules.pagination &&
            modules.pagination.initialize
        ) {

            modules.pagination.initialize(ctx);
        }

        if (
            modules.actions &&
            modules.actions.initialize
        ) {

            modules.actions.initialize(ctx);
        }

        if (options.autoLoad !== false) {

            ctx.load();
        }
    }

    window.DataTable =
        DataTable;

})(window, document);