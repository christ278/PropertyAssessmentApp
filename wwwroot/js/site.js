// -----------------------------------------------------------------------------
// REAL PROPERTY TAX CONCEPT SPINNER & PROCESSING OVERLAY CONTROLLER
// -----------------------------------------------------------------------------
window.RptSpinner = (function () {
    var overlay = null;
    var titleEl = null;
    var subtitleEl = null;
    var stepEl = null;
    var currentTimer = null;
    var safetyTimer = null;
    var defaultSteps = [
        "Verifying Land & Structure Registry...",
        "Evaluating Assessment Levels & Valuation Schedule...",
        "Executing Ledger & Tax Roll Balance Updates...",
        "Securing Assessment Trail & Stamping Transaction..."
    ];
    var stepIndex = 0;

    function init() {
        overlay = document.getElementById("appProcessingOverlay");
        if (overlay) {
            titleEl = document.getElementById("appProcessingTitle");
            subtitleEl = document.getElementById("appProcessingSubtitle");
            stepEl = document.getElementById("appProcessingStep");
        }
    }

    function show(options) {
        if (!overlay) init();
        if (!overlay) return;

        var opt = options || {};
        var title = opt.title || "Processing Assessment Record";
        var subtitle = opt.subtitle || "City Assessor FAAS & Real Property Tax Administration System";
        var step = opt.step || null;

        if (titleEl) titleEl.innerText = title;
        if (subtitleEl) subtitleEl.innerText = subtitle;

        stepIndex = 0;
        var activeSteps = (opt.steps && Array.isArray(opt.steps) && opt.steps.length > 0) ? opt.steps : defaultSteps;
        if (stepEl) {
            stepEl.innerText = step || activeSteps[0];
            if (currentTimer) clearInterval(currentTimer);
            if (activeSteps.length > 1) {
                currentTimer = setInterval(function () {
                    stepIndex = (stepIndex + 1) % activeSteps.length;
                    if (stepEl) stepEl.innerText = activeSteps[stepIndex];
                }, 1800);
            }
        }

        // Safety fallback timer: auto-hide after 15 seconds so UI is never permanently frozen
        if (safetyTimer) clearTimeout(safetyTimer);
        safetyTimer = setTimeout(function () {
            hide();
        }, 15000);

        overlay.classList.remove("hidden");
        // Trigger smooth transition
        setTimeout(function () {
            overlay.classList.remove("opacity-0");
            overlay.classList.add("opacity-100");
        }, 10);
    }

    function hide() {
        if (safetyTimer) {
            clearTimeout(safetyTimer);
            safetyTimer = null;
        }

        if (!overlay) init();
        if (!overlay) return;

        if (currentTimer) {
            clearInterval(currentTimer);
            currentTimer = null;
        }

        overlay.classList.remove("opacity-100");
        overlay.classList.add("opacity-0");
        setTimeout(function () {
            overlay.classList.add("hidden");
        }, 260);

        // Remove processing state from any marked button
        document.querySelectorAll(".btn-is-processing").forEach(function (btn) {
            btn.classList.remove("btn-is-processing");
            if (btn.dataset.originalHtml) {
                btn.innerHTML = btn.dataset.originalHtml;
                delete btn.dataset.originalHtml;
            }
        });
    }

    return {
        show: show,
        hide: hide
    };
})();

// Automatic hook for form submissions, navigation clicks & print actions
document.addEventListener("DOMContentLoaded", function () {
    var isNavigating = false;

    // 1. Intercept standard form submissions (Save, Update, Delete, Submit Assessment, Forward, Pay)
    document.addEventListener("submit", function (e) {
        // Crucial: If an earlier listener (e.g. client validation) called preventDefault(), NEVER display spinner
        if (e.defaultPrevented) return;

        var form = e.target;
        if (!form) return;

        // Skip if form has data-no-spinner or is marked novalidate that failed
        if (form.getAttribute("data-no-spinner") === "true") return;

        // Check HTML5 validity if supported
        if (typeof form.checkValidity === "function" && !form.checkValidity()) {
            return;
        }

        // Determine contextual label based on submitter or form action
        var submitter = e.submitter;
        var customTitle = "Processing Property Record...";
        var customSub = "Updating Real Property Tax Ledger & Valuation";

        if (submitter) {
            var btnText = (submitter.innerText || "").trim().toLowerCase();
            if (submitter.getAttribute("data-spinner-title")) {
                customTitle = submitter.getAttribute("data-spinner-title");
            } else if (btnText.indexOf("approve") !== -1) {
                customTitle = "Authorizing & Approving Assessment...";
                customSub = "Generating Official Tax Declaration Number & City Assessor Stamp";
            } else if (btnText.indexOf("reassess") !== -1 || btnText.indexOf("reassessment") !== -1) {
                customTitle = "Computing Reassessment & Tax Dues...";
                customSub = "Applying Schedule of Base Unit Construction Costs & Depreciation";
            } else if (btnText.indexOf("calculate") !== -1 || btnText.indexOf("compute") !== -1) {
                customTitle = "Computing Valuation & Tax Dues...";
                customSub = "Calculating Market Value, Assessment Level %, and Annual RPT Due";
            } else if (btnText.indexOf("forward") !== -1 || btnText.indexOf("route") !== -1) {
                customTitle = "Routing Real Property Transaction...";
                customSub = "Forwarding to Next Assessment Station / Division in Sequence";
            } else if (btnText.indexOf("pay") !== -1 || btnText.indexOf("receipt") !== -1 || btnText.indexOf("order of payment") !== -1) {
                customTitle = "Issuing Order of Payment / O.R...";
                customSub = "Posting Assessor Service Fee to Official City Treasury Ledger";
            } else if (btnText.indexOf("delete") !== -1 || btnText.indexOf("cancel") !== -1) {
                customTitle = "Cancelling / Archiving Record...";
                customSub = "Updating Assessment Roll Status & Audit Trails";
            } else if (btnText.indexOf("save") !== -1 || btnText.indexOf("create") !== -1 || btnText.indexOf("submit") !== -1) {
                customTitle = "Encoding & Saving Assessment...";
                customSub = "Writing Land, Building, or Machinery FAAS Details into Local Roll";
            }

            // Inline button spinner style
            submitter.classList.add("btn-is-processing");
            submitter.dataset.originalHtml = submitter.innerHTML;
            submitter.innerHTML = '<span class="inline-flex items-center gap-2"><svg class="animate-spin h-4 w-4 viewBox=0 0 24 24" fill="none"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path></svg><span>Processing...</span></span>';
        }

        window.RptSpinner.show({
            title: customTitle,
            subtitle: customSub
        });
    });

    // Dismiss spinner if any form control fails HTML5 constraint validation
    document.addEventListener("invalid", function () {
        if (window.RptSpinner) window.RptSpinner.hide();
    }, true);

    // 2. Intercept print / PDF downloads & action links (e.g. Master Record PDF, Certificate PDF, Print Sheet)
    document.addEventListener("click", function (e) {
        var btn = e.target.closest("a, button");
        if (!btn) return;

        var href = btn.getAttribute("href") || btn.getAttribute("data-href") || "";
        var onclick = btn.getAttribute("onclick") || "";
        var isPdfOrPrint = href.indexOf("Print") !== -1 || href.indexOf("Pdf") !== -1 || href.indexOf("Certificate") !== -1 ||
            onclick.indexOf("Print") !== -1 || onclick.indexOf("print") !== -1;

        if (btn.classList.contains("no-spinner") || btn.getAttribute("data-no-spinner") === "true") return;

        // If clicking a print or PDF generation link
        if (isPdfOrPrint && href && href !== "#" && !href.startsWith("javascript:")) {
            window.RptSpinner.show({
                title: "Generating Official Tax Document...",
                subtitle: "Rendering Field Appraisal & Assessment Sheet (Annex I-E / I-F Layout)"
            });

            // Automatically hide overlay after download has had time to start
            setTimeout(function () {
                window.RptSpinner.hide();
            }, 3500);
        }
    });

    // 3. Intercept navigation bar clicks (Pool Transactions, My Transactions, Assessment Records, and all nav items)
    // with 1-second delay so users can appreciate the animated Cadastral Assessor spinner.
    document.addEventListener("click", function (e) {
        // Block subsequent link clicks while transition countdown is active
        if (isNavigating) {
            var anyLink = e.target.closest("a");
            if (anyLink) {
                e.preventDefault();
                return;
            }
        }

        var link = e.target.closest("a");
        if (!link) return;

        // Skip middle-click, ctrl-click, meta-click, or right-click (allow open in new tab)
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) return;

        // Skip external or new tab targets
        if (link.getAttribute("target") === "_blank") return;

        // Skip download links or explicit opt-out
        if (link.hasAttribute("download") || link.classList.contains("no-spinner") || link.getAttribute("data-no-spinner") === "true") return;

        // Get target href
        var href = link.getAttribute("href");
        if (!href || href === "#" || href.startsWith("javascript:") || href.startsWith("#")) return;

        // Skip modal openers, toggles, or JS-driven action links
        var onclick = link.getAttribute("onclick") || "";
        if (onclick.indexOf("open") !== -1 || onclick.indexOf("toggle") !== -1 || onclick.indexOf("close") !== -1 || onclick.indexOf("Modal") !== -1) return;

        // Check if this is a navigation bar button/link:
        // - Sidebar nav links (.nav-item or inside sidebar <nav>)
        // - Horizontal top navbar links (.horizontal-nav-only a, #horizontalRequestsDropdownMenu a)
        // - Pool Transactions or My Transactions links/buttons anywhere on the page
        var isSidebarNav = link.classList.contains("nav-item") || (link.closest("nav") && !link.closest(".table-pagination-buttons"));
        var isHorizontalNav = link.closest(".horizontal-nav-only") || link.closest("#horizontalRequestsDropdownMenu");

        var hrefLower = href.toLowerCase();
        var linkTextLower = (link.innerText || "").trim().toLowerCase();

        var isPoolOrMyTransaction = hrefLower.indexOf("/workflow/pool") !== -1 ||
                                    hrefLower.indexOf("/workflow/mytransactions") !== -1 ||
                                    linkTextLower.indexOf("pool transaction") !== -1 ||
                                    linkTextLower.indexOf("my transaction") !== -1;

        var isNavbarAction = isSidebarNav || isHorizontalNav || isPoolOrMyTransaction;

        if (!isNavbarAction) return;

        // Contextual title & description for each module
        var config = {
            title: "Navigating to Workspace...",
            subtitle: "Office of the City Assessor • FAIRTRACK System",
            step: "Loading requested module & registry data..."
        };

        if (linkTextLower.indexOf("pool") !== -1 || hrefLower.indexOf("/workflow/pool") !== -1) {
            config.title = "Loading Workflow Routing Pool...";
            config.subtitle = "Departmental unassigned transactions & incoming documents";
            config.step = "Syncing live routing queue & division ledger...";
        } else if (linkTextLower.indexOf("my trans") !== -1 || linkTextLower.indexOf("my transaction") !== -1 || hrefLower.indexOf("/workflow/mytransactions") !== -1) {
            config.title = "Opening My Transactions Workspace...";
            config.subtitle = "Staff claimed assessment transactions & active working files";
            config.step = "Loading assigned property declarations & workflow trail...";
        } else if (linkTextLower.indexOf("system owner") !== -1 || hrefLower.indexOf("/dashboard/systemowner") !== -1) {
            config.title = "Loading System Owner Dashboard...";
            config.subtitle = "LGU-wide real property tax administration & infrastructure telemetry";
            config.step = "Aggregating City Assessor system metrics...";
        } else if (linkTextLower.indexOf("assessor head") !== -1 || linkTextLower.indexOf("head dashboard") !== -1 || hrefLower.indexOf("/dashboard/assessorhead") !== -1) {
            config.title = "Loading Assessor Head Executive Dashboard...";
            config.subtitle = "Operational performance, tax roll totals & pending approvals";
            config.step = "Fetching executive roll summaries & division statistics...";
        } else if (linkTextLower.indexOf("my dashboard") !== -1 || (linkTextLower.indexOf("dashboard") !== -1 && hrefLower.indexOf("/dashboard/staff") !== -1)) {
            config.title = "Loading Staff Assessment Dashboard...";
            config.subtitle = "Individual queue status, pending tasks & operational targets";
            config.step = "Fetching active task allocations...";
        } else if (linkTextLower.indexOf("assessment record") !== -1 || (linkTextLower.indexOf("record") !== -1 && hrefLower.indexOf("/assessment") !== -1)) {
            config.title = "Opening Assessment Records...";
            config.subtitle = "Master Real Property Tax Declarations & FAAS Registry";
            config.step = "Querying digital assessment roll & active property records...";
        } else if (linkTextLower.indexOf("owner") !== -1 || hrefLower.indexOf("/owner") !== -1) {
            config.title = "Opening Taxpayer / Owner Directory...";
            config.subtitle = "Registered Declarant Profiles, Tax Identification Numbers (TIN) & Holdings";
            config.step = "Accessing master taxpayer database...";
        } else if (linkTextLower.indexOf("property assessment") !== -1 || linkTextLower.indexOf("rpa") !== -1 || hrefLower.indexOf("/propertyassessmentrequest") !== -1) {
            config.title = "Loading Property Assessment (RPA)...";
            config.subtitle = "Subdivision, Consolidation, Discovery & Reassessment Requests";
            config.step = "Fetching assessment transaction partitions & filings...";
        } else if (linkTextLower.indexOf("inspection") !== -1 || linkTextLower.indexOf("rfi") !== -1 || hrefLower.indexOf("/requestforinspection") !== -1) {
            config.title = "Loading Inspection Requests (RFI)...";
            config.subtitle = "Field Appraisal, Site Inspections & Ocular Property Verification";
            config.step = "Loading ocular verification schedules & appraiser files...";
        } else if (linkTextLower.indexOf("faas encoding") !== -1 || linkTextLower.indexOf("wizard") !== -1 || hrefLower.indexOf("/assessment/create") !== -1) {
            config.title = "Launching FAAS Encoding Wizard...";
            config.subtitle = "Field Appraisal & Assessment Sheet digital registration";
            config.step = "Preparing structural schedules & valuation matrices...";
        } else if (linkTextLower.indexOf("land holding") !== -1 || hrefLower.indexOf("/landholdingrequest") !== -1) {
            config.title = "Loading Land Holding Requests...";
            config.subtitle = "Certificates of Total Landholdings & No-Property Verifications";
            config.step = "Accessing landholder index registry...";
        } else if (linkTextLower.indexOf("tax declaration request") !== -1 || hrefLower.indexOf("/taxdeclarationrequest") !== -1) {
            config.title = "Loading Tax Declaration Requests...";
            config.subtitle = "Certified True Copy Issuance & Official Property Records Inquiry";
            config.step = "Retrieving official request logs...";
        } else if (linkTextLower.indexOf("tax mapping") !== -1 || hrefLower.indexOf("/taxmapping") !== -1) {
            config.title = "Loading Spatial Tax Mapping...";
            config.subtitle = "GIS Cadastral Mapping, Section, Block & Lot Registry";
            config.step = "Connecting to GIS mapping layers & parcel boundaries...";
        } else if (linkTextLower.indexOf("order of payment") !== -1 || hrefLower.indexOf("/orderpayment") !== -1) {
            config.title = "Loading Order of Payment Fees...";
            config.subtitle = "Assessor Service Fee Matrix & Treasury Billing Records";
            config.step = "Fetching service fee itemization...";
        } else if (linkTextLower.indexOf("user account") !== -1 || hrefLower.indexOf("/useraccount") !== -1) {
            config.title = "Opening User Accounts...";
            config.subtitle = "Assessor Personnel Profiles, Roles & Security Credentials";
            config.step = "Verifying security access controls...";
        } else if (linkTextLower.indexOf("user role") !== -1 || hrefLower.indexOf("/userrole") !== -1) {
            config.title = "Opening User Roles & Permissions...";
            config.subtitle = "Role-Based Access Control & Department Division Workflows";
            config.step = "Loading permission matrices & division security...";
        } else {
            var label = link.querySelector("span") ? link.querySelector("span").innerText.trim() : link.innerText.trim();
            if (label) {
                config.title = "Loading " + label + "...";
            }
        }

        // Close mobile drawer if open
        if (typeof closeMobileSidebar === "function") {
            try { closeMobileSidebar(); } catch (err) { }
        }

        // Prevent immediate navigation
        e.preventDefault();
        isNavigating = true;

        // Show spinner immediately
        window.RptSpinner.show(config);

        // Delay 1 second (1000ms) as requested so the user can appreciate the animation
        setTimeout(function () {
            window.location.href = href;
        }, 1000);
    });

    // Hide spinner and reset navigation lock if page is shown from BFCache (back-forward navigation)
    window.addEventListener("pageshow", function (event) {
        isNavigating = false;
        if (window.RptSpinner) {
            window.RptSpinner.hide();
        }
    });
});
