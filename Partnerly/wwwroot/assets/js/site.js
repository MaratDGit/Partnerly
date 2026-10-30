
(function () {
    "use strict";

    function togglePassword(toggle) {
        const container = toggle.closest(".form-password-toggle");

        if (!container) return;

        const input = container.querySelector("input:not([type='hidden'])");
        const icon = toggle.querySelector("i") || (toggle.tagName === "I" ? toggle : null);

        if (!input || !icon) return;

        const isPassword = input.type === "password";

        input.type = isPassword ? "text" : "password";

        icon.classList.toggle("bx-hide", !isPassword);
        icon.classList.toggle("bx-show", isPassword);

        toggle.setAttribute(
            "aria-label",
            isPassword ? "Hide password" : "Show password"
        );
    }

    function findToggle(event) {
        return event.target.closest(".password-toggle-icon") ||
            event.target.closest(".form-password-toggle .input-group-text");
    }

    document.addEventListener("click", function (event) {
        const toggle = findToggle(event);

        if (!toggle) return;

        togglePassword(toggle);
    });

    document.addEventListener("keydown", function (event) {
        const toggle = findToggle(event);

        if (!toggle) return;

        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            togglePassword(toggle);
        }
    });

})();
