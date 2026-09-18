$(function () {

    const loader = document.getElementById("globalLoader");

    if (!loader) {
        return;
    }

    let loaderTimeout = null;


    function showLoader() {

        clearTimeout(loaderTimeout);

        // show
        loader.style.display = "flex";

        // max time to show — 15 seconds
        loaderTimeout = setTimeout(function () {
            hideLoader();
        }, 15000);
    }


    function hideLoader() {

        clearTimeout(loaderTimeout);
        loaderTimeout = null;
        loader.style.display = "none";
    }


    // ==========================================
    // AJAX
    // ==========================================

    $(document).ajaxStart(function () {
        showLoader();
    });

    $(document).ajaxStop(function () {
        hideLoader();
    });


    // ==========================================
    // FORM SUBMIT
    // ==========================================

    $(document).on("submit", "form", function () {
        showLoader();
    });


    // ==========================================
    // LINKS
    // ==========================================

    $(document).on("click", "a", function () {

        const href = $(this).attr("href");

        if (
            !href ||
            href.startsWith("#") ||
            href.startsWith("javascript:") ||
            $(this).attr("target") === "_blank" ||
            $(this).attr("data-bs-toggle") ||
            $(this).attr("data-no-loader")
        ) {
            return;
        }

        showLoader();
    });


    // ==========================================
    // BACK / FORWARD
    // ==========================================

    window.addEventListener("pageshow", function () {
        hideLoader();
    });


    // ==========================================
    // BEFORE PAGE GOES INTO BFCache
    // ==========================================

    window.addEventListener("pagehide", function () {
        hideLoader();
    });

});
