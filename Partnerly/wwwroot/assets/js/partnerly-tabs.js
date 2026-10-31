(function () {
    "use strict";

    document.addEventListener("shown.bs.tab", function (event) {
        var tabs = event.target && event.target.closest
            ? event.target.closest(".partnerly-tabs")
            : null;
        if (!tabs) {
            return;
        }
        window.dispatchEvent(new Event("resize"));
    });
})();
