(function () {
    "use strict";

    var overlayId = "partnerlyMySettings";
    var loadUrl = "/Dashboard/Settings";
    var loading = false;
    var saving = false;

    function overlay() {
        return document.getElementById(overlayId);
    }

    function isOpen() {
        var el = overlay();
        return !!(el && el.classList.contains("is-open"));
    }

    function isStandalone(el) {
        return !!(el && el.getAttribute("data-settings-standalone") === "true");
    }

    function setOpen(open) {
        var el = overlay();
        if (!el) {
            return;
        }
        el.hidden = !open;
        el.classList.toggle("is-open", open);
        el.setAttribute("aria-hidden", open ? "false" : "true");
        document.body.classList.toggle("is-my-settings-open", open);
        if (open && el.parentNode !== document.body) {
            document.body.appendChild(el);
        }
        if (open) {
            var first = el.querySelector("#firstName");
            window.setTimeout(function () {
                if (first) {
                    first.focus();
                }
            }, 40);
        }
    }

    function finishClose() {
        var el = overlay();
        if (!el) {
            return;
        }
        if (isStandalone(el)) {
            if (window.history.length > 1) {
                window.history.back();
            } else {
                window.location.href = "/";
            }
            return;
        }
        setOpen(false);
    }

    function showAlert(message) {
        var el = overlay();
        if (!el) {
            return;
        }
        var host = el.querySelector("[data-my-settings-alerts]");
        if (!host) {
            return;
        }
        host.innerHTML = "";
        if (!message) {
            return;
        }
        var wrap = document.createElement("div");
        wrap.className = "support-alert support-alert-danger";
        wrap.setAttribute("role", "alert");
        wrap.innerHTML =
            '<div class="support-alert-icon"><i class="bx bx-error-circle"></i></div>' +
            '<div class="support-alert-body"><div></div></div>';
        wrap.querySelector(".support-alert-body div").textContent = message;
        host.appendChild(wrap);
    }

    function clearFieldErrors(el) {
        var spans = el.querySelectorAll(".settings-validation");
        spans.forEach(function (span) {
            span.textContent = "";
        });
    }

    function setFieldErrors(el, errors) {
        if (!errors) {
            return;
        }
        Object.keys(errors).forEach(function (key) {
            if (!key) {
                return;
            }
            var span = el.querySelector('[data-valmsg-for="' + key + '"]');
            if (!span) {
                var inputs = el.querySelectorAll("input, textarea, select");
                for (var i = 0; i < inputs.length; i++) {
                    var name = inputs[i].getAttribute("name") || "";
                    if (name.toLowerCase() === key.toLowerCase()) {
                        span = inputs[i].closest(".col-12, .settings-password-field, .settings-field")
                            ? inputs[i].parentElement.parentElement.parentElement.querySelector(".settings-validation")
                            : null;
                        var group = inputs[i].closest(".col-12, .settings-password-field");
                        if (group) {
                            span = group.querySelector(".settings-validation");
                        }
                        break;
                    }
                }
            }
            if (span) {
                span.textContent = errors[key] || "";
            }
        });
    }

    function bindTabs(el) {
        var tabButtons = el.querySelectorAll('[data-bs-toggle="tab"]');
        var activeInput = el.querySelector("#activeTab");
        tabButtons.forEach(function (button) {
            button.addEventListener("shown.bs.tab", function (event) {
                if (activeInput) {
                    activeInput.value = event.target.getAttribute("data-bs-target") || "";
                }
            });
        });
        if (activeInput && activeInput.value) {
            var trigger = el.querySelector('[data-bs-target="' + activeInput.value + '"]');
            if (trigger && window.bootstrap && window.bootstrap.Tab) {
                new window.bootstrap.Tab(trigger).show();
            }
        }

        var checkbox = el.querySelector("#allowSendNotifications");
        var warningSpan = el.querySelector("#warningSpan");
        if (checkbox && warningSpan) {
            checkbox.addEventListener("change", function () {
                warningSpan.classList.toggle("d-none", checkbox.checked);
            });
        }
    }

    function saveAndClose() {
        var el = overlay();
        if (!el || saving) {
            return;
        }
        var form = el.querySelector("[data-my-settings-form]");
        if (!form) {
            finishClose();
            return;
        }
        saving = true;
        el.classList.add("is-saving");
        clearFieldErrors(el);
        showAlert("");
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
                    var error = new Error(payload.message || response.statusText || "Error");
                    error.payload = payload;
                    throw error;
                }
                return payload;
            });
        }).then(function () {
            finishClose();
        }).catch(function (err) {
            var payload = err && err.payload ? err.payload : {};
            setFieldErrors(el, payload.errors || {});
            showAlert(payload.message || (err && err.message) || "");
        }).then(function () {
            saving = false;
            el.classList.remove("is-saving");
        });
    }

    function bind(el) {
        if (!el || el.getAttribute("data-settings-bound") === "true") {
            return;
        }
        el.setAttribute("data-settings-bound", "true");
        bindTabs(el);
        var form = el.querySelector("[data-my-settings-form]");
        if (form) {
            form.addEventListener("submit", function (event) {
                event.preventDefault();
                saveAndClose();
            });
        }
    }

    function ensureOpen() {
        var el = overlay();
        if (el) {
            bind(el);
            setOpen(true);
            return;
        }
        if (loading) {
            return;
        }
        loading = true;
        fetch(loadUrl, {
            method: "GET",
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        }).then(function (response) {
            if (response.status === 401 || response.status === 403) {
                window.location.href = loadUrl;
                return null;
            }
            if (!response.ok) {
                throw new Error(response.statusText || "Error");
            }
            return response.text();
        }).then(function (html) {
            if (!html) {
                return;
            }
            var holder = document.createElement("div");
            holder.innerHTML = html;
            var next = holder.querySelector("#" + overlayId);
            if (!next) {
                window.location.href = loadUrl;
                return;
            }
            next.setAttribute("data-settings-standalone", "false");
            document.body.appendChild(next);
            bind(next);
            setOpen(true);
        }).catch(function () {
            window.location.href = loadUrl;
        }).then(function () {
            loading = false;
        });
    }

    function isOpener(element) {
        if (!element) {
            return false;
        }
        if (element.hasAttribute("data-open-my-settings")) {
            return true;
        }
        var href = element.getAttribute("href") || "";
        return /\/Dashboard\/Settings\/?(\?|#|$)/i.test(href);
    }

    document.addEventListener("click", function (event) {
        var closer = event.target.closest("[data-my-settings-close]");
        if (closer) {
            event.preventDefault();
            saveAndClose();
            return;
        }

        var el = overlay();
        if (el && event.target === el) {
            saveAndClose();
            return;
        }

        var opener = event.target.closest("[data-open-my-settings], a[href]");
        if (!opener || !isOpener(opener)) {
            return;
        }

        event.preventDefault();
        ensureOpen();
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && isOpen()) {
            event.preventDefault();
            saveAndClose();
        }
    });

    var existing = overlay();
    if (existing) {
        bind(existing);
        if (existing.classList.contains("is-open") || isStandalone(existing)) {
            setOpen(true);
        }
    }
})();
