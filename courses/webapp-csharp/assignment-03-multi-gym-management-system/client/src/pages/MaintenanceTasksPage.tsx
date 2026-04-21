import { useEffect, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { MaintenanceTask, Notice } from "../lib/types";
import { getErrorMessages, MaintenancePriority, MaintenanceTaskStatus, MaintenanceTaskType } from "../lib/types";

export function MaintenanceTasksPage() {
  const { api, session } = useAuth();
  const [tasks, setTasks] = useState<MaintenanceTask[]>([]);
  const [statusByTaskId, setStatusByTaskId] = useState<Record<string, MaintenanceTaskStatus>>({});
  const [notesByTaskId, setNotesByTaskId] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [submittingTaskId, setSubmittingTaskId] = useState<string | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

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
      const loadedTasks = await api.getMaintenanceTasks(session.activeGymCode);
      setTasks(loadedTasks);
      setStatusByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, task.status])));
      setNotesByTaskId(Object.fromEntries(loadedTasks.map((task) => [task.id, task.notes ?? ""])));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load maintenance tasks.");
    } finally {
      setIsLoading(false);
    }
  }

  async function updateTask(taskId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setSubmittingTaskId(taskId);
    setNotice(null);

    try {
      const updated = await api.updateMaintenanceTaskStatus(session.activeGymCode, taskId, {
        status: statusByTaskId[taskId] ?? MaintenanceTaskStatus.Open,
        notes: notesByTaskId[taskId]?.trim() || null,
      });

      setTasks((current) => current.map((task) => (task.id === updated.id ? updated : task)));
      setStatusByTaskId((current) => ({ ...current, [updated.id]: updated.status }));
      setNotesByTaskId((current) => ({ ...current, [updated.id]: updated.notes ?? "" }));
      setNotice({
        tone: "success",
        title: "Maintenance task updated",
        messages: [`Task ${updated.id.slice(0, 8)} is now ${maintenanceStatusLabel(updated.status)}.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not update maintenance task",
        messages: getErrorMessages(error),
      });
    } finally {
      setSubmittingTaskId(null);
    }
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">Caretaker workflow</p>
          <h2 className="workspace__title">Maintenance Tasks</h2>
          <p className="workspace__copy">
            Caretakers can review assigned equipment work and update task status through tenant REST endpoints.
          </p>
        </div>
      </header>

      <NoticeBanner notice={notice} />

      <section className="panel panel--list">
        {pageError ? <p className="state state--error">{pageError}</p> : null}
        {isLoading ? <p className="state">Loading maintenance tasks...</p> : null}
        {!isLoading && tasks.length === 0 ? <p className="state">No maintenance tasks are assigned in this gym.</p> : null}

        <div className="record-list" role="list">
          {tasks.map((task) => (
            <article className="record-card record-card--wide" key={task.id} role="listitem">
              <div className="record-card__body">
                <strong>{maintenanceTaskTypeLabel(task.taskType)} maintenance</strong>
                <span>
                  Priority {maintenancePriorityLabel(task.priority)} / current status {maintenanceStatusLabel(task.status)}
                </span>
                <span>{task.dueAtUtc ? `Due ${formatDate(task.dueAtUtc)}` : "No due date set"}</span>
              </div>
              <div className="inline-controls inline-controls--wide">
                <label className="field field--compact">
                  <span>Status</span>
                  <select
                    disabled={submittingTaskId === task.id}
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
                <label className="field field--compact field--notes">
                  <span>Notes</span>
                  <textarea
                    disabled={submittingTaskId === task.id}
                    onChange={(event) => setNotesByTaskId((current) => ({ ...current, [task.id]: event.target.value }))}
                    rows={2}
                    value={notesByTaskId[task.id] ?? ""}
                  />
                </label>
                <button
                  className="button"
                  disabled={submittingTaskId === task.id}
                  onClick={() => void updateTask(task.id)}
                  type="button"
                >
                  {submittingTaskId === task.id ? "Saving..." : "Update"}
                </button>
              </div>
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

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
