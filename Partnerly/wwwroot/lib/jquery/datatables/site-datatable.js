function initDataTable(tableId, ajaxUrl, columns, options = {}) {
    return $(`#${tableId}`).DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: ajaxUrl,
            type: "POST"
        },
        columns: columns,
    });
}