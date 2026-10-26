/**
 * Universal Responsive Table Pagination & Page Size (Limit) Controller
 * Works seamlessly with all tabular views in City Assessor Portal.
 */
(function () {
    function initTablePagination(card) {
        if (!card || card.dataset.paginationInitialized) return;
        card.dataset.paginationInitialized = "true";

        var table = card.querySelector("table");
        if (!table) return;

        var tbody = table.querySelector("tbody");
        if (!tbody) return;

        var limitSelect = card.querySelector(".table-limit-select") || document.querySelector(".table-limit-select[data-target='" + (card.id || "") + "']");
        var footer = card.querySelector(".table-pagination-footer");
        var showingStart = card.querySelector(".table-showing-start");
        var showingEnd = card.querySelector(".table-showing-end");
        var totalCountSpan = card.querySelector(".table-total-count");
        var buttonsContainer = card.querySelector(".table-pagination-buttons");

        var currentPage = 1;
        var pageSize = 10;

        if (limitSelect) {
            pageSize = parseInt(limitSelect.value, 10) || 10;
        }

        function getDataRows() {
            var rows = Array.from(tbody.querySelectorAll("tr"));
            // Filter out empty-state rows (rows containing td with colspan >= 5 or class no-records-row)
            return rows.filter(function (tr) {
                var td = tr.querySelector("td");
                if (!td) return false;
                if (tr.classList.contains("no-records-row")) return false;
                if (td.hasAttribute("colspan") && parseInt(td.getAttribute("colspan"), 10) >= 4) return false;
                return true;
            });
        }

        function renderPagination() {
            var allRows = getDataRows();
            var total = allRows.length;

            if (total === 0) {
                if (showingStart) showingStart.textContent = "0";
                if (showingEnd) showingEnd.textContent = "0";
                if (totalCountSpan) totalCountSpan.textContent = "0";
                if (buttonsContainer) buttonsContainer.innerHTML = "";
                if (footer) footer.classList.add("opacity-50");
                return;
            }

            if (footer) footer.classList.remove("opacity-50");

            var totalPages = Math.ceil(total / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            var startIndex = (currentPage - 1) * pageSize;
            var endIndex = Math.min(startIndex + pageSize, total);

            // Toggle visibility of rows
            allRows.forEach(function (row, idx) {
                if (idx >= startIndex && idx < endIndex) {
                    row.style.display = "";
                } else {
                    row.style.display = "none";
                }
            });

            // Update counter texts
            if (showingStart) showingStart.textContent = (startIndex + 1).toString();
            if (showingEnd) showingEnd.textContent = endIndex.toString();
            if (totalCountSpan) totalCountSpan.textContent = total.toString();

            // Render buttons
            if (buttonsContainer) {
                buttonsContainer.innerHTML = "";

                // Prev button
                var prevBtn = document.createElement("button");
                prevBtn.type = "button";
                prevBtn.className = "px-3 py-1.5 rounded-xl border border-slate-200 dark:border-stone-800 text-xs font-mono font-bold transition-all " +
                    (currentPage <= 1 ? "opacity-40 cursor-not-allowed bg-slate-50 dark:bg-stone-900 text-slate-400" : "bg-white dark:bg-stone-900 text-slate-700 dark:text-stone-300 hover:bg-slate-100 dark:hover:bg-stone-800 shadow-xs");
                prevBtn.innerHTML = "&lsaquo; Prev";
                prevBtn.disabled = currentPage <= 1;
                prevBtn.addEventListener("click", function () {
                    if (currentPage > 1) {
                        currentPage--;
                        renderPagination();
                    }
                });
                buttonsContainer.appendChild(prevBtn);

                // Page number buttons
                var maxVisibleButtons = 5;
                var startPage = Math.max(1, currentPage - 2);
                var endPage = Math.min(totalPages, startPage + maxVisibleButtons - 1);

                if (endPage - startPage < maxVisibleButtons - 1) {
                    startPage = Math.max(1, endPage - maxVisibleButtons + 1);
                }

                if (startPage > 1) {
                    var p1Btn = createPageBtn(1);
                    buttonsContainer.appendChild(p1Btn);
                    if (startPage > 2) {
                        var ellipsis = document.createElement("span");
                        ellipsis.className = "px-1.5 py-1 text-slate-400 font-bold";
                        ellipsis.textContent = "...";
                        buttonsContainer.appendChild(ellipsis);
                    }
                }

                for (var p = startPage; p <= endPage; p++) {
                    buttonsContainer.appendChild(createPageBtn(p));
                }

                if (endPage < totalPages) {
                    if (endPage < totalPages - 1) {
                        var ellipsis2 = document.createElement("span");
                        ellipsis2.className = "px-1.5 py-1 text-slate-400 font-bold";
                        ellipsis2.textContent = "...";
                        buttonsContainer.appendChild(ellipsis2);
                    }
                    var lastBtn = createPageBtn(totalPages);
                    buttonsContainer.appendChild(lastBtn);
                }

                // Next button
                var nextBtn = document.createElement("button");
                nextBtn.type = "button";
                nextBtn.className = "px-3 py-1.5 rounded-xl border border-slate-200 dark:border-stone-800 text-xs font-mono font-bold transition-all " +
                    (currentPage >= totalPages ? "opacity-40 cursor-not-allowed bg-slate-50 dark:bg-stone-900 text-slate-400" : "bg-white dark:bg-stone-900 text-slate-700 dark:text-stone-300 hover:bg-slate-100 dark:hover:bg-stone-800 shadow-xs");
                nextBtn.innerHTML = "Next &rsaquo;";
                nextBtn.disabled = currentPage >= totalPages;
                nextBtn.addEventListener("click", function () {
                    if (currentPage < totalPages) {
                        currentPage++;
                        renderPagination();
                    }
                });
                buttonsContainer.appendChild(nextBtn);
            }
        }

        function createPageBtn(pageNo) {
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "min-w-[32px] px-2.5 py-1.5 rounded-xl text-xs font-mono font-bold transition-all " +
                (pageNo === currentPage
                    ? "bg-emerald-600 text-white shadow-xs"
                    : "border border-slate-200 dark:border-stone-800 bg-white dark:bg-stone-900 text-slate-700 dark:text-stone-300 hover:bg-slate-100 dark:hover:bg-stone-800 shadow-xs");
            btn.textContent = pageNo.toString();
            btn.addEventListener("click", function () {
                currentPage = pageNo;
                renderPagination();
            });
            return btn;
        }

        // Limit Change Event Listener
        if (limitSelect) {
            limitSelect.addEventListener("change", function () {
                pageSize = parseInt(this.value, 10) || 10;
                currentPage = 1;
                renderPagination();
            });
        }

        // Initial render
        renderPagination();
    }

    // Auto-discover all tables on page load
    function initAllTables() {
        var cards = document.querySelectorAll(".paginated-table-card");
        cards.forEach(function (card) {
            initTablePagination(card);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initAllTables);
    } else {
        initAllTables();
    }

    window.initTablePagination = initTablePagination;
    window.initAllPaginatedTables = initAllTables;
})();
