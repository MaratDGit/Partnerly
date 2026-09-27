function initUniqueDataTable(tableId, ajaxUrl, options = {}) {
    const exportAllUrl = options.exportAllUrl || null;
    const exportSelectedUrl = options.exportSelectedUrl || null;

    $.getJSON(ajaxUrl, options.getDataParams || {}, function (response) {
        let fields = response.fields;
        let data = response.data;

        let selectedIds = new Set();
        let hasVisibleActions = data.some(row => (row.actions || []).some(a => a.isVisible));

        let columns = fields.map(f => {
            const isSelectColumn = f.fieldName === "select";
            return {
                data: f.fieldName,
                title: isSelectColumn ? `<input type="checkbox" id="select-all-checkbox" />` : f.displayName,
                visible: f.isVisible !== false,
                orderable: f.isSortable,
                isFilterable: f.isFilterable,
                cssClass: f.cssClass,
                style: f.style,
                valueStyles: f.valueStyles,

                render: function (data, type, row, searchValue) {

                    if (data == null) {
                        data = f.defaultValue ?? null;
                    }

                    if (data == null) {
                        return "";
                    }

                    const title = data;
                    if (typeof data === "string" && data.length > 40) {
                        data = data.substring(0, 40) + "...";
                    }

                    const styleAttributes = getGridStyleAttributes(f, data);

                    if (f.type === "checkbox") {
                        if (isSelectColumn) {
                            const checked = selectedIds.has(row.id) ? "checked" : "";
                            return `<input type="checkbox" class="row-select-checkbox" data-id="${row.id}" ${checked} />`;
                        }
                        return `<input type="checkbox" disabled ${data ? "checked" : ""} />`;
                    }

                    if (f.linkTemplate) {
                        let href = f.linkTemplate;
                        href = href.replace(/{(\w+)}/g, (match, key) => {
                            return row.hasOwnProperty(key) ? row[key] : "";
                        });

                        const displayData = highlightSearchText(data, searchValue);
                        return `<a 
                                title="${title}" 
                                href="${href}"
                                ${styleAttributes}
                                >${displayData}</a>`;
                    }

                    if (f.format) {
                        const [typeFormat, formatString] = f.format.split(":");

                        if (typeFormat === "date") {

                            const d = new Date(data);

                            if (isNaN(d.getTime())) {
                                return data;
                            }

                            if (formatString === "lastActivity") {

                                const now = new Date();

                                const diffMs = now.getTime() - d.getTime();

                                const diffMin = Math.floor(diffMs / 60000);
                                const diffHour = Math.floor(diffMin / 60);

                                const loc = dateLocalization;

                                if (diffMin < 1) {
                                    return loc.justNow;
                                }

                                if (diffMin === 1) {
                                    return loc.minuteAgo;
                                }

                                if (diffMin < 60) {
                                    return `${diffMin} ${loc.minutesAgo}`;
                                }

                                if (diffHour === 1) {
                                    return loc.hourAgo;
                                }

                                if (diffHour < 24) {
                                    return `${diffHour} ${loc.hoursAgo}`;
                                }

                                return new Intl.DateTimeFormat(loc.culture, {
                                    day: "2-digit",
                                    month: "2-digit",
                                    year: "numeric"
                                }).format(d);
                            }

                            switch (formatString) {

                                case "MM/dd/yyyy":
                                case "dd.MM.yyyy":
                                    return new Intl.DateTimeFormat(dateLocalization.culture, {
                                        day: "2-digit",
                                        month: "2-digit",
                                        year: "numeric"
                                    }).format(d);

                                case "dd/MM/yyyy HH:mm":
                                    return new Intl.DateTimeFormat(dateLocalization.culture, {
                                        day: "2-digit",
                                        month: "2-digit",
                                        year: "numeric",
                                        hour: "2-digit",
                                        minute: "2-digit"
                                    }).format(d);

                                default:
                                    return new Intl.DateTimeFormat(dateLocalization.culture).format(d);
                            }
                        }
                        if (typeFormat === "phone") {
                            const phone = String(data).replace(/\D/g, "");

                            if (phone.length === 11 && phone.startsWith("374")) {
                                return `+374-${phone.substring(3, 5)}-${phone.substring(5, 7)}-${phone.substring(7, 9)}-${phone.substring(9, 11)}`;
                            }

                            return data;
                        }
                        if (typeFormat === "number") return parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: parseInt(formatString) || 0 });
                        if (typeFormat === "currency") return new Intl.NumberFormat("en-US", { style: "currency", currency: formatString || "USD" }).format(data);
                        if (typeFormat === "size") {
                            const size = Number(data);

                            if (isNaN(size)) {
                                return data;
                            }

                            const units = ["B", "KB", "MB", "GB", "TB"];

                            let value = size;
                            let unitIndex = 0;

                            while (value >= 1024 && unitIndex < units.length - 1) {
                                value /= 1024;
                                unitIndex++;
                            }

                            const decimals = value < 10 && unitIndex > 0 ? 2 : 0;

                            return `${value.toLocaleString(undefined, {
                                minimumFractionDigits: decimals,
                                maximumFractionDigits: decimals
                            })} ${units[unitIndex]}`;
                        }
                    }

                    const displayData = highlightSearchText(data, searchValue);

                    return `<span title="${title}"${styleAttributes} ${f.attr || ""}>${displayData}</span>`;
                }
            };
        });

        if (hasVisibleActions) {
            columns.push({
                data: null,
                title: dataTableLocalization.actions,
                orderable: false,
                render: function (data, type, row) {
                    let items = (row.actions || []).filter(a => a.isVisible).map(a => {
                        let url = a.urlTemplate.replace("{id}", row.id);
                        return `<a class="dropdown-item ${a.cssClass || ""}" href="${url}" ${a.attr || ""}><i class="${a.icon} me-1"></i> ${a.displayName}</a>`;
                    }).join("");
                    return `<div class="dropdown">
                        <button type="button" class="btn p-0 dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                            <i class="bx bx-dots-vertical-rounded"></i>
                        </button>
                        <div class="dropdown-menu">${items}</div>
                    </div>`;
                }
            });
        }

        $(`#${tableId}`).CustomDataTable({
            data: data,
            columns: columns,
            pageSize: options.pageSize || 10,
            exportAllUrl: exportAllUrl,
            exportSelectedUrl: exportSelectedUrl
        });

        $(document).on('change', `#${tableId} .row-select-checkbox`, function () {
            const id = $(this).data('id');
            if (this.checked) selectedIds.add(id);
            else selectedIds.delete(id);
        });

        $(document).on("datatable:reset", function (e, args) {
            if (args.tableId !== tableId) return;
            selectedIds.clear();
            $(`#${tableId} .row-select-checkbox`).prop("checked", false);
            $(`#${tableId}`).closest(".custom-datatable-card").find(".custom-datatable-search input").val("");
            $(`#${tableId}`).trigger("reload");
        });

        // Экспорт с динамическими URL
        $(document).on("datatable:exportAll", function (e, args) {
            if (args.tableId !== tableId || !exportAllUrl) return;
            postToController(exportAllUrl, data);
        });

        $(document).on("datatable:exportSelected", function (e, args) {
            if (args.tableId !== tableId || !exportSelectedUrl) return;
            postToController(exportSelectedUrl, [...selectedIds]);
        });

        // Выбор всех строк
        $(document).on('change', '#select-all-checkbox', function () {
            if (this.checked) {
                // добавить все id в selectedIds
                response.data.forEach(row => selectedIds.add(row.id));
                $(`#${tableId} .row-select-checkbox`).prop('checked', true);
            } else {
                // снять все
                selectedIds.clear();
                $(`#${tableId} .row-select-checkbox`).prop('checked', false);
            }
        });

        // Синхронизация header при выборке отдельных чекбоксов
        $(document).on('change', `#${tableId} .row-select-checkbox`, function () {
            const id = $(this).data('id');
            if (this.checked) selectedIds.add(id);
            else selectedIds.delete(id);

            // обновляем состояние header checkbox
            const allChecked = response.data.every(row => selectedIds.has(row.id));
            $('#select-all-checkbox').prop('checked', allChecked);
        });

        // --- Делаем selectedIds доступным снаружи (по ID таблицы) ---
        window[`selectedIds_${tableId}`] = selectedIds;

    });
}

function postToController(url, data) {
    const form = document.createElement("form");
    form.method = "POST";
    form.action = url;

    const input = document.createElement("input");
    input.type = "hidden";
    input.name = "selectedIds";
    input.value = JSON.stringify(data);
    form.appendChild(input);

    document.body.appendChild(form);
    form.submit();
    document.body.removeChild(form);
}

function getGridStyleAttributes(field, value) {
    let cssClass = field.cssClass || "";
    let style = field.style || "";

    if (field.valueStyles && value != null) {
        const valueStyle = field.valueStyles[String(value)];

        if (valueStyle) {
            if (valueStyle.cssClass) {
                cssClass += ` ${valueStyle.cssClass}`;
            }

            if (valueStyle.style) {
                style += ` ${valueStyle.style}`;
            }
        }
    }

    let attributes = "";

    if (cssClass) {
        attributes += ` class="${cssClass}"`;
    }

    if (style) {
        attributes += ` style="${style}"`;
    }

    return attributes;
}

function highlightSearchText(value, search) {
    if (!search || value == null) {
        return value ?? "";
    }

    const text = String(value);
    const escapedSearch = search.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

    return text.replace(
        new RegExp(`(${escapedSearch})`, "gi"),
        '<mark class="datatable-search-highlight">$1</mark>'
    );
}
