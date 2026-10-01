(function (window, document) {

    "use strict";


    function DataTable(options) {

        if (!options) {
            return;
        }


        /*
         * ROOT
         *
         * This is the individual DataTable instance.
         *
         * IMPORTANT:
         * We don't change any existing CSS classes or IDs.
         */

        var root =
            document.getElementById(
                options.id
            );

        if (!root) {
            return;
        }


        /*
         * =========================================================
         * ELEMENTS
         * =========================================================
         */

        var body =
            root.querySelector(
                "#" + options.id + "-body"
            );

        var info =
            root.querySelector(
                "#" + options.id + "-info"
            );

        var pageSizeSelect =
            root.querySelector(
                "#" + options.id + "-page-size"
            );

        var pagination =
            root.querySelector(
                "#" + options.id + "-pagination"
            );

        var searchInput =
            root.querySelector(
                "#" + options.id + "-search"
            );


        if (!body) {
            return;
        }


        var searchTimer = null;


        /*
         * =========================================================
         * STATE
         * =========================================================
         */

        var state = {

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

            filters: {}
        };


        /*
         * =========================================================
         * FILTER MODAL
         * =========================================================
         */

        var filterModal =
            root.querySelector(
                "#" + options.id + "-filter-modal"
            );

        var filterButton =
            root.querySelector(
                "[data-table-filter-button]"
            );

        var filterCount =
            root.querySelector(
                "[data-table-filter-count]"
            );

        var filterApplyButton =
            filterModal
                ? filterModal.querySelector(
                    "[data-table-filter-apply]"
                )
                : null;

        var filterResetButton =
            filterModal
                ? filterModal.querySelector(
                    "[data-table-filter-reset]"
                )
                : null;

        var filterModalInstance = null;


        /*
         * =========================================================
         * LOAD
         * =========================================================
         */

        function load() {

            var url =
                new URL(
                    options.ajaxUrl,
                    window.location.origin
                );


            /*
             * Page
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
             * Search
             */

            if (state.search) {

                url.searchParams.set(
                    "search",
                    state.search
                );
            }


            /*
             * Sorting
             */

            if (state.sortField) {

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
             * Filters
             */

            Object.keys(
                state.filters
            ).forEach(function (field) {

                var value =
                    state.filters[field];

                if (Array.isArray(value)) {

                    value.forEach(
                        function (item) {

                            url.searchParams.append(
                                "Filters[" + field + "]",
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
                        "Filters[" + field + "]",
                        value
                    );
                }

            });


            /*
             * Loading
             */

            body.innerHTML =
                '<tr><td colspan="' +
                options.columnCount +
                '">Loading...</td></tr>';


            fetch(
                url.toString()
            )
                .then(function (response) {

                    if (!response.ok) {

                        throw new Error(
                            "Request failed: " +
                            response.status
                        );
                    }

                    return response.json();
                })
                .then(function (result) {

                    renderRows(
                        result.items
                    );

                    renderInfo(
                        result
                    );

                    renderPagination(
                        result
                    );
                })
                .catch(function (error) {

                    console.error(error);

                    body.innerHTML =
                        '<tr><td colspan="' +
                        options.columnCount +
                        '">Error loading data</td></tr>';
                });
        }


        /*
         * =========================================================
         * ROWS
         * =========================================================
         */

        function renderRows(items) {

            if (
                !items ||
                items.length === 0
            ) {

                body.innerHTML =
                    '<tr><td colspan="' +
                    options.columnCount +
                    '">No data</td></tr>';

                return;
            }

            var html = "";


            items.forEach(function (item) {

                var id =
                    getValue(
                        item,
                        options.keyField || "Id"
                    );

                html +=
                    '<tr data-row-id="' +
                    escapeHtml(id) +
                    '">';


                /*
                 * COLUMNS
                 */

                options.columns.forEach(
                    function (column) {

                        if (!column.visible) {
                            return;
                        }

                        var value =
                            getValue(
                                item,
                                column.field
                            );

                        var formatted =
                            formatCellValue(
                                value,
                                column,
                                item,
                                id
                            );

                        html +=
                            '<td data-field="' +
                            escapeHtml(
                                column.field
                            ) +
                            '" ' +
                            'data-original-value="' +
                            escapeHtml(
                                formatted.rawValue
                            ) +
                            '">' +
                            formatted.html +
                            "</td>";
                    }
                );


                /*
                 * ACTIONS
                 */

                if (hasActionsColumn()) {

                    html +=
                        '<td class="data-table-actions">';


                    /*
                     * INLINE EDIT
                     */

                    if (
                        options.inlineEditing &&
                        hasEditableColumns()
                    ) {

                        html +=
                            '<button type="button" ' +
                            'class="btn btn-sm btn-outline-primary ' +
                            'data-table-inline-edit" ' +
                            'data-id="' +
                            escapeHtml(id) +
                            '" ' +
                            'title="Edit row" ' +
                            'aria-label="Edit row">' +
                            '<i class="bx bx-edit-alt"></i>' +
                            '</button>';
                    }


                    /*
                     * VIEW
                     */

                    if (
                        options.actions &&
                        options.actions.enabled &&
                        options.actions.view &&
                        options.actions.viewUrl
                    ) {

                        html +=
                            '<a href="' +
                            escapeHtml(
                                options.actions.viewUrl +
                                "/" +
                                encodeURIComponent(id)
                            ) +
                            '" ' +
                            'class="btn btn-sm btn-outline-secondary" ' +
                            'title="View" ' +
                            'aria-label="View">' +
                            '<i class="bx bx-show"></i>' +
                            '</a>';
                    }


                    /*
                     * EDIT PAGE
                     */

                    if (
                        options.actions &&
                        options.actions.enabled &&
                        options.actions.edit &&
                        options.actions.editUrl
                    ) {

                        html +=
                            '<a href="' +
                            escapeHtml(
                                options.actions.editUrl +
                                "/" +
                                encodeURIComponent(id)
                            ) +
                            '" ' +
                            'class="btn btn-sm btn-outline-primary" ' +
                            'title="Edit" ' +
                            'aria-label="Edit">' +
                            '<i class="bx bx-edit"></i>' +
                            '</a>';
                    }


                    /*
                     * DELETE
                     */

                    if (
                        options.actions &&
                        options.actions.enabled &&
                        options.actions.delete &&
                        options.actions.deleteUrl
                    ) {

                        html +=
                            '<a href="' +
                            escapeHtml(
                                options.actions.deleteUrl +
                                "/" +
                                encodeURIComponent(id)
                            ) +
                            '" ' +
                            'class="btn btn-sm btn-outline-danger" ' +
                            'title="Delete" ' +
                            'aria-label="Delete">' +
                            '<i class="bx bx-trash"></i>' +
                            '</a>';
                    }


                    html += "</td>";
                }

                html += "</tr>";
            });


            body.innerHTML = html;


            bindInlineEditButtons();

            highlightSearchText();
        }


        /*
         * =========================================================
         * FORMAT CELL VALUE
         * =========================================================
         */

        function formatCellValue(
            value,
            column,
            row,
            rowId
        ) {

            /*
             * EMPTY
             */

            if (
                value === null ||
                value === undefined ||
                value === ""
            ) {

                return {

                    html:
                        escapeHtml(
                            column.emptyText
                        ),

                    rawValue: ""
                };
            }


            /*
             * BOOLEAN
             */

            if (
                column.type === "Boolean"
            ) {

                var checked =
                    value === true ||
                    value === "true" ||
                    value === "True" ||
                    value === 1 ||
                    value === "1";

                return {

                    html:
                        '<input type="checkbox" ' +
                        'class="form-check-input data-table-boolean" ' +
                        (
                            checked
                                ? "checked "
                                : ""
                        ) +
                        'disabled>',

                    rawValue:
                        checked
                            ? "true"
                            : "false"
                };
            }


            /*
             * NUMBER
             */

            if (
                column.type === "Number"
            ) {

                var numberValue =
                    Number(value);

                if (!isNaN(numberValue)) {

                    return {

                        html:
                            escapeHtml(
                                numberValue.toLocaleString()
                            ),

                        rawValue:
                            String(value)
                    };
                }

                return {

                    html:
                        escapeHtml(value),

                    rawValue:
                        String(value)
                };
            }


            /*
             * CURRENCY
             */

            if (
                column.type === "Currency"
            ) {

                var currencyValue =
                    Number(value);

                if (!isNaN(currencyValue)) {

                    return {

                        html:
                            escapeHtml(
                                currencyValue.toLocaleString(
                                    undefined,
                                    {
                                        minimumFractionDigits: 2,
                                        maximumFractionDigits: 2
                                    }
                                )
                            ),

                        rawValue:
                            String(value)
                    };
                }

                return {

                    html:
                        escapeHtml(value),

                    rawValue:
                        String(value)
                };
            }


            /*
             * DATE
             */

            if (
                column.type === "Date"
            ) {

                return {

                    html:
                        escapeHtml(
                            formatDate(
                                value,
                                column.format
                            )
                        ),

                    rawValue:
                        String(value)
                };
            }


            /*
             * DATETIME
             */

            if (
                column.type === "DateTime"
            ) {

                return {

                    html:
                        escapeHtml(
                            formatDateTime(
                                value,
                                column.format
                            )
                        ),

                    rawValue:
                        String(value)
                };
            }


            /*
             * EMAIL
             */

            if (
                column.type === "Email"
            ) {

                var email =
                    String(value);

                return {

                    html:
                        '<a href="mailto:' +
                        escapeHtml(email) +
                        '">' +
                        escapeHtml(email) +
                        "</a>",

                    rawValue:
                        email
                };
            }


            /*
             * LINK
             */

            if (
                column.type === "Link"
            ) {

                var linkValue =
                    value;

                if (
                    linkValue === null ||
                    linkValue === undefined ||
                    String(linkValue).trim() === ""
                ) {

                    return {

                        html:
                            escapeHtml(
                                column.emptyText || "—"
                            ),

                        rawValue: ""
                    };
                }

                var href =
                    buildColumnUrl(
                        column.urlTemplate,
                        row,
                        rowId
                    );

                if (!href) {

                    return {

                        html:
                            escapeHtml(
                                String(linkValue)
                            ),

                        rawValue:
                            String(linkValue)
                    };
                }

                return {

                    html:
                        '<a href="' +
                        escapeHtml(href) +
                        '" class="data-table-link">' +
                        escapeHtml(
                            String(linkValue)
                        ) +
                        '</a>',

                    rawValue:
                        String(linkValue)
                };
            }


            /*
             * BADGE
             */

            if (
                column.type === "Badge"
            ) {

                return {

                    html:
                        '<span class="data-table-badge">' +
                        escapeHtml(value) +
                        "</span>",

                    rawValue:
                        String(value)
                };
            }


            /*
             * TEXT
             */

            return {

                html:
                    escapeHtml(value),

                rawValue:
                    String(value)
            };
        }


        /*
         * =========================================================
         * COLUMN URL
         * =========================================================
         */

        function buildColumnUrl(
            template,
            row,
            rowId
        ) {

            if (!template) {
                return "";
            }

            return String(template).replace(
                /\{([^}]+)\}/g,
                function (
                    match,
                    fieldName
                ) {

                    if (
                        fieldName.toLowerCase() === "id" &&
                        rowId !== null &&
                        rowId !== undefined
                    ) {

                        return encodeURIComponent(
                            String(rowId)
                        );
                    }

                    var value =
                        getRowValue(
                            row,
                            fieldName
                        );

                    return value === null ||
                        value === undefined
                        ? ""
                        : encodeURIComponent(
                            String(value)
                        );
                }
            );
        }


        /*
         * =========================================================
         * GET ROW VALUE
         * =========================================================
         */

        function getRowValue(
            row,
            field
        ) {

            if (
                !row ||
                !field
            ) {
                return null;
            }

            var parts =
                field.split(".");

            var value =
                row;

            for (
                var i = 0;
                i < parts.length;
                i++
            ) {

                if (
                    value === null ||
                    value === undefined
                ) {
                    return null;
                }

                value =
                    value[parts[i]];
            }

            return value;
        }


        /*
         * =========================================================
         * FORMAT DATE
         * =========================================================
         */

        function formatDate(
            value,
            format
        ) {

            var date =
                parseDateValue(value);

            if (!date) {
                return value;
            }

            if (!format) {

                return date.toLocaleDateString();
            }

            return formatDatePattern(
                date,
                format
            );
        }


        /*
         * =========================================================
         * FORMAT DATETIME
         * =========================================================
         */

        function formatDateTime(
            value,
            format
        ) {

            var date =
                parseDateValue(value);

            if (!date) {
                return value;
            }


            /*
             * Relative
             */

            if (
                format &&
                format.toLowerCase() === "relative"
            ) {

                return formatRelativeDate(
                    date
                );
            }


            /*
             * No explicit format
             */

            if (!format) {

                return date.toLocaleString();
            }


            /*
             * Explicit format
             */

            return formatDatePattern(
                date,
                format
            );
        }


        /*
         * =========================================================
         * PARSE DATE
         * =========================================================
         */

        function parseDateValue(value) {

            if (
                value === null ||
                value === undefined ||
                value === ""
            ) {
                return null;
            }

            if (
                value instanceof Date
            ) {

                if (
                    isNaN(
                        value.getTime()
                    )
                ) {
                    return null;
                }

                return value;
            }

            var stringValue =
                String(value).trim();


            /*
             * Remove { }
             */

            if (
                stringValue.charAt(0) === "{" &&
                stringValue.charAt(
                    stringValue.length - 1
                ) === "}"
            ) {

                stringValue =
                    stringValue.substring(
                        1,
                        stringValue.length - 1
                    ).trim();
            }


            /*
             * .NET DateTime
             */

            var dotNetMatch =
                stringValue.match(
                    /^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2}):(\d{2})\s*(AM|PM)$/i
                );

            if (dotNetMatch) {

                var month =
                    Number(dotNetMatch[1]);

                var day =
                    Number(dotNetMatch[2]);

                var year =
                    Number(dotNetMatch[3]);

                var hours =
                    Number(dotNetMatch[4]);

                var minutes =
                    Number(dotNetMatch[5]);

                var seconds =
                    Number(dotNetMatch[6]);

                var amPm =
                    dotNetMatch[7].toUpperCase();


                if (
                    amPm === "PM" &&
                    hours < 12
                ) {
                    hours += 12;
                }

                if (
                    amPm === "AM" &&
                    hours === 12
                ) {
                    hours = 0;
                }


                return new Date(
                    Date.UTC(
                        year,
                        month - 1,
                        day,
                        hours,
                        minutes,
                        seconds
                    )
                );
            }


            /*
             * ISO without timezone
             */

            var isoLocalMatch =
                stringValue.match(
                    /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?$/
                );

            if (isoLocalMatch) {

                var milliseconds =
                    isoLocalMatch[7]
                        ? Number(
                            ("0." +
                                isoLocalMatch[7]) *
                            1000
                        )
                        : 0;

                return new Date(
                    Date.UTC(
                        Number(isoLocalMatch[1]),
                        Number(isoLocalMatch[2]) - 1,
                        Number(isoLocalMatch[3]),
                        Number(isoLocalMatch[4]),
                        Number(isoLocalMatch[5]),
                        Number(isoLocalMatch[6]),
                        milliseconds
                    )
                );
            }


            /*
             * ISO with timezone
             */

            var date =
                new Date(
                    stringValue
                );

            if (
                isNaN(
                    date.getTime()
                )
            ) {
                return null;
            }

            return date;
        }


        /*
         * =========================================================
         * RELATIVE DATE
         * =========================================================
         */

        function formatRelativeDate(
            date
        ) {

            var now =
                new Date();

            var diff =
                now.getTime() -
                date.getTime();

            var seconds =
                Math.floor(
                    diff / 1000
                );


            if (seconds < 0) {
                return "just now";
            }


            if (seconds < 60) {
                return "just now";
            }


            var minutes =
                Math.floor(
                    seconds / 60
                );

            if (minutes < 60) {

                return minutes +
                    (
                        minutes === 1
                            ? " minute ago"
                            : " minutes ago"
                    );
            }


            var hours =
                Math.floor(
                    minutes / 60
                );

            if (hours < 24) {

                return hours +
                    (
                        hours === 1
                            ? " hour ago"
                            : " hours ago"
                    );
            }


            var days =
                Math.floor(
                    hours / 24
                );

            if (days === 1) {
                return "yesterday";
            }


            if (days < 7) {

                return days +
                    " days ago";
            }


            return date.toLocaleDateString();
        }


        /*
         * =========================================================
         * DATE FORMAT PATTERN
         * =========================================================
         */

        function formatDatePattern(
            date,
            format
        ) {

            var result =
                String(format);

            var day =
                String(
                    date.getDate()
                ).padStart(
                    2,
                    "0"
                );

            var month =
                String(
                    date.getMonth() + 1
                ).padStart(
                    2,
                    "0"
                );

            var year =
                date.getFullYear();

            var hours =
                String(
                    date.getHours()
                ).padStart(
                    2,
                    "0"
                );

            var minutes =
                String(
                    date.getMinutes()
                ).padStart(
                    2,
                    "0"
                );

            var seconds =
                String(
                    date.getSeconds()
                ).padStart(
                    2,
                    "0"
                );


            result =
                result.replace(
                    /yyyy/g,
                    String(year)
                );

            result =
                result.replace(
                    /dd/g,
                    day
                );

            result =
                result.replace(
                    /MM/g,
                    month
                );

            result =
                result.replace(
                    /HH/g,
                    hours
                );

            result =
                result.replace(
                    /mm/g,
                    minutes
                );

            result =
                result.replace(
                    /ss/g,
                    seconds
                );

            return result;
        }


        /*
         * =========================================================
         * HAS ACTIONS COLUMN
         * =========================================================
         */

        function hasActionsColumn() {

            if (
                options.inlineEditing &&
                hasEditableColumns()
            ) {
                return true;
            }

            if (
                !options.actions ||
                !options.actions.enabled
            ) {
                return false;
            }

            return (
                (
                    options.actions.view &&
                    options.actions.viewUrl
                ) ||
                (
                    options.actions.edit &&
                    options.actions.editUrl
                ) ||
                (
                    options.actions.delete &&
                    options.actions.deleteUrl
                )
            );
        }


        /*
         * =========================================================
         * HAS EDITABLE COLUMNS
         * =========================================================
         */

        function hasEditableColumns() {

            if (!options.inlineEditing) {
                return false;
            }

            return options.columns.some(
                function (column) {

                    return column.visible &&
                        column.editable;
                }
            );
        }


        /*
         * =========================================================
         * INFO
         * =========================================================
         */

        function renderInfo(
            result
        ) {

            if (!info) {
                return;
            }

            if (
                result.total === 0
            ) {

                info.textContent =
                    "No records";

                return;
            }

            var start =
                (
                    (result.page - 1) *
                    result.pageSize
                ) + 1;

            var end =
                Math.min(
                    result.page *
                    result.pageSize,
                    result.total
                );

            info.textContent =
                "Showing " +
                start +
                "-" +
                end +
                " of " +
                result.total;
        }


        /*
         * =========================================================
         * PAGINATION
         * =========================================================
         */

        function renderPagination(
            result
        ) {

            if (!pagination) {
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

                pagination.innerHTML =
                    "";

                return;
            }

            var currentPage =
                result.page;

            var html = "";


            /*
             * First
             */

            html +=
                '<button type="button" ' +
                'class="btn btn-sm btn-outline-secondary" ' +
                'data-page="1" ' +
                (
                    currentPage === 1
                        ? "disabled"
                        : ""
                ) +
                '>&laquo;</button>';


            /*
             * Previous
             */

            html +=
                '<button type="button" ' +
                'class="btn btn-sm btn-outline-secondary" ' +
                'data-page="' +
                (currentPage - 1) +
                '" ' +
                (
                    currentPage === 1
                        ? "disabled"
                        : ""
                ) +
                '>&lsaquo;</button>';


            /*
             * Page range
             */

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
                endPage - startPage < 2
            ) {

                startPage =
                    Math.max(
                        1,
                        endPage - 2
                    );
            }


            /*
             * Pages
             */

            for (
                var page = startPage;
                page <= endPage;
                page++
            ) {

                html +=
                    '<button type="button" ' +
                    'class="btn btn-sm ' +
                    (
                        page === currentPage
                            ? "btn-primary"
                            : "btn-outline-secondary"
                    ) +
                    '" ' +
                    'data-page="' +
                    page +
                    '">' +
                    page +
                    '</button>';
            }


            /*
             * Next
             */

            html +=
                '<button type="button" ' +
                'class="btn btn-sm btn-outline-secondary" ' +
                'data-page="' +
                (currentPage + 1) +
                '" ' +
                (
                    currentPage === totalPages
                        ? "disabled"
                        : ""
                ) +
                '>&rsaquo;</button>';


            /*
             * Last
             */

            html +=
                '<button type="button" ' +
                'class="btn btn-sm btn-outline-secondary" ' +
                'data-page="' +
                totalPages +
                '" ' +
                (
                    currentPage === totalPages
                        ? "disabled"
                        : ""
                ) +
                '>&raquo;</button>';


            pagination.innerHTML =
                html;


            /*
             * Page click
             */

            var buttons =
                pagination.querySelectorAll(
                    "[data-page]"
                );

            buttons.forEach(
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
                                page === state.page
                            ) {
                                return;
                            }

                            state.page =
                                page;

                            load();
                        }
                    );
                }
            );
        }


        /*
         * =========================================================
         * GET VALUE
         * =========================================================
         */

        function getValue(
            object,
            field
        ) {

            var parts =
                field.split(".");

            var value =
                object;

            for (
                var i = 0;
                i < parts.length;
                i++
            ) {

                if (
                    value === null ||
                    value === undefined
                ) {
                    return null;
                }

                var currentKey =
                    Object.keys(value)
                        .find(
                            function (key) {

                                return key.toLowerCase() ===
                                    parts[i].toLowerCase();
                            }
                        );

                if (!currentKey) {
                    return null;
                }

                value =
                    value[currentKey];
            }

            return value;
        }


        /*
         * =========================================================
         * ESCAPE HTML
         * =========================================================
         */

        function escapeHtml(
            value
        ) {

            return String(value)
                .replace(
                    /&/g,
                    "&amp;"
                )
                .replace(
                    /</g,
                    "&lt;"
                )
                .replace(
                    />/g,
                    "&gt;"
                )
                .replace(
                    /"/g,
                    "&quot;"
                )
                .replace(
                    /'/g,
                    "&#039;"
                );
        }


        /*
         * =========================================================
         * SEARCH HIGHLIGHT
         * =========================================================
         */

        function escapeRegExp(
            value
        ) {

            return String(value)
                .replace(
                    /[.*+?^${}()|[\]\\]/g,
                    "\\$&"
                );
        }


        function highlightSearchText() {

            if (!body) {
                return;
            }


            /*
             * Remove previous highlights
             */

            var previousHighlights =
                body.querySelectorAll(
                    ".data-table-search-highlight"
                );

            previousHighlights.forEach(
                function (element) {

                    var parent =
                        element.parentNode;

                    if (!parent) {
                        return;
                    }

                    parent.replaceChild(
                        document.createTextNode(
                            element.textContent
                        ),
                        element
                    );

                    parent.normalize();
                }
            );


            /*
             * Nothing to highlight
             */

            if (!state.search) {
                return;
            }

            var searchValue =
                String(
                    state.search
                ).trim();

            if (!searchValue) {
                return;
            }

            var searchRegex =
                new RegExp(
                    escapeRegExp(
                        searchValue
                    ),
                    "gi"
                );


            /*
             * TreeWalker
             */

            var walker =
                document.createTreeWalker(
                    body,
                    NodeFilter.SHOW_TEXT,
                    {
                        acceptNode:
                            function (node) {

                                if (
                                    !node.nodeValue ||
                                    !node.nodeValue.trim()
                                ) {

                                    return NodeFilter.FILTER_REJECT;
                                }

                                var parent =
                                    node.parentElement;

                                if (!parent) {

                                    return NodeFilter.FILTER_REJECT;
                                }

                                if (
                                    parent.closest(
                                        "button, input, textarea, select"
                                    )
                                ) {

                                    return NodeFilter.FILTER_REJECT;
                                }

                                if (
                                    parent.closest(
                                        ".data-table-actions"
                                    )
                                ) {

                                    return NodeFilter.FILTER_REJECT;
                                }

                                return NodeFilter.FILTER_ACCEPT;
                            }
                    }
                );

            var nodes = [];

            var node;

            while (
                (node =
                    walker.nextNode())
            ) {

                nodes.push(node);
            }


            /*
             * Replace matching text
             */

            nodes.forEach(
                function (textNode) {

                    var text =
                        textNode.nodeValue;

                    if (
                        !searchRegex.test(text)
                    ) {

                        searchRegex.lastIndex =
                            0;

                        return;
                    }

                    searchRegex.lastIndex =
                        0;

                    var fragment =
                        document.createDocumentFragment();

                    var lastIndex =
                        0;

                    text.replace(
                        searchRegex,
                        function (
                            match,
                            offset
                        ) {

                            if (
                                offset > lastIndex
                            ) {

                                fragment.appendChild(
                                    document.createTextNode(
                                        text.substring(
                                            lastIndex,
                                            offset
                                        )
                                    )
                                );
                            }

                            var highlight =
                                document.createElement(
                                    "mark"
                                );

                            highlight.className =
                                "data-table-search-highlight";

                            highlight.textContent =
                                match;

                            fragment.appendChild(
                                highlight
                            );

                            lastIndex =
                                offset +
                                match.length;

                            return match;
                        }
                    );

                    if (
                        lastIndex < text.length
                    ) {

                        fragment.appendChild(
                            document.createTextNode(
                                text.substring(
                                    lastIndex
                                )
                            )
                        );
                    }

                    textNode.parentNode.replaceChild(
                        fragment,
                        textNode
                    );
                }
            );
        }


        /*
         * =========================================================
         * PAGE SIZE
         * =========================================================
         */

        if (pageSizeSelect) {

            pageSizeSelect.addEventListener(
                "change",
                function () {

                    state.pageSize =
                        parseInt(
                            pageSizeSelect.value,
                            10
                        );

                    state.page = 1;

                    load();
                }
            );
        }


        /*
         * =========================================================
         * SEARCH
         * =========================================================
         */

        if (searchInput) {

            searchInput.addEventListener(
                "input",
                function () {

                    clearTimeout(
                        searchTimer
                    );

                    searchTimer =
                        setTimeout(
                            function () {

                                state.search =
                                    searchInput.value.trim();

                                state.page = 1;

                                load();

                            },
                            options.search.debounceMs || 300
                        );
                }
            );
        }


        /*
         * =========================================================
         * SORTING
         * =========================================================
         */

        var sortButtons =
            root.querySelectorAll(
                ".data-table-sort"
            );

        sortButtons.forEach(
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
                            state.sortField === field
                        ) {

                            state.sortDirection =
                                state.sortDirection === "asc"
                                    ? "desc"
                                    : "asc";
                        }
                        else {

                            state.sortField =
                                field;

                            state.sortDirection =
                                "asc";
                        }

                        state.page = 1;

                        updateSortIcons();

                        load();
                    }
                );
            }
        );


        /*
         * =========================================================
         * SORT ICONS
         * =========================================================
         */

        function updateSortIcons() {

            sortButtons.forEach(
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
                        state.sortField
                    ) {

                        icon.textContent =
                            "";

                        return;
                    }

                    if (
                        state.sortDirection ===
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
        }


        /*
         * =========================================================
         * FILTERS
         * =========================================================
         */

        function updateFilterButton() {

            if (!filterButton) {
                return;
            }

            var count =
                Object.keys(
                    state.filters || {}
                ).length;


            if (filterCount) {

                filterCount.textContent =
                    String(count);

                filterCount.hidden =
                    count === 0;
            }


            if (count > 0) {

                filterButton.classList.add(
                    "has-filters"
                );

            }
            else {

                filterButton.classList.remove(
                    "has-filters"
                );
            }

            updateFilterColumnIndicators();
        }


        /*
         * =========================================================
         * FILTER COLUMN INDICATORS
         * =========================================================
         */

        function updateFilterColumnIndicators() {

            var headers =
                root.querySelectorAll(
                    "thead th"
                );

            if (
                !headers ||
                headers.length === 0
            ) {
                return;
            }


            /*
             * Remove old indicators
             */

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
                !state.filters ||
                Object.keys(
                    state.filters
                ).length === 0
            ) {
                return;
            }


            var visibleColumns =
                options.columns.filter(
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
                        !hasActiveFilter(
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
        }


        function hasActiveFilter(
            field
        ) {

            if (
                !state.filters ||
                !Object.prototype.hasOwnProperty.call(
                    state.filters,
                    field
                )
            ) {
                return false;
            }

            var value =
                state.filters[field];


            if (
                Array.isArray(value)
            ) {

                return value.some(
                    function (item) {

                        return (
                            item !== null &&
                            item !== undefined &&
                            String(item).trim() !== ""
                        );
                    }
                );
            }


            return (
                value !== null &&
                value !== undefined &&
                String(value).trim() !== ""
            );
        }


        /*
         * =========================================================
         * CLEAR FILTER MODAL
         * =========================================================
         */

        function clearFilterModal() {

            if (!filterModal) {
                return;
            }


            var controls =
                filterModal.querySelectorAll(
                    ".data-table-modal-filter-control"
                );

            controls.forEach(
                function (control) {

                    control.value =
                        "";
                }
            );


            var rangeControls =
                filterModal.querySelectorAll(
                    ".data-table-modal-range-control"
                );

            rangeControls.forEach(
                function (control) {

                    control.value =
                        "";
                }
            );


            var multiSelects =
                filterModal.querySelectorAll(
                    ".data-table-modal-multiselect"
                );

            multiSelects.forEach(
                function (container) {

                    var checkboxes =
                        container.querySelectorAll(
                            'input[type="checkbox"]'
                        );

                    checkboxes.forEach(
                        function (checkbox) {

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
        }


        /*
         * =========================================================
         * UPDATE MULTI SELECT TEXT
         * =========================================================
         */

        function updateMultiSelectText(
            container
        ) {

            var text =
                container.querySelector(
                    ".data-table-modal-multiselect-text"
                );

            if (!text) {
                return;
            }

            var checkboxes =
                container.querySelectorAll(
                    'input[type="checkbox"]'
                );

            var labels = [];


            checkboxes.forEach(
                function (checkbox) {

                    if (!checkbox.checked) {
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
                    labels.join(", ");

            }
            else {

                text.textContent =
                    labels.length +
                    " selected";
            }
        }


        /*
         * =========================================================
         * SYNC MODAL FROM STATE
         * =========================================================
         */

        function syncFilterModalFromState() {

            if (!filterModal) {
                return;
            }


            clearFilterModal();


            var filters =
                state.filters || {};


            /*
             * SIMPLE FILTERS
             */

            var controls =
                filterModal.querySelectorAll(
                    ".data-table-modal-filter-control"
                );

            controls.forEach(
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
                        value === undefined
                    ) {
                        return;
                    }

                    if (
                        Array.isArray(value)
                    ) {
                        return;
                    }

                    control.value =
                        String(value);
                }
            );


            /*
             * RANGE / MULTI SELECT
             */

            var filterElements =
                filterModal.querySelectorAll(
                    ".data-table-modal-filter"
                );

            filterElements.forEach(
                function (filterElement) {

                    var field =
                        filterElement.getAttribute(
                            "data-filter-field"
                        );

                    if (!field) {
                        return;
                    }

                    var value =
                        filters[field];


                    /*
                     * RANGE
                     */

                    var rangeControls =
                        filterElement.querySelectorAll(
                            ".data-table-modal-range-control"
                        );

                    if (
                        rangeControls.length > 0 &&
                        Array.isArray(value)
                    ) {

                        if (
                            rangeControls[0]
                        ) {

                            rangeControls[0].value =
                                value[0] || "";
                        }

                        if (
                            rangeControls[1]
                        ) {

                            rangeControls[1].value =
                                value[1] || "";
                        }
                    }


                    /*
                     * MULTI SELECT
                     */

                    var multiSelect =
                        filterElement.querySelector(
                            ".data-table-modal-multiselect"
                        );

                    if (multiSelect) {

                        var values =
                            Array.isArray(value)
                                ? value.map(
                                    function (item) {
                                        return String(item);
                                    }
                                )
                                : [];

                        var checkboxes =
                            multiSelect.querySelectorAll(
                                'input[type="checkbox"]'
                            );

                        checkboxes.forEach(
                            function (checkbox) {

                                checkbox.checked =
                                    values.indexOf(
                                        String(
                                            checkbox.value
                                        )
                                    ) !== -1;
                            }
                        );

                        updateMultiSelectText(
                            multiSelect
                        );
                    }
                }
            );
        }


        /*
         * =========================================================
         * READ FILTERS FROM MODAL
         * =========================================================
         */

        function readFiltersFromModal() {

            var filters = {};

            if (!filterModal) {
                return filters;
            }


            /*
             * SIMPLE
             */

            var controls =
                filterModal.querySelectorAll(
                    ".data-table-modal-filter-control"
                );

            controls.forEach(
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


            /*
             * FILTER ELEMENTS
             */

            var filterElements =
                filterModal.querySelectorAll(
                    ".data-table-modal-filter"
                );

            filterElements.forEach(
                function (filterElement) {

                    var field =
                        filterElement.getAttribute(
                            "data-filter-field"
                        );

                    if (!field) {
                        return;
                    }


                    /*
                     * RANGE
                     */

                    var rangeControls =
                        filterElement.querySelectorAll(
                            ".data-table-modal-range-control"
                        );

                    if (
                        rangeControls.length > 0
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


                    /*
                     * MULTI SELECT
                     */

                    var multiSelect =
                        filterElement.querySelector(
                            ".data-table-modal-multiselect"
                        );

                    if (multiSelect) {

                        var values = [];

                        var checkboxes =
                            multiSelect.querySelectorAll(
                                'input[type="checkbox"]'
                            );

                        checkboxes.forEach(
                            function (checkbox) {

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
                            values.length > 0
                        ) {

                            filters[field] =
                                values;
                        }
                    }
                }
            );


            return filters;
        }


        /*
         * =========================================================
         * INITIALIZE FILTER MODAL
         * =========================================================
         */

        function initializeFilterModal() {

            if (!filterModal) {

                updateFilterButton();

                return;
            }


            /*
             * Bootstrap
             */

            if (
                typeof bootstrap !==
                "undefined" &&
                bootstrap.Modal
            ) {

                filterModalInstance =
                    bootstrap.Modal.getOrCreateInstance(
                        filterModal
                    );

            }
            else {

                console.error(
                    "Bootstrap Modal is not available. " +
                    "Make sure bootstrap.bundle.min.js is loaded."
                );
            }


            /*
             * FILTER BUTTON
             */

            if (filterButton) {

                filterButton.addEventListener(
                    "click",
                    function () {

                        syncFilterModalFromState();

                        if (
                            filterModalInstance
                        ) {

                            filterModalInstance.show();
                        }
                    }
                );
            }


            /*
             * APPLY
             */

            if (filterApplyButton) {

                filterApplyButton.addEventListener(
                    "click",
                    function () {

                        state.filters =
                            readFiltersFromModal();

                        state.page =
                            1;

                        updateFilterButton();

                        if (
                            filterModalInstance
                        ) {

                            filterModalInstance.hide();
                        }

                        load();
                    }
                );
            }


            /*
             * RESET
             */

            if (filterResetButton) {

                filterResetButton.addEventListener(
                    "click",
                    function () {

                        state.filters =
                            {};

                        state.page =
                            1;

                        clearFilterModal();

                        updateFilterButton();

                        if (
                            filterModalInstance
                        ) {

                            filterModalInstance.hide();
                        }

                        load();
                    }
                );
            }


            /*
             * MULTI SELECTS
             */

            var multiSelects =
                filterModal.querySelectorAll(
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


                    /*
                     * Open / close
                     */

                    if (button) {

                        button.addEventListener(
                            "click",
                            function (event) {

                                event.preventDefault();
                                event.stopPropagation();


                                multiSelects.forEach(
                                    function (other) {

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


                    /*
                     * Checkbox
                     */

                    checkboxes.forEach(
                        function (checkbox) {

                            checkbox.addEventListener(
                                "change",
                                function () {

                                    updateMultiSelectText(
                                        container
                                    );
                                }
                            );
                        }
                    );
                }
            );


            /*
             * CLICK OUTSIDE
             *
             * IMPORTANT:
             * Previously this was attached to document.
             *
             * Now it is attached to THIS DataTable root.
             */

            root.addEventListener(
                "click",
                function (event) {

                    if (
                        !event.target.closest(
                            ".data-table-modal-multiselect"
                        )
                    ) {

                        multiSelects.forEach(
                            function (container) {

                                container.classList.remove(
                                    "open"
                                );
                            }
                        );
                    }
                }
            );


            /*
             * Bootstrap modal opened
             */

            filterModal.addEventListener(
                "show.bs.modal",
                function () {

                    syncFilterModalFromState();
                }
            );


            updateFilterButton();
        }


        /*
         * =========================================================
         * INLINE EDIT
         * =========================================================
         */

        function bindInlineEditButtons() {

            if (!options.inlineEditing) {
                return;
            }

            var buttons =
                body.querySelectorAll(
                    ".data-table-inline-edit"
                );

            buttons.forEach(
                function (button) {

                    button.addEventListener(
                        "click",
                        function () {

                            var id =
                                button.getAttribute(
                                    "data-id"
                                );

                            if (!id) {
                                return;
                            }

                            enterInlineEdit(
                                id
                            );
                        }
                    );
                }
            );
        }


        /*
         * =========================================================
         * ENTER INLINE EDIT
         * =========================================================
         */

        function enterInlineEdit(
            id
        ) {

            var rows =
                body.querySelectorAll(
                    "tr[data-row-id]"
                );

            var row = null;

            rows.forEach(
                function (item) {

                    if (
                        String(
                            item.getAttribute(
                                "data-row-id"
                            )
                        ) ===
                        String(id)
                    ) {

                        row = item;
                    }
                }
            );

            if (!row) {
                return;
            }

            if (
                row.classList.contains(
                    "data-table-editing"
                )
            ) {
                return;
            }

            row.classList.add(
                "data-table-editing"
            );


            /*
             * Editable cells
             */

            options.columns.forEach(
                function (column) {

                    if (
                        !column.visible ||
                        !column.editable
                    ) {
                        return;
                    }


                    /*
                     * IMPORTANT:
                     *
                     * Don't use escapeHtml()
                     * inside CSS selector.
                     *
                     * The field value is already
                     * generated by our backend.
                     */

                    var cell =
                        findCellByField(
                            row,
                            column.field
                        );

                    if (!cell) {
                        return;
                    }


                    var value =
                        cell.getAttribute(
                            "data-original-value"
                        );

                    if (
                        value === null
                    ) {

                        value =
                            cell.textContent.trim();

                        if (
                            value ===
                            column.emptyText
                        ) {

                            value =
                                "";
                        }

                        cell.setAttribute(
                            "data-original-value",
                            value
                        );
                    }


                    /*
                     * BOOLEAN
                     */

                    if (
                        column.type ===
                        "Boolean"
                    ) {

                        var checked =
                            value === "true" ||
                            value === "True" ||
                            value === "1";

                        cell.innerHTML =
                            '<input type="checkbox" ' +
                            'class="form-check-input data-table-inline-input" ' +
                            'data-field="' +
                            escapeHtml(
                                column.field
                            ) +
                            '" ' +
                            (
                                checked
                                    ? "checked"
                                    : ""
                            ) +
                            '>';

                    }
                    else {

                        var inputType =
                            "text";

                        var inputMode =
                            "";

                        if (
                            column.type ===
                            "Number" ||
                            column.type ===
                            "Currency"
                        ) {

                            inputType =
                                "text";

                            inputMode =
                                "numeric";
                        }


                        cell.innerHTML =
                            '<input type="' +
                            inputType +
                            '" ' +
                            'class="form-control form-control-sm ' +
                            'data-table-inline-input" ' +
                            'data-field="' +
                            escapeHtml(
                                column.field
                            ) +
                            '" ' +
                            (
                                inputMode
                                    ? 'inputmode="' +
                                    inputMode +
                                    '" '
                                    : ""
                            ) +
                            'value="' +
                            escapeHtml(
                                value
                            ) +
                            '">';


                        /*
                         * NUMBER VALIDATION
                         */

                        if (
                            column.type ===
                            "Number" ||
                            column.type ===
                            "Currency"
                        ) {

                            var numericInput =
                                cell.querySelector(
                                    ".data-table-inline-input"
                                );

                            if (
                                numericInput
                            ) {

                                numericInput.addEventListener(
                                    "input",
                                    function () {

                                        this.value =
                                            this.value.replace(
                                                /[^0-9.]/g,
                                                ""
                                            );
                                    }
                                );


                                numericInput.addEventListener(
                                    "keydown",
                                    function (
                                        event
                                    ) {

                                        var allowedKeys =
                                            [
                                                "Backspace",
                                                "Delete",
                                                "Tab",
                                                "ArrowLeft",
                                                "ArrowRight",
                                                "Home",
                                                "End"
                                            ];

                                        if (
                                            allowedKeys.indexOf(
                                                event.key
                                            ) !== -1
                                        ) {
                                            return;
                                        }

                                        if (
                                            event.ctrlKey ||
                                            event.metaKey
                                        ) {
                                            return;
                                        }

                                        if (
                                            event.key >= "0" &&
                                            event.key <= "9"
                                        ) {
                                            return;
                                        }

                                        if (
                                            event.key === "." &&
                                            this.value.indexOf(
                                                "."
                                            ) === -1
                                        ) {
                                            return;
                                        }

                                        event.preventDefault();
                                    }
                                );
                            }
                        }
                    }
                }
            );


            /*
             * ACTIONS
             */

            var actions =
                row.querySelector(
                    ".data-table-actions"
                );

            if (!actions) {
                return;
            }


            actions.innerHTML =
                '<button type="button" ' +
                'class="btn btn-sm btn-success ' +
                'data-table-inline-save" ' +
                'title="Save" ' +
                'aria-label="Save">' +
                '<i class="bx bx-check"></i>' +
                '</button> ' +

                '<button type="button" ' +
                'class="btn btn-sm btn-outline-secondary ' +
                'data-table-inline-cancel" ' +
                'title="Cancel" ' +
                'aria-label="Cancel">' +
                '<i class="bx bx-x"></i>' +
                '</button>';


            bindInlineSaveCancel(
                row
            );
        }


        /*
         * =========================================================
         * FIND CELL
         * =========================================================
         */

        function findCellByField(
            row,
            field
        ) {

            var cells =
                row.querySelectorAll(
                    "td[data-field]"
                );

            var result =
                null;

            cells.forEach(
                function (cell) {

                    if (
                        cell.getAttribute(
                            "data-field"
                        ) === field
                    ) {

                        result =
                            cell;
                    }
                }
            );

            return result;
        }


        /*
         * =========================================================
         * SAVE / CANCEL
         * =========================================================
         */

        function bindInlineSaveCancel(
            row
        ) {

            var saveButton =
                row.querySelector(
                    ".data-table-inline-save"
                );

            var cancelButton =
                row.querySelector(
                    ".data-table-inline-cancel"
                );


            /*
             * SAVE
             */

            if (saveButton) {

                saveButton.addEventListener(
                    "click",
                    function () {

                        saveInlineRow(
                            row
                        );
                    }
                );
            }


            /*
             * CANCEL
             */

            if (cancelButton) {

                cancelButton.addEventListener(
                    "click",
                    function () {

                        load();
                    }
                );
            }
        }


        /*
         * =========================================================
         * SAVE INLINE ROW
         * =========================================================
         */

        function saveInlineRow(
            row
        ) {

            var id =
                row.getAttribute(
                    "data-row-id"
                );

            if (!id) {
                return;
            }

            if (
                !options.inlineUpdateUrl
            ) {

                alert(
                    "Inline update URL is not configured."
                );

                return;
            }

            var fields = {};


            var inputs =
                row.querySelectorAll(
                    ".data-table-inline-input"
                );

            inputs.forEach(
                function (input) {

                    var field =
                        input.getAttribute(
                            "data-field"
                        );

                    if (!field) {
                        return;
                    }

                    if (
                        input.type ===
                        "checkbox"
                    ) {

                        fields[field] =
                            input.checked
                                ? "true"
                                : "false";

                    }
                    else {

                        fields[field] =
                            input.value;
                    }
                }
            );


            /*
             * REQUEST
             */

            var request = {

                id: id,

                fields: fields
            };


            /*
             * SAVE BUTTON
             */

            var saveButton =
                row.querySelector(
                    ".data-table-inline-save"
                );

            if (saveButton) {

                saveButton.disabled =
                    true;

                saveButton.innerHTML =
                    '<i class="bx bx-loader-alt bx-spin"></i>';

                saveButton.setAttribute(
                    "title",
                    "Saving..."
                );
            }


            /*
             * AJAX
             */

            fetch(
                options.inlineUpdateUrl,
                {
                    method: "POST",

                    headers: {

                        "Content-Type":
                            "application/json",

                        "RequestVerificationToken":
                            getAntiForgeryToken()
                    },

                    body:
                        JSON.stringify(
                            request
                        )
                }
            )
                .then(
                    function (response) {

                        return response
                            .json()
                            .then(
                                function (result) {

                                    if (
                                        !response.ok
                                    ) {

                                        throw new Error(
                                            result.message ||
                                            "Update failed."
                                        );
                                    }

                                    return result;
                                }
                            );
                    }
                )
                .then(
                    function (result) {

                        if (
                            !result.success
                        ) {

                            throw new Error(
                                result.message ||
                                "Update failed."
                            );
                        }

                        load();
                    }
                )
                .catch(
                    function (error) {

                        console.error(
                            error
                        );

                        alert(
                            error.message ||
                            "Unable to save changes."
                        );


                        if (saveButton) {

                            saveButton.disabled =
                                false;

                            saveButton.innerHTML =
                                '<i class="bx bx-check"></i>';

                            saveButton.setAttribute(
                                "title",
                                "Save"
                            );
                        }
                    }
                );
        }


        /*
         * =========================================================
         * ANTI FORGERY TOKEN
         * =========================================================
         *
         * IMPORTANT:
         * Search only inside this table.
         */

        function getAntiForgeryToken() {

            var input =
                root.querySelector(
                    'input[name="__RequestVerificationToken"]'
                );

            if (!input) {
                return "";
            }

            return input.value;
        }


        /*
         * =========================================================
         * INITIAL SORT
         * =========================================================
         */

        updateSortIcons();


        /*
         * =========================================================
         * INITIAL FILTER
         * =========================================================
         */

        initializeFilterModal();


        /*
         * =========================================================
         * RELOAD
         * =========================================================
         */

        function resetTableToDefaultState() {

            state.page =
                1;

            state.search =
                "";

            state.filters =
                {};

            state.sortField =
                options.sorting &&
                    options.sorting.defaultField
                    ? options.sorting.defaultField
                    : "";

            state.sortDirection =
                options.sorting &&
                    options.sorting.defaultDirection
                    ? options.sorting.defaultDirection
                    : "asc";


            /*
             * Search
             */

            if (searchInput) {

                searchInput.value =
                    "";
            }


            /*
             * Filter modal
             */

            syncFilterModalFromState();


            /*
             * Filter button
             */

            updateFilterButton();


            /*
             * Sorting
             */

            updateSortIcons();


            /*
             * Reload
             */

            load();
        }


        var reloadButton =
            root.querySelector(
                "[data-table-reload]"
            );

        if (reloadButton) {

            reloadButton.addEventListener(
                "click",
                function () {

                    resetTableToDefaultState();
                }
            );
        }


        /*
         * =========================================================
         * INITIAL LOAD
         * =========================================================
         */

        if (
            options.autoLoad !== false
        ) {

            load();
        }
    }


    /*
     * =========================================================
     * PUBLIC API
     * =========================================================
     */

    window.DataTable =
        DataTable;

})(window, document);