(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.utils = {

        setup: function (ctx) {

            ctx.getValue =
                function (object, field) {

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
                            Object.keys(
                                value
                            ).find(
                                function (key) {

                                    return (
                                        key.toLowerCase() ===
                                        parts[i].toLowerCase()
                                    );
                                }
                            );

                        if (!currentKey) {
                            return null;
                        }

                        value =
                            value[currentKey];
                    }

                    return value;
                };


            ctx.getRowValue =
                function (row, field) {

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
                };


            ctx.escapeHtml =
                function (value) {

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
                };


            ctx.escapeRegExp =
                function (value) {

                    return String(value).replace(
                        /[.*+?^${}()|[\]\\]/g,
                        "\\$&"
                    );
                };


            ctx.parseDateValue =
                function (value) {

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

                    if (
                        stringValue.charAt(0) ===
                        "{" &&
                        stringValue.charAt(
                            stringValue.length - 1
                        ) ===
                        "}"
                    ) {

                        stringValue =
                            stringValue.substring(
                                1,
                                stringValue.length - 1
                            ).trim();
                    }

                    var dotNetMatch =
                        stringValue.match(
                            /^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2}):(\d{2})\s*(AM|PM)$/i
                        );

                    if (dotNetMatch) {

                        var month =
                            Number(
                                dotNetMatch[1]
                            );

                        var day =
                            Number(
                                dotNetMatch[2]
                            );

                        var year =
                            Number(
                                dotNetMatch[3]
                            );

                        var hours =
                            Number(
                                dotNetMatch[4]
                            );

                        var minutes =
                            Number(
                                dotNetMatch[5]
                            );

                        var seconds =
                            Number(
                                dotNetMatch[6]
                            );

                        var amPm =
                            dotNetMatch[7]
                                .toUpperCase();

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

                    var isoLocalMatch =
                        stringValue.match(
                            /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?$/
                        );

                    if (isoLocalMatch) {

                        var milliseconds =
                            isoLocalMatch[7]
                                ? Number(
                                    (
                                        "0." +
                                        isoLocalMatch[7]
                                    ) * 1000
                                )
                                : 0;

                        return new Date(
                            Date.UTC(
                                Number(
                                    isoLocalMatch[1]
                                ),
                                Number(
                                    isoLocalMatch[2]
                                ) - 1,
                                Number(
                                    isoLocalMatch[3]
                                ),
                                Number(
                                    isoLocalMatch[4]
                                ),
                                Number(
                                    isoLocalMatch[5]
                                ),
                                Number(
                                    isoLocalMatch[6]
                                ),
                                milliseconds
                            )
                        );
                    }

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
                };


            ctx.formatRelativeDate =
                function (date) {

                    var dateLoc =
                        dateLocalizationDataTable || {};

                    var defaultDateLocalization = {
                        justNow: "just now",
                        minuteAgo: "minute ago",
                        minutesAgo: "minutes ago",
                        hourAgo: "hour ago",
                        hoursAgo: "hours ago",
                        daysAgo: "days ago",
                        yesterday: "yesterday"
                    };

                    Object.keys(defaultDateLocalization).forEach(function (key) {

                        if (
                            !dateLoc[key] ||
                            typeof dateLoc[key] !== "string" ||
                            !dateLoc[key].trim()
                        ) {
                            dateLoc[key] =
                                defaultDateLocalization[key];
                        }

                    });

                    var now =
                        new Date();

                    var diff =
                        now.getTime() -
                        date.getTime();

                    var seconds =
                        Math.floor(
                            diff / 1000
                        );

                    if (
                        seconds < 0 ||
                        seconds < 60
                    ) {
                        return dateLoc.justNow;
                    }

                    var minutes =
                        Math.floor(
                            seconds / 60
                        );

                    if (
                        minutes < 60
                    ) {

                        return minutes + " " +
                            (
                                minutes === 1
                                ? dateLoc.minuteAgo
                                : dateLoc.minutesAgo
                            );
                    }

                    var hours =
                        Math.floor(
                            minutes / 60
                        );

                    if (
                        hours < 24
                    ) {

                        return hours + " " +
                            (
                                hours === 1
                                ? dateLoc.hourAgo
                                : dateLoc.hoursAgo
                            );
                    }

                    var days =
                        Math.floor(
                            hours / 24
                        );

                    if (
                        days === 1
                    ) {
                        return dateLoc.yesterday;
                    }

                    if (
                        days < 7
                    ) {
                        return days + " " +
                            dateLoc.daysAgo;
                    }

                    return date.toLocaleDateString();
                };


            ctx.formatDatePattern =
                function (
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
                };


            ctx.formatDate =
                function (
                    value,
                    format
                ) {

                    var date =
                        ctx.parseDateValue(
                            value
                        );

                    if (!date) {
                        return value;
                    }

                    if (!format) {
                        return date.toLocaleDateString();
                    }

                    return ctx.formatDatePattern(
                        date,
                        format
                    );
                };


            ctx.formatDateTime =
                function (
                    value,
                    format
                ) {

                    var date =
                        ctx.parseDateValue(
                            value
                        );

                    if (!date) {
                        return value;
                    }

                    if (
                        format &&
                        format.toLowerCase() ===
                        "relative"
                    ) {

                        return ctx.formatRelativeDate(
                            date
                        );
                    }

                    if (!format) {
                        return date.toLocaleString();
                    }

                    return ctx.formatDatePattern(
                        date,
                        format
                    );
                };


            ctx.findCellByField =
                function (
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
                                ) ===
                                field
                            ) {

                                result =
                                    cell;
                            }
                        }
                    );

                    return result;
                };


            ctx.getAntiForgeryToken =
                function () {
                var input = ctx.root.querySelector(
                    'input[name="__RequestVerificationToken"]'
                );

                if (!input) {
                    input = document.querySelector(
                        'input[name="__RequestVerificationToken"]'
                    );
                }

                return input ? input.value : "";
            };


            ctx.getConfiguredKeyFields =
                function () {
                var columns = ctx.options.columns || [];

                var keyFields = columns
                    .filter(function (column) {
                        return column.isKey === true ||
                            column.IsKey === true;
                    })
                    .map(function (column) {
                        return column.field || column.Field;
                    })
                    .filter(function (field) {
                        return !!field;
                    });

                // Совместимость со старыми таблицами.
                if (keyFields.length === 0 && ctx.options.keyField) {
                    keyFields.push(ctx.options.keyField);
                }

                return keyFields;
            };

            ctx.getKeysFromItem =
                function (item) {
                var keys = {};

                ctx.getConfiguredKeyFields().forEach(function (field) {
                    keys[field] = ctx.getValue(item, field);
                });

                return keys;
            };

            ctx.getRowKeys =
                function (row) {
                if (!row) {
                    return {};
                }

                try {
                    return JSON.parse(row.getAttribute("data-row-keys") || "{}");
                } catch (error) {
                    console.error("Unable to read DataTable row keys.", error);
                    return {};
                }
                };


            ctx.getParentKeys =
                function () {
                var keys = {};

                // Explicit context, if provided on the DataTable element.
                var rawKeys = ctx.root.getAttribute("data-parent-keys");

                if (rawKeys) {
                    try {
                        var parsedKeys = JSON.parse(rawKeys);

                        if (
                            parsedKeys &&
                            typeof parsedKeys === "object" &&
                            !Array.isArray(parsedKeys)
                        ) {
                            Object.assign(keys, parsedKeys);
                        }
                    } catch (error) {
                        console.error("Invalid data-parent-keys:", error);
                    }
                }

                // Otherwise, discover keys marked on the parent edit form.
                var form = ctx.root.closest("form");

                if (form) {
                    form.querySelectorAll("[data-parent-key]").forEach(
                        function (input) {
                            var field = input.getAttribute("data-parent-key");

                            if (!field) {
                                return;
                            }

                            if (
                                input.type === "checkbox" &&
                                !input.checked
                            ) {
                                keys[field] = "false";
                                return;
                            }

                            keys[field] = input.value;
                        }
                    );
                }

                return keys;
            };

            ctx.appendParentKeysToUrl =
                function (url) {
                var parentKeys = ctx.getParentKeys();

                Object.keys(parentKeys).forEach(function (key) {
                    var value = parentKeys[key];

                    if (value !== null && value !== undefined) {
                        url.searchParams.set(
                            "ParentKeys[" + key + "]",
                            String(value)
                        );
                    }
                });

                return url;
            };

            ctx.addParentKeysToPayload =
                function (payload) {
                payload.parentKeys = ctx.getParentKeys();
                return payload;
            };
        }
    };

})(window, document);