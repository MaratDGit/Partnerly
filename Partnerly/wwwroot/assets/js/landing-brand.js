(function () {
    "use strict";

    var brand = document.querySelector(".landing-brand");
    if (!brand || !document.documentElement.classList.contains("landing-draw")) {
        return;
    }

    var logo = brand.querySelector(".site-logo");
    var textEl = brand.querySelector(".landing-brand-text");
    var letters = textEl ? textEl.querySelectorAll(".landing-brand-letter") : [];
    var paths = logo ? Array.prototype.slice.call(logo.querySelectorAll("path")) : [];
    var circles = logo ? Array.prototype.slice.call(logo.querySelectorAll("circle")) : [];
    var timers = [];
    var firstPass = true;
    var HOLD_MS = 5600;
    var ERASE_MS = 700;
    var pen = null;

    if (textEl) {
        pen = document.createElement("span");
        pen.className = "landing-brand-pen";
        pen.setAttribute("aria-hidden", "true");
        textEl.appendChild(pen);
    }

    function later(fn, ms) {
        var id = window.setTimeout(fn, ms);
        timers.push(id);
        return id;
    }

    function clearTimers() {
        timers.forEach(window.clearTimeout);
        timers = [];
    }

    function prepareLogo() {
        paths.forEach(function (path) {
            var len = Math.ceil(path.getTotalLength());
            path.style.strokeDasharray = String(len);
            path.style.strokeDashoffset = String(len);
        });
        circles.forEach(function (circle) {
            circle.style.opacity = "0";
        });
    }

    function rewindLogo() {
        paths.forEach(function (path) {
            path.style.transition = "none";
            path.style.strokeDashoffset = path.style.strokeDasharray;
        });
        circles.forEach(function (circle) {
            circle.style.transition = "none";
            circle.style.opacity = "0";
        });
    }

    function drawLogo() {
        paths.forEach(function (path, index) {
            later(function () {
                path.style.transition = "stroke-dashoffset 0.9s ease-out";
                path.style.strokeDashoffset = "0";
            }, 60 + index * 150);
        });

        later(function () {
            circles.forEach(function (circle, index) {
                later(function () {
                    circle.style.transition = "opacity 0.35s ease";
                    circle.style.opacity = "1";
                }, index * 60);
            });
        }, 60 + paths.length * 150 + 180);
    }

    function letterWriteMs(ch) {
        if (!ch || ch === " " || ch === "\u00A0") {
            return 70;
        }
        if (/[MWGOwm@%Q]/.test(ch)) {
            return 270;
        }
        if (/[iljI1.,:'!|]/.test(ch)) {
            return 115;
        }
        return 175 + (ch.charCodeAt(0) % 45);
    }

    function movePen(letter) {
        if (!pen || !letter) {
            return;
        }
        pen.style.left = (letter.offsetLeft + letter.offsetWidth) + "px";
        pen.style.top = (letter.offsetTop + letter.offsetHeight * 0.16) + "px";
        pen.style.height = (letter.offsetHeight * 0.7) + "px";
    }

    function hidePen() {
        if (!pen) {
            return;
        }
        pen.style.left = "";
    }

    function nameWriteDuration() {
        var total = 280;
        Array.prototype.forEach.call(letters, function (letter) {
            total += letterWriteMs(letter.textContent) * 0.68;
        });
        return total + 240;
    }

    function drawName() {
        var delay = 0;
        brand.classList.add("is-writing");
        Array.prototype.forEach.call(letters, function (letter) {
            var ms = letterWriteMs(letter.textContent);
            later(function () {
                letter.style.setProperty("--write-ms", ms + "ms");
                letter.classList.add("is-in");
                movePen(letter);
            }, 280 + delay);
            delay += ms * 0.68;
        });
        later(function () {
            brand.classList.remove("is-writing");
            hidePen();
        }, 280 + delay + 220);
    }

    function eraseLogo() {
        brand.classList.add("is-erasing");
        paths.forEach(function (path) {
            path.style.transition = "stroke-dashoffset 0.55s ease-in";
            path.style.strokeDashoffset = path.style.strokeDasharray;
        });
        circles.forEach(function (circle) {
            circle.style.transition = "opacity 0.28s ease";
            circle.style.opacity = "0";
        });
    }

    function loop() {
        clearTimers();
        brand.classList.remove("is-erasing");
        rewindLogo();
        void brand.offsetWidth;

        drawLogo();
        if (firstPass) {
            drawName();
        }

        var logoDone = 60 + paths.length * 150 + 700;
        var nameDone = nameWriteDuration();
        var drawnAt = firstPass ? Math.max(logoDone, nameDone) : logoDone;

        later(eraseLogo, drawnAt + HOLD_MS);
        later(function () {
            firstPass = false;
            loop();
        }, drawnAt + HOLD_MS + ERASE_MS);
    }

    prepareLogo();
    loop();
})();
