'use strict';

document.addEventListener('DOMContentLoaded', function () {

    const body = document.body;

    const sidebar =
        document.getElementById('partnerlySidebar');

    const sidebarToggle =
        document.querySelector('[data-sidebar-toggle]');

    const sidebarClose =
        document.querySelector('[data-sidebar-close]');

    const mobileToggles =
        document.querySelectorAll('[data-mobile-toggle]');

    const overlay =
        document.querySelector('[data-sidebar-overlay]');


    /*
     * =========================================================
     * Desktop sidebar collapsed state
     * =========================================================
     */

    const SIDEBAR_STATE_KEY =
        'partnerly-sidebar-collapsed';


    function setDesktopCollapsed(collapsed) {

        if (window.innerWidth <= 1199.98) {
            return;
        }

        body.classList.toggle(
            'partnerly-sidebar-collapsed',
            collapsed
        );

        localStorage.setItem(
            SIDEBAR_STATE_KEY,
            collapsed ? 'true' : 'false'
        );
    }


    function restoreDesktopSidebarState() {

        if (window.innerWidth <= 1199.98) {
            return;
        }

        const collapsed =
            localStorage.getItem(SIDEBAR_STATE_KEY) === 'true';

        body.classList.toggle(
            'partnerly-sidebar-collapsed',
            collapsed
        );
    }


    restoreDesktopSidebarState();


    if (sidebarToggle) {

        sidebarToggle.addEventListener(
            'click',
            function (event) {

                event.preventDefault();

                if (window.innerWidth <= 1199.98) {
                    return;
                }

                const collapsed =
                    !body.classList.contains(
                        'partnerly-sidebar-collapsed'
                    );

                setDesktopCollapsed(collapsed);
            }
        );
    }


    /*
     * =========================================================
     * Mobile sidebar
     * =========================================================
     */

    function openMobileSidebar() {

        if (!sidebar) {
            return;
        }

        sidebar.classList.add('mobile-open');

        if (overlay) {
            overlay.classList.add('show');
        }

        body.classList.add(
            'partnerly-mobile-menu-open'
        );
    }


    function closeMobileSidebar() {

        if (!sidebar) {
            return;
        }

        sidebar.classList.remove('mobile-open');

        if (overlay) {
            overlay.classList.remove('show');
        }

        body.classList.remove(
            'partnerly-mobile-menu-open'
        );
    }


    function toggleMobileSidebar() {

        if (!sidebar) {
            return;
        }

        if (sidebar.classList.contains('mobile-open')) {
            closeMobileSidebar();
        }
        else {
            openMobileSidebar();
        }
    }


    mobileToggles.forEach(function (button) {

        button.addEventListener(
            'click',
            function (event) {

                event.preventDefault();

                toggleMobileSidebar();
            }
        );

    });


    /*
     * Explicit mobile close button
     */

    if (sidebarClose) {

        sidebarClose.addEventListener(
            'click',
            function (event) {

                event.preventDefault();

                closeMobileSidebar();
            }
        );

    }


    /*
     * Click outside sidebar
     */

    if (overlay) {

        overlay.addEventListener(
            'click',
            function () {

                closeMobileSidebar();

            }
        );

    }


    /*
     * Click anywhere outside sidebar
     *
     * This is an additional protection in case something
     * is rendered above/beside the overlay.
     */

    document.addEventListener(
        'click',
        function (event) {

            if (window.innerWidth > 1199.98) {
                return;
            }

            if (!sidebar ||
                !sidebar.classList.contains('mobile-open')) {
                return;
            }

            const clickedInsideSidebar =
                sidebar.contains(event.target);

            const clickedMobileButton =
                event.target.closest('[data-mobile-toggle]');

            if (
                !clickedInsideSidebar &&
                !clickedMobileButton
            ) {
                closeMobileSidebar();
            }

        }
    );


    /*
     * Close after selecting a menu item
     */

    if (sidebar) {

        sidebar.addEventListener(
            'click',
            function (event) {

                const link =
                    event.target.closest(
                        '.partnerly-menu-link'
                    );

                if (!link) {
                    return;
                }

                if (window.innerWidth <= 1199.98) {
                    closeMobileSidebar();
                }

            }
        );

    }


    /*
     * Escape
     */

    document.addEventListener(
        'keydown',
        function (event) {

            if (event.key !== 'Escape') {
                return;
            }

            closeMobileSidebar();

        }
    );


    /*
     * Responsive state
     */

    let previousWidth =
        window.innerWidth;


    window.addEventListener(
        'resize',
        function () {

            const currentWidth =
                window.innerWidth;


            if (
                previousWidth > 1199.98 &&
                currentWidth <= 1199.98
            ) {

                closeMobileSidebar();

            }


            if (
                previousWidth <= 1199.98 &&
                currentWidth > 1199.98
            ) {

                closeMobileSidebar();

                restoreDesktopSidebarState();

            }


            previousWidth =
                currentWidth;

        }
    );

});