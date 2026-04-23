import { type FormEvent, useEffect, useMemo, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type {
  CoachingPlan,
  CoachingPlanItemDecision,
  CoachingPlanItemRequest,
  CoachingPlanStatus,
  MemberSummary,
  Notice,
  Staff,
} from "../lib/types";
import {
  CoachingPlanItemDecision as CoachingPlanItemDecisionEnum,
  CoachingPlanStatus as CoachingPlanStatusEnum,
  getErrorMessages,
} from "../lib/types";

interface CoachingItemForm {
  id: string;
  title: string;
  notes: string;
  targetDate: string;
}

export function TrainerCoachingWorkspacePage() {
  const { api, session } = useAuth();
  const [plans, setPlans] = useState<CoachingPlan[]>([]);
  const [members, setMembers] = useState<MemberSummary[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [selectedMemberFilter, setSelectedMemberFilter] = useState("");
  const [activePlanId, setActivePlanId] = useState<string | null>(null);
  const [createTitle, setCreateTitle] = useState("");
  const [createNotes, setCreateNotes] = useState("");
  const [createMemberId, setCreateMemberId] = useState("");
  const [createTrainerId, setCreateTrainerId] = useState("");
  const [createItems, setCreateItems] = useState<CoachingItemForm[]>(() => [newCoachingItemForm()]);
  const [statusByPlanId, setStatusByPlanId] = useState<Record<string, CoachingPlanStatus>>({});
  const [decisionByItemId, setDecisionByItemId] = useState<Record<string, CoachingPlanItemDecision>>({});
  const [decisionNotesByItemId, setDecisionNotesByItemId] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [statusUpdatePlanId, setStatusUpdatePlanId] = useState<string | null>(null);
  const [decisionUpdateItemId, setDecisionUpdateItemId] = useState<string | null>(null);
  const [deletePlanId, setDeletePlanId] = useState<string | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const canManagePlans = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner" || session?.activeRole === "Trainer";
  const isMemberRole = session?.activeRole === "Member";

  useEffect(() => {
    void loadReferenceData();
  }, []);

  useEffect(() => {
    void loadPlans(selectedMemberFilter || undefined);
  }, [selectedMemberFilter]);

  const activePlan = useMemo(() => plans.find((plan) => plan.id === activePlanId) ?? null, [activePlanId, plans]);

  async function loadReferenceData() {
    if (!session?.activeGymCode) {
      return;
    }

    try {
      const [loadedMembers, loadedStaff] = await Promise.all([
        canManagePlans || isMemberRole ? api.getMembers(session.activeGymCode) : Promise.resolve([]),
        canManagePlans ? api.getStaff(session.activeGymCode) : Promise.resolve([]),
      ]);

      setMembers(loadedMembers);
      setStaff(loadedStaff);
      if (!createMemberId) {
        setCreateMemberId(loadedMembers[0]?.id ?? "");
      }
      if (!createTrainerId) {
        setCreateTrainerId(loadedStaff[0]?.id ?? "");
      }
    } catch {
      setMembers([]);
      setStaff([]);
    }
  }

  async function loadPlans(memberId?: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      const loadedPlans = await api.getCoachingPlans(session.activeGymCode, memberId);
      setPlans(loadedPlans);
      setActivePlanId((current) => current ?? loadedPlans[0]?.id ?? null);
      setStatusByPlanId(Object.fromEntries(loadedPlans.map((plan) => [plan.id, plan.status])));
      setDecisionByItemId(
        Object.fromEntries(
          loadedPlans.flatMap((plan) =>
            plan.items
              .filter((item) => item.decision !== undefined && item.decision !== null)
              .map((item) => [item.id, item.decision as CoachingPlanItemDecision]),
          ),
        ),
      );
      setDecisionNotesByItemId(
        Object.fromEntries(loadedPlans.flatMap((plan) => plan.items.map((item) => [item.id, item.decisionNotes ?? ""]))),
      );
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load coaching plans.");
    } finally {
      setIsLoading(false);
    }
  }

  async function createPlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session?.activeGymCode) {
      return;
    }

    const items = createItems
      .map((item, index) => toCoachingItemRequest(item, index))
      .filter((item): item is CoachingPlanItemRequest => item !== null);

    if (!createMemberId || !createTitle.trim() || items.length === 0) {
      setNotice({
        tone: "error",
        title: "Could not create coaching plan",
        messages: ["Member, title, and at least one item are required."],
      });
      return;
    }

    setIsCreating(true);
    setNotice(null);

    try {
      const created = await api.createCoachingPlan(session.activeGymCode, {
        memberId: createMemberId,
        trainerStaffId: createTrainerId || null,
        createdByStaffId: createTrainerId || null,
        title: createTitle.trim(),
        notes: createNotes.trim() || null,
        items,
      });

      setPlans((current) => [created, ...current]);
      setActivePlanId(created.id);
      setStatusByPlanId((current) => ({ ...current, [created.id]: created.status }));
      setCreateTitle("");
      setCreateNotes("");
      setCreateItems([newCoachingItemForm()]);
      setNotice({
        tone: "success",
        title: "Coaching plan created",
        messages: [`${created.title} is ready for member decisions.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not create coaching plan",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsCreating(false);
    }
  }

  async function updatePlanStatus(planId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setStatusUpdatePlanId(planId);
    setNotice(null);

    try {
      const updated = await api.updateCoachingPlanStatus(session.activeGymCode, planId, {
        status: statusByPlanId[planId] ?? CoachingPlanStatusEnum.Draft,
        notes: null,
      });

      setPlans((current) => current.map((plan) => (plan.id === updated.id ? updated : plan)));
      setStatusByPlanId((current) => ({ ...current, [updated.id]: updated.status }));
      setNotice({
        tone: "success",
        title: "Coaching plan status updated",
        messages: [`${updated.title} is now ${coachingPlanStatusLabel(updated.status)}.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not update coaching plan status",
        messages: getErrorMessages(error),
      });
    } finally {
      setStatusUpdatePlanId(null);
    }
  }

  async function decidePlanItem(planId: string, itemId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setDecisionUpdateItemId(itemId);
    setNotice(null);

    try {
      const updated = await api.decideCoachingPlanItem(session.activeGymCode, planId, itemId, {
        decision: decisionByItemId[itemId] ?? CoachingPlanItemDecisionEnum.Accepted,
        notes: decisionNotesByItemId[itemId]?.trim() || null,
      });

      setPlans((current) => current.map((plan) => (plan.id === updated.id ? updated : plan)));
      setNotice({
        tone: "success",
        title: "Coaching plan item decision saved",
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not save item decision",
        messages: getErrorMessages(error),
      });
    } finally {
      setDecisionUpdateItemId(null);
    }
  }

  async function deletePlan(planId: string, title: string) {
    if (!session?.activeGymCode) {
      return;
    }

    if (!window.confirm(`Delete coaching plan "${title}"?`)) {
      return;
    }

    setDeletePlanId(planId);
    setNotice(null);

    try {
      await api.deleteCoachingPlan(session.activeGymCode, planId);
      setPlans((current) => current.filter((plan) => plan.id !== planId));
      if (activePlanId === planId) {
        setActivePlanId(null);
      }
      setNotice({
        tone: "success",
        title: "Coaching plan deleted",
        messages: [`${title} was removed.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not delete coaching plan",
        messages: getErrorMessages(error),
      });
    } finally {
      setDeletePlanId(null);
    }
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">Trainer workflow</p>
          <h2 className="workspace__title">Coaching workspace</h2>
          <p className="workspace__copy">
            Build and track coaching plans, then capture member and trainer decisions per plan item.
          </p>
        </div>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        {canManagePlans ? (
          <section className="panel">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Create</p>
                <h3>New coaching plan</h3>
              </div>
            </div>

            <form className="form" onSubmit={(event) => void createPlan(event)}>
              <label className="field">
                <span>Member</span>
                <select
                  disabled={isCreating || members.length === 0}
                  onChange={(event) => setCreateMemberId(event.target.value)}
                  value={createMemberId}
                >
                  {members.map((member) => (
                    <option key={member.id} value={member.id}>
                      {member.fullName} / {member.memberCode}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Assigned trainer</span>
                <select disabled={isCreating || staff.length === 0} onChange={(event) => setCreateTrainerId(event.target.value)} value={createTrainerId}>
                  <option value="">Unassigned</option>
                  {staff.map((staffMember) => (
                    <option key={staffMember.id} value={staffMember.id}>
                      {staffMember.fullName}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Title</span>
                <input disabled={isCreating} onChange={(event) => setCreateTitle(event.target.value)} value={createTitle} />
              </label>

              <label className="field">
                <span>Notes</span>
                <textarea disabled={isCreating} onChange={(event) => setCreateNotes(event.target.value)} rows={3} value={createNotes} />
              </label>

              <div className="form">
                {createItems.map((item, index) => (
                  <div className="panel" key={item.id}>
                    <div className="editor-header">
                      <strong>Item {index + 1}</strong>
                      <button
                        className="button button--ghost"
                        disabled={isCreating || createItems.length <= 1}
                        onClick={() => setCreateItems((current) => current.filter((currentItem) => currentItem.id !== item.id))}
                        type="button"
                      >
                        Remove
                      </button>
                    </div>
                    <div className="form__row">
                      <label className="field">
                        <span>Title</span>
                        <input
                          disabled={isCreating}
                          onChange={(event) => updateCreateItem(item.id, { title: event.target.value })}
                          value={item.title}
                        />
                      </label>
                      <label className="field">
                        <span>Target date</span>
                        <input
                          disabled={isCreating}
                          onChange={(event) => updateCreateItem(item.id, { targetDate: event.target.value })}
                          type="date"
                          value={item.targetDate}
                        />
                      </label>
                    </div>
                    <label className="field">
                      <span>Notes</span>
                      <input
                        disabled={isCreating}
                        onChange={(event) => updateCreateItem(item.id, { notes: event.target.value })}
                        value={item.notes}
                      />
                    </label>
                  </div>
                ))}
              </div>

              <div className="form__actions">
                <button className="button button--ghost" disabled={isCreating} onClick={() => setCreateItems((current) => [...current, newCoachingItemForm()])} type="button">
                  Add item
                </button>
                <button className="button" disabled={isCreating || members.length === 0} type="submit">
                  {isCreating ? "Creating..." : "Create coaching plan"}
                </button>
              </div>
            </form>
          </section>
        ) : null}

        <section className="panel panel--list">
          <div className="toolbar">
            <label className="field">
              <span>Filter by member</span>
              <select onChange={(event) => setSelectedMemberFilter(event.target.value)} value={selectedMemberFilter}>
                <option value="">All visible plans</option>
                {members.map((member) => (
                  <option key={member.id} value={member.id}>
                    {member.fullName}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">Loading coaching plans...</p> : null}
          {!isLoading && plans.length === 0 ? <p className="state">No coaching plans found for this gym context.</p> : null}

          <div className="record-list" role="list">
            {plans.map((plan) => (
              <article className="record-card record-card--wide" key={plan.id} role="listitem">
                <button className="record-card__body" onClick={() => setActivePlanId(plan.id)} type="button">
                  <strong>{plan.title}</strong>
                  <span>{plan.memberName}</span>
                  <span>{coachingPlanStatusLabel(plan.status)}</span>
                </button>
                {canManagePlans ? (
                  <button
                    className="button button--danger"
                    disabled={deletePlanId === plan.id}
                    onClick={() => void deletePlan(plan.id, plan.title)}
                    type="button"
                  >
                    {deletePlanId === plan.id ? "Deleting..." : "Delete"}
                  </button>
                ) : null}
              </article>
            ))}
          </div>
        </section>

        <section className="panel">
          {!activePlan ? <p className="state">Select a coaching plan to review items and status.</p> : null}
          {activePlan ? (
            <div className="session-detail">
              <div className="editor-header">
                <div>
                  <p className="workspace__eyebrow">Plan detail</p>
                  <h3>{activePlan.title}</h3>
                </div>
                <span className="badge">{coachingPlanStatusLabel(activePlan.status)}</span>
              </div>

              <p className="workspace__copy workspace__copy--dark">{activePlan.notes?.trim() || "No plan notes yet."}</p>

              <dl className="definition-list">
                <div>
                  <dt>Member</dt>
                  <dd>{activePlan.memberName}</dd>
                </div>
                <div>
                  <dt>Trainer</dt>
                  <dd>{activePlan.trainerStaffName || "Unassigned"}</dd>
                </div>
                <div>
                  <dt>Published</dt>
                  <dd>{activePlan.publishedAtUtc ? formatDateTime(activePlan.publishedAtUtc) : "-"}</dd>
                </div>
                <div>
                  <dt>Completed</dt>
                  <dd>{activePlan.completedAtUtc ? formatDateTime(activePlan.completedAtUtc) : "-"}</dd>
                </div>
              </dl>

              {canManagePlans ? (
                <div className="inline-controls">
                  <label className="field field--compact">
                    <span>Status</span>
                    <select
                      disabled={statusUpdatePlanId === activePlan.id}
                      onChange={(event) =>
                        setStatusByPlanId((current) => ({
                          ...current,
                          [activePlan.id]: Number(event.target.value) as CoachingPlanStatus,
                        }))
                      }
                      value={statusByPlanId[activePlan.id] ?? activePlan.status}
                    >
                      <option value={CoachingPlanStatusEnum.Draft}>Draft</option>
                      <option value={CoachingPlanStatusEnum.Published}>Published</option>
                      <option value={CoachingPlanStatusEnum.Active}>Active</option>
                      <option value={CoachingPlanStatusEnum.Completed}>Completed</option>
                      <option value={CoachingPlanStatusEnum.Cancelled}>Cancelled</option>
                    </select>
                  </label>
                  <button className="button" disabled={statusUpdatePlanId === activePlan.id} onClick={() => void updatePlanStatus(activePlan.id)} type="button">
                    {statusUpdatePlanId === activePlan.id ? "Saving..." : "Update status"}
                  </button>
                </div>
              ) : null}

              <div className="record-list" role="list">
                {activePlan.items.map((item) => (
                  <article className="record-card record-card--wide" key={item.id} role="listitem">
                    <div className="record-card__body">
                      <strong>
                        #{item.sequence} {item.title}
                      </strong>
                      <span>{item.notes || "No notes"}</span>
                      <span>Target: {item.targetDate ? formatDate(item.targetDate) : "none"}</span>
                      <span>Decision: {item.decision === undefined || item.decision === null ? "Pending" : coachingItemDecisionLabel(item.decision)}</span>
                    </div>
                    <div className="inline-controls inline-controls--wide">
                      <label className="field field--compact">
                        <span>Decision</span>
                        <select
                          disabled={decisionUpdateItemId === item.id}
                          onChange={(event) =>
                            setDecisionByItemId((current) => ({
                              ...current,
                              [item.id]: Number(event.target.value) as CoachingPlanItemDecision,
                            }))
                          }
                          value={decisionByItemId[item.id] ?? item.decision ?? CoachingPlanItemDecisionEnum.Accepted}
                        >
                          <option value={CoachingPlanItemDecisionEnum.Accepted}>Accepted</option>
                          <option value={CoachingPlanItemDecisionEnum.Deferred}>Deferred</option>
                          {!isMemberRole ? <option value={CoachingPlanItemDecisionEnum.Completed}>Completed</option> : null}
                          <option value={CoachingPlanItemDecisionEnum.Skipped}>Skipped</option>
                        </select>
                      </label>
                      <label className="field field--notes">
                        <span>Decision notes</span>
                        <input
                          disabled={decisionUpdateItemId === item.id}
                          onChange={(event) => setDecisionNotesByItemId((current) => ({ ...current, [item.id]: event.target.value }))}
                          value={decisionNotesByItemId[item.id] ?? item.decisionNotes ?? ""}
                        />
                      </label>
                      <button
                        className="button"
                        disabled={decisionUpdateItemId === item.id}
                        onClick={() => void decidePlanItem(activePlan.id, item.id)}
                        type="button"
                      >
                        {decisionUpdateItemId === item.id ? "Saving..." : "Save decision"}
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          ) : null}
        </section>
      </div>
    </section>
  );

  function updateCreateItem(itemId: string, next: Partial<CoachingItemForm>) {
    setCreateItems((current) => current.map((item) => (item.id === itemId ? { ...item, ...next } : item)));
  }
}

function toCoachingItemRequest(item: CoachingItemForm, index: number): CoachingPlanItemRequest | null {
  if (!item.title.trim()) {
    return null;
  }

  return {
    sequence: index + 1,
    title: item.title.trim(),
    notes: item.notes.trim() || null,
    targetDate: item.targetDate || null,
  };
}

function newCoachingItemForm(): CoachingItemForm {
  return {
    id: crypto.randomUUID(),
    title: "",
    notes: "",
    targetDate: "",
  };
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function coachingPlanStatusLabel(status: CoachingPlanStatus) {
  switch (status) {
    case CoachingPlanStatusEnum.Published:
      return "Published";
    case CoachingPlanStatusEnum.Active:
      return "Active";
    case CoachingPlanStatusEnum.Completed:
      return "Completed";
    case CoachingPlanStatusEnum.Cancelled:
      return "Cancelled";
    default:
      return "Draft";
  }
}

function coachingItemDecisionLabel(decision: CoachingPlanItemDecision) {
  switch (decision) {
    case CoachingPlanItemDecisionEnum.Deferred:
      return "Deferred";
    case CoachingPlanItemDecisionEnum.Completed:
      return "Completed";
    case CoachingPlanItemDecisionEnum.Skipped:
      return "Skipped";
    default:
      return "Accepted";
  }
}
