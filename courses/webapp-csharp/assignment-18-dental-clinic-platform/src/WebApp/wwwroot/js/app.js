(() => {
    const sessionStorageKey = "dental-saas-ui-session";
    const themeStorageKey = "dental-saas-ui-theme";
    const maxLogEntries = 60;

    const state = {
        ...loadSession(),
        patients: [],
        dentists: [],
        treatmentRooms: [],
        appointments: [],
        openPlanItems: [],
        companyUsers: [],
        companySettings: null,
        subscription: null,
        costEstimates: [],
        systemAnalytics: null,
        featureFlags: [],
        supportCompanies: [],
        supportTickets: [],
        billingSubscriptions: [],
        billingInvoices: []
    };

    let pendingDelete = {
        id: "",
        label: ""
    };

    const logEntries = [];
    const elements = {
        authPill: document.getElementById("auth-pill"),
        companyPill: document.getElementById("company-pill"),
        appRoot: document.getElementById("app-root"),
        gatewayScreen: document.getElementById("gateway-screen"),
        gatewayLoginForm: document.getElementById("gateway-login-form"),
        gatewayOpenAuthButton: document.getElementById("gateway-open-auth"),
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
        themeToggle: document.getElementById("theme-toggle"),
        logoutButton: document.getElementById("logout-btn"),
        refreshPatientsButton: document.getElementById("refresh-patients-btn"),
        refreshResourcesButton: document.getElementById("refresh-resources-btn"),
        refreshAppointmentsButton: document.getElementById("refresh-appointments-btn"),
        refreshPlanItemsButton: document.getElementById("refresh-plan-items-btn"),
        refreshPlatformButton: document.getElementById("refresh-platform-btn"),
        refreshSupportButton: document.getElementById("refresh-support-btn"),
        refreshBillingButton: document.getElementById("refresh-billing-btn"),
        refreshCompanyUsersButton: document.getElementById("refresh-company-users-btn"),
        refreshCompanySettingsButton: document.getElementById("refresh-company-settings-btn"),
        refreshSubscriptionButton: document.getElementById("refresh-subscription-btn"),
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
        companyUserForm: document.getElementById("company-user-form"),
        companyUsersBody: document.getElementById("company-users-body"),
        companySettingsForm: document.getElementById("company-settings-form"),
        loadingBar: document.getElementById("global-loading"),
        toastRegion: document.getElementById("toast-region"),
        patientDeleteDialog: document.getElementById("patient-delete-dialog"),
        patientDeleteText: document.getElementById("patient-delete-text"),
        confirmDeleteButton: document.getElementById("confirm-delete-btn")
    };

    bindEvents();
    initializeTheme();
    initializeTabs();
    renderSession();
    renderPatients([]);
    renderDentists([]);
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

    function bindEvents() {
        bindAsyncSubmit(elements.gatewayLoginForm, onGatewayLoginSubmit);
        bindAsyncSubmit(elements.onboardingForm, onOnboardingSubmit);
        bindAsyncSubmit(elements.loginForm, onLoginSubmit);
        bindAsyncSubmit(elements.switchForm, onSwitchSubmit);
        bindAsyncSubmit(elements.forgotPasswordForm, onForgotPasswordSubmit);
        bindAsyncSubmit(elements.resetPasswordForm, onResetPasswordSubmit);
        bindAsyncSubmit(elements.patientForm, onPatientCreateSubmit);
        bindAsyncSubmit(elements.dentistForm, onDentistCreateSubmit);
        bindAsyncSubmit(elements.treatmentRoomForm, onTreatmentRoomCreateSubmit);
        bindAsyncSubmit(elements.appointmentForm, onAppointmentCreateSubmit);
        bindAsyncSubmit(elements.planDecisionForm, onPlanDecisionSubmit);
        bindAsyncSubmit(elements.costEstimateForm, onCostEstimateSubmit);
        bindAsyncSubmit(elements.legalEstimateForm, onLegalEstimateSubmit);
        bindAsyncSubmit(elements.featureFlagForm, onFeatureFlagSubmit);
        bindAsyncSubmit(elements.companyActivationForm, onCompanyActivationSubmit);
        bindAsyncSubmit(elements.supportTicketForm, onSupportTicketSubmit);
        bindAsyncSubmit(elements.billingSubscriptionForm, onBillingSubscriptionSubmit);
        bindAsyncSubmit(elements.billingInvoiceStatusForm, onBillingInvoiceStatusSubmit);
        bindAsyncSubmit(elements.subscriptionForm, onSubscriptionSubmit);
        bindAsyncSubmit(elements.companyUserForm, onCompanyUserUpsertSubmit);
        bindAsyncSubmit(elements.companySettingsForm, onCompanySettingsSubmit);

        bindAsyncClick(elements.refreshPatientsButton, async () => {
            await refreshPatients({ trigger: elements.refreshPatientsButton });
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

        bindAsyncClick(elements.logoutButton, onLogoutClick);
        bindSyncClick(elements.gatewayOpenAuthButton, onGatewayOpenAuthClick);
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

        window.addEventListener("hashchange", () => {
            activateScreen(getRequestedScreen(), { updateHash: false });
        });
    }

    function bindAsyncSubmit(form, handler) {
        if (!form) return;
        form.addEventListener("submit", async (event) => {
            try {
                await handler(event);
            } catch (error) {
                reportError(error);
            }
        });
    }

    function bindAsyncClick(element, handler) {
        if (!element) return;
        element.addEventListener("click", async (event) => {
            try {
                await handler(event);
            } catch (error) {
                reportError(error);
            }
        });
    }

    function bindSyncClick(element, handler) {
        if (!element) return;
        element.addEventListener("click", handler);
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
        activateScreen(getRequestedScreen(), { updateHash: false });
    }

    function getRequestedScreen() {
        const raw = (window.location.hash || "").replace("#", "").trim();
        const allowed = new Set(["overview", "platform", "support", "billing", "auth", "patients", "resources", "appointments", "plans", "team", "settings", "logs"]);
        return allowed.has(raw) ? raw : "overview";
    }

    function activateScreen(screenId, options = {}) {
        const updateHash = options.updateHash !== false;
        const panels = document.querySelectorAll("[data-screen]");
        const tabs = document.querySelectorAll("[data-tab-target]");

        panels.forEach((panel) => {
            const isMatch = panel.getAttribute("data-screen") === screenId;
            panel.hidden = !isMatch;
        });

        tabs.forEach((tab) => {
            const isMatch = tab.getAttribute("data-tab-target") === screenId;
            if (tab.classList.contains("tab")) {
                tab.classList.toggle("is-active", isMatch);
                tab.setAttribute("aria-selected", isMatch ? "true" : "false");
            }
        });

        if (updateHash) {
            history.replaceState(null, "", `#${screenId}`);
        }
    }

    async function onOnboardingSubmit(event) {
        event.preventDefault();
        const form = event.currentTarget;
        const payload = {
            companyName: form.companyName.value.trim(),
            companySlug: form.companySlug.value.trim().toLowerCase(),
            ownerEmail: form.ownerEmail.value.trim(),
            ownerPassword: form.ownerPassword.value,
            countryCode: form.countryCode.value.trim().toUpperCase()
        };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/system/onboarding/registercompany", {
                method: "POST",
                body: payload,
                auth: false,
                tag: "ONBOARDING"
            });

            log("ONBOARDING/SUCCESS", payload, data);
            showToast("Company onboarding completed.", "success");
            form.reset();
            form.countryCode.value = "EE";
            activateScreen("auth");
        });
    }

    async function onGatewayLoginSubmit(event) {
        event.preventDefault();
        const form = event.currentTarget;
        await performLogin(form.email.value.trim(), form.password.value, form, "GATEWAY");
    }

    function onGatewayOpenAuthClick() {
        if (elements.gatewayScreen) {
            elements.gatewayScreen.hidden = true;
        }
        if (elements.appRoot) {
            elements.appRoot.hidden = false;
        }
        activateScreen("auth");
    }

    async function onLoginSubmit(event) {
        event.preventDefault();
        const form = event.currentTarget;
        await performLogin(form.email.value.trim(), form.password.value, form, "ACCESS");
    }

    async function performLogin(email, password, form, sourceTag) {
        const payload = { email, password };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/account/login", {
                method: "POST",
                body: payload,
                auth: false,
                tag: `LOGIN/${sourceTag}`
            });

            applyJwtResponse(data);
            renderSession();
            log("LOGIN/SUCCESS", { email: payload.email, source: sourceTag }, data);
            showToast("Signed in successfully.", "success");
            await refreshAllViewsForCurrentSession({ silentToast: true, silentSyncStatus: true });
            activateScreen(resolveLandingScreen());
        });
    }

    async function onSwitchSubmit(event) {
        event.preventDefault();
        requireJwt();

        const form = event.currentTarget;
        const payload = {
            companySlug: form.companySlug.value.trim().toLowerCase()
        };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/account/switchcompany", {
                method: "POST",
                body: payload,
                auth: true,
                tag: "SWITCH"
            });

            applyJwtResponse(data);
            renderSession();
            log("SWITCH/SUCCESS", payload, data);
            showToast(`Tenant switched to ${state.companySlug}.`, "success");
            await refreshAllViewsForCurrentSession({ silentToast: true, silentSyncStatus: true });
            activateScreen(resolveLandingScreen());
        });
    }

    async function onForgotPasswordSubmit(event) {
        event.preventDefault();
        const form = event.currentTarget;
        const payload = {
            email: form.email.value.trim()
        };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/account/forgotpassword", {
                method: "POST",
                body: payload,
                auth: false,
                tag: "ACCOUNT/FORGOT-PASSWORD"
            });

            if (data.resetToken && elements.resetPasswordForm) {
                elements.resetPasswordForm.email.value = payload.email;
                elements.resetPasswordForm.resetToken.value = data.resetToken;
            }

            log("ACCOUNT/FORGOT-PASSWORD/SUCCESS", payload, {
                tokenReturned: Boolean(data.resetToken)
            });
            showToast(data.resetToken
                ? "Reset token generated (development mode)."
                : "If account exists, reset instructions are available.",
            "info");
        });
    }

    async function onResetPasswordSubmit(event) {
        event.preventDefault();
        const form = event.currentTarget;
        const payload = {
            email: form.email.value.trim(),
            resetToken: form.resetToken.value.trim(),
            newPassword: form.newPassword.value
        };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/account/resetpassword", {
                method: "POST",
                body: payload,
                auth: false,
                tag: "ACCOUNT/RESET-PASSWORD"
            });

            log("ACCOUNT/RESET-PASSWORD/SUCCESS", { email: payload.email }, data);
            showToast("Password reset completed.", "success");
            form.reset();
        });
    }

    async function onPatientCreateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            firstName: form.firstName.value.trim(),
            lastName: form.lastName.value.trim(),
            dateOfBirth: form.dateOfBirth.value || null,
            personalCode: optional(form.personalCode.value),
            email: optional(form.email.value),
            phone: optional(form.phone.value)
        };

        await withBusy(form, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/patients`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "PATIENT/CREATE"
            });

            showToast("Patient created.", "success");
            form.reset();
            await refreshPatients({ silentToast: true, silentSyncStatus: true });
        });
    }

    async function onDentistCreateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            displayName: form.displayName.value.trim(),
            licenseNumber: form.licenseNumber.value.trim(),
            specialty: optional(form.specialty.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/dentists`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "DENTIST/CREATE"
            });

            log("DENTIST/CREATE/SUCCESS", payload, data);
            showToast("Dentist added.", "success");
            form.reset();
            await refreshDentists({ silentToast: true });
        });
    }

    async function onTreatmentRoomCreateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            name: form.name.value.trim(),
            code: form.code.value.trim().toUpperCase(),
            isActiveRoom: Boolean(form.isActiveRoom.checked)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/treatmentrooms`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "TREATMENT-ROOM/CREATE"
            });

            log("TREATMENT-ROOM/CREATE/SUCCESS", payload, data);
            showToast("Treatment room added.", "success");
            form.reset();
            form.isActiveRoom.checked = true;
            await refreshTreatmentRooms({ silentToast: true });
        });
    }

    async function onAppointmentCreateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            patientId: form.patientId.value,
            dentistId: form.dentistId.value,
            treatmentRoomId: form.treatmentRoomId.value,
            startAtUtc: toUtcIso(form.startAtLocal.value),
            endAtUtc: toUtcIso(form.endAtLocal.value),
            notes: optional(form.notes.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/appointments`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "APPOINTMENT/CREATE"
            });

            log("APPOINTMENT/CREATE/SUCCESS", payload, data);
            showToast("Appointment created.", "success");
            form.reset();
            await refreshAppointments({ silentToast: true });
        });
    }

    async function onPlanDecisionSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const selection = form.planItemSelection.value;
        const [planId, planItemId] = selection.split("|");
        if (!planId || !planItemId) {
            throw new Error("Select a pending plan item first.");
        }

        const payload = {
            planId,
            planItemId,
            decision: form.decision.value,
            notes: optional(form.notes.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/treatmentplans/recorditemdecision`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "TREATMENT-PLAN/DECISION"
            });

            log("TREATMENT-PLAN/DECISION/SUCCESS", payload, data);
            showToast("Plan item decision saved.", "success");
            form.notes.value = "";
            await refreshOpenPlanItems({ silentToast: true });
        });
    }

    async function onCostEstimateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            patientId: form.patientId.value,
            treatmentPlanId: form.treatmentPlanId.value.trim(),
            insurancePlanId: optional(form.insurancePlanId.value),
            estimateNumber: form.estimateNumber.value.trim(),
            formatCode: form.formatCode.value.trim().toUpperCase(),
            totalEstimatedAmount: Number(form.totalEstimatedAmount.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/costestimates`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "COST-ESTIMATE/CREATE"
            });

            log("COST-ESTIMATE/CREATE/SUCCESS", payload, data);
            showToast("Cost estimate created.", "success");
            if (elements.legalEstimateForm) {
                elements.legalEstimateForm.costEstimateId.value = data.id || "";
            }
            await refreshCostEstimates({ silentToast: true });
        });
    }

    async function onLegalEstimateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const estimateId = form.costEstimateId.value.trim();
        if (!estimateId) {
            throw new Error("Cost estimate ID is required.");
        }

        const countryCode = optional(form.countryCode.value)?.toUpperCase();
        const query = countryCode ? `?countryCode=${encodeURIComponent(countryCode)}` : "";

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/costestimates/${estimateId}/legal${query}`, {
                method: "GET",
                auth: true,
                tag: "COST-ESTIMATE/LEGAL"
            });

            log("COST-ESTIMATE/LEGAL/SUCCESS", { estimateId, countryCode }, data);
            renderLegalEstimateOutput(data.generatedText || "No legal output generated.");
            showToast(`Legal preview generated (${data.documentType || "Document"}).`, "success");
        });
    }

    async function onFeatureFlagSubmit(event) {
        event.preventDefault();
        requireSystemRole("SystemAdmin");

        const form = event.currentTarget;
        const payload = {
            key: form.key.value.trim(),
            enabled: Boolean(form.enabled.checked)
        };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/system/platform/featureflags", {
                method: "PUT",
                body: payload,
                auth: true,
                tag: "PLATFORM/FEATURE-FLAG"
            });

            renderFeatureFlags(Array.isArray(data) ? data : []);
            showToast("Feature flag updated.", "success");
        });
    }

    async function onCompanyActivationSubmit(event) {
        event.preventDefault();
        requireSystemRole("SystemAdmin");

        const form = event.currentTarget;
        const companyId = form.companyId.value.trim();
        if (!companyId) {
            throw new Error("Company ID is required.");
        }

        await withBusy(form, async () => {
            await apiRequest(`/api/v1/system/platform/companies/${companyId}/activation`, {
                method: "PUT",
                body: {
                    isActive: Boolean(form.isActive.checked)
                },
                auth: true,
                tag: "PLATFORM/COMPANY-ACTIVATION"
            });

            showToast("Company activation updated.", "success");
            await refreshPlatformData({ silentToast: true });
            await refreshSupportData({ silentToast: true });
            await refreshBillingData({ silentToast: true });
        });
    }

    async function onSupportTicketSubmit(event) {
        event.preventDefault();
        requireSystemRole("SystemSupport", "SystemAdmin");

        const form = event.currentTarget;
        const payload = {
            companySlug: form.companySlug.value.trim().toLowerCase(),
            subject: form.subject.value.trim(),
            details: form.details.value.trim()
        };

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/system/support/tickets", {
                method: "POST",
                body: payload,
                auth: true,
                tag: "SUPPORT/TICKET-CREATE"
            });

            log("SUPPORT/TICKET-CREATE/SUCCESS", payload, data);
            showToast("Support ticket created.", "success");
            form.reset();
            await refreshSupportData({ silentToast: true });
        });
    }

    async function onBillingSubscriptionSubmit(event) {
        event.preventDefault();
        requireSystemRole("SystemBilling", "SystemAdmin");

        const form = event.currentTarget;
        const subscriptionId = form.subscriptionId.value.trim();
        if (!subscriptionId) {
            throw new Error("Subscription ID is required.");
        }

        const payload = {
            tier: form.tier.value,
            status: form.status.value,
            userLimit: Number(form.userLimit.value),
            entityLimit: Number(form.entityLimit.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/system/billing/subscriptions/${subscriptionId}`, {
                method: "PUT",
                body: payload,
                auth: true,
                tag: "BILLING/SUBSCRIPTION-UPDATE"
            });

            log("BILLING/SUBSCRIPTION-UPDATE/SUCCESS", payload, data);
            showToast("Subscription updated.", "success");
            await refreshBillingData({ silentToast: true });
        });
    }

    async function onBillingInvoiceStatusSubmit(event) {
        event.preventDefault();
        requireSystemRole("SystemBilling", "SystemAdmin");

        const form = event.currentTarget;
        const invoiceId = form.invoiceId.value.trim();
        if (!invoiceId) {
            throw new Error("Invoice ID is required.");
        }

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/system/billing/invoices/${invoiceId}/status`, {
                method: "PUT",
                body: { status: form.status.value },
                auth: true,
                tag: "BILLING/INVOICE-STATUS"
            });

            log("BILLING/INVOICE-STATUS/SUCCESS", { invoiceId, status: form.status.value }, data);
            showToast("Invoice status updated.", "success");
            await refreshBillingData({ silentToast: true });
        });
    }

    async function onSubscriptionSubmit(event) {
        event.preventDefault();
        requireTenant();
        if (!hasCompanyRole("CompanyOwner")) {
            throw new Error("Only CompanyOwner can change subscription tier.");
        }

        const form = event.currentTarget;
        const payload = {
            tier: form.tier.value
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/subscription`, {
                method: "PUT",
                body: payload,
                auth: true,
                tag: "TENANT/SUBSCRIPTION-UPDATE"
            });

            renderSubscription(data);
            showToast("Subscription tier updated.", "success");
        });
    }

    async function onCompanyUserUpsertSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            email: form.email.value.trim(),
            roleName: form.roleName.value,
            isActive: Boolean(form.isActive.checked),
            temporaryPassword: optional(form.temporaryPassword.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/companyusers`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "COMPANY-USERS/UPSERT"
            });

            log("COMPANY-USERS/UPSERT/SUCCESS", { email: payload.email, roleName: payload.roleName }, data);
            showToast("Membership saved.", "success");
            form.temporaryPassword.value = "";
            await refreshCompanyUsers({ silentToast: true });
        });
    }

    async function onCompanySettingsSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const payload = {
            countryCode: form.countryCode.value.trim().toUpperCase(),
            currencyCode: form.currencyCode.value.trim().toUpperCase(),
            timezone: form.timezone.value.trim(),
            defaultXrayIntervalMonths: Number(form.defaultXrayIntervalMonths.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/companysettings`, {
                method: "PUT",
                body: payload,
                auth: true,
                tag: "COMPANY-SETTINGS/UPDATE"
            });

            log("COMPANY-SETTINGS/UPDATE/SUCCESS", payload, data);
            showToast("Company settings saved.", "success");
            renderCompanySettings(data);
        });
    }

    async function onLogoutClick() {
        await withBusy(elements.logoutButton, async () => {
            if (state.jwt && state.refreshToken) {
                try {
                    await apiRequest("/api/v1/account/logout", {
                        method: "POST",
                        body: {
                            jwt: state.jwt,
                            refreshToken: state.refreshToken
                        },
                        auth: true,
                        tag: "LOGOUT"
                    });
                } catch {
                    // Server-side token may already be invalid; local session still needs to be cleared.
                }
            }

            clearSession();
            renderSession();
            renderPatients([]);
            renderDentists([]);
            renderTreatmentRooms([]);
            renderAppointments([]);
            renderOpenPlanItems([]);
            renderCompanyUsers([]);
            renderCompanySettings(null);
            renderSubscription(null);
            renderPlatformAnalytics(null);
            renderFeatureFlags([]);
            renderSupportCompanies([]);
            renderSupportTickets([]);
            renderBillingSubscriptions([]);
            renderBillingInvoices([]);
            renderLegalEstimateOutput("No legal preview generated yet.");
            setSyncStatus("Idle", "neutral");
            log("LOGOUT/SUCCESS", null, { message: "Session cleared." });
            showToast("Logged out.", "info");
            activateScreen("overview");
        });
    }

    async function refreshAllViewsForCurrentSession(options = {}) {
        if (!state.jwt) {
            return;
        }

        const silentToast = options.silentToast === true;
        const silentSyncStatus = options.silentSyncStatus === true;

        if (hasSystemRole("SystemAdmin")) {
            await refreshPlatformData({ silentToast: true, trigger: options.trigger });
            await refreshSupportData({ silentToast: true, trigger: options.trigger });
            await refreshBillingData({ silentToast: true, trigger: options.trigger });
        } else {
            if (hasSystemRole("SystemSupport")) {
                await refreshSupportData({ silentToast: true, trigger: options.trigger });
            }
            if (hasSystemRole("SystemBilling")) {
                await refreshBillingData({ silentToast: true, trigger: options.trigger });
            }
        }

        if (state.companySlug) {
            await refreshPatients({ silentToast: true, silentSyncStatus: true, trigger: options.trigger });
            await refreshClinicalViews({ silentToast: true, trigger: options.trigger });
            await refreshTenantAdminViews({ silentToast: true, trigger: options.trigger });
            await refreshTenantSubscription({ silentToast: true, silentErrors: true, trigger: options.trigger });
        } else if (!hasAnySystemRole()) {
            setSyncStatus("No active tenant", "warning");
        }

        if (!silentSyncStatus && state.companySlug) {
            setSyncStatus(`Synced ${formatTime(new Date())}`, "success");
        }

        if (!silentToast) {
            showToast("Workspace refreshed.", "info");
        }
    }

    async function refreshPatients(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshPatientsButton,
            silentToast = false,
            silentSyncStatus = false
        } = options;

        renderPatientsSkeleton();
        setSyncStatus("Syncing...", "warning");

        try {
            const patients = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/patients`, {
                    method: "GET",
                    auth: true,
                    tag: "PATIENT/LIST"
                });

                return Array.isArray(data) ? data : [];
            });

            state.patients = patients;
            renderPatients(patients);
            renderSession();
            log("PATIENT/LIST/SUCCESS", null, patients);

            if (!silentSyncStatus) {
                setSyncStatus(`Synced ${formatTime(new Date())}`, "success");
            } else {
                setSyncStatus("Synced", "success");
            }

            if (!silentToast) {
                showToast(`${patients.length} patient${patients.length === 1 ? "" : "s"} loaded.`, "info");
            }
        } catch (error) {
            setSyncStatus("Sync failed", "danger");
            throw error;
        }
    }

    async function refreshClinicalViews(options = {}) {
        if (!state.companySlug) {
            return;
        }

        const canAccessResources = hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee");
        const canAccessPlanDecisions = hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager");

        if (!canAccessResources) {
            state.dentists = [];
            state.treatmentRooms = [];
            state.appointments = [];
            state.openPlanItems = [];
            renderDentists([]);
            renderTreatmentRooms([]);
            renderAppointments([]);
            renderOpenPlanItems([]);
            return;
        }

        await refreshResources({ silentToast: true, silentErrors: true, trigger: options.trigger });
        await refreshAppointments({ silentToast: true, silentErrors: true, trigger: options.trigger });

        if (canAccessPlanDecisions) {
            await refreshOpenPlanItems({ silentToast: true, silentErrors: true, trigger: options.trigger });
            await refreshCostEstimates({ silentToast: true, silentErrors: true, trigger: options.trigger });
        } else {
            state.openPlanItems = [];
            renderOpenPlanItems([]);
            state.costEstimates = [];
        }
    }

    async function refreshPlatformData(options = {}) {
        const {
            trigger = elements.refreshPlatformButton,
            silentToast = false
        } = options;

        if (!hasSystemRole("SystemAdmin")) {
            renderPlatformAnalytics(null);
            renderFeatureFlags([]);
            return;
        }

        await refreshPlatformAnalytics({ trigger, silentToast: true });
        await refreshFeatureFlags({ trigger, silentToast: true });

        if (!silentToast) {
            showToast("Platform data refreshed.", "info");
        }
    }

    async function refreshPlatformAnalytics(options = {}) {
        const {
            trigger = elements.refreshPlatformButton,
            silentToast = false
        } = options;

        const analytics = await withBusy(trigger, async () => {
            return await apiRequest("/api/v1/system/platform/analytics", {
                method: "GET",
                auth: true,
                tag: "PLATFORM/ANALYTICS"
            });
        });

        renderPlatformAnalytics(analytics);
        if (!silentToast) {
            showToast("Platform analytics loaded.", "info");
        }
    }

    async function refreshFeatureFlags(options = {}) {
        const {
            trigger = elements.refreshPlatformButton,
            silentToast = false
        } = options;

        const flags = await withBusy(trigger, async () => {
            const data = await apiRequest("/api/v1/system/platform/featureflags", {
                method: "GET",
                auth: true,
                tag: "PLATFORM/FEATURE-FLAGS"
            });
            return Array.isArray(data) ? data : [];
        });

        renderFeatureFlags(flags);
        if (!silentToast) {
            showToast("Feature flags loaded.", "info");
        }
    }

    async function refreshSupportData(options = {}) {
        const {
            trigger = elements.refreshSupportButton,
            silentToast = false
        } = options;

        if (!hasSystemRole("SystemSupport", "SystemAdmin")) {
            renderSupportCompanies([]);
            renderSupportTickets([]);
            return;
        }

        await refreshSupportCompanies({ trigger, silentToast: true });
        await refreshSupportTickets({ trigger, silentToast: true });

        if (!silentToast) {
            showToast("Support data refreshed.", "info");
        }
    }

    async function refreshSupportCompanies(options = {}) {
        const {
            trigger = elements.refreshSupportButton,
            silentToast = false
        } = options;

        const companies = await withBusy(trigger, async () => {
            const data = await apiRequest("/api/v1/system/support/companies", {
                method: "GET",
                auth: true,
                tag: "SUPPORT/COMPANIES"
            });
            return Array.isArray(data) ? data : [];
        });

        renderSupportCompanies(companies);
        if (!silentToast) {
            showToast("Support snapshots loaded.", "info");
        }
    }

    async function refreshSupportTickets(options = {}) {
        const {
            trigger = elements.refreshSupportButton,
            silentToast = false
        } = options;

        const tickets = await withBusy(trigger, async () => {
            const data = await apiRequest("/api/v1/system/support/tickets", {
                method: "GET",
                auth: true,
                tag: "SUPPORT/TICKETS"
            });
            return Array.isArray(data) ? data : [];
        });

        renderSupportTickets(tickets);
        if (!silentToast) {
            showToast("Support tickets loaded.", "info");
        }
    }

    async function refreshBillingData(options = {}) {
        const {
            trigger = elements.refreshBillingButton,
            silentToast = false
        } = options;

        if (!hasSystemRole("SystemBilling", "SystemAdmin")) {
            renderBillingSubscriptions([]);
            renderBillingInvoices([]);
            return;
        }

        await refreshBillingSubscriptions({ trigger, silentToast: true });
        await refreshBillingInvoices({ trigger, silentToast: true });

        if (!silentToast) {
            showToast("Billing data refreshed.", "info");
        }
    }

    async function refreshBillingSubscriptions(options = {}) {
        const {
            trigger = elements.refreshBillingButton,
            silentToast = false
        } = options;

        const subscriptions = await withBusy(trigger, async () => {
            const data = await apiRequest("/api/v1/system/billing/subscriptions", {
                method: "GET",
                auth: true,
                tag: "BILLING/SUBSCRIPTIONS"
            });
            return Array.isArray(data) ? data : [];
        });

        renderBillingSubscriptions(subscriptions);
        if (!silentToast) {
            showToast("Subscriptions loaded.", "info");
        }
    }

    async function refreshBillingInvoices(options = {}) {
        const {
            trigger = elements.refreshBillingButton,
            silentToast = false
        } = options;

        const invoices = await withBusy(trigger, async () => {
            const data = await apiRequest("/api/v1/system/billing/invoices", {
                method: "GET",
                auth: true,
                tag: "BILLING/INVOICES"
            });
            return Array.isArray(data) ? data : [];
        });

        renderBillingInvoices(invoices);
        if (!silentToast) {
            showToast("Invoices loaded.", "info");
        }
    }

    async function refreshTenantSubscription(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshSubscriptionButton,
            silentToast = false,
            silentErrors = false
        } = options;

        try {
            const subscription = await withBusy(trigger, async () => {
                return await apiRequest(`/api/v1/${state.companySlug}/subscription`, {
                    method: "GET",
                    auth: true,
                    tag: "TENANT/SUBSCRIPTION"
                });
            });

            renderSubscription(subscription);
            if (!silentToast) {
                showToast("Subscription loaded.", "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderSubscription(null);
                return;
            }
            throw error;
        }
    }

    async function refreshCostEstimates(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshPlanItemsButton,
            silentToast = false,
            silentErrors = false
        } = options;

        try {
            const estimates = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/costestimates`, {
                    method: "GET",
                    auth: true,
                    tag: "COST-ESTIMATE/LIST"
                });
                return Array.isArray(data) ? data : [];
            });

            state.costEstimates = estimates;
            if (!silentToast) {
                showToast("Cost estimates loaded.", "info");
            }
        } catch (error) {
            if (silentErrors) {
                state.costEstimates = [];
                return;
            }
            throw error;
        }
    }

    async function refreshResources(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshResourcesButton,
            silentToast = false,
            silentErrors = false
        } = options;

        await refreshDentists({ trigger, silentToast: true, silentErrors });
        await refreshTreatmentRooms({ trigger, silentToast: true, silentErrors });

        if (!silentToast) {
            showToast("Clinical resources refreshed.", "info");
        }
    }

    async function refreshDentists(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshResourcesButton,
            silentToast = false,
            silentErrors = false
        } = options;

        renderDentistsSkeleton();

        try {
            const dentists = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/dentists`, {
                    method: "GET",
                    auth: true,
                    tag: "DENTIST/LIST"
                });
                return Array.isArray(data) ? data : [];
            });

            state.dentists = dentists;
            renderDentists(dentists);
            log("DENTIST/LIST/SUCCESS", null, dentists);

            if (!silentToast) {
                showToast(`${dentists.length} dentist${dentists.length === 1 ? "" : "s"} loaded.`, "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderDentists([]);
                return;
            }
            throw error;
        }
    }

    async function refreshTreatmentRooms(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshResourcesButton,
            silentToast = false,
            silentErrors = false
        } = options;

        renderTreatmentRoomsSkeleton();

        try {
            const rooms = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/treatmentrooms`, {
                    method: "GET",
                    auth: true,
                    tag: "TREATMENT-ROOM/LIST"
                });
                return Array.isArray(data) ? data : [];
            });

            state.treatmentRooms = rooms;
            renderTreatmentRooms(rooms);
            log("TREATMENT-ROOM/LIST/SUCCESS", null, rooms);

            if (!silentToast) {
                showToast(`${rooms.length} room${rooms.length === 1 ? "" : "s"} loaded.`, "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderTreatmentRooms([]);
                return;
            }
            throw error;
        }
    }

    async function refreshAppointments(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshAppointmentsButton,
            silentToast = false,
            silentErrors = false
        } = options;

        renderAppointmentsSkeleton();

        try {
            const appointments = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/appointments`, {
                    method: "GET",
                    auth: true,
                    tag: "APPOINTMENT/LIST"
                });
                return Array.isArray(data) ? data : [];
            });

            state.appointments = appointments;
            renderAppointments(appointments);
            log("APPOINTMENT/LIST/SUCCESS", null, appointments);

            if (!silentToast) {
                showToast(`${appointments.length} appointment${appointments.length === 1 ? "" : "s"} loaded.`, "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderAppointments([]);
                return;
            }
            throw error;
        }
    }

    async function refreshOpenPlanItems(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshPlanItemsButton,
            silentToast = false,
            silentErrors = false
        } = options;

        renderPlanItemsSkeleton();

        try {
            const items = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/treatmentplans/openitems`, {
                    method: "GET",
                    auth: true,
                    tag: "TREATMENT-PLAN/OPEN-ITEMS"
                });
                return Array.isArray(data) ? data : [];
            });

            state.openPlanItems = items;
            renderOpenPlanItems(items);
            log("TREATMENT-PLAN/OPEN-ITEMS/SUCCESS", null, items);

            if (!silentToast) {
                showToast(`${items.length} pending plan item${items.length === 1 ? "" : "s"} loaded.`, "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderOpenPlanItems([]);
                return;
            }
            throw error;
        }
    }

    async function refreshTenantAdminViews(options = {}) {
        if (!state.companySlug) {
            return;
        }

        const isOwner = hasCompanyRole("CompanyOwner");
        const isAdmin = hasCompanyRole("CompanyAdmin");

        if (!isOwner && !isAdmin) {
            state.companyUsers = [];
            state.companySettings = null;
            renderCompanyUsers([]);
            renderCompanySettings(null);
            return;
        }

        await refreshCompanyUsers({ silentToast: true, silentErrors: true, trigger: options.trigger });

        if (isOwner) {
            await refreshCompanySettings({ silentToast: true, silentErrors: true, trigger: options.trigger });
        } else {
            state.companySettings = null;
            renderCompanySettings(null);
        }
    }

    async function refreshCompanyUsers(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshCompanyUsersButton,
            silentToast = false,
            silentErrors = false
        } = options;

        renderCompanyUsersSkeleton();

        try {
            const users = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/companyusers`, {
                    method: "GET",
                    auth: true,
                    tag: "COMPANY-USERS/LIST"
                });
                return Array.isArray(data) ? data : [];
            });

            state.companyUsers = users;
            renderCompanyUsers(users);
            log("COMPANY-USERS/LIST/SUCCESS", null, users);

            if (!silentToast) {
                showToast(`${users.length} membership${users.length === 1 ? "" : "s"} loaded.`, "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderCompanyUsers([]);
                return;
            }
            throw error;
        }
    }

    async function refreshCompanySettings(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshCompanySettingsButton,
            silentToast = false,
            silentErrors = false
        } = options;

        try {
            const settings = await withBusy(trigger, async () => {
                return await apiRequest(`/api/v1/${state.companySlug}/companysettings`, {
                    method: "GET",
                    auth: true,
                    tag: "COMPANY-SETTINGS/GET"
                });
            });

            state.companySettings = settings;
            renderCompanySettings(settings);
            log("COMPANY-SETTINGS/GET/SUCCESS", null, settings);

            if (!silentToast) {
                showToast("Company settings loaded.", "info");
            }
        } catch (error) {
            if (silentErrors) {
                renderCompanySettings(null);
                return;
            }
            throw error;
        }
    }

    function openDeleteDialog(patient) {
        const id = String(patient.id || "");
        if (!id) {
            return;
        }

        const fullName = `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "this patient";
        pendingDelete = { id, label: fullName };

        if (!elements.patientDeleteDialog || typeof elements.patientDeleteDialog.showModal !== "function") {
            const confirmed = window.confirm(`Delete ${fullName}?`);
            if (confirmed) {
                void confirmDelete();
            }
            return;
        }

        if (elements.patientDeleteText) {
            elements.patientDeleteText.textContent = `Delete ${fullName}? This action cannot be undone.`;
        }
        elements.patientDeleteDialog.showModal();
    }

    async function onDeleteDialogClose() {
        if (!elements.patientDeleteDialog) {
            return;
        }

        if (elements.patientDeleteDialog.returnValue !== "confirm") {
            pendingDelete = { id: "", label: "" };
            return;
        }

        try {
            await confirmDelete();
        } catch (error) {
            reportError(error);
        }
    }

    async function confirmDelete() {
        if (!pendingDelete.id) {
            return;
        }

        const patientId = pendingDelete.id;
        const patientLabel = pendingDelete.label;
        pendingDelete = { id: "", label: "" };

        await withBusy(elements.confirmDeleteButton, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/patients/${patientId}`, {
                method: "DELETE",
                auth: true,
                tag: "PATIENT/DELETE"
            });
            log("PATIENT/DELETE/SUCCESS", { id: patientId }, { message: "Deleted" });
            showToast(`Deleted ${patientLabel}.`, "success");
        });

        await refreshPatients({ silentToast: true, silentSyncStatus: true });
    }

    async function withBusy(target, action) {
        const controls = collectControls(target);
        setLoadingBar(true);
        setControlsBusy(controls, true);
        setContainerBusy(target, true);

        try {
            return await action();
        } finally {
            setControlsBusy(controls, false);
            setContainerBusy(target, false);
            setLoadingBar(false);
        }
    }

    function collectControls(target) {
        if (!target) return [];

        if (target instanceof HTMLFormElement) {
            return Array.from(target.querySelectorAll("button, input, select, textarea"));
        }

        if (target instanceof HTMLElement) {
            return [target];
        }

        return [];
    }

    function setContainerBusy(target, isBusy) {
        if (!(target instanceof HTMLElement)) {
            return;
        }

        if (isBusy) {
            target.setAttribute("aria-busy", "true");
        } else {
            target.removeAttribute("aria-busy");
        }
    }

    function setControlsBusy(controls, isBusy) {
        controls.forEach((control) => {
            if (!(control instanceof HTMLElement)) {
                return;
            }

            if (isBusy) {
                control.dataset.previousDisabled = control.disabled ? "true" : "false";
                control.disabled = true;
            } else {
                const wasDisabled = control.dataset.previousDisabled === "true";
                control.disabled = wasDisabled;
                delete control.dataset.previousDisabled;
            }
        });
    }

    function toggleFormControls(form, isEnabled) {
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        const controls = form.querySelectorAll("button, input, select, textarea");
        controls.forEach((control) => {
            if (!(control instanceof HTMLElement)) {
                return;
            }

            if (isEnabled) {
                if (control.dataset.lockedByRole === "true") {
                    delete control.dataset.lockedByRole;
                    control.disabled = false;
                }
            } else if (!control.disabled) {
                control.dataset.lockedByRole = "true";
                control.disabled = true;
            }
        });
    }

    function setLoadingBar(isActive) {
        if (!elements.loadingBar) {
            return;
        }

        elements.loadingBar.classList.toggle("is-active", isActive);
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
            const errorMessage = readErrorMessage(parsedBody) || `HTTP ${response.status}`;
            throw new Error(errorMessage);
        }

        return rawBody.length === 0 ? {} : parsedBody;
    }

    function renderSession() {
        const isAuthenticated = Boolean(state.jwt);
        const hasTenant = Boolean(state.companySlug);
        const systemRoleLabel = state.systemRoles.length > 0 ? state.systemRoles.join(", ") : "";

        if (elements.gatewayScreen) {
            elements.gatewayScreen.hidden = isAuthenticated;
        }
        if (elements.appRoot) {
            elements.appRoot.hidden = !isAuthenticated;
        }

        if (elements.authPill) {
            elements.authPill.textContent = isAuthenticated ? "Signed in" : "Not signed in";
            setBadgeVariant(elements.authPill, isAuthenticated ? "success" : "warning");
        }

        if (elements.companyPill) {
            elements.companyPill.textContent = hasTenant ? `Tenant: ${state.companySlug}` : "No tenant";
            setBadgeVariant(elements.companyPill, hasTenant ? "neutral" : "warning");
        }

        if (elements.sessionRole) {
            elements.sessionRole.textContent = systemRoleLabel || state.companyRole || "None";
        }

        if (elements.sessionTenant) {
            elements.sessionTenant.textContent = state.companySlug || "-";
        }

        if (elements.sessionExpiry) {
            elements.sessionExpiry.textContent = state.expiresInSeconds > 0
                ? `${state.expiresInSeconds}s`
                : "Unknown";
        }

        if (elements.overviewAuth) {
            elements.overviewAuth.textContent = isAuthenticated ? "Online" : "Offline";
        }

        if (elements.overviewTenant) {
            elements.overviewTenant.textContent = state.companySlug || "-";
        }

        if (elements.overviewPatientCount) {
            elements.overviewPatientCount.textContent = String(state.patients.length);
        }

        const allowedTabs = getAllowedTabs(isAuthenticated, hasTenant);
        applyTabVisibility(allowedTabs);

        const canManagePlatform = hasSystemRole("SystemAdmin");
        const canSupport = hasSystemRole("SystemSupport", "SystemAdmin");
        const canBill = hasSystemRole("SystemBilling", "SystemAdmin");
        const canAccessResources = hasTenant && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee");
        const canManageResources = hasTenant && hasCompanyRole("CompanyOwner", "CompanyAdmin");
        const canManageAppointments = hasTenant && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee");
        const canRecordPlanDecisions = hasTenant && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager");
        const canManageUsers = hasTenant && hasCompanyRole("CompanyOwner", "CompanyAdmin");
        const canManageSettings = hasTenant && hasCompanyRole("CompanyOwner");

        if (elements.refreshResourcesButton) {
            elements.refreshResourcesButton.disabled = !canAccessResources;
        }
        toggleFormControls(elements.dentistForm, canManageResources);
        toggleFormControls(elements.treatmentRoomForm, canManageResources);

        if (elements.refreshAppointmentsButton) {
            elements.refreshAppointmentsButton.disabled = !canManageAppointments;
        }
        toggleFormControls(elements.appointmentForm, canManageAppointments);

        if (elements.refreshPlanItemsButton) {
            elements.refreshPlanItemsButton.disabled = !canRecordPlanDecisions;
        }
        toggleFormControls(elements.planDecisionForm, canRecordPlanDecisions);
        toggleFormControls(elements.costEstimateForm, canRecordPlanDecisions);
        toggleFormControls(elements.legalEstimateForm, canRecordPlanDecisions);

        if (elements.refreshCompanyUsersButton) {
            elements.refreshCompanyUsersButton.disabled = !canManageUsers;
        }
        toggleFormControls(elements.companyUserForm, canManageUsers);

        if (elements.refreshCompanySettingsButton) {
            elements.refreshCompanySettingsButton.disabled = !canManageSettings;
        }
        toggleFormControls(elements.companySettingsForm, canManageSettings);
        toggleFormControls(elements.subscriptionForm, canManageSettings);

        if (elements.refreshPlatformButton) {
            elements.refreshPlatformButton.disabled = !canManagePlatform;
        }
        if (elements.refreshSupportButton) {
            elements.refreshSupportButton.disabled = !canSupport;
        }
        if (elements.refreshBillingButton) {
            elements.refreshBillingButton.disabled = !canBill;
        }
        toggleFormControls(elements.featureFlagForm, canManagePlatform);
        toggleFormControls(elements.companyActivationForm, canManagePlatform);
        toggleFormControls(elements.supportTicketForm, canSupport);
        toggleFormControls(elements.billingSubscriptionForm, canBill);
        toggleFormControls(elements.billingInvoiceStatusForm, canBill);

        const requested = getRequestedScreen();
        if (isAuthenticated && !allowedTabs.has(requested)) {
            activateScreen(resolveLandingScreen());
        }

        saveSession();
    }

    function setBadgeVariant(element, variant) {
        if (!element) return;

        element.classList.remove("badge--neutral", "badge--success", "badge--warning", "badge--danger");
        if (variant === "success") {
            element.classList.add("badge--success");
        } else if (variant === "warning") {
            element.classList.add("badge--warning");
        } else if (variant === "danger") {
            element.classList.add("badge--danger");
        } else {
            element.classList.add("badge--neutral");
        }
    }

    function setSyncStatus(text, variant) {
        if (!elements.overviewSyncStatus) {
            return;
        }

        elements.overviewSyncStatus.textContent = text;
        setBadgeVariant(elements.overviewSyncStatus, variant);
    }

    function renderPlatformAnalytics(analytics) {
        state.systemAnalytics = analytics;

        if (elements.platformCompanyCount) {
            elements.platformCompanyCount.textContent = String(analytics?.companyCount ?? 0);
        }
        if (elements.platformUserCount) {
            elements.platformUserCount.textContent = String(analytics?.userCount ?? 0);
        }
        if (elements.platformInvoiceCount) {
            elements.platformInvoiceCount.textContent = String(analytics?.invoiceCount ?? 0);
        }
    }

    function renderFeatureFlags(flags) {
        state.featureFlags = Array.isArray(flags) ? flags : [];

        if (!elements.featureFlagsBody) {
            return;
        }

        clearElement(elements.featureFlagsBody);
        if (state.featureFlags.length === 0) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 2;
            const content = document.createElement("div");
            content.className = "empty-state";
            content.textContent = "No feature flags loaded.";
            cell.appendChild(content);
            row.appendChild(cell);
            elements.featureFlagsBody.appendChild(row);
            return;
        }

        state.featureFlags.forEach((flag) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(flag.key || "-"));
            const statusCell = document.createElement("td");
            const badge = document.createElement("span");
            badge.className = `badge ${flag.enabled ? "badge--success" : "badge--warning"}`;
            badge.textContent = flag.enabled ? "Enabled" : "Disabled";
            statusCell.appendChild(badge);
            row.appendChild(statusCell);
            elements.featureFlagsBody.appendChild(row);
        });
    }

    function renderSupportCompanies(companies) {
        state.supportCompanies = Array.isArray(companies) ? companies : [];
        if (!elements.supportCompaniesBody) {
            return;
        }

        clearElement(elements.supportCompaniesBody);
        if (state.supportCompanies.length === 0) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 5;
            const content = document.createElement("div");
            content.className = "empty-state";
            content.textContent = "No snapshot data loaded.";
            cell.appendChild(content);
            row.appendChild(cell);
            elements.supportCompaniesBody.appendChild(row);
            return;
        }

        state.supportCompanies.forEach((company) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(company.companyName || "-"));
            row.appendChild(createCell(company.companySlug || "-"));
            row.appendChild(createCell(String(company.activeUserCount ?? 0)));
            row.appendChild(createCell(String(company.patientCount ?? 0)));
            row.appendChild(createCell(String(company.openInvoiceCount ?? 0)));
            elements.supportCompaniesBody.appendChild(row);
        });
    }

    function renderSupportTickets(tickets) {
        state.supportTickets = Array.isArray(tickets) ? tickets : [];
        if (!elements.supportTicketsBody) {
            return;
        }

        clearElement(elements.supportTicketsBody);
        if (state.supportTickets.length === 0) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 5;
            const content = document.createElement("div");
            content.className = "empty-state";
            content.textContent = "No support tickets loaded.";
            cell.appendChild(content);
            row.appendChild(cell);
            elements.supportTicketsBody.appendChild(row);
            return;
        }

        state.supportTickets.forEach((ticket) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(ticket.ticketId || "-"));
            row.appendChild(createCell(ticket.companySlug || "-"));
            row.appendChild(createCell(ticket.subject || "-"));
            row.appendChild(createCell(ticket.status || "-"));
            row.appendChild(createCell(formatDateTime(ticket.createdAtUtc)));
            elements.supportTicketsBody.appendChild(row);
        });
    }

    function renderBillingSubscriptions(subscriptions) {
        state.billingSubscriptions = Array.isArray(subscriptions) ? subscriptions : [];
        if (!elements.billingSubscriptionsBody) {
            return;
        }

        clearElement(elements.billingSubscriptionsBody);
        if (state.billingSubscriptions.length === 0) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 6;
            const content = document.createElement("div");
            content.className = "empty-state";
            content.textContent = "No subscriptions loaded.";
            cell.appendChild(content);
            row.appendChild(cell);
            elements.billingSubscriptionsBody.appendChild(row);
            return;
        }

        state.billingSubscriptions.forEach((subscription) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(subscription.companySlug || "-"));
            row.appendChild(createCell(subscription.tier || "-"));
            row.appendChild(createCell(subscription.status || "-"));
            row.appendChild(createCell(String(subscription.userLimit ?? 0)));
            row.appendChild(createCell(String(subscription.entityLimit ?? 0)));
            row.appendChild(createCell(subscription.subscriptionId || "-"));
            elements.billingSubscriptionsBody.appendChild(row);
        });
    }

    function renderBillingInvoices(invoices) {
        state.billingInvoices = Array.isArray(invoices) ? invoices : [];
        if (!elements.billingInvoicesBody) {
            return;
        }

        clearElement(elements.billingInvoicesBody);
        if (state.billingInvoices.length === 0) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 7;
            const content = document.createElement("div");
            content.className = "empty-state";
            content.textContent = "No invoices loaded.";
            cell.appendChild(content);
            row.appendChild(cell);
            elements.billingInvoicesBody.appendChild(row);
            return;
        }

        state.billingInvoices.forEach((invoice) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(invoice.companySlug || "-"));
            row.appendChild(createCell(invoice.invoiceNumber || "-"));
            row.appendChild(createCell(formatMoney(invoice.totalAmount)));
            row.appendChild(createCell(formatMoney(invoice.balanceAmount)));
            row.appendChild(createCell(invoice.status || "-"));
            row.appendChild(createCell(formatDateTime(invoice.dueDateUtc)));
            row.appendChild(createCell(invoice.invoiceId || "-"));
            elements.billingInvoicesBody.appendChild(row);
        });
    }

    function renderSubscription(subscription) {
        state.subscription = subscription;
        if (!elements.subscriptionForm) {
            return;
        }

        const tier = subscription?.tier || "Free";
        elements.subscriptionForm.tier.value = tier;

        if (elements.subscriptionPill) {
            if (!subscription) {
                elements.subscriptionPill.textContent = "No subscription loaded";
                setBadgeVariant(elements.subscriptionPill, "warning");
            } else {
                elements.subscriptionPill.textContent = `${subscription.tier} | users: ${subscription.userLimit} | entities: ${subscription.entityLimit}`;
                setBadgeVariant(elements.subscriptionPill, "neutral");
            }
        }
    }

    function renderLegalEstimateOutput(text) {
        if (!elements.legalEstimateOutput) {
            return;
        }

        elements.legalEstimateOutput.textContent = text || "No legal preview generated yet.";
    }

    function renderPatients(patients) {
        if (!elements.patientsBody) {
            return;
        }

        clearElement(elements.patientsBody);

        if (!Array.isArray(patients) || patients.length === 0) {
            renderPatientsEmptyState();
            state.patients = [];
            renderAppointmentSelectOptions();
            return;
        }

        patients.forEach((patient) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(`${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "-"));
            row.appendChild(createCell(patient.dateOfBirth ?? "-"));
            row.appendChild(createCell(patient.personalCode ?? "-"));
            row.appendChild(createCell(patient.email ?? "-"));
            row.appendChild(createCell(patient.phone ?? "-"));

            const actionsCell = createCell("");
            actionsCell.classList.add("text-right");
            const deleteButton = document.createElement("button");
            deleteButton.type = "button";
            deleteButton.className = "btn btn--ghost btn--sm";
            deleteButton.textContent = "Delete";
            deleteButton.setAttribute("aria-label", `Delete patient ${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim());
            deleteButton.addEventListener("click", () => openDeleteDialog(patient));

            actionsCell.appendChild(deleteButton);
            row.appendChild(actionsCell);
            elements.patientsBody.appendChild(row);
        });

        state.patients = patients;
        renderAppointmentSelectOptions();
    }

    function renderDentists(dentists) {
        if (!elements.dentistsBody) {
            return;
        }

        clearElement(elements.dentistsBody);

        if (!Array.isArray(dentists) || dentists.length === 0) {
            renderDentistsEmptyState();
            state.dentists = [];
            renderAppointmentSelectOptions();
            return;
        }

        dentists.forEach((dentist) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(dentist.displayName || "-"));
            row.appendChild(createCell(dentist.licenseNumber || "-"));
            row.appendChild(createCell(dentist.specialty || "-"));
            row.appendChild(createCell(dentist.id || "-"));
            elements.dentistsBody.appendChild(row);
        });

        state.dentists = dentists;
        renderAppointmentSelectOptions();
    }

    function renderTreatmentRooms(rooms) {
        if (!elements.treatmentRoomsBody) {
            return;
        }

        clearElement(elements.treatmentRoomsBody);

        if (!Array.isArray(rooms) || rooms.length === 0) {
            renderTreatmentRoomsEmptyState();
            state.treatmentRooms = [];
            renderAppointmentSelectOptions();
            return;
        }

        rooms.forEach((room) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(room.name || "-"));
            row.appendChild(createCell(room.code || "-"));

            const statusCell = document.createElement("td");
            const statusBadge = document.createElement("span");
            statusBadge.className = `badge ${room.isActiveRoom ? "badge--success" : "badge--warning"}`;
            statusBadge.textContent = room.isActiveRoom ? "Active" : "Inactive";
            statusCell.appendChild(statusBadge);
            row.appendChild(statusCell);

            row.appendChild(createCell(room.id || "-"));
            elements.treatmentRoomsBody.appendChild(row);
        });

        state.treatmentRooms = rooms;
        renderAppointmentSelectOptions();
    }

    function renderAppointments(appointments) {
        if (!elements.appointmentsBody) {
            return;
        }

        clearElement(elements.appointmentsBody);

        if (!Array.isArray(appointments) || appointments.length === 0) {
            renderAppointmentsEmptyState();
            state.appointments = [];
            return;
        }

        appointments.forEach((appointment) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(resolvePatientName(appointment.patientId)));
            row.appendChild(createCell(resolveDentistName(appointment.dentistId)));
            row.appendChild(createCell(resolveRoomLabel(appointment.treatmentRoomId)));
            row.appendChild(createCell(formatDateTime(appointment.startAtUtc)));
            row.appendChild(createCell(formatDateTime(appointment.endAtUtc)));

            const statusCell = document.createElement("td");
            const statusBadge = document.createElement("span");
            statusBadge.className = "badge badge--neutral";
            statusBadge.textContent = appointment.status || "-";
            statusCell.appendChild(statusBadge);
            row.appendChild(statusCell);

            elements.appointmentsBody.appendChild(row);
        });

        state.appointments = appointments;
    }

    function renderOpenPlanItems(items) {
        if (!elements.planItemsBody) {
            return;
        }

        clearElement(elements.planItemsBody);
        updatePlanItemSelection(items);

        if (!Array.isArray(items) || items.length === 0) {
            renderOpenPlanItemsEmptyState();
            state.openPlanItems = [];
            return;
        }

        items.forEach((item) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(item.patientName || "-"));
            row.appendChild(createCell(item.planId || "-"));
            row.appendChild(createCell(item.planItemId || "-"));
            row.appendChild(createCell(item.treatmentTypeName || "-"));
            row.appendChild(createCell(item.urgency || "-"));
            row.appendChild(createCell(formatMoney(item.estimatedPrice)));
            elements.planItemsBody.appendChild(row);
        });

        state.openPlanItems = items;
    }

    function renderDentistsSkeleton() {
        if (!elements.dentistsBody) {
            return;
        }

        clearElement(elements.dentistsBody);
        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 4; cellIndex += 1) {
                const cell = document.createElement("td");
                const skeleton = document.createElement("div");
                skeleton.className = "skeleton";
                skeleton.style.width = `${55 + (cellIndex * 8)}%`;
                cell.appendChild(skeleton);
                row.appendChild(cell);
            }
            elements.dentistsBody.appendChild(row);
        }
    }

    function renderTreatmentRoomsSkeleton() {
        if (!elements.treatmentRoomsBody) {
            return;
        }

        clearElement(elements.treatmentRoomsBody);
        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 4; cellIndex += 1) {
                const cell = document.createElement("td");
                const skeleton = document.createElement("div");
                skeleton.className = "skeleton";
                skeleton.style.width = `${50 + (cellIndex * 10)}%`;
                cell.appendChild(skeleton);
                row.appendChild(cell);
            }
            elements.treatmentRoomsBody.appendChild(row);
        }
    }

    function renderAppointmentsSkeleton() {
        if (!elements.appointmentsBody) {
            return;
        }

        clearElement(elements.appointmentsBody);
        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 6; cellIndex += 1) {
                const cell = document.createElement("td");
                const skeleton = document.createElement("div");
                skeleton.className = "skeleton";
                skeleton.style.width = `${50 + (cellIndex * 7)}%`;
                cell.appendChild(skeleton);
                row.appendChild(cell);
            }
            elements.appointmentsBody.appendChild(row);
        }
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

    function renderDentistsEmptyState() {
        if (!elements.dentistsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 4;
        content.className = "empty-state";
        content.textContent = "No dentists loaded.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.dentistsBody.appendChild(row);
    }

    function renderTreatmentRoomsEmptyState() {
        if (!elements.treatmentRoomsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 4;
        content.className = "empty-state";
        content.textContent = "No treatment rooms loaded.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.treatmentRoomsBody.appendChild(row);
    }

    function renderAppointmentsEmptyState() {
        if (!elements.appointmentsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 6;
        content.className = "empty-state";
        content.textContent = "No appointments loaded.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.appointmentsBody.appendChild(row);
    }

    function renderOpenPlanItemsEmptyState() {
        if (!elements.planItemsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 6;
        content.className = "empty-state";
        content.textContent = "No pending plan items loaded.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.planItemsBody.appendChild(row);
    }

    function renderAppointmentSelectOptions() {
        const patientOptions = state.patients.map((patient) => ({
                value: patient.id,
                label: `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "Unnamed patient"
            }));

        setSelectOptions(
            elements.appointmentPatientSelect,
            patientOptions,
            "Select patient"
        );

        setSelectOptions(
            elements.estimatePatientSelect,
            patientOptions,
            "Select patient"
        );

        setSelectOptions(
            elements.appointmentDentistSelect,
            state.dentists.map((dentist) => ({
                value: dentist.id,
                label: dentist.displayName || dentist.licenseNumber || "Unnamed dentist"
            })),
            "Select dentist"
        );

        setSelectOptions(
            elements.appointmentRoomSelect,
            state.treatmentRooms
                .filter((room) => room.isActiveRoom)
                .map((room) => ({
                    value: room.id,
                    label: `${room.code || room.name || "Room"}${room.name ? ` - ${room.name}` : ""}`
                })),
            "Select room"
        );
    }

    function updatePlanItemSelection(items) {
        setSelectOptions(
            elements.planItemSelection,
            (Array.isArray(items) ? items : []).map((item) => ({
                value: `${item.planId}|${item.planItemId}`,
                label: `${item.patientName || "Unknown patient"} • ${item.treatmentTypeName || "Treatment"} • ${item.urgency || "Urgency"}`
            })),
            "Select plan item"
        );
    }

    function setSelectOptions(selectElement, options, placeholder) {
        if (!(selectElement instanceof HTMLSelectElement)) {
            return;
        }

        const previousValue = selectElement.value;
        clearElement(selectElement);

        const placeholderOption = document.createElement("option");
        placeholderOption.value = "";
        placeholderOption.textContent = placeholder;
        selectElement.appendChild(placeholderOption);

        options.forEach((option) => {
            const element = document.createElement("option");
            element.value = option.value;
            element.textContent = option.label;
            selectElement.appendChild(element);
        });

        if (previousValue && options.some((option) => option.value === previousValue)) {
            selectElement.value = previousValue;
        } else {
            selectElement.value = "";
        }
    }

    function resolvePatientName(patientId) {
        const patient = state.patients.find((item) => item.id === patientId);
        if (!patient) {
            return patientId || "-";
        }

        return `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || patient.id;
    }

    function resolveDentistName(dentistId) {
        const dentist = state.dentists.find((item) => item.id === dentistId);
        return dentist?.displayName || dentist?.licenseNumber || dentistId || "-";
    }

    function resolveRoomLabel(roomId) {
        const room = state.treatmentRooms.find((item) => item.id === roomId);
        if (!room) {
            return roomId || "-";
        }

        return `${room.code || "-"}${room.name ? ` (${room.name})` : ""}`;
    }

    function renderCompanyUsers(users) {
        if (!elements.companyUsersBody) {
            return;
        }

        clearElement(elements.companyUsersBody);

        if (!Array.isArray(users) || users.length === 0) {
            renderCompanyUsersEmptyState();
            state.companyUsers = [];
            return;
        }

        users.forEach((membership) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(membership.email || "-"));
            row.appendChild(createCell(membership.roleName || "-"));

            const statusCell = document.createElement("td");
            const statusBadge = document.createElement("span");
            statusBadge.className = `badge ${membership.isActive ? "badge--success" : "badge--warning"}`;
            statusBadge.textContent = membership.isActive ? "Active" : "Inactive";
            statusCell.appendChild(statusBadge);
            row.appendChild(statusCell);

            row.appendChild(createCell(formatDateTime(membership.assignedAtUtc)));
            elements.companyUsersBody.appendChild(row);
        });

        state.companyUsers = users;
    }

    function renderCompanyUsersSkeleton() {
        if (!elements.companyUsersBody) {
            return;
        }

        clearElement(elements.companyUsersBody);

        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 4; cellIndex += 1) {
                const cell = document.createElement("td");
                const skeleton = document.createElement("div");
                skeleton.className = "skeleton";
                skeleton.style.width = `${55 + (cellIndex * 8)}%`;
                cell.appendChild(skeleton);
                row.appendChild(cell);
            }
            elements.companyUsersBody.appendChild(row);
        }
    }

    function renderCompanyUsersEmptyState() {
        if (!elements.companyUsersBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 4;
        content.className = "empty-state";
        content.textContent = "No memberships loaded.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.companyUsersBody.appendChild(row);
    }

    function renderCompanySettings(settings) {
        if (!elements.companySettingsForm) {
            return;
        }

        const form = elements.companySettingsForm;
        const countryCode = settings?.countryCode || "EE";
        const currencyCode = settings?.currencyCode || "EUR";
        const timezone = settings?.timezone || "Europe/Tallinn";
        const interval = Number(settings?.defaultXrayIntervalMonths || 12);

        form.countryCode.value = countryCode;
        form.currencyCode.value = currencyCode;
        form.timezone.value = timezone;
        form.defaultXrayIntervalMonths.value = String(interval);

        state.companySettings = settings;
    }

    function renderPatientsSkeleton() {
        if (!elements.patientsBody) {
            return;
        }

        clearElement(elements.patientsBody);

        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 6; cellIndex += 1) {
                const cell = document.createElement("td");
                const skeleton = document.createElement("div");
                skeleton.className = "skeleton";
                skeleton.style.width = cellIndex === 5 ? "4rem" : `${40 + (cellIndex * 8)}%`;
                cell.appendChild(skeleton);
                row.appendChild(cell);
            }
            elements.patientsBody.appendChild(row);
        }
    }

    function renderPatientsEmptyState() {
        if (!elements.patientsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");

        cell.colSpan = 6;
        content.className = "empty-state";
        content.textContent = "No patient data yet.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.patientsBody.appendChild(row);
    }

    function createCell(text) {
        const cell = document.createElement("td");
        cell.textContent = text;
        return cell;
    }

    function clearElement(element) {
        while (element.firstChild) {
            element.removeChild(element.firstChild);
        }
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
        state.dentists = [];
        state.treatmentRooms = [];
        state.appointments = [];
        state.openPlanItems = [];
        state.companyUsers = [];
        state.companySettings = null;
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

    function getAllowedTabs(isAuthenticated, hasTenant) {
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

        if (!hasTenant) {
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

    function formatTime(value) {
        return value.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    }

    function formatDateTime(value) {
        if (!value) {
            return "-";
        }

        const parsed = new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return "-";
        }

        return parsed.toLocaleString();
    }

    function toUtcIso(localDateTimeValue) {
        const parsed = new Date(localDateTimeValue);
        if (Number.isNaN(parsed.getTime())) {
            throw new Error("Invalid date/time value.");
        }

        return parsed.toISOString();
    }

    function formatMoney(value) {
        const parsed = Number(value);
        if (!Number.isFinite(parsed)) {
            return "-";
        }

        return parsed.toFixed(2);
    }

    function extractSystemRolesFromJwt(jwt) {
        if (!jwt || typeof jwt !== "string") {
            return [];
        }

        try {
            const parts = jwt.split(".");
            if (parts.length < 2) {
                return [];
            }

            const payloadBase64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
            const padded = payloadBase64 + "=".repeat((4 - (payloadBase64.length % 4)) % 4);
            const payload = JSON.parse(window.atob(padded));

            const rolesClaim = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? payload.role;
            const roles = Array.isArray(rolesClaim) ? rolesClaim : (rolesClaim ? [rolesClaim] : []);
            const allowed = new Set(["SystemAdmin", "SystemSupport", "SystemBilling"]);

            return roles
                .filter((role) => typeof role === "string" && allowed.has(role))
                .map((role) => role.trim());
        } catch {
            return [];
        }
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
            return {
                jwt: parsed.jwt || "",
                refreshToken: parsed.refreshToken || "",
                expiresInSeconds: Number(parsed.expiresInSeconds || 0),
                companySlug: parsed.companySlug || "",
                companyRole: parsed.companyRole || "",
                systemRoles: Array.isArray(parsed.systemRoles) ? parsed.systemRoles : extractSystemRolesFromJwt(parsed.jwt || "")
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
})();
