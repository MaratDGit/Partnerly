(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.search = {

        setup: function (ctx) {

            ctx.highlightSearchText =
                function () {

                    if (!ctx.body) {
                        return;
                    }


                    var previousHighlights =
                        ctx.body.querySelectorAll(
                            ".data-table-search-highlight"
                        );


                    previousHighlights.forEach(
                        function (element) {

                            var parent =
                                element.parentNode;

                            if (!parent) {
                                return;
                            }

                            parent.replaceChild(
                                document.createTextNode(
                                    element.textContent
                                ),
                                element
                            );

                            parent.normalize();
                        }
                    );


                    if (
                        !ctx.state.search
                    ) {
                        return;
                    }


                    var searchValue =
                        String(
                            ctx.state.search
                        ).trim();


                    if (!searchValue) {
                        return;
                    }


                    var searchRegex =
                        new RegExp(
                            ctx.escapeRegExp(
                                searchValue
                            ),
                            "gi"
                        );


                    var walker =
                        document.createTreeWalker(
                            ctx.body,
                            NodeFilter.SHOW_TEXT,
                            {
                                acceptNode:
                                    function (node) {

                                        if (
                                            !node.nodeValue ||
                                            !node.nodeValue.trim()
                                        ) {

                                            return NodeFilter.FILTER_REJECT;
                                        }


                                        var parent =
                                            node.parentElement;


                                        if (!parent) {
                                            return NodeFilter.FILTER_REJECT;
                                        }


                                        if (
                                            parent.closest(
                                                "button, input, textarea, select"
                                            )
                                        ) {

                                            return NodeFilter.FILTER_REJECT;
                                        }


                                        if (
                                            parent.closest(
                                                ".data-table-actions"
                                            )
                                        ) {

                                            return NodeFilter.FILTER_REJECT;
                                        }


                                        return NodeFilter.FILTER_ACCEPT;
                                    }
                            }
                        );


                    var nodes =
                        [];

                    var node;


                    while (
                        (
                            node =
                            walker.nextNode()
                        )
                    ) {

                        nodes.push(
                            node
                        );
                    }


                    nodes.forEach(
                        function (textNode) {

                            var text =
                                textNode.nodeValue;


                            if (
                                !searchRegex.test(
                                    text
                                )
                            ) {

                                searchRegex.lastIndex =
                                    0;

                                return;
                            }


                            searchRegex.lastIndex =
                                0;


                            var fragment =
                                document.createDocumentFragment();


                            var lastIndex =
                                0;


                            text.replace(
                                searchRegex,
                                function (
                                    match,
                                    offset
                                ) {

                                    if (
                                        offset >
                                        lastIndex
                                    ) {

                                        fragment.appendChild(
                                            document.createTextNode(
                                                text.substring(
                                                    lastIndex,
                                                    offset
                                                )
                                            )
                                        );
                                    }


                                    var highlight =
                                        document.createElement(
                                            "mark"
                                        );


                                    highlight.className =
                                        "data-table-search-highlight";


                                    highlight.textContent =
                                        match;


                                    fragment.appendChild(
                                        highlight
                                    );


                                    lastIndex =
                                        offset +
                                        match.length;


                                    return match;
                                }
                            );


                            if (
                                lastIndex <
                                text.length
                            ) {

                                fragment.appendChild(
                                    document.createTextNode(
                                        text.substring(
                                            lastIndex
                                        )
                                    )
                                );
                            }


                            textNode.parentNode.replaceChild(
                                fragment,
                                textNode
                            );
                        }
                    );
                };
        },


        initialize: function (ctx) {

            if (!ctx.searchInput) {
                return;
            }


            ctx.searchInput.addEventListener(
                "input",
                function () {

                    clearTimeout(
                        ctx.searchTimer
                    );


                    ctx.searchTimer =
                        setTimeout(
                            function () {

                                ctx.state.search =
                                    ctx.searchInput.value.trim();

                                ctx.state.page =
                                    1;

                                ctx.load();

                            },
                            (
                                ctx.options.search &&
                                ctx.options.search.debounceMs
                            ) || 300
                        );
                }
            );
        }
    };

})(window, document);