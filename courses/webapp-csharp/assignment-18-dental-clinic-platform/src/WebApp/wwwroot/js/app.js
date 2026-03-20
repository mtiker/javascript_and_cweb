(() => {
    const sessionStorageKey = "dental-saas-ui-session";
    const themeStorageKey = "dental-saas-ui-theme";
    const maxLogEntries = 60;
    const baseTitle = "Dental Clinic SaaS Console";
    const screenRouteMap = {
        overview: "/app/overview",
        platform: "/app/platform",
        support: "/app/support",
        billing: "/app/billing",
        auth: "/app/auth",
        patients: "/app/patients",
        resources: "/app/resources",
        appointments: "/app/appointments",
        plans: "/app/finance",
        team: "/app/team",
        settings: "/app/settings",
        logs: "/app/logs"
    };
    const screenTitleMap = {
        overview: "Overview",
        platform: "Platform",
        support: "Support",
        billing: "Billing",
        auth: "Access",
        patients: "Patients",
        resources: "Resources",
        appointments: "Schedule",
        plans: "Finance",
        team: "Team",
        settings: "Settings",
        logs: "API Logs"
    };
    const routeScreenMap = Object.fromEntries(
        Object.entries(screenRouteMap).map(([screenId, route]) => [normalizeRoute(route), screenId])
    );
    routeScreenMap["/app/plans"] = "plans";
    const validScreens = new Set(Object.keys(screenRouteMap));
    const permanentToothGroups = [
        [18, 17, 16, 15, 14, 13, 12, 11],
        [21, 22, 23, 24, 25, 26, 27, 28],
        [48, 47, 46, 45, 44, 43, 42, 41],
        [31, 32, 33, 34, 35, 36, 37, 38]
    ];
    const permanentToothNumbers = permanentToothGroups.flat();
    const toothChartRows = [
        {
            arch: "upper",
            teeth: [...permanentToothGroups[0], ...permanentToothGroups[1]]
        },
        {
            arch: "lower",
            teeth: [...permanentToothGroups[2], ...permanentToothGroups[3]]
        }
    ];
    const svgNamespace = "http://www.w3.org/2000/svg";
    const toothConditionOptions = [
        "Healthy",
        "Caries",
        "Filled",
        "Crown",
        "RootCanal",
        "Missing"
    ];

	    const state = {
	        ...loadSession(),
	        patients: [],
	        patientProfile: null,
	        selectedPatientToothNumber: null,
	        dentistProfile: null,
	        editingTreatmentRoomId: null,
	        dentists: [],
        dentistDirectory: {},
        treatmentRooms: [],
	        treatmentTypes: [],
        treatmentPlans: [],
	        appointments: [],
	        openPlanItems: [],
	        companyUsers: [],
	        companySettings: null,
	        subscription: null,
	        costEstimates: [],
        financeWorkspace: null,
        financePatientId: "",
        financeInvoiceDetail: null,
        financeSelectedInvoiceId: "",
        systemAnalytics: null,
        featureFlags: [],
	        supportCompanies: [],
	        supportTickets: [],
	        billingSubscriptions: [],
	        billingInvoices: [],
	        pendingDelete: {
	            id: "",
	            label: ""
	        }
	    };

    const logEntries = [];
	    const elements = {
        authPill: document.getElementById("auth-pill"),
        companyPill: document.getElementById("company-pill"),
        appRoot: document.getElementById("app-root"),
        gatewayScreen: document.getElementById("gateway-screen"),
        gatewayLoginForm: document.getElementById("gateway-login-form"),
        authGrid: document.getElementById("auth-grid"),
        onboardingCard: document.getElementById("onboarding-card"),
        sessionRole: document.getElementById("session-role"),
        sessionTenant: document.getElementById("session-tenant"),
        sessionExpiry: document.getElementById("session-expiry"),
        overviewAuth: document.getElementById("overview-auth"),
        overviewTenant: document.getElementById("overview-tenant"),
        overviewPatientCount: document.getElementById("overview-patient-count"),
        overviewSyncStatus: document.getElementById("overview-sync-status"),
        platformCompanyCount: document.getElementById("platform-company-count"),
        platformUserCount: document.getElementById("platform-user-count"),
        platformInvoiceCount: document.getElementById("platform-invoice-count"),
        logBox: document.getElementById("log-box"),
        patientsBody: document.getElementById("patients-body"),
        patientsWorkspace: document.getElementById("patients-workspace"),
        patientProfilePanel: document.getElementById("patient-profile-panel"),
        patientProfileName: document.getElementById("patient-profile-name"),
        patientProfileMeta: document.getElementById("patient-profile-meta"),
        patientProfileIssueCount: document.getElementById("patient-profile-issue-count"),
        patientProfileTreatmentCount: document.getElementById("patient-profile-treatment-count"),
        patientProfileDob: document.getElementById("patient-profile-dob"),
        patientProfileCode: document.getElementById("patient-profile-code"),
        patientProfileEmail: document.getElementById("patient-profile-email"),
        patientProfilePhone: document.getElementById("patient-profile-phone"),
        patientProfileForm: document.getElementById("patient-profile-form"),
        patientProfileBackButton: document.getElementById("patient-profile-back-btn"),
        patientProfileRefreshButton: document.getElementById("patient-profile-refresh-btn"),
        patientProfileDeleteButton: document.getElementById("patient-profile-delete-btn"),
        patientToothChart: document.getElementById("patient-tooth-chart"),
        patientToothHoverCard: document.getElementById("patient-tooth-hover-card"),
        patientSelectedToothTitle: document.getElementById("patient-selected-tooth-title"),
        patientSelectedToothSummary: document.getElementById("patient-selected-tooth-summary"),
        patientSelectedToothHistory: document.getElementById("patient-selected-tooth-history"),
        themeToggle: document.getElementById("theme-toggle"),
        logoutButton: document.getElementById("logout-btn"),
        refreshPatientsButton: document.getElementById("refresh-patients-btn"),
        refreshResourcesButton: document.getElementById("refresh-resources-btn"),
        refreshAppointmentsButton: document.getElementById("refresh-appointments-btn"),
        refreshPlanItemsButton: document.getElementById("refresh-plan-items-btn"),
        refreshFinanceButton: document.getElementById("refresh-finance-btn"),
        refreshPlatformButton: document.getElementById("refresh-platform-btn"),
        refreshSupportButton: document.getElementById("refresh-support-btn"),
        refreshBillingButton: document.getElementById("refresh-billing-btn"),
        refreshCompanyUsersButton: document.getElementById("refresh-company-users-btn"),
        refreshCompanySettingsButton: document.getElementById("refresh-company-settings-btn"),
        refreshSubscriptionButton: document.getElementById("refresh-subscription-btn"),
        resourcesWorkspace: document.getElementById("resources-workspace"),
        dentistProfilePanel: document.getElementById("dentist-profile-panel"),
        dentistProfileName: document.getElementById("dentist-profile-name"),
        dentistProfileMeta: document.getElementById("dentist-profile-meta"),
        dentistProfileAppointmentCount: document.getElementById("dentist-profile-appointment-count"),
        dentistProfileUpcomingCount: document.getElementById("dentist-profile-upcoming-count"),
        dentistProfilePastCount: document.getElementById("dentist-profile-past-count"),
        dentistProfileLicense: document.getElementById("dentist-profile-license"),
        dentistProfileSpecialty: document.getElementById("dentist-profile-specialty"),
        dentistProfileId: document.getElementById("dentist-profile-id"),
        dentistProfileForm: document.getElementById("dentist-profile-form"),
        dentistProfileBackButton: document.getElementById("dentist-profile-back-btn"),
        dentistProfileRefreshButton: document.getElementById("dentist-profile-refresh-btn"),
        dentistProfileDeleteButton: document.getElementById("dentist-profile-delete-btn"),
	        dentistProfileAppointmentsSummary: document.getElementById("dentist-profile-appointments-summary"),
	        dentistProfileAppointmentsBody: document.getElementById("dentist-profile-appointments-body"),
	        dentistProfileAppointmentSearch: document.getElementById("dentistProfileAppointmentSearch"),
	        dentistProfileAppointmentFilter: document.getElementById("dentistProfileAppointmentFilter"),
	        treatmentRoomSubmitButton: document.getElementById("treatment-room-submit-btn"),
	        treatmentRoomCancelButton: document.getElementById("treatment-room-cancel-btn"),
	        onboardingForm: document.getElementById("onboarding-form"),
        loginForm: document.getElementById("login-form"),
        switchForm: document.getElementById("switch-form"),
        forgotPasswordForm: document.getElementById("forgot-password-form"),
        resetPasswordForm: document.getElementById("reset-password-form"),
        patientForm: document.getElementById("patient-form"),
        dentistForm: document.getElementById("dentist-form"),
        treatmentRoomForm: document.getElementById("treatment-room-form"),
        appointmentForm: document.getElementById("appointment-form"),
        planDecisionForm: document.getElementById("plan-decision-form"),
        costEstimateForm: document.getElementById("cost-estimate-form"),
        legalEstimateForm: document.getElementById("legal-estimate-form"),
        legalEstimateOutput: document.getElementById("legal-estimate-output"),
        featureFlagForm: document.getElementById("feature-flag-form"),
        companyActivationForm: document.getElementById("company-activation-form"),
        supportTicketForm: document.getElementById("support-ticket-form"),
        billingSubscriptionForm: document.getElementById("billing-subscription-form"),
        billingInvoiceStatusForm: document.getElementById("billing-invoice-status-form"),
        subscriptionForm: document.getElementById("subscription-form"),
        subscriptionPill: document.getElementById("subscription-pill"),
	        dentistsBody: document.getElementById("dentists-body"),
	        treatmentRoomsBody: document.getElementById("treatment-rooms-body"),
	        appointmentsBody: document.getElementById("appointments-body"),
	        appointmentsSummary: document.getElementById("appointments-summary"),
	        appointmentSearch: document.getElementById("appointmentSearch"),
	        appointmentStatusFilter: document.getElementById("appointmentStatusFilter"),
	        appointmentsPastToggleButton: document.getElementById("appointments-past-toggle-btn"),
	        appointmentClinicalForm: document.getElementById("appointment-clinical-form"),
        appointmentClinicalItems: document.getElementById("appointment-clinical-items"),
        appointmentClinicalAppointmentSelect: document.getElementById("appointmentClinicalAppointmentId"),
        appointmentClinicalPerformedAtInput: document.getElementById("appointmentClinicalPerformedAt"),
        appointmentClinicalMarkCompleted: document.getElementById("appointmentClinicalMarkCompleted"),
        appointmentClinicalAddRowButton: document.getElementById("appointment-clinical-add-row-btn"),
        appointmentClinicalItemTemplate: document.getElementById("appointment-clinical-item-template"),
        planItemsBody: document.getElementById("plan-items-body"),
        featureFlagsBody: document.getElementById("feature-flags-body"),
        supportCompaniesBody: document.getElementById("support-companies-body"),
        supportTicketsBody: document.getElementById("support-tickets-body"),
        billingSubscriptionsBody: document.getElementById("billing-subscriptions-body"),
        billingInvoicesBody: document.getElementById("billing-invoices-body"),
        appointmentPatientSelect: document.getElementById("appointmentPatientId"),
        appointmentDentistSelect: document.getElementById("appointmentDentistId"),
        appointmentRoomSelect: document.getElementById("appointmentRoomId"),
	        planItemSelection: document.getElementById("planItemSelection"),
	        estimatePatientSelect: document.getElementById("estimatePatientId"),
        financePatientSelect: document.getElementById("financePatientId"),
        financeSummary: document.getElementById("finance-summary"),
        financePlanSelect: document.getElementById("financePlanId"),
        financePlanStatus: document.getElementById("financePlanStatus"),
        financePlanSubmitButton: document.getElementById("finance-plan-submit-btn"),
        financePlanItemsBody: document.getElementById("finance-plan-items-body"),
        financePolicyForm: document.getElementById("finance-policy-form"),
        financePolicyResetButton: document.getElementById("finance-policy-reset-btn"),
        financePolicyInsurancePlanSelect: document.getElementById("financePolicyInsurancePlanId"),
        financePoliciesBody: document.getElementById("finance-policies-body"),
        financeEstimatesBody: document.getElementById("finance-estimates-body"),
        financeInvoiceGenerateForm: document.getElementById("finance-invoice-generate-form"),
        financeInvoiceEstimateSelect: document.getElementById("financeInvoiceEstimateId"),
        financeProceduresBody: document.getElementById("finance-procedures-body"),
        financeInvoicesBody: document.getElementById("finance-invoices-body"),
        financeInvoiceDetailSummary: document.getElementById("finance-invoice-detail-summary"),
        financeInvoiceLinesBody: document.getElementById("finance-invoice-lines-body"),
        financePaymentForm: document.getElementById("finance-payment-form"),
        financePaymentsBody: document.getElementById("finance-payments-body"),
        financePaymentPlanForm: document.getElementById("finance-payment-plan-form"),
        financePaymentPlanInstallments: document.getElementById("finance-payment-plan-installments"),
        financeInstallmentsBody: document.getElementById("finance-installments-body"),
        financeAddInstallmentButton: document.getElementById("finance-add-installment-btn"),
        paymentPlanInstallmentTemplate: document.getElementById("payment-plan-installment-template"),
        companyUserForm: document.getElementById("company-user-form"),
        companyRoleSwitchForm: document.getElementById("company-role-switch-form"),
        companyRoleSwitchSelect: document.getElementById("companyRoleSwitchRole"),
        companyRoleSwitchHelp: document.getElementById("company-role-switch-help"),
        companyUsersBody: document.getElementById("company-users-body"),
        companySettingsForm: document.getElementById("company-settings-form"),
        loadingBar: document.getElementById("global-loading"),
        toastRegion: document.getElementById("toast-region"),
        patientDeleteDialog: document.getElementById("patient-delete-dialog"),
	        patientDeleteText: document.getElementById("patient-delete-text"),
	        confirmDeleteButton: document.getElementById("confirm-delete-btn")
	    };
	    const moduleRegistrations = Array.isArray(window.__dentalAppModules) ? window.__dentalAppModules : [];
	    const app = {
	        state,
	        elements,
	        screenRouteMap,
	        screenTitleMap,
	        routeScreenMap,
	        validScreens,
	        permanentToothGroups,
	        permanentToothNumbers,
	        toothChartRows,
	        svgNamespace,
	        toothConditionOptions
	    };

    function bindEvents() {
        bindAsyncSubmit(elements.gatewayLoginForm, onGatewayLoginSubmit);
        bindAsyncSubmit(elements.onboardingForm, onOnboardingSubmit);
        bindAsyncSubmit(elements.loginForm, onLoginSubmit);
        bindAsyncSubmit(elements.switchForm, onSwitchSubmit);
        bindAsyncSubmit(elements.forgotPasswordForm, onForgotPasswordSubmit);
        bindAsyncSubmit(elements.resetPasswordForm, onResetPasswordSubmit);
        bindAsyncSubmit(elements.patientForm, onPatientCreateSubmit);
        bindAsyncSubmit(elements.patientProfileForm, onPatientProfileSubmit);
        bindAsyncSubmit(elements.dentistForm, onDentistCreateSubmit);
        bindAsyncSubmit(elements.dentistProfileForm, onDentistProfileSubmit);
        bindAsyncSubmit(elements.treatmentRoomForm, onTreatmentRoomCreateSubmit);
        bindAsyncSubmit(elements.appointmentForm, onAppointmentCreateSubmit);
        bindAsyncSubmit(elements.appointmentClinicalForm, onAppointmentClinicalSubmit);
	        bindAsyncSubmit(elements.planDecisionForm, onPlanDecisionSubmit);
	        bindAsyncSubmit(elements.costEstimateForm, onCostEstimateSubmit);
	        bindAsyncSubmit(elements.legalEstimateForm, onLegalEstimateSubmit);
        bindAsyncSubmit(elements.financePolicyForm, onFinancePolicySubmit);
        bindAsyncSubmit(elements.financeInvoiceGenerateForm, onFinanceInvoiceGenerateSubmit);
        bindAsyncSubmit(elements.financePaymentForm, onFinancePaymentSubmit);
        bindAsyncSubmit(elements.financePaymentPlanForm, onFinancePaymentPlanSubmit);
	        bindAsyncSubmit(elements.featureFlagForm, onFeatureFlagSubmit);
        bindAsyncSubmit(elements.companyActivationForm, onCompanyActivationSubmit);
        bindAsyncSubmit(elements.supportTicketForm, onSupportTicketSubmit);
        bindAsyncSubmit(elements.billingSubscriptionForm, onBillingSubscriptionSubmit);
        bindAsyncSubmit(elements.billingInvoiceStatusForm, onBillingInvoiceStatusSubmit);
        bindAsyncSubmit(elements.subscriptionForm, onSubscriptionSubmit);
        bindAsyncSubmit(elements.companyUserForm, onCompanyUserUpsertSubmit);
        bindAsyncSubmit(elements.companyRoleSwitchForm, onCompanyRoleSwitchSubmit);
        bindAsyncSubmit(elements.companySettingsForm, onCompanySettingsSubmit);

        bindAsyncClick(elements.refreshPatientsButton, async () => {
            await refreshPatients({ trigger: elements.refreshPatientsButton });
        });
        bindAsyncClick(elements.patientProfileRefreshButton, async () => {
            await refreshPatientProfile({ trigger: elements.patientProfileRefreshButton });
        });
        bindAsyncClick(elements.patientProfileBackButton, async () => {
            closePatientProfile();
        });
        bindAsyncClick(elements.patientProfileDeleteButton, async () => {
            if (state.patientProfile) {
                openDeleteDialog(state.patientProfile);
            }
        });
        bindAsyncClick(elements.dentistProfileRefreshButton, async () => {
            await refreshDentistProfile({ trigger: elements.dentistProfileRefreshButton });
        });
        bindAsyncClick(elements.dentistProfileBackButton, async () => {
            closeDentistProfile();
        });
        bindAsyncClick(elements.dentistProfileDeleteButton, async () => {
            await onDentistProfileDelete();
        });
        bindAsyncClick(elements.refreshResourcesButton, async () => {
            await refreshResources({ trigger: elements.refreshResourcesButton });
        });
        bindAsyncClick(elements.refreshAppointmentsButton, async () => {
            await refreshAppointments({ trigger: elements.refreshAppointmentsButton });
        });
	        bindAsyncClick(elements.refreshPlanItemsButton, async () => {
	            await refreshOpenPlanItems({ trigger: elements.refreshPlanItemsButton });
	        });
        bindAsyncClick(elements.refreshFinanceButton, async () => {
            await refreshFinanceWorkspace({ trigger: elements.refreshFinanceButton });
        });
        bindAsyncClick(elements.financePlanSubmitButton, async () => {
            await onFinancePlanSubmit();
        });
        bindSyncClick(elements.financePolicyResetButton, () => {
            resetFinancePolicyForm();
        });
        bindAsyncClick(elements.refreshPlatformButton, async () => {
            await refreshPlatformData({ trigger: elements.refreshPlatformButton });
        });
        bindAsyncClick(elements.refreshSupportButton, async () => {
            await refreshSupportData({ trigger: elements.refreshSupportButton });
        });
        bindAsyncClick(elements.refreshBillingButton, async () => {
            await refreshBillingData({ trigger: elements.refreshBillingButton });
        });
        bindAsyncClick(elements.refreshCompanyUsersButton, async () => {
            await refreshCompanyUsers({ trigger: elements.refreshCompanyUsersButton });
        });
        bindAsyncClick(elements.refreshCompanySettingsButton, async () => {
            await refreshCompanySettings({ trigger: elements.refreshCompanySettingsButton });
        });
        bindAsyncClick(elements.refreshSubscriptionButton, async () => {
            await refreshTenantSubscription({ trigger: elements.refreshSubscriptionButton });
        });
	        bindSyncClick(elements.appointmentClinicalAddRowButton, () => {
	            addAppointmentClinicalEntryRow();
	        });
        bindSyncClick(elements.financeAddInstallmentButton, () => {
            addFinanceInstallmentRow();
        });
        bindSyncChange(elements.financePatientSelect, () => {
            void onFinancePatientChange();
        });
        bindSyncChange(elements.financePlanSelect, () => {
            renderFinancePlanReview();
        });
        bindSyncChange(elements.appointmentClinicalAppointmentSelect, () => {
            renderAppointmentClinicalSelectOptions();
        });
	        bindSyncInput(elements.dentistProfileAppointmentSearch, () => {
	            renderDentistProfileAppointments();
	        });
	        bindSyncChange(elements.dentistProfileAppointmentFilter, () => {
	            renderDentistProfileAppointments();
	        });
	        bindSyncInput(elements.appointmentSearch, () => {
	            renderAppointments(state.appointments);
	        });
	        bindSyncChange(elements.appointmentStatusFilter, () => {
	            renderAppointments(state.appointments);
	        });
	        bindSyncClick(elements.appointmentsPastToggleButton, () => {
	            const isShowingPast = elements.appointmentsPastToggleButton.dataset.showPast === "true";
	            elements.appointmentsPastToggleButton.dataset.showPast = isShowingPast ? "false" : "true";
	            renderAppointments(state.appointments);
	        });
	        bindSyncClick(elements.treatmentRoomCancelButton, () => {
	            resetTreatmentRoomForm();
	        });

	        bindAsyncClick(elements.logoutButton, onLogoutClick);
        bindSyncClick(elements.themeToggle, onThemeToggle);

        if (elements.patientDeleteDialog) {
            elements.patientDeleteDialog.addEventListener("close", onDeleteDialogClose);
        }

        const tabButtons = document.querySelectorAll("[data-tab-target]");
        tabButtons.forEach((button) => {
            button.addEventListener("click", () => {
                const target = button.getAttribute("data-tab-target");
                if (target) {
                    activateScreen(target);
                }
            });
        });

        window.addEventListener("popstate", () => {
            activateScreen(getRequestedScreen(), { updatePath: false });
        });
    }

    function initializeTheme() {
        const savedTheme = localStorage.getItem(themeStorageKey) || "dark";
        applyTheme(savedTheme);
    }

    function onThemeToggle() {
        const current = document.documentElement.getAttribute("data-theme");
        const next = current === "dark" ? "light" : "dark";
        applyTheme(next);
        localStorage.setItem(themeStorageKey, next);
    }

    function applyTheme(themeName) {
        const safeTheme = themeName === "light" ? "light" : "dark";
        document.documentElement.setAttribute("data-theme", safeTheme);

        if (elements.themeToggle) {
            const isDark = safeTheme === "dark";
            elements.themeToggle.textContent = isDark ? "Dark mode" : "Light mode";
            elements.themeToggle.setAttribute("aria-pressed", isDark ? "true" : "false");
        }
    }

    function initializeTabs() {
        activateScreen(getRequestedScreen(), { updatePath: false });
    }

    function getRequestedScreen() {
        const pathScreen = routeScreenMap[normalizeRoute(window.location.pathname)];
        if (pathScreen) {
            return pathScreen;
        }

        // Backward compatibility for old hash-based links.
        const legacyHash = (window.location.hash || "").replace("#", "").trim();
        if (validScreens.has(legacyHash)) {
            return legacyHash;
        }

        return "overview";
    }

    function activateScreen(screenId, options = {}) {
        const updatePath = options.updatePath !== false;
        const useReplaceState = options.replaceHistory === true;
        const safeScreen = validScreens.has(screenId) ? screenId : "overview";
        const panels = document.querySelectorAll("[data-screen]");
        const tabs = document.querySelectorAll("[data-tab-target]");

        panels.forEach((panel) => {
            const isMatch = panel.getAttribute("data-screen") === safeScreen;
            panel.hidden = !isMatch;
        });

        tabs.forEach((tab) => {
            const isMatch = tab.getAttribute("data-tab-target") === safeScreen;
            if (tab.classList.contains("tab")) {
                tab.classList.toggle("is-active", isMatch);
                tab.setAttribute("aria-selected", isMatch ? "true" : "false");
            }
        });

        if (updatePath) {
            const nextPath = screenRouteMap[safeScreen] || screenRouteMap.overview;
            const currentPath = normalizeRoute(window.location.pathname);
            if (nextPath !== currentPath) {
                if (useReplaceState) {
                    history.replaceState(null, "", nextPath);
                } else {
                    history.pushState(null, "", nextPath);
                }
            }
        }

        const screenTitle = screenTitleMap[safeScreen] || "Workspace";
        document.title = `${screenTitle} | ${baseTitle}`;
    }

    async function apiRequest(url, options) {
        const requestOptions = {
            method: options.method || "GET",
            headers: {
                "Content-Type": "application/json"
            }
        };

        if (options.auth) {
            requireJwt();
            requestOptions.headers.Authorization = `Bearer ${state.jwt}`;
        }

        if (options.body !== undefined) {
            requestOptions.body = JSON.stringify(options.body);
        }

        const response = await fetch(url, requestOptions);
        const contentType = response.headers.get("content-type") || "";
        const rawBody = await response.text();
        let parsedBody = rawBody;

        if (contentType.includes("application/json") && rawBody.length > 0) {
            try {
                parsedBody = JSON.parse(rawBody);
            } catch {
                parsedBody = rawBody;
            }
        }

        if (!response.ok) {
            log(options.tag || "API/ERROR", options.body || null, parsedBody, true);
            if (options.auth && response.status === 401) {
                clearSession();
                renderSession();
                syncPublicEntryState();
                throw new Error("Session expired. Sign in again.");
            }
            const errorMessage = readErrorMessage(parsedBody) || `HTTP ${response.status}`;
            throw new Error(errorMessage);
        }

        return rawBody.length === 0 ? {} : parsedBody;
    }

    function renderPlanItemsSkeleton() {
        if (!elements.planItemsBody) {
            return;
        }

        clearElement(elements.planItemsBody);
        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 6; cellIndex += 1) {
                const cell = document.createElement("td");
                const skeleton = document.createElement("div");
                skeleton.className = "skeleton";
                skeleton.style.width = `${45 + (cellIndex * 8)}%`;
                cell.appendChild(skeleton);
                row.appendChild(cell);
            }
            elements.planItemsBody.appendChild(row);
        }
    }

    function resolvePatientName(patientId) {
        const patient = resolvePatient(patientId);
        if (!patient) {
            return patientId || "-";
        }

        return `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || patient.id;
    }

    function resolvePatient(patientId) {
        return state.patients.find((item) => item.id === patientId) || null;
    }

    function canManagePatientsUi() {
        return Boolean(
            state.companySlug
            && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee")
        );
    }

    function canManageAppointmentsUi() {
        return Boolean(
            state.companySlug
            && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee")
        );
    }

    function applyJwtResponse(data) {
        state.jwt = data.jwt || "";
        state.refreshToken = data.refreshToken || "";
        state.expiresInSeconds = Number(data.expiresInSeconds || 0);
        state.companySlug = data.activeCompanySlug || state.companySlug || "";
        state.companyRole = data.activeCompanyRole || state.companyRole || "";
        state.systemRoles = extractSystemRolesFromJwt(state.jwt);
    }

	    function clearSession() {
	        state.jwt = "";
	        state.refreshToken = "";
	        state.expiresInSeconds = 0;
        state.companySlug = "";
        state.companyRole = "";
        state.systemRoles = [];
        state.patients = [];
        state.patientProfile = null;
        state.selectedPatientToothNumber = null;
        state.dentistProfile = null;
        state.dentists = [];
        state.dentistDirectory = {};
        state.treatmentRooms = [];
	        state.treatmentTypes = [];
	        state.appointments = [];
	        state.openPlanItems = [];
	        state.companyUsers = [];
	        state.companySettings = null;
	        state.subscription = null;
	        state.costEstimates = [];
	        state.treatmentPlans = [];
	        state.financeWorkspace = null;
	        state.financePatientId = "";
	        state.financeInvoiceDetail = null;
	        state.financeSelectedInvoiceId = "";
	        state.systemAnalytics = null;
	        state.featureFlags = [];
	        state.supportCompanies = [];
	        state.supportTickets = [];
	        state.billingSubscriptions = [];
	        state.billingInvoices = [];
	        state.pendingDelete = { id: "", label: "" };
	        saveSession();
	    }

    function requireJwt() {
        if (!state.jwt) {
            throw new Error("Sign in before running this action.");
        }
    }

    function requireTenant() {
        requireJwt();
        if (!state.companySlug) {
            throw new Error("No active tenant. Login or switch company first.");
        }
        if (!state.companyRole) {
            throw new Error("Active tenant exists, but this session has no company role for tenant endpoints.");
        }
    }

    function hasCompanyRole(...allowedRoles) {
        if (!state.companyRole) {
            return false;
        }

        return allowedRoles.includes(state.companyRole);
    }

    function hasSystemRole(...allowedRoles) {
        if (!Array.isArray(state.systemRoles) || state.systemRoles.length === 0) {
            return false;
        }

        return state.systemRoles.some((role) => allowedRoles.includes(role));
    }

    function hasAnySystemRole() {
        return Array.isArray(state.systemRoles) && state.systemRoles.length > 0;
    }

    function requireSystemRole(...allowedRoles) {
        requireJwt();
        if (!hasSystemRole(...allowedRoles)) {
            throw new Error("Current user role has no access to this system action.");
        }
    }

    function resolveLandingScreen() {
        if (hasSystemRole("SystemAdmin")) {
            return "platform";
        }
        if (hasSystemRole("SystemSupport")) {
            return "support";
        }
        if (hasSystemRole("SystemBilling")) {
            return "billing";
        }
        if (hasCompanyRole("CompanyOwner")) {
            return "team";
        }
        if (hasCompanyRole("CompanyAdmin")) {
            return "team";
        }
        if (hasCompanyRole("CompanyManager")) {
            return "plans";
        }
        if (hasCompanyRole("CompanyEmployee")) {
            return "appointments";
        }
        return "overview";
    }

    function getAllowedTabs(isAuthenticated, hasTenantAccess) {
        const tabs = new Set();
        tabs.add("overview");
        tabs.add("auth");
        tabs.add("logs");

        if (!isAuthenticated) {
            return tabs;
        }

        if (hasSystemRole("SystemAdmin")) {
            tabs.add("platform");
            tabs.add("support");
            tabs.add("billing");
        } else {
            if (hasSystemRole("SystemSupport")) {
                tabs.add("support");
            }
            if (hasSystemRole("SystemBilling")) {
                tabs.add("billing");
            }
        }

        if (!hasTenantAccess) {
            return tabs;
        }

        tabs.add("patients");

        if (hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee")) {
            tabs.add("resources");
            tabs.add("appointments");
        }

        if (hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager")) {
            tabs.add("plans");
        }

        if (hasCompanyRole("CompanyOwner", "CompanyAdmin")) {
            tabs.add("team");
        }

        if (hasCompanyRole("CompanyOwner")) {
            tabs.add("settings");
        }

        return tabs;
    }

    function applyTabVisibility(allowedTabs) {
        const targets = document.querySelectorAll("[data-tab-target]");
        targets.forEach((element) => {
            const target = element.getAttribute("data-tab-target");
            if (!target) {
                return;
            }

            const isAllowed = allowedTabs.has(target);
            element.hidden = !isAllowed;
            if (element instanceof HTMLElement) {
                element.setAttribute("aria-hidden", isAllowed ? "false" : "true");
            }
        });
    }

    function normalizeRoute(path) {
        if (!path || path === "/") {
            return "/";
        }

        const trimmed = path.trim().toLowerCase();
        return trimmed.endsWith("/") ? trimmed.slice(0, -1) : trimmed;
    }

    function syncPublicEntryState() {
        document.title = baseTitle;
        const currentPath = normalizeRoute(window.location.pathname);
        if (currentPath !== "/") {
            history.replaceState(null, "", "/");
        }
    }

    function readErrorMessage(payload) {
        if (!payload) return null;
        if (typeof payload === "string") return payload;
        if (payload.detail) return payload.detail;
        if (payload.title) return payload.title;
        if (payload.message) return payload.message;

        if (payload.errors && typeof payload.errors === "object") {
            const firstKey = Object.keys(payload.errors)[0];
            if (firstKey && Array.isArray(payload.errors[firstKey]) && payload.errors[firstKey].length > 0) {
                return payload.errors[firstKey][0];
            }
        }

        if (Array.isArray(payload.messages) && payload.messages.length > 0) {
            return payload.messages[0];
        }

        return null;
    }

    function reportError(error) {
        const message = error instanceof Error ? error.message : "Unexpected error.";
        showToast(message, "error", "Request failed");
    }

    function showToast(message, tone = "info", title) {
        if (!elements.toastRegion) {
            return;
        }

        const toast = document.createElement("article");
        toast.className = `toast toast--${tone}`;
        toast.setAttribute("role", "status");

        const toastTitle = document.createElement("span");
        toastTitle.className = "toast__title";
        toastTitle.textContent = title || defaultToastTitle(tone);

        const toastMessage = document.createElement("div");
        toastMessage.className = "toast__message";
        toastMessage.textContent = message;

        toast.appendChild(toastTitle);
        toast.appendChild(toastMessage);
        elements.toastRegion.prepend(toast);

        window.setTimeout(() => {
            toast.remove();
        }, 4200);
    }

    function defaultToastTitle(tone) {
        if (tone === "success") return "Success";
        if (tone === "error") return "Error";
        return "Info";
    }

    function log(tag, requestPayload, responsePayload, isError = false) {
        if (!elements.logBox) {
            return;
        }

        const block = [
            `[${new Date().toISOString()}] ${tag}${isError ? " [ERROR]" : ""}`,
            requestPayload !== null && requestPayload !== undefined
                ? `request: ${safeStringify(requestPayload)}`
                : "request: -",
            `response: ${safeStringify(responsePayload)}`
        ].join("\n");

        logEntries.unshift(block);
        if (logEntries.length > maxLogEntries) {
            logEntries.length = maxLogEntries;
        }

        elements.logBox.textContent = logEntries.join("\n\n");
    }

    function safeStringify(value) {
        if (typeof value === "string") {
            return value;
        }
        try {
            return JSON.stringify(value, null, 2);
        } catch {
            return String(value);
        }
    }

    function extractSystemRolesFromJwt(jwt) {
        const payload = decodeJwtPayload(jwt);
        if (!payload) {
            return [];
        }

        const rolesClaim = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? payload.role;
        const roles = Array.isArray(rolesClaim) ? rolesClaim : (rolesClaim ? [rolesClaim] : []);
        const allowed = new Set(["SystemAdmin", "SystemSupport", "SystemBilling"]);

        return roles
            .filter((role) => typeof role === "string" && allowed.has(role))
            .map((role) => role.trim());
    }

    function getCurrentUserIdFromJwt() {
        const payload = decodeJwtPayload(state.jwt);
        const claim = payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] ?? payload?.sub;
        return typeof claim === "string" ? claim : "";
    }

    function getCurrentUserEmailFromJwt() {
        const payload = decodeJwtPayload(state.jwt);
        const claim = payload?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] ?? payload?.email;
        return typeof claim === "string" ? claim : "";
    }

    function decodeJwtPayload(jwt) {
        if (!jwt || typeof jwt !== "string") {
            return null;
        }

        try {
            const parts = jwt.split(".");
            if (parts.length < 2) {
                return null;
            }

            const payloadBase64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
            const padded = payloadBase64 + "=".repeat((4 - (payloadBase64.length % 4)) % 4);
            return JSON.parse(window.atob(padded));
        } catch {
            return null;
        }
    }

    function isJwtExpired(jwt) {
        const payload = decodeJwtPayload(jwt);
        if (!payload || typeof payload.exp !== "number") {
            return false;
        }

        const nowInSeconds = Math.floor(Date.now() / 1000);
        return payload.exp <= nowInSeconds;
    }

    function loadSession() {
        try {
            const raw = localStorage.getItem(sessionStorageKey);
            if (!raw) {
                return {
                    jwt: "",
                    refreshToken: "",
                    expiresInSeconds: 0,
                    companySlug: "",
                    companyRole: "",
                    systemRoles: []
                };
            }

            const parsed = JSON.parse(raw);
            const jwt = parsed.jwt || "";
            if (jwt && isJwtExpired(jwt)) {
                localStorage.removeItem(sessionStorageKey);
                return {
                    jwt: "",
                    refreshToken: "",
                    expiresInSeconds: 0,
                    companySlug: "",
                    companyRole: "",
                    systemRoles: []
                };
            }

            return {
                jwt,
                refreshToken: parsed.refreshToken || "",
                expiresInSeconds: Number(parsed.expiresInSeconds || 0),
                companySlug: parsed.companySlug || "",
                companyRole: parsed.companyRole || "",
                systemRoles: Array.isArray(parsed.systemRoles) ? parsed.systemRoles : extractSystemRolesFromJwt(jwt)
            };
        } catch {
            return {
                jwt: "",
                refreshToken: "",
                expiresInSeconds: 0,
                companySlug: "",
                companyRole: "",
                systemRoles: []
            };
        }
    }

    function saveSession() {
        localStorage.setItem(sessionStorageKey, JSON.stringify({
            jwt: state.jwt,
            refreshToken: state.refreshToken,
            expiresInSeconds: state.expiresInSeconds,
            companySlug: state.companySlug,
            companyRole: state.companyRole,
            systemRoles: state.systemRoles
        }));
    }

	    function optional(value) {
	        const trimmed = (value || "").trim();
	        return trimmed.length === 0 ? null : trimmed;
	    }

	    Object.assign(app, {
	        state,
	        elements,
	        activateScreen,
	        apiRequest,
	        applyJwtResponse,
	        applyTabVisibility,
	        canManagePatientsUi,
	        clearSession,
	        getCurrentUserEmailFromJwt,
	        getCurrentUserIdFromJwt,
	        getAllowedTabs,
	        getRequestedScreen,
	        hasAnySystemRole,
	        hasCompanyRole,
	        hasSystemRole,
	        log,
	        optional,
	        permanentToothNumbers,
	        renderPlanItemsSkeleton,
	        reportError,
	        requireJwt,
	        requireSystemRole,
	        requireTenant,
	        resolvePatientName,
	        resolvePatient,
	        resolveLandingScreen,
	        saveSession,
	        showToast,
	        syncPublicEntryState,
	        toothConditionOptions
	    });

	    moduleRegistrations.forEach((registerModule) => {
	        if (typeof registerModule === "function") {
	            registerModule(app);
	        }
	    });

	    const {
	        closePatientProfile,
	        closeDentistProfile,
	        bindAsyncClick,
	        bindAsyncSubmit,
	        bindSyncChange,
	        bindSyncClick,
	        bindSyncInput,
	        clearElement,
	        createCell,
	        createContentCell,
	        createEntityLinkButton,
	        createPatientActionButton,
	        formatConditionLabel,
	        formatDateTime,
	        formatDateTimeLocalInput,
	        formatMoney,
	        formatTime,
	        focusElementIfPossible,
	        onDeleteDialogClose,
	        onForgotPasswordSubmit,
	        onGatewayLoginSubmit,
	        onLoginSubmit,
	        onLogoutClick,
	        onOnboardingSubmit,
	        onResetPasswordSubmit,
	        onSwitchSubmit,
	        onDentistCreateSubmit,
	        onDentistProfileDelete,
	        onDentistProfileSubmit,
	        onPatientCreateSubmit,
	        onPatientProfileSubmit,
	        onTreatmentRoomCreateSubmit,
	        onAppointmentCreateSubmit,
	        onAppointmentClinicalSubmit,
	        onPlanDecisionSubmit,
	        onCostEstimateSubmit,
	        onLegalEstimateSubmit,
	        onFinancePatientChange,
	        onFinancePlanSubmit,
	        onFinancePolicySubmit,
	        onFinanceInvoiceGenerateSubmit,
	        onFinancePaymentSubmit,
	        onFinancePaymentPlanSubmit,
	        openDeleteDialog,
	        openDentistProfile,
	        openPatientProfile,
	        onFeatureFlagSubmit,
	        onCompanyActivationSubmit,
	        onSupportTicketSubmit,
	        onBillingSubscriptionSubmit,
	        onBillingInvoiceStatusSubmit,
	        onSubscriptionSubmit,
	        onCompanyUserUpsertSubmit,
	        onCompanyRoleSwitchSubmit,
	        onCompanySettingsSubmit,
	        refreshAppointments,
	        refreshAllViewsForCurrentSession,
	        refreshClinicalViews,
	        refreshDentistProfile,
	        refreshPatientProfile,
	        refreshPatients,
	        refreshResources,
	        refreshTreatmentRooms,
	        refreshTreatmentTypes,
	        refreshTreatmentPlans,
	        refreshCostEstimates,
	        refreshOpenPlanItems,
	        refreshFinanceWorkspace,
	        refreshPlatformData,
	        refreshSupportData,
	        refreshBillingData,
	        refreshTenantAdminViews,
	        refreshCompanyUsers,
	        refreshCompanySettings,
	        refreshTenantSubscription,
	        renderAppointments,
	        renderDentistProfile,
	        renderDentists,
	        renderDentistProfileAppointments,
	        getDentistAppointments,
	        isUpcomingAppointment,
	        renderAppointmentSelectOptions,
	        renderPatientProfile,
	        renderPatients,
	        renderTreatmentRooms,
	        renderOpenPlanItems,
	        syncFinancePatientSelect,
	        renderFinanceWorkspace,
	        renderFinancePlanReview,
	        renderFinanceInvoiceDetail,
	        resetFinancePolicyForm,
	        addFinanceInstallmentRow,
	        resetFinancePaymentPlanForm,
	        renderAppointmentClinicalSelectOptions,
	        renderLegalEstimateOutput,
	        resetAppointmentClinicalForm,
	        resetTreatmentRoomForm,
	        addAppointmentClinicalEntryRow,
	        renderCompanyUsers,
	        renderCompanySettings,
	        renderSubscription,
	        renderPlatformAnalytics,
	        renderFeatureFlags,
	        renderSupportCompanies,
	        renderSupportTickets,
	        renderBillingSubscriptions,
	        renderBillingInvoices,
	        renderSession,
	        resolveDentist,
	        resolveDentistName,
	        resolveRoomLabel,
	        resolveTreatmentRoom,
	        setBadgeVariant,
	        setSelectOptions,
	        setSyncStatus,
	        setText,
	        toUtcIso,
	        toggleFormControls,
	        withBusy
	    } = app;

	    bindEvents();
	    initializeTheme();
	    initializeTabs();
	    renderSession();
	    renderPatients([]);
	    renderPatientProfile(null);
	    renderDentists([]);
	    renderDentistProfile(null);
	    renderTreatmentRooms([]);
	    renderAppointments([]);
	    renderOpenPlanItems([]);
	    renderCompanyUsers([]);
	    renderCompanySettings(null);
	    renderSubscription(null);
	    renderFeatureFlags([]);
	    renderSupportCompanies([]);
	    renderSupportTickets([]);
	    renderBillingSubscriptions([]);
	    renderBillingInvoices([]);
	    resetAppointmentClinicalForm();
	    resetFinancePolicyForm();
	    resetFinancePaymentPlanForm();
	    renderFinanceWorkspace(null);
	    renderFinanceInvoiceDetail(null);
	    setSyncStatus("Idle", "neutral");
	    log("SYSTEM/READY", null, { message: "UI initialized." });

	    if (state.jwt) {
	        (async () => {
	            try {
	                await refreshAllViewsForCurrentSession({ silentToast: true, silentSyncStatus: true });
	            } catch (error) {
	                setSyncStatus("Sync failed", "danger");
	                reportError(error);
	            }
	        })();
	    }
	})();
