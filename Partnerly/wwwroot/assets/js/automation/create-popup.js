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
        setMode(false);
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

    var editId = null;
    var titleEl = document.getElementById("automationCreateTitle");

    function setMode(editing, id) {
        editId = editing ? id : null;
        overlay.classList.toggle("is-edit", !!editing);
        overlay.querySelectorAll("[data-create-only]").forEach(function (el) { el.hidden = !!editing; });
        if (titleEl) {
            titleEl.textContent = titleEl.getAttribute(editing ? "data-edit-title" : "data-create-title") || titleEl.textContent;
        }
        if (form) {
            form.setAttribute("action", form.getAttribute(editing ? "data-edit-url" : "data-create-url") || form.action);
            var hidden = form.querySelector('input[name="id"]');
            if (editing) {
                if (!hidden) {
                    hidden = document.createElement("input");
                    hidden.type = "hidden";
                    hidden.name = "id";
                    form.appendChild(hidden);
                }
                hidden.value = id;
            } else if (hidden) {
                hidden.remove();
            }
        }
    }

    // Edit only the name and description of an existing automation.
    function edit(id, name, description) {
        setMode(true, id);
        overlay.hidden = false;
        overlay.classList.add("is-open");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("is-automation-create-open");
        if (nameInput) {
            nameInput.value = name || "";
            window.setTimeout(function () { nameInput.focus(); }, 40);
        }
        if (descriptionInput) {
            descriptionInput.value = description || "";
        }
    }

    window.PartnerlyAutomationCreate = { edit: edit };

    function close() {
        setMode(false);
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
                var wasEdit = !!editId;
                close();
                if (wasEdit) {
                    if (window.PartnerlyAutomationHub) {
                        window.PartnerlyAutomationHub.addRow(payload);
                    }
                    return;
                }
                if (window.PartnerlyAgentsMenu) {
                    window.PartnerlyAgentsMenu.add(payload);
                }
                if (window.PartnerlyAutomationHub) {
                    window.PartnerlyAutomationHub.addRow(payload);
                }
                if (payload.id) {
                    window.location.href = "/Automation/Builder/" + encodeURIComponent(payload.id);
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

    // A click on a grid row (not on its action buttons) opens the name/description popup.
    var hubRoot = document.getElementById("automationHubRoot");
    if (hubRoot && hubRoot.getAttribute("data-can-edit") === "true") {
        hubRoot.addEventListener("click", function (event) {
            var row = event.target.closest("tr[data-automation-row]");
            if (!row || event.target.closest("a, button, .automation-hub-actions")) {
                return;
            }
            var nameEl = row.querySelector(".automation-hub-name");
            var descEl = row.querySelector(".automation-hub-desc");
            edit(row.getAttribute("data-automation-row"), nameEl ? nameEl.textContent : "", descEl ? descEl.textContent : "");
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
