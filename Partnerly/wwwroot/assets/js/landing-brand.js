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

    function drawName() {
        Array.prototype.forEach.call(letters, function (letter, index) {
            later(function () {
                letter.classList.add("is-in");
            }, 380 + index * 70);
        });
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
        var nameDone = 380 + letters.length * 70 + 280;
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
