(function () {
    "use strict";

    var overlay = document.getElementById("landingAuth");
    if (!overlay) {
        return;
    }

    var appName = overlay.getAttribute("data-app-name") || "";
    var titles = {
        login: overlay.getAttribute("data-title-login") || "",
        register: overlay.getAttribute("data-title-register") || "",
        forgot: overlay.getAttribute("data-title-forgot") || "",
        "register-ok": overlay.getAttribute("data-title-register-ok") || "",
        "forgot-ok": overlay.getAttribute("data-title-forgot-ok") || "",
        "email-ok": overlay.getAttribute("data-title-email-ok") || ""
    };

    function isOpen() {
        return overlay.classList.contains("is-open");
    }

    function setTitle(panel) {
        var page = titles[panel];
        document.title = page ? page + " - " + appName : appName;
    }

    function syncUrl(panel) {
        try {
            var url = new URL(window.location.href);
            url.pathname = "/";
            if (panel) {
                url.searchParams.set("auth", panel);
            } else {
                url.searchParams.delete("auth");
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
        var panels = overlay.querySelectorAll("[data-auth-panel]");
        var found = false;
        for (var i = 0; i < panels.length; i += 1) {
            var match = panels[i].getAttribute("data-auth-panel") === panel;
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
        window.requestAnimationFrame(function () {
            var input = overlay.querySelector(".landing-auth-panel.is-active input:not([type='hidden']):not([readonly])");
            if (input) {
                input.focus();
            }
        });
    }

    function open(panel) {
        if (!Object.prototype.hasOwnProperty.call(titles, panel)) {
            return;
        }
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-auth-open");
        setPanel(panel);
        syncUrl(panel);
    }

    function close() {
        overlay.classList.remove("is-open");
        overlay.setAttribute("aria-hidden", "true");
        overlay.setAttribute("data-open", "");
        document.body.classList.remove("is-auth-open");
        setTitle("");
        syncUrl(null);
    }

    function togglePassword(toggle) {
        var container = toggle.closest(".form-password-toggle");
        if (!container) {
            return;
        }
        var input = container.querySelector("input");
        var icon = toggle.querySelector("i");
        if (!input || !icon) {
            return;
        }
        var show = input.type === "password";
        input.type = show ? "text" : "password";
        icon.classList.toggle("bx-hide", !show);
        icon.classList.toggle("bx-show", show);
        toggle.setAttribute("aria-label", show ? "Hide password" : "Show password");
    }

    document.addEventListener("click", function (event) {
        var closer = event.target.closest("[data-auth-close]");
        if (closer) {
            event.preventDefault();
            close();
            return;
        }

        var trigger = event.target.closest("[data-auth]");
        if (trigger) {
            var panel = trigger.getAttribute("data-auth");
            if (panel) {
                event.preventDefault();
                open(panel);
                return;
            }
        }

        var passwordToggle = event.target.closest(".password-toggle-icon");
        if (passwordToggle && overlay.contains(passwordToggle)) {
            event.preventDefault();
            togglePassword(passwordToggle);
            return;
        }

        if (isOpen() && event.target === overlay) {
            close();
        }
    });

    document.addEventListener("keydown", function (event) {
        var passwordToggle = event.target.closest(".password-toggle-icon");
        if (passwordToggle && overlay.contains(passwordToggle) && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
            togglePassword(passwordToggle);
            return;
        }

        if (event.key === "Escape" && isOpen()) {
            var info = document.getElementById("landingInfo");
            if (info && info.classList.contains("is-open")) {
                return;
            }
            close();
        }
    });

    var initial = overlay.getAttribute("data-open");
    if (initial && Object.prototype.hasOwnProperty.call(titles, initial)) {
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-auth-open");
        setPanel(initial);
        syncUrl(initial);
    }
})();
