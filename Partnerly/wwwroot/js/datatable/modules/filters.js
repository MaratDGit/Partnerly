(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.filters = {

        setup: function (ctx) {

            ctx.updateFilterButton =
                function () {

                    if (!ctx.filterButton) {
                        return;
                    }


                    var filters =
                        ctx.state.filters || {};


                    var count =
                        Object.keys(
                            filters
                        ).length;


                    if (ctx.filterCount) {

                        ctx.filterCount.textContent =
                            String(count);

                        ctx.filterCount.hidden =
                            count === 0;
                    }


                    if (
                        count > 0
                    ) {

                        ctx.filterButton.classList.add(
                            "has-filters"
                        );
                    }
                    else {

                        ctx.filterButton.classList.remove(
                            "has-filters"
                        );
                    }


                    ctx.updateFilterColumnIndicators();
                };


            ctx.hasActiveFilter =
                function (field) {

                    if (
                        !ctx.state.filters ||
                        !Object.prototype.hasOwnProperty.call(
                            ctx.state.filters,
                            field
                        )
                    ) {

                        return false;
                    }


                    var value =
                        ctx.state.filters[
                        field
                        ];


                    if (
                        Array.isArray(
                            value
                        )
                    ) {

                        return value.some(
                            function (item) {

                                return (
                                    item !== null &&
                                    item !== undefined &&
                                    String(
                                        item
                                    ).trim() !== ""
                                );
                            }
                        );
                    }


                    return (
                        value !== null &&
                        value !== undefined &&
                        String(
                            value
                        ).trim() !== ""
                    );
                };


            ctx.updateFilterColumnIndicators =
                function () {

                    var headers =
                        ctx.root.querySelectorAll(
                            "thead th"
                        );


                    if (
                        !headers ||
                        headers.length === 0
                    ) {
                        return;
                    }


                    headers.forEach(
                        function (header) {

                            var oldIcon =
                                header.querySelector(
                                    ".data-table-column-filter-icon"
                                );


                            if (oldIcon) {
                                oldIcon.remove();
                            }
                        }
                    );


                    if (
                        !ctx.state.filters ||
                        Object.keys(
                            ctx.state.filters
                        ).length === 0
                    ) {
                        return;
                    }


                    var visibleColumns =
                        ctx.options.columns.filter(
                            function (column) {

                                return column.visible;
                            }
                        );


                    visibleColumns.forEach(
                        function (
                            column,
                            index
                        ) {

                            if (
                                !ctx.hasActiveFilter(
                                    column.field
                                )
                            ) {
                                return;
                            }


                            var header =
                                headers[index];


                            if (!header) {
                                return;
                            }


                            var icon =
                                document.createElement(
                                    "i"
                                );


                            icon.className =
                                "bx bx-filter-alt data-table-column-filter-icon";


                            icon.setAttribute(
                                "title",
                                "Filter: " +
                                column.title
                            );


                            icon.setAttribute(
                                "aria-label",
                                "Filter active for " +
                                column.title
                            );


                            var sortButton =
                                header.querySelector(
                                    ".data-table-sort"
                                );


                            if (sortButton) {

                                sortButton.appendChild(
                                    icon
                                );
                            }
                            else {

                                header.appendChild(
                                    icon
                                );
                            }
                        }
                    );
                };


            ctx.clearFilterModal =
                function () {

                    if (
                        !ctx.filterModal
                    ) {
                        return;
                    }


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-filter-control"
                        )
                        .forEach(
                            function (control) {

                                control.value =
                                    "";
                            }
                        );


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-range-control"
                        )
                        .forEach(
                            function (control) {

                                control.value =
                                    "";
                            }
                        );


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-multiselect"
                        )
                        .forEach(
                            function (container) {

                                container
                                    .querySelectorAll(
                                        'input[type="checkbox"]'
                                    )
                                    .forEach(
                                        function (
                                            checkbox
                                        ) {

                                            checkbox.checked =
                                                false;
                                        }
                                    );


                                var text =
                                    container.querySelector(
                                        ".data-table-modal-multiselect-text"
                                    );


                                if (text) {

                                    text.textContent =
                                        "All";
                                }


                                container.classList.remove(
                                    "open"
                                );
                            }
                        );
                };


            ctx.updateMultiSelectText =
                function (container) {

                    var text =
                        container.querySelector(
                            ".data-table-modal-multiselect-text"
                        );


                    if (!text) {
                        return;
                    }


                    var labels =
                        [];


                    container
                        .querySelectorAll(
                            'input[type="checkbox"]'
                        )
                        .forEach(
                            function (checkbox) {

                                if (
                                    !checkbox.checked
                                ) {
                                    return;
                                }


                                var label =
                                    checkbox.closest(
                                        "label"
                                    );


                                if (!label) {
                                    return;
                                }


                                var span =
                                    label.querySelector(
                                        "span"
                                    );


                                if (span) {

                                    labels.push(
                                        span.textContent.trim()
                                    );
                                }
                            }
                        );


                    if (
                        labels.length === 0
                    ) {

                        text.textContent =
                            "All";
                    }
                    else if (
                        labels.length <= 2
                    ) {

                        text.textContent =
                            labels.join(
                                ", "
                            );
                    }
                    else {

                        text.textContent =
                            labels.length +
                            " selected";
                    }
                };


            ctx.syncFilterModalFromState =
                function () {

                    if (
                        !ctx.filterModal
                    ) {
                        return;
                    }


                    ctx.clearFilterModal();


                    var filters =
                        ctx.state.filters ||
                        {};


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-filter-control"
                        )
                        .forEach(
                            function (control) {

                                var field =
                                    control.getAttribute(
                                        "data-field"
                                    );


                                if (!field) {
                                    return;
                                }


                                var value =
                                    filters[field];


                                if (
                                    value === null ||
                                    value === undefined ||
                                    Array.isArray(value)
                                ) {
                                    return;
                                }


                                control.value =
                                    String(value);
                            }
                        );


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-filter"
                        )
                        .forEach(
                            function (
                                filterElement
                            ) {

                                var field =
                                    filterElement.getAttribute(
                                        "data-filter-field"
                                    );


                                if (!field) {
                                    return;
                                }


                                var value =
                                    filters[field];


                                var rangeControls =
                                    filterElement.querySelectorAll(
                                        ".data-table-modal-range-control"
                                    );


                                if (
                                    rangeControls.length >
                                    0 &&
                                    Array.isArray(
                                        value
                                    )
                                ) {

                                    if (
                                        rangeControls[0]
                                    ) {

                                        rangeControls[0].value =
                                            value[0] ||
                                            "";
                                    }


                                    if (
                                        rangeControls[1]
                                    ) {

                                        rangeControls[1].value =
                                            value[1] ||
                                            "";
                                    }
                                }


                                var multiSelect =
                                    filterElement.querySelector(
                                        ".data-table-modal-multiselect"
                                    );


                                if (
                                    multiSelect
                                ) {

                                    var values =
                                        Array.isArray(
                                            value
                                        )
                                            ? value.map(
                                                function (
                                                    item
                                                ) {

                                                    return String(
                                                        item
                                                    );
                                                }
                                            )
                                            : [];


                                    multiSelect
                                        .querySelectorAll(
                                            'input[type="checkbox"]'
                                        )
                                        .forEach(
                                            function (
                                                checkbox
                                            ) {

                                                checkbox.checked =
                                                    values.indexOf(
                                                        String(
                                                            checkbox.value
                                                        )
                                                    ) !==
                                                    -1;
                                            }
                                        );


                                    ctx.updateMultiSelectText(
                                        multiSelect
                                    );
                                }
                            }
                        );
                };


            ctx.readFiltersFromModal =
                function () {

                    var filters =
                        {};


                    if (
                        !ctx.filterModal
                    ) {
                        return filters;
                    }


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-filter-control"
                        )
                        .forEach(
                            function (control) {

                                var field =
                                    control.getAttribute(
                                        "data-field"
                                    );


                                if (!field) {
                                    return;
                                }


                                var value =
                                    control.value;


                                if (
                                    value !== null &&
                                    value !== undefined &&
                                    value !== ""
                                ) {

                                    filters[field] =
                                        value;
                                }
                            }
                        );


                    ctx.filterModal
                        .querySelectorAll(
                            ".data-table-modal-filter"
                        )
                        .forEach(
                            function (
                                filterElement
                            ) {

                                var field =
                                    filterElement.getAttribute(
                                        "data-filter-field"
                                    );


                                if (!field) {
                                    return;
                                }


                                var rangeControls =
                                    filterElement.querySelectorAll(
                                        ".data-table-modal-range-control"
                                    );


                                if (
                                    rangeControls.length >
                                    0
                                ) {

                                    var min =
                                        rangeControls[0]
                                            ? rangeControls[0].value
                                            : "";


                                    var max =
                                        rangeControls[1]
                                            ? rangeControls[1].value
                                            : "";


                                    if (
                                        min !== "" ||
                                        max !== ""
                                    ) {

                                        filters[field] =
                                            [
                                                min,
                                                max
                                            ];
                                    }


                                    return;
                                }


                                var multiSelect =
                                    filterElement.querySelector(
                                        ".data-table-modal-multiselect"
                                    );


                                if (
                                    multiSelect
                                ) {

                                    var values =
                                        [];


                                    multiSelect
                                        .querySelectorAll(
                                            'input[type="checkbox"]'
                                        )
                                        .forEach(
                                            function (
                                                checkbox
                                            ) {

                                                if (
                                                    checkbox.checked
                                                ) {

                                                    values.push(
                                                        checkbox.value
                                                    );
                                                }
                                            }
                                        );


                                    if (
                                        values.length >
                                        0
                                    ) {

                                        filters[field] =
                                            values;
                                    }
                                }
                            }
                        );


                    return filters;
                };
        },


        initialize: function (ctx) {

            if (
                !ctx.filterModal
            ) {

                ctx.updateFilterButton();

                return;
            }


            if (
                typeof bootstrap !==
                "undefined" &&
                bootstrap.Modal
            ) {

                ctx.filterModalInstance =
                    bootstrap.Modal.getOrCreateInstance(
                        ctx.filterModal
                    );
            }
            else {

                console.error(
                    "Bootstrap Modal is not available. Make sure bootstrap.bundle.min.js is loaded."
                );
            }


            if (
                ctx.filterButton
            ) {

                ctx.filterButton.addEventListener(
                    "click",
                    function () {

                        ctx.syncFilterModalFromState();


                        if (
                            ctx.filterModalInstance
                        ) {

                            ctx.filterModalInstance.show();
                        }
                    }
                );
            }


            if (
                ctx.filterApplyButton
            ) {

                ctx.filterApplyButton.addEventListener(
                    "click",
                    function () {

                        ctx.state.filters =
                            ctx.readFiltersFromModal();


                        ctx.state.page =
                            1;


                        ctx.updateFilterButton();


                        if (
                            ctx.filterModalInstance
                        ) {

                            ctx.filterModalInstance.hide();
                        }


                        ctx.load();
                    }
                );
            }


            if (
                ctx.filterResetButton
            ) {

                ctx.filterResetButton.addEventListener(
                    "click",
                    function () {

                        ctx.state.filters =
                            {};


                        ctx.state.page =
                            1;


                        ctx.clearFilterModal();


                        ctx.updateFilterButton();


                        if (
                            ctx.filterModalInstance
                        ) {

                            ctx.filterModalInstance.hide();
                        }


                        ctx.load();
                    }
                );
            }


            var multiSelects =
                ctx.filterModal.querySelectorAll(
                    ".data-table-modal-multiselect"
                );


            multiSelects.forEach(
                function (container) {

                    var button =
                        container.querySelector(
                            ".data-table-modal-multiselect-button"
                        );


                    var checkboxes =
                        container.querySelectorAll(
                            'input[type="checkbox"]'
                        );


                    if (button) {

                        button.addEventListener(
                            "click",
                            function (event) {

                                event.preventDefault();

                                event.stopPropagation();


                                multiSelects.forEach(
                                    function (
                                        other
                                    ) {

                                        if (
                                            other !==
                                            container
                                        ) {

                                            other.classList.remove(
                                                "open"
                                            );
                                        }
                                    }
                                );


                                container.classList.toggle(
                                    "open"
                                );
                            }
                        );
                    }


                    checkboxes.forEach(
                        function (checkbox) {

                            checkbox.addEventListener(
                                "change",
                                function () {

                                    ctx.updateMultiSelectText(
                                        container
                                    );
                                }
                            );
                        }
                    );
                }
            );


            ctx.root.addEventListener(
                "click",
                function (event) {

                    if (
                        !event.target.closest(
                            ".data-table-modal-multiselect"
                        )
                    ) {

                        multiSelects.forEach(
                            function (
                                container
                            ) {

                                container.classList.remove(
                                    "open"
                                );
                            }
                        );
                    }
                }
            );


            ctx.filterModal.addEventListener(
                "show.bs.modal",
                function () {

                    ctx.syncFilterModalFromState();
                }
            );


            ctx.updateFilterButton();
        }
    };

})(window, document);