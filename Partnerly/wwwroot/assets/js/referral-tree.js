(function () {
    const NODE_W = 228;
    const NODE_H = 86;
    const GAP_X = 36;
    const GAP_Y = 92;
    const MIN_SCALE = 0.35;
    const MAX_SCALE = 1.8;

    const stage = document.querySelector("[data-tree-stage]");
    const viewport = document.querySelector("[data-tree-viewport]");
    const linksSvg = document.querySelector("[data-tree-links]");
    const nodesEl = document.querySelector("[data-tree-nodes]");
    const emptyEl = document.querySelector("[data-tree-empty]");
    if (!stage || !viewport || !linksSvg || !nodesEl) {
        return;
    }

    const i18n = window.referralTreeTranslations || {};
    const currentUserId = window.referralTreeCurrentUserId;

    function normalizeNode(node) {
        if (!node) return null;
        return {
            id: node.id || node.Id,
            displayName: node.displayName || node.DisplayName || "",
            referralCode: node.referralCode || node.ReferralCode || "",
            photoUrl: node.photoUrl || node.PhotoUrl || "/assets/img/profiles/default-user.png",
            roleName: node.roleName || node.RoleName || "",
            directCount: node.directCount ?? node.DirectCount ?? 0,
            totalCount: node.totalCount ?? node.TotalCount ?? 1,
            level: node.level ?? node.Level ?? 0,
            isBlocked: node.isBlocked ?? node.IsBlocked ?? false,
            isOnline: node.isOnline ?? node.IsOnlayn ?? node.IsOnline ?? false,
            createdDate: node.createdDate || node.CreatedDate || null,
            children: (node.children || node.Children || []).map(normalizeNode)
        };
    }

    let tree = normalizeNode(window.referralTreeData);
    let collapsed = new Set();
    let selectedId = tree ? tree.id : null;
    let searchTerm = "";
    let transform = { x: 40, y: 40, scale: 1 };
    let dragging = false;
    let dragStart = { x: 0, y: 0, tx: 0, ty: 0 };
    let pointers = new Map();

    function escapeHtml(value) {
        return String(value ?? "").replace(/[&<>"']/g, function (ch) {
            return ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[ch];
        });
    }

    function walk(node, fn, parent) {
        if (!node) return;
        fn(node, parent);
        (node.children || []).forEach(function (child) {
            walk(child, fn, node);
        });
    }

    function visibleChildren(node) {
        if (!node || collapsed.has(node.id)) return [];
        return node.children || [];
    }

    function measure(node) {
        const kids = visibleChildren(node);
        if (!kids.length) {
            node._width = NODE_W;
            return NODE_W;
        }
        let width = 0;
        kids.forEach(function (child) {
            width += measure(child) + GAP_X;
        });
        width -= GAP_X;
        node._width = Math.max(NODE_W, width);
        return node._width;
    }

    function place(node, x, y) {
        node._x = x + (node._width - NODE_W) / 2;
        node._y = y;
        const kids = visibleChildren(node);
        let childX = x;
        kids.forEach(function (child) {
            place(child, childX, y + NODE_H + GAP_Y);
            childX += child._width + GAP_X;
        });
    }

    function collectLayout(node, acc) {
        acc.nodes.push(node);
        acc.maxX = Math.max(acc.maxX, node._x + NODE_W);
        acc.maxY = Math.max(acc.maxY, node._y + NODE_H);
        visibleChildren(node).forEach(function (child) {
            acc.links.push({ from: node, to: child });
            collectLayout(child, acc);
        });
        return acc;
    }

    function matchesSearch(node) {
        if (!searchTerm) return false;
        const hay = ((node.displayName || "") + " " + (node.referralCode || "")).toLowerCase();
        return hay.includes(searchTerm);
    }

    function buildKeepIds() {
        const keep = new Set();
        if (!tree || (!selectedId && !searchTerm)) return keep;

        const parentOf = new Map();
        walk(tree, function (node, parent) {
            if (parent) parentOf.set(node.id, parent);
        });

        function addAncestors(id) {
            let current = parentOf.get(id);
            while (current) {
                keep.add(current.id);
                current = parentOf.get(current.id);
            }
        }

        if (selectedId) {
            walk(tree, function (node) {
                if (node.id === selectedId) {
                    keep.add(node.id);
                    addAncestors(node.id);
                    walk(node, function (descendant) {
                        keep.add(descendant.id);
                    });
                }
            });
        }

        if (searchTerm) {
            walk(tree, function (node) {
                if (matchesSearch(node)) {
                    keep.add(node.id);
                    addAncestors(node.id);
                    walk(node, function (descendant) {
                        keep.add(descendant.id);
                    });
                }
            });
        }

        return keep;
    }

    function applyTransform() {
        viewport.style.transform =
            "translate(" + transform.x + "px, " + transform.y + "px) scale(" + transform.scale + ")";
    }

    function bezier(from, to) {
        const x1 = from._x + NODE_W / 2;
        const y1 = from._y + NODE_H;
        const x2 = to._x + NODE_W / 2;
        const y2 = to._y;
        const midY = (y1 + y2) / 2;
        return "M " + x1 + " " + y1 + " C " + x1 + " " + midY + ", " + x2 + " " + midY + ", " + x2 + " " + y2;
    }

    function updateStats() {
        if (!tree) return;
        let levels = 1;
        walk(tree, function (node) {
            levels = Math.max(levels, (node.level || 0) + 1);
        });
        const network = document.querySelector('[data-stat="network"]');
        const direct = document.querySelector('[data-stat="direct"]');
        const levelsEl = document.querySelector('[data-stat="levels"]');
        if (network) network.textContent = tree.totalCount || 1;
        if (direct) direct.textContent = tree.directCount || 0;
        if (levelsEl) levelsEl.textContent = levels;
    }

    function defaultCollapse() {
        collapsed = new Set();
        if (!tree || (tree.totalCount || 0) <= 18) return;
        walk(tree, function (node) {
            if ((node.level || 0) >= 2 && (node.children || []).length) {
                collapsed.add(node.id);
            }
        });
    }

    function render() {
        nodesEl.innerHTML = "";
        linksSvg.innerHTML = "";
        if (!tree) {
            emptyEl.hidden = false;
            const solo = document.querySelector("[data-tree-solo]");
            if (solo) solo.hidden = true;
            return;
        }

        emptyEl.hidden = true;
        const solo = document.querySelector("[data-tree-solo]");
        if (solo) solo.hidden = (tree.directCount || 0) > 0;
        measure(tree);
        place(tree, 24, 24);
        const layout = collectLayout(tree, { nodes: [], links: [], maxX: 0, maxY: 0 });
        const svgW = Math.max(layout.maxX + 80, 800);
        const svgH = Math.max(layout.maxY + 80, 400);
        linksSvg.setAttribute("width", svgW);
        linksSvg.setAttribute("height", svgH);
        linksSvg.setAttribute("viewBox", "0 0 " + svgW + " " + svgH);

        const defs = document.createElementNS("http://www.w3.org/2000/svg", "defs");
        defs.innerHTML =
            '<linearGradient id="referralTreeStroke" x1="0" y1="0" x2="0" y2="1">' +
            '<stop offset="0%" stop-color="#696cff" stop-opacity="0.85"/>' +
            '<stop offset="100%" stop-color="#71dd37" stop-opacity="0.7"/>' +
            "</linearGradient>";
        linksSvg.appendChild(defs);

        const keepIds = buildKeepIds();
        const highlightIds = new Set();
        if (selectedId) {
            const parentOf = new Map();
            walk(tree, function (node, parent) {
                if (parent) parentOf.set(node.id, parent);
            });
            let current = layout.nodes.find(function (n) { return n.id === selectedId; });
            while (current) {
                highlightIds.add(current.id);
                current = parentOf.get(current.id);
            }
        }

        layout.links.forEach(function (link) {
            const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("d", bezier(link.from, link.to));
            const active = highlightIds.size
                ? highlightIds.has(link.from.id) && highlightIds.has(link.to.id)
                : false;
            if (keepIds.size && !keepIds.has(link.to.id)) path.classList.add("is-dim");
            if (active) path.classList.add("is-active");
            linksSvg.appendChild(path);
        });

        layout.nodes.forEach(function (node) {
            const el = document.createElement("div");
            el.className = "referral-tree-node";
            el.style.left = node._x + "px";
            el.style.top = node._y + "px";
            if ((node.level || 0) === 0) el.classList.add("is-root");
            if (selectedId === node.id) el.classList.add("is-active");
            if (matchesSearch(node)) el.classList.add("is-match");
            if (keepIds.size && !keepIds.has(node.id)) {
                el.classList.add("is-dim");
            }

            const statusClass = node.isBlocked
                ? "is-blocked"
                : (node.isOnline ? "is-online" : "");
            const childCount = (node.children || []).length;
            const role = node.roleName
                ? '<span class="referral-tree-role">' + escapeHtml(node.roleName) + "</span>"
                : "";

            el.innerHTML =
                '<div class="referral-tree-avatar">' +
                    '<img src="' + escapeHtml(node.photoUrl) + '" alt="">' +
                    '<span class="referral-tree-status ' + statusClass + '" title="' +
                        escapeHtml(node.isBlocked ? i18n.blocked : (node.isOnline ? i18n.online : "")) +
                    '"></span>' +
                "</div>" +
                '<div class="referral-tree-meta">' +
                    '<span class="referral-tree-name" title="' + escapeHtml(node.displayName) + '">' +
                        escapeHtml(node.displayName) +
                    "</span>" +
                    '<span class="referral-tree-code">' + escapeHtml(node.referralCode || "") + "</span>" +
                    role +
                "</div>" +
                (childCount
                    ? '<button type="button" class="referral-tree-toggle" data-toggle="' + node.id + '">' +
                        (collapsed.has(node.id) ? "+" + childCount : childCount) +
                      "</button>"
                    : "");

            el.addEventListener("click", function (event) {
                if (event.target.closest("[data-toggle]")) return;
                selectedId = node.id;
                render();
            });

            const toggle = el.querySelector("[data-toggle]");
            if (toggle) {
                toggle.addEventListener("click", function (event) {
                    event.stopPropagation();
                    if (collapsed.has(node.id)) collapsed.delete(node.id);
                    else collapsed.add(node.id);
                    render();
                });
            }

            nodesEl.appendChild(el);
        });

        updateStats();
    }

    function fitView() {
        if (!tree) return;
        measure(tree);
        place(tree, 24, 24);
        const layout = collectLayout(tree, { nodes: [], links: [], maxX: 0, maxY: 0 });
        const stageRect = stage.getBoundingClientRect();
        const contentW = Math.max(layout.maxX + 48, NODE_W);
        const contentH = Math.max(layout.maxY + 48, NODE_H);
        const scale = Math.min(
            MAX_SCALE,
            Math.max(MIN_SCALE, Math.min((stageRect.width - 48) / contentW, (stageRect.height - 48) / contentH))
        );
        transform.scale = scale;
        transform.x = (stageRect.width - contentW * scale) / 2;
        transform.y = Math.max(24, (stageRect.height - contentH * scale) / 5);
        applyTransform();
    }

    function zoomBy(delta, originX, originY) {
        const prev = transform.scale;
        const next = Math.min(MAX_SCALE, Math.max(MIN_SCALE, prev * delta));
        if (next === prev) return;
        const rect = stage.getBoundingClientRect();
        const cx = (originX ?? (rect.left + rect.width / 2)) - rect.left;
        const cy = (originY ?? (rect.top + rect.height / 2)) - rect.top;
        const wx = (cx - transform.x) / prev;
        const wy = (cy - transform.y) / prev;
        transform.scale = next;
        transform.x = cx - wx * next;
        transform.y = cy - wy * next;
        applyTransform();
    }

    async function loadTree(userId, updateUrl) {
        const url = userId
            ? "/Dashboard/GetReferralTree?userId=" + encodeURIComponent(userId)
            : "/Dashboard/GetReferralTree";
        try {
            const response = await fetch(url, { headers: { Accept: "application/json" } });
            if (!response.ok) throw new Error("load");
            const payload = await response.json();
            tree = normalizeNode(payload.tree || payload.Tree || null);
            selectedId = tree ? tree.id : null;
            defaultCollapse();
            const lookup = document.getElementById("referralTreeUserLookup");
            const hidden = document.getElementById("referralTreeUserId");
            if (tree && lookup) lookup.value = tree.displayName;
            if (tree && hidden) hidden.value = tree.id;
            render();
            fitView();
            if (updateUrl) {
                const next = userId
                    ? "/Dashboard/ReferralTree?userId=" + encodeURIComponent(userId)
                    : "/Dashboard/ReferralTree";
                history.replaceState(null, "", next);
            }
        } catch (e) {
            emptyEl.hidden = false;
            emptyEl.querySelector("p").textContent = i18n.loadError || "Could not load the tree.";
        }
    }

    stage.addEventListener("pointerdown", function (event) {
        if (event.target.closest(".referral-tree-node")) return;
        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        dragging = pointers.size === 1;
        dragStart = { x: event.clientX, y: event.clientY, tx: transform.x, ty: transform.y };
        stage.setPointerCapture(event.pointerId);
    });

    stage.addEventListener("pointermove", function (event) {
        if (!pointers.has(event.pointerId)) return;
        pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
        if (pointers.size === 2) {
            const pts = Array.from(pointers.values());
            const dist = Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y);
            if (!stage._pinch) stage._pinch = dist;
            zoomBy(dist / stage._pinch, (pts[0].x + pts[1].x) / 2, (pts[0].y + pts[1].y) / 2);
            stage._pinch = dist;
            return;
        }
        if (!dragging) return;
        transform.x = dragStart.tx + (event.clientX - dragStart.x);
        transform.y = dragStart.ty + (event.clientY - dragStart.y);
        applyTransform();
    });

    function endPointer(event) {
        pointers.delete(event.pointerId);
        dragging = false;
        stage._pinch = null;
    }

    stage.addEventListener("pointerup", endPointer);
    stage.addEventListener("pointercancel", endPointer);

    stage.addEventListener("wheel", function (event) {
        event.preventDefault();
        zoomBy(event.deltaY < 0 ? 1.1 : 0.9, event.clientX, event.clientY);
    }, { passive: false });

    document.querySelectorAll("[data-tree-action]").forEach(function (button) {
        button.addEventListener("click", function () {
            const action = button.getAttribute("data-tree-action");
            if (action === "zoom-in") zoomBy(1.15);
            if (action === "zoom-out") zoomBy(0.87);
            if (action === "fit") fitView();
            if (action === "expand") {
                collapsed.clear();
                render();
                fitView();
            }
            if (action === "collapse") {
                if (!tree) return;
                walk(tree, function (node) {
                    if ((node.children || []).length) collapsed.add(node.id);
                });
                collapsed.delete(tree.id);
                render();
                fitView();
            }
            if (action === "me") loadTree(currentUserId, true);
        });
    });

    const searchInput = document.getElementById("referralTreeSearch");
    if (searchInput) {
        searchInput.addEventListener("input", function () {
            searchTerm = searchInput.value.trim().toLowerCase();
            if (searchTerm) {
                walk(tree, function (node) {
                    let hasMatch = false;
                    walk(node, function (current) {
                        if (matchesSearch(current)) hasMatch = true;
                    });
                    if (hasMatch) collapsed.delete(node.id);
                });
            }
            render();
        });
    }

    const hiddenUser = document.getElementById("referralTreeUserId");
    if (hiddenUser) {
        hiddenUser.addEventListener("change", function () {
            if (hiddenUser.value) loadTree(hiddenUser.value, true);
        });
    }

    stage.addEventListener("keydown", function (event) {
        if (event.key === "+" || event.key === "=") zoomBy(1.15);
        if (event.key === "-" || event.key === "_") zoomBy(0.87);
        if (event.key === "0") fitView();
    });

    defaultCollapse();
    render();
    requestAnimationFrame(fitView);
    window.addEventListener("resize", function () {
        fitView();
    });
    window.addEventListener("partnerly-layout-change", function () {
        fitView();
    });
})();
