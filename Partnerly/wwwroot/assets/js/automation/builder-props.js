(function () {
    "use strict";

    var panel = document.querySelector("[data-props-panel]");
    var head = panel && panel.querySelector(".automation-props-head");
    var wrap = panel && panel.closest(".automation-canvas-wrap");
    if (!panel || !head || !wrap) {
        return;
    }

    var KEY = "partnerly.builder.propsPos";
    var drag = null;

    function clamp(left, top) {
        var maxLeft = Math.max(0, wrap.clientWidth - panel.offsetWidth);
        var maxTop = Math.max(0, wrap.clientHeight - panel.offsetHeight);
        return {
            left: Math.min(Math.max(0, left), maxLeft),
            top: Math.min(Math.max(0, top), maxTop)
        };
    }

    function place(left, top) {
        var p = clamp(left, top);
        panel.style.left = p.left + "px";
        panel.style.top = p.top + "px";
        panel.style.right = "auto";
        panel.style.maxHeight = Math.max(160, wrap.clientHeight - p.top - 12) + "px";
        return p;
    }

    function restore() {
        try {
            var saved = JSON.parse(localStorage.getItem(KEY) || "null");
            if (saved && typeof saved.left === "number" && typeof saved.top === "number") {
                place(saved.left, saved.top);
            }
        } catch (e) { /* storage unavailable */ }
    }

    head.style.cursor = "move";
    head.style.touchAction = "none";

    head.addEventListener("pointerdown", function (event) {
        if (event.button !== 0 || event.target.closest("button")) {
            return;
        }
        var rect = panel.getBoundingClientRect();
        var wrapRect = wrap.getBoundingClientRect();
        drag = {
            dx: event.clientX - rect.left,
            dy: event.clientY - rect.top,
            ox: wrapRect.left,
            oy: wrapRect.top
        };
        head.setPointerCapture(event.pointerId);
        event.preventDefault();
    });

    head.addEventListener("pointermove", function (event) {
        if (!drag) { return; }
        place(event.clientX - drag.ox - drag.dx, event.clientY - drag.oy - drag.dy);
    });

    function end(event) {
        if (!drag) { return; }
        drag = null;
        try { head.releasePointerCapture(event.pointerId); } catch (e) { /* already released */ }
        try {
            localStorage.setItem(KEY, JSON.stringify({
                left: parseFloat(panel.style.left) || 0,
                top: parseFloat(panel.style.top) || 0
            }));
        } catch (e) { /* storage unavailable */ }
    }

    head.addEventListener("pointerup", end);
    head.addEventListener("pointercancel", end);

    window.addEventListener("resize", function () {
        if (panel.style.left) {
            place(parseFloat(panel.style.left) || 0, parseFloat(panel.style.top) || 0);
        }
    });

    // Re-apply the remembered spot whenever the panel opens (its size is known then).
    new MutationObserver(restore).observe(panel, { attributes: true, attributeFilter: ["class", "hidden"] });
    restore();
})();
