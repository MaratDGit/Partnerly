const SESSION_KEY = "partnerlyInspector";
const MAX_HTML = 1500000;
const MAX_TEXT = 120000;
const MAX_LIST = 400;

const frameCache = {};
const mergeTimers = {};

async function readState() {
    const stored = await chrome.storage.session.get(SESSION_KEY);
    return stored[SESSION_KEY] || {};
}

async function writeState(patch) {
    const current = await readState();
    const next = Object.assign({}, current, patch);
    await chrome.storage.session.set({ [SESSION_KEY]: next });
    return next;
}

async function clearWatch() {
    const state = await readState();
    if (state.watchTabId) {
        delete frameCache[state.watchTabId];
        clearTimeout(mergeTimers[state.watchTabId]);
        delete mergeTimers[state.watchTabId];
    }
    await writeState({
        watchTabId: null,
        watchWindowId: null
    });
    if (state.partnerlyTabId) {
        safeSend(state.partnerlyTabId, { type: "DETACHED" });
    }
}

function safeSend(tabId, message) {
    if (!tabId) {
        return;
    }
    chrome.tabs.sendMessage(tabId, message).catch(function () { /* tab gone */ });
}

async function injectObserver(tabId, frameId) {
    if (!tabId) {
        return;
    }
    if (typeof frameId === "number") {
        try {
            await chrome.scripting.executeScript({
                target: { tabId: tabId, frameIds: [frameId] },
                files: ["observe.js"]
            });
        } catch (e) {
            /* chrome://, sandboxed or opaque frames */
        }
        return;
    }

    let frames = [{ frameId: 0 }];
    try {
        frames = await chrome.webNavigation.getAllFrames({ tabId: tabId }) || frames;
    } catch (e) {
        /* webNavigation not ready */
    }
    for (let i = 0; i < frames.length; i++) {
        try {
            await chrome.scripting.executeScript({
                target: { tabId: tabId, frameIds: [frames[i].frameId] },
                files: ["observe.js"]
            });
        } catch (e) {
            /* skip blocked frame */
        }
    }
}

function takeList(list) {
    return (list || []).slice(0, MAX_LIST);
}

function mergeFrames(tabId) {
    const map = frameCache[tabId] || {};
    const payloads = Object.keys(map).map(function (id) {
        return Object.assign({}, map[id], { frameId: Number(id) });
    });
    if (!payloads.length) {
        return null;
    }
    payloads.sort(function (a, b) {
        const aTop = a.frame && a.frame.isTop ? 1 : 0;
        const bTop = b.frame && b.frame.isTop ? 1 : 0;
        return bTop - aTop || a.frameId - b.frameId;
    });
    const top = payloads[0];
    const usedUrls = {};
    const htmlParts = [];
    const textParts = [];
    const events = [];
    const iframeMeta = [];
    const details = {
        links: [],
        images: [],
        forms: [],
        inputs: [],
        buttons: [],
        headings: [],
        tags: {}
    };
    const counts = {
        nodes: 0,
        links: 0,
        images: 0,
        forms: 0,
        inputs: 0,
        buttons: 0,
        headings: 0,
        scripts: 0,
        frames: 0
    };

    function consume(payload) {
        if (!payload) {
            return;
        }
        const url = (payload.frame && payload.frame.url) || payload.url || "";
        const isTop = !!(payload.frame && payload.frame.isTop);
        usedUrls[url] = true;
        counts.frames += 1;
        const c = payload.counts || {};
        counts.nodes += c.nodes || 0;
        counts.links += c.links || 0;
        counts.images += c.images || 0;
        counts.forms += c.forms || 0;
        counts.inputs += c.inputs || 0;
        counts.buttons += c.buttons || 0;
        counts.headings += c.headings || 0;
        counts.scripts += c.scripts || 0;
        const d = payload.details || {};
        details.links = details.links.concat(d.links || []);
        details.images = details.images.concat(d.images || []);
        details.forms = details.forms.concat(d.forms || []);
        details.inputs = details.inputs.concat(d.inputs || []);
        details.buttons = details.buttons.concat(d.buttons || []);
        details.headings = details.headings.concat(d.headings || []);
        (d.tags || []).forEach(function (tag) {
            details.tags[tag.name] = (details.tags[tag.name] || 0) + (tag.count || 0);
        });
        (payload.events || []).forEach(function (item) {
            events.push(item);
        });
        (payload.iframes || []).forEach(function (item) {
            iframeMeta.push(item);
        });
        if (payload.html) {
            htmlParts.push(isTop
                ? payload.html
                : "\n<!-- ===== IFRAME " + url + " ===== -->\n" + payload.html);
        }
        if (payload.text) {
            textParts.push(isTop
                ? payload.text
                : "\n--- iframe " + url + " ---\n" + payload.text);
        }
    }

    payloads.forEach(consume);
    (top.inlineFrames || []).forEach(function (frame) {
        if (usedUrls[frame.url]) {
            return;
        }
        consume({
            frame: { isTop: false, url: frame.url },
            url: frame.url,
            html: frame.html,
            text: frame.text,
            counts: {},
            details: {},
            events: [],
            iframes: []
        });
    });

    const tagList = Object.keys(details.tags)
        .sort(function (a, b) { return details.tags[b] - details.tags[a]; })
        .slice(0, 40)
        .map(function (name) { return { name: name, count: details.tags[name] }; });

    let html = htmlParts.join("\n");
    if (html.length > MAX_HTML) {
        html = html.slice(0, MAX_HTML);
    }
    let text = textParts.join("\n");
    if (text.length > MAX_TEXT) {
        text = text.slice(0, MAX_TEXT);
    }

    return {
        type: "SNAPSHOT",
        reason: top.reason,
        url: top.url,
        title: top.title,
        readyState: top.readyState,
        mutations: top.mutations,
        at: Date.now(),
        counts: counts,
        details: {
            links: takeList(details.links),
            images: takeList(details.images),
            forms: takeList(details.forms),
            inputs: takeList(details.inputs),
            buttons: takeList(details.buttons),
            headings: takeList(details.headings),
            tags: tagList,
            iframes: iframeMeta
        },
        events: events.slice(-40),
        frames: payloads.map(function (payload) {
            return {
                url: (payload.frame && payload.frame.url) || payload.url || "",
                isTop: !!(payload.frame && payload.frame.isTop)
            };
        }),
        text: text,
        html: html
    };
}

function queueMerge(tabId, partnerlyTabId) {
    clearTimeout(mergeTimers[tabId]);
    mergeTimers[tabId] = setTimeout(function () {
        const merged = mergeFrames(tabId);
        if (merged) {
            safeSend(partnerlyTabId, merged);
        }
    }, 80);
}

function clampBounds(bounds) {
    if (!bounds) {
        return null;
    }
    return {
        left: Math.round(Number(bounds.left) || 0),
        top: Math.round(Number(bounds.top) || 0),
        width: Math.max(400, Math.round(Number(bounds.width) || 800)),
        height: Math.max(400, Math.round(Number(bounds.height) || 600)),
        focused: bounds.focused === true
    };
}

async function applyBounds(windowId, bounds) {
    const next = clampBounds(bounds);
    if (!windowId || !next) {
        return;
    }
    const update = {
        left: next.left,
        top: next.top,
        width: next.width,
        height: next.height,
        state: "normal"
    };
    if (next.focused) {
        update.focused = true;
    }
    try {
        await chrome.windows.update(windowId, update);
    } catch (e) {
        /* window closed */
    }
}

async function layoutWindows(layout) {
    const state = await readState();
    if (layout && layout.partnerly && state.partnerlyWindowId) {
        await applyBounds(state.partnerlyWindowId, Object.assign({}, layout.partnerly, { focused: false }));
    }
    if (layout && layout.site && state.watchWindowId) {
        await applyBounds(state.watchWindowId, Object.assign({}, layout.site, { focused: true }));
    }
}

async function openAndWatch(url, layout, sender) {
    const partnerlyTabId = sender && sender.tab ? sender.tab.id : null;
    const partnerlyWindowId = sender && sender.tab ? sender.tab.windowId : null;
    let state = await writeState({
        partnerlyTabId: partnerlyTabId,
        partnerlyWindowId: partnerlyWindowId
    });

    if (layout && layout.partnerly && partnerlyWindowId) {
        await applyBounds(partnerlyWindowId, Object.assign({}, layout.partnerly, { focused: false }));
    }

    const siteBounds = clampBounds(layout && layout.site) || {
        left: 0,
        top: 0,
        width: 900,
        height: 800
    };

    if (state.watchWindowId) {
        try {
            await chrome.windows.get(state.watchWindowId);
            await applyBounds(state.watchWindowId, Object.assign({}, siteBounds, { focused: true }));
            const tabs = await chrome.tabs.query({ windowId: state.watchWindowId });
            const tab = tabs && tabs[0];
            if (tab && tab.id) {
                await chrome.tabs.update(tab.id, { url: url });
                await writeState({ watchTabId: tab.id });
                await injectObserver(tab.id);
                return { ok: true, windowId: state.watchWindowId, tabId: tab.id };
            }
        } catch (e) {
            await writeState({ watchWindowId: null, watchTabId: null });
        }
    }

    const created = await chrome.windows.create({
        url: url,
        type: "popup",
        focused: true,
        left: siteBounds.left,
        top: siteBounds.top,
        width: siteBounds.width,
        height: siteBounds.height
    });

    const tab = created.tabs && created.tabs[0];
    await writeState({
        watchWindowId: created.id,
        watchTabId: tab ? tab.id : null
    });
    if (tab && tab.id) {
        await injectObserver(tab.id);
    }
    return { ok: true, windowId: created.id, tabId: tab ? tab.id : null };
}

async function raiseWatchWindow() {
    const state = await readState();
    if (!state.watchWindowId) {
        return { ok: false };
    }
    try {
        await chrome.windows.update(state.watchWindowId, { focused: true, drawAttention: true });
        return { ok: true };
    } catch (e) {
        return { ok: false };
    }
}

chrome.runtime.onMessage.addListener(function (message, sender, sendResponse) {
    const type = message && message.type;
    if (type === "PING") {
        writeState({
            partnerlyTabId: sender.tab ? sender.tab.id : null,
            partnerlyWindowId: sender.tab ? sender.tab.windowId : null
        }).then(function () {
            sendResponse({ ok: true, ready: true });
        });
        return true;
    }

    if (type === "OPEN") {
        openAndWatch(message.url, message.layout, sender).then(sendResponse).catch(function (error) {
            sendResponse({ ok: false, error: String(error && error.message || error) });
        });
        return true;
    }

    if (type === "LAYOUT") {
        layoutWindows(message.layout).then(function () {
            sendResponse({ ok: true });
        });
        return true;
    }

    if (type === "FOCUS") {
        raiseWatchWindow().then(sendResponse);
        return true;
    }

    if (type === "OPEN_EXTENSIONS") {
        chrome.tabs.create({ url: "chrome://extensions/?id=" + chrome.runtime.id }).then(function () {
            sendResponse({ ok: true, opened: true, id: chrome.runtime.id });
        }).catch(function (error) {
            sendResponse({ ok: false, error: String(error && error.message || error) });
        });
        return true;
    }

    if (type === "NAVIGATE") {
        readState().then(function (state) {
            if (!state.watchTabId || !message.url) {
                sendResponse({ ok: false });
                return;
            }
            return chrome.tabs.update(state.watchTabId, { url: message.url }).then(function () {
                sendResponse({ ok: true });
            });
        }).catch(function () {
            sendResponse({ ok: false });
        });
        return true;
    }

    if (type === "REINJECT") {
        readState().then(function (state) {
            const tabId = sender.tab && sender.tab.id;
            if (tabId && tabId === state.watchTabId) {
                injectObserver(tabId);
            }
        });
        return false;
    }

    if (type === "SNAPSHOT") {
        readState().then(function (state) {
            const tabId = sender.tab && sender.tab.id;
            if (!tabId || tabId !== state.watchTabId) {
                return;
            }
            const frameId = typeof sender.frameId === "number" ? sender.frameId : 0;
            if (!frameCache[tabId]) {
                frameCache[tabId] = {};
            }
            frameCache[tabId][frameId] = message;
            queueMerge(tabId, state.partnerlyTabId);
        });
        return false;
    }

    return false;
});

chrome.tabs.onUpdated.addListener(function (tabId, changeInfo) {
    readState().then(function (state) {
        if (tabId !== state.watchTabId) {
            return;
        }
        if (changeInfo.status === "loading" && changeInfo.url) {
            frameCache[tabId] = {};
        }
        if (changeInfo.status === "loading" || changeInfo.status === "complete") {
            injectObserver(tabId);
        }
        if (changeInfo.url || changeInfo.title || changeInfo.status) {
            safeSend(state.partnerlyTabId, {
                type: "NAV",
                url: changeInfo.url,
                title: changeInfo.title,
                status: changeInfo.status
            });
        }
    });
});

chrome.webNavigation.onCommitted.addListener(function (details) {
    readState().then(function (state) {
        if (!state.watchTabId || details.tabId !== state.watchTabId) {
            return;
        }
        if (details.frameId === 0) {
            frameCache[details.tabId] = {};
        }
        injectObserver(details.tabId, details.frameId);
    });
});

chrome.tabs.onRemoved.addListener(function (tabId) {
    readState().then(function (state) {
        if (tabId === state.watchTabId) {
            clearWatch();
        }
    });
});

chrome.windows.onRemoved.addListener(function (windowId) {
    readState().then(function (state) {
        if (windowId === state.watchWindowId) {
            clearWatch();
        }
    });
});
