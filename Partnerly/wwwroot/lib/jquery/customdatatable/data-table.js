(function ($) {
    $.fn.CustomDataTable = function (options) {
        //const settings = $.extend({
        //    data: [],
        //    columns: [],
        //    pageSize: 10
        //}, options);
        const settings = $.extend({
            data: [],
            columns: [],
            pageSize: 10,
            exportAllUrl: null,
            exportSelectedUrl: null
        }, options)

        return this.each(function () {
            const $table = $(this);
            let activeFilters = {}; // хранит последние применённые фильтры
            $table.empty();

            const $card = $('<div class="custom-datatable-card"></div>');
            $table.wrap($card);
            const $cardWrapper = $table.parent();

            // --- Создаём модальное окно фильтров, если его ещё нет ---
            const modalId = `${$table.attr("id")}-filter-modal`;
            if (!$(`#${modalId}`).length) {
                                const modalHtml = `
                    <div class="modal fade" id="${modalId}" tabindex="-1" aria-hidden="true">
                        <div class="modal-dialog modal-lg modal-dialog-scrollable">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h5 class="modal-title">Фильтры</h5>
                                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Закрыть"></button>
                                </div>
                                <div class="modal-body">
                                    <form id="${modalId}-form" class="row g-3"></form>
                                </div>
                                <div class="modal-footer">
                                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                                    <button type="button" class="btn btn-primary btn-apply-filters">Применить</button>
                                </div>
                            </div>
                        </div>
                    </div>`;
                $("body").append(modalHtml);
            }

            // --- Панель кнопок ---
            const $buttonsDiv = $('<div class="custom-datatable-buttons mb-2 d-flex align-items-center"></div>');

            // Кнопка сброса
            $buttonsDiv.append(`
                    <a class="btn-cancel btn btn-sm me-2" title="Сбросить всё">
                        <i class="bx bx-undo"></i>
                    </a>
                `);

            // Кнопка экспорта — добавляем только если есть URL
            if (settings.exportAllUrl || settings.exportSelectedUrl) {
                const $exportDropdown = $(`
                    <div class="dropdown">
                        <a class="btn btn-sm dropdown-toggle btn-excel" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Экспорт в Excel">
                            <i class="bx bxs-file-export"></i>
                        </a>
                        <ul class="dropdown-menu">
                            ${settings.exportAllUrl ? '<li><a class="dropdown-item btn-export-all" href="#">Экспорт всё</a></li>' : ''}
                            ${settings.exportSelectedUrl ? '<li><a class="dropdown-item btn-export-selected" href="#">Экспорт выбранные</a></li>' : ''}
                        </ul>
                    </div>
                `);
                $buttonsDiv.append($exportDropdown);
            }

            // Кнопка фильтров
            $buttonsDiv.append(`
                <a class="btn btn-sm btn-filter" title="Фильтры">
                    <i class="bx bx-filter"></i>
                </a>
            `);
            
            $cardWrapper.prepend($buttonsDiv);

            // --- Поиск ---
            const $searchDiv = $('<div class="custom-datatable-search"><input type="text" placeholder="Поиск..." /><i class="bx bx-x"></i></div>');
            $cardWrapper.prepend($searchDiv);
            const $searchInput = $searchDiv.find('input');
            const $clearBtn = $searchDiv.find('i');

            // --- Информация ---
            const $info = $('<div class="custom-datatable-info"></div>');
            $cardWrapper.append($info);

            // --- Обертка для горизонтального скролла ---
            const $wrapper = $('<div class="custom-datatable-wrapper"></div>');
            $table.wrap($wrapper);

            let filteredData = [...settings.data];
            let currentPage = 1;
            const pageSize = settings.pageSize;

            let sortColumn = settings.columns.find(c => c.orderable)?.data || null;
            let sortOrder = "asc";

            function renderTable() {
                const $thead = $('<thead></thead>');
                const $trHead = $('<tr></tr>');

                settings.columns.forEach(col => {
                    const $th = $(`<th>
                        <span class="th-content">${col.title} ${col.orderable ? '<i class="bx bx-sort"></i>' : ''}</span>
                        <div class="resize-handle"></div>
                    </th>`).data('column', col.data).data('orderable', col.orderable);

                    if (col.data === sortColumn && col.orderable) {
                        const icon = sortOrder === 'asc' ? 'bx-sort-up' : 'bx-sort-down';
                        $th.find('i').attr('class', 'bx ' + icon);
                    }

                    $trHead.append($th);
                });

                $thead.append($trHead);

                // Сортировка
                if (sortColumn) {
                    const colDef = settings.columns.find(c => c.data === sortColumn);
                    if (colDef && colDef.orderable) {
                        filteredData.sort((a, b) => {
                            let v1 = a[sortColumn], v2 = b[sortColumn];
                            if (typeof v1 === 'string') v1 = v1.toLowerCase();
                            if (typeof v2 === 'string') v2 = v2.toLowerCase();
                            if (v1 < v2) return sortOrder === 'asc' ? -1 : 1;
                            if (v1 > v2) return sortOrder === 'asc' ? 1 : -1;
                            return 0;
                        });
                    }
                }

                const $tbody = $('<tbody></tbody>');
                const start = (currentPage - 1) * pageSize;
                const pageData = filteredData.slice(start, start + pageSize);

                pageData.forEach(row => {
                    const $tr = $('<tr></tr>');
                    settings.columns.forEach(col => {
                        let value = row[col.data];
                        if (col.render) value = col.render(value, null, row);
                        $tr.append(`<td>${value}</td>`);
                    });
                    $tbody.append($tr);
                });

                $table.empty().append($thead).append($tbody);

                // Info
                if ($searchInput.val().trim() !== '') {
                    $info.text(`Отфильтровано записей: ${filteredData.length} из ${settings.data.length}`);
                } else {
                    $info.text(`Всего записей: ${settings.data.length}`);
                }

                enableColumnResize();
            }

            function enableColumnResize() {
                $table.find('th .resize-handle').off('mousedown').on('mousedown', function (e) {
                    e.preventDefault();
                    const th = $(this).closest('th');
                    const startX = e.pageX;
                    const startWidth = th.width();

                    $(document).on('mousemove.resize', function (e2) {
                        const newWidth = startWidth + (e2.pageX - startX);
                        th.width(newWidth);
                    });

                    $(document).on('mouseup.resize', function () {
                        $(document).off('.resize');
                    });
                });
            }

            function renderPagination() {
                $cardWrapper.find('.custom-datatable-pagination').remove();
                const totalPages = Math.ceil(filteredData.length / pageSize);
                if (totalPages <= 1) return;

                const $pagination = $('<div class="custom-datatable-pagination"></div>');

                const $first = $('<button><i class="bx bx-chevrons-left"></i></button>')
                    .prop('disabled', currentPage === 1)
                    .click(() => { currentPage = 1; renderTable(); renderPagination(); updateFilterIcons(); });

                const $prev = $('<button><i class="bx bx-chevron-left"></i></button>')
                    .prop('disabled', currentPage === 1)
                    .click(() => { currentPage = Math.max(1, currentPage - 1); renderTable(); renderPagination(); updateFilterIcons(); });

                $pagination.append($first, $prev);

                // --- Добавляем кнопки с номерами страниц ---
                const maxVisible = 3; // сколько кнопок показывать максимум
                let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
                let endPage = Math.min(totalPages, startPage + maxVisible - 1);

                if (endPage - startPage < maxVisible - 1) {
                    startPage = Math.max(1, endPage - maxVisible + 1);
                }

                for (let i = startPage; i <= endPage; i++) {
                    const $btn = $(`<button>${i}</button>`)
                        .addClass(i === currentPage ? 'active' : '')
                        .click(() => {
                            currentPage = i;
                            renderTable();
                            renderPagination();
                        });
                    $pagination.append($btn);
                }

                const $next = $('<button><i class="bx bx-chevron-right"></i></button>')
                    .prop('disabled', currentPage === totalPages)
                    .click(() => { currentPage = Math.min(totalPages, currentPage + 1); renderTable(); renderPagination(); updateFilterIcons(); });

                const $last = $('<button><i class="bx bx-chevrons-right"></i></button>')
                    .prop('disabled', currentPage === totalPages)
                    .click(() => { currentPage = totalPages; renderTable(); renderPagination(); updateFilterIcons(); });

                $pagination.append($next, $last);

                // --- Добавляем внизу таблицы ---
                $cardWrapper.append($pagination);

                // --- Добавляем текст "Показано X–Y из Z записей" ---
                const start = (currentPage - 1) * pageSize + 1;
                const end = Math.min(currentPage * pageSize, filteredData.length);
                const infoText = `Показано ${start}–${end} из ${filteredData.length} записей`;
                $info.text(infoText);
            }

            function updateFilterIcons() {
                $table.find("th").each(function () {
                    const field = $(this).data("column");
                    const $icon = $(this).find(".filter-indicator");

                    if (activeFilters[field]) {
                        if ($icon.length === 0) {
                            $(this).find(".th-content").append(
                                ` <i class="bx bx-filter-alt filter-indicator" style="color:#4e73df;" title="Фильтр активен"></i>`
                            );
                        }
                    } else {
                        $icon.remove();
                    }
                });
            }


            // Поиск
            $searchInput.on('input', function () {
                const val = $(this).val().toLowerCase();
                filteredData = settings.data.filter(row => settings.columns.some(col => String(row[col.data] || '').toLowerCase().includes(val)));
                currentPage = 1;
                renderTable();
                renderPagination();
                updateFilterIcons();
            });

            $clearBtn.on('click', function () {
                $searchInput.val('');
                filteredData = [...settings.data];
                currentPage = 1;
                renderTable();
                renderPagination();
                updateFilterIcons();
            });

            // Сортировка
            $table.on('click', 'th', function (e) {
                // если клик был по input (checkbox), выходим
                if ($(e.target).is('input')) return;

                const col = $(this).data('column');
                const orderable = $(this).data('orderable');
                if (!col || !orderable) return;

                if (sortColumn === col) sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
                else { sortColumn = col; sortOrder = 'asc'; }

                renderTable();
                renderPagination();
                updateFilterIcons();
            });

            // --- Кнопка фильтров ---
            $buttonsDiv.find(".btn-filter").on("click", function () {
                const modal = $(`#${modalId}`);
                const $form = modal.find("form");
                $form.empty();

                settings.columns.forEach(col => {
                    if (!col.data || col.data === "select" || col.title === "Действия" || !col.isFilterable) return;

                    const uniqueValues = [...new Set(settings.data.map(r => r[col.data]))].filter(v => v != null && v !== "");

                    let inputHtml = "";

                    if (col.dataType === "date" || col.title.toLowerCase().includes("дата")) {
                                    inputHtml = `
                            <div class="col-md-6">
                                <label>${col.title}</label>
                                <div class="input-group">
                                    <input type="date" class="form-control filter-input" placeholder="От" data-field="${col.data}" data-type="date-from">
                                    <span class="input-group-text">–</span>
                                    <input type="date" class="form-control filter-input" placeholder="До" data-field="${col.data}" data-type="date-to">
                                </div>
                            </div>
                        `;
                    } else if (typeof uniqueValues[0] === "boolean" || col.type === "checkbox") {
                                    inputHtml = `
                            <div class="col-md-6">
                                <label>${col.title}</label>
                                <select class="form-select filter-input" data-field="${col.data}">
                                    <option value="">Все</option>
                                    <option value="true">Да</option>
                                    <option value="false">Нет</option>
                                </select>
                            </div>
                        `;
                    } else {
                        inputHtml = `
                <div class="col-md-6">
                    <label>${col.title}</label>
                    <select class="form-select filter-input" data-field="${col.data}">
                        <option value="">Все</option>
                        ${uniqueValues.map(v => `<option value="${v}">${v}</option>`).join("")}
                    </select>
                </div>
            `;
                    }

                    $form.append(inputHtml);
                });

                // --- Восстанавливаем сохранённые фильтры ---
                Object.entries(activeFilters).forEach(([field, f]) => {
                    if (f.value !== undefined) {
                        $form.find(`[data-field="${field}"]`).val(f.value);
                    }
                    if (f.from) {
                        $form.find(`[data-field="${field}"][data-type="date-from"]`).val(f.from);
                    }
                    if (f.to) {
                        $form.find(`[data-field="${field}"][data-type="date-to"]`).val(f.to);
                    }
                });

                // --- Ограничение для date полей ---
                modal.find('.filter-input[data-type="date-from"]').on('input', function () {
                    const fromDate = $(this).val();
                    const field = $(this).data('field');
                    const $to = modal.find(`.filter-input[data-field="${field}"][data-type="date-to"]`);
                    if (fromDate) {
                        $to.attr('min', fromDate);
                    } else {
                        $to.removeAttr('min');
                    }
                });

                modal.find('.filter-input[data-type="date-to"]').on('input', function () {
                    const toDate = $(this).val();
                    const field = $(this).data('field');
                    const $from = modal.find(`.filter-input[data-field="${field}"][data-type="date-from"]`);
                    if (toDate) {
                        $from.attr('max', toDate);
                    } else {
                        $from.removeAttr('max');
                    }
                });

                modal.modal("show");
            });

            // --- Применить фильтры ---
            $(document).on("click", `#${modalId} .btn-apply-filters`, function () {
                const modal = $(`#${modalId}`);
                activeFilters = {}; // очищаем старые

                modal.find(".filter-input").each(function () {
                    const field = $(this).data("field");
                    const type = $(this).data("type");
                    const value = $(this).val();

                    if (value) {
                        if (!activeFilters[field]) activeFilters[field] = {};
                        if (type === "date-from") activeFilters[field].from = value;
                        else if (type === "date-to") activeFilters[field].to = value;
                        else activeFilters[field].value = value;
                    }
                });

                // фильтрация данных
                filteredData = settings.data.filter(row => {
                    return Object.keys(activeFilters).every(field => {
                        const f = activeFilters[field];
                        const val = row[field];

                        if (f.value !== undefined && f.value !== "") {
                            return String(val) === f.value;
                        }

                        if (f.from || f.to) {
                            const d = new Date(val);
                            if (f.from && d < new Date(f.from)) return false;
                            if (f.to && d > new Date(f.to)) return false;
                        }

                        return true;
                    });
                });

                currentPage = 1;
                renderTable();
                renderPagination();
                updateFilterIcons();
                modal.modal("hide");
            });

            // --- Кнопки ---
            $buttonsDiv.find('.btn-cancel').on('click', function () {
                activeFilters = {}; // очищаем все фильтры
                filteredData = [...settings.data];
                currentPage = 1;
                renderTable();
                renderPagination();
                updateFilterIcons();
                $(document).trigger("datatable:reset", { tableId: $table.attr("id") });
            });

            $buttonsDiv.find('.btn-export-all').on('click', function (e) {
                e.preventDefault();
                $(document).trigger("datatable:exportAll", { tableId: $table.attr("id") });
            });

            $buttonsDiv.find('.btn-export-selected').on('click', function (e) {
                e.preventDefault();
                $(document).trigger("datatable:exportSelected", { tableId: $table.attr("id") });
            });

            // --- Обработчик reload из initUniqueDataTable ---
            $table.on("reload", function () {
                filteredData = [...settings.data];
                currentPage = 1;
                sortColumn = settings.columns.find(c => c.orderable)?.data || null;
                sortOrder = "asc";
                renderTable();
                renderPagination();
                updateFilterIcons();
            });

            renderTable();
            renderPagination();
            updateFilterIcons();
        });
    };
})(jQuery);
