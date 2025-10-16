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
                <h5 id="lookupTitle">Выберите запись</h5>
                <button id="lookupClose" class="lookup-close">&times;</button>
            </div>
            <div class="lookup-body">
                <input type="text" id="lookupSearch" placeholder="Поиск..." class="form-control mb-2" />
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

    tbody.innerHTML = "<tr><td colspan='99'>Загрузка...</td></tr>";
    thead.innerHTML = "";

    title.textContent = input.dataset.lookupTitle || "Выберите запись";

    let url = input.dataset.lookupSource;

    // ======== ВСТАВКА: добавляем параметр fieldName ========
    const paramName = input.dataset.lookupParam;           // например: "fieldName"
    const paramValue = input.dataset.lookupParamValue;     // например: "Name"
    if (paramName && paramValue) {
        url += `?${encodeURIComponent(paramName)}=${encodeURIComponent(paramValue)}`;
    }
    // =========================================================

    fetch(url)
        .then(res => res.json())
        .then(data => {
            if (!Array.isArray(data) || data.length === 0) {
                tbody.innerHTML = "<tr><td colspan='99' class='text-muted'>Нет данных</td></tr>";
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
                tr.innerHTML = columns.map(col => `<td>${item[col] ?? ""}</td>`).join("");
                tr.dataset.itemId = item.id;
                tr.addEventListener("click", () => handleLookupSelection(item, input));
                tbody.appendChild(tr);
            });
        })
        .catch(() => {
            tbody.innerHTML = "<tr><td colspan='99' class='text-danger'>Ошибка загрузки</td></tr>";
        });

    modal.style.display = "flex";
}

function handleLookupSelection(item, input) {
    closeLookupModal();

    const editUrlBase = input.dataset.editUrlBase;

    if (editUrlBase) {
        // Переход на страницу редактирования
        window.location.href = editUrlBase + item.id;
    } else {
        // Вставляем значение в поле
        const displayField = input.dataset.lookupDisplayField || "name";
        input.value = item[displayField] ?? Object.values(item)[0];
        input.dataset.selectedId = item.id ?? "";
    }
}

function closeLookupModal() {
    document.getElementById("lookupModal").style.display = "none";
}
