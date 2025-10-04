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
                title: f.displayName,
                visible: f.isVisible,
                orderable: f.isSortable,
                render: function (data, type, row) {
                    if (data == null && f.defaultValue !== undefined && f.defaultValue !== null) data = f.defaultValue;
                    if (f.type === "checkbox") {
                        if (isSelectColumn) {
                            const checked = selectedIds.has(row.id) ? "checked" : "";
                            return `<input type="checkbox" class="row-select-checkbox" data-id="${row.id}" ${checked} />`;
                        }
                        return `<input type="checkbox" disabled ${data ? "checked" : ""} />`;
                    }
                    if (f.linkTemplate) return `<a href="${f.linkTemplate.replace("{id}", row.id)}">${data}</a>`;
                    if (f.format) {
                        const [typeFormat, formatString] = f.format.split(":");
                        if (typeFormat === "date") {
                            const d = new Date(data);
                            if (isNaN(d)) return data;
                            switch (formatString) {
                                case "MM/dd/yyyy":
                                    return (d.getMonth() + 1).toString().padStart(2, "0") + "/" + d.getDate().toString().padStart(2, "0") + "/" + d.getFullYear();
                                case "dd.MM.yyyy":
                                    return d.getDate().toString().padStart(2, "0") + "." + (d.getMonth() + 1).toString().padStart(2, "0") + "." + d.getFullYear();
                                case "dd/MM/yyyy HH:mm":
                                    return d.getDate().toString().padStart(2, "0") + "/" + (d.getMonth() + 1).toString().padStart(2, "0") + "/" + d.getFullYear() + " " + d.getHours().toString().padStart(2, "0") + ":" + d.getMinutes().toString().padStart(2, "0");
                                default:
                                    return d.toLocaleDateString();
                            }
                        }
                        if (typeFormat === "number") return parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: parseInt(formatString) || 0 });
                        if (typeFormat === "currency") return new Intl.NumberFormat("en-US", { style: "currency", currency: formatString || "USD" }).format(data);
                    }
                    return `<span ${f.attr || ""}>${data}</span>`;
                }
            };
        });

        if (hasVisibleActions) {
            columns.push({
                data: null,
                title: "Действия",
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

        $(`#${tableId}`).CustomDataTable({ data: data, columns: columns });

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
