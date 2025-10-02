
function enableSaveOnChange(formSelector, saveButtonSelector) {
    let form = document.querySelector(formSelector);
    if (!form) return;

    let saveBtn = form.querySelector(saveButtonSelector);
    if (!saveBtn) return;

    // Сохраняем исходные значения при загрузке
    let initialData = {};
    form.querySelectorAll("input, textarea, select").forEach(el => {
        if (el.name) {
            initialData[el.name] = el.value;
        }
    });

    // Проверка на изменения
    function checkChanges() {
        let changed = false;
        form.querySelectorAll("input, textarea, select").forEach(el => {
            if (el.name && initialData[el.name] !== el.value) {
                changed = true;
            }
        });

        saveBtn.disabled = !changed;
    }

    // Слушаем изменения
    form.addEventListener("input", checkChanges);
    form.addEventListener("change", checkChanges);

    // Сразу выключаем кнопку при старте
    saveBtn.disabled = true;
}
