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
    var gather = null;

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
        gather = null;
        createDots();
    }

    function easeInOutCubic(t) {
        return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
    }

    function gatherToAuth(card, onReady) {
        if (reduceMotion || !card) {
            if (typeof onReady === "function") {
                onReady();
            }
            return;
        }

        var canvasRect = canvas.getBoundingClientRect();
        var cardRect = card.getBoundingClientRect();
        var left = cardRect.left - canvasRect.left;
        var top = cardRect.top - canvasRect.top;
        var innerW = Math.max(24, cardRect.width - 28);
        var innerH = Math.max(24, cardRect.height - 28);
        var i;
        var dot;
        var u;
        var v;

        for (i = 0; i < dots.length; i++) {
            dot = dots[i];
            dot.homeX = dot.x;
            dot.homeY = dot.y;
            dot.homeSize = dot.size;
            u = Math.random();
            v = Math.random();
            if (Math.random() < 0.38) {
                u = 0.5 + (Math.random() - 0.5) * 0.5;
                v = 0.5 + (Math.random() - 0.5) * 0.55;
            }
            dot.targetX = left + 14 + u * innerW;
            dot.targetY = top + 14 + v * innerH;
            dot.targetSize = 4.2 + Math.random() * 11;
            dot.gatherDelay = Math.min(0.38, Math.hypot(dot.targetX - dot.x, dot.targetY - dot.y) / 2400);
        }

        gather = {
            t: 0,
            duration: 1.28,
            delaySpan: 0.38,
            phase: "in",
            onReady: onReady,
            ready: false
        };
    }

    function cancelGather() {
        gather = null;
    }

    function draw(now) {
        var dt = lastFrame ? Math.min(0.033, (now - lastFrame) / 1000) : 0.016;
        lastFrame = now;
        var target = countForSize();
        var gathering = gather !== null;
        var gatherEase = 0;
        var i;
        var dot;
        var fadeIn;
        var fadeOut;
        var envelope;
        var twinkle;
        var alpha;
        var radius;
        var gx;

        ctx.clearRect(0, 0, width, height);

        if (gathering) {
            gather.t += dt;
            gatherEase = Math.min(1, gather.t / gather.duration);

            if (gather.phase === "in" && !gather.ready && gatherEase >= 0.72) {
                gather.ready = true;
                if (typeof gather.onReady === "function") {
                    gather.onReady();
                }
            }

            if (gather.t >= gather.duration) {
                if (gather.phase === "in") {
                    gather.phase = "hold";
                    gather.t = 0;
                    gather.duration = 0.22;
                } else if (gather.phase === "hold") {
                    gather.phase = "out";
                    gather.t = 0;
                    gather.duration = 1.35;
                } else {
                    gather = null;
                    gathering = false;
                }
            }
        }

        if (!reduceMotion && !gathering) {
            spawnWait -= dt;
            if (spawnWait <= 0) {
                var need = Math.max(1, Math.round((target - dots.length) * 0.12));
                spawnDots(need + (Math.random() < 0.45 ? 1 : 0));
                spawnWait = 0.05 + Math.random() * 0.12;
            }
        }

        for (i = dots.length - 1; i >= 0; i--) {
            dot = dots[i];

            if (gathering) {
                if (gather.phase === "hold") {
                    gx = 1;
                } else {
                    gx = Math.min(1, Math.max(0, (gather.t - (dot.gatherDelay || 0)) / Math.max(0.2, gather.duration - (gather.delaySpan || 0))));
                    gx = easeInOutCubic(gx);
                }

                if (gather.phase === "out") {
                    dot.x = dot.targetX + (dot.homeX - dot.targetX) * gx;
                    dot.y = dot.targetY + (dot.homeY - dot.targetY) * gx;
                    radius = (dot.targetSize + (dot.homeSize - dot.targetSize) * gx) * sizeScale;
                    alpha = Math.max(0.18, 1 - gx * 0.5);
                } else {
                    dot.x = dot.homeX + (dot.targetX - dot.homeX) * gx;
                    dot.y = dot.homeY + (dot.targetY - dot.homeY) * gx;
                    radius = (dot.homeSize + (dot.targetSize - dot.homeSize) * gx) * sizeScale;
                    alpha = Math.min(1, 0.22 + gx * 0.7);
                }

                ctx.beginPath();
                ctx.fillStyle = "rgba(" + dot.color + "," + alpha.toFixed(3) + ")";
                ctx.arc(dot.x, dot.y, radius, 0, Math.PI * 2);
                ctx.fill();
                continue;
            }

            if (!reduceMotion) {
                dot.life += dt;
                dot.phase += dt * dot.speed;
            }

            if (!reduceMotion && dot.life >= dot.maxLife) {
                dots.splice(i, 1);
                continue;
            }

            fadeIn = Math.min(1, dot.life / 0.4);
            fadeOut = Math.min(1, (dot.maxLife - dot.life) / 0.65);
            envelope = reduceMotion ? 1 : Math.min(fadeIn, fadeOut);
            twinkle = 0.18 + (Math.sin(dot.phase) * 0.5 + 0.5) * 0.72;
            alpha = twinkle * envelope;

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

    window.partnerlyLandingDots = {
        gatherToAuth: gatherToAuth,
        cancelGather: cancelGather
    };
})();
