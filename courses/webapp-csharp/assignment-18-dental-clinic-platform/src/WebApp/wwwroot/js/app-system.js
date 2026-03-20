(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerSystemModule(app) {
    const {
        state,
        elements,
        requireSystemRole,
        withBusy,
        apiRequest,
        showToast,
        log,
        hasSystemRole,
        clearElement,
        createCell,
        formatDateTime,
        formatMoney
    } = app;

    function renderEmptyStateRow(body, colSpan, message) {
        if (!body) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = colSpan;
        content.className = "empty-state";
        content.textContent = message;
        cell.appendChild(content);
        row.appendChild(cell);
        body.appendChild(row);
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
            renderEmptyStateRow(elements.featureFlagsBody, 2, "No feature flags loaded.");
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
            renderEmptyStateRow(elements.supportCompaniesBody, 5, "No snapshot data loaded.");
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
            renderEmptyStateRow(elements.supportTicketsBody, 5, "No support tickets loaded.");
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
            renderEmptyStateRow(elements.billingSubscriptionsBody, 6, "No subscriptions loaded.");
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
            renderEmptyStateRow(elements.billingInvoicesBody, 7, "No invoices loaded.");
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

    Object.assign(app, {
        onFeatureFlagSubmit,
        onCompanyActivationSubmit,
        onSupportTicketSubmit,
        onBillingSubscriptionSubmit,
        onBillingInvoiceStatusSubmit,
        refreshPlatformData,
        refreshPlatformAnalytics,
        refreshFeatureFlags,
        refreshSupportData,
        refreshSupportCompanies,
        refreshSupportTickets,
        refreshBillingData,
        refreshBillingSubscriptions,
        refreshBillingInvoices,
        renderPlatformAnalytics,
        renderFeatureFlags,
        renderSupportCompanies,
        renderSupportTickets,
        renderBillingSubscriptions,
        renderBillingInvoices
    });
});
