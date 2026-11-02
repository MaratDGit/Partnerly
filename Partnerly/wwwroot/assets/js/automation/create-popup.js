(function () {
    "use strict";

    var overlay = document.getElementById("automationCreate");
    if (!overlay) {
        return;
    }

    var nameInput = document.getElementById("automationCreateName");
    var descriptionInput = document.getElementById("automationCreateDescription");
    var templateSelect = document.getElementById("automationCreateTemplate");
    var form = overlay.querySelector("form");

    function isOpen() {
        return overlay.classList.contains("is-open");
    }

    function open(options) {
        options = options || {};
        overlay.hidden = false;
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-automation-create-open");
        if (templateSelect) {
            templateSelect.value = options.templateId || "";
            applyTemplate(options.name, options.description, !options.name);
        }
        if (nameInput) {
            if (options.name) {
                nameInput.value = options.name;
            }
            window.setTimeout(function () { nameInput.focus(); }, 40);
        }
        if (descriptionInput && options.description) {
            descriptionInput.value = options.description;
        }
    }

    function close() {
        overlay.classList.remove("is-open");
        overlay.setAttribute("aria-hidden", "true");
        overlay.hidden = true;
        document.body.classList.remove("is-automation-create-open");
        if (form) {
            form.reset();
        }
    }

    function selectedOption() {
        if (!templateSelect || templateSelect.selectedIndex < 0) {
            return null;
        }
        return templateSelect.options[templateSelect.selectedIndex];
    }

    function applyTemplate(forcedName, forcedDescription, fillEmptyOnly) {
        var option = selectedOption();
        var templateName = forcedName || (option && option.getAttribute("data-name")) || "";
        var templateDescription = forcedDescription || (option && option.getAttribute("data-description")) || "";
        if (nameInput && (!fillEmptyOnly || !nameInput.value)) {
            nameInput.value = templateName;
        }
        if (descriptionInput && (!fillEmptyOnly || !descriptionInput.value)) {
            descriptionInput.value = templateDescription;
        }
    }

    function isCreateOpener(element) {
        if (!element) {
            return false;
        }
        if (element.hasAttribute("data-open-create-automation")) {
            return true;
        }
        var href = element.getAttribute("href") || "";
        return /\/Automation\/Create\/?(\?|#|$)/i.test(href);
    }

    if (templateSelect) {
        templateSelect.addEventListener("change", function () {
            applyTemplate(null, null, false);
        });
    }

    document.addEventListener("click", function (event) {
        var closer = event.target.closest("[data-automation-create-close]");
        if (closer) {
            event.preventDefault();
            close();
            return;
        }

        if (event.target === overlay) {
            close();
            return;
        }

        var opener = event.target.closest("[data-open-create-automation], a[href]");
        if (!opener || !isCreateOpener(opener)) {
            return;
        }

        event.preventDefault();
        open({
            templateId: opener.getAttribute("data-template-id") || "",
            name: opener.getAttribute("data-template-name") || "",
            description: opener.getAttribute("data-template-description") || ""
        });
    });

    if (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            var submitButton = form.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.disabled = true;
            }
            fetch(form.getAttribute("action") || form.action, {
                method: "POST",
                body: new FormData(form),
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            }).then(function (response) {
                return response.text().then(function (text) {
                    var payload = {};
                    try {
                        payload = text ? JSON.parse(text) : {};
                    } catch (err) {
                        payload = { message: text };
                    }
                    if (!response.ok || payload.success === false) {
                        throw new Error(payload.message || response.statusText || "Error");
                    }
                    return payload;
                });
            }).then(function (payload) {
                close();
                if (window.PartnerlyAgentsMenu) {
                    window.PartnerlyAgentsMenu.add(payload);
                }
                if (window.PartnerlyAutomationHub) {
                    window.PartnerlyAutomationHub.addRow(payload);
                    if (payload.id) {
                        window.PartnerlyAutomationHub.openBuilder(payload.id);
                    }
                } else if (payload.id) {
                    window.location.href = "/Automation?edit=" + encodeURIComponent(payload.id);
                }
            }).catch(function (err) {
                if (window.PartnerlyAutomationHub && window.PartnerlyAutomationHub.toast) {
                    window.PartnerlyAutomationHub.toast(err.message || "Error", "error");
                } else {
                    window.alert(err.message || "Error");
                }
            }).then(function () {
                if (submitButton) {
                    submitButton.disabled = false;
                }
            });
        });
    }

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && isOpen()) {
            close();
        }
    });

    try {
        var params = new URLSearchParams(window.location.search);
        if (params.get("create") === "1") {
            open();
            params.delete("create");
            var query = params.toString();
            var next = window.location.pathname + (query ? "?" + query : "") + window.location.hash;
            window.history.replaceState({}, "", next);
        }
    } catch (err) { }
})();
