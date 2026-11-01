(function (window, document) {

    "use strict";

    window.DataTableModules =
        window.DataTableModules || {};

    window.DataTableModules.bulkDelete = {

        setup: function (ctx) {

            var options = ctx.options || {};
            var bulk = options.bulkDelete;

            if (!bulk || bulk.enabled === false) {
                return;
            }

            var button = ctx.root.querySelector("[data-table-bulk-delete]");
            var modal = ctx.root.querySelector("#" + options.id + "-bulk-delete-modal") ||
                document.getElementById(options.id + "-bulk-delete-modal");

            if (!button || !modal) {
                return;
            }

            if (modal.parentElement !== document.body) {
                document.body.appendChild(modal);
            }

            var selectedEl = modal.querySelector("[data-bulk-delete-selected]");
            var deletedEl = modal.querySelector("[data-bulk-delete-deleted]");
            var remainingEl = modal.querySelector("[data-bulk-delete-remaining]");
            var progressEl = modal.querySelector("[data-bulk-delete-progress]");
            var statusEl = modal.querySelector("[data-bulk-delete-status]");
            var startButton = modal.querySelector("[data-bulk-delete-start]");
            var stopButton = modal.querySelector("[data-bulk-delete-stop]");
            var closeButton = modal.querySelector("[data-bulk-delete-close]");
            var titleEl = modal.querySelector("[data-bulk-delete-title]");

            var session = {
                queue: [],
                selected: 0,
                deleted: 0,
                failed: 0,
                running: false,
                stopRequested: false,
                closeAfterStop: false,
                logged: false
            };

            function text(key, fallback) {
                if (typeof dataTableLocalization !== "undefined" && dataTableLocalization[key]) {
                    return dataTableLocalization[key];
                }
                return fallback;
            }

            function format(template, value) {
                return String(template || "").replace("{0}", String(value));
            }

            function hasSelectedColumn() {
                var columns = options.columns || [];
                for (var i = 0; i < columns.length; i++) {
                    var column = columns[i];
                    if (column &&
                        String(column.field).toLowerCase() === "selected" &&
                        column.visible !== false) {
                        return true;
                    }
                }
                return false;
            }

            ctx.updateBulkDeleteButton = function () {
                var selectedCount = ctx.state && ctx.state.selectedRows
                    ? ctx.state.selectedRows.length
                    : 0;

                var visible = hasSelectedColumn() && selectedCount > 0 && !session.running;
                button.hidden = !visible;
            };

            function selectedCount() {
                return ctx.state && ctx.state.selectedRows
                    ? ctx.state.selectedRows.length
                    : 0;
            }

            function remainingCount() {
                return session.queue.length;
            }

            function setStatus(message) {
                if (statusEl) {
                    statusEl.textContent = message || "";
                }
            }

            function updateStats() {
                if (selectedEl) {
                    selectedEl.textContent = String(session.selected);
                }
                if (deletedEl) {
                    deletedEl.textContent = String(session.deleted);
                }
                if (remainingEl) {
                    remainingEl.textContent = String(remainingCount());
                }
                if (progressEl) {
                    var total = session.selected || 0;
                    var done = session.deleted + session.failed;
                    var percent = total > 0 ? done / total : 0;
                    progressEl.style.transform = "scaleX(" + percent + ")";
                }

                if (startButton) {
                    startButton.disabled = session.running || remainingCount() === 0;
                }
                if (stopButton) {
                    stopButton.disabled = !session.running;
                }
            }

            function getModal() {
                if (window.bootstrap && bootstrap.Modal) {
                    return bootstrap.Modal.getOrCreateInstance(modal, {
                        backdrop: "static",
                        keyboard: false
                    });
                }
                return null;
            }

            function showModal() {
                var instance = getModal();
                if (instance) {
                    instance.show();
                    return;
                }
                modal.classList.add("show");
                modal.style.display = "block";
            }

            function hideModal() {
                var instance = getModal();
                if (instance) {
                    instance.hide();
                    return;
                }
                modal.classList.remove("show");
                modal.style.display = "none";
            }

            function keysFor(id) {
                var map = ctx.state.selectedRowKeys || {};
                return map[id] || {};
            }

            function resetSessionFromSelection() {
                var ids = (ctx.state.selectedRows || []).slice();
                session.queue = ids.map(function (id) {
                    return {
                        id: String(id),
                        keys: keysFor(id)
                    };
                });
                session.selected = session.queue.length;
                session.deleted = 0;
                session.failed = 0;
                session.running = false;
                session.stopRequested = false;
                session.closeAfterStop = false;
                session.logged = false;
            }

            function writeLog() {
                if (session.logged || session.deleted <= 0 || !bulk.logUrl) {
                    return Promise.resolve();
                }

                session.logged = true;

                return fetch(bulk.logUrl, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": ctx.getAntiForgeryToken()
                    },
                    body: JSON.stringify({
                        TableID: options.id,
                        Selected: session.selected,
                        Deleted: session.deleted,
                        Remaining: remainingCount(),
                        Stopped: session.stopRequested
                    })
                }).catch(function () {
                    session.logged = false;
                });
            }

            function deleteOne(item) {
                var payload = {
                    TableID: options.id,
                    id: item.id,
                    keys: item.keys || {}
                };

                if (typeof ctx.addParentKeysToPayload === "function") {
                    ctx.addParentKeysToPayload(payload);
                }

                return fetch(bulk.url, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": ctx.getAntiForgeryToken()
                    },
                    body: JSON.stringify(payload)
                }).then(function (response) {
                    if (typeof ctx.handleActionResponse === "function") {
                        return ctx.handleActionResponse(response);
                    }
                    if (!response.ok) {
                        throw new Error("Action failed.");
                    }
                    return response.json();
                });
            }

            function finishRun(complete) {
                session.running = false;
                updateStats();
                ctx.updateBulkDeleteButton();

                if (complete) {
                    setStatus(text("bulkDeleteComplete", "Deletion completed"));
                    if (titleEl) {
                        titleEl.textContent = text("bulkDeleteComplete", "Deletion completed");
                    }
                } else {
                    setStatus(text("bulkDelete", "Bulk delete"));
                }

                return writeLog().then(function () {
                    if (typeof ctx.load === "function") {
                        ctx.load();
                    }
                    if (session.closeAfterStop) {
                        session.closeAfterStop = false;
                        hideModal();
                    }
                });
            }

            function processNext() {
                if (!session.running) {
                    return;
                }

                if (session.stopRequested) {
                    finishRun(false);
                    return;
                }

                if (session.queue.length === 0) {
                    finishRun(true);
                    return;
                }

                var item = session.queue[0];

                deleteOne(item)
                    .then(function () {
                        session.queue.shift();
                        session.deleted += 1;

                        if (typeof ctx.removeSelectedRow === "function") {
                            ctx.removeSelectedRow(item.id);
                        }

                        if (ctx.state.selectedRowKeys) {
                            delete ctx.state.selectedRowKeys[item.id];
                        }

                        updateStats();
                        processNext();
                    })
                    .catch(function (error) {
                        session.queue.shift();
                        session.failed += 1;
                        updateStats();
                        setStatus(error && error.message
                            ? error.message
                            : text("bulkDeleteFailed", "Unable to delete record."));
                        processNext();
                    });
            }

            function confirmStart() {
                var count = remainingCount();
                if (count <= 0) {
                    return false;
                }

                return window.confirm(
                    format(
                        text("bulkDeleteStartConfirm", "Delete {0} selected records?"),
                        count
                    )
                );
            }

            function confirmStop() {
                return window.confirm(
                    text("bulkDeleteStopConfirm", "Stop the deletion process?")
                );
            }

            button.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();

                if (!hasSelectedColumn() || selectedCount() === 0) {
                    return;
                }

                resetSessionFromSelection();
                if (titleEl) {
                    titleEl.textContent = text("bulkDelete", "Bulk delete");
                }
                setStatus("");
                updateStats();
                showModal();
            });

            if (startButton) {
                startButton.addEventListener("click", function (event) {
                    event.preventDefault();

                    if (session.running || remainingCount() === 0) {
                        return;
                    }

                    if (!confirmStart()) {
                        return;
                    }

                    session.running = true;
                    session.stopRequested = false;
                    session.logged = false;
                    if (titleEl) {
                        titleEl.textContent = text("bulkDeleteProcessing", "Deleting");
                    }
                    setStatus(text("bulkDeleteProcessing", "Deleting"));
                    updateStats();
                    button.hidden = true;
                    processNext();
                });
            }

            if (stopButton) {
                stopButton.addEventListener("click", function (event) {
                    event.preventDefault();

                    if (!session.running) {
                        return;
                    }

                    if (!confirmStop()) {
                        return;
                    }

                    session.stopRequested = true;
                });
            }

            function requestClose() {
                if (session.running) {
                    if (!confirmStop()) {
                        return;
                    }
                    session.stopRequested = true;
                    session.closeAfterStop = true;
                    return;
                }

                writeLog().then(hideModal);
            }

            if (closeButton) {
                closeButton.addEventListener("click", function (event) {
                    event.preventDefault();
                    requestClose();
                });
            }

            modal.addEventListener("hide.bs.modal", function (event) {
                if (session.running) {
                    event.preventDefault();
                    requestClose();
                }
            });

            ctx.updateBulkDeleteButton();
        }
    };

})(window, document);
