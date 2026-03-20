(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerTenantAdminModule(app) {
    const {
        state,
        elements,
        requireTenant,
        hasCompanyRole,
        withBusy,
        apiRequest,
        showToast,
        log,
        optional,
        applyJwtResponse,
        renderSession,
        refreshAllViewsForCurrentSession,
        resolveLandingScreen,
        activateScreen,
        clearElement,
        createCell,
        formatDateTime,
        setSelectOptions,
        getCurrentUserIdFromJwt,
        getCurrentUserEmailFromJwt,
        setBadgeVariant
    } = app;

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

    async function onCompanyRoleSwitchSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const roleName = form.roleName.value;
        if (!roleName) {
            throw new Error("Select a role first.");
        }

        await withBusy(form, async () => {
            const data = await apiRequest("/api/v1/account/switchrole", {
                method: "POST",
                body: { roleName },
                auth: true,
                tag: "ACCOUNT/SWITCH-ROLE"
            });

            applyJwtResponse(data);
            renderSession();
            log("ACCOUNT/SWITCH-ROLE/SUCCESS", { roleName }, data);
            showToast(`Active role switched to ${state.companyRole}.`, "success");
            await refreshAllViewsForCurrentSession({ silentToast: true, silentSyncStatus: true, trigger: form });
            activateScreen(resolveLandingScreen());
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

    function renderCompanyUsers(users) {
        if (!elements.companyUsersBody) {
            return;
        }

        clearElement(elements.companyUsersBody);

        if (!Array.isArray(users) || users.length === 0) {
            renderCompanyUsersEmptyState();
            state.companyUsers = [];
            renderCompanyRoleSwitchOptions([]);
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
        renderCompanyRoleSwitchOptions(users);
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

    function renderCompanyRoleSwitchOptions(users) {
        const memberships = Array.isArray(users) ? users : [];
        const currentUserId = getCurrentUserIdFromJwt();
        const currentUserEmail = getCurrentUserEmailFromJwt();
        const ownMemberships = memberships
            .filter((membership) => membership.isActive)
            .filter((membership) =>
                (currentUserId && membership.appUserId === currentUserId)
                || (currentUserEmail && membership.email?.toLowerCase() === currentUserEmail.toLowerCase()))
            .sort((left, right) => String(left.roleName || "").localeCompare(String(right.roleName || "")));

        setSelectOptions(
            elements.companyRoleSwitchSelect,
            ownMemberships.map((membership) => ({
                value: membership.roleName,
                label: membership.roleName
            })),
            "Select role"
        );

        if (elements.companyRoleSwitchSelect instanceof HTMLSelectElement && state.companyRole) {
            const hasCurrentRole = ownMemberships.some((membership) => membership.roleName === state.companyRole);
            if (hasCurrentRole) {
                elements.companyRoleSwitchSelect.value = state.companyRole;
            }
        }

        if (elements.companyRoleSwitchHelp) {
            if (ownMemberships.length === 0) {
                elements.companyRoleSwitchHelp.textContent = "No additional active memberships for the current signed-in user were found in this company.";
            } else if (ownMemberships.length === 1) {
                elements.companyRoleSwitchHelp.textContent = "Your account currently has one active membership in this company, so there is nothing else to switch to.";
            } else {
                elements.companyRoleSwitchHelp.textContent = "Switching updates the active company role in your current JWT for this company only. Your other memberships stay stored and can be selected again later.";
            }
        }
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

    Object.assign(app, {
        onSubscriptionSubmit,
        onCompanyUserUpsertSubmit,
        onCompanyRoleSwitchSubmit,
        onCompanySettingsSubmit,
        refreshTenantSubscription,
        refreshTenantAdminViews,
        refreshCompanyUsers,
        refreshCompanySettings,
        renderCompanyUsers,
        renderCompanyUsersSkeleton,
        renderCompanyUsersEmptyState,
        renderCompanyRoleSwitchOptions,
        renderCompanySettings,
        renderSubscription
    });
});
