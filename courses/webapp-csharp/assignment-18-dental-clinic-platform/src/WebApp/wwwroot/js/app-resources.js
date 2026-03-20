(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerResourcesModule(app) {
    const {
        state,
        elements,
        apiRequest,
        clearElement,
        createCell,
        createPatientActionButton,
        focusElementIfPossible,
        hasCompanyRole,
        log,
        optional,
        requireTenant,
        setText,
        showToast,
        withBusy
    } = app;

    function syncAppointmentSelectOptions() {
        if (typeof app.renderAppointmentSelectOptions === "function") {
            app.renderAppointmentSelectOptions();
        }
    }

    function renderDentistAppointments() {
        if (typeof app.renderDentistProfileAppointments === "function") {
            app.renderDentistProfileAppointments();
        }
    }

    function getDentistAppointmentsList(dentistId) {
        if (typeof app.getDentistAppointments === "function") {
            return app.getDentistAppointments(dentistId);
        }

        return [];
    }

    function isUpcoming(appointment) {
        if (typeof app.isUpcomingAppointment === "function") {
            return app.isUpcomingAppointment(appointment);
        }

        return false;
    }

    function rerenderAppointments() {
        if (typeof app.renderAppointments === "function") {
            app.renderAppointments(state.appointments);
        }
    }

    function refreshAppointmentClinicalOptions() {
        if (typeof app.renderAppointmentClinicalSelectOptions === "function") {
            app.renderAppointmentClinicalSelectOptions();
        }
    }

    function resetAppointmentClinicalForm() {
        if (typeof app.resetAppointmentClinicalForm === "function") {
            app.resetAppointmentClinicalForm();
        }
    }

    function renderOpenPlanItems(items) {
        if (typeof app.renderOpenPlanItems === "function") {
            app.renderOpenPlanItems(items);
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

    function syncFinancePatientSelect() {
        if (typeof app.syncFinancePatientSelect === "function") {
            app.syncFinancePatientSelect();
        }
    }

    async function refreshAppointments(options) {
        if (typeof app.refreshAppointments === "function") {
            await app.refreshAppointments(options);
        }
    }

    async function refreshTreatmentPlans(options) {
        if (typeof app.refreshTreatmentPlans === "function") {
            await app.refreshTreatmentPlans(options);
        }
    }

    async function refreshOpenPlanItems(options) {
        if (typeof app.refreshOpenPlanItems === "function") {
            await app.refreshOpenPlanItems(options);
        }
    }

    async function refreshCostEstimates(options) {
        if (typeof app.refreshCostEstimates === "function") {
            await app.refreshCostEstimates(options);
        }
    }

    async function refreshFinanceWorkspace(options) {
        if (typeof app.refreshFinanceWorkspace === "function") {
            await app.refreshFinanceWorkspace(options);
        }
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

    async function onDentistProfileSubmit(event) {
        event.preventDefault();
        requireTenant();

        const dentistId = state.dentistProfile?.id;
        if (!dentistId) {
            throw new Error("Open a dentist profile before saving changes.");
        }

        const form = event.currentTarget;
        const payload = {
            displayName: form.displayName.value.trim(),
            licenseNumber: form.licenseNumber.value.trim(),
            specialty: optional(form.specialty.value)
        };

        await withBusy(form, async () => {
            const data = await apiRequest(`/api/v1/${state.companySlug}/dentists/${dentistId}`, {
                method: "PUT",
                body: payload,
                auth: true,
                tag: "DENTIST/UPDATE"
            });

            rememberDentists([data]);
            log("DENTIST/UPDATE/SUCCESS", { id: dentistId }, data);
            showToast("Dentist profile updated.", "success");
            await refreshDentists({ silentToast: true, trigger: form });
            rerenderAppointments();

            const refreshedDentist = resolveDentist(dentistId) || data;
            renderDentistProfile(refreshedDentist, { preserveFilters: true });
        });
    }

    async function onDentistProfileDelete() {
        requireTenant();

        const dentist = state.dentistProfile;
        if (!dentist?.id) {
            throw new Error("Open a dentist profile before deleting.");
        }

        const dentistLabel = dentist.displayName || dentist.licenseNumber || "this dentist";
        const confirmed = window.confirm(`Delete ${dentistLabel}?`);
        if (!confirmed) {
            return;
        }

        await withBusy(elements.dentistProfileDeleteButton, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/dentists/${dentist.id}`, {
                method: "DELETE",
                auth: true,
                tag: "DENTIST/DELETE"
            });

            log("DENTIST/DELETE/SUCCESS", { id: dentist.id }, { message: "Deleted" });
            showToast(`Deleted ${dentistLabel}.`, "success");
        });

        await refreshDentists({ silentToast: true, trigger: elements.dentistProfileDeleteButton });
        rerenderAppointments();
        closeDentistProfile();
    }

    async function onTreatmentRoomCreateSubmit(event) {
        event.preventDefault();
        requireTenant();

        const form = event.currentTarget;
        const roomId = state.editingTreatmentRoomId;
        const payload = {
            name: form.name.value.trim(),
            code: form.code.value.trim().toUpperCase(),
            isActiveRoom: Boolean(form.isActiveRoom.checked)
        };

        await withBusy(form, async () => {
            const isEditing = Boolean(roomId);
            const data = await apiRequest(`/api/v1/${state.companySlug}/treatmentrooms${isEditing ? `/${roomId}` : ""}`, {
                method: isEditing ? "PUT" : "POST",
                body: payload,
                auth: true,
                tag: isEditing ? "TREATMENT-ROOM/UPDATE" : "TREATMENT-ROOM/CREATE"
            });

            log(isEditing ? "TREATMENT-ROOM/UPDATE/SUCCESS" : "TREATMENT-ROOM/CREATE/SUCCESS", payload, data);
            showToast(isEditing ? "Treatment room updated." : "Treatment room added.", "success");
            resetTreatmentRoomForm();
            await refreshTreatmentRooms({ silentToast: true });
        });
    }

    async function onTreatmentRoomDelete(roomId) {
        requireTenant();

        const room = resolveTreatmentRoom(roomId);
        if (!room?.id) {
            throw new Error("Select a treatment room before deleting.");
        }

        const roomLabel = room.name || room.code || "this room";
        const confirmed = window.confirm(`Delete ${roomLabel}?`);
        if (!confirmed) {
            return;
        }

        await withBusy(elements.treatmentRoomForm || elements.treatmentRoomsBody, async () => {
            await apiRequest(`/api/v1/${state.companySlug}/treatmentrooms/${room.id}`, {
                method: "DELETE",
                auth: true,
                tag: "TREATMENT-ROOM/DELETE"
            });

            log("TREATMENT-ROOM/DELETE/SUCCESS", { id: room.id }, { message: "Deleted" });
            showToast(`Deleted ${roomLabel}.`, "success");
        });

        if (state.editingTreatmentRoomId === room.id) {
            resetTreatmentRoomForm();
        }

        await refreshTreatmentRooms({ silentToast: true });
    }

    async function refreshTreatmentTypes(options = {}) {
        requireTenant();

        const {
            trigger = elements.refreshAppointmentsButton,
            silentErrors = false
        } = options;

        try {
            const types = await withBusy(trigger, async () => {
                const data = await apiRequest(`/api/v1/${state.companySlug}/treatmenttypes`, {
                    method: "GET",
                    auth: true,
                    tag: "TREATMENT-TYPES/LIST"
                });

                return Array.isArray(data) ? data : [];
            });

            state.treatmentTypes = types;
            refreshAppointmentClinicalOptions();
        } catch (error) {
            if (silentErrors) {
                state.treatmentTypes = [];
                refreshAppointmentClinicalOptions();
                return;
            }

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
            state.dentistProfile = null;
            state.dentists = [];
            state.treatmentRooms = [];
            state.treatmentTypes = [];
            state.treatmentPlans = [];
            state.appointments = [];
            state.openPlanItems = [];
            state.costEstimates = [];
            state.financeWorkspace = null;
            state.financeInvoiceDetail = null;
            state.financeSelectedInvoiceId = "";
            renderDentists([]);
            renderDentistProfile(null);
            renderTreatmentRooms([]);
            rerenderAppointments();
            renderFinanceWorkspace(null);
            renderFinanceInvoiceDetail(null);
            refreshAppointmentClinicalOptions();
            resetAppointmentClinicalForm();
            renderOpenPlanItems([]);
            return;
        }

        await refreshResources({ silentToast: true, silentErrors: true, trigger: options.trigger });
        await refreshTreatmentTypes({ silentErrors: true, trigger: options.trigger });
        await refreshAppointments({ silentToast: true, silentErrors: true, trigger: options.trigger });
        await refreshTreatmentPlans({ silentErrors: true, trigger: options.trigger });

        if (state.dentistProfile?.id) {
            renderDentistProfile(resolveDentist(state.dentistProfile.id) || state.dentistProfile, { preserveFilters: true });
        }

        if (canAccessPlanDecisions) {
            await refreshOpenPlanItems({ silentToast: true, silentErrors: true, trigger: options.trigger });
            await refreshCostEstimates({ silentToast: true, silentErrors: true, trigger: options.trigger });
            if (state.financePatientId) {
                await refreshFinanceWorkspace({ patientId: state.financePatientId, silentToast: true, silentErrors: true, trigger: options.trigger });
            } else {
                syncFinancePatientSelect();
            }
        } else {
            state.openPlanItems = [];
            renderOpenPlanItems([]);
            state.costEstimates = [];
            state.financeWorkspace = null;
            state.financeInvoiceDetail = null;
            state.financeSelectedInvoiceId = "";
            renderFinanceWorkspace(null);
            renderFinanceInvoiceDetail(null);
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

    async function refreshDentistProfile(options = {}) {
        requireTenant();

        const dentistId = options.dentistId || state.dentistProfile?.id;
        if (!dentistId) {
            return;
        }

        const trigger = options.trigger || elements.dentistProfileRefreshButton || elements.refreshResourcesButton;
        await refreshDentists({ trigger, silentToast: true });
        await refreshAppointments({ trigger, silentToast: true, silentErrors: true });

        const dentist = resolveDentist(dentistId);
        if (!dentist) {
            closeDentistProfile();
            throw new Error("Dentist was not found.");
        }

        renderDentistProfile(dentist, { preserveFilters: true });

        if (!options.silentToast) {
            showToast("Dentist profile loaded.", "info");
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

            rememberDentists(dentists);
            state.dentists = dentists;
            renderDentists(dentists);
            syncDentistProfileAfterDentistListRefresh(dentists);
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

    function renderDentists(dentists) {
        if (!elements.dentistsBody) {
            return;
        }

        clearElement(elements.dentistsBody);

        if (!Array.isArray(dentists) || dentists.length === 0) {
            renderDentistsEmptyState();
            state.dentists = [];
            syncAppointmentSelectOptions();
            return;
        }

        const canAccessResources = canAccessResourcesUi();
        const canManageResources = canManageResourcesUi();

        dentists.forEach((dentist) => {
            const row = document.createElement("tr");
            row.dataset.dentistId = dentist.id || "";
            row.appendChild(createCell(dentist.displayName || "-"));
            row.appendChild(createCell(dentist.licenseNumber || "-"));
            row.appendChild(createCell(dentist.specialty || "-"));

            const idCell = createCell(dentist.id || "-", "cell--id");
            row.appendChild(idCell);

            const actionsCell = createCell("");
            actionsCell.classList.add("text-right", "table-actions");

            const viewButton = createPatientActionButton("View", "btn btn--ghost btn--sm", () => {
                void openDentistProfile(dentist.id);
            });
            const editButton = createPatientActionButton("Edit", "btn btn--secondary btn--sm", () => {
                void openDentistProfile(dentist.id, { focusForm: true });
            });

            viewButton.disabled = !canAccessResources;
            editButton.disabled = !canManageResources;

            actionsCell.appendChild(viewButton);
            actionsCell.appendChild(editButton);
            row.appendChild(actionsCell);
            elements.dentistsBody.appendChild(row);
        });

        state.dentists = dentists;
        syncAppointmentSelectOptions();
    }

    function renderTreatmentRooms(rooms) {
        if (!elements.treatmentRoomsBody) {
            return;
        }

        state.treatmentRooms = Array.isArray(rooms) ? rooms : [];
        clearElement(elements.treatmentRoomsBody);

        if (state.treatmentRooms.length === 0) {
            if (state.editingTreatmentRoomId) {
                resetTreatmentRoomForm();
            }
            renderTreatmentRoomsEmptyState();
            syncAppointmentSelectOptions();
            return;
        }

        const canManageResources = canManageResourcesUi();

        state.treatmentRooms.forEach((room) => {
            const row = document.createElement("tr");
            row.appendChild(createCell(room.name || "-"));
            row.appendChild(createCell(room.code || "-"));

            const statusCell = document.createElement("td");
            const statusBadge = document.createElement("span");
            statusBadge.className = `badge ${room.isActiveRoom ? "badge--success" : "badge--warning"}`;
            statusBadge.textContent = room.isActiveRoom ? "Active" : "Inactive";
            statusCell.appendChild(statusBadge);
            row.appendChild(statusCell);

            row.appendChild(createCell(room.id || "-", "cell--id"));
            const actionsCell = createCell("");
            actionsCell.classList.add("text-right", "table-actions");

            const editButton = document.createElement("button");
            editButton.type = "button";
            editButton.className = "btn btn--secondary btn--sm";
            editButton.textContent = "Edit";
            editButton.disabled = !canManageResources;
            editButton.addEventListener("click", () => {
                populateTreatmentRoomForm(room);
            });
            actionsCell.appendChild(editButton);

            const deleteButton = document.createElement("button");
            deleteButton.type = "button";
            deleteButton.className = "btn btn--destructive btn--sm";
            deleteButton.textContent = "Delete";
            deleteButton.disabled = !canManageResources;
            deleteButton.addEventListener("click", () => {
                void onTreatmentRoomDelete(room.id);
            });
            actionsCell.appendChild(deleteButton);
            row.appendChild(actionsCell);

            elements.treatmentRoomsBody.appendChild(row);
        });

        if (state.editingTreatmentRoomId && !state.treatmentRooms.some((room) => room.id === state.editingTreatmentRoomId)) {
            resetTreatmentRoomForm();
        }

        syncAppointmentSelectOptions();
    }

    function renderDentistProfile(profile, options = {}) {
        state.dentistProfile = profile || null;

        if (!elements.dentistProfilePanel || !elements.resourcesWorkspace) {
            return;
        }

        if (!profile) {
            elements.dentistProfilePanel.hidden = true;
            elements.resourcesWorkspace.hidden = false;
            setText(elements.dentistProfileName, "Select a dentist");
            setText(elements.dentistProfileMeta, "Open a dentist to review details and appointment activity.");
            setText(elements.dentistProfileAppointmentCount, "0");
            setText(elements.dentistProfileUpcomingCount, "0");
            setText(elements.dentistProfilePastCount, "0");
            setText(elements.dentistProfileLicense, "-");
            setText(elements.dentistProfileSpecialty, "-");
            setText(elements.dentistProfileId, "-");
            setText(elements.dentistProfileAppointmentsSummary, "Use search and the time filter to review this dentist's schedule history.");

            if (elements.dentistProfileForm) {
                elements.dentistProfileForm.reset();
            }

            resetDentistProfileFilters();
            renderDentistAppointments();
            return;
        }

        rememberDentists([profile]);

        if (!options.preserveFilters) {
            resetDentistProfileFilters();
        }

        elements.resourcesWorkspace.hidden = true;
        elements.dentistProfilePanel.hidden = false;

        const appointments = getDentistAppointmentsList(profile.id);
        const upcomingCount = appointments.filter((appointment) => isUpcoming(appointment)).length;
        const pastCount = appointments.length - upcomingCount;

        setText(elements.dentistProfileName, profile.displayName || "Unnamed dentist");
        setText(
            elements.dentistProfileMeta,
            `${appointments.length} appointment${appointments.length === 1 ? "" : "s"} recorded. ${upcomingCount} upcoming, ${pastCount} past.`
        );
        setText(elements.dentistProfileAppointmentCount, String(appointments.length));
        setText(elements.dentistProfileUpcomingCount, String(upcomingCount));
        setText(elements.dentistProfilePastCount, String(pastCount));
        setText(elements.dentistProfileLicense, profile.licenseNumber || "-");
        setText(elements.dentistProfileSpecialty, profile.specialty || "-");
        setText(elements.dentistProfileId, profile.id || "-");

        if (elements.dentistProfileForm) {
            elements.dentistProfileForm.displayName.value = profile.displayName || "";
            elements.dentistProfileForm.licenseNumber.value = profile.licenseNumber || "";
            elements.dentistProfileForm.specialty.value = profile.specialty || "";
        }

        renderDentistAppointments();

        if (options.focusForm && elements.dentistProfileForm?.displayName instanceof HTMLElement) {
            focusElementIfPossible(elements.dentistProfileForm.displayName);
        }
    }

    async function openDentistProfile(dentistId, options = {}) {
        if (!dentistId) {
            return;
        }

        const trigger = options.trigger || elements.refreshResourcesButton;
        if (!resolveDentist(dentistId)) {
            await refreshDentists({ trigger, silentToast: true });
        }

        if (state.appointments.length === 0) {
            await refreshAppointments({ trigger, silentToast: true, silentErrors: true });
        }

        const dentist = resolveDentist(dentistId);
        if (!dentist) {
            throw new Error("Dentist was not found.");
        }

        renderDentistProfile(dentist, {
            focusForm: options.focusForm === true,
            preserveFilters: options.preserveFilters === true
        });
    }

    function closeDentistProfile() {
        renderDentistProfile(null);
    }

    function resetDentistProfileFilters() {
        if (elements.dentistProfileAppointmentSearch instanceof HTMLInputElement) {
            elements.dentistProfileAppointmentSearch.value = "";
        }
        if (elements.dentistProfileAppointmentFilter instanceof HTMLSelectElement) {
            elements.dentistProfileAppointmentFilter.value = "all";
        }
    }

    function populateTreatmentRoomForm(room) {
        if (!(elements.treatmentRoomForm instanceof HTMLFormElement) || !room) {
            return;
        }

        state.editingTreatmentRoomId = room.id;
        elements.treatmentRoomForm.name.value = room.name || "";
        elements.treatmentRoomForm.code.value = room.code || "";
        elements.treatmentRoomForm.isActiveRoom.checked = Boolean(room.isActiveRoom);

        if (elements.treatmentRoomSubmitButton) {
            elements.treatmentRoomSubmitButton.textContent = "Save room";
        }

        if (elements.treatmentRoomCancelButton) {
            elements.treatmentRoomCancelButton.hidden = false;
        }

        focusElementIfPossible(elements.treatmentRoomForm.name);
    }

    function resetTreatmentRoomForm() {
        state.editingTreatmentRoomId = null;

        if (!(elements.treatmentRoomForm instanceof HTMLFormElement)) {
            return;
        }

        elements.treatmentRoomForm.reset();
        elements.treatmentRoomForm.isActiveRoom.checked = true;

        if (elements.treatmentRoomSubmitButton) {
            elements.treatmentRoomSubmitButton.textContent = "Add room";
        }

        if (elements.treatmentRoomCancelButton) {
            elements.treatmentRoomCancelButton.hidden = true;
        }
    }

    function syncDentistProfileAfterDentistListRefresh(dentists) {
        if (!state.dentistProfile?.id) {
            return;
        }

        const freshDentist = (Array.isArray(dentists) ? dentists : []).find((item) => item.id === state.dentistProfile.id) || null;
        if (!freshDentist) {
            closeDentistProfile();
            return;
        }

        renderDentistProfile(freshDentist, { preserveFilters: true });
    }

    function rememberDentists(dentists) {
        (Array.isArray(dentists) ? dentists : []).forEach((dentist) => {
            if (!dentist?.id) {
                return;
            }

            state.dentistDirectory[dentist.id] = {
                id: dentist.id,
                displayName: dentist.displayName || "",
                licenseNumber: dentist.licenseNumber || "",
                specialty: dentist.specialty || null
            };
        });
    }

    function renderDentistsSkeleton() {
        if (!elements.dentistsBody) {
            return;
        }

        clearElement(elements.dentistsBody);
        for (let rowIndex = 0; rowIndex < 3; rowIndex += 1) {
            const row = document.createElement("tr");
            for (let cellIndex = 0; cellIndex < 5; cellIndex += 1) {
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

    function renderDentistsEmptyState() {
        if (!elements.dentistsBody) {
            return;
        }

        const row = document.createElement("tr");
        const cell = document.createElement("td");
        const content = document.createElement("div");
        cell.colSpan = 5;
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
        cell.colSpan = 5;
        content.className = "empty-state";
        content.textContent = "No treatment rooms loaded.";
        cell.appendChild(content);
        row.appendChild(cell);
        elements.treatmentRoomsBody.appendChild(row);
    }

    function resolveDentistName(dentistId) {
        const dentist = resolveDentist(dentistId) || state.dentistDirectory[dentistId] || null;
        return dentist?.displayName || dentist?.licenseNumber || dentistId || "-";
    }

    function resolveRoomLabel(roomId) {
        const room = resolveTreatmentRoom(roomId);
        if (!room) {
            return roomId || "-";
        }

        return `${room.code || "-"}${room.name ? ` (${room.name})` : ""}`;
    }

    function resolveDentist(dentistId) {
        return state.dentists.find((item) => item.id === dentistId) || null;
    }

    function resolveTreatmentRoom(roomId) {
        return state.treatmentRooms.find((item) => item.id === roomId) || null;
    }

    function canAccessResourcesUi() {
        return Boolean(
            state.companySlug
            && hasCompanyRole("CompanyOwner", "CompanyAdmin", "CompanyManager", "CompanyEmployee")
        );
    }

    function canManageResourcesUi() {
        return Boolean(
            state.companySlug
            && hasCompanyRole("CompanyOwner", "CompanyAdmin")
        );
    }

    Object.assign(app, {
        canAccessResourcesUi,
        canManageResourcesUi,
        closeDentistProfile,
        onDentistCreateSubmit,
        onDentistProfileDelete,
        onDentistProfileSubmit,
        onTreatmentRoomCreateSubmit,
        openDentistProfile,
        refreshClinicalViews,
        refreshDentistProfile,
        refreshDentists,
        refreshResources,
        refreshTreatmentRooms,
        refreshTreatmentTypes,
        renderDentistProfile,
        renderDentists,
        renderTreatmentRooms,
        resetTreatmentRoomForm,
        resolveDentist,
        resolveDentistName,
        resolveRoomLabel,
        resolveTreatmentRoom
    });
});
