(function () {
    if (!document.querySelector("[data-inspector-root]")) {
        return;
    }
    if (window.__partnerlyInspectorBridge) {
        return;
    }
    window.__partnerlyInspectorBridge = true;

    function reply(id, response) {
        window.postMessage({
            source: "partnerly-inspector-ext",
            id: id,
            response: response || {}
        }, "*");
    }

    window.addEventListener("message", function (event) {
        if (event.source !== window || !event.data || event.data.source !== "partnerly-inspector-page") {
            return;
        }
        const payload = event.data.payload || {};
        chrome.runtime.sendMessage(payload, function (response) {
            reply(event.data.id, response || { ok: false });
        });
    });

    chrome.runtime.onMessage.addListener(function (message) {
        window.postMessage({
            source: "partnerly-inspector-ext",
            type: message && message.type,
            payload: message
        }, "*");
    });

    window.postMessage({
        source: "partnerly-inspector-ext",
        type: "READY",
        payload: { type: "READY" }
    }, "*");
})();
