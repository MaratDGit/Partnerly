
document.addEventListener("DOMContentLoaded", () => {
    initLookupModal();

    document.querySelectorAll(".lookup-icon").forEach(icon => {
        icon.addEventListener("click", (e) => {
            const input = icon.parentElement.querySelector("input[data-lookup='true']");
            if (input) openLookupModal(input);
            e.stopPropagation();
        });
    });
});

function initLookupModal() {
    if (document.getElementById("lookupModal")) return; // уже есть   

    const modalHtml = `
    <div id="lookupModal" class="lookup-modal" style="display:none;">
        <div class="lookup-content">
            <div class="lookup-header">
                <h5 id="lookupTitle">${lookupLocalization.selectRecord}</h5>
                <button id="lookupClose" class="lookup-close">&times;</button>
            </div>
            <div class="lookup-body">
                <input type="text" id="lookupSearch" placeholder="${lookupLocalization.search}" class="form-control mb-2" />
                <div class="table-container">
                    <table id="lookupTable" class="table table-hover">
                        <thead><tr id="lookupHead"></tr></thead>
                        <tbody id="lookupBody"></tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>`;

    document.body.insertAdjacentHTML("beforeend", modalHtml);

    // Обработчики закрытия
    document.getElementById("lookupClose").addEventListener("click", closeLookupModal);
    document.getElementById("lookupModal").addEventListener("click", e => {
        if (e.target.id === "lookupModal") closeLookupModal();
    });

    // Поиск по таблице
    document.getElementById("lookupSearch").addEventListener("input", (e) => {
        const value = e.target.value.toLowerCase();
        const rows = document.querySelectorAll("#lookupBody tr");
        rows.forEach(r => {
            r.style.display = r.innerText.toLowerCase().includes(value) ? "" : "none";
        });
    });
}

function openLookupModal(input) {
    const modal = document.getElementById("lookupModal");
    const tbody = document.getElementById("lookupBody");
    const thead = document.getElementById("lookupHead");
    const title = document.getElementById("lookupTitle");

    tbody.innerHTML = `<tr><td colspan='99'>${lookupLocalization.loading}</td></tr>`;
    thead.innerHTML = "";

    title.textContent = input.dataset.lookupTitle || lookupLocalization.selectRecord;

    let url = input.dataset.lookupSource;

    // ======== ВСТАВКА: добавляем параметр fieldName ========
    const paramName = input.dataset.lookupParam;           // "fieldName"
    const paramValue = input.dataset.lookupParamValue;     // "Name"
    if (paramName && paramValue) {
        url += `?${encodeURIComponent(paramName)}=${encodeURIComponent(paramValue)}`;
    }
    // =========================================================

    fetch(url)
        .then(res => res.json())
        .then(data => {
            if (!Array.isArray(data) || data.length === 0) {
                tbody.innerHTML = `<tr><td colspan='99' class='text-muted'>${lookupLocalization.noData}</td></tr>`;
                return;
            }

            const columnNamesAttr = input.dataset.lookupColumnNames
                ? input.dataset.lookupColumnNames.split(",").reduce((acc, pair) => {
                    const [key, label] = pair.split(":").map(s => s.trim());
                    if (key && label) acc[key] = label;
                    return acc;
                }, {})
                : {};

            const columns = Object.keys(columnNamesAttr);

            columns.forEach(col => {
                const th = document.createElement("th");
                th.textContent = columnNamesAttr[col];
                thead.appendChild(th);
            });

            tbody.innerHTML = "";
            data.forEach(item => {
                const tr = document.createElement("tr");
                /*tr.innerHTML = columns.map(col => `<td>${item[col] ?? ""}</td>`).join("");*/

                tr.innerHTML = columns.map(col => {
                    const value = item[col];

                    // если значение булево — отрисовать checkbox
                    if (typeof value === "boolean") {
                        return `<td class="text-center">
                    <input type="checkbox" disabled ${value ? "checked" : ""}>
                </td>`;
                    }

                    // если это "true"/"false" в виде строки — тоже checkbox
                    if (value === "true" || value === "false") {
                        return `<td class="text-center">
                    <input type="checkbox" disabled ${value === "true" ? "checked" : ""}>
                </td>`;
                    }

                    // обычное значение
                    return `<td>${truncateText(value) ?? ""}</td>`;
                }).join("");

                tr.dataset.itemId = item.id;
                tr.addEventListener("click", () => handleLookupSelection(item, input));
                tbody.appendChild(tr);
            });
        })
        .catch(() => {
            tbody.innerHTML = `<tr><td colspan="99" class="text-danger">${lookupLocalization.loadError}</td></tr>`;
        });

    modal.style.display = "flex";
}

function handleLookupSelection(item, input) {
    closeLookupModal();

    const editUrlBase = input.dataset.editUrlBase;

    if (editUrlBase && editUrlBase.trim() !== "") {
        window.location.href = editUrlBase + item.id;
        return;
    }

    // 1️⃣ Определяем поле отображения
    const displayField = input.dataset.lookupDisplayField || "name";
    const displayValue = item[displayField] ?? item.name ?? Object.values(item)[0];
    input.value = displayValue ?? "";

    // 2️⃣ Обновляем скрытое поле (если есть)
    const targetIdFieldId = input.dataset.lookupTargetId;
    if (targetIdFieldId) {
        const hiddenInput = document.getElementById(targetIdFieldId);
        if (hiddenInput) {
            hiddenInput.value = item.id ?? "";
            hiddenInput.dispatchEvent(new Event("change", { bubbles: true })); // 🔥 ключевая строка
        }
    }

    // 3️⃣ Сохраняем ID в dataset
    input.dataset.selectedId = item.id ?? "";

    // 4️⃣ Триггерим событие для самого lookup-поля
    input.dispatchEvent(new Event("change", { bubbles: true }));
}

function closeLookupModal() {
    document.getElementById("lookupModal").style.display = "none";
}

function truncateText(value, maxLength = 25) {
    if (value == null) return "";
    const text = String(value);
    return text.length > maxLength
        ? text.substring(0, maxLength) + "..."
        : text;
}


/**
 * Opens the existing Lookup modal for DataTable.
 * Reuses #lookupModal and the existing Lookup CSS.
 */
function openDataTableLookup(options) {
    initLookupModal();

    const modal = document.getElementById("lookupModal");
    const title = document.getElementById("lookupTitle");
    const search = document.getElementById("lookupSearch");
    const thead = document.getElementById("lookupHead");
    const tbody = document.getElementById("lookupBody");

    if (!modal || !title || !search || !thead || !tbody) {
        console.error("Lookup modal elements were not found.");
        return;
    }

    const columns = options.columns || [];

    title.textContent = options.title || lookupLocalization.selectRecord;
    search.placeholder =
        options.searchPlaceholder || lookupLocalization.search;
    search.value = "";

    thead.replaceChildren();
    tbody.replaceChildren();

    columns.forEach(function (column) {
        const th = document.createElement("th");
        th.textContent = column.title || column.field;

        if (column.width) {
            th.style.width = column.width;
        }

        thead.appendChild(th);
    });

    modal.style.display = "flex";

    const loadingRow = document.createElement("tr");
    const loadingCell = document.createElement("td");
    loadingCell.colSpan = Math.max(columns.length, 1);
    loadingCell.textContent = lookupLocalization.loading;
    loadingRow.appendChild(loadingCell);
    tbody.appendChild(loadingRow);

    let records = [];

    function renderRows(items) {
        tbody.replaceChildren();

        if (!items.length) {
            const tr = document.createElement("tr");
            const td = document.createElement("td");

            td.colSpan = Math.max(columns.length, 1);
            td.className = "text-muted";
            td.textContent = lookupLocalization.noData;

            tr.appendChild(td);
            tbody.appendChild(tr);
            return;
        }

        items.forEach(function (item) {
            const tr = document.createElement("tr");

            columns.forEach(function (column) {
                const td = document.createElement("td");
                const value = item[column.field];

                if (
                    column.displayAsCheckbox ||
                    typeof value === "boolean" ||
                    value === "true" ||
                    value === "false"
                ) {
                    const checkbox = document.createElement("input");
                    checkbox.type = "checkbox";
                    checkbox.disabled = true;
                    checkbox.checked = value === true || value === "true";

                    td.classList.add("text-center");
                    td.appendChild(checkbox);
                } else {
                    const text = value == null ? "" : String(value);
                    td.textContent = text;
                    td.title = text;
                }

                tr.appendChild(td);
            });

            tr.addEventListener("click", function () {
                modal.style.display = "none";

                if (typeof options.onSelect === "function") {
                    options.onSelect(item);
                }
            });

            tbody.appendChild(tr);
        });
    }

    function filterRows() {
        const term = search.value.trim().toLowerCase();

        renderRows(records.filter(function (item) {
            return columns.some(function (column) {
                const value = item[column.field];

                return value != null &&
                    String(value).toLowerCase().includes(term);
            });
        }));
    }

    // Replace the existing search handler while this Lookup is active.
    search.oninput = filterRows;

    const url = new URL(options.url, window.location.origin);

    Object.entries(options.parameters || {}).forEach(
        function ([key, value]) {
            if (value != null && value !== "") {
                url.searchParams.set(key, value);
            }
        }
    );

    fetch(url.toString(), {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    })
        .then(function (response) {
            if (!response.ok) {
                throw new Error("Lookup request failed.");
            }

            return response.json();
        })
        .then(function (data) {
            if (!Array.isArray(data)) {
                throw new Error("Lookup response must be an array.");
            }

            records = data;
            filterRows();
        })
        .catch(function (error) {
            console.error(error);

            tbody.replaceChildren();

            const tr = document.createElement("tr");
            const td = document.createElement("td");

            td.colSpan = Math.max(columns.length, 1);
            td.className = "text-danger";
            td.textContent = lookupLocalization.loadError;

            tr.appendChild(td);
            tbody.appendChild(tr);
        });
}
