import { ValidationError } from "./errors.js";
import { TaskService } from "./service.js";
import {
  Task,
  TaskFilters,
  TaskId,
  TaskQueryOptions,
  TaskSort,
  TaskStatistics
} from "./types.js";
import {
  normalizeDependencyList,
  normalizeTagList,
  normalizeText
} from "./utils.js";

type CommandName = "add" | "list" | "update" | "delete" | "filter" | "search" | "sort";
type Tone = "neutral" | "success" | "error";

interface UIElements {
  form: HTMLFormElement;
  taskId: HTMLInputElement;
  title: HTMLInputElement;
  description: HTMLTextAreaElement;
  status: HTMLSelectElement;
  priority: HTMLSelectElement;
  category: HTMLSelectElement;
  dueDate: HTMLInputElement;
  tags: HTMLInputElement;
  dependencies: HTMLInputElement;
  recurrenceFrequency: HTMLSelectElement;
  recurrenceInterval: HTMLInputElement;
  recurrenceEndDate: HTMLInputElement;
  saveBtn: HTMLButtonElement;
  resetBtn: HTMLButtonElement;
  deleteSelectedBtn: HTMLButtonElement;
  clearQueryBtn: HTMLButtonElement;
  commandButtons: NodeListOf<HTMLButtonElement>;
  searchQuery: HTMLInputElement;
  filterStatus: HTMLSelectElement;
  filterPriority: HTMLSelectElement;
  filterCategory: HTMLSelectElement;
  filterDueBefore: HTMLInputElement;
  filterTag: HTMLInputElement;
  sortBy: HTMLSelectElement;
  sortDirection: HTMLSelectElement;
  statusMessage: HTMLParagraphElement;
  tableBody: HTMLTableSectionElement;
  emptyState: HTMLParagraphElement;
  rowTemplate: HTMLTemplateElement;
  statTotal: HTMLElement;
  statCompleted: HTMLElement;
  statBlocked: HTMLElement;
  statOverdue: HTMLElement;
  statRate: HTMLElement;
}

interface UIState {
  busy: boolean;
  selectedTaskId: TaskId | null;
  visibleTasks: Task[];
  query: TaskQueryOptions;
}

function defaultFilters(): TaskFilters {
  return {
    status: "",
    priority: "",
    category: "",
    dueBefore: "",
    tag: ""
  };
}

function defaultSort(): TaskSort {
  return {
    by: "priority",
    direction: "asc"
  };
}

function defaultQueryOptions(): TaskQueryOptions {
  return {
    search: "",
    filters: defaultFilters(),
    sort: defaultSort()
  };
}

function recurrenceText(task: Task): string {
  if (task.recurrence.frequency === "none") {
    return "none";
  }

  const end = task.recurrence.endDate ? `, until ${task.recurrence.endDate}` : "";
  return `${task.recurrence.frequency} / ${task.recurrence.interval}${end}`;
}

export class TaskManagerUI {
  private readonly service: TaskService;
  private readonly doc: Document;
  private readonly elements: UIElements;
  private readonly state: UIState;
  private searchTimer: number | null = null;

  constructor(service: TaskService, doc: Document = document) {
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

  public async init(): Promise<void> {
    this.bindEvents();
    await this.executeSafely("list", async () => {
      await this.refreshView();
    });
  }

  private byId<TElement extends HTMLElement>(id: string): TElement {
    const node = this.doc.getElementById(id);
    if (!node) {
      throw new Error(`Missing UI element: ${id}`);
    }
    return node as TElement;
  }

  private collectElements(): UIElements {
    const commandButtons = this.doc.querySelectorAll<HTMLButtonElement>(".cmd-btn");
    if (commandButtons.length === 0) {
      throw new Error("No command buttons found.");
    }

    return {
      form: this.byId<HTMLFormElement>("task-form"),
      taskId: this.byId<HTMLInputElement>("task-id"),
      title: this.byId<HTMLInputElement>("title"),
      description: this.byId<HTMLTextAreaElement>("description"),
      status: this.byId<HTMLSelectElement>("status"),
      priority: this.byId<HTMLSelectElement>("priority"),
      category: this.byId<HTMLSelectElement>("category"),
      dueDate: this.byId<HTMLInputElement>("due-date"),
      tags: this.byId<HTMLInputElement>("tags"),
      dependencies: this.byId<HTMLInputElement>("dependencies"),
      recurrenceFrequency: this.byId<HTMLSelectElement>("recurrence-frequency"),
      recurrenceInterval: this.byId<HTMLInputElement>("recurrence-interval"),
      recurrenceEndDate: this.byId<HTMLInputElement>("recurrence-end-date"),
      saveBtn: this.byId<HTMLButtonElement>("save-btn"),
      resetBtn: this.byId<HTMLButtonElement>("reset-btn"),
      deleteSelectedBtn: this.byId<HTMLButtonElement>("delete-selected-btn"),
      clearQueryBtn: this.byId<HTMLButtonElement>("clear-query-btn"),
      commandButtons,
      searchQuery: this.byId<HTMLInputElement>("search-query"),
      filterStatus: this.byId<HTMLSelectElement>("filter-status"),
      filterPriority: this.byId<HTMLSelectElement>("filter-priority"),
      filterCategory: this.byId<HTMLSelectElement>("filter-category"),
      filterDueBefore: this.byId<HTMLInputElement>("filter-due-before"),
      filterTag: this.byId<HTMLInputElement>("filter-tag"),
      sortBy: this.byId<HTMLSelectElement>("sort-by"),
      sortDirection: this.byId<HTMLSelectElement>("sort-direction"),
      statusMessage: this.byId<HTMLParagraphElement>("status-message"),
      tableBody: this.byId<HTMLTableSectionElement>("task-table-body"),
      emptyState: this.byId<HTMLParagraphElement>("empty-state"),
      rowTemplate: this.byId<HTMLTemplateElement>("task-row-template"),
      statTotal: this.byId<HTMLElement>("stat-total"),
      statCompleted: this.byId<HTMLElement>("stat-completed"),
      statBlocked: this.byId<HTMLElement>("stat-blocked"),
      statOverdue: this.byId<HTMLElement>("stat-overdue"),
      statRate: this.byId<HTMLElement>("stat-rate")
    };
  }

  private bindEvents(): void {
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
      this.clearPendingSearch();
      this.resetQueryInputs();
      this.state.query = defaultQueryOptions();
      await this.executeSafely("list", async () => {
        await this.refreshView();
      });
    });

    this.elements.searchQuery.addEventListener("input", () => {
      this.queueSearch();
    });

    this.elements.searchQuery.addEventListener("keydown", async (event) => {
      if (event.key !== "Enter") {
        return;
      }

      event.preventDefault();
      this.clearPendingSearch();
      this.state.query.search = normalizeText(this.elements.searchQuery.value);
      await this.runSearch();
    });

    this.elements.commandButtons.forEach((button) => {
      button.addEventListener("click", async () => {
        const command = button.dataset.command as CommandName | undefined;
        if (!command) {
          return;
        }
        await this.handleCommand(command);
      });
    });

    this.elements.tableBody.addEventListener("click", async (event) => {
      const trigger = (event.target as HTMLElement).closest("button");
      if (!trigger) {
        return;
      }

      const row = trigger.closest("tr") as HTMLTableRowElement | null;
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

  private async handleCommand(command: CommandName): Promise<void> {
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
        await this.runSearch();
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

  private async handleSave(options: { forcedMode?: "add" | "update" } = {}): Promise<void> {
    await this.executeSafely(options.forcedMode ?? "add", async () => {
      const mode = options.forcedMode ?? (this.state.selectedTaskId ? "update" : "add");
      const taskCore = this.service.createTaskCoreFromInput(this.readFormRawValues());

      if (mode === "update") {
        const selectedId = this.state.selectedTaskId;
        if (!selectedId) {
          throw new ValidationError("Select task before update.");
        }
        await this.service.updateTask(selectedId, taskCore);
      } else {
        await this.service.addTask(taskCore);
        this.resetForm();
      }

      await this.refreshView();
    });
  }

  private async handleDeleteSelected(): Promise<void> {
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

  private readFormRawValues(): {
    title: string;
    description: string;
    status: string;
    priority: string;
    category: string;
    dueDate: string;
    tags: string[];
    dependencies: string[];
    recurrenceFrequency: string;
    recurrenceInterval: string;
    recurrenceEndDate: string;
  } {
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

  private readFilterInputs(): TaskFilters {
    return {
      status: this.elements.filterStatus.value as TaskFilters["status"],
      priority: this.elements.filterPriority.value as TaskFilters["priority"],
      category: this.elements.filterCategory.value as TaskFilters["category"],
      dueBefore: this.elements.filterDueBefore.value,
      tag: normalizeText(this.elements.filterTag.value)
    };
  }

  private readSortInputs(): TaskSort {
    return {
      by: this.elements.sortBy.value as TaskSort["by"],
      direction: this.elements.sortDirection.value as TaskSort["direction"]
    };
  }

  private queueSearch(): void {
    this.state.query.search = normalizeText(this.elements.searchQuery.value);
    this.clearPendingSearch();
    this.searchTimer = window.setTimeout(() => {
      this.searchTimer = null;
      void this.runSearch();
    }, 150);
  }

  private clearPendingSearch(): void {
    if (this.searchTimer !== null) {
      window.clearTimeout(this.searchTimer);
      this.searchTimer = null;
    }
  }

  private async runSearch(): Promise<void> {
    await this.executeSafely("search", async () => {
      await this.refreshView();
    });
  }

  private resetQueryInputs(): void {
    this.elements.searchQuery.value = "";
    this.elements.filterStatus.value = "";
    this.elements.filterPriority.value = "";
    this.elements.filterCategory.value = "";
    this.elements.filterDueBefore.value = "";
    this.elements.filterTag.value = "";
    this.elements.sortBy.value = "priority";
    this.elements.sortDirection.value = "asc";
  }

  private loadTaskIntoForm(taskId: TaskId): void {
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

  private resetForm(): void {
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

  private renderTasks(tasks: Task[]): void {
    this.elements.tableBody.textContent = "";
    this.elements.emptyState.style.display = tasks.length > 0 ? "none" : "block";

    for (const task of tasks) {
      const fragment = this.elements.rowTemplate.content.cloneNode(true) as DocumentFragment;
      const row = fragment.querySelector("tr") as HTMLTableRowElement | null;
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

      if (
        !idCell ||
        !titleCell ||
        !descriptionCell ||
        !statusPill ||
        !priorityPill ||
        !categoryCell ||
        !dueCell ||
        !recurrenceCell ||
        !dependenciesCell ||
        !tagsCell
      ) {
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

  private renderStatistics(stats: TaskStatistics): void {
    this.elements.statTotal.textContent = String(stats.total);
    this.elements.statCompleted.textContent = String(stats.completed);
    this.elements.statBlocked.textContent = String(stats.blocked);
    this.elements.statOverdue.textContent = String(stats.overdue);
    this.elements.statRate.textContent = `${stats.completionRate}%`;
  }

  private async refreshView(): Promise<void> {
    const tasks = await this.service.queryTasks(this.state.query);
    const stats = await this.service.getStatistics(tasks);
    this.state.visibleTasks = tasks;
    this.renderTasks(tasks);
    this.renderStatistics(stats);
  }

  private async executeSafely(
    command: CommandName | "startup",
    action: () => Promise<void | { cancelled: true; message?: string }>
  ): Promise<void> {
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
    } catch (error) {
      console.error(error);
      const message =
        error instanceof Error && error.message.trim()
          ? error.message
          : "Unexpected error occurred.";
      this.setStatus(message, "error");
    } finally {
      this.setBusy(false);
    }
  }

  private setBusy(busy: boolean): void {
    this.state.busy = busy;
    this.elements.commandButtons.forEach((button) => {
      button.disabled = busy;
    });
    this.elements.saveBtn.disabled = busy;
    this.elements.resetBtn.disabled = busy;
    this.elements.deleteSelectedBtn.disabled = busy;
    this.elements.clearQueryBtn.disabled = busy;
  }

  private setStatus(message: string, tone: Tone): void {
    this.elements.statusMessage.textContent = message;
    this.elements.statusMessage.dataset.tone = tone;
  }
}
