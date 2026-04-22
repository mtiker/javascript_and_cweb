import { type FormEvent, useEffect, useMemo, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import { useLanguage } from "../lib/language";
import type { MemberDetail, MemberSummary, Notice, TrainingCategory, TrainingSession } from "../lib/types";
import { getErrorMessages, TrainingSessionStatus } from "../lib/types";

export function SessionsPage() {
  const { api, session } = useAuth();
  const { t } = useLanguage();
  const [sessions, setSessions] = useState<TrainingSession[]>([]);
  const [categories, setCategories] = useState<TrainingCategory[]>([]);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [activeSession, setActiveSession] = useState<TrainingSession | null>(null);
  const [members, setMembers] = useState<MemberSummary[]>([]);
  const [currentMember, setCurrentMember] = useState<MemberDetail | null>(null);
  const [selectedMemberId, setSelectedMemberId] = useState("");
  const [paymentReference, setPaymentReference] = useState("");
  const [scheduleForm, setScheduleForm] = useState(createDefaultScheduleForm());
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isScheduling, setIsScheduling] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const canChooseMember = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner";
  const canManageTraining = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner";
  const bookingMemberId = currentMember?.id ?? selectedMemberId;

  useEffect(() => {
    void loadWorkspace();
  }, []);

  useEffect(() => {
    if (activeSessionId) {
      void loadSession(activeSessionId);
    } else {
      setActiveSession(null);
    }
  }, [activeSessionId]);

  const upcomingSessions = useMemo(
    () => sessions.filter((item) => item.status !== TrainingSessionStatus.Cancelled),
    [sessions],
  );

  async function loadWorkspace() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      const [loadedSessions, loadedMembers, loadedCurrentMember, loadedCategories] = await Promise.all([
        api.getTrainingSessions(session.activeGymCode),
        canChooseMember ? api.getMembers(session.activeGymCode) : Promise.resolve([]),
        session.activeRole === "Member" ? api.getCurrentMember(session.activeGymCode) : Promise.resolve(null),
        canManageTraining ? api.getTrainingCategories(session.activeGymCode) : Promise.resolve([]),
      ]);

      setSessions(loadedSessions);
      setMembers(loadedMembers);
      setCurrentMember(loadedCurrentMember);
      setCategories(loadedCategories);
      setSelectedMemberId(loadedMembers[0]?.id ?? "");
      setScheduleForm((current) => ({ ...current, categoryId: current.categoryId || loadedCategories[0]?.id || "" }));
      setActiveSessionId((current) => current ?? loadedSessions[0]?.id ?? null);
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load sessions.");
    } finally {
      setIsLoading(false);
    }
  }

  async function loadSession(sessionId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setIsDetailLoading(true);

    try {
      setActiveSession(await api.getTrainingSession(session.activeGymCode, sessionId));
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not load session details",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsDetailLoading(false);
    }
  }

  const handleBook = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!session?.activeGymCode || !activeSession || !bookingMemberId) {
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      const booking = await api.createBooking(session.activeGymCode, {
        trainingSessionId: activeSession.id,
        memberId: bookingMemberId,
        paymentReference: paymentReference.trim() || null,
      });

      setPaymentReference("");
      setNotice({
        tone: "success",
        title: "Booking created",
        messages: [
          booking.paymentRequired
            ? `Payment reference accepted for ${booking.chargedPrice.toFixed(2)} ${activeSession.currencyCode}.`
            : "Membership coverage was applied and no payment was required.",
        ],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not create booking",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleScheduleSession = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!session?.activeGymCode) {
      return;
    }

    if (!scheduleForm.categoryId || !scheduleForm.name.trim() || !scheduleForm.startAt || !scheduleForm.endAt) {
      setNotice({
        tone: "error",
        title: "Could not schedule session",
        messages: ["Category, name, start time, and end time are required."],
      });
      return;
    }

    setIsScheduling(true);
    setNotice(null);

    try {
      const created = await api.createTrainingSession(session.activeGymCode, {
        categoryId: scheduleForm.categoryId,
        name: scheduleForm.name.trim(),
        description: scheduleForm.description.trim() || null,
        startAtUtc: new Date(scheduleForm.startAt).toISOString(),
        endAtUtc: new Date(scheduleForm.endAt).toISOString(),
        capacity: Number(scheduleForm.capacity),
        basePrice: Number(scheduleForm.basePrice),
        currencyCode: scheduleForm.currencyCode.trim() || "EUR",
        status: Number(scheduleForm.status) as TrainingSessionStatus,
        trainerContractIds: [],
      });

      setSessions((current) => [...current, created].sort((left, right) => left.startAtUtc.localeCompare(right.startAtUtc)));
      setActiveSessionId(created.id);
      setScheduleForm((current) => ({
        ...createDefaultScheduleForm(),
        categoryId: current.categoryId,
        currencyCode: current.currencyCode,
      }));
      setNotice({
        tone: "success",
        title: "Session scheduled",
        messages: [`${created.name} is available in the training calendar.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not schedule session",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsScheduling(false);
    }
  };

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">{t("REST workflow")}</p>
          <h2 className="workspace__title">{t("sessions")}</h2>
          <p className="workspace__copy">
            Browse published training sessions, inspect details, and create member bookings through tenant API calls.
          </p>
        </div>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        {canManageTraining ? (
          <section className="panel">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">{t("Schedule")}</p>
                <h3>{t("New training session")}</h3>
              </div>
            </div>

            <form className="form" onSubmit={(event) => void handleScheduleSession(event)}>
              <label className="field">
                <span>{t("Category")}</span>
                <select
                  disabled={isScheduling || categories.length === 0}
                  onChange={(event) => setScheduleForm((current) => ({ ...current, categoryId: event.target.value }))}
                  value={scheduleForm.categoryId}
                >
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>{t("Name")}</span>
                <input
                  disabled={isScheduling}
                  onChange={(event) => setScheduleForm((current) => ({ ...current, name: event.target.value }))}
                  value={scheduleForm.name}
                />
              </label>

              <div className="form__row">
                <label className="field">
                  <span>{t("Starts")}</span>
                  <input
                    disabled={isScheduling}
                    onChange={(event) => setScheduleForm((current) => ({ ...current, startAt: event.target.value }))}
                    type="datetime-local"
                    value={scheduleForm.startAt}
                  />
                </label>
                <label className="field">
                  <span>{t("Ends")}</span>
                  <input
                    disabled={isScheduling}
                    onChange={(event) => setScheduleForm((current) => ({ ...current, endAt: event.target.value }))}
                    type="datetime-local"
                    value={scheduleForm.endAt}
                  />
                </label>
              </div>

              <div className="form__row">
                <label className="field">
                  <span>{t("Capacity")}</span>
                  <input
                    disabled={isScheduling}
                    min={1}
                    onChange={(event) => setScheduleForm((current) => ({ ...current, capacity: event.target.value }))}
                    type="number"
                    value={scheduleForm.capacity}
                  />
                </label>
                <label className="field">
                  <span>{t("Base price")}</span>
                  <input
                    disabled={isScheduling}
                    min={0}
                    onChange={(event) => setScheduleForm((current) => ({ ...current, basePrice: event.target.value }))}
                    step="0.01"
                    type="number"
                    value={scheduleForm.basePrice}
                  />
                </label>
              </div>

              <div className="form__row">
                <label className="field">
                  <span>{t("Currency code")}</span>
                  <input
                    disabled={isScheduling}
                    maxLength={8}
                    onChange={(event) => setScheduleForm((current) => ({ ...current, currencyCode: event.target.value }))}
                    value={scheduleForm.currencyCode}
                  />
                </label>
                <label className="field">
                  <span>{t("Status")}</span>
                  <select
                    disabled={isScheduling}
                    onChange={(event) => setScheduleForm((current) => ({ ...current, status: event.target.value }))}
                    value={scheduleForm.status}
                  >
                    <option value={TrainingSessionStatus.Published}>{t("Published")}</option>
                    <option value={TrainingSessionStatus.Draft}>{t("Draft")}</option>
                  </select>
                </label>
              </div>

              <label className="field">
                <span>{t("Description")}</span>
                <textarea
                  disabled={isScheduling}
                  onChange={(event) => setScheduleForm((current) => ({ ...current, description: event.target.value }))}
                  rows={3}
                  value={scheduleForm.description}
                />
              </label>

              <div className="form__actions">
                <button className="button" disabled={isScheduling || categories.length === 0} type="submit">
                  {isScheduling ? t("Scheduling...") : t("Schedule session")}
                </button>
                {categories.length === 0 ? <span className="state">{t("Create a category before scheduling sessions.")}</span> : null}
              </div>
            </form>
          </section>
        ) : null}

        <section className="panel panel--list">
          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">{t("Loading sessions...")}</p> : null}
          {!isLoading && sessions.length === 0 ? <p className="state">{t("No training sessions have been published yet.")}</p> : null}

          <div className="record-list" role="list">
            {upcomingSessions.map((item) => (
              <article className="record-card" key={item.id} role="listitem">
                <button className="record-card__body" onClick={() => setActiveSessionId(item.id)} type="button">
                  <strong>{item.name}</strong>
                  <span>{formatRange(item.startAtUtc, item.endAtUtc)}</span>
                  <span>
                    {item.capacity} places / {item.basePrice.toFixed(2)} {item.currencyCode}
                  </span>
                </button>
                <span className="badge">{sessionStatusLabel(item.status)}</span>
              </article>
            ))}
          </div>
        </section>

        <section className="panel">
          {isDetailLoading ? <p className="state">{t("Loading session details...")}</p> : null}
          {!activeSession && !isDetailLoading ? <p className="state">{t("Select a session to open booking details.")}</p> : null}
          {activeSession ? (
            <div className="session-detail">
              <div className="editor-header">
                <div>
                  <p className="workspace__eyebrow">{t("Session detail")}</p>
                  <h3>{activeSession.name}</h3>
                </div>
                <span className="badge">{sessionStatusLabel(activeSession.status)}</span>
              </div>

              <dl className="definition-list">
                <div>
                  <dt>{t("When")}</dt>
                  <dd>{formatRange(activeSession.startAtUtc, activeSession.endAtUtc)}</dd>
                </div>
                <div>
                  <dt>{t("Capacity")}</dt>
                  <dd>{activeSession.capacity} members</dd>
                </div>
                <div>
                  <dt>{t("Price")}</dt>
                  <dd>
                    {activeSession.basePrice.toFixed(2)} {activeSession.currencyCode}
                  </dd>
                </div>
                <div>
                  <dt>{t("Trainers")}</dt>
                  <dd>{activeSession.trainerContractIds.length || "No assigned trainers"}</dd>
                </div>
              </dl>

              <p className="workspace__copy workspace__copy--dark">
                {activeSession.description?.trim() || "No session description has been added yet."}
              </p>

              {activeSession.status === TrainingSessionStatus.Published ? (
                <form className="form" onSubmit={(event) => void handleBook(event)}>
                  {canChooseMember ? (
                    <label className="field">
                      <span>{t("Member")}</span>
                      <select
                        disabled={isSubmitting || members.length === 0}
                        onChange={(event) => setSelectedMemberId(event.target.value)}
                        value={selectedMemberId}
                      >
                        {members.map((member) => (
                          <option key={member.id} value={member.id}>
                            {member.fullName} / {member.memberCode}
                          </option>
                        ))}
                      </select>
                    </label>
                  ) : null}

                  <label className="field">
                    <span>{t("Payment reference")}</span>
                    <input
                      disabled={isSubmitting}
                      onChange={(event) => setPaymentReference(event.target.value)}
                      placeholder="Required when a paid booking is not covered by membership"
                      value={paymentReference}
                    />
                  </label>

                  <div className="form__actions">
                    <button className="button" disabled={isSubmitting || !bookingMemberId} type="submit">
                      {isSubmitting ? t("Booking...") : t("Book session")}
                    </button>
                    {!bookingMemberId ? <span className="state">{t("No member profile is available for this role.")}</span> : null}
                  </div>
                </form>
              ) : (
                <p className="state">{t("Only published sessions can be booked.")}</p>
              )}
            </div>
          ) : null}
        </section>
      </div>
    </section>
  );
}

function formatRange(startAtUtc: string, endAtUtc: string) {
  const start = new Date(startAtUtc);
  const end = new Date(endAtUtc);
  const date = new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(start);
  const endTime = new Intl.DateTimeFormat(undefined, {
    timeStyle: "short",
  }).format(end);

  return `${date} - ${endTime}`;
}

function sessionStatusLabel(status: TrainingSessionStatus) {
  switch (status) {
    case TrainingSessionStatus.Published:
      return "Published";
    case TrainingSessionStatus.Cancelled:
      return "Cancelled";
    case TrainingSessionStatus.Completed:
      return "Completed";
    default:
      return "Draft";
  }
}

function createDefaultScheduleForm() {
  return {
    categoryId: "",
    name: "",
    description: "",
    startAt: "",
    endAt: "",
    capacity: "12",
    basePrice: "0",
    currencyCode: "EUR",
    status: String(TrainingSessionStatus.Published),
  };
}
