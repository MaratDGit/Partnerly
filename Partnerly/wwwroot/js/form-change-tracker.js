
//function enableSaveOnChange(formSelector, saveButtonSelector) {
//    let form = document.querySelector(formSelector);
//    if (!form) return;

//    let saveBtn = form.querySelector(saveButtonSelector);
//    if (!saveBtn) return;

//    // Сохраняем исходные значения при загрузке
//    let initialData = {};
//    form.querySelectorAll("input, textarea, select").forEach(el => {
//        if (el.name) {
//            initialData[el.name] = el.value;
//        }
//    });

//    // Проверка на изменения
//    function checkChanges() {
//        let changed = false;
//        form.querySelectorAll("input, textarea, select").forEach(el => {
//            if (el.name && initialData[el.name] !== el.value) {
//                changed = true;
//            }
//        });

//        saveBtn.disabled = !changed;
//    }

//    // Слушаем изменения
//    form.addEventListener("input", checkChanges);
//    form.addEventListener("change", checkChanges);

//    // Сразу выключаем кнопку при старте
//    saveBtn.disabled = true;
//}

//window.markFormChanged = function (formSelector) {
//    let form = document.querySelector(formSelector);
//    if (form) form.dispatchEvent(new Event("change"));
//};

function enableSaveOnChange(formSelector, saveButtonSelector) {
    let form = document.querySelector(formSelector);
    if (!form) return;

    let saveBtn = document.querySelector(saveButtonSelector);
    if (!saveBtn) return;

    // Сохраняем исходные значения при загрузке
    let initialData = {};
    form.querySelectorAll("input, textarea, select").forEach(el => {
        let key = el.name || el.id || el.dataset.id || null; // <-- теперь используем id или data-id
        if (key) initialData[key] = el.type === "checkbox" ? el.checked : el.value;
    });

    function checkChanges() {
        let changed = false;
        form.querySelectorAll("input, textarea, select").forEach(el => {
            let key = el.name || el.id || el.dataset.id || null;
            if (!key) return;

            let current = el.type === "checkbox" ? el.checked : el.value;
            if (initialData[key] !== current) {
                changed = true;
            }
        });

        saveBtn.disabled = !changed;
    }

    form.addEventListener("input", checkChanges);
    form.addEventListener("change", checkChanges);

    // Сразу выключаем кнопку
    saveBtn.disabled = true;
}
