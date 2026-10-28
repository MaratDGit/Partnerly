(function () {
    if (window.__partnerlyInspectorObserve) {
        return;
    }
    window.__partnerlyInspectorObserve = true;

    const MAX = {
        links: 250,
        images: 150,
        forms: 60,
        inputs: 200,
        buttons: 150,
        headings: 80,
        events: 25,
        tags: 40
    };

    let mutations = 0;
    let timer = 0;
    let lastKey = "";
    const events = [];

    function count(selector) {
        return document.querySelectorAll(selector).length;
    }

    function take(nodeList, map, max) {
        const out = [];
        const len = nodeList.length;
        for (let i = 0; i < len && out.length < max; i++) {
            out.push(map(nodeList[i], i));
        }
        return out;
    }

    function textOf(el, max) {
        return String((el && (el.innerText || el.textContent)) || "").replace(/\s+/g, " ").trim().slice(0, max || 160);
    }

    function collect() {
        const inputs = document.querySelectorAll("input, textarea, select");
        const tags = {};
        const nodes = document.querySelectorAll("*");
        for (let i = 0; i < nodes.length; i++) {
            const name = nodes[i].tagName ? nodes[i].tagName.toLowerCase() : "?";
            tags[name] = (tags[name] || 0) + 1;
        }
        const tagList = Object.keys(tags)
            .sort(function (a, b) { return tags[b] - tags[a]; })
            .slice(0, MAX.tags)
            .map(function (name) { return { name: name, count: tags[name] }; });

        const frameUrl = location.href;
        function stamp(item) {
            item.frame = frameUrl;
            return item;
        }

        return {
            links: take(document.querySelectorAll("a[href]"), function (a) {
                return stamp({ href: a.href || "", text: textOf(a, 140) });
            }, MAX.links),
            images: take(document.querySelectorAll("img"), function (img) {
                return stamp({
                    src: img.currentSrc || img.src || "",
                    alt: String(img.alt || "").slice(0, 120)
                });
            }, MAX.images),
            forms: take(document.querySelectorAll("form"), function (form) {
                const fields = [];
                const els = form.querySelectorAll("input, textarea, select");
                for (let i = 0; i < els.length && fields.length < 30; i++) {
                    fields.push(els[i].name || els[i].id || (els[i].type || els[i].tagName));
                }
                return stamp({
                    action: form.action || "",
                    method: (form.method || "get").toUpperCase(),
                    fields: fields
                });
            }, MAX.forms),
            inputs: take(inputs, function (el) {
                const type = String(el.type || el.tagName).toLowerCase();
                const secret = type === "password" || type === "hidden";
                return stamp({
                    tag: el.tagName.toLowerCase(),
                    type: type,
                    name: el.name || "",
                    id: el.id || "",
                    placeholder: String(el.placeholder || "").slice(0, 80),
                    value: secret ? "••••" : String(el.value || "").slice(0, 80)
                });
            }, MAX.inputs),
            buttons: take(document.querySelectorAll("button, [type=button], [type=submit]"), function (el) {
                return stamp({
                    type: String(el.type || "button"),
                    text: textOf(el, 120),
                    disabled: !!el.disabled
                });
            }, MAX.buttons),
            headings: take(document.querySelectorAll("h1, h2, h3, h4, h5, h6"), function (el) {
                return stamp({ level: el.tagName.toLowerCase(), text: textOf(el, 200) });
            }, MAX.headings),
            tags: tagList
        };
    }

    function listIframes() {
        const nodes = document.querySelectorAll("iframe, frame");
        const out = [];
        for (let i = 0; i < nodes.length && out.length < 80; i++) {
            const el = nodes[i];
            let accessible = false;
            let innerUrl = "";
            try {
                const doc = el.contentDocument;
                if (doc) {
                    accessible = true;
                    innerUrl = (doc.location && doc.location.href) || "";
                }
            } catch (e) {
                accessible = false;
            }
            out.push({
                src: el.src || el.getAttribute("src") || "",
                name: el.name || el.id || "",
                accessible: accessible,
                innerUrl: innerUrl
            });
        }
        return out;
    }

    function collectSameOriginFrameDocs() {
        if (window !== window.top) {
            return [];
        }
        const nodes = document.querySelectorAll("iframe, frame");
        const out = [];
        for (let i = 0; i < nodes.length && out.length < 40; i++) {
            const el = nodes[i];
            try {
                const doc = el.contentDocument;
                if (!doc || !doc.documentElement || doc === document) {
                    continue;
                }
                out.push({
                    url: (doc.location && doc.location.href) || el.src || "",
                    src: el.src || "",
                    html: String(doc.documentElement.outerHTML || "").slice(0, 400000),
                    text: String((doc.body && doc.body.innerText) || "").slice(0, 50000)
                });
            } catch (e) {
                /* cross-origin */
            }
        }
        return out;
    }

    function snapshot(reason) {
        mutations += 1;
        events.push({ reason: reason || "mutation", at: Date.now() });
        if (events.length > MAX.events) {
            events.shift();
        }

        const html = (document.documentElement && document.documentElement.outerHTML) || "";
        const text = (document.body && document.body.innerText) || "";
        const details = collect();
        const payload = {
            type: "SNAPSHOT",
            reason: reason || "mutation",
            url: location.href,
            title: document.title || "",
            readyState: document.readyState,
            mutations: mutations,
            at: Date.now(),
            frame: {
                isTop: window === window.top,
                url: location.href,
                name: window.name || ""
            },
            iframes: listIframes(),
            inlineFrames: collectSameOriginFrameDocs(),
            counts: {
                nodes: count("*"),
                links: count("a[href]"),
                images: count("img"),
                forms: count("form"),
                inputs: count("input, textarea, select"),
                buttons: count("button, [type=button], [type=submit]"),
                headings: count("h1, h2, h3, h4, h5, h6"),
                scripts: count("script")
            },
            details: details,
            events: events.slice(),
            text: text.slice(0, 120000),
            html: html.slice(0, 400000)
        };
        const key = payload.url + "|" + payload.title + "|" + payload.counts.nodes + "|" + payload.text.length + "|" + payload.reason;
        if (key === lastKey && reason === "mutation") {
            return;
        }
        lastKey = key;
        chrome.runtime.sendMessage(payload).catch(function () { /* background asleep */ });
    }

    function schedule(reason) {
        clearTimeout(timer);
        timer = setTimeout(function () {
            snapshot(reason);
        }, 140);
    }

    const observer = new MutationObserver(function () {
        schedule("mutation");
    });
    observer.observe(document.documentElement, {
        subtree: true,
        childList: true,
        attributes: true,
        characterData: true
    });

    ["click", "input", "change", "keyup"].forEach(function (name) {
        document.addEventListener(name, function () {
            schedule(name);
        }, true);
    });

    window.addEventListener("hashchange", function () {
        snapshot("hash");
    });
    window.addEventListener("popstate", function () {
        snapshot("popstate");
    });
    window.addEventListener("load", function () {
        snapshot("load");
    });

    try {
        const push = history.pushState;
        const replace = history.replaceState;
        history.pushState = function () {
            const result = push.apply(this, arguments);
            snapshot("pushState");
            return result;
        };
        history.replaceState = function () {
            const result = replace.apply(this, arguments);
            snapshot("replaceState");
            return result;
        };
    } catch (e) {
        /* some sites lock history */
    }

    snapshot("ready");

    if (window === window.top) {
        document.addEventListener("load", function (event) {
            const el = event.target;
            if (!el || !el.tagName) {
                return;
            }
            const tag = el.tagName.toUpperCase();
            if (tag === "IFRAME" || tag === "FRAME") {
                chrome.runtime.sendMessage({ type: "REINJECT" }).catch(function () { /* ignore */ });
                schedule("iframe");
            }
        }, true);
    }
})();
