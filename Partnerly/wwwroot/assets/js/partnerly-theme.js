
(function () {
    "use strict";

    const STORAGE_KEY = "partnerly-theme";
    const VALID_THEMES = ["light", "dark", "system"];

    const root = document.documentElement;

    const systemTheme = window.matchMedia(
        "(prefers-color-scheme: dark)"
    );

    function getElements() {
        return {
            button: document.getElementById("partnerlyThemeButton"),
            icon: document.getElementById("partnerlyThemeIcon"),
            options: document.querySelectorAll("[data-theme-value]")
        };
    }

    function getSavedTheme() {
        try {
            const saved = localStorage.getItem(STORAGE_KEY);

            return VALID_THEMES.includes(saved)
                ? saved
                : "system";
        } catch {
            return "system";
        }
    }

    function getEffectiveTheme(theme) {
        if (theme === "system") {
            return systemTheme.matches ? "dark" : "light";
        }

        return theme;
    }

    function applyTheme(theme) {
        const effectiveTheme = getEffectiveTheme(theme);
        const elements = getElements();

        // Theme used by Partnerly CSS.
        root.setAttribute("data-theme", effectiveTheme);

        // User preference: light, dark or system.
        root.setAttribute("data-theme-preference", theme);

        // Bootstrap 5.3+ color mode.
        root.setAttribute("data-bs-theme", effectiveTheme);

        // Keep the existing layout/template classes intact.
        root.classList.toggle(
            "partnerly-theme-dark",
            effectiveTheme === "dark"
        );

        root.classList.toggle(
            "partnerly-theme-light",
            effectiveTheme === "light"
        );

        if (elements.icon) {
            elements.icon.className =
                effectiveTheme === "dark"
                    ? "bx bx-moon"
                    : "bx bx-sun";
        }

        if (elements.button) {
            elements.button.setAttribute(
                "aria-label",
                `Current theme: ${theme}. Change theme`
            );

            elements.button.setAttribute(
                "title",
                `Theme: ${theme}`
            );
        }

        elements.options.forEach(function (option) {
            const selected =
                option.dataset.themeValue === theme;

            option.classList.toggle("active", selected);

            option.setAttribute(
                "aria-pressed",
                String(selected)
            );

            if (selected) {
                option.setAttribute("aria-current", "true");
            } else {
                option.removeAttribute("aria-current");
            }
        });
    }

    function saveTheme(theme) {
        if (!VALID_THEMES.includes(theme)) {
            return;
        }

        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch {
            // Theme switching still works without localStorage.
        }

        applyTheme(theme);
    }

    function initializeThemeSwitcher() {
        const elements = getElements();

        elements.options.forEach(function (option) {
            option.addEventListener("click", function () {
                saveTheme(this.dataset.themeValue);
            });
        });

        applyTheme(getSavedTheme());
    }

    // React to OS theme changes only when System is selected.
    function handleSystemThemeChange() {
        if (getSavedTheme() === "system") {
            applyTheme("system");
        }
    }

    if (typeof systemTheme.addEventListener === "function") {
        systemTheme.addEventListener(
            "change",
            handleSystemThemeChange
        );
    } else {
        // Compatibility with older browsers.
        systemTheme.addListener(handleSystemThemeChange);
    }

    // Apply the saved theme immediately, then bind the controls.
    applyTheme(getSavedTheme());

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initializeThemeSwitcher,
            { once: true }
        );
    } else {
        initializeThemeSwitcher();
    }
})();
