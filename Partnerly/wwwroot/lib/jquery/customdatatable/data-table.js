(function ($) {
    $.fn.CustomDataTable = function (options) {
        const settings = $.extend({
            data: [],
            columns: [],
            pageSize: 10
        }, options);

        return this.each(function () {
            const $table = $(this);
            $table.empty();

            const $card = $('<div class="custom-datatable-card"></div>');
            $table.wrap($card);
            const $cardWrapper = $table.parent();

            // --- Панель кнопок ---
            const $buttonsDiv = $(`
                <div class="custom-datatable-buttons mb-2 d-flex align-items-center">
                    <button class="btn-cancel btn btn-sm me-2" title="Сбросить всё">
                        <i class="bx bx-undo"></i>
                    </button>

                    <div class="dropdown">
                        <button class="btn btn-sm dropdown-toggle btn-excel" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Экспорт в Excel">
                            <i class="bx bxs-file-export"></i>
                        </button>
                        <ul class="dropdown-menu">
                            <li><a class="dropdown-item btn-export-all" href="#">Экспорт всё</a></li>
                            <li><a class="dropdown-item btn-export-selected" href="#">Экспорт выбранные</a></li>
                        </ul>
                    </div>
                </div>
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
                    .click(() => { currentPage = 1; renderTable(); renderPagination(); });

                const $prev = $('<button><i class="bx bx-chevron-left"></i></button>')
                    .prop('disabled', currentPage === 1)
                    .click(() => { currentPage = Math.max(1, currentPage - 1); renderTable(); renderPagination(); });

                $pagination.append($first, $prev);

                // --- Добавляем кнопки с номерами страниц ---
                const maxVisible = 5; // сколько кнопок показывать максимум
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
                    .click(() => { currentPage = Math.min(totalPages, currentPage + 1); renderTable(); renderPagination(); });

                const $last = $('<button><i class="bx bx-chevrons-right"></i></button>')
                    .prop('disabled', currentPage === totalPages)
                    .click(() => { currentPage = totalPages; renderTable(); renderPagination(); });

                $pagination.append($next, $last);

                // --- Добавляем внизу таблицы ---
                $cardWrapper.append($pagination);

                // --- Добавляем текст "Показано X–Y из Z записей" ---
                const start = (currentPage - 1) * pageSize + 1;
                const end = Math.min(currentPage * pageSize, filteredData.length);
                const infoText = `Показано ${start}–${end} из ${filteredData.length} записей`;
                $info.text(infoText);
            }


            // Поиск
            $searchInput.on('input', function () {
                const val = $(this).val().toLowerCase();
                filteredData = settings.data.filter(row => settings.columns.some(col => String(row[col.data] || '').toLowerCase().includes(val)));
                currentPage = 1;
                renderTable();
                renderPagination();
            });

            $clearBtn.on('click', function () {
                $searchInput.val('');
                filteredData = [...settings.data];
                currentPage = 1;
                renderTable();
                renderPagination();
            });

            // Сортировка
            $table.on('click', 'th', function () {
                const col = $(this).data('column');
                const orderable = $(this).data('orderable');
                if (!col || !orderable) return;

                if (sortColumn === col) sortOrder = sortOrder === 'asc' ? 'desc' : 'asc';
                else { sortColumn = col; sortOrder = 'asc'; }

                renderTable();
                renderPagination();
            });

            // --- Кнопки ---
            $buttonsDiv.find('.btn-cancel').on('click', function () {
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
            });

            renderTable();
            renderPagination();
        });
    };
})(jQuery);
