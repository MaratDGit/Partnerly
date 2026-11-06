// Extra tools of the agent editor, built on top of builder.js (window.PartnerlyBuilder.internals):
//  - check before Publish (cards that are not ready)
//  - select several cards (Shift + drag / Shift + click), move / copy / delete them, align and distribute
//  - map + card search
//  - sticky notes on the canvas
//  - a past run shown on the canvas (done / failed / skipped cards)
(function () {
    "use strict";

    var started = false;

    function boot() {
        if (started) { return; }
        var builder = window.PartnerlyBuilder;
        if (!builder || !builder.internals) { return; }
        started = true;
        init(builder, builder.internals);
    }

    if (window.PartnerlyBuilder && window.PartnerlyBuilder.internals) {
        boot();
    } else {
        window.addEventListener("partnerly:builder-ready", boot);
    }

    function init(builder, I) {
        var editor = I.editor;
        var root = I.root;
        var wrap = root.closest(".automation-canvas-wrap") || root.parentElement;
        var zoomBar = document.querySelector(".automation-zoom");
        var form = document.getElementById("automationBuilderForm");

        var text = {};
        try {
            text = JSON.parse((document.getElementById("automationToolsI18n") || {}).textContent || "{}");
        } catch (e) {
            text = {};
        }
        function tt(key, a) {
            var value = text[key] || key;
            return value.replace("{0}", a == null ? "" : a);
        }

        document.body.classList.add("has-builder-tools");

        function live() { return editor.drawflow.drawflow.Home.data; }
        function nodeEl(id) { return document.getElementById("node-" + id); }
        function make(tag, className, content) {
            var el = document.createElement(tag);
            if (className) { el.className = className; }
            if (content != null) { el.textContent = content; }
            return el;
        }
        function svgIcon(name) {
            var shapes = {
                left: '<path d="M3 2v16"/><rect x="5" y="5" width="11" height="3.2" rx="1"/><rect x="5" y="11.8" width="6.5" height="3.2" rx="1"/>',
                center: '<path d="M10 2v16"/><rect x="3.5" y="5" width="13" height="3.2" rx="1"/><rect x="6" y="11.8" width="8" height="3.2" rx="1"/>',
                right: '<path d="M17 2v16"/><rect x="4" y="5" width="11" height="3.2" rx="1"/><rect x="8.5" y="11.8" width="6.5" height="3.2" rx="1"/>',
                top: '<path d="M2 3h16"/><rect x="5" y="5" width="3.2" height="11" rx="1"/><rect x="11.8" y="5" width="3.2" height="6.5" rx="1"/>',
                middle: '<path d="M2 10h16"/><rect x="5" y="3.5" width="3.2" height="13" rx="1"/><rect x="11.8" y="6" width="3.2" height="8" rx="1"/>',
                bottom: '<path d="M2 17h16"/><rect x="5" y="4" width="3.2" height="11" rx="1"/><rect x="11.8" y="8.5" width="3.2" height="6.5" rx="1"/>',
                disth: '<path d="M3 2v16M17 2v16"/><rect x="8" y="5.5" width="4" height="9" rx="1"/>',
                distv: '<path d="M2 3h16M2 17h16"/><rect x="5.5" y="8" width="9" height="4" rx="1"/>'
            };
            return '<svg viewBox="0 0 20 20" width="18" height="18" fill="currentColor" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" aria-hidden="true">' + shapes[name] + "</svg>";
        }
        function zoomButton(iconClass, title, attr) {
            var button = make("button", "automation-zoom-btn");
            button.type = "button";
            button.setAttribute(attr, "");
            button.title = title;
            button.setAttribute("aria-label", title);
            var i = make("i", "bx " + iconClass);
            i.setAttribute("aria-hidden", "true");
            button.appendChild(i);
            if (zoomBar) { zoomBar.appendChild(button); }
            return button;
        }

        // canvas <-> screen helpers (the canvas scales around its own middle)
        function origin() {
            var o = (getComputedStyle(editor.precanvas).transformOrigin || "0px 0px").split(" ").map(parseFloat);
            return { x: o[0] || 0, y: o[1] || 0 };
        }
        function toWorld(clientX, clientY) {
            var area = root.getBoundingClientRect();
            var z = editor.zoom || 1;
            var o = origin();
            return {
                x: (clientX - area.left - (editor.canvas_x || 0) - o.x) / z + o.x,
                y: (clientY - area.top - (editor.canvas_y || 0) - o.y) / z + o.y
            };
        }
        function allIds() {
            return Object.keys(live()).map(String);
        }
        function box(id) {
            var el = nodeEl(id);
            var data = live()[id];
            return { id: id, x: data.pos_x, y: data.pos_y, w: el ? el.offsetWidth : 176, h: el ? el.offsetHeight : 76 };
        }

        // Move cards to new places (data + element), lines follow; smooth = slide.
        function moveCards(positions, smooth) {
            var ids = Object.keys(positions);
            if (!ids.length) { return; }
            if (smooth) { root.classList.add("is-arranging"); }
            ids.forEach(function (id) {
                var data = live()[id];
                var el = nodeEl(id);
                data.pos_x = positions[id].x;
                data.pos_y = positions[id].y;
                if (el) { el.style.left = positions[id].x + "px"; el.style.top = positions[id].y + "px"; }
            });
            function follow() { allIds().forEach(function (id) { editor.updateConnectionNodes("node-" + id); }); }
            if (!smooth) { follow(); I.syncHidden(); return; }
            var begin = performance.now();
            (function tick() {
                follow();
                if (performance.now() - begin < 900) { window.setTimeout(tick, 16); }
            })();
            window.setTimeout(function () {
                root.classList.remove("is-arranging");
                follow();
                I.syncHidden();
            }, 950);
        }

        // =================================================================
        //  Several cards at once
        // =================================================================
        var multi = [];
        var bar = make("div", "automation-multibar");
        bar.hidden = true;
        var barCount = make("span", "automation-multibar-count");
        bar.appendChild(barCount);
        var ALIGNS = [
            ["left", "alignLeft"], ["center", "alignCenter"], ["right", "alignRight"],
            ["top", "alignTop"], ["middle", "alignMiddle"], ["bottom", "alignBottom"],
            ["disth", "distH"], ["distv", "distV"]
        ];
        ALIGNS.forEach(function (item) {
            var button = make("button", "automation-multibar-btn");
            button.type = "button";
            button.setAttribute("data-multi-op", item[0]);
            button.title = tt(item[1]);
            button.setAttribute("aria-label", tt(item[1]));
            button.innerHTML = svgIcon(item[0]);
            bar.appendChild(button);
        });
        var barClear = make("button", "automation-multibar-btn is-clear");
        barClear.type = "button";
        barClear.setAttribute("data-multi-clear", "");
        barClear.title = tt("multiClear");
        barClear.setAttribute("aria-label", tt("multiClear"));
        barClear.innerHTML = '<i class="bx bx-x" aria-hidden="true"></i>';
        bar.appendChild(barClear);
        wrap.appendChild(bar);

        function paintMulti() {
            Array.prototype.forEach.call(root.querySelectorAll(".auto-node.is-multi"), function (el) { el.classList.remove("is-multi"); });
            multi.forEach(function (id) {
                var inner = root.querySelector("#node-" + id + " .auto-node");
                if (inner) { inner.classList.add("is-multi"); }
            });
            bar.hidden = multi.length < 2;
            barCount.textContent = tt("multiCount", multi.length);
            bar.title = tt("multiHint");
        }
        function setMulti(ids) {
            multi = ids.map(String).filter(function (id, i, list) { return live()[id] && list.indexOf(id) === i; });
            paintMulti();
        }
        function clearMulti() { if (multi.length) { setMulti([]); } }
        function currentSingle() {
            var el = editor.node_selected;
            return el && el.id ? el.id.replace("node-", "") : null;
        }

        function isCanvasTarget(target) {
            var first = target && target.classList && target.classList[0];
            return first === "parent-drawflow" || first === "drawflow" || first === "automation-canvas";
        }

        // --- rubber band (Shift + drag on the empty canvas) and Shift + click on a card
        var lasso = null;
        function onRootMouseDown(event) {
            if (event.button !== 0 || !I.canEditNow()) { return; }
            var target = event.target;
            if (target.closest && target.closest(".automation-note, .automation-node-menu, .output, .input, .connection, .point")) { return; }
            var card = target.closest ? target.closest(".drawflow-node") : null;
            if (event.shiftKey && card && root.contains(card)) {
                event.preventDefault();
                event.stopPropagation();
                var id = card.id.replace("node-", "");
                var base = multi.length ? multi.slice() : (currentSingle() ? [currentSingle()] : []);
                var at = base.indexOf(id);
                if (at === -1) { base.push(id); } else { base.splice(at, 1); }
                setMulti(base);
                return;
            }
            if (event.shiftKey && isCanvasTarget(target)) {
                event.preventDefault();
                event.stopPropagation();
                var area = wrap.getBoundingClientRect();
                lasso = { x0: event.clientX, y0: event.clientY, el: make("div", "automation-lasso"), keep: multi.slice() };
                wrap.appendChild(lasso.el);
                drawLasso(event.clientX, event.clientY, area);
                window.addEventListener("mousemove", onLassoMove);
                window.addEventListener("mouseup", onLassoEnd);
                return;
            }
            if (card && root.contains(card)) {
                var cardId = card.id.replace("node-", "");
                if (multi.length > 1 && multi.indexOf(cardId) !== -1) {
                    startGroupDrag(cardId, event);
                } else {
                    clearMulti();
                }
                return;
            }
            if (isCanvasTarget(target)) {
                canvasPress = { x: event.clientX, y: event.clientY };
            }
        }
        var canvasPress = null;
        function drawLasso(x, y, area) {
            var left = Math.min(lasso.x0, x) - area.left, top = Math.min(lasso.y0, y) - area.top;
            lasso.el.style.left = left + "px";
            lasso.el.style.top = top + "px";
            lasso.el.style.width = Math.abs(x - lasso.x0) + "px";
            lasso.el.style.height = Math.abs(y - lasso.y0) + "px";
        }
        function onLassoMove(event) {
            if (lasso) { drawLasso(event.clientX, event.clientY, wrap.getBoundingClientRect()); }
        }
        function onLassoEnd(event) {
            window.removeEventListener("mousemove", onLassoMove);
            window.removeEventListener("mouseup", onLassoEnd);
            if (!lasso) { return; }
            var x0 = Math.min(lasso.x0, event.clientX), x1 = Math.max(lasso.x0, event.clientX);
            var y0 = Math.min(lasso.y0, event.clientY), y1 = Math.max(lasso.y0, event.clientY);
            var picked = lasso.keep.slice();
            Array.prototype.forEach.call(root.querySelectorAll(".drawflow-node"), function (el) {
                var r = el.getBoundingClientRect();
                if (r.left < x1 && r.right > x0 && r.top < y1 && r.bottom > y0) {
                    var id = el.id.replace("node-", "");
                    if (picked.indexOf(id) === -1) { picked.push(id); }
                }
            });
            if (lasso.el.parentNode) { lasso.el.parentNode.removeChild(lasso.el); }
            lasso = null;
            setMulti(picked);
        }
        function onRootMouseUp(event) {
            if (canvasPress && Math.abs(event.clientX - canvasPress.x) < 4 && Math.abs(event.clientY - canvasPress.y) < 4) {
                clearMulti();
            }
            canvasPress = null;
        }
        root.addEventListener("mousedown", onRootMouseDown, true);
        window.addEventListener("mouseup", onRootMouseUp);

        // --- the whole group follows the card that is being dragged
        var drag = null;
        function startGroupDrag(id, event) {
            var el = nodeEl(id);
            if (!el) { return; }
            drag = { id: id, sx: event.clientX, sy: event.clientY, moved: false, start: {}, lead: { x: parseFloat(el.style.left) || 0, y: parseFloat(el.style.top) || 0 } };
            multi.forEach(function (other) { drag.start[other] = { x: live()[other].pos_x, y: live()[other].pos_y }; });
            window.addEventListener("mousemove", onGroupMove);
            window.addEventListener("mouseup", onGroupEnd);
        }
        function onGroupMove(event) {
            if (!drag) { return; }
            if (!drag.moved && Math.abs(event.clientX - drag.sx) + Math.abs(event.clientY - drag.sy) < 4) { return; }
            drag.moved = true;
            var lead = nodeEl(drag.id);
            if (!lead) { return; }
            var dx = (parseFloat(lead.style.left) || 0) - drag.lead.x;
            var dy = (parseFloat(lead.style.top) || 0) - drag.lead.y;
            multi.forEach(function (other) {
                if (other === drag.id) { return; }
                var el = nodeEl(other);
                var data = live()[other];
                if (!el || !data) { return; }
                data.pos_x = drag.start[other].x + dx;
                data.pos_y = drag.start[other].y + dy;
                el.style.left = data.pos_x + "px";
                el.style.top = data.pos_y + "px";
            });
            multi.forEach(function (other) { editor.updateConnectionNodes("node-" + other); });
        }
        function onGroupEnd() {
            window.removeEventListener("mousemove", onGroupMove);
            window.removeEventListener("mouseup", onGroupEnd);
            var finished = drag;
            drag = null;
            if (!finished || !finished.moved) {
                clearMulti();
                return;
            }
            // Drawflow has just stored the dragged card: every other card gets exactly the same
            // shift, so the group keeps its shape.
            var lead = live()[finished.id];
            if (!lead) { return; }
            var dx = lead.pos_x - finished.start[finished.id].x;
            var dy = lead.pos_y - finished.start[finished.id].y;
            var map = {};
            multi.forEach(function (id) {
                map[id] = { x: finished.start[id].x + dx, y: finished.start[id].y + dy };
            });
            moveCards(map, false);
        }

        // --- align / distribute
        function alignOp(op) {
            if (multi.length < 2 || !I.canEditNow()) { return; }
            var boxes = multi.map(box);
            var minX = Math.min.apply(null, boxes.map(function (b) { return b.x; }));
            var maxR = Math.max.apply(null, boxes.map(function (b) { return b.x + b.w; }));
            var minY = Math.min.apply(null, boxes.map(function (b) { return b.y; }));
            var maxB = Math.max.apply(null, boxes.map(function (b) { return b.y + b.h; }));
            var map = {};
            boxes.forEach(function (b) { map[b.id] = { x: b.x, y: b.y }; });
            if (op === "left") { boxes.forEach(function (b) { map[b.id].x = minX; }); }
            else if (op === "right") { boxes.forEach(function (b) { map[b.id].x = maxR - b.w; }); }
            else if (op === "center") { boxes.forEach(function (b) { map[b.id].x = (minX + maxR) / 2 - b.w / 2; }); }
            else if (op === "top") { boxes.forEach(function (b) { map[b.id].y = minY; }); }
            else if (op === "bottom") { boxes.forEach(function (b) { map[b.id].y = maxB - b.h; }); }
            else if (op === "middle") { boxes.forEach(function (b) { map[b.id].y = (minY + maxB) / 2 - b.h / 2; }); }
            else if (op === "disth" || op === "distv") {
                var horizontal = op === "disth";
                var sorted = boxes.slice().sort(function (a, b) { return horizontal ? a.x - b.x : a.y - b.y; });
                var total = sorted.reduce(function (sum, b) { return sum + (horizontal ? b.w : b.h); }, 0);
                var span = horizontal ? maxR - minX : maxB - minY;
                var gap = sorted.length > 1 ? (span - total) / (sorted.length - 1) : 0;
                var cursor = horizontal ? minX : minY;
                sorted.forEach(function (b) {
                    if (horizontal) { map[b.id].x = cursor; cursor += b.w + gap; }
                    else { map[b.id].y = cursor; cursor += b.h + gap; }
                });
            }
            moveCards(map, true);
        }
        bar.addEventListener("click", function (event) {
            var op = event.target.closest && event.target.closest("[data-multi-op]");
            if (op) { alignOp(op.getAttribute("data-multi-op")); return; }
            if (event.target.closest && event.target.closest("[data-multi-clear]")) { clearMulti(); }
        });
        ["mousedown", "pointerdown", "touchstart", "wheel"].forEach(function (name) {
            bar.addEventListener(name, function (event) { event.stopPropagation(); }, { passive: true });
        });

        // --- group copy / paste
        var groupClip = null;
        var pasteCount = 0;
        function copyGroup() {
            I.persistSelected();
            var set = {};
            multi.forEach(function (id) { set[id] = true; });
            var home = editor.export().drawflow.Home.data;
            var nodes = [], edges = [];
            multi.forEach(function (id) {
                var node = home[id];
                if (!node) { return; }
                nodes.push({ id: id, data: JSON.parse(JSON.stringify(node.data)), x: node.pos_x, y: node.pos_y });
                Object.keys(node.outputs || {}).forEach(function (outKey) {
                    (node.outputs[outKey].connections || []).forEach(function (link) {
                        if (set[String(link.node)]) { edges.push({ from: id, to: String(link.node), output: outKey }); }
                    });
                });
            });
            groupClip = { nodes: nodes, edges: edges };
            pasteCount = 0;
        }
        function pasteGroup() {
            if (!groupClip || !I.canEditNow()) { return; }
            pasteCount += 1;
            var shift = 36 * pasteCount;
            var made = {};
            groupClip.nodes.forEach(function (item, index) {
                var data = JSON.parse(JSON.stringify(item.data));
                data.id = "n" + Date.now() + index + Math.floor(Math.random() * 1000);
                data.name = I.uniqueName(I.slug(data.type));
                made[item.id] = I.addNode(data, item.x + shift, item.y + shift);
            });
            groupClip.edges.forEach(function (edge) {
                try { editor.addConnection(made[edge.from], made[edge.to], edge.output, "input_1"); } catch (e) { /* ignore */ }
            });
            I.syncHidden();
            setMulti(Object.keys(made).map(function (k) { return String(made[k]); }));
        }

        // --- keys: Delete, Escape, Ctrl+A / C / V for the group
        function dialogOpen() { return !!document.querySelector(".automation-create.is-open, .automation-hub-overlay:not([hidden])"); }
        function onKey(event) {
            if (!document.body.contains(root) || dialogOpen()) { return; }
            if (event.key === "Escape") {
                clearMulti();
                clearRunMarks();
                closeNav();
                return;
            }
            if (I.typingInField() || !I.canEditNow()) { return; }
            var ctrl = event.ctrlKey || event.metaKey;
            var key = String(event.key).toLowerCase();
            if ((event.key === "Delete" || event.key === "Backspace") && multi.length > 1) {
                event.preventDefault();
                event.stopPropagation();
                var doomed = multi.slice();
                setMulti([]);
                doomed.forEach(function (id) { try { editor.removeNodeId("node-" + id); } catch (e) { /* ignore */ } });
                I.clearEditorSelection();
                return;
            }
            if (ctrl && !event.shiftKey && !event.altKey && key === "a") {
                event.preventDefault();
                setMulti(allIds());
                return;
            }
            if (ctrl && key === "c" && multi.length > 1) {
                event.stopPropagation();
                copyGroup();
                return;
            }
            if (ctrl && key === "v" && groupClip && (multi.length > 1 || !currentSingle())) {
                event.preventDefault();
                event.stopPropagation();
                pasteGroup();
            }
        }
        document.addEventListener("keydown", onKey, true);

        // =================================================================
        //  Notes on the canvas
        // =================================================================
        var NOTE_COLORS = ["yellow", "blue", "green", "pink"];
        var notes = builder.internals.getNotes();
        var layer = null;
        var noteTimer = null;

        function ensureLayer() {
            if (layer && layer.isConnected) { return; }
            layer = make("div", "automation-notes");
            editor.precanvas.insertBefore(layer, editor.precanvas.firstChild);
        }
        function commitNotes() {
            window.clearTimeout(noteTimer);
            noteTimer = null;
            I.setNotes(notes);
        }
        function commitLater() {
            window.clearTimeout(noteTimer);
            noteTimer = window.setTimeout(commitNotes, 350);
        }
        function noteById(id) {
            for (var i = 0; i < notes.length; i++) { if (notes[i].id === id) { return notes[i]; } }
            return null;
        }
        function paintNoteColor(el, color) {
            NOTE_COLORS.forEach(function (name) { el.classList.remove("is-" + name); });
            el.classList.add("is-" + (NOTE_COLORS.indexOf(color) === -1 ? "yellow" : color));
        }
        function buildNote(note) {
            var editable = I.canEditNow();
            var el = make("div", "automation-note");
            el.setAttribute("data-note-id", note.id);
            el.style.left = note.x + "px";
            el.style.top = note.y + "px";
            el.style.width = (note.w || 200) + "px";
            el.style.height = (note.h || 120) + "px";
            paintNoteColor(el, note.color);

            var head = make("div", "automation-note-head");
            if (editable) {
                NOTE_COLORS.forEach(function (name) {
                    var dot = make("button", "automation-note-dot is-" + name);
                    dot.type = "button";
                    dot.title = tt("noteColor");
                    dot.setAttribute("aria-label", tt("noteColor") + ": " + name);
                    dot.addEventListener("click", function () {
                        note.color = name;
                        paintNoteColor(el, name);
                        commitNotes();
                    });
                    head.appendChild(dot);
                });
                var del = make("button", "automation-note-del");
                del.type = "button";
                del.title = tt("noteDelete");
                del.setAttribute("aria-label", tt("noteDelete"));
                del.innerHTML = '<i class="bx bx-trash" aria-hidden="true"></i>';
                del.addEventListener("click", function () {
                    notes = notes.filter(function (item) { return item.id !== note.id; });
                    renderNotes();
                    commitNotes();
                });
                head.appendChild(del);
            }
            var area = make("textarea", "automation-note-text");
            area.value = note.text || "";
            area.placeholder = tt("notePlaceholder");
            area.readOnly = !editable;
            area.addEventListener("input", function () {
                note.text = area.value;
                commitLater();
            });
            el.appendChild(head);
            el.appendChild(area);

            // drag by the header
            head.addEventListener("mousedown", function (event) {
                if (event.button !== 0 || !editable || event.target.closest("button")) { return; }
                event.preventDefault();
                event.stopPropagation();
                var startX = event.clientX, startY = event.clientY, baseX = note.x, baseY = note.y;
                var z = editor.zoom || 1;
                function move(e) {
                    note.x = baseX + (e.clientX - startX) / z;
                    note.y = baseY + (e.clientY - startY) / z;
                    el.style.left = note.x + "px";
                    el.style.top = note.y + "px";
                }
                function up() {
                    window.removeEventListener("mousemove", move);
                    window.removeEventListener("mouseup", up);
                    if (note.x !== baseX || note.y !== baseY) { commitNotes(); }
                }
                window.addEventListener("mousemove", move);
                window.addEventListener("mouseup", up);
            });
            ["mousedown", "touchstart", "pointerdown", "dblclick", "contextmenu"].forEach(function (name) {
                el.addEventListener(name, function (event) {
                    if (name === "mousedown" && event.target.closest(".automation-note-head") && !event.defaultPrevented) { /* handled above */ }
                    event.stopPropagation();
                });
            });
            // resize (the browser's own handle)
            if (typeof ResizeObserver === "function") {
                var ready = false;
                new ResizeObserver(function () {
                    if (!ready) { ready = true; return; }
                    var w = el.offsetWidth, h = el.offsetHeight;
                    if (Math.abs(w - (note.w || 200)) > 1 || Math.abs(h - (note.h || 120)) > 1) {
                        note.w = w;
                        note.h = h;
                        commitLater();
                    }
                }).observe(el);
            }
            return el;
        }
        function renderNotes() {
            ensureLayer();
            layer.innerHTML = "";
            notes.forEach(function (note) { layer.appendChild(buildNote(note)); });
        }
        renderNotes();

        var noteButton = zoomButton("bx-note", tt("noteAdd"), "data-zoom-note");
        noteButton.disabled = !I.canEditNow();
        noteButton.addEventListener("click", function () {
            if (!I.canEditNow()) { return; }
            var area = root.getBoundingClientRect();
            var middle = toWorld(area.left + area.width / 2, area.top + area.height / 2);
            var note = { id: "note" + Date.now(), text: "", x: Math.round(middle.x - 100), y: Math.round(middle.y - 60), w: 200, h: 120, color: "yellow" };
            notes.push(note);
            renderNotes();
            commitNotes();
            var fresh = layer.querySelector('[data-note-id="' + note.id + '"] textarea');
            if (fresh) { fresh.focus(); }
        });

        // the graph was replaced (undo / redo / load): notes and selection start from it
        window.addEventListener("partnerly:notes-changed", function () {
            notes = builder.internals.getNotes();
            renderNotes();
            clearMulti();
        });

        // =================================================================
        //  Map + card search
        // =================================================================
        var nav = make("div", "automation-mapbox");
        nav.hidden = true;
        var navSearch = make("input", "automation-mapbox-search");
        navSearch.type = "search";
        navSearch.placeholder = tt("navSearch");
        navSearch.setAttribute("aria-label", tt("navSearch"));
        var navList = make("div", "automation-mapbox-list");
        var mapCanvas = make("canvas", "automation-mapbox-map");
        nav.appendChild(navSearch);
        nav.appendChild(navList);
        nav.appendChild(mapCanvas);
        wrap.appendChild(nav);
        ["mousedown", "pointerdown", "touchstart", "wheel"].forEach(function (name) {
            nav.addEventListener(name, function (event) { event.stopPropagation(); }, { passive: true });
        });
        var navButton = zoomButton("bx-map-alt", tt("navTitle"), "data-zoom-nav");

        var MAP_W = 232, MAP_H = 132;
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        mapCanvas.width = MAP_W * dpr;
        mapCanvas.height = MAP_H * dpr;
        mapCanvas.style.width = MAP_W + "px";
        mapCanvas.style.height = MAP_H + "px";
        var mapCtx = mapCanvas.getContext("2d");
        var mapView = null;
        var lastSignature = "";

        function viewRect() {
            var area = root.getBoundingClientRect();
            var a = toWorld(area.left, area.top), b = toWorld(area.right, area.bottom);
            return { x: a.x, y: a.y, w: b.x - a.x, h: b.y - a.y };
        }
        function drawMap() {
            var ids = allIds();
            var items = ids.map(function (id) {
                var b = box(id);
                var data = live()[id].data || {};
                b.kind = data.kind; b.off = !!data.disabled;
                return b;
            });
            var view = viewRect();
            var signature = JSON.stringify([items.map(function (b) { return [b.x, b.y, b.off]; }), view, multi, notes.length]);
            if (signature === lastSignature) { return; }
            lastSignature = signature;
            // While the map is being dragged its scale stays fixed; otherwise it would rescale under the
            // pointer on every move and the view would race away.
            if (!(mapDown && mapView)) {
                var minX = view.x, minY = view.y, maxX = view.x + view.w, maxY = view.y + view.h;
                items.concat(notes.map(function (n) { return { x: n.x, y: n.y, w: n.w || 200, h: n.h || 120, note: true }; })).forEach(function (b) {
                    minX = Math.min(minX, b.x); minY = Math.min(minY, b.y);
                    maxX = Math.max(maxX, b.x + b.w); maxY = Math.max(maxY, b.y + b.h);
                });
                var pad = 30;
                minX -= pad; minY -= pad; maxX += pad; maxY += pad;
                var fit = Math.min(MAP_W / (maxX - minX), MAP_H / (maxY - minY));
                mapView = {
                    scale: fit,
                    offX: (MAP_W - (maxX - minX) * fit) / 2 - minX * fit,
                    offY: (MAP_H - (maxY - minY) * fit) / 2 - minY * fit
                };
            }
            var scale = mapView.scale, offX = mapView.offX, offY = mapView.offY;
            var dark = document.documentElement.getAttribute("data-theme") === "dark" || document.documentElement.classList.contains("dark-style");
            mapCtx.setTransform(dpr, 0, 0, dpr, 0, 0);
            mapCtx.clearRect(0, 0, MAP_W, MAP_H);
            notes.forEach(function (n) {
                mapCtx.fillStyle = dark ? "rgba(250, 204, 21, 0.28)" : "rgba(250, 204, 21, 0.45)";
                mapCtx.fillRect(offX + n.x * scale, offY + n.y * scale, (n.w || 200) * scale, (n.h || 120) * scale);
            });
            items.forEach(function (b) {
                var picked = multi.indexOf(b.id) !== -1;
                mapCtx.fillStyle = b.off ? "#94a3b8" : (b.kind === "trigger" ? "#f59e0b" : "#6366f1");
                mapCtx.fillRect(offX + b.x * scale, offY + b.y * scale, Math.max(b.w * scale, 3), Math.max(b.h * scale, 2));
                if (picked) {
                    mapCtx.strokeStyle = "#0ea5e9";
                    mapCtx.lineWidth = 1.5;
                    mapCtx.strokeRect(offX + b.x * scale - 1, offY + b.y * scale - 1, b.w * scale + 2, b.h * scale + 2);
                }
            });
            mapCtx.strokeStyle = dark ? "#c4b5fd" : "#4338ca";
            mapCtx.lineWidth = 1.5;
            mapCtx.strokeRect(offX + view.x * scale, offY + view.y * scale, view.w * scale, view.h * scale);
        }
        function mapLoop() {
            if (nav.hidden) { return; }
            drawMap();
            window.requestAnimationFrame(mapLoop);
        }
        function mapPoint(event) {
            var rect = mapCanvas.getBoundingClientRect();
            return {
                x: (event.clientX - rect.left - mapView.offX) / mapView.scale,
                y: (event.clientY - rect.top - mapView.offY) / mapView.scale
            };
        }
        // Press inside the frame: the view is grabbed where you touched it and follows the pointer.
        // Press outside: the view glides there once, then follows.
        var mapDown = false;
        var mapGrab = { x: 0, y: 0 };
        mapCanvas.addEventListener("pointerdown", function (event) {
            if (!mapView) { return; }
            var p = mapPoint(event);
            var v = viewRect();
            var inside = p.x >= v.x && p.x <= v.x + v.w && p.y >= v.y && p.y <= v.y + v.h;
            mapGrab = inside ? { x: p.x - (v.x + v.w / 2), y: p.y - (v.y + v.h / 2) } : { x: 0, y: 0 };
            mapDown = true;
            mapCanvas.setPointerCapture(event.pointerId);
            if (!inside) { I.centerOn(p.x, p.y, true); }
        });
        mapCanvas.addEventListener("pointermove", function (event) {
            if (!mapDown || !mapView) { return; }
            var p = mapPoint(event);
            I.centerOn(p.x - mapGrab.x, p.y - mapGrab.y, false);
        });
        function mapRelease() { mapDown = false; lastSignature = ""; }
        mapCanvas.addEventListener("pointerup", mapRelease);
        mapCanvas.addEventListener("pointercancel", mapRelease);

        function renderResults() {
            var query = navSearch.value.trim().toLowerCase();
            navList.innerHTML = "";
            var cards = (builder.analyze ? builder.analyze().cards : []).filter(function (card) {
                var hay = ((card.name || "") + " " + (card.typeLabel || "")).toLowerCase();
                return !query || hay.indexOf(query) !== -1;
            }).slice(0, 7);
            if (!cards.length) {
                navList.appendChild(make("div", "automation-mapbox-empty", tt("navNone")));
                return;
            }
            cards.forEach(function (card) {
                var row = make("button", "automation-mapbox-item");
                row.type = "button";
                var icon = make("i", "bx " + (card.kind === "trigger" ? "bxs-bolt" : "bx-cube-alt"));
                icon.setAttribute("aria-hidden", "true");
                row.appendChild(icon);
                row.appendChild(make("strong", null, card.name || card.typeLabel));
                row.appendChild(make("span", null, card.typeLabel));
                row.addEventListener("click", function () { builder.focusCard(card.domId); });
                navList.appendChild(row);
            });
        }
        navSearch.addEventListener("input", renderResults);
        navSearch.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                var first = navList.querySelector(".automation-mapbox-item");
                if (first) { event.preventDefault(); first.click(); }
            } else if (event.key === "Escape") {
                closeNav();
            }
        });
        function openNav() {
            var arrangeWindow = document.querySelector("[data-arrange-panel]");
            if (arrangeWindow) { arrangeWindow.hidden = true; }
            nav.hidden = false;
            navButton.classList.add("is-on");
            lastSignature = "";
            renderResults();
            mapLoop();
            window.setTimeout(function () { navSearch.focus(); }, 30);
        }
        function closeNav() {
            nav.hidden = true;
            navButton.classList.remove("is-on");
        }
        navButton.addEventListener("click", function () { if (nav.hidden) { openNav(); } else { closeNav(); } });
        // the "tidy up" window uses the same corner: only one of them at a time
        document.addEventListener("click", function (event) {
            if (event.target.closest && event.target.closest("[data-zoom-arrange]")) { closeNav(); }
        });

        // =================================================================
        //  Check before Publish
        // =================================================================
        var allowPublish = false;
        function findProblems() {
            var cards = builder.analyze ? builder.analyze().cards : [];
            var problems = [];
            var active = cards.filter(function (card) { return !card.disabled; });
            if (cards.length && !active.some(function (card) { return card.kind === "trigger"; })) {
                problems.push({ text: tt("pubNoTrigger") });
            }
            active.forEach(function (card) {
                var name = card.name || card.typeLabel;
                if (card.missing.length) {
                    problems.push({ id: card.domId, text: name + ": " + tt("pubSetup") + " - " + card.missing.join(", ") });
                } else if (!card.wired) {
                    problems.push({ id: card.domId, text: name + ": " + tt("pubUnlinked") });
                }
            });
            return problems;
        }
        function askPublish(problems, button) {
            var overlay = make("div", "automation-create is-open");
            overlay.setAttribute("role", "alertdialog");
            overlay.setAttribute("aria-modal", "true");
            var card = make("div", "automation-create-card");
            card.appendChild(make("h2", null, tt("pubTitle")));
            card.appendChild(make("p", "automation-create-lead", tt("pubLead")));
            var list = make("ul", "automation-prepublish-list");
            problems.forEach(function (problem) {
                var item = make("li");
                item.appendChild(make("span", null, problem.text));
                if (problem.id != null) {
                    var show = make("button", "btn btn-sm btn-outline-primary", tt("pubShow"));
                    show.type = "button";
                    show.addEventListener("click", function () {
                        close();
                        builder.focusCard(problem.id);
                    });
                    item.appendChild(show);
                }
                list.appendChild(item);
            });
            card.appendChild(list);
            var actions = make("div", "automation-create-actions");
            var cancel = make("button", "btn btn-outline-secondary", tt("pubCancel"));
            cancel.type = "button";
            var anyway = make("button", "btn btn-primary", tt("pubAnyway"));
            anyway.type = "button";
            actions.appendChild(cancel);
            actions.appendChild(anyway);
            card.appendChild(actions);
            overlay.appendChild(card);
            document.body.appendChild(overlay);
            document.body.classList.add("is-automation-create-open");
            function close() {
                if (overlay.parentNode) { overlay.parentNode.removeChild(overlay); }
                document.body.classList.remove("is-automation-create-open");
            }
            cancel.addEventListener("click", close);
            overlay.addEventListener("click", function (event) { if (event.target === overlay) { close(); } });
            anyway.addEventListener("click", function () {
                close();
                allowPublish = true;
                button.click();
            });
            cancel.focus();
        }
        document.addEventListener("click", function (event) {
            var button = event.target.closest ? event.target.closest('[data-act="publish"]') : null;
            if (!button || button.disabled) { return; }
            if (allowPublish) { allowPublish = false; return; }
            var problems = findProblems();
            if (!problems.length) { return; }
            event.preventDefault();
            event.stopPropagation();
            askPublish(problems, button);
        }, true);

        // =================================================================
        //  A past run on the canvas
        // =================================================================
        var runBanner = null;
        function clearRunMarks() {
            Array.prototype.forEach.call(root.querySelectorAll(".auto-node.is-run-ok, .auto-node.is-run-failed, .auto-node.is-run-skipped, .auto-node.is-run-none, .auto-node.is-run-live"), function (el) {
                el.classList.remove("is-run-ok", "is-run-failed", "is-run-skipped", "is-run-none", "is-run-live");
                el.removeAttribute("data-run-note");
            });
            if (runBanner && runBanner.parentNode) { runBanner.parentNode.removeChild(runBanner); }
            runBanner = null;
        }
        function showRun(steps) {
            clearRunMarks();
            var byId = {};
            steps.forEach(function (step) { byId[step.nodeId] = step; });
            var home = editor.export().drawflow.Home.data;
            Object.keys(home).forEach(function (key) {
                var data = home[key].data || {};
                var inner = root.querySelector("#node-" + home[key].id + " .auto-node");
                if (!inner) { return; }
                var step = byId[data.id];
                if (!step && data.kind === "trigger" && !data.disabled) {
                    // triggers are not steps: a run exists because one of them fired
                    inner.classList.add("is-run-ok");
                    inner.setAttribute("data-run-note", tt("runOk"));
                    return;
                }
                if (!step) { inner.classList.add("is-run-none"); inner.setAttribute("data-run-note", tt("runNotRun")); return; }
                var status = String(step.status || "");
                if (status === "succeeded") { inner.classList.add("is-run-ok"); inner.setAttribute("data-run-note", tt("runOk")); }
                else if (status === "failed") { inner.classList.add("is-run-failed"); inner.setAttribute("data-run-note", step.error ? tt("runFailed") + ": " + step.error : tt("runFailed")); }
                else if (status === "skipped") { inner.classList.add("is-run-skipped"); inner.setAttribute("data-run-note", tt("runSkipped")); }
                else { inner.classList.add("is-run-live"); inner.setAttribute("data-run-note", status); }
            });
            runBanner = make("div", "automation-run-banner");
            runBanner.appendChild(make("span", null, tt("runBanner")));
            var hide = make("button", "automation-run-banner-btn", tt("runClear"));
            hide.type = "button";
            hide.addEventListener("click", clearRunMarks);
            runBanner.appendChild(hide);
            wrap.appendChild(runBanner);
        }
        window.addEventListener("partnerly:show-run", function (event) {
            showRun((event.detail && event.detail.steps) || []);
        });
        document.addEventListener("click", function (event) {
            var button = event.target.closest ? event.target.closest("[data-show-run]") : null;
            if (!button) { return; }
            var steps = [];
            try { steps = JSON.parse(button.getAttribute("data-run-steps") || "[]"); } catch (e) { steps = []; }
            // close the History pop-ups so the canvas is visible
            Array.prototype.forEach.call(document.querySelectorAll(".automation-hub-overlay:not([hidden]) [data-automation-hub-close]"), function (closer) { closer.click(); });
            window.dispatchEvent(new CustomEvent("partnerly:show-run", { detail: { steps: steps } }));
        });
    }
})();
