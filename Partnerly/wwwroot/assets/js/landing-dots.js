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
    var links = {};
    var lastFrame = 0;
    var rafId = 0;
    var spawnWait = 0;
    var sizeScale = 1;
    var MIN_SCALE = 0.4;
    var MAX_SCALE = 2.5;
    var nextId = 1;
    var LINK_GROW = 0.9;
    var LINK_FADE = 1.7;
    var MAX_LINKS_PER_DOT = 2;

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

    function linkMaxDist() {
        return 82 * Math.max(0.85, Math.min(sizeScale, 1.55));
    }

    function makeDot(seeded) {
        var maxLife = 2.2 + Math.random() * 4.8;
        return {
            id: nextId++,
            x: Math.random() * width,
            y: Math.random() * height,
            vx: (Math.random() - 0.5) * 14,
            vy: (Math.random() - 0.5) * 14,
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
        links = {};
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

    function lerp(a, b, t) {
        return a + (b - a) * t;
    }

    function pairKey(a, b) {
        return a.id < b.id ? a.id + ":" + b.id : b.id + ":" + a.id;
    }

    function cellKey(cx, cy) {
        return cx + ":" + cy;
    }

    function buildGrid(cell) {
        var grid = {};
        var i;
        var dot;
        var key;
        for (i = 0; i < dots.length; i++) {
            dot = dots[i];
            key = cellKey(Math.floor(dot.x / cell), Math.floor(dot.y / cell));
            if (!grid[key]) {
                grid[key] = [];
            }
            grid[key].push(dot);
        }
        return grid;
    }

    function nearestInRange(dot, grid, cell, maxDist) {
        var cx = Math.floor(dot.x / cell);
        var cy = Math.floor(dot.y / cell);
        var best = null;
        var bestDist = maxDist;
        var dx;
        var dy;
        var list;
        var j;
        var other;
        var dist;
        for (dx = -1; dx <= 1; dx++) {
            for (dy = -1; dy <= 1; dy++) {
                list = grid[cellKey(cx + dx, cy + dy)];
                if (!list) {
                    continue;
                }
                for (j = 0; j < list.length; j++) {
                    other = list[j];
                    if (other.id === dot.id) {
                        continue;
                    }
                    if (other.maxLife - other.life < 0.4 || other.life < 0.2) {
                        continue;
                    }
                    dist = Math.hypot(other.x - dot.x, other.y - dot.y);
                    if (dist > 10 && dist < bestDist) {
                        bestDist = dist;
                        best = other;
                    }
                }
            }
        }
        return best ? { other: best, dist: bestDist } : null;
    }

    function updateLinks(dt) {
        var maxDist = linkMaxDist();
        var cell = Math.max(24, maxDist);
        var grid = buildGrid(cell);
        var desired = {};
        var counts = {};
        var i;
        var dot;
        var found;
        var key;
        var a;
        var b;
        var link;
        var alive = {};

        for (i = 0; i < dots.length; i++) {
            alive[dots[i].id] = dots[i];
        }

        for (i = 0; i < dots.length; i++) {
            dot = dots[i];
            if (dot.maxLife - dot.life < 0.4 || dot.life < 0.2) {
                continue;
            }
            found = nearestInRange(dot, grid, cell, maxDist);
            if (!found) {
                continue;
            }
            if ((counts[dot.id] || 0) >= MAX_LINKS_PER_DOT || (counts[found.other.id] || 0) >= MAX_LINKS_PER_DOT) {
                continue;
            }
            key = pairKey(dot, found.other);
            if (desired[key]) {
                continue;
            }
            desired[key] = found.dist;
            counts[dot.id] = (counts[dot.id] || 0) + 1;
            counts[found.other.id] = (counts[found.other.id] || 0) + 1;
            a = (found.other.x - dot.x) / found.dist;
            b = (found.other.y - dot.y) / found.dist;
            dot.vx += a * 7 * dt;
            dot.vy += b * 7 * dt;
            found.other.vx -= a * 7 * dt;
            found.other.vy -= b * 7 * dt;
        }

        for (key in links) {
            if (!Object.prototype.hasOwnProperty.call(links, key)) {
                continue;
            }
            link = links[key];
            a = alive[link.a];
            b = alive[link.b];
            if (!a || !b) {
                delete links[key];
                continue;
            }
            link.ax = a.x;
            link.ay = a.y;
            link.bx = b.x;
            link.by = b.y;
            link.colorA = a.color;
            link.colorB = b.color;
            if (desired[key]) {
                link.dist = desired[key];
                link.progress = Math.min(1, link.progress + dt * LINK_GROW);
                delete desired[key];
            } else {
                link.progress -= dt * LINK_FADE;
                if (link.progress <= 0) {
                    delete links[key];
                }
            }
        }

        for (key in desired) {
            if (!Object.prototype.hasOwnProperty.call(desired, key)) {
                continue;
            }
            a = key.split(":");
            if (!alive[a[0]] || !alive[a[1]]) {
                continue;
            }
            links[key] = {
                a: parseInt(a[0], 10),
                b: parseInt(a[1], 10),
                ax: alive[a[0]].x,
                ay: alive[a[0]].y,
                bx: alive[a[1]].x,
                by: alive[a[1]].y,
                colorA: alive[a[0]].color,
                colorB: alive[a[1]].color,
                dist: desired[key],
                progress: 0.02
            };
        }
    }

    function mixColor(a, b) {
        var pa = (a || "129,140,248").split(",");
        var pb = (b || "129,140,248").split(",");
        return Math.round((parseInt(pa[0], 10) + parseInt(pb[0], 10)) / 2) + "," +
            Math.round((parseInt(pa[1], 10) + parseInt(pb[1], 10)) / 2) + "," +
            Math.round((parseInt(pa[2], 10) + parseInt(pb[2], 10)) / 2);
    }

    function drawLinks() {
        var key;
        var link;
        var t;
        var x1;
        var y1;
        var x2;
        var y2;
        var alpha;
        var color;
        var maxDist = linkMaxDist();

        ctx.lineCap = "round";
        ctx.lineWidth = Math.max(0.55, 0.7 * sizeScale);

        for (key in links) {
            if (!Object.prototype.hasOwnProperty.call(links, key)) {
                continue;
            }
            link = links[key];
            if (link.progress <= 0) {
                continue;
            }
            t = link.progress * 0.5;
            x1 = lerp(link.ax, link.bx, t);
            y1 = lerp(link.ay, link.by, t);
            x2 = lerp(link.bx, link.ax, t);
            y2 = lerp(link.by, link.ay, t);
            alpha = (0.1 + 0.32 * (1 - Math.min(1, link.dist / maxDist))) * link.progress;
            color = mixColor(link.colorA, link.colorB);
            ctx.strokeStyle = "rgba(" + color + "," + alpha.toFixed(3) + ")";
            ctx.beginPath();
            ctx.moveTo(link.ax, link.ay);
            ctx.lineTo(x1, y1);
            ctx.moveTo(link.bx, link.by);
            ctx.lineTo(x2, y2);
            ctx.stroke();

            if (link.progress < 0.98) {
                ctx.fillStyle = "rgba(" + color + "," + Math.min(0.7, alpha + 0.22).toFixed(3) + ")";
                ctx.beginPath();
                ctx.arc(x1, y1, 1.05 * sizeScale, 0, Math.PI * 2);
                ctx.fill();
                ctx.beginPath();
                ctx.arc(x2, y2, 1.05 * sizeScale, 0, Math.PI * 2);
                ctx.fill();
            }
        }
    }

    function draw(now) {
        var dt = lastFrame ? Math.min(0.033, (now - lastFrame) / 1000) : 0.016;
        lastFrame = now;
        var target = countForSize();
        var i;
        var dot;
        var fadeIn;
        var fadeOut;
        var envelope;
        var twinkle;
        var alpha;
        var speed;

        ctx.clearRect(0, 0, width, height);

        if (!reduceMotion) {
            spawnWait -= dt;
            if (spawnWait <= 0) {
                var need = Math.max(1, Math.round((target - dots.length) * 0.12));
                spawnDots(need + (Math.random() < 0.45 ? 1 : 0));
                spawnWait = 0.05 + Math.random() * 0.12;
            }
        }

        for (i = dots.length - 1; i >= 0; i--) {
            dot = dots[i];

            if (!reduceMotion) {
                dot.life += dt;
                dot.phase += dt * dot.speed;
                dot.vx += (Math.random() - 0.5) * 10 * dt;
                dot.vy += (Math.random() - 0.5) * 10 * dt;
                dot.vx *= 0.985;
                dot.vy *= 0.985;
                speed = Math.hypot(dot.vx, dot.vy);
                if (speed > 22) {
                    dot.vx = dot.vx / speed * 22;
                    dot.vy = dot.vy / speed * 22;
                }
                dot.x += dot.vx * dt;
                dot.y += dot.vy * dt;
                if (dot.x < 0) {
                    dot.x = 0;
                    dot.vx *= -0.7;
                } else if (dot.x > width) {
                    dot.x = width;
                    dot.vx *= -0.7;
                }
                if (dot.y < 0) {
                    dot.y = 0;
                    dot.vy *= -0.7;
                } else if (dot.y > height) {
                    dot.y = height;
                    dot.vy *= -0.7;
                }
            }

            if (!reduceMotion && dot.life >= dot.maxLife) {
                dots.splice(i, 1);
                continue;
            }
        }

        if (!reduceMotion) {
            updateLinks(dt);
            drawLinks();
        }

        for (i = 0; i < dots.length; i++) {
            dot = dots[i];
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
})();
