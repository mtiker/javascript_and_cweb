(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerFinanceModule(app) {
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
        clearElement,
        createCell,
        createContentCell,
        createEntityLinkButton,
        formatConditionLabel,
        formatDateTime,
        formatDateTimeLocalInput,
        formatMoney,
        focusElementIfPossible,
        permanentToothNumbers,
        renderLegalEstimateOutput,
        renderPlanItemsSkeleton,
        resolvePatientName,
        setBadgeVariant,
        setSelectOptions,
        toothConditionOptions,
        toUtcIso
    } = app;

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
            treatmentPlanId: form.treatmentPlanId.value,
            patientInsurancePolicyId: optional(form.patientInsurancePolicyId.value),
            estimateNumber: form.estimateNumber.value.trim(),
            formatCode: form.formatCode.value.trim().toUpperCase()
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
            await refreshFinanceWorkspace({ patientId: payload.patientId, silentToast: true, trigger: form });
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

    async function onFinancePatientChange() {
        const patientId = elements.financePatientSelect?.value || "";
        state.financePatientId = patientId;
        state.financeSelectedInvoiceId = "";

        if (elements.estimatePatientSelect instanceof HTMLSelectElement) {
            elements.estimatePatientSelect.value = patientId;
        }

        await refreshFinanceWorkspace({ patientId, silentToast: true, trigger: elements.financePatientSelect });
    }

    async function onFinancePlanSubmit() {
        requireTenant();

        const planId = elements.financePlanSelect?.value || "";
        if (!planId) {
            throw new Error("Select a treatment plan first.");
        }

        await withBusy(elements.financePlanSubmitButton || elements.financePlanSelect, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/treatmentplans/${planId}/submit`, {
                method: "POST",
                body: {},
                auth: true,
                tag: "TREATMENT-PLAN/SUBMIT"
            });

            showToast("Treatment plan submitted.", "success");
            await refreshTreatmentPlans({ silentErrors: true, trigger: elements.financePlanSubmitButton });
            await refreshOpenPlanItems({ silentToast: true, trigger: elements.financePlanSubmitButton });
            await refreshFinanceWorkspace({ patientId: state.financePatientId, silentToast: true, trigger: elements.financePlanSubmitButton });
        });
    }

    async function onFinancePolicySubmit(event) {
        event.preventDefault();
        requireTenant();

        const patientId = state.financePatientId || elements.financePatientSelect?.value || "";
        if (!patientId) {
            throw new Error("Select a patient context first.");
        }

        const form = event.currentTarget;
        const policyId = form.policyId.value || "";
        const payload = {
            patientId,
            insurancePlanId: form.insurancePlanId.value,
            policyNumber: form.policyNumber.value.trim(),
            memberNumber: optional(form.memberNumber.value),
            groupNumber: optional(form.groupNumber.value),
            coverageStart: form.coverageStart.value,
            coverageEnd: optional(form.coverageEnd.value),
            annualMaximum: Number(form.annualMaximum.value),
            deductible: Number(form.deductible.value),
            coveragePercent: Number(form.coveragePercent.value),
            status: form.status.value
        };

        await withBusy(form, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/patientinsurancepolicies${policyId ? `/${policyId}` : ""}`, {
                method: policyId ? "PUT" : "POST",
                body: payload,
                auth: true,
                tag: policyId ? "INSURANCE-POLICY/UPDATE" : "INSURANCE-POLICY/CREATE"
            });

            showToast(policyId ? "Patient insurance policy updated." : "Patient insurance policy created.", "success");
            resetFinancePolicyForm();
            await refreshFinanceWorkspace({ patientId, silentToast: true, trigger: form });
        });
    }

    async function onFinanceInvoiceGenerateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const patientId = state.financePatientId || elements.financePatientSelect?.value || "";
        if (!patientId) {
            throw new Error("Select a patient context first.");
        }

        const selectedTreatmentIds = getSelectedFinanceProcedureIds();
        if (selectedTreatmentIds.length === 0) {
            throw new Error("Select at least one performed procedure first.");
        }

        const form = event.currentTarget;
        const payload = {
            patientId,
            costEstimateId: optional(form.costEstimateId.value),
            invoiceNumber: form.invoiceNumber.value.trim(),
            dueDateUtc: toUtcIsoFromDate(form.dueDate.value),
            treatmentIds: selectedTreatmentIds
        };

        await withBusy(form, async () => {
            const invoice = await apiRequest(`/api/v1/${state.companySlug}/invoices/generate-from-procedures`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "FINANCE/INVOICE-GENERATE"
            });

            showToast("Invoice generated from procedures.", "success");
            state.financeSelectedInvoiceId = invoice.id || "";
            await refreshFinanceWorkspace({ patientId, silentToast: true, trigger: form });
            if (state.financeSelectedInvoiceId) {
                await refreshFinanceInvoiceDetail({ invoiceId: state.financeSelectedInvoiceId, silentToast: true, trigger: form });
            }
        });
    }

    async function onFinancePaymentSubmit(event) {
        event.preventDefault();
        requireTenant();

        const invoiceId = state.financeSelectedInvoiceId;
        if (!invoiceId) {
            throw new Error("Select an invoice first.");
        }

        const form = event.currentTarget;
        const payload = {
            amount: Number(form.amount.value),
            paidAtUtc: toUtcIso(form.paidAtLocal.value),
            method: form.method.value.trim(),
            reference: optional(form.reference.value),
            notes: optional(form.notes.value)
        };

        await withBusy(form, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/invoices/${invoiceId}/payments`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "FINANCE/PAYMENT-CREATE"
            });

            showToast("Payment posted.", "success");
            await refreshFinanceWorkspace({ patientId: state.financePatientId, silentToast: true, trigger: form });
            await refreshFinanceInvoiceDetail({ invoiceId, silentToast: true, trigger: form });
        });
    }

    async function onFinancePaymentPlanSubmit(event) {
        event.preventDefault();
        requireTenant();

        const invoiceId = state.financeSelectedInvoiceId;
        if (!invoiceId) {
            throw new Error("Select an invoice first.");
        }

        const form = event.currentTarget;
        const installments = getFinanceInstallmentRows().map((row) => {
            const dueDate = row.querySelector('[data-installment-field="dueDate"]')?.value || "";
            const amount = row.querySelector('[data-installment-field="amount"]')?.value || "";
            if (!dueDate || !amount) {
                throw new Error("Complete due date and amount for every installment.");
            }

            return {
                dueDateUtc: toUtcIsoFromDate(dueDate),
                amount: Number(amount)
            };
        });

        const payload = {
            invoiceId,
            startsAtUtc: toUtcIsoFromDate(form.startsAt.value),
            terms: form.terms.value.trim(),
            installments
        };

        const existingPlanId = state.financeInvoiceDetail?.paymentPlan?.id || "";

        await withBusy(form, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/paymentplans${existingPlanId ? `/${existingPlanId}` : ""}`, {
                method: existingPlanId ? "PUT" : "POST",
                body: payload,
                auth: true,
                tag: existingPlanId ? "FINANCE/PAYMENT-PLAN-UPDATE" : "FINANCE/PAYMENT-PLAN-CREATE"
            });

            showToast(existingPlanId ? "Payment plan updated." : "Payment plan created.", "success");
            await refreshFinanceWorkspace({ patientId: state.financePatientId, silentToast: true, trigger: form });
            await refreshFinanceInvoiceDetail({ invoiceId, silentToast: true, trigger: form });
        });
    }

    async function refreshTreatmentPlans(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshFinanceButton || elements.refreshPlanItemsButton,
            silentErrors = false
        } = options;

        try {
            const plans = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/treatmentplans`, {
                    method: "GET",
                    auth: true,
                    tag: "TREATMENT-PLAN/LIST"
                });

                return Array.isArray(data) ? data : [];
            });

            state.treatmentPlans = plans;
            syncFinancePatientSelect();
            renderAppointmentClinicalSelectOptions();
        } catch (error) {
            if (silentErrors) {
                state.treatmentPlans = [];
                syncFinancePatientSelect();
                renderAppointmentClinicalSelectOptions();
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
    async function refreshFinanceWorkspace(options = {}) {
        requireTenant();

        const patientId = options.patientId || state.financePatientId || elements.financePatientSelect?.value || "";
        if (!patientId) {
            state.financeWorkspace = null;
            renderFinanceWorkspace(null);
            return;
        }

        const {
            trigger = elements.refreshFinanceButton || elements.refreshPlanItemsButton,
            silentToast = false,
            silentErrors = false
        } = options;

        try {
            const workspace = await withBusy(trigger, async () => {
                return await apiRequest(`/api/v1/${state.companySlug}/finance/workspace/${patientId}`, {
                    method: "GET",
                    auth: true,
                    tag: "FINANCE/WORKSPACE"
                });
            });

            state.financePatientId = patientId;
            state.financeWorkspace = workspace;
            renderFinanceWorkspace(workspace);

            if (state.financeSelectedInvoiceId) {
                const invoiceStillVisible = (workspace?.invoices || []).some((invoice) => invoice.id === state.financeSelectedInvoiceId);
                if (invoiceStillVisible) {
                    await refreshFinanceInvoiceDetail({ invoiceId: state.financeSelectedInvoiceId, silentToast: true, trigger });
                } else {
                    state.financeSelectedInvoiceId = "";
                    renderFinanceInvoiceDetail(null);
                }
            }

            if (!silentToast) {
                showToast("Finance workspace loaded.", "info");
            }
        } catch (error) {
            if (silentErrors) {
                state.financeWorkspace = null;
                renderFinanceWorkspace(null);
                return;
            }

            throw error;
        }
    }

    async function refreshFinanceInvoiceDetail(options = {}) {
        requireTenant();

        const invoiceId = options.invoiceId || state.financeSelectedInvoiceId;
        if (!invoiceId) {
            renderFinanceInvoiceDetail(null);
            return;
        }

        const trigger = options.trigger || elements.refreshFinanceButton || elements.financeInvoicesBody;
        const detail = await withBusy(trigger, async () => {
            return await apiRequest(`/api/v1/${state.companySlug}/invoices/${invoiceId}`, {
                method: "GET",
                auth: true,
                tag: "FINANCE/INVOICE-DETAIL"
            });
        });

        state.financeSelectedInvoiceId = invoiceId;
        state.financeInvoiceDetail = detail;
        renderFinanceInvoiceDetail(detail);

        if (!options.silentToast) {
            showToast("Invoice detail loaded.", "info");
        }
    }

    function renderOpenPlanItems(items) {
        if (!elements.planItemsBody) {
            return;
        }

        clearElement(elements.planItemsBody);
        updatePlanItemSelection(items);

        if (!Array.isArray(items) || items.length === 0) {
            renderTableEmptyState(elements.planItemsBody, 6, "No pending treatment-plan items right now.");
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

    function renderTableEmptyState(body, colSpan, message) {
        if (!body) {
            return;
        }

        clearElement(body);

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

    function getStatusBadgeVariant(status) {
        const normalized = String(status || "").trim().toLowerCase();
        if (["accepted", "approved", "active", "paid", "completed"].includes(normalized)) {
            return "success";
        }
        if (["draft", "pending", "prepared", "issued", "deferred", "partiallyaccepted", "scheduled", "sent"].includes(normalized)) {
            return "warning";
        }
        if (["cancelled", "rejected", "expired", "overdue", "defaulted", "archived"].includes(normalized)) {
            return "danger";
        }
        return "neutral";
    }

    function createStatusBadge(status) {
        const badge = document.createElement("span");
        badge.className = "badge";
        badge.textContent = status || "-";
        setBadgeVariant(badge, getStatusBadgeVariant(status));
        return badge;
    }

    function formatDateOnlyValue(value) {
        if (!value) {
            return "-";
        }

        const raw = String(value);
        return raw.length >= 10 ? raw.slice(0, 10) : raw;
    }

    function toDateInputValue(value) {
        const formatted = formatDateOnlyValue(value);
        return formatted === "-" ? "" : formatted;
    }

    function syncFinancePatientSelect() {
        const patientMap = new Map();

        state.patients.forEach((patient) => {
            if (!patient?.id) {
                return;
            }

            patientMap.set(patient.id, {
                value: patient.id,
                label: `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "Unnamed patient"
            });
        });

        const workspacePatient = state.financeWorkspace?.patient;
        if (workspacePatient?.id && !patientMap.has(workspacePatient.id)) {
            patientMap.set(workspacePatient.id, {
                value: workspacePatient.id,
                label: `${workspacePatient.firstName ?? ""} ${workspacePatient.lastName ?? ""}`.trim() || "Unnamed patient"
            });
        }

        const options = Array.from(patientMap.values())
            .sort((left, right) => left.label.localeCompare(right.label));

        setSelectOptions(elements.financePatientSelect, options, "Select patient");

        const selectedPatientId = state.financePatientId || workspacePatient?.id || "";
        if (elements.financePatientSelect instanceof HTMLSelectElement) {
            const hasSelectedPatient = options.some((option) => option.value === selectedPatientId);
            elements.financePatientSelect.value = hasSelectedPatient ? selectedPatientId : "";
        }
    }

    function renderFinanceWorkspace(workspace) {
        state.financeWorkspace = workspace || null;
        syncFinancePatientSelect();

        if (!workspace?.patient) {
            state.financeSelectedInvoiceId = "";
            state.financeInvoiceDetail = null;

            if (elements.financeSummary) {
                elements.financeSummary.textContent = "Choose a patient to review plan, insurance, estimate, procedures, invoices, payments, and payment plans in one workflow.";
            }

            setSelectOptions(elements.financePlanSelect, [], "Select plan");
            setSelectOptions(elements.financePolicyInsurancePlanSelect, [], "Select insurance plan");

            if (elements.costEstimateForm instanceof HTMLFormElement) {
                elements.costEstimateForm.patientId.value = state.financePatientId || "";
                setSelectOptions(elements.costEstimateForm.treatmentPlanId, [], "Select plan");
                setSelectOptions(elements.costEstimateForm.patientInsurancePolicyId, [], "No patient policy");
            }

            if (elements.legalEstimateForm instanceof HTMLFormElement) {
                setSelectOptions(elements.legalEstimateForm.costEstimateId, [], "Select estimate");
            }

            setSelectOptions(elements.financeInvoiceEstimateSelect, [], "No estimate");
            renderFinancePlanReview();
            renderFinancePolicies([]);
            renderFinanceEstimates([]);
            renderFinanceProcedures([]);
            renderFinanceInvoices([]);
            renderFinanceInvoiceDetail(null);
            resetFinancePolicyForm();
            return;
        }

        const patientName = `${workspace.patient.firstName ?? ""} ${workspace.patient.lastName ?? ""}`.trim() || "Unnamed patient";
        const plans = Array.isArray(workspace.plans) ? workspace.plans : [];
        const policies = Array.isArray(workspace.policies) ? workspace.policies : [];
        const estimates = Array.isArray(workspace.estimates) ? workspace.estimates : [];
        const procedures = Array.isArray(workspace.procedures) ? workspace.procedures : [];
        const invoices = Array.isArray(workspace.invoices) ? workspace.invoices : [];
        const insurancePlans = Array.isArray(workspace.insurancePlans) ? workspace.insurancePlans : [];

        if (elements.financeSummary) {
            elements.financeSummary.textContent = `${patientName}: ${plans.length} plan(s), ${policies.length} policy/policies, ${estimates.length} estimate(s), ${procedures.length} procedure(s), ${invoices.length} invoice(s).`;
        }

        setSelectOptions(
            elements.financePlanSelect,
            plans.map((plan) => ({
                value: plan.id,
                label: `${plan.status || "Draft"}${plan.submittedAtUtc ? ` - ${formatDateTime(plan.submittedAtUtc)}` : ""}`
            })),
            "Select plan"
        );
        if (elements.financePlanSelect instanceof HTMLSelectElement && !elements.financePlanSelect.value && plans.length > 0) {
            elements.financePlanSelect.value = plans[0].id;
        }

        setSelectOptions(
            elements.financePolicyInsurancePlanSelect,
            insurancePlans.map((plan) => ({
                value: plan.id,
                label: `${plan.name || "Insurance plan"}${plan.countryCode ? ` - ${plan.countryCode}` : ""}`
            })),
            "Select insurance plan"
        );

        if (elements.costEstimateForm instanceof HTMLFormElement) {
            elements.costEstimateForm.patientId.value = workspace.patient.id || "";
            setSelectOptions(
                elements.costEstimateForm.treatmentPlanId,
                plans.map((plan) => ({
                    value: plan.id,
                    label: `${plan.status || "Draft"}${plan.submittedAtUtc ? ` - ${formatDateTime(plan.submittedAtUtc)}` : ""}`
                })),
                "Select plan"
            );
            setSelectOptions(
                elements.costEstimateForm.patientInsurancePolicyId,
                policies.map((policy) => ({
                    value: policy.id,
                    label: `${policy.policyNumber || "Policy"} - ${policy.insurancePlanName || "Plan"}`
                })),
                "No patient policy"
            );
        }

        if (elements.legalEstimateForm instanceof HTMLFormElement) {
            setSelectOptions(
                elements.legalEstimateForm.costEstimateId,
                estimates.map((estimate) => ({
                    value: estimate.id,
                    label: `${estimate.estimateNumber || "Estimate"} - ${estimate.status || "Draft"}`
                })),
                "Select estimate"
            );
        }

        setSelectOptions(
            elements.financeInvoiceEstimateSelect,
            estimates.map((estimate) => ({
                value: estimate.id,
                label: `${estimate.estimateNumber || "Estimate"} - ${estimate.status || "Draft"}`
            })),
            "No estimate"
        );

        if (elements.financeInvoiceGenerateForm instanceof HTMLFormElement && !elements.financeInvoiceGenerateForm.dueDate.value) {
            const suggestedDueDate = new Date();
            suggestedDueDate.setDate(suggestedDueDate.getDate() + 14);
            elements.financeInvoiceGenerateForm.dueDate.value = toDateInputValue(suggestedDueDate);
        }

        const editingPolicyId = elements.financePolicyForm instanceof HTMLFormElement
            ? elements.financePolicyForm.policyId.value
            : "";
        if (editingPolicyId && !policies.some((policy) => policy.id === editingPolicyId)) {
            resetFinancePolicyForm();
        }

        renderFinancePlanReview();
        renderFinancePolicies(policies);
        renderFinanceEstimates(estimates);
        renderFinanceProcedures(procedures);
        renderFinanceInvoices(invoices);

        if (!invoices.some((invoice) => invoice.id === state.financeSelectedInvoiceId)) {
            state.financeSelectedInvoiceId = "";
            state.financeInvoiceDetail = null;
            renderFinanceInvoiceDetail(null);
        }
    }

    function renderFinancePlanReview() {
        const plans = state.financeWorkspace?.plans || [];
        const selectedPlanId = elements.financePlanSelect?.value || "";
        const plan = plans.find((item) => item.id === selectedPlanId) || null;
        const canManageFinance = hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager");

        if (elements.financePlanStatus) {
            elements.financePlanStatus.textContent = plan?.status || "No plan selected";
            setBadgeVariant(elements.financePlanStatus, getStatusBadgeVariant(plan?.status));
        }

        if (elements.financePlanSubmitButton instanceof HTMLButtonElement) {
            elements.financePlanSubmitButton.textContent = plan?.submittedAtUtc ? "Plan submitted" : "Submit selected plan";
            elements.financePlanSubmitButton.disabled = !plan || !canManageFinance || Boolean(plan.submittedAtUtc);
        }

        if (elements.financePlanSelect instanceof HTMLSelectElement) {
            elements.financePlanSelect.disabled = !canManageFinance || plans.length === 0;
        }

        if (elements.costEstimateForm instanceof HTMLFormElement && plan?.id) {
            elements.costEstimateForm.treatmentPlanId.value = plan.id;
        }

        if (!plan) {
            renderTableEmptyState(elements.financePlanItemsBody, 5, "Select a patient plan to inspect its items.");
            return;
        }

        if (!elements.financePlanItemsBody) {
            return;
        }

        clearElement(elements.financePlanItemsBody);

        const items = Array.isArray(plan.items) ? plan.items : [];
        if (items.length === 0) {
            renderTableEmptyState(elements.financePlanItemsBody, 5, "This treatment plan has no items.");
            return;
        }

        items.forEach((item) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(String(item.sequence ?? "-")));
            row.appendChild(createCell(item.treatmentTypeName || "-"));
            row.appendChild(createCell(item.urgency || "-"));
            row.appendChild(createCell(formatMoney(item.estimatedPrice)));
            row.appendChild(createContentCell(createStatusBadge(item.decision || "Pending")));
            elements.financePlanItemsBody.appendChild(row);
        });
    }

    function fillFinancePolicyForm(policy) {
        if (!(elements.financePolicyForm instanceof HTMLFormElement) || !policy) {
            return;
        }

        elements.financePolicyForm.policyId.value = policy.id || "";
        elements.financePolicyForm.insurancePlanId.value = policy.insurancePlanId || "";
        elements.financePolicyForm.policyNumber.value = policy.policyNumber || "";
        elements.financePolicyForm.memberNumber.value = policy.memberNumber || "";
        elements.financePolicyForm.groupNumber.value = policy.groupNumber || "";
        elements.financePolicyForm.coverageStart.value = toDateInputValue(policy.coverageStart);
        elements.financePolicyForm.coverageEnd.value = toDateInputValue(policy.coverageEnd);
        elements.financePolicyForm.annualMaximum.value = String(Number(policy.annualMaximum || 0));
        elements.financePolicyForm.deductible.value = String(Number(policy.deductible || 0));
        elements.financePolicyForm.coveragePercent.value = String(Number(policy.coveragePercent || 0));
        elements.financePolicyForm.status.value = policy.status || "Active";

        focusElementIfPossible(elements.financePolicyForm.policyNumber);
    }

    function resetFinancePolicyForm() {
        if (!(elements.financePolicyForm instanceof HTMLFormElement)) {
            return;
        }

        elements.financePolicyForm.reset();
        elements.financePolicyForm.policyId.value = "";
        elements.financePolicyForm.coverageStart.value = toDateInputValue(new Date());
        elements.financePolicyForm.coverageEnd.value = "";
        elements.financePolicyForm.annualMaximum.value = "0";
        elements.financePolicyForm.deductible.value = "0";
        elements.financePolicyForm.coveragePercent.value = "0";
        elements.financePolicyForm.status.value = "Active";
    }
    function renderFinancePolicies(policies) {
        if (!elements.financePoliciesBody) {
            return;
        }

        if (!Array.isArray(policies) || policies.length === 0) {
            renderTableEmptyState(elements.financePoliciesBody, 5, "No policies loaded for this patient.");
            return;
        }

        clearElement(elements.financePoliciesBody);

        policies.forEach((policy) => {
            const row = document.createElement("tr");
            const coverageLabel = `${formatDateOnlyValue(policy.coverageStart)}${policy.coverageEnd ? ` -> ${formatDateOnlyValue(policy.coverageEnd)}` : ""} | ${formatMoney(policy.coveragePercent)}% / max ${formatMoney(policy.annualMaximum)}`;
            const policyButton = createEntityLinkButton(
                policy.policyNumber || "Policy",
                [policy.memberNumber, policy.groupNumber].filter(Boolean).join(" | ") || "Open policy details",
                () => {
                    fillFinancePolicyForm(policy);
                }
            );

            row.appendChild(createContentCell(policyButton));
            row.appendChild(createCell(policy.insurancePlanName || "-"));
            row.appendChild(createCell(coverageLabel));
            row.appendChild(createCell(formatMoney(policy.deductible)));
            row.appendChild(createContentCell(createStatusBadge(policy.status)));
            elements.financePoliciesBody.appendChild(row);
        });
    }

    function renderFinanceEstimates(estimates) {
        if (!elements.financeEstimatesBody) {
            return;
        }

        if (!Array.isArray(estimates) || estimates.length === 0) {
            renderTableEmptyState(elements.financeEstimatesBody, 5, "No estimates loaded for this patient.");
            return;
        }

        clearElement(elements.financeEstimatesBody);

        estimates.forEach((estimate) => {
            const row = document.createElement("tr");
            const estimateButton = createEntityLinkButton(
                estimate.estimateNumber || "Estimate",
                `${estimate.formatCode || "STD"} | ${formatDateTime(estimate.generatedAtUtc)}`,
                () => {
                    if (elements.legalEstimateForm instanceof HTMLFormElement) {
                        elements.legalEstimateForm.costEstimateId.value = estimate.id || "";
                    }
                    if (elements.financeInvoiceEstimateSelect instanceof HTMLSelectElement) {
                        elements.financeInvoiceEstimateSelect.value = estimate.id || "";
                    }
                }
            );

            row.appendChild(createContentCell(estimateButton));
            row.appendChild(createCell(formatMoney(estimate.totalEstimatedAmount)));
            row.appendChild(createCell(formatMoney(estimate.coverageAmount)));
            row.appendChild(createCell(formatMoney(estimate.patientEstimatedAmount)));
            row.appendChild(createContentCell(createStatusBadge(estimate.status)));
            elements.financeEstimatesBody.appendChild(row);
        });
    }

    function renderFinanceProcedures(procedures) {
        if (!elements.financeProceduresBody) {
            return;
        }

        const selectedIds = new Set(getSelectedFinanceProcedureIds());
        const planItemLookup = new Map();

        (state.financeWorkspace?.plans || []).forEach((plan) => {
            (Array.isArray(plan.items) ? plan.items : []).forEach((item) => {
                if (item?.id) {
                    planItemLookup.set(item.id, item);
                }
            });
        });

        if (!Array.isArray(procedures) || procedures.length === 0) {
            renderTableEmptyState(elements.financeProceduresBody, 6, "No performed procedures loaded for this patient.");
            return;
        }

        clearElement(elements.financeProceduresBody);

        procedures.forEach((procedure) => {
            const row = document.createElement("tr");

            const selectCell = document.createElement("td");
            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.value = procedure.id || "";
            checkbox.checked = selectedIds.has(procedure.id);
            checkbox.setAttribute("data-finance-procedure-select", "true");
            checkbox.setAttribute("aria-label", `Select procedure ${procedure.treatmentTypeName || procedure.id}`);
            selectCell.appendChild(checkbox);
            row.appendChild(selectCell);

            row.appendChild(createCell(formatDateTime(procedure.performedAtUtc)));
            row.appendChild(createCell(procedure.treatmentTypeName || "-"));
            row.appendChild(createCell(procedure.toothNumber ? String(procedure.toothNumber) : "-"));
            row.appendChild(createCell(formatMoney(procedure.price)));

            const linkedPlanItem = procedure.planItemId ? planItemLookup.get(procedure.planItemId) : null;
            const planItemLabel = linkedPlanItem
                ? `#${linkedPlanItem.sequence ?? "-"} - ${linkedPlanItem.decision || "Pending"}`
                : "-";
            row.appendChild(createCell(planItemLabel));

            elements.financeProceduresBody.appendChild(row);
        });
    }

    function getSelectedFinanceProcedureIds() {
        return Array.from(elements.financeProceduresBody?.querySelectorAll('input[data-finance-procedure-select="true"]:checked') ?? [])
            .map((input) => input instanceof HTMLInputElement ? input.value : "")
            .filter(Boolean);
    }

    function renderFinanceInvoices(invoices) {
        if (!elements.financeInvoicesBody) {
            return;
        }

        if (!Array.isArray(invoices) || invoices.length === 0) {
            renderTableEmptyState(elements.financeInvoicesBody, 6, "No invoices loaded for this patient.");
            return;
        }

        clearElement(elements.financeInvoicesBody);

        invoices.forEach((invoice) => {
            const row = document.createElement("tr");
            const invoiceButton = createEntityLinkButton(
                invoice.invoiceNumber || "Invoice",
                `Due ${formatDateTime(invoice.dueDateUtc)}`,
                () => {
                    void refreshFinanceInvoiceDetail({
                        invoiceId: invoice.id,
                        silentToast: true,
                        trigger: elements.financeInvoicesBody
                    });
                }
            );

            row.appendChild(createContentCell(invoiceButton));
            row.appendChild(createCell(formatMoney(invoice.totalAmount)));
            row.appendChild(createCell(formatMoney(invoice.patientResponsibilityAmount)));
            row.appendChild(createCell(formatMoney(invoice.paidAmount)));
            row.appendChild(createCell(formatMoney(invoice.balanceAmount)));
            row.appendChild(createContentCell(createStatusBadge(invoice.status)));
            elements.financeInvoicesBody.appendChild(row);
        });
    }

    function renderFinanceInvoiceDetail(detail) {
        state.financeInvoiceDetail = detail || null;

        if (!detail) {
            if (elements.financeInvoiceDetailSummary) {
                elements.financeInvoiceDetailSummary.textContent = "Pick an invoice row to inspect lines, payments, and payment plan installments.";
            }

            renderTableEmptyState(elements.financeInvoiceLinesBody, 6, "Select an invoice to inspect its lines.");
            renderTableEmptyState(elements.financePaymentsBody, 4, "Select an invoice to inspect its payments.");
            renderTableEmptyState(elements.financeInstallmentsBody, 4, "Select an invoice to inspect its payment plan.");

            if (elements.financePaymentForm instanceof HTMLFormElement) {
                elements.financePaymentForm.reset();
                elements.financePaymentForm.paidAtLocal.value = formatDateTimeLocalInput(new Date());
            }

            resetFinancePaymentPlanForm();
            return;
        }

        if (elements.financeInvoiceDetailSummary) {
            elements.financeInvoiceDetailSummary.textContent = `${detail.invoiceNumber || "Invoice"} | total ${formatMoney(detail.totalAmount)} | paid ${formatMoney(detail.paidAmount)} | balance ${formatMoney(detail.balanceAmount)} | due ${formatDateTime(detail.dueDateUtc)}.`;
        }

        if (elements.financeInvoiceLinesBody) {
            clearElement(elements.financeInvoiceLinesBody);
            if (!Array.isArray(detail.lines) || detail.lines.length === 0) {
                renderTableEmptyState(elements.financeInvoiceLinesBody, 6, "This invoice has no lines.");
            } else {
                detail.lines.forEach((line) => {
                    const row = document.createElement("tr");
                    row.appendChild(createCell(line.description || "-"));
                    row.appendChild(createCell(formatMoney(line.quantity)));
                    row.appendChild(createCell(formatMoney(line.unitPrice)));
                    row.appendChild(createCell(formatMoney(line.lineTotal)));
                    row.appendChild(createCell(formatMoney(line.coverageAmount)));
                    row.appendChild(createCell(formatMoney(line.patientAmount)));
                    elements.financeInvoiceLinesBody.appendChild(row);
                });
            }
        }

        if (elements.financePaymentsBody) {
            clearElement(elements.financePaymentsBody);
            if (!Array.isArray(detail.payments) || detail.payments.length === 0) {
                renderTableEmptyState(elements.financePaymentsBody, 4, "No payments have been posted to this invoice yet.");
            } else {
                detail.payments.forEach((payment) => {
                    const row = document.createElement("tr");
                    row.appendChild(createCell(formatDateTime(payment.paidAtUtc)));
                    row.appendChild(createCell(formatMoney(payment.amount)));
                    row.appendChild(createCell(payment.method || "-"));
                    row.appendChild(createCell(payment.reference || "-"));
                    elements.financePaymentsBody.appendChild(row);
                });
            }
        }

        if (elements.financeInstallmentsBody) {
            clearElement(elements.financeInstallmentsBody);
            const installments = detail.paymentPlan?.installments || [];
            if (installments.length === 0) {
                renderTableEmptyState(elements.financeInstallmentsBody, 4, "No payment plan installments are linked to this invoice.");
            } else {
                installments.forEach((installment) => {
                    const row = document.createElement("tr");
                    row.appendChild(createCell(formatDateOnlyValue(installment.dueDateUtc)));
                    row.appendChild(createCell(formatMoney(installment.amount)));
                    row.appendChild(createContentCell(createStatusBadge(installment.status)));
                    row.appendChild(createCell(formatDateTime(installment.paidAtUtc)));
                    elements.financeInstallmentsBody.appendChild(row);
                });
            }
        }

        if (elements.financePaymentForm instanceof HTMLFormElement) {
            elements.financePaymentForm.amount.value = Number(detail.balanceAmount) > 0 ? formatMoney(detail.balanceAmount) : "";
            elements.financePaymentForm.paidAtLocal.value = formatDateTimeLocalInput(new Date());
            elements.financePaymentForm.reference.value = "";
            elements.financePaymentForm.notes.value = "";
        }

        resetFinancePaymentPlanForm(detail);
    }

    function getFinanceInstallmentRows() {
        return Array.from(elements.financePaymentPlanInstallments?.querySelectorAll(".clinical-entry") ?? []);
    }

    function updateFinanceInstallmentRemoveButtons() {
        const rows = getFinanceInstallmentRows();
        rows.forEach((row) => {
            const removeButton = row.querySelector('[data-installment-action="remove"]');
            if (removeButton instanceof HTMLButtonElement) {
                removeButton.disabled = rows.length <= 1;
            }
        });
    }

    function addFinanceInstallmentRow(initialValues = {}) {
        if (!elements.financePaymentPlanInstallments || !(elements.paymentPlanInstallmentTemplate instanceof HTMLTemplateElement)) {
            return;
        }

        const row = elements.paymentPlanInstallmentTemplate.content.firstElementChild?.cloneNode(true);
        if (!(row instanceof HTMLElement)) {
            return;
        }

        const dueDateInput = row.querySelector('[data-installment-field="dueDate"]');
        const amountInput = row.querySelector('[data-installment-field="amount"]');
        const removeButton = row.querySelector('[data-installment-action="remove"]');

        if (dueDateInput instanceof HTMLInputElement) {
            dueDateInput.value = toDateInputValue(initialValues.dueDate);
        }
        if (amountInput instanceof HTMLInputElement && initialValues.amount !== undefined && initialValues.amount !== null && initialValues.amount !== "") {
            amountInput.value = Number.isFinite(Number(initialValues.amount))
                ? formatMoney(initialValues.amount)
                : String(initialValues.amount);
        }
        if (removeButton instanceof HTMLButtonElement) {
            removeButton.addEventListener("click", () => {
                row.remove();
                if (getFinanceInstallmentRows().length === 0) {
                    addFinanceInstallmentRow();
                }
                updateFinanceInstallmentRemoveButtons();
            });
        }

        elements.financePaymentPlanInstallments.appendChild(row);
        updateFinanceInstallmentRemoveButtons();
    }

    function resetFinancePaymentPlanForm(detail = null) {
        if (!(elements.financePaymentPlanForm instanceof HTMLFormElement)) {
            return;
        }

        elements.financePaymentPlanForm.reset();
        clearElement(elements.financePaymentPlanInstallments);

        const paymentPlan = detail?.paymentPlan || null;
        elements.financePaymentPlanForm.startsAt.value = toDateInputValue(paymentPlan?.startsAtUtc || detail?.dueDateUtc || new Date());
        elements.financePaymentPlanForm.terms.value = paymentPlan?.terms || "";

        if (Array.isArray(paymentPlan?.installments) && paymentPlan.installments.length > 0) {
            paymentPlan.installments.forEach((installment) => {
                addFinanceInstallmentRow({
                    dueDate: installment.dueDateUtc,
                    amount: installment.amount
                });
            });
            return;
        }

        if (detail && Number(detail.balanceAmount) > 0) {
            addFinanceInstallmentRow({
                dueDate: detail.dueDateUtc,
                amount: detail.balanceAmount
            });
            return;
        }

        addFinanceInstallmentRow();
    }
    function getAcceptedDeferredPlanItemsForPatient(patientId) {
        if (!patientId) {
            return [];
        }

        return state.treatmentPlans
            .filter((plan) => plan.patientId === patientId)
            .flatMap((plan) => (Array.isArray(plan.items) ? plan.items : [])
                .filter((item) => item.decision === "Accepted" || item.decision === "Deferred")
                .map((item) => ({
                    ...item,
                    planId: plan.id,
                    planStatus: plan.status,
                    planSubmittedAtUtc: plan.submittedAtUtc
                })))
            .sort((left, right) => {
                if ((left.planSubmittedAtUtc || "") !== (right.planSubmittedAtUtc || "")) {
                    return String(right.planSubmittedAtUtc || "").localeCompare(String(left.planSubmittedAtUtc || ""));
                }
                return Number(left.sequence || 0) - Number(right.sequence || 0);
            });
    }

    function getCurrentAppointmentClinicalPatientId() {
        const appointmentId = elements.appointmentClinicalAppointmentSelect?.value || "";
        const appointment = state.appointments.find((item) => item.id === appointmentId) || null;
        return appointment?.patientId || "";
    }

    function syncAppointmentClinicalRowFromPlanItem(row, planItemId) {
        if (!row || !planItemId) {
            return;
        }

        const patientId = getCurrentAppointmentClinicalPatientId();
        const selectedPlanItem = getAcceptedDeferredPlanItemsForPatient(patientId)
            .find((item) => item.id === planItemId);
        if (!selectedPlanItem) {
            return;
        }

        const treatmentTypeSelect = row.querySelector('[data-clinical-field="treatmentTypeId"]');
        const priceInput = row.querySelector('[data-clinical-field="price"]');

        if (treatmentTypeSelect instanceof HTMLSelectElement) {
            treatmentTypeSelect.value = selectedPlanItem.treatmentTypeId || "";
        }
        if (priceInput instanceof HTMLInputElement && !priceInput.value) {
            priceInput.value = String(selectedPlanItem.estimatedPrice ?? "");
        }
    }

    function renderAppointmentClinicalSelectOptions() {
        setSelectOptions(
            elements.appointmentClinicalAppointmentSelect,
            state.appointments.map((appointment) => ({
                value: appointment.id,
                label: `${resolvePatientName(appointment.patientId)} - ${formatDateTime(appointment.startAtUtc)} - ${appointment.status || "Scheduled"}`
            })),
            "Select appointment"
        );

        const rows = Array.from(elements.appointmentClinicalItems?.querySelectorAll(".clinical-entry") ?? []);
        rows.forEach((row) => {
            populateAppointmentClinicalRow(row);
        });
        updateAppointmentClinicalRemoveButtons();
    }

    function resetAppointmentClinicalForm(options = {}) {
        if (!(elements.appointmentClinicalForm instanceof HTMLFormElement)) {
            return;
        }

        elements.appointmentClinicalForm.reset();

        if (elements.appointmentClinicalItems) {
            clearElement(elements.appointmentClinicalItems);
        }

        addAppointmentClinicalEntryRow();

        if (elements.appointmentClinicalPerformedAtInput) {
            elements.appointmentClinicalPerformedAtInput.value = formatDateTimeLocalInput(new Date());
        }

        if (elements.appointmentClinicalMarkCompleted) {
            elements.appointmentClinicalMarkCompleted.checked = true;
        }

        if (elements.appointmentClinicalAppointmentSelect) {
            elements.appointmentClinicalAppointmentSelect.value = options.appointmentId || "";
        }

        renderAppointmentClinicalSelectOptions();
    }

    function addAppointmentClinicalEntryRow(initialValues = {}) {
        if (!elements.appointmentClinicalItems || !(elements.appointmentClinicalItemTemplate instanceof HTMLTemplateElement)) {
            return;
        }

        const row = elements.appointmentClinicalItemTemplate.content.firstElementChild?.cloneNode(true);
        if (!(row instanceof HTMLElement)) {
            return;
        }

        const removeButton = row.querySelector('[data-clinical-action="remove"]');
        if (removeButton instanceof HTMLButtonElement) {
            removeButton.addEventListener("click", () => {
                row.remove();
                if ((elements.appointmentClinicalItems?.children.length || 0) === 0) {
                    addAppointmentClinicalEntryRow();
                }
                updateAppointmentClinicalRemoveButtons();
            });
        }

        const typeSelect = row.querySelector('[data-clinical-field="treatmentTypeId"]');
        const planItemSelect = row.querySelector('[data-clinical-field="planItemId"]');
        const priceInput = row.querySelector('[data-clinical-field="price"]');
        if (typeSelect instanceof HTMLSelectElement && priceInput instanceof HTMLInputElement) {
            typeSelect.addEventListener("change", () => {
                const treatmentType = state.treatmentTypes.find((item) => item.id === typeSelect.value);
                if (treatmentType && !priceInput.value) {
                    priceInput.value = String(treatmentType.basePrice ?? 0);
                }
            });
        }
        if (planItemSelect instanceof HTMLSelectElement) {
            planItemSelect.addEventListener("change", () => {
                syncAppointmentClinicalRowFromPlanItem(row, planItemSelect.value);
            });
        }

        elements.appointmentClinicalItems.appendChild(row);
        populateAppointmentClinicalRow(row, initialValues);
        updateAppointmentClinicalRemoveButtons();
    }

    function populateAppointmentClinicalRow(row, initialValues = {}) {
        const toothSelect = row.querySelector('[data-clinical-field="toothNumber"]');
        const planItemSelect = row.querySelector('[data-clinical-field="planItemId"]');
        const treatmentTypeSelect = row.querySelector('[data-clinical-field="treatmentTypeId"]');
        const conditionSelect = row.querySelector('[data-clinical-field="condition"]');
        const priceInput = row.querySelector('[data-clinical-field="price"]');
        const notesInput = row.querySelector('[data-clinical-field="notes"]');
        const patientId = getCurrentAppointmentClinicalPatientId();
        const planItems = getAcceptedDeferredPlanItemsForPatient(patientId);

        setSelectOptions(
            toothSelect,
            permanentToothNumbers.map((toothNumber) => ({
                value: String(toothNumber),
                label: `Tooth ${toothNumber}`
            })),
            "Select tooth"
        );

        setSelectOptions(
            planItemSelect,
            planItems.map((item) => ({
                value: item.id,
                label: `#${item.sequence ?? "-"} - ${item.treatmentTypeName || "Treatment"} - ${item.decision || "Pending"}`
            })),
            "No linked plan item"
        );

        setSelectOptions(
            treatmentTypeSelect,
            state.treatmentTypes.map((type) => ({
                value: type.id,
                label: `${type.name || "Treatment"}${type.basePrice !== undefined ? ` - ${formatMoney(type.basePrice)}` : ""}`
            })),
            "Select treatment"
        );

        setSelectOptions(
            conditionSelect,
            toothConditionOptions.map((condition) => ({
                value: condition,
                label: formatConditionLabel(condition)
            })),
            "Select status"
        );

        if (toothSelect instanceof HTMLSelectElement) {
            toothSelect.value = initialValues.toothNumber ? String(initialValues.toothNumber) : toothSelect.value;
        }

        if (planItemSelect instanceof HTMLSelectElement) {
            planItemSelect.value = initialValues.planItemId || planItemSelect.value;
        }

        if (treatmentTypeSelect instanceof HTMLSelectElement) {
            treatmentTypeSelect.value = initialValues.treatmentTypeId || treatmentTypeSelect.value;
        }

        if (conditionSelect instanceof HTMLSelectElement) {
            conditionSelect.value = initialValues.condition || conditionSelect.value || toothConditionOptions[0];
        }

        if (priceInput instanceof HTMLInputElement) {
            priceInput.value = initialValues.price ?? "";
        }

        if (notesInput instanceof HTMLTextAreaElement) {
            notesInput.value = initialValues.notes || "";
        }

        if (planItemSelect instanceof HTMLSelectElement && planItemSelect.value) {
            syncAppointmentClinicalRowFromPlanItem(row, planItemSelect.value);
        }
    }

    function updateAppointmentClinicalRemoveButtons() {
        const rows = Array.from(elements.appointmentClinicalItems?.querySelectorAll(".clinical-entry") ?? []);
        rows.forEach((row) => {
            const removeButton = row.querySelector('[data-clinical-action="remove"]');
            if (removeButton instanceof HTMLButtonElement) {
                removeButton.disabled = rows.length <= 1;
            }
        });
    }

    function updatePlanItemSelection(items) {
        setSelectOptions(
            elements.planItemSelection,
            (Array.isArray(items) ? items : []).map((item) => ({
                value: `${item.planId}|${item.planItemId}`,
                label: `${item.patientName || "Unknown patient"} - ${item.treatmentTypeName || "Treatment"} - ${item.urgency || "Urgency"}`
            })),
            "Select plan item"
        );
    }

    function toUtcIsoFromDate(dateValue) {
        if (!dateValue) {
            throw new Error("Invalid date value.");
        }

        const parsed = new Date(`${dateValue}T00:00`);
        if (Number.isNaN(parsed.getTime())) {
            throw new Error("Invalid date value.");
        }

        return parsed.toISOString();
    }

    Object.assign(app, {
        onPlanDecisionSubmit,
        onCostEstimateSubmit,
        onLegalEstimateSubmit,
        onFinancePatientChange,
        onFinancePlanSubmit,
        onFinancePolicySubmit,
        onFinanceInvoiceGenerateSubmit,
        onFinancePaymentSubmit,
        onFinancePaymentPlanSubmit,
        refreshTreatmentPlans,
        refreshCostEstimates,
        refreshOpenPlanItems,
        refreshFinanceWorkspace,
        renderOpenPlanItems,
        syncFinancePatientSelect,
        renderFinanceWorkspace,
        renderFinancePlanReview,
        renderFinanceInvoiceDetail,
        resetFinancePolicyForm,
        addFinanceInstallmentRow,
        resetFinancePaymentPlanForm,
        renderAppointmentClinicalSelectOptions,
        resetAppointmentClinicalForm,
        addAppointmentClinicalEntryRow
    });
});
