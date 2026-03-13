import { ValidationError } from "./errors.js";
import { normalizeDependencyList, normalizeTagList, normalizeText } from "./utils.js";
function defaultFilters() {
    return {
        status: "",
        priority: "",
        category: "",
        dueBefore: "",
        tag: ""
    };
}
function defaultSort() {
    return {
        by: "priority",
        direction: "asc"
    };
}
function defaultQueryOptions() {
    return {
        search: "",
        filters: defaultFilters(),
        sort: defaultSort()
    };
}
function recurrenceText(task) {
    if (task.recurrence.frequency === "none") {
        return "none";
    }
    const end = task.recurrence.endDate ? `, until ${task.recurrence.endDate}` : "";
    return `${task.recurrence.frequency} / ${task.recurrence.interval}${end}`;
}
export class TaskManagerUI {
    constructor(service, doc = document) {
        this.service = service;
        this.doc = doc;
        this.elements = this.collectElements();
        this.state = {
            busy: false,
            selectedTaskId: null,
            visibleTasks: [],
            query: defaultQueryOptions()
        };
    }
    async init() {
        this.bindEvents();
        await this.executeSafely("list", async () => {
            await this.refreshView();
        });
    }
    byId(id) {
        const node = this.doc.getElementById(id);
        if (!node) {
            throw new Error(`Missing UI element: ${id}`);
        }
        return node;
    }
    collectElements() {
        const commandButtons = this.doc.querySelectorAll(".cmd-btn");
        if (commandButtons.length === 0) {
            throw new Error("No command buttons found.");
        }
        return {
            form: this.byId("task-form"),
            taskId: this.byId("task-id"),
            title: this.byId("title"),
            description: this.byId("description"),
            status: this.byId("status"),
            priority: this.byId("priority"),
            category: this.byId("category"),
            dueDate: this.byId("due-date"),
            tags: this.byId("tags"),
            dependencies: this.byId("dependencies"),
            recurrenceFrequency: this.byId("recurrence-frequency"),
            recurrenceInterval: this.byId("recurrence-interval"),
            recurrenceEndDate: this.byId("recurrence-end-date"),
            saveBtn: this.byId("save-btn"),
            resetBtn: this.byId("reset-btn"),
            deleteSelectedBtn: this.byId("delete-selected-btn"),
            clearQueryBtn: this.byId("clear-query-btn"),
            commandButtons,
            searchQuery: this.byId("search-query"),
            filterStatus: this.byId("filter-status"),
            filterPriority: this.byId("filter-priority"),
            filterCategory: this.byId("filter-category"),
            filterDueBefore: this.byId("filter-due-before"),
            filterTag: this.byId("filter-tag"),
            sortBy: this.byId("sort-by"),
            sortDirection: this.byId("sort-direction"),
            statusMessage: this.byId("status-message"),
            tableBody: this.byId("task-table-body"),
            emptyState: this.byId("empty-state"),
            rowTemplate: this.byId("task-row-template"),
            statTotal: this.byId("stat-total"),
            statCompleted: this.byId("stat-completed"),
            statBlocked: this.byId("stat-blocked"),
            statOverdue: this.byId("stat-overdue"),
            statRate: this.byId("stat-rate")
        };
    }
    bindEvents() {
        this.elements.form.addEventListener("submit", async (event) => {
            event.preventDefault();
            await this.handleSave();
        });
        this.elements.resetBtn.addEventListener("click", () => {
            this.resetForm();
            this.setStatus("Editor reset.", "neutral");
        });
        this.elements.deleteSelectedBtn.addEventListener("click", async () => {
            await this.handleDeleteSelected();
        });
        this.elements.clearQueryBtn.addEventListener("click", async () => {
            this.resetQueryInputs();
            this.state.query = defaultQueryOptions();
            await this.executeSafely("list", async () => {
                await this.refreshView();
            });
        });
        this.elements.commandButtons.forEach((button) => {
            button.addEventListener("click", async () => {
                const command = button.dataset.command;
                if (!command) {
                    return;
                }
                await this.handleCommand(command);
            });
        });
        this.elements.tableBody.addEventListener("click", async (event) => {
            const trigger = event.target.closest("button");
            if (!trigger) {
                return;
            }
            const row = trigger.closest("tr");
            const taskId = row?.dataset.taskId ?? "";
            if (!taskId) {
                return;
            }
            if (trigger.classList.contains("edit-btn")) {
                this.loadTaskIntoForm(taskId);
                return;
            }
            if (trigger.classList.contains("delete-btn")) {
                await this.executeSafely("delete", async () => {
                    if (!window.confirm("Delete this task permanently?")) {
                        return { cancelled: true, message: "Delete cancelled." };
                    }
                    await this.service.deleteTask(taskId);
                    if (this.state.selectedTaskId === taskId) {
                        this.resetForm();
                    }
                    await this.refreshView();
                });
            }
        });
    }
    async handleCommand(command) {
        switch (command) {
            case "add":
                await this.handleSave({ forcedMode: "add" });
                break;
            case "list":
                this.state.query = defaultQueryOptions();
                this.resetQueryInputs();
                await this.executeSafely("list", async () => {
                    await this.refreshView();
                });
                break;
            case "update":
                await this.handleSave({ forcedMode: "update" });
                break;
            case "delete":
                await this.handleDeleteSelected();
                break;
            case "filter":
                this.state.query.filters = this.readFilterInputs();
                await this.executeSafely("filter", async () => {
                    await this.refreshView();
                });
                break;
            case "search":
                this.state.query.search = normalizeText(this.elements.searchQuery.value);
                await this.executeSafely("search", async () => {
                    await this.refreshView();
                });
                break;
            case "sort":
                this.state.query.sort = this.readSortInputs();
                await this.executeSafely("sort", async () => {
                    await this.refreshView();
                });
                break;
            default:
                this.setStatus(`Unknown command ${command}.`, "error");
        }
    }
    async handleSave(options = {}) {
        await this.executeSafely(options.forcedMode ?? "add", async () => {
            const mode = options.forcedMode ?? (this.state.selectedTaskId ? "update" : "add");
            const taskCore = this.service.createTaskCoreFromInput(this.readFormRawValues());
            if (mode === "update") {
                const selectedId = this.state.selectedTaskId;
                if (!selectedId) {
                    throw new ValidationError("Select task before update.");
                }
                await this.service.updateTask(selectedId, taskCore);
            }
            else {
                await this.service.addTask(taskCore);
                this.resetForm();
            }
            await this.refreshView();
        });
    }
    async handleDeleteSelected() {
        await this.executeSafely("delete", async () => {
            const selectedId = this.state.selectedTaskId;
            if (!selectedId) {
                throw new ValidationError("No selected task.");
            }
            if (!window.confirm("Delete selected task permanently?")) {
                return { cancelled: true, message: "Delete cancelled." };
            }
            await this.service.deleteTask(selectedId);
            this.resetForm();
            await this.refreshView();
        });
    }
    readFormRawValues() {
        return {
            title: this.elements.title.value,
            description: this.elements.description.value,
            status: this.elements.status.value,
            priority: this.elements.priority.value,
            category: this.elements.category.value,
            dueDate: this.elements.dueDate.value,
            tags: normalizeTagList(this.elements.tags.value),
            dependencies: normalizeDependencyList(this.elements.dependencies.value),
            recurrenceFrequency: this.elements.recurrenceFrequency.value,
            recurrenceInterval: this.elements.recurrenceInterval.value,
            recurrenceEndDate: this.elements.recurrenceEndDate.value
        };
    }
    readFilterInputs() {
        return {
            status: this.elements.filterStatus.value,
            priority: this.elements.filterPriority.value,
            category: this.elements.filterCategory.value,
            dueBefore: this.elements.filterDueBefore.value,
            tag: normalizeText(this.elements.filterTag.value)
        };
    }
    readSortInputs() {
        return {
            by: this.elements.sortBy.value,
            direction: this.elements.sortDirection.value
        };
    }
    resetQueryInputs() {
        this.elements.searchQuery.value = "";
        this.elements.filterStatus.value = "";
        this.elements.filterPriority.value = "";
        this.elements.filterCategory.value = "";
        this.elements.filterDueBefore.value = "";
        this.elements.filterTag.value = "";
        this.elements.sortBy.value = "priority";
        this.elements.sortDirection.value = "asc";
    }
    loadTaskIntoForm(taskId) {
        const task = this.state.visibleTasks.find((item) => item.id === taskId);
        if (!task) {
            this.setStatus("Task not found in current view.", "error");
            return;
        }
        this.state.selectedTaskId = task.id;
        this.elements.taskId.value = task.id;
        this.elements.title.value = task.title;
        this.elements.description.value = task.description;
        this.elements.status.value = task.status;
        this.elements.priority.value = task.priority;
        this.elements.category.value = task.category;
        this.elements.dueDate.value = task.dueDate ?? "";
        this.elements.tags.value = task.tags.join(", ");
        this.elements.dependencies.value = task.dependencies.join(", ");
        this.elements.recurrenceFrequency.value = task.recurrence.frequency;
        this.elements.recurrenceInterval.value = String(task.recurrence.interval);
        this.elements.recurrenceEndDate.value = task.recurrence.endDate ?? "";
        this.elements.saveBtn.textContent = "Update Task";
        this.renderTasks(this.state.visibleTasks);
        this.setStatus(`Loaded task ${task.id}.`, "neutral");
    }
    resetForm() {
        this.elements.form.reset();
        this.elements.status.value = "todo";
        this.elements.priority.value = "medium";
        this.elements.category.value = "study";
        this.elements.recurrenceFrequency.value = "none";
        this.elements.recurrenceInterval.value = "1";
        this.elements.taskId.value = "";
        this.elements.saveBtn.textContent = "Add Task";
        this.state.selectedTaskId = null;
        this.renderTasks(this.state.visibleTasks);
    }
    renderTasks(tasks) {
        this.elements.tableBody.textContent = "";
        this.elements.emptyState.style.display = tasks.length > 0 ? "none" : "block";
        for (const task of tasks) {
            const fragment = this.elements.rowTemplate.content.cloneNode(true);
            const row = fragment.querySelector("tr");
            if (!row) {
                continue;
            }
            row.dataset.taskId = task.id;
            if (task.id === this.state.selectedTaskId) {
                row.style.background = "rgba(11, 114, 133, 0.08)";
            }
            const idCell = fragment.querySelector(".id-cell");
            const titleCell = fragment.querySelector(".task-title");
            const descriptionCell = fragment.querySelector(".task-description");
            const statusPill = fragment.querySelector(".status-cell .pill");
            const priorityPill = fragment.querySelector(".priority-cell .pill");
            const categoryCell = fragment.querySelector(".category-cell");
            const dueCell = fragment.querySelector(".due-cell");
            const recurrenceCell = fragment.querySelector(".recurrence-cell");
            const dependenciesCell = fragment.querySelector(".dependencies-cell");
            const tagsCell = fragment.querySelector(".tags-cell");
            if (!idCell ||
                !titleCell ||
                !descriptionCell ||
                !statusPill ||
                !priorityPill ||
                !categoryCell ||
                !dueCell ||
                !recurrenceCell ||
                !dependenciesCell ||
                !tagsCell) {
                continue;
            }
            idCell.textContent = task.id;
            titleCell.textContent = task.title;
            descriptionCell.textContent = task.description || "No description";
            statusPill.textContent = task.status;
            statusPill.classList.add(`pill-status-${task.status}`);
            priorityPill.textContent = task.priority;
            priorityPill.classList.add(`pill-priority-${task.priority}`);
            categoryCell.textContent = task.category;
            dueCell.textContent = task.dueDate ?? "No due date";
            recurrenceCell.textContent = recurrenceText(task);
            dependenciesCell.textContent = task.dependencies.join(", ") || "none";
            tagsCell.textContent = task.tags.join(", ") || "none";
            this.elements.tableBody.appendChild(fragment);
        }
    }
    renderStatistics(stats) {
        this.elements.statTotal.textContent = String(stats.total);
        this.elements.statCompleted.textContent = String(stats.completed);
        this.elements.statBlocked.textContent = String(stats.blocked);
        this.elements.statOverdue.textContent = String(stats.overdue);
        this.elements.statRate.textContent = `${stats.completionRate}%`;
    }
    async refreshView() {
        const tasks = await this.service.queryTasks(this.state.query);
        const stats = await this.service.getStatistics(tasks);
        this.state.visibleTasks = tasks;
        this.renderTasks(tasks);
        this.renderStatistics(stats);
    }
    async executeSafely(command, action) {
        if (this.state.busy) {
            return;
        }
        this.setBusy(true);
        try {
            const result = await action();
            if (result && result.cancelled) {
                this.setStatus(result.message ?? "Command cancelled.", "neutral");
                return;
            }
            this.setStatus(`Command "${command}" executed.`, "success");
        }
        catch (error) {
            console.error(error);
            const message = error instanceof Error && error.message.trim()
                ? error.message
                : "Unexpected error occurred.";
            this.setStatus(message, "error");
        }
        finally {
            this.setBusy(false);
        }
    }
    setBusy(busy) {
        this.state.busy = busy;
        this.elements.commandButtons.forEach((button) => {
            button.disabled = busy;
        });
        this.elements.saveBtn.disabled = busy;
        this.elements.resetBtn.disabled = busy;
        this.elements.deleteSelectedBtn.disabled = busy;
        this.elements.clearQueryBtn.disabled = busy;
    }
    setStatus(message, tone) {
        this.elements.statusMessage.textContent = message;
        this.elements.statusMessage.dataset.tone = tone;
    }
}
