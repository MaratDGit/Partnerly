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

    var ALLOWED_ICONS = {
        "bx-envelope": true,
        "bx-message-dots": true,
        "bx-news": true,
        "bx-edit-alt": true,
        "bx-play-circle": true,
        "bx-check-circle": true,
        "bx-bot": true,
        "bx-task": true
    };

    var phrases = [];
    var replies = [];
    var prompts = [];
    var googleUrl = "";
    var appleUrl = "";
    var googleLabel = "Google";
    var appleLabel = "Apple";
    var loginLabel = "Log in";
    var signupLabel = "Sign up";
    var sliderPrev = "Previous";
    var sliderNext = "Next";
    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var phraseIndex = 0;
    var charIndex = 0;
    var deleting = false;
    var timer = 0;
    var paused = false;
    var chatOn = false;
    var replyIndex = 0;
    var botTimer = 0;
    var busy = false;

    function parseList(raw) {
        try {
            var parsed = JSON.parse(raw || "[]");
            return Array.isArray(parsed) ? parsed.filter(Boolean) : [];
        } catch (error) {
            return [];
        }
    }

    function parsePrompts(raw) {
        try {
            var parsed = JSON.parse(raw || "[]");
            return Array.isArray(parsed) ? parsed.filter(function (item) {
                return item && item.id && item.label;
            }) : [];
        } catch (error) {
            return [];
        }
    }

    function readCopy() {
        phrases = parseList(form.getAttribute("data-type-phrases"));
        replies = parseList(form.getAttribute("data-chat-replies"));
        prompts = parsePrompts(form.getAttribute("data-chat-prompts"));
        googleUrl = form.getAttribute("data-google-url") || "";
        appleUrl = form.getAttribute("data-apple-url") || "";
        googleLabel = form.getAttribute("data-google-label") || "Google";
        appleLabel = form.getAttribute("data-apple-label") || "Apple";
        loginLabel = form.getAttribute("data-login-label") || "Log in";
        signupLabel = form.getAttribute("data-signup-label") || "Sign up";
        sliderPrev = form.getAttribute("data-slider-prev") || "Previous";
        sliderNext = form.getAttribute("data-slider-next") || "Next";

        if (!phrases.length) {
            phrases = [""];
        }

        if (!replies.length) {
            replies = [""];
        }
    }

    readCopy();

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

    function iconEl(name) {
        var icon = document.createElement("i");
        icon.className = "bx " + (ALLOWED_ICONS[name] ? name : "bx-bot");
        icon.setAttribute("aria-hidden", "true");
        return icon;
    }

    function sliderFrom(prompt) {
        var slides = Array.isArray(prompt.slides) ? prompt.slides.filter(function (slide) {
            return slide && (slide.title || slide.body);
        }) : [];

        if (!slides.length) {
            return bubble(prompt.text || "", "bot");
        }

        var row = document.createElement("div");
        row.className = "landing-chat-msg is-bot is-rich";

        var shell = document.createElement("div");
        shell.className = "landing-chat-slider";
        shell.setAttribute("data-slide-index", "0");

        var track = document.createElement("div");
        track.className = "landing-chat-slides";

        slides.forEach(function (slide, index) {
            var card = document.createElement("article");
            card.className = "landing-chat-slide" + (index === 0 ? " is-active" : "");

            var head = document.createElement("div");
            head.className = "landing-chat-slide-head";
            head.appendChild(iconEl(slide.icon));

            var title = document.createElement("strong");
            title.textContent = slide.title || "";
            head.appendChild(title);
            card.appendChild(head);

            if (slide.body) {
                var body = document.createElement("p");
                body.textContent = slide.body;
                card.appendChild(body);
            }

            track.appendChild(card);
        });

        shell.appendChild(track);

        if (slides.length > 1) {
            var nav = document.createElement("div");
            nav.className = "landing-chat-slider-nav";

            var prev = document.createElement("button");
            prev.type = "button";
            prev.className = "landing-chat-slider-btn";
            prev.setAttribute("data-slide-dir", "-1");
            prev.setAttribute("aria-label", sliderPrev);
            prev.innerHTML = "<i class=\"bx bx-chevron-left\" aria-hidden=\"true\"></i>";

            var dots = document.createElement("div");
            dots.className = "landing-chat-slider-dots";
            slides.forEach(function (_, index) {
                var dot = document.createElement("button");
                dot.type = "button";
                dot.className = "landing-chat-slider-dot" + (index === 0 ? " is-active" : "");
                dot.setAttribute("data-slide-to", String(index));
                dot.setAttribute("aria-label", String(index + 1));
                dots.appendChild(dot);
            });

            var next = document.createElement("button");
            next.type = "button";
            next.className = "landing-chat-slider-btn";
            next.setAttribute("data-slide-dir", "1");
            next.setAttribute("aria-label", sliderNext);
            next.innerHTML = "<i class=\"bx bx-chevron-right\" aria-hidden=\"true\"></i>";

            nav.appendChild(prev);
            nav.appendChild(dots);
            nav.appendChild(next);
            shell.appendChild(nav);
        }

        row.appendChild(shell);
        return row;
    }

    function showSlide(slider, index) {
        var slides = slider.querySelectorAll(".landing-chat-slide");
        var dots = slider.querySelectorAll(".landing-chat-slider-dot");
        var total = slides.length;
        if (!total) {
            return;
        }

        var next = (index + total) % total;
        slider.setAttribute("data-slide-index", String(next));

        for (var i = 0; i < total; i += 1) {
            slides[i].classList.toggle("is-active", i === next);
            if (dots[i]) {
                dots[i].classList.toggle("is-active", i === next);
            }
        }
    }

    function promptButtons(exceptId) {
        var row = document.createElement("div");
        row.className = "landing-chat-prompts";

        prompts.forEach(function (prompt) {
            if (prompt.id === exceptId) {
                return;
            }

            var button = document.createElement("button");
            button.type = "button";
            button.className = "landing-prompt";
            button.setAttribute("data-landing-prompt", prompt.id);
            button.textContent = prompt.label;
            row.appendChild(button);
        });

        return row.childNodes.length ? row : null;
    }

    function findPrompt(id) {
        for (var i = 0; i < prompts.length; i += 1) {
            if (prompts[i].id === id) {
                return prompts[i];
            }
        }

        return null;
    }

    function normalize(text) {
        return (text || "").toLowerCase().replace(/\s+/g, " ").trim();
    }

    function matchPrompt(question) {
        var q = normalize(question);
        if (!q) {
            return null;
        }

        for (var i = 0; i < prompts.length; i += 1) {
            var prompt = prompts[i];
            var label = normalize(prompt.label);
            if (label && (q === label || q.indexOf(label) !== -1 || label.indexOf(q) !== -1)) {
                return prompt;
            }
        }

        if (/what is this|что это|ինչ է սա/.test(q)) {
            return findPrompt("what");
        }

        if (/how (do i )?start|как начать|ինչպես սկսել/.test(q)) {
            return findPrompt("start");
        }

        if (/example|пример|օրինակ/.test(q)) {
            return findPrompt("example");
        }

        if (/how .*work|как .*работ|ինչպես .*աշխատ/.test(q)) {
            return findPrompt("how");
        }

        return null;
    }

    function scrollThread() {
        if (!thread) {
            return;
        }
        thread.scrollTop = thread.scrollHeight;
    }

    function finishBot(pending, prompt, fallbackText) {
        var bot;

        if (prompt && prompt.kind === "slider") {
            bot = sliderFrom(prompt);
        } else {
            bot = bubble((prompt && prompt.text) || fallbackText || replies[0], "bot");
        }

        if (prompt && prompt.auth) {
            bot.appendChild(authRow());
        } else if (!prompt) {
            bot.appendChild(authRow());
        }

        var chips = promptButtons(prompt && prompt.id);
        if (chips) {
            bot.appendChild(chips);
        }

        pending.replaceWith(bot);
        busy = false;
        scrollThread();
    }

    function ask(question, promptId) {
        if (!thread || busy) {
            return;
        }

        var prompt = promptId ? findPrompt(promptId) : matchPrompt(question);
        var label = (prompt && prompt.label) || question;
        if (!label) {
            return;
        }

        busy = true;
        thread.appendChild(bubble(label, "user"));
        var pending = typingRow();
        thread.appendChild(pending);
        scrollThread();

        window.clearTimeout(botTimer);
        botTimer = window.setTimeout(function () {
            if (!prompt) {
                var text = replies[replyIndex % replies.length] || replies[0];
                replyIndex += 1;
                finishBot(pending, null, text);
                return;
            }

            finishBot(pending, prompt);
        }, reduceMotion ? 0 : 650);
    }

    function pickPrompt(id) {
        var prompt = findPrompt(id);
        if (!prompt || busy) {
            return;
        }

        enterChat();
        ask(prompt.label, prompt.id);
        input.value = "";
        form.classList.remove("has-value");
        input.focus();
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
        if (!question || busy) {
            return;
        }

        enterChat();
        ask(question);
        input.value = "";
        form.classList.remove("has-value");
        input.focus();
    });

    stage.addEventListener("click", function (event) {
        var promptBtn = event.target.closest("[data-landing-prompt]");
        if (promptBtn) {
            event.preventDefault();
            pickPrompt(promptBtn.getAttribute("data-landing-prompt"));
            return;
        }

        var slider = event.target.closest(".landing-chat-slider");
        if (!slider) {
            return;
        }

        var dirBtn = event.target.closest("[data-slide-dir]");
        if (dirBtn) {
            event.preventDefault();
            var current = parseInt(slider.getAttribute("data-slide-index") || "0", 10) || 0;
            showSlide(slider, current + parseInt(dirBtn.getAttribute("data-slide-dir"), 10));
            return;
        }

        var dot = event.target.closest("[data-slide-to]");
        if (dot) {
            event.preventDefault();
            showSlide(slider, parseInt(dot.getAttribute("data-slide-to"), 10) || 0);
        }
    });

    function refresh() {
        readCopy();

        if (chatOn) {
            return;
        }

        window.clearTimeout(timer);
        phraseIndex = 0;
        charIndex = 0;
        deleting = false;

        if (paused) {
            render("");
            return;
        }

        if (reduceMotion) {
            render(phrases[0] || "");
            return;
        }

        tick();
    }

    window.partnerlyLandingSearch = {
        refresh: refresh
    };

    if (reduceMotion) {
        render(phrases[0] || "");
        return;
    }

    tick();
})();
