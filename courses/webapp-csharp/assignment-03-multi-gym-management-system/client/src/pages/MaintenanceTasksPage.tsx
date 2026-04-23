import { type FormEvent, useEffect, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import { useLanguage } from "../lib/language";
import type { Equipment, MaintenanceTask, Notice, Staff } from "../lib/types";
import { getErrorMessages, MaintenancePriority, MaintenanceTaskStatus, MaintenanceTaskType } from "../lib/types";

export function MaintenanceTasksPage() {
  const { api, session } = useAuth();
  const { t } = useLanguage();
  const [tasks, setTasks] = useState<MaintenanceTask[]>([]);
  const [equipment, setEquipment] = useState<Equipment[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [createForm, setCreateForm] = useState(createDefaultTaskForm());
  const [statusByTaskId, setStatusByTaskId] = useState<Record<string, MaintenanceTaskStatus>>({});
  const [notesByTaskId, setNotesByTaskId] = useState<Record<string, string>>({});
  const [completionNotesByTaskId, setCompletionNotesByTaskId] = useState<Record<string, string>>({});
  const [assignedStaffByTaskId, setAssignedStaffByTaskId] = useState<Record<string, string>>({});
  const [assignmentNotesByTaskId, setAssignmentNotesByTaskId] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [isGeneratingDue, setIsGeneratingDue] = useState(false);
  const [statusUpdatingTaskId, setStatusUpdatingTaskId] = useState<string | null>(null);
  const [assignmentUpdatingTaskId, setAssignmentUpdatingTaskId] = useState<string | null>(null);
  const [historyLoadingTaskId, setHistoryLoadingTaskId] = useState<string | null>(null);
  const [deletingTaskId, setDeletingTaskId] = useState<string | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const canAssignStaff = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner";
  const canManageTasks = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner";

  useEffect(() => {
    void loadTasks();
  }, []);

  async function loadTasks() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      const [loadedTasks, loadedEquipment, loadedStaff] = await Promise.all([
        api.getMaintenanceTasks(session.activeGymCode),
        api.getEquipment(session.activeGymCode),
        canAssignStaff ? api.getStaff(session.activeGymCode) : Promise.resolve([]),
      ]);
      setTasks(loadedTasks);
      setEquipment(loadedEquipment);
      setStaff(loadedStaff);
      setCreateForm((current) => ({
        ...current,
        equipmentId: current.equipmentId || loadedEquipment[0]?.id || "",
        assignedStaffId: current.assignedStaffId || loadedStaff[0]?.id || "",
      }));
      setStatusByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, task.status])));
      setNotesByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, task.notes ?? ""])));
      setCompletionNotesByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, task.completionNotes ?? ""])));
      setAssignedStaffByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, task.assignedStaffId ?? ""])));
      setAssignmentNotesByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, ""])));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load maintenance tasks.");
    } finally {
      setIsLoading(false);
    }
  }

  async function createTask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session?.activeGymCode || !createForm.equipmentId) {
      setNotice({
        tone: "error",
        title: "Could not schedule maintenance",
        messages: ["Equipment is required before a maintenance task can be scheduled."],
      });
      return;
    }

    setIsCreating(true);
    setNotice(null);

    try {
      const created = await api.createMaintenanceTask(session.activeGymCode, {
        equipmentId: createForm.equipmentId,
        assignedStaffId: createForm.assignedStaffId || null,
        taskType: Number(createForm.taskType) as MaintenanceTaskType,
        priority: Number(createForm.priority) as MaintenancePriority,
        status: MaintenanceTaskStatus.Open,
        dueAtUtc: createForm.dueAt ? new Date(createForm.dueAt).toISOString() : null,
        notes: createForm.notes.trim() || null,
      });

      setTasks((current) => [created, ...current]);
      setStatusByTaskId((current) => ({ ...current, [created.id]: created.status }));
      setNotesByTaskId((current) => ({ ...current, [created.id]: created.notes ?? "" }));
      setCompletionNotesByTaskId((current) => ({ ...current, [created.id]: created.completionNotes ?? "" }));
      setAssignedStaffByTaskId((current) => ({ ...current, [created.id]: created.assignedStaffId ?? "" }));
      setCreateForm((current) => ({
        ...createDefaultTaskForm(),
        equipmentId: current.equipmentId,
        assignedStaffId: current.assignedStaffId,
      }));
      setNotice({
        tone: "success",
        title: "Maintenance scheduled",
        messages: [`${equipmentLabel(created)} has a new ${maintenanceTaskTypeLabel(created.taskType).toLowerCase()} task.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not schedule maintenance",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsCreating(false);
    }
  }

  async function updateTaskStatus(taskId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setStatusUpdatingTaskId(taskId);
    setNotice(null);

    try {
      const updated = await api.updateMaintenanceTaskStatus(session.activeGymCode, taskId, {
        status: statusByTaskId[taskId] ?? MaintenanceTaskStatus.Open,
        notes: notesByTaskId[taskId]?.trim() || null,
        completionNotes: completionNotesByTaskId[taskId]?.trim() || null,
      });

      setTasks((current) => current.map((task) => (task.id === updated.id ? updated : task)));
      setStatusByTaskId((current) => ({ ...current, [updated.id]: updated.status }));
      setNotesByTaskId((current) => ({ ...current, [updated.id]: updated.notes ?? "" }));
      setCompletionNotesByTaskId((current) => ({ ...current, [updated.id]: updated.completionNotes ?? "" }));
      setNotice({
        tone: "success",
        title: "Maintenance task updated",
        messages: [`${equipmentLabel(updated)} is now ${maintenanceStatusLabel(updated.status)}.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not update maintenance task",
        messages: getErrorMessages(error),
      });
    } finally {
      setStatusUpdatingTaskId(null);
    }
  }

  async function updateTaskAssignment(taskId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setAssignmentUpdatingTaskId(taskId);
    setNotice(null);

    try {
      const updated = await api.updateMaintenanceTaskAssignment(session.activeGymCode, taskId, {
        assignedStaffId: assignedStaffByTaskId[taskId] || null,
        assignedByStaffId: null,
        notes: assignmentNotesByTaskId[taskId]?.trim() || null,
      });

      setTasks((current) => current.map((task) => (task.id === updated.id ? updated : task)));
      setAssignedStaffByTaskId((current) => ({ ...current, [updated.id]: updated.assignedStaffId ?? "" }));
      setAssignmentNotesByTaskId((current) => ({ ...current, [updated.id]: "" }));
      setNotice({
        tone: "success",
        title: "Assignment updated",
        messages: [updated.assignedStaffName ? `Task assigned to ${updated.assignedStaffName}.` : "Task is now unassigned."],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not update assignment",
        messages: getErrorMessages(error),
      });
    } finally {
      setAssignmentUpdatingTaskId(null);
    }
  }

  async function refreshAssignmentHistory(taskId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setHistoryLoadingTaskId(taskId);

    try {
      const history = await api.getMaintenanceTaskAssignmentHistory(session.activeGymCode, taskId);
      setTasks((current) => current.map((task) => (task.id === taskId ? { ...task, assignmentHistory: history } : task)));
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not refresh assignment history",
        messages: getErrorMessages(error),
      });
    } finally {
      setHistoryLoadingTaskId(null);
    }
  }

  async function generateDueTasks() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsGeneratingDue(true);
    setNotice(null);

    try {
      const response = await api.generateDueMaintenanceTasks(session.activeGymCode);
      await loadTasks();
      setNotice({
        tone: "success",
        title: "Recurring maintenance generated",
        messages: response.messages,
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not generate recurring maintenance tasks",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsGeneratingDue(false);
    }
  }

  async function deleteTask(taskId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    const task = tasks.find((entry) => entry.id === taskId);
    if (!task) {
      return;
    }

    if (!window.confirm(`Delete maintenance task for ${equipmentLabel(task)}?`)) {
      return;
    }

    setDeletingTaskId(taskId);
    setNotice(null);

    try {
      await api.deleteMaintenanceTask(session.activeGymCode, taskId);
      setTasks((current) => current.filter((taskEntry) => taskEntry.id !== taskId));
      setNotice({
        tone: "success",
        title: "Maintenance task deleted",
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not delete maintenance task",
        messages: getErrorMessages(error),
      });
    } finally {
      setDeletingTaskId(null);
    }
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">{t("Caretaker workflow")}</p>
          <h2 className="workspace__title">Maintenance workspace</h2>
          <p className="workspace__copy">Manage recurring and breakdown work with assignment history, downtime tracking, and completion notes.</p>
        </div>
        {canManageTasks ? (
          <button className="button button--secondary" disabled={isGeneratingDue} onClick={() => void generateDueTasks()} type="button">
            {isGeneratingDue ? "Generating..." : "Generate due tasks"}
          </button>
        ) : null}
      </header>

      <NoticeBanner notice={notice} />

      <section className="panel">
        <div className="editor-header">
          <div>
            <p className="workspace__eyebrow">{t("Schedule")}</p>
            <h3>{t("New maintenance task")}</h3>
          </div>
        </div>

        <form className="form" onSubmit={(event) => void createTask(event)}>
          <div className="form__row">
            <label className="field">
              <span>{t("Equipment")}</span>
              <select
                disabled={isCreating || equipment.length === 0}
                onChange={(event) => setCreateForm((current) => ({ ...current, equipmentId: event.target.value }))}
                value={createForm.equipmentId}
              >
                {equipment.map((item) => (
                  <option key={item.id} value={item.id}>
                    {equipmentLabel(item)}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>{t("Assigned to")}</span>
              <select
                disabled={isCreating || !canAssignStaff || staff.length === 0}
                onChange={(event) => setCreateForm((current) => ({ ...current, assignedStaffId: event.target.value }))}
                value={createForm.assignedStaffId}
              >
                <option value="">{t("Unassigned")}</option>
                {staff.map((staffMember) => (
                  <option key={staffMember.id} value={staffMember.id}>
                    {staffMember.fullName} / {staffMember.staffCode}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="form__row">
            <label className="field">
              <span>{t("Task type")}</span>
              <select
                disabled={isCreating}
                onChange={(event) => setCreateForm((current) => ({ ...current, taskType: event.target.value }))}
                value={createForm.taskType}
              >
                <option value={MaintenanceTaskType.Scheduled}>{t("Scheduled")}</option>
                <option value={MaintenanceTaskType.Breakdown}>{t("Breakdown")}</option>
              </select>
            </label>
            <label className="field">
              <span>{t("Priority")}</span>
              <select
                disabled={isCreating}
                onChange={(event) => setCreateForm((current) => ({ ...current, priority: event.target.value }))}
                value={createForm.priority}
              >
                <option value={MaintenancePriority.Low}>{t("Low")}</option>
                <option value={MaintenancePriority.Medium}>{t("Medium")}</option>
                <option value={MaintenancePriority.High}>{t("High")}</option>
                <option value={MaintenancePriority.Critical}>{t("Critical")}</option>
              </select>
            </label>
          </div>

          <label className="field">
            <span>{t("Due")}</span>
            <input
              disabled={isCreating}
              onChange={(event) => setCreateForm((current) => ({ ...current, dueAt: event.target.value }))}
              type="datetime-local"
              value={createForm.dueAt}
            />
          </label>

          <label className="field">
            <span>{t("Notes")}</span>
            <textarea
              disabled={isCreating}
              onChange={(event) => setCreateForm((current) => ({ ...current, notes: event.target.value }))}
              rows={3}
              value={createForm.notes}
            />
          </label>

          <div className="form__actions">
            <button className="button" disabled={isCreating || equipment.length === 0} type="submit">
              {isCreating ? t("Scheduling...") : t("Schedule maintenance")}
            </button>
            {equipment.length === 0 ? <span className="state">{t("Add equipment before scheduling maintenance.")}</span> : null}
          </div>
        </form>
      </section>

      <section className="panel panel--list">
        {pageError ? <p className="state state--error">{pageError}</p> : null}
        {isLoading ? <p className="state">{t("Loading maintenance tasks...")}</p> : null}
        {!isLoading && tasks.length === 0 ? <p className="state">{t("No maintenance tasks are assigned in this gym.")}</p> : null}

        <div className="record-list" role="list">
          {tasks.map((task) => (
            <article className="record-card record-card--wide" key={task.id} role="listitem">
              <div className="record-card__body">
                <strong>{equipmentLabel(task)}</strong>
                <span>{maintenanceTaskTypeLabel(task.taskType)} maintenance</span>
                <span>
                  Priority {maintenancePriorityLabel(task.priority)} / current status {maintenanceStatusLabel(task.status)}
                  {task.isOverdue ? " / overdue" : ""}
                </span>
                <span>
                  {task.dueAtUtc ? `Due ${formatDateTime(task.dueAtUtc)}` : "No due date set"}
                  {task.assignedStaffName ? ` / ${task.assignedStaffName}` : ""}
                </span>
                <span>
                  Downtime:{" "}
                  {task.downtimeStartedAtUtc
                    ? `${formatDateTime(task.downtimeStartedAtUtc)}${task.downtimeEndedAtUtc ? ` - ${formatDateTime(task.downtimeEndedAtUtc)}` : " - ongoing"}`
                    : "none"}
                </span>
                {task.completionNotes ? <span>Completion notes: {task.completionNotes}</span> : null}
              </div>

              <div className="inline-controls inline-controls--wide">
                <label className="field field--compact">
                  <span>{t("Status")}</span>
                  <select
                    disabled={statusUpdatingTaskId === task.id}
                    onChange={(event) =>
                      setStatusByTaskId((current) => ({
                        ...current,
                        [task.id]: Number(event.target.value) as MaintenanceTaskStatus,
                      }))
                    }
                    value={statusByTaskId[task.id] ?? task.status}
                  >
                    <option value={MaintenanceTaskStatus.Open}>Open</option>
                    <option value={MaintenanceTaskStatus.InProgress}>In progress</option>
                    <option value={MaintenanceTaskStatus.Done}>Done</option>
                  </select>
                </label>
                <label className="field field--notes">
                  <span>{t("Notes")}</span>
                  <textarea
                    disabled={statusUpdatingTaskId === task.id}
                    onChange={(event) => setNotesByTaskId((current) => ({ ...current, [task.id]: event.target.value }))}
                    rows={2}
                    value={notesByTaskId[task.id] ?? ""}
                  />
                </label>
                <label className="field field--notes">
                  <span>Completion notes</span>
                  <textarea
                    disabled={statusUpdatingTaskId === task.id}
                    onChange={(event) => setCompletionNotesByTaskId((current) => ({ ...current, [task.id]: event.target.value }))}
                    rows={2}
                    value={completionNotesByTaskId[task.id] ?? ""}
                  />
                </label>
                <button className="button" disabled={statusUpdatingTaskId === task.id} onClick={() => void updateTaskStatus(task.id)} type="button">
                  {statusUpdatingTaskId === task.id ? t("Saving...") : t("Update")}
                </button>
              </div>

              {canAssignStaff ? (
                <div className="inline-controls inline-controls--wide">
                  <label className="field field--compact">
                    <span>Assign staff</span>
                    <select
                      disabled={assignmentUpdatingTaskId === task.id}
                      onChange={(event) => setAssignedStaffByTaskId((current) => ({ ...current, [task.id]: event.target.value }))}
                      value={assignedStaffByTaskId[task.id] ?? ""}
                    >
                      <option value="">Unassigned</option>
                      {staff.map((staffMember) => (
                        <option key={staffMember.id} value={staffMember.id}>
                          {staffMember.fullName}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="field field--notes">
                    <span>Assignment note</span>
                    <input
                      disabled={assignmentUpdatingTaskId === task.id}
                      onChange={(event) => setAssignmentNotesByTaskId((current) => ({ ...current, [task.id]: event.target.value }))}
                      value={assignmentNotesByTaskId[task.id] ?? ""}
                    />
                  </label>
                  <button className="button" disabled={assignmentUpdatingTaskId === task.id} onClick={() => void updateTaskAssignment(task.id)} type="button">
                    {assignmentUpdatingTaskId === task.id ? "Saving..." : "Update assignment"}
                  </button>
                  <button
                    className="button button--ghost"
                    disabled={historyLoadingTaskId === task.id}
                    onClick={() => void refreshAssignmentHistory(task.id)}
                    type="button"
                  >
                    {historyLoadingTaskId === task.id ? "Refreshing..." : "Refresh history"}
                  </button>
                  {canManageTasks ? (
                    <button className="button button--danger" disabled={deletingTaskId === task.id} onClick={() => void deleteTask(task.id)} type="button">
                      {deletingTaskId === task.id ? "Deleting..." : "Delete"}
                    </button>
                  ) : null}
                </div>
              ) : null}

              {task.assignmentHistory.length > 0 ? (
                <div className="table-scroll">
                  <table className="mini-table">
                    <thead>
                      <tr>
                        <th>Assigned at</th>
                        <th>Assigned to</th>
                        <th>Assigned by</th>
                        <th>Notes</th>
                      </tr>
                    </thead>
                    <tbody>
                      {task.assignmentHistory.slice(0, 5).map((entry) => (
                        <tr key={entry.id}>
                          <td>{formatDateTime(entry.assignedAtUtc)}</td>
                          <td>{entry.assignedStaffName || "Unassigned"}</td>
                          <td>{entry.assignedByStaffName || "-"}</td>
                          <td>{entry.notes || "-"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : null}
            </article>
          ))}
        </div>
      </section>
    </section>
  );
}

function maintenanceStatusLabel(status: MaintenanceTaskStatus) {
  switch (status) {
    case MaintenanceTaskStatus.InProgress:
      return "In progress";
    case MaintenanceTaskStatus.Done:
      return "Done";
    default:
      return "Open";
  }
}

function maintenanceTaskTypeLabel(type: MaintenanceTaskType) {
  return type === MaintenanceTaskType.Breakdown ? "Breakdown" : "Scheduled";
}

function maintenancePriorityLabel(priority: MaintenancePriority) {
  switch (priority) {
    case MaintenancePriority.Low:
      return "low";
    case MaintenancePriority.High:
      return "high";
    case MaintenancePriority.Critical:
      return "critical";
    default:
      return "medium";
  }
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function equipmentLabel(item: Equipment | MaintenanceTask) {
  if ("equipmentName" in item) {
    return item.equipmentAssetTag ? `${item.equipmentAssetTag} / ${item.equipmentName}` : item.equipmentName;
  }

  return item.assetTag || item.serialNumber || item.id.slice(0, 8);
}

function createDefaultTaskForm() {
  return {
    equipmentId: "",
    assignedStaffId: "",
    taskType: String(MaintenanceTaskType.Scheduled),
    priority: String(MaintenancePriority.Medium),
    dueAt: "",
    notes: "",
  };
}
