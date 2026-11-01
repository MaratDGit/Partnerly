(function (document, window) {

    "use strict";

    var switching = false;
    var SEARCH_ATTRS = [
        "data-type-phrases",
        "data-chat-replies",
        "data-chat-prompts",
        "data-slider-prev",
        "data-slider-next",
        "data-google-url",
        "data-apple-url",
        "data-google-label",
        "data-apple-label",
        "data-login-label",
        "data-signup-label"
    ];

    document.addEventListener("submit", function (event) {

        var form = event.target;

        if (
            !form ||
            form.tagName !== "FORM" ||
            !form.closest(".partnerly-language")
        ) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        var submitter = event.submitter;

        if (
            submitter &&
            submitter.classList.contains("is-current")
        ) {
            closeLanguageMenu(form);
            return;
        }

        switchLanguage(form, submitter);

    }, true);

    function isLanding() {
        return !!(document.body && document.body.classList.contains("landing-body"));
    }

    function closeLanguageMenu(form) {

        var root = form && form.closest(".partnerly-language");
        var button = root && root.querySelector(".partnerly-language-button");

        if (
            !button ||
            !window.bootstrap ||
            !bootstrap.Dropdown
        ) {
            return;
        }

        var dropdown = bootstrap.Dropdown.getInstance(button);

        if (dropdown) {
            dropdown.hide();
        }
    }

    function disposeLanguageDropdown(button) {

        if (
            !button ||
            !window.bootstrap ||
            !bootstrap.Dropdown
        ) {
            return;
        }

        var dropdown = bootstrap.Dropdown.getInstance(button);

        if (dropdown) {
            dropdown.hide();
            dropdown.dispose();
        }
    }

    function finishSwitch(ok) {

        switching = false;
        document.documentElement.classList.remove("partnerly-language-switching");

        if (!ok && window.PartnerlyLoader) {
            PartnerlyLoader.hide();
        }
    }

    function replaceInner(currentRoot, nextRoot, selector) {

        var current = currentRoot.querySelector(selector);
        var next = nextRoot.querySelector(selector);

        if (current && next) {
            current.innerHTML = next.innerHTML;
        }
    }

    function copyAttributes(from, to, names) {

        if (!from || !to) {
            return;
        }

        names.forEach(function (name) {
            var value = from.getAttribute(name);

            if (value === null) {
                to.removeAttribute(name);
            } else {
                to.setAttribute(name, value);
            }
        });
    }

    function swapLanguageSwitcher(nextDoc) {

        var current = document.querySelector(".partnerly-language");
        var next = nextDoc.querySelector(".partnerly-language");

        if (!current || !next) {
            return;
        }

        disposeLanguageDropdown(current.querySelector(".partnerly-language-button"));
        current.replaceWith(document.importNode(next, true));
    }

    function swapSearchCopy(nextDoc) {

        var current = document.querySelector(".landing-search");
        var next = nextDoc.querySelector(".landing-search");

        if (!current || !next) {
            return;
        }

        copyAttributes(next, current, SEARCH_ATTRS);

        var currentInput = current.querySelector(".landing-search-input");
        var nextInput = next.querySelector(".landing-search-input");
        var currentSubmit = current.querySelector(".landing-search-submit");
        var nextSubmit = next.querySelector(".landing-search-submit");

        if (currentInput && nextInput) {
            copyAttributes(nextInput, currentInput, ["aria-label"]);
        }

        if (currentSubmit && nextSubmit) {
            copyAttributes(nextSubmit, currentSubmit, ["aria-label"]);
        }
    }

    function swapOverlay(id, nextDoc) {

        var current = document.getElementById(id);
        var next = nextDoc.getElementById(id);

        if (!current || !next) {
            return;
        }

        var open = current.classList.contains("is-open");
        var openPanel = current.getAttribute("data-open") || "";

        Array.prototype.forEach.call(next.attributes, function (attr) {
            if (
                attr.name === "id" ||
                attr.name === "class" ||
                attr.name === "aria-hidden" ||
                attr.name === "data-open"
            ) {
                return;
            }

            current.setAttribute(attr.name, attr.value);
        });

        current.innerHTML = next.innerHTML;

        if (!open) {
            current.classList.remove("is-open");
            current.setAttribute("aria-hidden", "true");
            current.setAttribute("data-open", "");
            return;
        }

        current.classList.add("is-open");
        current.setAttribute("aria-hidden", "false");
        current.setAttribute("data-open", openPanel);

        var panels = current.querySelectorAll("[data-auth-panel], [data-info-panel]");

        for (var i = 0; i < panels.length; i += 1) {
            var key = panels[i].getAttribute("data-auth-panel") ||
                panels[i].getAttribute("data-info-panel");
            panels[i].classList.toggle("is-active", key === openPanel);
        }
    }

    function applyLandingLanguage(html) {

        var nextDoc = new DOMParser().parseFromString(html, "text/html");

        if (
            !nextDoc.body ||
            !nextDoc.body.classList.contains("landing-body")
        ) {
            throw new Error("language-landing");
        }

        var lang = nextDoc.documentElement.getAttribute("lang");

        if (lang) {
            document.documentElement.setAttribute("lang", lang);
        }

        var title = nextDoc.querySelector("title");

        if (title) {
            document.title = title.textContent;
        }

        swapLanguageSwitcher(nextDoc);
        replaceInner(document, nextDoc, ".landing-hero-copy");
        replaceInner(document, nextDoc, ".landing-hero-actions");
        replaceInner(document, nextDoc, "[data-landing-prompts]");
        replaceInner(document, nextDoc, "#globalLoader .loader-text");
        swapSearchCopy(nextDoc);
        swapOverlay("landingAuth", nextDoc);
        swapOverlay("landingInfo", nextDoc);

        if (window.partnerlyLandingSearch && typeof window.partnerlyLandingSearch.refresh === "function") {
            window.partnerlyLandingSearch.refresh();
        }

        if (window.partnerlyLandingAuth && typeof window.partnerlyLandingAuth.refresh === "function") {
            window.partnerlyLandingAuth.refresh();
        }

        if (window.partnerlyLandingInfo && typeof window.partnerlyLandingInfo.refresh === "function") {
            window.partnerlyLandingInfo.refresh();
        }
    }

    function switchLanguage(form, submitter) {

        if (switching) {
            return;
        }

        switching = true;
        document.documentElement.classList.add("partnerly-language-switching");

        var action = form.getAttribute("action") || "/Language/SetLanguage";
        var body = new FormData(form);
        var stayOnLanding = isLanding();

        if (submitter && submitter.name) {
            body.set(submitter.name, submitter.value);
        }

        fetch(action, {
            method: "POST",
            body: body,
            credentials: "same-origin",
            headers: {
                "X-Requested-With": "XMLHttpRequest",
                "Accept": "application/json"
            }
        })
            .then(function (response) {

                if (!response.ok) {
                    throw new Error("language-post");
                }

                return fetch(window.location.href, {
                    credentials: "same-origin",
                    cache: "no-store",
                    headers: {
                        "Accept": "text/html"
                    }
                });
            })
            .then(function (response) {

                if (!response.ok) {
                    throw new Error("language-get");
                }

                return response.text();
            })
            .then(function (html) {

                if (stayOnLanding && isLanding()) {
                    applyLandingLanguage(html);
                    finishSwitch(true);
                    return;
                }

                document.open("text/html", "replace");
                document.write(html);
                document.close();
            })
            .catch(function () {

                finishSwitch(false);

                if (stayOnLanding && isLanding()) {
                    return;
                }

                form.submit();
            });
    }

})(document, window);
