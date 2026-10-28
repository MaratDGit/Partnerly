$(function () {

    const loader = document.getElementById("globalLoader");

    if (!loader) {
        return;
    }

    let loaderTimeout = null;
    let visible = false;

    function showLoader() {
        clearTimeout(loaderTimeout);
        visible = true;
        loader.style.display = "flex";
        loader.removeAttribute("hidden");
        loader.setAttribute("aria-hidden", "false");

        loaderTimeout = setTimeout(function () {
            hideLoader();
        }, 15000);
    }

    function hideLoader() {
        clearTimeout(loaderTimeout);
        loaderTimeout = null;
        visible = false;
        loader.style.display = "none";
        loader.setAttribute("hidden", "");
        loader.setAttribute("aria-hidden", "true");
    }

    function hasNoLoaderFlag(element) {
        if (!element) {
            return false;
        }
        return element.hasAttribute("data-no-loader") ||
            $(element).closest("[data-no-loader]").length > 0;
    }

    function isEventCancelled(event) {
        return !!(event && (
            event.defaultPrevented ||
            (typeof event.isDefaultPrevented === "function" && event.isDefaultPrevented())
        ));
    }

    function jqueryAjaxBusy() {
        return typeof $ !== "undefined" && typeof $.active === "number" && $.active > 0;
    }

    function hideLoaderIfPageReady() {
        if (document.readyState === "complete" && !jqueryAjaxBusy()) {
            hideLoader();
        }
    }

    function showLoaderUnlessCancelled(event) {
        if (isEventCancelled(event)) {
            hideLoader();
            return;
        }

        showLoader();

        window.setTimeout(function () {
            if (isEventCancelled(event)) {
                hideLoader();
            }
        }, 0);
    }

    $(document).ajaxStart(function () {
        showLoader();
    });

    $(document).ajaxStop(function () {
        hideLoaderIfPageReady();
    });

    $(document).ajaxError(function () {
        hideLoaderIfPageReady();
    });

    $(document).on("submit", "form", function (event) {
        if (hasNoLoaderFlag(this)) {
            hideLoader();
            return;
        }

        showLoaderUnlessCancelled(event);
    });

    $(document).on("click", "a", function (event) {
        const href = $(this).attr("href");

        if (
            hasNoLoaderFlag(this) ||
            !href ||
            href.startsWith("#") ||
            href.startsWith("javascript:") ||
            $(this).attr("target") === "_blank" ||
            $(this).attr("data-bs-toggle")
        ) {
            return;
        }

        showLoaderUnlessCancelled(event);
    });

    window.addEventListener("pageshow", hideLoader);
    window.addEventListener("pagehide", hideLoader);
    window.addEventListener("load", hideLoaderIfPageReady);
    document.addEventListener("visibilitychange", function () {
        if (document.visibilityState === "visible") {
            hideLoaderIfPageReady();
        }
    });

    if (document.readyState === "complete") {
        hideLoader();
    }

    window.PartnerlyLoader = {
        show: showLoader,
        hide: hideLoader,
        hideIfReady: hideLoaderIfPageReady,
        isVisible: function () { return visible; }
    };
});
