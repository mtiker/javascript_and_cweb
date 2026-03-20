(window.__dentalAppModules = window.__dentalAppModules || []).push(function registerUiCoreModule(app) {
    const {
        elements,
        reportError
    } = app;

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

    function bindSyncInput(element, handler) {
        if (!element) return;
        element.addEventListener("input", handler);
    }

    function bindSyncChange(element, handler) {
        if (!element) return;
        element.addEventListener("change", handler);
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

    function renderLegalEstimateOutput(text) {
        if (!elements.legalEstimateOutput) {
            return;
        }

        elements.legalEstimateOutput.textContent = text || "No legal preview generated yet.";
    }

    function createPatientActionButton(label, className, onClick) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = className;
        button.textContent = label;
        button.addEventListener("click", onClick);
        return button;
    }

    function createEntityLinkButton(title, meta, onClick) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "entity-link";
        button.addEventListener("click", onClick);

        const titleElement = document.createElement("span");
        titleElement.className = "entity-link__title";
        titleElement.textContent = title;
        button.appendChild(titleElement);

        if (meta) {
            const metaElement = document.createElement("span");
            metaElement.className = "entity-link__meta";
            metaElement.textContent = meta;
            button.appendChild(metaElement);
        }

        return button;
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

    function setText(element, text) {
        if (element) {
            element.textContent = text;
        }
    }

    function formatConditionLabel(condition) {
        return String(condition || "Unknown").replace(/([a-z])([A-Z])/g, "$1 $2");
    }

    function formatDateTimeLocalInput(value) {
        const parsed = value instanceof Date ? value : new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return "";
        }

        const pad = (part) => String(part).padStart(2, "0");
        return `${parsed.getFullYear()}-${pad(parsed.getMonth() + 1)}-${pad(parsed.getDate())}T${pad(parsed.getHours())}:${pad(parsed.getMinutes())}`;
    }

    function focusElementIfPossible(element) {
        if (element instanceof HTMLElement && typeof element.focus === "function") {
            element.focus({ preventScroll: false });
        }
    }

    function createCell(text, className = "") {
        const cell = document.createElement("td");
        if (className) {
            cell.className = className;
        }
        cell.textContent = text;
        return cell;
    }

    function createContentCell(content, className = "") {
        const cell = document.createElement("td");
        if (className) {
            cell.className = className;
        }
        if (content instanceof Node) {
            cell.appendChild(content);
        } else {
            cell.textContent = String(content ?? "");
        }
        return cell;
    }

    function clearElement(element) {
        while (element.firstChild) {
            element.removeChild(element.firstChild);
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

    Object.assign(app, {
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
        renderLegalEstimateOutput,
        setBadgeVariant,
        setSelectOptions,
        setSyncStatus,
        setText,
        toggleFormControls,
        toUtcIso,
        withBusy
    });
});
