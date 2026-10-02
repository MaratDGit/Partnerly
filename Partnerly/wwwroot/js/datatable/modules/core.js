(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.core = {

        createContext: function (options) {

            if (!options) {
                return null;
            }

            var root =
                document.getElementById(
                    options.id
                );

            if (!root) {
                return null;
            }

            var body =
                root.querySelector(
                    "#" +
                    options.id +
                    "-body"
                );

            if (!body) {
                return null;
            }

            var filterModal =
                root.querySelector(
                    "#" +
                    options.id +
                    "-filter-modal"
                );

            return {

                window: window,

                document: document,

                options: options,

                root: root,

                body: body,

                info:
                    root.querySelector(
                        "#" +
                        options.id +
                        "-info"
                    ),

                pageSizeSelect:
                    root.querySelector(
                        "#" +
                        options.id +
                        "-page-size"
                    ),

                pagination:
                    root.querySelector(
                        "#" +
                        options.id +
                        "-pagination"
                    ),

                searchInput:
                    root.querySelector(
                        "#" +
                        options.id +
                        "-search"
                    ),

                filterModal:
                    filterModal,

                filterButton:
                    root.querySelector(
                        "[data-table-filter-button]"
                    ),

                filterCount:
                    root.querySelector(
                        "[data-table-filter-count]"
                    ),

                filterApplyButton:
                    filterModal
                        ? filterModal.querySelector(
                            "[data-table-filter-apply]"
                        )
                        : null,

                filterResetButton:
                    filterModal
                        ? filterModal.querySelector(
                            "[data-table-filter-reset]"
                        )
                        : null,

                filterModalInstance:
                    null,

                searchTimer:
                    null,

                state:
                    null
            };
        }
    };

})(window, document);