(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerAppointmentsModule(app) {
    const {
        state,
        elements,
        activateScreen,
        apiRequest,
        clearElement,
        createCell,
        createContentCell,
        createEntityLinkButton,
        focusElementIfPossible,
        formatDateTime,
        formatTime,
        hasCompanyRole,
        log,
        openDentistProfile,
        openPatientProfile,
        optional,
        refreshPatientProfile,
        renderDentistProfile,
        requireTenant,
        resolveDentist,
        resolveDentistName,
        resolvePatient,
        resolvePatientName,
        resolveRoomLabel,
        resolveTreatmentRoom,
        setSelectOptions,
        showToast,
        syncFinancePatientSelect,
        toUtcIso,
        withBusy,
        renderAppointmentClinicalSelectOptions,
        resetAppointmentClinicalForm
    } = app;

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

    async function onAppointmentClinicalSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const appointmentId = form.appointmentId.value;
        if (!appointmentId) {
            throw new Error("Select an appointment first.");
        }

        const rows = Array.from(elements.appointmentClinicalItems?.querySelectorAll(".clinical-entry") ?? []);
        if (rows.length === 0) {
            throw new Error("Add at least one tooth entry.");
        }

        const items = rows.map((row) => {
            const toothNumber = row.querySelector('[data-clinical-field="toothNumber"]')?.value || "";
            const planItemId = row.querySelector('[data-clinical-field="planItemId"]')?.value || "";
            const treatmentTypeId = row.querySelector('[data-clinical-field="treatmentTypeId"]')?.value || "";
            const condition = row.querySelector('[data-clinical-field="condition"]')?.value || "";
            const priceRaw = row.querySelector('[data-clinical-field="price"]')?.value || "";
            const notes = row.querySelector('[data-clinical-field="notes"]')?.value || "";

            if (!toothNumber || !treatmentTypeId || !condition) {
                throw new Error("Complete tooth, treatment type, and status for each clinical entry.");
            }

            return {
                toothNumber: Number(toothNumber),
                planItemId: optional(planItemId),
                treatmentTypeId,
                condition,
                price: priceRaw === "" ? null : Number(priceRaw),
                notes: optional(notes)
            };
        });

        const payload = {
            performedAtUtc: toUtcIso(form.performedAtLocal.value),
            markAppointmentCompleted: Boolean(form.markCompleted.checked),
            items
        };

        await withBusy(form, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/appointments/${appointmentId}/clinical-record`, {
                method: "POST",
                body: payload,
                auth: true,
                tag: "APPOINTMENT/CLINICAL-RECORD"
            });

            const appointment = state.appointments.find((item) => item.id === appointmentId) || null;
            showToast(`${items.length} tooth entr${items.length === 1 ? "y" : "ies"} recorded.`, "success");
            resetAppointmentClinicalForm({ appointmentId });
            await refreshAppointments({ silentToast: true });

            if (appointment && state.patientProfile?.id === appointment.patientId) {
                await refreshPatientProfile({ patientId: appointment.patientId, silentToast: true, trigger: form });
            }
        });
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
            if (state.dentistProfile?.id) {
                renderDentistProfile(resolveDentist(state.dentistProfile.id) || state.dentistProfile, { preserveFilters: true });
            }
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

    function renderAppointments(appointments) {
        if (!elements.appointmentsBody) {
            return;
        }

        state.appointments = Array.isArray(appointments) ? appointments : [];
        updateAppointmentsToolbar();

        clearElement(elements.appointmentsBody);

        if (state.appointments.length === 0) {
            updateAppointmentsSummary([], [], false);
            renderAppointmentsEmptyState("No appointments loaded.");
            renderAppointmentClinicalSelectOptions();
            return;
        }

        const canManageAppointments = canManageAppointmentsUi();
        const filteredAppointments = getVisibleAppointments(state.appointments);

        if (filteredAppointments.length === 0) {
            renderAppointmentsEmptyState("No appointments match the current schedule filters.");
            renderAppointmentClinicalSelectOptions();
            return;
        }

        filteredAppointments.forEach((appointment) => {
            const row = document.createElement("tr");
            row.appendChild(createAppointmentPatientCell(appointment));
            row.appendChild(createAppointmentDentistCell(appointment));
            row.appendChild(createAppointmentRoomCell(appointment));
            row.appendChild(createAppointmentTimeCell(appointment.startAtUtc));
            row.appendChild(createAppointmentTimeCell(appointment.endAtUtc));

            const statusCell = document.createElement("td");
            statusCell.className = "appointment-cell appointment-cell--status";
            const statusBadge = document.createElement("span");
            statusBadge.className = `badge ${getAppointmentStatusBadgeClass(appointment.status)}`;
            statusBadge.textContent = formatAppointmentStatus(appointment.status);
            statusCell.appendChild(statusBadge);
            row.appendChild(statusCell);

            const actionsCell = createCell("");
            actionsCell.classList.add("text-right", "table-actions", "appointment-cell");
            const recordButton = document.createElement("button");
            recordButton.type = "button";
            recordButton.className = "btn btn--secondary btn--sm";
            recordButton.textContent = "Record work";
            recordButton.disabled = !canManageAppointments;
            recordButton.addEventListener("click", () => {
                recordAppointmentWork(appointment.id);
            });
            actionsCell.appendChild(recordButton);
            row.appendChild(actionsCell);

            elements.appointmentsBody.appendChild(row);
        });
        renderAppointmentClinicalSelectOptions();
    }

    function renderDentistProfileAppointments() {
        if (!elements.dentistProfileAppointmentsBody) {
            return;
        }

        clearElement(elements.dentistProfileAppointmentsBody);

        const dentist = state.dentistProfile;
        if (!dentist?.id) {
            renderDentistProfileAppointmentsEmptyState("Open a dentist profile to inspect appointments.");
            return;
        }

        const allAppointments = getDentistAppointments(dentist.id);
        const searchTerm = (elements.dentistProfileAppointmentSearch?.value || "").trim().toLowerCase();
        const timeFilter = elements.dentistProfileAppointmentFilter?.value || "all";
        const filteredAppointments = allAppointments.filter((appointment) => {
            const matchesTimeFilter = timeFilter === "all"
                || (timeFilter === "upcoming" && isUpcomingAppointment(appointment))
                || (timeFilter === "past" && !isUpcomingAppointment(appointment));

            if (!matchesTimeFilter) {
                return false;
            }

            if (!searchTerm) {
                return true;
            }

            const patientName = resolvePatientName(appointment.patientId);
            const roomLabel = resolveRoomLabel(appointment.treatmentRoomId);
            const haystack = [
                patientName,
                roomLabel,
                appointment.status,
                appointment.notes,
                formatDateTime(appointment.startAtUtc),
                formatDateTime(appointment.endAtUtc)
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase();

            return haystack.includes(searchTerm);
        });

        if (elements.dentistProfileAppointmentsSummary) {
            const filterLabel = timeFilter === "upcoming"
                ? "upcoming appointments"
                : timeFilter === "past"
                    ? "past appointments"
                    : "appointments";
            elements.dentistProfileAppointmentsSummary.textContent = filteredAppointments.length === allAppointments.length
                ? `${filteredAppointments.length} ${filterLabel} shown for this dentist.`
                : `${filteredAppointments.length} of ${allAppointments.length} ${filterLabel} shown for this dentist.`;
        }

        if (filteredAppointments.length === 0) {
            renderDentistProfileAppointmentsEmptyState("No appointments match the current dentist filter.");
            return;
        }

        filteredAppointments.forEach((appointment) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(resolvePatientName(appointment.patientId)));
            row.appendChild(createCell(resolveRoomLabel(appointment.treatmentRoomId)));
            row.appendChild(createContentCell(createAppointmentTimeCell(appointment.startAtUtc).firstChild));
            row.appendChild(createContentCell(createAppointmentTimeCell(appointment.endAtUtc).firstChild));

            const statusCell = document.createElement("td");
            const statusBadge = document.createElement("span");
            statusBadge.className = `badge ${getAppointmentStatusBadgeClass(appointment.status)}`;
            statusBadge.textContent = formatAppointmentStatus(appointment.status);
            statusCell.appendChild(statusBadge);
            row.appendChild(statusCell);

            row.appendChild(createCell(appointment.notes || "-"));
            elements.dentistProfileAppointmentsBody.appendChild(row);
        });
    }

    function renderDentistProfileAppointmentsEmptyState(message) {
        if (!elements.dentistProfileAppointmentsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        cell.colSpan = 6;
        const content = document.createElement("div");
        content.className = "empty-state";
        content.textContent = message;
        cell.appendChild(content);
        row.appendChild(cell);
        elements.dentistProfileAppointmentsBody.appendChild(row);
    }

    function getDentistAppointments(dentistId) {
        if (!dentistId) {
            return [];
        }

        return state.appointments
            .filter((appointment) => appointment.dentistId === dentistId)
            .sort((left, right) => new Date(left.startAtUtc).getTime() - new Date(right.startAtUtc).getTime());
    }

    function isUpcomingAppointment(appointment) {
        const endTime = new Date(appointment?.endAtUtc || appointment?.startAtUtc).getTime();
        return !Number.isNaN(endTime) && endTime >= Date.now();
    }

    function getVisibleAppointments(appointments) {
        const showPastAppointments = elements.appointmentsPastToggleButton?.dataset.showPast === "true";
        const statusFilter = normalizeAppointmentStatusFilter(elements.appointmentStatusFilter?.value || "all");
        const searchTerm = (elements.appointmentSearch?.value || "").trim().toLowerCase();

        const visibleAppointments = appointments.filter((appointment) => {
            if (!showPastAppointments && !isUpcomingAppointment(appointment)) {
                return false;
            }

            if (statusFilter !== "all" && normalizeAppointmentStatusFilter(appointment.status) !== statusFilter) {
                return false;
            }

            if (!searchTerm) {
                return true;
            }

            const haystack = [
                resolvePatientName(appointment.patientId),
                resolveDentistName(appointment.dentistId),
                resolveRoomLabel(appointment.treatmentRoomId),
                appointment.notes,
                formatAppointmentStatus(appointment.status),
                formatDateTime(appointment.startAtUtc),
                formatDateTime(appointment.endAtUtc)
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase();

            return haystack.includes(searchTerm);
        });

        updateAppointmentsSummary(appointments, visibleAppointments, showPastAppointments);
        return visibleAppointments;
    }

    function updateAppointmentsToolbar() {
        if (!elements.appointmentsPastToggleButton) {
            return;
        }

        const showPastAppointments = elements.appointmentsPastToggleButton.dataset.showPast === "true";
        elements.appointmentsPastToggleButton.textContent = showPastAppointments
            ? "Hide past appointments"
            : "Show past appointments";
    }

    function updateAppointmentsSummary(allAppointments, visibleAppointments, showPastAppointments) {
        if (!elements.appointmentsSummary) {
            return;
        }

        if (!Array.isArray(allAppointments) || allAppointments.length === 0) {
            elements.appointmentsSummary.textContent = "No appointments loaded for the active tenant yet.";
            return;
        }

        const upcomingCount = allAppointments.filter((appointment) => isUpcomingAppointment(appointment)).length;
        const pastCount = allAppointments.length - upcomingCount;
        const statusFilter = normalizeAppointmentStatusFilter(elements.appointmentStatusFilter?.value || "all");
        const searchTerm = (elements.appointmentSearch?.value || "").trim();
        const scopeLabel = showPastAppointments ? "appointments" : "ongoing and upcoming appointments";
        const filterBits = [];

        if (statusFilter !== "all") {
            filterBits.push(`${formatAppointmentStatus(statusFilter)} only`);
        }

        if (searchTerm) {
            filterBits.push(`search "${searchTerm}"`);
        }

        const filterLabel = filterBits.length > 0 ? ` Filtered by ${filterBits.join(" and ")}.` : "";
        elements.appointmentsSummary.textContent =
            `${visibleAppointments.length} of ${allAppointments.length} ${scopeLabel} shown. ${upcomingCount} ongoing or upcoming, ${pastCount} past.${filterLabel}`;
    }

    function normalizeAppointmentStatusFilter(value) {
        return String(value || "all")
            .trim()
            .toLowerCase()
            .replace(/[\s_-]+/g, "");
    }

    function canManageAppointmentsUi() {
        return Boolean(
            state.companySlug
            && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee")
        );
    }

    function recordAppointmentWork(appointmentId) {
        if (!appointmentId || !(elements.appointmentClinicalForm instanceof HTMLFormElement)) {
            return;
        }

        resetAppointmentClinicalForm({ appointmentId });
        activateScreen("appointments");
        focusElementIfPossible(elements.appointmentClinicalForm.appointmentId);
        elements.appointmentClinicalForm.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function renderAppointmentsSkeleton() {
        if (!elements.appointmentsBody) {
            return;
        }

        clearElement(elements.appointmentsBody);
        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 7; cellIndex += 1) {
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

    function renderAppointmentsEmptyState(message = "No appointments loaded.") {
        if (!elements.appointmentsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 7;
        content.className = "empty-state";
        content.textContent = message;
        cell.appendChild(content);
        row.appendChild(cell);
        elements.appointmentsBody.appendChild(row);
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

        syncFinancePatientSelect();
        renderAppointmentClinicalSelectOptions();
    }

    function createAppointmentPatientCell(appointment) {
        const patient = resolvePatient(appointment.patientId);
        const title = resolvePatientName(appointment.patientId);
        const meta = patient?.personalCode || "Open patient record";
        const button = createEntityLinkButton(title, meta, () => {
            if (!appointment.patientId) {
                return;
            }

            activateScreen("patients");
            void openPatientProfile(appointment.patientId);
        });
        button.setAttribute("aria-label", `Open patient record for ${title}`);
        return createContentCell(button, "appointment-cell appointment-cell--person");
    }

    function createAppointmentDentistCell(appointment) {
        const dentist = resolveDentist(appointment.dentistId);
        const title = resolveDentistName(appointment.dentistId);
        const meta = dentist?.specialty || "Open dentist profile";
        const button = createEntityLinkButton(title, meta, () => {
            openDentistFromAppointment(appointment.dentistId);
        });
        button.setAttribute("aria-label", `Open dentist profile for ${title}`);
        return createContentCell(button, "appointment-cell appointment-cell--person");
    }

    function createAppointmentRoomCell(appointment) {
        const room = resolveTreatmentRoom(appointment.treatmentRoomId);
        const wrapper = document.createElement("div");
        wrapper.className = "appointment-card";

        const title = document.createElement("span");
        title.className = "appointment-card__title";
        title.textContent = room?.code || appointment.treatmentRoomId || "-";
        wrapper.appendChild(title);

        const meta = document.createElement("span");
        meta.className = "appointment-card__meta";
        meta.textContent = room?.name || "Treatment room";
        wrapper.appendChild(meta);

        return createContentCell(wrapper, "appointment-cell appointment-cell--room");
    }

    function createAppointmentTimeCell(value) {
        const parsed = value ? new Date(value) : null;
        const wrapper = document.createElement("div");
        wrapper.className = "appointment-time";

        const date = document.createElement("span");
        date.className = "appointment-time__date";
        date.textContent = parsed && !Number.isNaN(parsed.getTime())
            ? parsed.toLocaleDateString([], { day: "2-digit", month: "short", year: "numeric" })
            : "-";
        wrapper.appendChild(date);

        const clock = document.createElement("span");
        clock.className = "appointment-time__clock";
        clock.textContent = parsed && !Number.isNaN(parsed.getTime())
            ? formatTime(parsed)
            : "-";
        wrapper.appendChild(clock);

        return createContentCell(wrapper, "appointment-cell appointment-cell--time");
    }

    function openDentistFromAppointment(dentistId) {
        if (!dentistId) {
            return;
        }

        activateScreen("resources");
        void openDentistProfile(dentistId);
    }

    function formatAppointmentStatus(status) {
        const normalized = normalizeAppointmentStatusFilter(status);
        if (normalized === "confirmed") {
            return "Confirmed";
        }
        if (normalized === "completed") {
            return "Completed";
        }
        if (normalized === "cancelled") {
            return "Cancelled";
        }
        return "Scheduled";
    }

    function getAppointmentStatusBadgeClass(status) {
        if (status === "Completed") {
            return "badge--success";
        }
        if (status === "Cancelled") {
            return "badge--danger";
        }
        if (status === "Confirmed") {
            return "badge--accent";
        }
        return "badge--warning";
    }

    Object.assign(app, {
        onAppointmentCreateSubmit,
        onAppointmentClinicalSubmit,
        refreshAppointments,
        renderAppointments,
        renderDentistProfileAppointments,
        getDentistAppointments,
        isUpcomingAppointment,
        renderAppointmentSelectOptions
    });
});
