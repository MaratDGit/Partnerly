//function initUniqueDataTable(tableId, ajaxUrl, columnDefs) {
//    return $(`#${tableId}`).DataTable({
//        ajax: {
//            url: ajaxUrl,
//            type: "GET"
//        },
//        columns: columnDefs
//    });
//}

function initUniqueDataTable(tableId, ajaxUrl, columnDefs, options = {}) {
    const config = {
        showActions: options.showActions ?? false, // включать колонку действий или нет
        columns: columnDefs
    };

    const columns = [...config.columns];

    // Если нужно добавить колонку действий
    if (config.showActions) {
        columns.push({
            data: "id",
            orderable: false,
            searchable: false,
            render: function (data, type, row) {
                // row.actions — массив объектов { label, href }
                if (!row.actions || row.actions.length === 0) return "";
                let html = `<div class="dropdown">
                                <button type="button" class="btn p-0 dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                                    <i class="bx bx-dots-vertical-rounded"></i>
                                </button>
                                <div class="dropdown-menu">`;
                row.actions.forEach(a => {
                    html += `<a class="dropdown-item" href="${a.href}">${a.label}</a>`;
                });
                html += `</div></div>`;
                return html;
            }
        });
    }

    return $(`#${tableId}`).DataTable({
        ajax: {
            url: ajaxUrl,
            type: "GET" // или POST
        },
        columns: columns
    });
}
