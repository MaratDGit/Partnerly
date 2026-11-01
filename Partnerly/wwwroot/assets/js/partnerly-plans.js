(function () {
    "use strict";

    var overlay = document.getElementById("partnerlyPlans");
    if (!overlay) {
        return;
    }

    function isOpen() {
        return overlay.classList.contains("is-open");
    }

    function open() {
        overlay.hidden = false;
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-plans-open");
    }

    function close() {
        overlay.classList.remove("is-open");
        overlay.setAttribute("aria-hidden", "true");
        overlay.hidden = true;
        document.body.classList.remove("is-plans-open");
    }

    function stopBackgroundScroll(event) {
        if (!isOpen()) {
            return;
        }

        var card = overlay.querySelector(".partnerly-plans-card");
        if (card && card.contains(event.target)) {
            var delta = event.deltaY || 0;
            var atTop = card.scrollTop <= 0 && delta < 0;
            var atBottom = card.scrollTop + card.clientHeight >= card.scrollHeight - 1 && delta > 0;
            if (!atTop && !atBottom) {
                return;
            }
        }

        event.preventDefault();
    }

    overlay.addEventListener("wheel", stopBackgroundScroll, { passive: false });
    overlay.addEventListener("touchmove", stopBackgroundScroll, { passive: false });

    document.addEventListener("click", function (event) {
        var opener = event.target.closest("[data-plans-open]");
        if (opener) {
            event.preventDefault();
            open();
            return;
        }

        if (event.target.closest("[data-plans-close]") || event.target === overlay) {
            event.preventDefault();
            close();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && isOpen()) {
            event.preventDefault();
            close();
        }
    });
})();
