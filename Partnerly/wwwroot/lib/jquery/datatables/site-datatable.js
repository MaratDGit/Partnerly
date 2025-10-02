function initUniqueDataTable(tableId, ajaxUrl) {
    $.getJSON(ajaxUrl, function (response) {
        let fields = response.fields;
        let data = response.data;

        // Проверяем, есть ли хотя бы одна видимая action
        let hasVisibleActions = data.some(row => (row.actions || []).some(a => a.isVisible));

        let columns = fields.map(f => {
            return {
                data: f.fieldName,
                title: f.displayName,
                visible: f.isVisible,
                orderable: f.isSortable,
                render: function (data, type, row) {
                    if (!data) return "";

                    // Ссылка
                    if (f.linkTemplate) {
                        let link = f.linkTemplate.replace("{id}", row.id);
                        return `<a href="${link}">${data}</a>`;
                    }

                    // Форматирование
                    if (f.format) {
                        let [typeFormat, formatString] = f.format.split(":");
                        if (typeFormat === "date") {
                            let d = new Date(data);
                            if (isNaN(d)) return data;
                            switch (formatString) {
                                case "MM/dd/yyyy":
                                    return (d.getMonth() + 1).toString().padStart(2, "0") + "/" +
                                        d.getDate().toString().padStart(2, "0") + "/" +
                                        d.getFullYear();
                                case "dd.MM.yyyy":
                                    return d.getDate().toString().padStart(2, "0") + "." +
                                        (d.getMonth() + 1).toString().padStart(2, "0") + "." +
                                        d.getFullYear();
                                default:
                                    return d.toLocaleDateString();
                            }
                        }
                        if (typeFormat === "number") {
                            let num = parseFloat(data);
                            if (isNaN(num)) return data;
                            return num.toLocaleString(undefined, { minimumFractionDigits: parseInt(formatString) || 0 });
                        }
                        if (typeFormat === "currency") {
                            let num = parseFloat(data);
                            if (isNaN(num)) return data;
                            return new Intl.NumberFormat("en-US", { style: "currency", currency: formatString || "USD" }).format(num);
                        }
                    }

                    return `<span ${f.attr || ""}>${data}</span>`;
                }
            };
        });

        // Добавляем колонку действий только если есть хотя бы одна видимая action
        if (hasVisibleActions) {
            columns.push({
                data: null,
                title: "Действия",
                orderable: false,
                render: function (data, type, row) {
                    let visibleActions = (row.actions || []).filter(a => a.isVisible);
                    if (visibleActions.length === 0) return "";

                    let items = visibleActions.map(a => {
                        let url = a.urlTemplate.replace("{id}", row.id);
                        return `
                            <a class="dropdown-item ${a.cssClass || ""}" href="${url}" ${a.attr || ""}>
                                <i class="${a.icon} me-1"></i> ${a.displayName}
                            </a>`;
                    }).join("");

                    return `
                        <div class="dropdown">
                            <button type="button" class="btn p-0 dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                                <i class="bx bx-dots-vertical-rounded"></i>
                            </button>
                            <div class="dropdown-menu">
                                ${items}
                            </div>
                        </div>`;
                }
            });
        }

        $(`#${tableId}`).DataTable({
            data: data,
            columns: columns
        });
    });
}
