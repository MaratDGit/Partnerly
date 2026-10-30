(function () {
    "use strict";

    var form = document.querySelector(".landing-search");
    var stage = document.querySelector(".landing-stage");
    if (!form || !stage) {
        return;
    }

    var input = form.querySelector(".landing-search-input");
    var typeEl = form.querySelector(".landing-search-type");
    var thread = stage.querySelector("[data-landing-thread]");
    if (!input || !typeEl) {
        return;
    }

    var phrases = parseList(form.getAttribute("data-type-phrases"));
    var replies = parseList(form.getAttribute("data-chat-replies"));
    var googleUrl = form.getAttribute("data-google-url") || "";
    var appleUrl = form.getAttribute("data-apple-url") || "";
    var googleLabel = form.getAttribute("data-google-label") || "Google";
    var appleLabel = form.getAttribute("data-apple-label") || "Apple";
    var loginLabel = form.getAttribute("data-login-label") || "Log in";
    var signupLabel = form.getAttribute("data-signup-label") || "Sign up";
    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var phraseIndex = 0;
    var charIndex = 0;
    var deleting = false;
    var timer = 0;
    var paused = false;
    var chatOn = false;
    var replyIndex = 0;
    var botTimer = 0;

    if (!phrases.length) {
        phrases = [""];
    }

    if (!replies.length) {
        replies = [""];
    }

    function parseList(raw) {
        try {
            var parsed = JSON.parse(raw || "[]");
            return Array.isArray(parsed) ? parsed.filter(Boolean) : [];
        } catch (error) {
            return [];
        }
    }

    function render(text) {
        typeEl.textContent = text;
        form.classList.toggle("is-empty", !text && !input.value);
    }

    function stopTypewriter() {
        paused = true;
        window.clearTimeout(timer);
        typeEl.textContent = "";
        form.classList.add("is-typing-user");
    }

    function tick() {
        if (paused || reduceMotion || chatOn) {
            return;
        }

        var phrase = phrases[phraseIndex] || "";

        if (!deleting) {
            charIndex += 1;
            render(phrase.slice(0, charIndex));
            if (charIndex >= phrase.length) {
                deleting = true;
                timer = window.setTimeout(tick, 1600);
                return;
            }
            timer = window.setTimeout(tick, 70 + Math.random() * 50);
            return;
        }

        charIndex -= 1;
        render(phrase.slice(0, Math.max(charIndex, 0)));
        if (charIndex <= 0) {
            deleting = false;
            phraseIndex = (phraseIndex + 1) % phrases.length;
            timer = window.setTimeout(tick, 420);
            return;
        }

        timer = window.setTimeout(tick, 36);
    }

    function pinInPlace(el) {
        if (!el) {
            return;
        }

        var rect = el.getBoundingClientRect();
        el.style.position = "fixed";
        el.style.left = rect.left + "px";
        el.style.top = rect.top + "px";
        el.style.width = rect.width + "px";
        el.style.margin = "0";
        el.style.zIndex = "3";
    }

    function enterChat() {
        if (chatOn) {
            return;
        }

        chatOn = true;
        stopTypewriter();

        var copy = stage.querySelector(".landing-hero-copy");
        var actions = stage.querySelector(".landing-hero-actions");

        if (reduceMotion) {
            stage.classList.add("is-chat");
            document.body.classList.add("is-chat");
            return;
        }

        var first = form.getBoundingClientRect();
        pinInPlace(copy);
        pinInPlace(actions);
        stage.classList.add("is-chat");
        document.body.classList.add("is-chat");

        var last = form.getBoundingClientRect();
        var dy = first.top - last.top;
        if (Math.abs(dy) < 1) {
            return;
        }

        form.style.transition = "none";
        form.style.transform = "translateY(" + dy + "px)";
        form.getBoundingClientRect();

        requestAnimationFrame(function () {
            form.style.transition = "transform 0.7s cubic-bezier(0.22, 1, 0.36, 1)";
            form.style.transform = "translateY(0)";
        });

        form.addEventListener("transitionend", function onEnd(event) {
            if (event.target !== form || event.propertyName !== "transform") {
                return;
            }
            form.removeEventListener("transitionend", onEnd);
            form.style.transition = "";
            form.style.transform = "";
        });
    }

    function bubble(text, kind) {
        var row = document.createElement("div");
        row.className = "landing-chat-msg is-" + kind;
        var body = document.createElement("div");
        body.className = "landing-chat-bubble";
        body.textContent = text;
        row.appendChild(body);
        return row;
    }

    function providerLink(url, label, iconClass, authPanel) {
        var link = document.createElement("a");
        link.className = "landing-chat-provider";
        link.setAttribute("aria-label", label);
        link.title = label;
        if (authPanel) {
            link.href = "#";
            link.setAttribute("data-auth", authPanel);
        } else {
            link.href = url;
        }
        var icon = document.createElement("i");
        icon.className = iconClass;
        icon.setAttribute("aria-hidden", "true");
        link.appendChild(icon);
        return link;
    }

    function authRow() {
        var row = document.createElement("div");
        row.className = "landing-chat-auth";
        row.appendChild(providerLink("#", loginLabel, "bx bx-log-in", "login"));
        row.appendChild(providerLink("#", signupLabel, "bx bx-user-plus", "register"));
        if (googleUrl) {
            row.appendChild(providerLink(googleUrl, googleLabel, "bx bxl-google"));
        }
        if (appleUrl) {
            row.appendChild(providerLink(appleUrl, appleLabel, "bx bxl-apple"));
        }
        return row;
    }

    function typingRow() {
        var row = document.createElement("div");
        row.className = "landing-chat-msg is-bot is-typing";
        var body = document.createElement("div");
        body.className = "landing-chat-bubble";
        body.innerHTML = "<span></span><span></span><span></span>";
        row.appendChild(body);
        return row;
    }

    function scrollThread() {
        if (!thread) {
            return;
        }
        thread.scrollTop = thread.scrollHeight;
    }

    function ask(question) {
        if (!thread) {
            return;
        }

        thread.appendChild(bubble(question, "user"));
        var pending = typingRow();
        thread.appendChild(pending);
        scrollThread();

        // Guest replies stay local until AI search is enabled.
        window.clearTimeout(botTimer);
        botTimer = window.setTimeout(function () {
            var text = replies[replyIndex % replies.length] || replies[0];
            replyIndex += 1;
            var bot = bubble(text, "bot");
            bot.appendChild(authRow());
            pending.replaceWith(bot);
            scrollThread();
        }, reduceMotion ? 0 : 650);
    }

    input.addEventListener("focus", stopTypewriter);
    input.addEventListener("input", function () {
        form.classList.toggle("has-value", input.value.length > 0);
    });

    input.addEventListener("blur", function () {
        if (input.value || chatOn) {
            return;
        }
        form.classList.remove("is-typing-user", "has-value");
        paused = false;
        deleting = false;
        charIndex = 0;
        if (!reduceMotion) {
            tick();
        }
    });

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        var question = (input.value || "").trim();
        if (!question) {
            return;
        }

        enterChat();
        ask(question);
        input.value = "";
        form.classList.remove("has-value");
        input.focus();
    });

    if (reduceMotion) {
        render(phrases[0] || "");
        return;
    }

    tick();
})();
