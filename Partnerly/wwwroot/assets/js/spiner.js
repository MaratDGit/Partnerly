$(document).ready(function () {
    // 🌀 Показываем лоадер при AJAX
    $(document).ajaxStart(function () {
        $("#globalLoader").fadeIn(200);
    }).ajaxStop(function () {
        $("#globalLoader").fadeOut(200);
    });

    // 🌀 Показываем лоадер при отправке любой формы
    $("form").on("submit", function () {
        $("#globalLoader").fadeIn(200);
    });

    // 🌀 Показываем лоадер при переходе по ссылке
    $(document).on("click", "a", function (e) {
        const href = $(this).attr("href");

        // Пропускаем:
        if (
            !href ||                                  // нет href
            href.startsWith("#") ||                    // якорные ссылки
            href.startsWith("javascript:") ||          // javascript-ссылки
            $(this).attr("target") === "_blank" ||     // открытие в новой вкладке
            $(this).attr("data-bs-toggle") ||       // bootstrap dropdown, collapse, modal и т.п.
            $(this).attr("data-no-loader")         // наш кастомный атрибут
        ) {
            return; // не показываем лоадер
        }

        $("#globalLoader").fadeIn(200);
    });

    // 🌀 Если используешь SPA-навигацию (например, AJAX переходы) — можно добавить обработчик popstate
    window.addEventListener('beforeunload', function () {
        $("#globalLoader").fadeIn(200);
    });
});
