(function () {
    var root = document.querySelector("[data-site-search]");
    if (!root) {
        return;
    }

    var input = root.querySelector(".partnerly-site-search-input");
    var panel = root.querySelector(".partnerly-site-search-panel");
    var hint = root.querySelector("[data-site-search-hint]");
    var results = root.querySelector("[data-site-search-results]");
    if (!input || !panel || !hint || !results) {
        return;
    }

    var url = root.getAttribute("data-search-url") || "/Search/Query";
    var emptyText = root.getAttribute("data-empty") || "";
    var noResultsText = root.getAttribute("data-no-results") || "";
    var timer = 0;
    var abort = null;
    var activeIndex = -1;

    function showPanel() {
        panel.hidden = false;
    }

    function hidePanel() {
        panel.hidden = true;
        activeIndex = -1;
    }

    function setHint(text) {
        hint.textContent = text || "";
        hint.hidden = !text;
    }

    function clearResults() {
        results.innerHTML = "";
        activeIndex = -1;
    }

    function items() {
        return results.querySelectorAll(".partnerly-site-search-item");
    }

    function setActive(index) {
        var list = items();
        if (!list.length) {
            activeIndex = -1;
            return;
        }

        if (index < 0) {
            index = list.length - 1;
        }
        if (index >= list.length) {
            index = 0;
        }

        activeIndex = index;
        for (var i = 0; i < list.length; i++) {
            list[i].classList.toggle("is-active", i === activeIndex);
        }

        if (list[activeIndex] && typeof list[activeIndex].scrollIntoView === "function") {
            list[activeIndex].scrollIntoView({ block: "nearest" });
        }
    }

    function readGroups(data) {
        if (!data) {
            return [];
        }
        return data.groups || data.Groups || [];
    }

    function readField(item, camel, pascal) {
        return item[camel] || item[pascal] || "";
    }

    function render(data) {
        clearResults();
        var groups = readGroups(data);
        if (!groups.length) {
            setHint(noResultsText);
            return;
        }

        setHint("");
        groups.forEach(function (group) {
            var section = document.createElement("div");
            section.className = "partnerly-site-search-group";

            var title = document.createElement("div");
            title.className = "partnerly-site-search-group-title";
            title.textContent = readField(group, "title", "Title");
            section.appendChild(title);

            (group.items || group.Items || []).forEach(function (item) {
                var link = document.createElement("a");
                link.className = "partnerly-site-search-item";
                link.href = readField(item, "url", "Url") || "#";

                var icon = document.createElement("i");
                icon.className = readField(item, "icon", "Icon") || "bx bx-search";
                link.appendChild(icon);

                var body = document.createElement("span");
                body.className = "partnerly-site-search-item-body";

                var name = document.createElement("span");
                name.className = "partnerly-site-search-item-title";
                name.textContent = readField(item, "title", "Title");
                body.appendChild(name);

                var subtitle = readField(item, "subtitle", "Subtitle");
                if (subtitle) {
                    var meta = document.createElement("span");
                    meta.className = "partnerly-site-search-item-meta";
                    meta.textContent = subtitle;
                    body.appendChild(meta);
                }

                link.appendChild(body);
                section.appendChild(link);
            });

            results.appendChild(section);
        });

        setActive(0);
    }

    function search(term) {
        if (abort) {
            abort.abort();
            abort = null;
        }

        if (term.length < 2) {
            clearResults();
            setHint(emptyText);
            return;
        }

        abort = new AbortController();
        var queryUrl = url + (url.indexOf("?") >= 0 ? "&" : "?") + "q=" + encodeURIComponent(term);

        fetch(queryUrl, {
            signal: abort.signal,
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        }).then(function (response) {
            if (!response.ok) {
                throw new Error("search failed");
            }
            return response.json();
        }).then(function (data) {
            render(data);
        }).catch(function (error) {
            if (error && error.name === "AbortError") {
                return;
            }
            clearResults();
            setHint(noResultsText);
        });
    }

    input.addEventListener("focus", function () {
        showPanel();
        if (!results.childElementCount) {
            setHint(emptyText);
        }
    });

    input.addEventListener("input", function () {
        showPanel();
        window.clearTimeout(timer);
        var term = input.value.trim();
        timer = window.setTimeout(function () {
            search(term);
        }, 220);
    });

    input.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            hidePanel();
            input.blur();
            return;
        }

        var list = items();
        if (!list.length) {
            return;
        }

        if (event.key === "ArrowDown") {
            event.preventDefault();
            showPanel();
            setActive(activeIndex + 1);
            return;
        }

        if (event.key === "ArrowUp") {
            event.preventDefault();
            showPanel();
            setActive(activeIndex - 1);
            return;
        }

        if (event.key === "Enter" && activeIndex >= 0 && list[activeIndex]) {
            event.preventDefault();
            window.location.href = list[activeIndex].href;
        }
    });

    document.addEventListener("click", function (event) {
        if (!root.contains(event.target)) {
            hidePanel();
        }
    });
})();
