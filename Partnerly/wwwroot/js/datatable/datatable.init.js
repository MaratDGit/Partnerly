(function (window, document) {

    "use strict";

    function initializeDataTables() {

        var elements =
            document.querySelectorAll(
                '[data-datatable="true"]'
            );

        elements.forEach(function (element) {

            var id =
                element.getAttribute("id");

            if (!id) {
                return;
            }

            var configElement =
                element.querySelector(
                    "#" + id + "-config"
                );

            if (!configElement) {

                console.error(
                    "DataTable configuration not found for: " +
                    id
                );

                return;
            }

            var config;

            try {

                config =
                    JSON.parse(
                        configElement.textContent
                    );

            }
            catch (error) {

                console.error(
                    "Unable to parse DataTable configuration for: " +
                    id,
                    error
                );

                return;
            }

            if (!config) {
                return;
            }

            if (typeof window.DataTable !== "function") {

                console.error(
                    "DataTable is not loaded."
                );

                return;
            }

            window.DataTable(config);
        });
    }


    /*
     * DOM READY
     */

    if (
        document.readyState === "loading"
    ) {

        document.addEventListener(
            "DOMContentLoaded",
            initializeDataTables
        );

    }
    else {

        initializeDataTables();
    }

})(window, document);