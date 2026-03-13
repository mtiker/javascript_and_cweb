(function () {
  "use strict";

  const STORAGE_KEY = "task_manager_v1";
  const STATUS_VALUES = Object.freeze(["todo", "in_progress", "done"]);
  const PRIORITY_VALUES = Object.freeze(["low", "medium", "high"]);

  const LIMITS = Object.freeze({
    title: 100,
    description: 500,
    tags: 10,
    tagLength: 20,
    query: 80
  });

  const PRIORITY_ORDER = Object.freeze({
    high: 0,
    medium: 1,
    low: 2
  });

  class AppError extends Error {
    constructor(message, code = "APP_ERROR") {
      super(message);
      this.name = "AppError";
      this.code = code;
    }
  }

  class ValidationError extends AppError {
    constructor(message) {
      super(message, "VALIDATION_ERROR");
      this.name = "ValidationError";
    }
  }

  class StorageError extends AppError {
    constructor(message) {
      super(message, "STORAGE_ERROR");
      this.name = "StorageError";
    }
  }

  class NotFoundError extends AppError {
    constructor(message) {
      super(message, "NOT_FOUND");
      this.name = "NotFoundError";
    }
  }

  function createTaskId() {
    if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
      return crypto.randomUUID();
    }

    const stamp = Date.now().toString(36);
    const random = Math.random().toString(36).slice(2, 10);
    return `task-${stamp}-${random}`;
  }

  function normalizeText(value) {
    return String(value ?? "").trim();
  }

  function normalizeTags(value) {
    const rawTags = Array.isArray(value) ? value : String(value ?? "").split(",");
    const tags = rawTags
      .map((tag) => normalizeText(tag).toLowerCase())
      .filter(Boolean);

    return [...new Set(tags)];
  }

  function isValidDateOnly(value) {
    if (!value) {
      return true;
    }

    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
      return false;
    }

    const [year, month, day] = value.split("-").map(Number);
    const date = new Date(Date.UTC(year, month - 1, day));

    return (
      date.getUTCFullYear() === year &&
      date.getUTCMonth() === month - 1 &&
      date.getUTCDate() === day
    );
  }

  function formatDate(value) {
    return value || "No date";
  }

  function formatTags(value) {
    if (!Array.isArray(value) || value.length === 0) {
      return "No tags";
    }

    return value.join(", ");
  }

  function hasAnyFilter(filters) {
    return Boolean(
      filters &&
        (filters.status || filters.priority || filters.dueBefore || filters.tag)
    );
  }

  async function nextTick() {
    await Promise.resolve();
  }

  function ensureAllowed(fieldName, value, allowedValues) {
    if (!allowedValues.includes(value)) {
      throw new ValidationError(
        `${fieldName} must be one of: ${allowedValues.join(", ")}.`
      );
    }
  }

  function validateTaskInput(rawInput, { partial = false } = {}) {
    if (
      rawInput === null ||
      typeof rawInput !== "object" ||
      Array.isArray(rawInput)
    ) {
      throw new ValidationError("Task payload must be a plain object.");
    }

    const payload = {};

    if (!partial || rawInput.title !== undefined) {
      const title = normalizeText(rawInput.title);
      if (!title) {
        throw new ValidationError("Title is required.");
      }
      if (title.length > LIMITS.title) {
        throw new ValidationError(`Title cannot exceed ${LIMITS.title} characters.`);
      }
      payload.title = title;
    }

    if (!partial || rawInput.description !== undefined) {
      const description = normalizeText(rawInput.description);
      if (description.length > LIMITS.description) {
        throw new ValidationError(
          `Description cannot exceed ${LIMITS.description} characters.`
        );
      }
      payload.description = description;
    }

    if (!partial || rawInput.status !== undefined) {
      const status = normalizeText(rawInput.status) || "todo";
      ensureAllowed("status", status, STATUS_VALUES);
      payload.status = status;
    }

    if (!partial || rawInput.priority !== undefined) {
      const priority = normalizeText(rawInput.priority) || "medium";
      ensureAllowed("priority", priority, PRIORITY_VALUES);
      payload.priority = priority;
    }

    if (!partial || rawInput.dueDate !== undefined) {
      const dueDate = normalizeText(rawInput.dueDate);
      if (!isValidDateOnly(dueDate)) {
        throw new ValidationError("dueDate must use YYYY-MM-DD format.");
      }
      payload.dueDate = dueDate || null;
    }

    if (!partial || rawInput.tags !== undefined) {
      const tags = normalizeTags(rawInput.tags);
      if (tags.length > LIMITS.tags) {
        throw new ValidationError(`A task can contain at most ${LIMITS.tags} tags.`);
      }
      const invalidTag = tags.find((tag) => tag.length > LIMITS.tagLength);
      if (invalidTag) {
        throw new ValidationError(
          `Tag "${invalidTag}" exceeds ${LIMITS.tagLength} characters.`
        );
      }
      payload.tags = tags;
    }

    if (partial && Object.keys(payload).length === 0) {
      throw new ValidationError("Update request does not include valid fields.");
    }

    return payload;
  }

  function validateFilters(rawFilters = {}) {
    const filters = {
      status: normalizeText(rawFilters.status),
      priority: normalizeText(rawFilters.priority),
      dueBefore: normalizeText(rawFilters.dueBefore),
      tag: normalizeText(rawFilters.tag).toLowerCase()
    };

    if (filters.status) {
      ensureAllowed("status filter", filters.status, STATUS_VALUES);
    }

    if (filters.priority) {
      ensureAllowed("priority filter", filters.priority, PRIORITY_VALUES);
    }

    if (filters.dueBefore && !isValidDateOnly(filters.dueBefore)) {
      throw new ValidationError("dueBefore filter must use YYYY-MM-DD.");
    }

    return filters;
  }

  function validateSearchQuery(query) {
    const normalized = normalizeText(query);
    if (normalized.length > LIMITS.query) {
      throw new ValidationError(
        `Search query max length is ${LIMITS.query} characters.`
      );
    }
    return normalized.toLowerCase();
  }

  class TaskStorage {
    constructor(storage = window.localStorage, key = STORAGE_KEY) {
      this.storage = storage;
      this.key = key;
      this.lock = Promise.resolve();
    }

    async withLock(job) {
      const run = async () => {
        await nextTick();
        return job();
      };

      this.lock = this.lock.then(run, run);
      return this.lock;
    }

    async readAll() {
      return this.withLock(() => {
        try {
          const raw = this.storage.getItem(this.key);
          if (!raw) {
            return [];
          }

          const parsed = JSON.parse(raw);
          if (!Array.isArray(parsed)) {
            throw new Error("Stored value is not an array.");
          }

          return parsed;
        } catch (error) {
          throw new StorageError(
            `Failed to read tasks from storage: ${error.message}`
          );
        }
      });
    }

    async writeAll(tasks) {
      return this.withLock(() => {
        if (!Array.isArray(tasks)) {
          throw new StorageError("writeAll expects an array of tasks.");
        }

        try {
          this.storage.setItem(this.key, JSON.stringify(tasks));
        } catch (error) {
          throw new StorageError(
            `Failed to write tasks to storage: ${error.message}`
          );
        }
      });
    }
  }

  class TaskService {
    constructor(storage) {
      this.storage = storage;
    }

    async listTasks() {
      const records = await this.storage.readAll();
      const sanitized = records
        .map((record) => this.normalizeStoredTask(record))
        .filter(Boolean);

      return sanitized.sort((left, right) => this.compareTasks(left, right));
    }

    async addTask(rawInput) {
      const input = validateTaskInput(rawInput);
      const now = new Date().toISOString();
      const task = {
        id: createTaskId(),
        ...input,
        createdAt: now,
        updatedAt: now
      };

      const tasks = await this.listTasks();
      tasks.push(task);
      await this.storage.writeAll(tasks);
      return task;
    }

    async updateTask(taskId, rawPatch) {
      const id = normalizeText(taskId);
      if (!id) {
        throw new ValidationError("taskId is required for update.");
      }

      const patch = validateTaskInput(rawPatch, { partial: true });
      const tasks = await this.listTasks();
      const index = tasks.findIndex((task) => task.id === id);

      if (index === -1) {
        throw new NotFoundError(`Task with id "${id}" was not found.`);
      }

      tasks[index] = {
        ...tasks[index],
        ...patch,
        updatedAt: new Date().toISOString()
      };

      await this.storage.writeAll(tasks);
      return tasks[index];
    }

    async deleteTask(taskId) {
      const id = normalizeText(taskId);
      if (!id) {
        throw new ValidationError("taskId is required for delete.");
      }

      const tasks = await this.listTasks();
      const index = tasks.findIndex((task) => task.id === id);

      if (index === -1) {
        throw new NotFoundError(`Task with id "${id}" was not found.`);
      }

      const removed = tasks.splice(index, 1)[0];
      await this.storage.writeAll(tasks);
      return removed;
    }

    async filterTasks(rawFilters, dataset = null) {
      const filters = validateFilters(rawFilters);
      const tasks = Array.isArray(dataset) ? dataset : await this.listTasks();

      return tasks.filter((task) => {
        if (filters.status && task.status !== filters.status) {
          return false;
        }

        if (filters.priority && task.priority !== filters.priority) {
          return false;
        }

        if (filters.dueBefore) {
          if (!task.dueDate) {
            return false;
          }
          if (task.dueDate > filters.dueBefore) {
            return false;
          }
        }

        if (filters.tag && !task.tags.includes(filters.tag)) {
          return false;
        }

        return true;
      });
    }

    async searchTasks(rawQuery, dataset = null) {
      const query = validateSearchQuery(rawQuery);
      const tasks = Array.isArray(dataset) ? dataset : await this.listTasks();

      if (!query) {
        return tasks;
      }

      return tasks.filter((task) => {
        const haystack = [
          task.title,
          task.description,
          ...(Array.isArray(task.tags) ? task.tags : [])
        ]
          .join(" ")
          .toLowerCase();

        return haystack.includes(query);
      });
    }

    normalizeStoredTask(record) {
      if (!record || typeof record !== "object" || Array.isArray(record)) {
        return null;
      }

      const id = normalizeText(record.id);
      if (!id) {
        return null;
      }

      try {
        const validated = validateTaskInput({
          title: record.title,
          description: record.description ?? "",
          status: record.status ?? "todo",
          priority: record.priority ?? "medium",
          dueDate: record.dueDate ?? null,
          tags: record.tags ?? []
        });

        return {
          id,
          ...validated,
          createdAt: normalizeText(record.createdAt),
          updatedAt: normalizeText(record.updatedAt)
        };
      } catch {
        return null;
      }
    }

    compareTasks(left, right) {
      const leftPriority = PRIORITY_ORDER[left.priority] ?? 99;
      const rightPriority = PRIORITY_ORDER[right.priority] ?? 99;
      if (leftPriority !== rightPriority) {
        return leftPriority - rightPriority;
      }

      const leftDue = left.dueDate || "9999-12-31";
      const rightDue = right.dueDate || "9999-12-31";
      if (leftDue !== rightDue) {
        return leftDue.localeCompare(rightDue);
      }

      return left.title.localeCompare(right.title);
    }
  }

  function emptyFilters() {
    return {
      status: "",
      priority: "",
      dueBefore: "",
      tag: ""
    };
  }

  class TaskManagerUI {
    constructor(service, doc = document) {
      this.service = service;
      this.doc = doc;
      this.state = {
        busy: false,
        selectedTaskId: null,
        visibleTasks: [],
        filters: emptyFilters(),
        query: ""
      };

      this.elements = this.collectElements();
    }

    collectElements() {
      const byId = (id) => this.doc.getElementById(id);
      const elements = {
        form: byId("task-form"),
        taskId: byId("task-id"),
        title: byId("title"),
        description: byId("description"),
        status: byId("status"),
        priority: byId("priority"),
        dueDate: byId("due-date"),
        tags: byId("tags"),
        saveBtn: byId("save-btn"),
        resetBtn: byId("reset-btn"),
        deleteSelectedBtn: byId("delete-selected-btn"),
        commandButtons: this.doc.querySelectorAll(".cmd-btn"),
        searchQuery: byId("search-query"),
        filterStatus: byId("filter-status"),
        filterPriority: byId("filter-priority"),
        filterDueBefore: byId("filter-due-before"),
        filterTag: byId("filter-tag"),
        clearFiltersBtn: byId("clear-filters-btn"),
        statusMessage: byId("status-message"),
        tableBody: byId("task-table-body"),
        emptyState: byId("empty-state"),
        rowTemplate: byId("task-row-template")
      };

      for (const [key, value] of Object.entries(elements)) {
        if (!value || (value instanceof NodeList && value.length === 0)) {
          throw new Error(`Required UI element is missing: ${key}`);
        }
      }

      return elements;
    }

    async init() {
      this.bindEvents();
      await this.executeSafely("list", async () => {
        await this.refreshView();
      });
    }

    bindEvents() {
      this.elements.form.addEventListener("submit", async (event) => {
        event.preventDefault();
        await this.handleSave();
      });

      this.elements.resetBtn.addEventListener("click", () => {
        this.resetForm();
        this.setStatus("Editor reset. Ready for add command.", "neutral");
      });

      this.elements.deleteSelectedBtn.addEventListener("click", async () => {
        await this.handleDeleteSelected();
      });

      this.elements.clearFiltersBtn.addEventListener("click", async () => {
        this.clearFilterInputs();
        this.state.filters = emptyFilters();
        this.state.query = "";
        await this.executeSafely("list", async () => {
          await this.refreshView();
        });
      });

      this.elements.commandButtons.forEach((button) => {
        button.addEventListener("click", async () => {
          const command = button.dataset.command;
          await this.handleCommand(command);
        });
      });

      this.elements.tableBody.addEventListener("click", async (event) => {
        const trigger = event.target.closest("button");
        if (!trigger) {
          return;
        }

        const row = trigger.closest("tr");
        if (!row) {
          return;
        }

        const id = row.dataset.taskId || "";
        if (!id) {
          return;
        }

        if (trigger.classList.contains("edit-btn")) {
          this.loadTaskIntoForm(id);
          return;
        }

        if (trigger.classList.contains("delete-btn")) {
          await this.executeSafely("delete", async () => {
            if (!window.confirm("Delete this task permanently?")) {
              return { cancelled: true, message: "Delete command cancelled." };
            }
            await this.service.deleteTask(id);
            if (this.state.selectedTaskId === id) {
              this.resetForm();
            }
            await this.refreshView();
          });
        }
      });
    }

    async handleCommand(command) {
      if (!command) {
        return;
      }

      switch (command) {
        case "add":
          await this.handleSave({ forceMode: "add" });
          break;
        case "list":
          this.clearFilterInputs();
          this.state.filters = emptyFilters();
          this.state.query = "";
          await this.executeSafely("list", async () => {
            await this.refreshView();
          });
          break;
        case "update":
          await this.handleSave({ forceMode: "update" });
          break;
        case "delete":
          await this.handleDeleteSelected();
          break;
        case "filter":
          this.state.filters = this.readFilterInputs();
          await this.executeSafely("filter", async () => {
            await this.refreshView();
          });
          break;
        case "search":
          this.state.query = normalizeText(this.elements.searchQuery.value);
          await this.executeSafely("search", async () => {
            await this.refreshView();
          });
          break;
        default:
          this.setStatus(`Unknown command: ${command}`, "error");
      }
    }

    async handleSave({ forceMode } = {}) {
      await this.executeSafely(forceMode || "save", async () => {
        const mode = forceMode || (this.state.selectedTaskId ? "update" : "add");
        const payload = this.readTaskForm();

        if (mode === "update") {
          const id = this.state.selectedTaskId;
          if (!id) {
            throw new ValidationError(
              "Select a task from the table first if you want to use update."
            );
          }
          await this.service.updateTask(id, payload);
        } else {
          await this.service.addTask(payload);
          this.resetForm();
        }

        await this.refreshView();
      });
    }

    async handleDeleteSelected() {
      await this.executeSafely("delete", async () => {
        const id = this.state.selectedTaskId;
        if (!id) {
          throw new ValidationError("No task selected for delete.");
        }

        if (!window.confirm("Delete selected task permanently?")) {
          return { cancelled: true, message: "Delete command cancelled." };
        }

        await this.service.deleteTask(id);
        this.resetForm();
        await this.refreshView();
      });
    }

    async refreshView() {
      let tasks = await this.service.listTasks();

      if (hasAnyFilter(this.state.filters)) {
        tasks = await this.service.filterTasks(this.state.filters, tasks);
      }

      if (this.state.query) {
        tasks = await this.service.searchTasks(this.state.query, tasks);
      }

      this.state.visibleTasks = tasks;
      this.renderTasks(tasks);
    }

    readTaskForm() {
      return {
        title: this.elements.title.value,
        description: this.elements.description.value,
        status: this.elements.status.value,
        priority: this.elements.priority.value,
        dueDate: this.elements.dueDate.value,
        tags: this.elements.tags.value
      };
    }

    readFilterInputs() {
      return {
        status: this.elements.filterStatus.value,
        priority: this.elements.filterPriority.value,
        dueBefore: this.elements.filterDueBefore.value,
        tag: this.elements.filterTag.value
      };
    }

    clearFilterInputs() {
      this.elements.searchQuery.value = "";
      this.elements.filterStatus.value = "";
      this.elements.filterPriority.value = "";
      this.elements.filterDueBefore.value = "";
      this.elements.filterTag.value = "";
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
      this.elements.dueDate.value = task.dueDate || "";
      this.elements.tags.value = task.tags.join(", ");
      this.elements.saveBtn.textContent = "Update Task";
      this.setStatus(`Task "${task.title}" loaded for update command.`, "neutral");
      this.renderTasks(this.state.visibleTasks);
    }

    resetForm() {
      this.elements.form.reset();
      this.elements.status.value = "todo";
      this.elements.priority.value = "medium";
      this.elements.taskId.value = "";
      this.elements.saveBtn.textContent = "Add Task";
      this.state.selectedTaskId = null;
      this.renderTasks(this.state.visibleTasks);
    }

    renderTasks(tasks) {
      this.elements.tableBody.textContent = "";
      this.elements.emptyState.style.display = tasks.length > 0 ? "none" : "block";

      tasks.forEach((task) => {
        const fragment = this.elements.rowTemplate.content.cloneNode(true);
        const row = fragment.querySelector("tr");
        row.dataset.taskId = task.id;

        const idCell = fragment.querySelector(".id-cell");
        idCell.textContent = task.id;

        const titleCell = fragment.querySelector(".title-cell");
        const titleText = titleCell.querySelector(".title-text");
        const descriptionText = titleCell.querySelector(".description-text");
        titleText.textContent = task.title;
        descriptionText.textContent = task.description || "No description";
        if (this.state.selectedTaskId === task.id) {
          row.style.background = "rgba(15, 118, 110, 0.08)";
        }

        const statusPill = fragment.querySelector(".status-cell .pill");
        statusPill.textContent = task.status;
        statusPill.classList.add(`pill-status-${task.status}`);

        const priorityPill = fragment.querySelector(".priority-cell .pill");
        priorityPill.textContent = task.priority;
        priorityPill.classList.add(`pill-priority-${task.priority}`);

        const dueCell = fragment.querySelector(".due-cell");
        dueCell.textContent = formatDate(task.dueDate);

        const tagsCell = fragment.querySelector(".tags-cell");
        tagsCell.textContent = formatTags(task.tags);

        this.elements.tableBody.appendChild(fragment);
      });
    }

    async executeSafely(commandName, handler) {
      if (this.state.busy) {
        return;
      }

      this.setBusy(true);
      try {
        const result = await handler();
        if (result && result.cancelled) {
          this.setStatus(
            result.message || `Command "${commandName}" cancelled.`,
            "neutral"
          );
          return;
        }
        this.setStatus(`Command "${commandName}" executed successfully.`, "success");
      } catch (error) {
        this.handleError(error);
      } finally {
        this.setBusy(false);
      }
    }

    setBusy(flag) {
      this.state.busy = flag;
      this.elements.commandButtons.forEach((button) => {
        button.disabled = flag;
      });
      this.elements.saveBtn.disabled = flag;
      this.elements.resetBtn.disabled = flag;
      this.elements.deleteSelectedBtn.disabled = flag;
      this.elements.clearFiltersBtn.disabled = flag;
    }

    setStatus(message, tone = "neutral") {
      this.elements.statusMessage.textContent = message;
      this.elements.statusMessage.dataset.tone = tone;
    }

    handleError(error) {
      console.error(error);

      const message =
        error && typeof error.message === "string" && error.message.trim()
          ? error.message
          : "Unexpected error occurred.";

      this.setStatus(message, "error");
    }
  }

  async function bootstrap() {
    const storage = new TaskStorage(window.localStorage);
    const service = new TaskService(storage);
    const ui = new TaskManagerUI(service);
    await ui.init();
  }

  window.addEventListener("DOMContentLoaded", async () => {
    try {
      await bootstrap();
    } catch (error) {
      console.error(error);
      const fallback = document.getElementById("status-message");
      if (fallback) {
        fallback.textContent =
          "Fatal startup error. Open browser console for technical details.";
        fallback.dataset.tone = "error";
      }
    }
  });
})();
