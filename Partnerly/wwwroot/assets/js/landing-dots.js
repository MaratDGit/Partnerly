(function () {
    "use strict";

    var canvas = document.getElementById("landingDots");
    if (!canvas || !canvas.getContext) {
        return;
    }

    var ctx = canvas.getContext("2d", { alpha: true });
    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var dpr = 1;
    var width = 0;
    var height = 0;
    var dots = [];
    var lastFrame = 0;
    var rafId = 0;
    var spawnWait = 0;
    var sizeScale = 1;
    var MIN_SCALE = 0.4;
    var MAX_SCALE = 2.5;

    var COLORS = [
        "105, 108, 255",
        "129, 140, 248",
        "139, 92, 246",
        "167, 139, 250",
        "99, 102, 241"
    ];

    function countForSize() {
        var area = width * height;
        return Math.max(180, Math.min(420, Math.round(area / 5200)));
    }

    function makeDot(seeded) {
        var maxLife = 2.2 + Math.random() * 4.8;
        return {
            x: Math.random() * width,
            y: Math.random() * height,
            size: 1.15 + Math.random() * 1.35,
            color: COLORS[Math.floor(Math.random() * COLORS.length)],
            phase: Math.random() * Math.PI * 2,
            speed: 0.35 + Math.random() * 0.55,
            life: seeded ? Math.random() * maxLife * 0.75 : 0,
            maxLife: maxLife
        };
    }

    function createDots() {
        var count = countForSize();
        dots = [];
        for (var i = 0; i < count; i++) {
            dots.push(makeDot(true));
        }
        spawnWait = 0.08;
    }

    function spawnDots(count) {
        var target = countForSize();
        var i;
        for (i = 0; i < count && dots.length < target + 40; i++) {
            dots.push(makeDot(false));
        }
    }

    function resize() {
        var rect = canvas.getBoundingClientRect();
        dpr = Math.min(window.devicePixelRatio || 1, 2);
        width = Math.max(1, rect.width);
        height = Math.max(1, rect.height);
        canvas.width = Math.round(width * dpr);
        canvas.height = Math.round(height * dpr);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        createDots();
    }

    function draw(now) {
        var dt = lastFrame ? Math.min(0.033, (now - lastFrame) / 1000) : 0.016;
        lastFrame = now;
        var target = countForSize();

        ctx.clearRect(0, 0, width, height);

        if (!reduceMotion) {
            spawnWait -= dt;
            if (spawnWait <= 0) {
                var need = Math.max(1, Math.round((target - dots.length) * 0.12));
                spawnDots(need + (Math.random() < 0.45 ? 1 : 0));
                spawnWait = 0.05 + Math.random() * 0.12;
            }
        }

        for (var i = dots.length - 1; i >= 0; i--) {
            var dot = dots[i];

            if (!reduceMotion) {
                dot.life += dt;
                dot.phase += dt * dot.speed;
            }

            if (!reduceMotion && dot.life >= dot.maxLife) {
                dots.splice(i, 1);
                continue;
            }

            var fadeIn = Math.min(1, dot.life / 0.4);
            var fadeOut = Math.min(1, (dot.maxLife - dot.life) / 0.65);
            var envelope = reduceMotion ? 1 : Math.min(fadeIn, fadeOut);
            var twinkle = 0.18 + (Math.sin(dot.phase) * 0.5 + 0.5) * 0.72;
            var alpha = twinkle * envelope;

            ctx.beginPath();
            ctx.fillStyle = "rgba(" + dot.color + "," + alpha.toFixed(3) + ")";
            ctx.arc(dot.x, dot.y, dot.size * sizeScale, 0, Math.PI * 2);
            ctx.fill();
        }

        rafId = window.requestAnimationFrame(draw);
    }

    function canScaleDots() {
        if (document.body.classList.contains("is-auth-open") ||
            document.body.classList.contains("is-info-open")) {
            return false;
        }
        if (document.body.classList.contains("is-chat")) {
            return false;
        }
        var stage = document.querySelector(".landing-stage");
        if (stage && stage.classList.contains("is-chat")) {
            return false;
        }
        var auth = document.getElementById("landingAuth");
        if (auth && auth.classList.contains("is-open")) {
            return false;
        }
        return true;
    }

    function onWheel(event) {
        if (!canScaleDots()) {
            return;
        }

        event.preventDefault();
        var next = sizeScale * Math.exp(-event.deltaY * 0.0016);
        if (next < MIN_SCALE) {
            next = MIN_SCALE;
        } else if (next > MAX_SCALE) {
            next = MAX_SCALE;
        }
        sizeScale = next;
    }

    resize();
    window.addEventListener("resize", resize);
    window.addEventListener("wheel", onWheel, { passive: false });

    document.addEventListener("visibilitychange", function () {
        if (document.hidden) {
            window.cancelAnimationFrame(rafId);
            lastFrame = 0;
        } else {
            rafId = window.requestAnimationFrame(draw);
        }
    });

    rafId = window.requestAnimationFrame(draw);
})();
