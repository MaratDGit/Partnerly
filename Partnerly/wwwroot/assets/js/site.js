
(function () {
    "use strict";

    function togglePassword(toggle) {
        const container = toggle.closest(".form-password-toggle");

        if (!container) return;

        const input = container.querySelector("input");
        const icon = toggle.querySelector("i");

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

    document.addEventListener("click", function (event) {
        const toggle = event.target.closest(".password-toggle-icon");

        if (!toggle) return;

        togglePassword(toggle);
    });

    document.addEventListener("keydown", function (event) {
        const toggle = event.target.closest(".password-toggle-icon");

        if (!toggle) return;

        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            togglePassword(toggle);
        }
    });

})();
