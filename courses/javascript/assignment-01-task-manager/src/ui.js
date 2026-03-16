import { formatDate, formatTags, hasAnyFilter, normalizeText } from "./utils.js";
import { ValidationError } from "./errors.js";

function emptyFilters() {
  return {
    status: "",
    priority: "",
    dueBefore: "",
    tag: ""
  };
}

export class TaskManagerUI {
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
    this.elements.title.addEventListener("input", () => {
      this.setFieldError(this.elements.title, false);
    });

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
    this.setFieldError(this.elements.title, false);
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
        this.setStatus(result.message || `Command "${commandName}" cancelled.`, "neutral");
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

    this.setFieldError(this.elements.title, message === "Title is required.");
    this.setStatus(message, "error");
  }

  setFieldError(element, hasError) {
    element.classList.toggle("input-error", hasError);
    element.setAttribute("aria-invalid", hasError ? "true" : "false");
  }
}
