document.addEventListener("DOMContentLoaded", function () {
    var root = document.querySelector("[data-dashboards]");
    if (!root) {
        return;
    }

    var tabs = Array.prototype.slice.call(root.querySelectorAll("[data-dash-tab]"));
    var panels = Array.prototype.slice.call(root.querySelectorAll("[data-dash-panel]"));

    function firstTab() {
        return tabs.length
            ? tabs[0].getAttribute("data-dash-tab")
            : "";
    }

    function show(name) {
        var exists = tabs.some(function (tab) {
            return tab.getAttribute("data-dash-tab") === name;
        });
        var target = exists ? name : firstTab();

        if (!target) {
            return;
        }

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

        if (target === "games") {
            window.dispatchEvent(new Event("partnerly-layout-change"));
        }
    }

    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            show(tab.getAttribute("data-dash-tab"));
        });
    });

    var hash = (location.hash || "").replace("#", "");
    show(hash);
});
