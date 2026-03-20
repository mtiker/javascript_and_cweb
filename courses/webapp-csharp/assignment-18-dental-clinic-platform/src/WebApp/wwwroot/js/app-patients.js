(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerPatientsModule(app) {
    const {
        state,
        elements,
        toothChartRows,
        svgNamespace,
        apiRequest,
        canManagePatientsUi,
        clearElement,
        createCell,
        createPatientActionButton,
        focusElementIfPossible,
        formatConditionLabel,
        formatDateTime,
        formatMoney,
        formatTime,
        log,
        optional,
        renderSession,
        reportError,
        requireTenant,
        setSyncStatus,
        setText,
        showToast,
        withBusy
    } = app;

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

    async function onPatientProfileSubmit(event) {
        event.preventDefault();
        requireTenant();

        const patientId = state.patientProfile?.id;
        if (!patientId) {
            throw new Error("Open a patient profile before saving changes.");
        }

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
            await apiRequest(`/api/v1/${state.companySlug}/patients/${patientId}`, {
                method: "PUT",
                body: payload,
                auth: true,
                tag: "PATIENT/UPDATE"
            });

            showToast("Patient profile updated.", "success");
            await refreshPatients({ silentToast: true, silentSyncStatus: true });
            await refreshPatientProfile({ patientId, silentToast: true, trigger: form });
        });
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

    async function refreshPatientProfile(options = {}) {
        requireTenant();

        const patientId = options.patientId || state.patientProfile?.id;
        if (!patientId) {
            return;
        }

        const trigger = options.trigger || elements.patientProfileRefreshButton || elements.refreshPatientsButton;
        const profile = await withBusy(trigger, async () => {
            return await apiRequest(`/api/v1/${state.companySlug}/patients/${patientId}/profile`, {
                method: "GET",
                auth: true,
                tag: "PATIENT/PROFILE"
            });
        });

        renderPatientProfile(profile, {
            selectedToothNumber: state.selectedPatientToothNumber
        });

        if (!options.silentToast) {
            showToast("Patient profile loaded.", "info");
        }
    }

    function openDeleteDialog(patient) {
        const id = String(patient.id || "");
        if (!id) {
            return;
        }

        const fullName = `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "this patient";
        state.pendingDelete = { id, label: fullName };

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
            state.pendingDelete = { id: "", label: "" };
            return;
        }

        try {
            await confirmDelete();
        } catch (error) {
            reportError(error);
        }
    }

    async function confirmDelete() {
        if (!state.pendingDelete?.id) {
            return;
        }

        const patientId = state.pendingDelete.id;
        const patientLabel = state.pendingDelete.label;
        const shouldCloseProfile = state.patientProfile?.id === patientId;
        state.pendingDelete = { id: "", label: "" };

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
        if (shouldCloseProfile) {
            closePatientProfile();
        }
    }

    function syncAppointmentPatientOptions() {
        if (typeof app.renderAppointmentSelectOptions === "function") {
            app.renderAppointmentSelectOptions();
        }
    }

    function renderPatients(patients) {
        if (!elements.patientsBody) {
            return;
        }

        clearElement(elements.patientsBody);

        if (!Array.isArray(patients) || patients.length === 0) {
            renderPatientsEmptyState();
            state.patients = [];
            syncAppointmentPatientOptions();
            return;
        }

        const canManagePatients = canManagePatientsUi();

        patients.forEach((patient) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(`${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "-"));
            row.appendChild(createCell(patient.dateOfBirth ?? "-"));
            row.appendChild(createCell(patient.personalCode ?? "-"));
            row.appendChild(createCell(patient.email ?? "-"));
            row.appendChild(createCell(patient.phone ?? "-"));

            const actionsCell = createCell("");
            actionsCell.classList.add("text-right");
            actionsCell.classList.add("table-actions");
            const viewButton = createPatientActionButton("View", "btn btn--ghost btn--sm", () => {
                void openPatientProfile(patient.id);
            });
            const editButton = createPatientActionButton("Edit", "btn btn--secondary btn--sm", () => {
                void openPatientProfile(patient.id, { focusForm: true });
            });
            const deleteButton = createPatientActionButton("Delete", "btn btn--ghost btn--sm", () => {
                openDeleteDialog(patient);
            });
            viewButton.disabled = !canManagePatients;
            editButton.disabled = !canManagePatients;
            deleteButton.disabled = !canManagePatients;
            deleteButton.setAttribute("aria-label", `Delete patient ${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim());

            actionsCell.appendChild(viewButton);
            actionsCell.appendChild(editButton);
            actionsCell.appendChild(deleteButton);
            row.appendChild(actionsCell);
            elements.patientsBody.appendChild(row);
        });

        state.patients = patients;
        syncAppointmentPatientOptions();

        if (state.patientProfile?.id && !patients.some((patient) => patient.id === state.patientProfile.id)) {
            closePatientProfile();
        }
    }

    function renderPatientProfile(profile, options = {}) {
        state.patientProfile = profile || null;

        if (!elements.patientProfilePanel || !elements.patientsWorkspace) {
            return;
        }

        if (!profile) {
            elements.patientProfilePanel.hidden = true;
            elements.patientsWorkspace.hidden = false;
            state.selectedPatientToothNumber = null;
            setText(elements.patientProfileName, "Select a patient");
            setText(elements.patientProfileMeta, "Open a patient record to see the full tooth chart and treatment history.");
            setText(elements.patientProfileIssueCount, "0");
            setText(elements.patientProfileTreatmentCount, "0");
            setText(elements.patientProfileDob, "-");
            setText(elements.patientProfileCode, "-");
            setText(elements.patientProfileEmail, "-");
            setText(elements.patientProfilePhone, "-");

            if (elements.patientProfileForm) {
                elements.patientProfileForm.reset();
            }

            updatePatientToothHoverCard(null);

            if (elements.patientToothChart) {
                clearElement(elements.patientToothChart);
                const emptyState = document.createElement("div");
                emptyState.className = "empty-state";
                emptyState.textContent = "Select a patient to load a dental chart.";
                elements.patientToothChart.appendChild(emptyState);
            }

            renderSelectedPatientTooth(null);
            return;
        }

        elements.patientsWorkspace.hidden = true;
        elements.patientProfilePanel.hidden = false;

        const teeth = Array.isArray(profile.teeth) ? profile.teeth : [];
        const activeIssueCount = teeth.filter((tooth) => (tooth.condition || "Healthy") !== "Healthy").length;
        const treatmentCount = teeth.reduce((sum, tooth) => sum + (Array.isArray(tooth.history) ? tooth.history.length : 0), 0);

        setText(elements.patientProfileName, `${profile.firstName ?? ""} ${profile.lastName ?? ""}`.trim() || "Unnamed patient");
        setText(
            elements.patientProfileMeta,
            `${activeIssueCount} active issue${activeIssueCount === 1 ? "" : "s"} - ${treatmentCount} recorded treatment${treatmentCount === 1 ? "" : "s"}`
        );
        setText(elements.patientProfileIssueCount, String(activeIssueCount));
        setText(elements.patientProfileTreatmentCount, String(treatmentCount));
        setText(elements.patientProfileDob, profile.dateOfBirth || "-");
        setText(elements.patientProfileCode, profile.personalCode || "-");
        setText(elements.patientProfileEmail, profile.email || "-");
        setText(elements.patientProfilePhone, profile.phone || "-");

        if (elements.patientProfileForm) {
            elements.patientProfileForm.firstName.value = profile.firstName || "";
            elements.patientProfileForm.lastName.value = profile.lastName || "";
            elements.patientProfileForm.dateOfBirth.value = profile.dateOfBirth || "";
            elements.patientProfileForm.personalCode.value = profile.personalCode || "";
            elements.patientProfileForm.email.value = profile.email || "";
            elements.patientProfileForm.phone.value = profile.phone || "";
        }

        const selectedTooth = resolveSelectedPatientTooth(profile, options.selectedToothNumber ?? state.selectedPatientToothNumber);
        state.selectedPatientToothNumber = selectedTooth?.toothNumber ?? null;

        renderPatientToothChart(teeth, state.selectedPatientToothNumber);
        renderSelectedPatientTooth(selectedTooth);
        updatePatientToothHoverCard(selectedTooth);

        if (options.focusForm && elements.patientProfileForm?.firstName instanceof HTMLElement) {
            focusElementIfPossible(elements.patientProfileForm.firstName);
        }
    }

    function renderPatientToothChart(teeth, selectedToothNumber) {
        if (!elements.patientToothChart) {
            return;
        }

        clearElement(elements.patientToothChart);

        const toothMap = new Map((Array.isArray(teeth) ? teeth : []).map((tooth) => [Number(tooth.toothNumber), tooth]));
        const selectedTooth = selectedToothNumber ? toothMap.get(Number(selectedToothNumber)) || null : null;

        toothChartRows.forEach(({ arch, teeth: toothNumbers }) => {
            const archSection = document.createElement("section");
            archSection.className = `tooth-chart__arch tooth-chart__arch--${arch}`;
            archSection.setAttribute("aria-label", arch === "upper" ? "Upper jaw" : "Lower jaw");

            const archLabel = document.createElement("div");
            archLabel.className = "tooth-chart__arch-label";
            archLabel.textContent = arch === "upper" ? "Upper arch" : "Lower arch";
            archSection.appendChild(archLabel);

            const row = document.createElement("div");
            row.className = `tooth-chart__row tooth-chart__row--${arch}`;

            toothNumbers.forEach((toothNumber) => {
                const tooth = toothMap.get(toothNumber) || {
                    toothNumber,
                    condition: "Healthy",
                    notes: null,
                    history: []
                };

                row.appendChild(createPatientToothButton({
                    tooth,
                    arch,
                    selectedTooth,
                    selectedToothNumber,
                    teeth
                }));
            });

            archSection.appendChild(row);
            elements.patientToothChart.appendChild(archSection);
        });
    }

    function createPatientToothButton({ tooth, arch, selectedTooth, selectedToothNumber, teeth }) {
        const tone = getToothConditionTone(tooth.condition);
        const toothNumber = Number(tooth.toothNumber);
        const visual = getToothVisualSpec(toothNumber, arch);
        const previewText = buildToothPreviewText(tooth);

        const button = document.createElement("button");
        button.type = "button";
        button.className = `tooth-button tooth-button--${arch} tooth-button--${tone} tooth-button--${visual.kind}`;
        button.style.setProperty("--tooth-width", `${visual.width}rem`);
        button.style.setProperty("--tooth-height", `${visual.height}rem`);
        if (selectedToothNumber === toothNumber) {
            button.classList.add("is-selected");
        }

        button.title = previewText;
        button.setAttribute("aria-label", `Tooth ${toothNumber}. ${previewText}`);
        button.addEventListener("mouseenter", () => updatePatientToothHoverCard(tooth));
        button.addEventListener("focus", () => updatePatientToothHoverCard(tooth));
        button.addEventListener("mouseleave", () => updatePatientToothHoverCard(selectedTooth));
        button.addEventListener("blur", () => updatePatientToothHoverCard(selectedTooth));
        button.addEventListener("click", () => {
            state.selectedPatientToothNumber = toothNumber;
            renderPatientToothChart(teeth, toothNumber);
            renderSelectedPatientTooth(tooth);
            updatePatientToothHoverCard(tooth);
        });

        const visualWrapper = document.createElement("span");
        visualWrapper.className = "tooth-button__visual";
        visualWrapper.setAttribute("aria-hidden", "true");
        visualWrapper.appendChild(createToothSvg({ toothNumber, arch, kind: visual.kind, tone }));
        button.appendChild(visualWrapper);

        const meta = document.createElement("span");
        meta.className = "tooth-button__meta";

        const number = document.createElement("span");
        number.className = "tooth-button__number";
        number.textContent = String(toothNumber);
        meta.appendChild(number);

        const status = document.createElement("span");
        status.className = "tooth-button__status";
        status.textContent = formatConditionLabel(tooth.condition);
        meta.appendChild(status);

        button.appendChild(meta);
        return button;
    }

    function getToothVisualSpec(toothNumber, arch) {
        const position = Number(String(toothNumber).slice(-1));
        const isUpper = arch === "upper";

        if (position === 1) {
            return {
                kind: "incisor-central",
                width: 2.45,
                height: isUpper ? 5.9 : 7.4
            };
        }

        if (position === 2) {
            return {
                kind: "incisor-lateral",
                width: 2.2,
                height: isUpper ? 5.5 : 7
            };
        }

        if (position === 3) {
            return {
                kind: "canine",
                width: 2.25,
                height: isUpper ? 6.6 : 7.9
            };
        }

        if (position === 4 || position === 5) {
            return {
                kind: "premolar",
                width: position === 4 ? 2.75 : 2.95,
                height: isUpper ? 5.7 : 7.3
            };
        }

        return {
            kind: "molar",
            width: position === 8 ? 3.3 : 3.5,
            height: isUpper ? 5.2 : 7.1
        };
    }

    function createToothSvg({ toothNumber, arch, kind, tone }) {
        const svg = createSvgElement("svg");
        svg.setAttribute("class", `tooth-button__svg tooth-button__svg--${arch}`);
        svg.setAttribute("viewBox", "0 0 100 160");
        svg.setAttribute("role", "presentation");
        svg.setAttribute("focusable", "false");

        const defs = createSvgElement("defs");
        const bodyGradient = createSvgElement("linearGradient");
        const bodyGradientId = `tooth-body-${toothNumber}`;
        bodyGradient.setAttribute("id", bodyGradientId);
        bodyGradient.setAttribute("x1", "18%");
        bodyGradient.setAttribute("y1", "0%");
        bodyGradient.setAttribute("x2", "86%");
        bodyGradient.setAttribute("y2", "100%");
        appendGradientStop(bodyGradient, "0%", tone === "missing" ? "#c7d0da" : "#ffffff");
        appendGradientStop(bodyGradient, "52%", tone === "missing" ? "#aeb8c3" : "#f5f9fd");
        appendGradientStop(bodyGradient, "100%", tone === "missing" ? "#7f8b97" : "#d9e3ec");
        defs.appendChild(bodyGradient);

        const glossGradient = createSvgElement("radialGradient");
        const glossGradientId = `tooth-gloss-${toothNumber}`;
        glossGradient.setAttribute("id", glossGradientId);
        glossGradient.setAttribute("cx", "26%");
        glossGradient.setAttribute("cy", arch === "upper" ? "18%" : "12%");
        glossGradient.setAttribute("r", "68%");
        appendGradientStop(glossGradient, "0%", "#ffffff", 0.92);
        appendGradientStop(glossGradient, "45%", "#ffffff", 0.2);
        appendGradientStop(glossGradient, "100%", "#ffffff", 0);
        defs.appendChild(glossGradient);

        svg.appendChild(defs);

        const silhouettePath = getToothSilhouettePath(kind, arch);
        const groovePath = getToothGroovePath(kind, arch);

        const body = createSvgElement("path");
        body.setAttribute("class", "tooth-button__body");
        body.setAttribute("d", silhouettePath);
        body.setAttribute("fill", `url(#${bodyGradientId})`);
        svg.appendChild(body);

        const gloss = createSvgElement("path");
        gloss.setAttribute("class", "tooth-button__gloss");
        gloss.setAttribute("d", silhouettePath);
        gloss.setAttribute("fill", `url(#${glossGradientId})`);
        svg.appendChild(gloss);

        if (groovePath) {
            const groove = createSvgElement("path");
            groove.setAttribute("class", "tooth-button__groove");
            groove.setAttribute("d", groovePath);
            svg.appendChild(groove);
        }

        if (tone === "missing") {
            const mark = createSvgElement("path");
            mark.setAttribute("class", "tooth-button__missing-mark");
            mark.setAttribute("d", arch === "upper" ? "M18 24 L82 132 M82 24 L18 132" : "M18 20 L82 144 M82 20 L18 144");
            svg.appendChild(mark);
        }

        return svg;
    }

    function appendGradientStop(gradient, offset, color, opacity = 1) {
        const stop = createSvgElement("stop");
        stop.setAttribute("offset", offset);
        stop.setAttribute("stop-color", color);
        if (opacity !== 1) {
            stop.setAttribute("stop-opacity", String(opacity));
        }
        gradient.appendChild(stop);
    }

    function createSvgElement(tagName) {
        return document.createElementNS(svgNamespace, tagName);
    }

    function getToothSilhouettePath(kind, arch) {
        if (arch === "upper") {
            if (kind === "molar") {
                return "M12 118 C10 88 14 56 20 30 C24 14 34 10 42 18 C46 6 54 6 58 18 C66 10 76 14 80 30 C86 56 90 88 88 118 C76 138 24 138 12 118 Z";
            }

            if (kind === "premolar") {
                return "M18 124 C16 94 20 56 26 28 C30 12 40 10 48 20 C54 8 64 12 68 28 C74 56 78 94 76 124 C66 140 28 140 18 124 Z";
            }

            if (kind === "canine") {
                return "M28 134 C20 112 22 78 28 40 C32 18 42 4 50 4 C58 4 68 18 72 40 C78 78 80 112 72 134 C64 142 36 142 28 134 Z";
            }

            if (kind === "incisor-lateral") {
                return "M24 134 C18 116 18 82 22 44 C26 18 34 10 50 10 C66 10 74 18 78 44 C82 82 82 116 76 134 C68 142 32 142 24 134 Z";
            }

            return "M22 136 C16 120 16 84 22 42 C26 16 36 8 50 8 C64 8 74 16 78 42 C84 84 84 120 78 136 C70 144 30 144 22 136 Z";
        }

        if (kind === "molar") {
            return "M10 28 C10 12 22 6 38 10 C46 4 54 4 62 10 C78 6 90 12 90 28 C90 48 82 60 76 72 C70 84 72 104 76 130 C78 144 74 152 68 152 C62 152 58 144 56 134 C52 112 52 92 50 78 C48 92 48 112 44 134 C42 144 38 152 32 152 C26 152 22 144 24 130 C28 104 30 84 24 72 C18 60 10 48 10 28 Z";
        }

        if (kind === "premolar") {
            return "M20 28 C20 12 32 8 50 10 C68 8 80 12 80 28 C80 48 72 62 62 78 C56 88 58 112 60 136 C60 148 56 154 50 154 C44 154 40 148 40 136 C42 112 44 88 38 78 C28 62 20 48 20 28 Z";
        }

        if (kind === "canine") {
            return "M28 24 C30 12 38 6 50 6 C62 6 70 12 72 24 C76 44 70 62 62 80 C56 94 56 118 56 140 C56 150 54 156 50 156 C46 156 44 150 44 140 C44 118 44 94 38 80 C30 62 24 44 28 24 Z";
        }

        if (kind === "incisor-lateral") {
            return "M30 22 C32 12 40 8 50 8 C60 8 68 12 70 22 C72 40 66 58 60 74 C56 90 56 112 54 136 C54 148 52 154 50 154 C48 154 46 148 46 136 C44 112 44 90 40 74 C34 58 28 40 30 22 Z";
        }

        return "M32 22 C34 12 40 8 50 8 C60 8 66 12 68 22 C70 40 64 58 58 74 C54 90 54 114 52 140 C52 150 50 156 50 156 C50 156 48 150 48 140 C46 114 46 90 42 74 C36 58 30 40 32 22 Z";
    }

    function getToothGroovePath(kind, arch) {
        if (arch === "upper") {
            if (kind === "molar") {
                return "M24 36 C32 26 42 22 50 30 C58 22 68 26 76 36 M32 44 C38 54 44 74 46 112 M68 44 C62 54 56 74 54 112";
            }

            if (kind === "premolar") {
                return "M32 34 C38 28 44 26 50 30 C56 26 62 28 68 34 M50 30 C50 54 50 82 50 118";
            }

            return "M50 18 C48 40 48 80 50 126";
        }

        if (kind === "molar") {
            return "M24 30 C32 24 42 20 50 28 C58 20 68 24 76 30 M50 30 C50 60 50 96 50 132";
        }

        if (kind === "premolar") {
            return "M36 26 C40 22 46 20 50 24 C54 20 60 22 64 26 M50 26 C50 56 50 96 50 138";
        }

        return "M50 14 C50 40 50 84 50 142";
    }

    function renderSelectedPatientTooth(tooth) {
        setText(elements.patientSelectedToothTitle, tooth ? `Tooth ${tooth.toothNumber}` : "Tooth history");
        setText(
            elements.patientSelectedToothSummary,
            tooth
                ? `${formatConditionLabel(tooth.condition)} - ${tooth.lastTreatmentAtUtc ? `last treatment ${formatDateTime(tooth.lastTreatmentAtUtc)}` : "no treatment history yet"}`
                : "Select a tooth to inspect status changes and previous treatments."
        );

        if (!elements.patientSelectedToothHistory) {
            return;
        }

        clearElement(elements.patientSelectedToothHistory);

        if (!tooth) {
            const emptyState = document.createElement("div");
            emptyState.className = "empty-state";
            emptyState.textContent = "Select a tooth to inspect its full history.";
            elements.patientSelectedToothHistory.appendChild(emptyState);
            return;
        }

        const dropdown = document.createElement("details");
        dropdown.className = "tooth-history-dropdown";
        dropdown.open = true;

        const summary = document.createElement("summary");
        summary.textContent = `${Array.isArray(tooth.history) ? tooth.history.length : 0} history entr${tooth.history?.length === 1 ? "y" : "ies"}`;
        dropdown.appendChild(summary);

        const content = document.createElement("div");
        content.className = "tooth-history-dropdown__content";

        const statusNote = document.createElement("div");
        statusNote.className = "tooth-history-current";
        statusNote.textContent = tooth.notes
            ? `Current status note: ${tooth.notes}`
            : "No current tooth note recorded.";
        content.appendChild(statusNote);

        if (!Array.isArray(tooth.history) || tooth.history.length === 0) {
            const emptyState = document.createElement("div");
            emptyState.className = "empty-state";
            emptyState.textContent = "No treatment history recorded for this tooth yet.";
            content.appendChild(emptyState);
        } else {
            tooth.history.forEach((entry) => {
                const item = document.createElement("article");
                item.className = "tooth-history-entry";

                const head = document.createElement("div");
                head.className = "tooth-history-entry__head";
                head.textContent = `${entry.treatmentTypeName || "Treatment"} - ${formatDateTime(entry.performedAtUtc)}`;
                item.appendChild(head);

                const meta = document.createElement("p");
                meta.className = "text-muted";
                meta.textContent = entry.notes
                    ? `${entry.notes} - ${formatMoney(entry.price)}`
                    : `Recorded price: ${formatMoney(entry.price)}`;
                item.appendChild(meta);

                content.appendChild(item);
            });
        }

        dropdown.appendChild(content);
        elements.patientSelectedToothHistory.appendChild(dropdown);
    }

    async function openPatientProfile(patientId, options = {}) {
        if (!patientId) {
            return;
        }

        await refreshPatientProfile({
            patientId,
            trigger: options.trigger || elements.refreshPatientsButton,
            silentToast: true
        });

        if (options.focusForm && elements.patientProfileForm?.firstName instanceof HTMLElement) {
            focusElementIfPossible(elements.patientProfileForm.firstName);
        }
    }

    function closePatientProfile() {
        renderPatientProfile(null);
    }

    function resolveSelectedPatientTooth(profile, preferredToothNumber) {
        const teeth = Array.isArray(profile?.teeth) ? profile.teeth : [];
        if (teeth.length === 0) {
            return null;
        }

        if (preferredToothNumber) {
            const explicitMatch = teeth.find((tooth) => Number(tooth.toothNumber) === Number(preferredToothNumber));
            if (explicitMatch) {
                return explicitMatch;
            }
        }

        return teeth.find((tooth) => Array.isArray(tooth.history) && tooth.history.length > 0)
            || teeth.find((tooth) => tooth.condition && tooth.condition !== "Healthy")
            || teeth[0];
    }

    function updatePatientToothHoverCard(tooth) {
        if (!elements.patientToothHoverCard) {
            return;
        }

        elements.patientToothHoverCard.textContent = tooth
            ? `Tooth ${tooth.toothNumber}: ${buildToothPreviewText(tooth)}`
            : "Hover over a tooth to preview its latest condition.";
    }

    function buildToothPreviewText(tooth) {
        if (!tooth) {
            return "No tooth selected.";
        }

        const latestTreatmentLabel = tooth.lastTreatmentAtUtc
            ? `${tooth.lastTreatmentTypeName || "Treatment"} on ${formatDateTime(tooth.lastTreatmentAtUtc)}`
            : "No treatment recorded";

        const detail = tooth.lastTreatmentNotes || tooth.notes || "No procedure notes recorded.";
        return `${formatConditionLabel(tooth.condition)}. Last treatment: ${latestTreatmentLabel}. ${detail}`;
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

    function getToothConditionTone(condition) {
        const value = String(condition || "Healthy");
        if (value === "Caries") return "caries";
        if (value === "Filled") return "filled";
        if (value === "Crown") return "crown";
        if (value === "RootCanal") return "rootcanal";
        if (value === "Missing") return "missing";
        return "healthy";
    }

    Object.assign(app, {
        closePatientProfile,
        onDeleteDialogClose,
        onPatientCreateSubmit,
        onPatientProfileSubmit,
        openDeleteDialog,
        openPatientProfile,
        refreshPatientProfile,
        refreshPatients,
        renderPatientProfile,
        renderPatients
    });
});
