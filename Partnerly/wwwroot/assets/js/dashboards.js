document.addEventListener("DOMContentLoaded", function () {
    var root = document.querySelector("[data-dashboards]");
    if (!root) {
        return;
    }

    var tabs = Array.prototype.slice.call(root.querySelectorAll("[data-dash-tab]"));
    var panels = Array.prototype.slice.call(root.querySelectorAll("[data-dash-panel]"));
    var nav = root.querySelector(".dashboards-tabs");
    var indicator = null;
    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function firstTab() {
        return tabs.length
            ? tabs[0].getAttribute("data-dash-tab")
            : "";
    }

    function ensureIndicator() {
        if (!nav || indicator) {
            return indicator;
        }

        indicator = nav.querySelector(".dashboards-tab-indicator");
        if (!indicator) {
            indicator = document.createElement("span");
            indicator.className = "dashboards-tab-indicator";
            indicator.setAttribute("aria-hidden", "true");
            nav.insertBefore(indicator, nav.firstChild);
        }

        return indicator;
    }

    function moveIndicator(tab, instant) {
        if (!nav || !tab) {
            return;
        }

        var mark = ensureIndicator();
        if (!mark) {
            return;
        }

        if (instant || reduceMotion) {
            mark.style.transition = "none";
        }

        mark.style.width = tab.offsetWidth + "px";
        mark.style.height = tab.offsetHeight + "px";
        mark.style.transform = "translate(" + tab.offsetLeft + "px, " + tab.offsetTop + "px)";
        nav.classList.add("has-indicator");

        if (instant || reduceMotion) {
            void mark.offsetWidth;
            mark.style.transition = "";
        }
    }

    function show(name, instant) {
        var exists = tabs.some(function (tab) {
            return tab.getAttribute("data-dash-tab") === name;
        });
        var target = exists ? name : firstTab();
        var activeTab = null;

        if (!target) {
            return;
        }

        tabs.forEach(function (tab) {
            var active = tab.getAttribute("data-dash-tab") === target;
            tab.classList.toggle("is-active", active);
            tab.setAttribute("aria-selected", active ? "true" : "false");
            if (active) {
                activeTab = tab;
            }
        });

        panels.forEach(function (panel) {
            var on = panel.getAttribute("data-dash-panel") === target;
            panel.hidden = !on;
            panel.classList.toggle("is-active", on);
            panel.classList.toggle("is-animating", on && !instant && !reduceMotion);
        });

        moveIndicator(activeTab, instant);

        if (history.replaceState) {
            history.replaceState(null, "", "#" + target);
        }

        if (target === "games") {
            window.dispatchEvent(new Event("partnerly-layout-change"));
        }
    }

    tabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            show(tab.getAttribute("data-dash-tab"), false);
        });
    });

    window.addEventListener("resize", function () {
        var active = nav && nav.querySelector(".dashboards-tab.is-active");
        if (active) {
            moveIndicator(active, true);
        }
    });

    var hash = (location.hash || "").replace("#", "");
    show(hash, true);
});
