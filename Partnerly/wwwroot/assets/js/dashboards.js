(function () {
    var root = document.querySelector("[data-dashboards]");
    if (!root) {
        return;
    }

    var tabs = Array.prototype.slice.call(root.querySelectorAll("[data-dash-tab]"));
    var panels = Array.prototype.slice.call(root.querySelectorAll("[data-dash-panel]"));

    function show(name) {
        var exists = tabs.some(function (tab) {
            return tab.getAttribute("data-dash-tab") === name;
        });
        var target = exists ? name : "overview";

        tabs.forEach(function (tab) {
            var active = tab.getAttribute("data-dash-tab") === target;
            tab.classList.toggle("is-active", active);
            tab.setAttribute("aria-selected", active ? "true" : "false");
        });

        panels.forEach(function (panel) {
            panel.hidden = panel.getAttribute("data-dash-panel") !== target;
        });

        if (history.replaceState) {
            history.replaceState(null, "", "#" + target);
        }
    }

    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            show(tab.getAttribute("data-dash-tab"));
        });
    });

    root.querySelectorAll("[data-dash-goto]").forEach(function (el) {
        el.addEventListener("click", function () {
            show(el.getAttribute("data-dash-goto"));
        });
    });

    var hash = (location.hash || "#overview").replace("#", "");
    show(hash);
})();
