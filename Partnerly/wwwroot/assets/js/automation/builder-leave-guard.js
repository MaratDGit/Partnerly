(function () {
    "use strict";

    // Unsaved changes on the editor canvas: the browser asks on refresh / close / address change,
    // and clicks on site links open our own Save / Don't save / Stay dialog.
    var form = document.getElementById("automationBuilderForm");
    if (!form) {
        return;
    }

    var canEdit = form.getAttribute("data-can-edit") === "true";
    var leaving = false;
    var dialog = null;

    var text = {};
    try {
        var i18nEl = document.getElementById("automationBuilderI18n");
        text = JSON.parse(i18nEl ? i18nEl.textContent || "{}" : "{}");
    } catch (err) {
        text = {};
    }

    function label(key, fallback) {
        return text[key] || fallback;
    }

    function isDirty() {
        var builder = window.PartnerlyBuilder;
        return canEdit && !leaving && !!builder && builder.isDirty();
    }

    window.addEventListener("beforeunload", function (event) {
        if (!isDirty()) {
            return;
        }
        event.preventDefault();
        event.returnValue = "";
    });

    function save() {
        if (typeof window.syncAutomationBuilder === "function") {
            window.syncAutomationBuilder();
        }
        var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        return fetch(form.getAttribute("data-save-url"), {
            method: "POST",
            body: new FormData(form),
            credentials: "same-origin",
            headers: {
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": tokenInput ? tokenInput.value : ""
            }
        }).then(function (response) {
            return response.json().catch(function () { return {}; }).then(function (payload) {
                if (!response.ok || payload.success === false) {
                    throw new Error(payload.message || response.statusText || "Error");
                }
                window.PartnerlyBuilder.markSaved();
            });
        });
    }

    function closeDialog() {
        if (dialog) {
            dialog.remove();
            dialog = null;
            document.body.classList.remove("is-automation-create-open");
        }
    }

    function go(href) {
        leaving = true;
        window.location.href = href;
    }

    function button(textValue, className) {
        var el = document.createElement("button");
        el.type = "button";
        el.className = "btn " + className;
        el.textContent = textValue;
        return el;
    }

    function ask(href) {
        closeDialog();
        dialog = document.createElement("div");
        dialog.className = "automation-create is-open";
        dialog.setAttribute("role", "alertdialog");
        dialog.setAttribute("aria-modal", "true");

        var card = document.createElement("div");
        card.className = "automation-create-card";
        var title = document.createElement("h2");
        title.textContent = label("leaveTitle", "Unsaved changes");
        var lead = document.createElement("p");
        lead.className = "automation-create-lead";
        lead.textContent = label("leaveText", "Save the changes to this workflow before leaving?");
        var error = document.createElement("div");
        error.className = "alert alert-danger";
        error.hidden = true;

        var actions = document.createElement("div");
        actions.className = "automation-create-actions";
        var stay = button(label("leaveStay", "Stay"), "btn-outline-secondary");
        var discard = button(label("leaveDiscard", "Don't save"), "btn-outline-danger");
        var saveBtn = button(label("leaveSave", "Save and leave"), "btn-primary");
        actions.appendChild(stay);
        actions.appendChild(discard);
        actions.appendChild(saveBtn);

        card.appendChild(title);
        card.appendChild(lead);
        card.appendChild(error);
        card.appendChild(actions);
        dialog.appendChild(card);
        document.body.appendChild(dialog);
        document.body.classList.add("is-automation-create-open");

        stay.addEventListener("click", closeDialog);
        discard.addEventListener("click", function () { go(href); });
        saveBtn.addEventListener("click", function () {
            saveBtn.disabled = true;
            discard.disabled = true;
            save().then(function () {
                go(href);
            }).catch(function (err) {
                error.textContent = err.message || "Error";
                error.hidden = false;
                saveBtn.disabled = false;
                discard.disabled = false;
            });
        });
        dialog.addEventListener("click", function (event) {
            if (event.target === dialog) {
                closeDialog();
            }
        });
        stay.focus();
    }

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && dialog) {
            closeDialog();
        }
    });

    document.addEventListener("click", function (event) {
        if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return;
        }
        var link = event.target.closest ? event.target.closest("a[href]") : null;
        if (!link || dialog || !isDirty()) {
            return;
        }
        var href = link.getAttribute("href") || "";
        if (!href || href.charAt(0) === "#" || /^(javascript|mailto|tel):/i.test(href)
            || link.target === "_blank" || link.hasAttribute("download")
            || link.classList.contains("is-disabled") || link.getAttribute("aria-disabled") === "true") {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        ask(link.href);
    }, true);
})();
