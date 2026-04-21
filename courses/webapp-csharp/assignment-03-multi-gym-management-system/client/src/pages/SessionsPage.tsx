import { type FormEvent, useEffect, useMemo, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { MemberDetail, MemberSummary, Notice, TrainingSession } from "../lib/types";
import { getErrorMessages, TrainingSessionStatus } from "../lib/types";

export function SessionsPage() {
  const { api, session } = useAuth();
  const [sessions, setSessions] = useState<TrainingSession[]>([]);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [activeSession, setActiveSession] = useState<TrainingSession | null>(null);
  const [members, setMembers] = useState<MemberSummary[]>([]);
  const [currentMember, setCurrentMember] = useState<MemberDetail | null>(null);
  const [selectedMemberId, setSelectedMemberId] = useState("");
  const [paymentReference, setPaymentReference] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const canChooseMember = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner";
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
      const [loadedSessions, loadedMembers, loadedCurrentMember] = await Promise.all([
        api.getTrainingSessions(session.activeGymCode),
        canChooseMember ? api.getMembers(session.activeGymCode) : Promise.resolve([]),
        session.activeRole === "Member" ? api.getCurrentMember(session.activeGymCode) : Promise.resolve(null),
      ]);

      setSessions(loadedSessions);
      setMembers(loadedMembers);
      setCurrentMember(loadedCurrentMember);
      setSelectedMemberId(loadedMembers[0]?.id ?? "");
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
    setNotice(null);

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

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">REST workflow</p>
          <h2 className="workspace__title">Sessions</h2>
          <p className="workspace__copy">
            Browse published training sessions, inspect details, and create member bookings through tenant API calls.
          </p>
        </div>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        <section className="panel panel--list">
          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">Loading sessions...</p> : null}
          {!isLoading && sessions.length === 0 ? <p className="state">No training sessions have been published yet.</p> : null}

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
          {isDetailLoading ? <p className="state">Loading session details...</p> : null}
          {!activeSession && !isDetailLoading ? <p className="state">Select a session to open booking details.</p> : null}
          {activeSession ? (
            <div className="session-detail">
              <div className="editor-header">
                <div>
                  <p className="workspace__eyebrow">Session detail</p>
                  <h3>{activeSession.name}</h3>
                </div>
                <span className="badge">{sessionStatusLabel(activeSession.status)}</span>
              </div>

              <dl className="definition-list">
                <div>
                  <dt>When</dt>
                  <dd>{formatRange(activeSession.startAtUtc, activeSession.endAtUtc)}</dd>
                </div>
                <div>
                  <dt>Capacity</dt>
                  <dd>{activeSession.capacity} members</dd>
                </div>
                <div>
                  <dt>Price</dt>
                  <dd>
                    {activeSession.basePrice.toFixed(2)} {activeSession.currencyCode}
                  </dd>
                </div>
                <div>
                  <dt>Trainers</dt>
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
                      <span>Member</span>
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
                    <span>Payment reference</span>
                    <input
                      disabled={isSubmitting}
                      onChange={(event) => setPaymentReference(event.target.value)}
                      placeholder="Required when a paid booking is not covered by membership"
                      value={paymentReference}
                    />
                  </label>

                  <div className="form__actions">
                    <button className="button" disabled={isSubmitting || !bookingMemberId} type="submit">
                      {isSubmitting ? "Booking..." : "Book session"}
                    </button>
                    {!bookingMemberId ? <span className="state">No member profile is available for this role.</span> : null}
                  </div>
                </form>
              ) : (
                <p className="state">Only published sessions can be booked.</p>
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
