(function () {
    "use strict";

    var overlay = document.getElementById("landingInfo");
    if (!overlay) {
        return;
    }

    var appName = "";
    var titles = {};

    function readTitles() {
        var auth = document.getElementById("landingAuth");
        appName = auth ? auth.getAttribute("data-app-name") || "" : "";
        titles = {
            privacy: overlay.getAttribute("data-title-privacy") || ""
        };
    }

    readTitles();

    function isOpen() {
        return overlay.classList.contains("is-open");
    }

    function setTitle(panel) {
        var page = titles[panel];
        if (page) {
            document.title = page + (appName ? " - " + appName : "");
        }
    }

    function restoreAuthTitle() {
        var auth = document.getElementById("landingAuth");
        if (auth && auth.classList.contains("is-open")) {
            var panel = auth.getAttribute("data-open") || "login";
            var title = auth.getAttribute("data-title-" + panel);
            if (title) {
                document.title = title + (appName ? " - " + appName : "");
                return;
            }
        }
        document.title = appName || document.title;
    }

    function syncUrl(openPrivacy) {
        try {
            var url = new URL(window.location.href);
            url.pathname = "/";
            if (openPrivacy) {
                url.searchParams.set("dialog", "privacy");
            } else {
                url.searchParams.delete("dialog");
            }
            var next = url.pathname + url.search + url.hash;
            if (next !== window.location.pathname + window.location.search + window.location.hash) {
                history.replaceState(null, "", next);
            }
        } catch (error) {
            // Keep the current address if the URL cannot be updated.
        }
    }

    function setPanel(panel) {
        var panels = overlay.querySelectorAll("[data-info-panel]");
        var found = false;
        for (var i = 0; i < panels.length; i += 1) {
            var match = panels[i].getAttribute("data-info-panel") === panel;
            panels[i].classList.toggle("is-active", match);
            if (match) {
                found = true;
            }
        }
        if (!found) {
            return;
        }
        overlay.setAttribute("data-open", panel);
        setTitle(panel);
    }

    function open(panel) {
        if (!Object.prototype.hasOwnProperty.call(titles, panel)) {
            return;
        }
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-info-open");
        setPanel(panel);
        syncUrl(panel === "privacy");
    }

    function close() {
        overlay.classList.remove("is-open");
        overlay.setAttribute("aria-hidden", "true");
        overlay.setAttribute("data-open", "");
        document.body.classList.remove("is-info-open");
        restoreAuthTitle();
        syncUrl(false);
    }

    document.addEventListener("click", function (event) {
        var closer = event.target.closest("[data-info-close]");
        if (closer) {
            event.preventDefault();
            close();
            return;
        }

        var trigger = event.target.closest("[data-info]");
        if (trigger) {
            var panel = trigger.getAttribute("data-info");
            if (panel) {
                event.preventDefault();
                open(panel);
            }
        }

        if (isOpen() && event.target === overlay) {
            close();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && isOpen()) {
            event.preventDefault();
            close();
        }
    });

    window.partnerlyLandingInfo = {
        refresh: function () {
            readTitles();
            if (isOpen()) {
                setTitle(overlay.getAttribute("data-open") || "privacy");
            }
        }
    };

    var initial = overlay.getAttribute("data-open");
    if (initial && Object.prototype.hasOwnProperty.call(titles, initial)) {
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-info-open");
        setPanel(initial);
        syncUrl(initial === "privacy");
    }
})();
