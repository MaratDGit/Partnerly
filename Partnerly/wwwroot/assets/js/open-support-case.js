(function () {
    "use strict";

    var overlayId = "partnerlySupportCase";
    var loadUrl = "/SupportTickets/OpenNewCase";
    var loading = false;

    function overlay() {
        return document.getElementById(overlayId);
    }

    function isOpen() {
        var el = overlay();
        return !!(el && el.classList.contains("is-open"));
    }

    function isStandalone(el) {
        return !!(el && el.getAttribute("data-support-standalone") === "true");
    }

    function setOpen(open) {
        var el = overlay();
        if (!el) {
            return;
        }
        el.hidden = !open;
        el.classList.toggle("is-open", open);
        el.setAttribute("aria-hidden", open ? "false" : "true");
        document.body.classList.toggle("is-support-case-open", open);
        if (open && el.parentNode !== document.body) {
            document.body.appendChild(el);
        }
        if (open) {
            var subject = el.querySelector('input[name="Subject"]');
            window.setTimeout(function () {
                if (subject) {
                    subject.focus();
                }
            }, 40);
        }
    }

    function close() {
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

    function alertHtml(kind, title, message) {
        var icon = kind === "success" ? "bx-check" : "bx-error-circle";
        var wrap = document.createElement("div");
        wrap.className = "support-alert support-alert-" + (kind === "success" ? "success" : "danger");
        wrap.setAttribute("role", "alert");
        wrap.innerHTML =
            '<div class="support-alert-icon"><i class="bx ' + icon + '"></i></div>' +
            '<div class="support-alert-body"><strong></strong><div></div></div>' +
            '<button type="button" class="btn-close support-alert-close" data-close-support-alert></button>';
        wrap.querySelector("strong").textContent = title || "";
        wrap.querySelector(".support-alert-body div").textContent = message || "";
        return wrap;
    }

    function showAlert(kind, title, message) {
        var el = overlay();
        if (!el) {
            return;
        }
        var host = el.querySelector("[data-support-case-alerts]");
        if (!host) {
            return;
        }
        host.innerHTML = "";
        host.appendChild(alertHtml(kind, title, message));
    }

    function setFieldError(name, message) {
        var el = overlay();
        if (!el) {
            return;
        }
        var span = el.querySelector('[data-valmsg="' + name + '"]');
        if (span) {
            span.textContent = message || "";
        }
    }

    function clearFieldErrors() {
        setFieldError("Subject", "");
        setFieldError("Message", "");
    }

    function bind(el) {
        if (!el || el.getAttribute("data-support-bound") === "true") {
            return;
        }
        el.setAttribute("data-support-bound", "true");

        var form = el.querySelector("[data-support-case-form]");
        if (!form) {
            return;
        }

        form.addEventListener("submit", function (event) {
            event.preventDefault();
            clearFieldErrors();
            var submitButton = form.querySelector('button[type="submit"]');
            if (submitButton && submitButton.disabled) {
                return;
            }
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
                        var error = new Error(payload.message || response.statusText || "Error");
                        error.payload = payload;
                        throw error;
                    }
                    return payload;
                });
            }).then(function (payload) {
                var subject = form.querySelector('input[name="Subject"]');
                var message = form.querySelector('textarea[name="Message"]');
                if (subject) {
                    subject.value = "";
                }
                if (message) {
                    message.value = "";
                }
                showAlert("success", "", payload.message || "");
            }).catch(function (err) {
                var payload = err && err.payload ? err.payload : {};
                var errors = payload.errors || {};
                if (errors.Subject) {
                    setFieldError("Subject", errors.Subject);
                }
                if (errors.Message) {
                    setFieldError("Message", errors.Message);
                }
                if (payload.message) {
                    showAlert("danger", "", payload.message);
                }
            }).then(function () {
                if (submitButton) {
                    submitButton.disabled = false;
                }
            });
        });
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
            next.setAttribute("data-support-standalone", "false");
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
        if (element.hasAttribute("data-open-support-case")) {
            return true;
        }
        var href = element.getAttribute("href") || "";
        return /\/SupportTickets\/OpenNewCase\/?(\?|#|$)/i.test(href);
    }

    document.addEventListener("click", function (event) {
        var closer = event.target.closest("[data-support-case-close]");
        if (closer) {
            event.preventDefault();
            close();
            return;
        }

        var dismiss = event.target.closest("[data-close-support-alert]");
        if (dismiss) {
            event.preventDefault();
            var alertBox = dismiss.closest(".support-alert");
            if (alertBox) {
                alertBox.classList.add("is-hiding");
                window.setTimeout(function () {
                    alertBox.remove();
                }, 180);
            }
            return;
        }

        var el = overlay();
        if (el && event.target === el) {
            close();
            return;
        }

        var opener = event.target.closest("[data-open-support-case], a[href]");
        if (!opener || !isOpener(opener)) {
            return;
        }

        event.preventDefault();
        ensureOpen();
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && isOpen()) {
            close();
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
