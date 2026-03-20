(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerAuthModule(app) {
    const {
        state,
        elements,
        activateScreen,
        apiRequest,
        applyJwtResponse,
        applyTabVisibility,
        clearSession,
        formatTime,
        getAllowedTabs,
        hasAnySystemRole,
        hasCompanyRole,
        hasSystemRole,
        log,
        renderLegalEstimateOutput,
        requireJwt,
        requireSystemRole,
        resolveLandingScreen,
        setBadgeVariant,
        setSyncStatus,
        showToast,
        syncPublicEntryState,
        toggleFormControls,
        withBusy
    } = app;

    function renderPatients(items) {
        if (typeof app.renderPatients === "function") {
            app.renderPatients(items);
        }
    }

    function renderPatientProfile(profile) {
        if (typeof app.renderPatientProfile === "function") {
            app.renderPatientProfile(profile);
        }
    }

    function renderDentists(items) {
        if (typeof app.renderDentists === "function") {
            app.renderDentists(items);
        }
    }

    function renderDentistProfile(profile) {
        if (typeof app.renderDentistProfile === "function") {
            app.renderDentistProfile(profile);
        }
    }

    function renderTreatmentRooms(items) {
        if (typeof app.renderTreatmentRooms === "function") {
            app.renderTreatmentRooms(items);
        }
    }

    function renderAppointments(items) {
        if (typeof app.renderAppointments === "function") {
            app.renderAppointments(items);
        }
    }

    function renderOpenPlanItems(items) {
        if (typeof app.renderOpenPlanItems === "function") {
            app.renderOpenPlanItems(items);
        }
    }

    function renderCompanyUsers(items) {
        if (typeof app.renderCompanyUsers === "function") {
            app.renderCompanyUsers(items);
        }
    }

    function renderCompanySettings(settings) {
        if (typeof app.renderCompanySettings === "function") {
            app.renderCompanySettings(settings);
        }
    }

    function renderSubscription(subscription) {
        if (typeof app.renderSubscription === "function") {
            app.renderSubscription(subscription);
        }
    }

    function renderPlatformAnalytics(analytics) {
        if (typeof app.renderPlatformAnalytics === "function") {
            app.renderPlatformAnalytics(analytics);
        }
    }

    function renderFeatureFlags(flags) {
        if (typeof app.renderFeatureFlags === "function") {
            app.renderFeatureFlags(flags);
        }
    }

    function renderSupportCompanies(companies) {
        if (typeof app.renderSupportCompanies === "function") {
            app.renderSupportCompanies(companies);
        }
    }

    function renderSupportTickets(tickets) {
        if (typeof app.renderSupportTickets === "function") {
            app.renderSupportTickets(tickets);
        }
    }

    function renderBillingSubscriptions(subscriptions) {
        if (typeof app.renderBillingSubscriptions === "function") {
            app.renderBillingSubscriptions(subscriptions);
        }
    }

    function renderBillingInvoices(invoices) {
        if (typeof app.renderBillingInvoices === "function") {
            app.renderBillingInvoices(invoices);
        }
    }

    function resetFinancePolicyForm() {
        if (typeof app.resetFinancePolicyForm === "function") {
            app.resetFinancePolicyForm();
        }
    }

    function resetFinancePaymentPlanForm() {
        if (typeof app.resetFinancePaymentPlanForm === "function") {
            app.resetFinancePaymentPlanForm();
        }
    }

    function renderFinanceWorkspace(workspace) {
        if (typeof app.renderFinanceWorkspace === "function") {
            app.renderFinanceWorkspace(workspace);
        }
    }

    function renderFinanceInvoiceDetail(detail) {
        if (typeof app.renderFinanceInvoiceDetail === "function") {
            app.renderFinanceInvoiceDetail(detail);
        }
    }

    function renderFinancePlanReview() {
        if (typeof app.renderFinancePlanReview === "function") {
            app.renderFinancePlanReview();
        }
    }

    function resetAppointmentClinicalForm() {
        if (typeof app.resetAppointmentClinicalForm === "function") {
            app.resetAppointmentClinicalForm();
        }
    }

    async function refreshPlatformData(options) {
        if (typeof app.refreshPlatformData === "function") {
            await app.refreshPlatformData(options);
        }
    }

    async function refreshSupportData(options) {
        if (typeof app.refreshSupportData === "function") {
            await app.refreshSupportData(options);
        }
    }

    async function refreshBillingData(options) {
        if (typeof app.refreshBillingData === "function") {
            await app.refreshBillingData(options);
        }
    }

    async function refreshPatients(options) {
        if (typeof app.refreshPatients === "function") {
            await app.refreshPatients(options);
        }
    }

    async function refreshClinicalViews(options) {
        if (typeof app.refreshClinicalViews === "function") {
            await app.refreshClinicalViews(options);
        }
    }

    async function refreshTenantAdminViews(options) {
        if (typeof app.refreshTenantAdminViews === "function") {
            await app.refreshTenantAdminViews(options);
        }
    }

    async function refreshTenantSubscription(options) {
        if (typeof app.refreshTenantSubscription === "function") {
            await app.refreshTenantSubscription(options);
        }
    }

    async function refreshPatientProfile(options) {
        if (typeof app.refreshPatientProfile === "function") {
            await app.refreshPatientProfile(options);
        }
    }

    async function onOnboardingSubmit(event) {
        event.preventDefault();
        requireSystemRole("SystemAdmin", "SystemSupport");

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
                auth: true,
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
            showToast(
                data.resetToken
                    ? "Reset token generated (development mode)."
                    : "If account exists, reset instructions are available.",
                "info"
            );
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
            renderPatientProfile(null);
            renderDentists([]);
            renderDentistProfile(null);
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
            resetFinancePolicyForm();
            resetFinancePaymentPlanForm();
            renderFinanceWorkspace(null);
            renderFinanceInvoiceDetail(null);
            renderLegalEstimateOutput("No legal preview generated yet.");
            resetAppointmentClinicalForm();
            setSyncStatus("Idle", "neutral");
            log("LOGOUT/SUCCESS", null, { message: "Session cleared." });
            showToast("Logged out.", "info");
            syncPublicEntryState();
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

        if (state.companySlug && state.companyRole) {
            await refreshPatients({ silentToast: true, silentSyncStatus: true, trigger: options.trigger });
            await refreshClinicalViews({ silentToast: true, trigger: options.trigger });
            await refreshTenantAdminViews({ silentToast: true, trigger: options.trigger });
            await refreshTenantSubscription({ silentToast: true, silentErrors: true, trigger: options.trigger });

            if (state.patientProfile?.id) {
                await refreshPatientProfile({ silentToast: true, trigger: options.trigger });
            }
        } else if (state.companySlug) {
            setSyncStatus("Tenant selected without company access", "warning");
        } else if (!hasAnySystemRole()) {
            setSyncStatus("No active tenant", "warning");
        }

        if (!silentSyncStatus && state.companySlug && state.companyRole) {
            setSyncStatus(`Synced ${formatTime(new Date())}`, "success");
        }

        if (!silentToast) {
            showToast("Workspace refreshed.", "info");
        }
    }

    function renderSession() {
        const isAuthenticated = Boolean(state.jwt);
        const hasTenant = Boolean(state.companySlug);
        const hasTenantAccess = Boolean(state.companySlug && state.companyRole);
        const systemRoleLabel = state.systemRoles.length > 0 ? state.systemRoles.join(", ") : "";

        if (elements.gatewayScreen) {
            elements.gatewayScreen.hidden = isAuthenticated;
        }
        if (elements.appRoot) {
            elements.appRoot.hidden = !isAuthenticated;
        }
        if (!isAuthenticated) {
            syncPublicEntryState();
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

        const allowedTabs = getAllowedTabs(isAuthenticated, hasTenantAccess);
        applyTabVisibility(allowedTabs);

        const canManagePlatform = hasSystemRole("SystemAdmin");
        const canOnboardCompanies = hasSystemRole("SystemAdmin", "SystemSupport");
        const canSupport = hasSystemRole("SystemSupport", "SystemAdmin");
        const canBill = hasSystemRole("SystemBilling", "SystemAdmin");
        const canAccessResources = hasTenantAccess && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee");
        const canManageResources = hasTenantAccess && hasCompanyRole("CompanyOwner", "CompanyAdmin");
        const canManagePatients = hasTenantAccess && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee");
        const canManageAppointments = hasTenantAccess && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee");
        const canRecordPlanDecisions = hasTenantAccess && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager");
        const canManageUsers = hasTenantAccess && hasCompanyRole("CompanyOwner", "CompanyAdmin");
        const canManageSettings = hasTenantAccess && hasCompanyRole("CompanyOwner");

        if (elements.refreshPatientsButton) {
            elements.refreshPatientsButton.disabled = !canManagePatients;
        }
        if (elements.patientProfileRefreshButton) {
            elements.patientProfileRefreshButton.disabled = !canManagePatients;
        }
        if (elements.patientProfileDeleteButton) {
            elements.patientProfileDeleteButton.disabled = !canManagePatients;
        }
        toggleFormControls(elements.patientForm, canManagePatients);
        toggleFormControls(elements.patientProfileForm, canManagePatients);

        if (elements.refreshResourcesButton) {
            elements.refreshResourcesButton.disabled = !canAccessResources;
        }
        toggleFormControls(elements.dentistForm, canManageResources);
        toggleFormControls(elements.dentistProfileForm, canManageResources);
        toggleFormControls(elements.treatmentRoomForm, canManageResources);
        if (elements.dentistProfileRefreshButton) {
            elements.dentistProfileRefreshButton.disabled = !canAccessResources;
        }
        if (elements.dentistProfileDeleteButton) {
            elements.dentistProfileDeleteButton.disabled = !canManageResources;
        }

        if (elements.refreshAppointmentsButton) {
            elements.refreshAppointmentsButton.disabled = !canManageAppointments;
        }
        toggleFormControls(elements.appointmentForm, canManageAppointments);
        toggleFormControls(elements.appointmentClinicalForm, canManageAppointments);
        if (elements.appointmentClinicalAddRowButton) {
            elements.appointmentClinicalAddRowButton.disabled = !canManageAppointments;
        }

        if (elements.refreshPlanItemsButton) {
            elements.refreshPlanItemsButton.disabled = !canRecordPlanDecisions;
        }
        toggleFormControls(elements.planDecisionForm, canRecordPlanDecisions);
        toggleFormControls(elements.costEstimateForm, canRecordPlanDecisions);
        toggleFormControls(elements.legalEstimateForm, canRecordPlanDecisions);
        if (elements.refreshFinanceButton) {
            elements.refreshFinanceButton.disabled = !canRecordPlanDecisions;
        }
        if (elements.financePatientSelect instanceof HTMLSelectElement) {
            elements.financePatientSelect.disabled = !canRecordPlanDecisions;
        }
        if (elements.financePlanSelect instanceof HTMLSelectElement) {
            elements.financePlanSelect.disabled = !canRecordPlanDecisions;
        }
        if (elements.financePolicyResetButton) {
            elements.financePolicyResetButton.disabled = !canRecordPlanDecisions;
        }
        if (elements.financeAddInstallmentButton) {
            elements.financeAddInstallmentButton.disabled = !canRecordPlanDecisions;
        }
        toggleFormControls(elements.financePolicyForm, canRecordPlanDecisions);
        toggleFormControls(elements.financeInvoiceGenerateForm, canRecordPlanDecisions);
        toggleFormControls(elements.financePaymentForm, canRecordPlanDecisions);
        toggleFormControls(elements.financePaymentPlanForm, canRecordPlanDecisions);
        renderFinancePlanReview();

        if (elements.refreshCompanyUsersButton) {
            elements.refreshCompanyUsersButton.disabled = !canManageUsers;
        }
        toggleFormControls(elements.companyUserForm, canManageUsers);
        toggleFormControls(elements.companyRoleSwitchForm, hasTenantAccess);

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
        toggleFormControls(elements.onboardingForm, canOnboardCompanies);
        toggleFormControls(elements.supportTicketForm, canSupport);
        toggleFormControls(elements.billingSubscriptionForm, canBill);
        toggleFormControls(elements.billingInvoiceStatusForm, canBill);
        if (elements.onboardingCard) {
            elements.onboardingCard.hidden = !canOnboardCompanies;
        }
        if (elements.authGrid) {
            elements.authGrid.classList.toggle("auth-grid--single", !canOnboardCompanies);
        }

        const requested = typeof app.getRequestedScreen === "function" ? app.getRequestedScreen() : "overview";
        if (isAuthenticated && !allowedTabs.has(requested)) {
            activateScreen(resolveLandingScreen());
        }

        if (typeof app.saveSession === "function") {
            app.saveSession();
        }
    }

    Object.assign(app, {
        onForgotPasswordSubmit,
        onGatewayLoginSubmit,
        onLoginSubmit,
        onLogoutClick,
        onOnboardingSubmit,
        onResetPasswordSubmit,
        onSwitchSubmit,
        refreshAllViewsForCurrentSession,
        renderSession
    });
});
