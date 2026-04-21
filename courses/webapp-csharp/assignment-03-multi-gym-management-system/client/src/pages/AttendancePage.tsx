import { useEffect, useMemo, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { Booking, Notice, TrainingSession } from "../lib/types";
import { BookingStatus, getErrorMessages } from "../lib/types";

export function AttendancePage() {
  const { api, session } = useAuth();
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [sessions, setSessions] = useState<TrainingSession[]>([]);
  const [statusByBookingId, setStatusByBookingId] = useState<Record<string, BookingStatus>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [submittingBookingId, setSubmittingBookingId] = useState<string | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  useEffect(() => {
    void loadAttendance();
  }, []);

  const sessionById = useMemo(() => new Map(sessions.map((item) => [item.id, item])), [sessions]);

  async function loadAttendance() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      const [loadedBookings, loadedSessions] = await Promise.all([
        api.getBookings(session.activeGymCode),
        api.getTrainingSessions(session.activeGymCode),
      ]);

      setBookings(loadedBookings);
      setSessions(loadedSessions);
      setStatusByBookingId(Object.fromEntries(loadedBookings.map((booking) => [booking.id, booking.status])));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load attendance.");
    } finally {
      setIsLoading(false);
    }
  }

  async function updateAttendance(bookingId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setSubmittingBookingId(bookingId);
    setNotice(null);

    try {
      const updated = await api.updateAttendance(session.activeGymCode, bookingId, {
        status: statusByBookingId[bookingId] ?? BookingStatus.Booked,
      });

      setBookings((current) => current.map((booking) => (booking.id === updated.id ? updated : booking)));
      setStatusByBookingId((current) => ({ ...current, [updated.id]: updated.status }));
      setNotice({
        tone: "success",
        title: "Attendance updated",
        messages: [`Booking ${updated.id.slice(0, 8)} is now ${bookingStatusLabel(updated.status)}.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not update attendance",
        messages: getErrorMessages(error),
      });
    } finally {
      setSubmittingBookingId(null);
    }
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">Trainer workflow</p>
          <h2 className="workspace__title">Attendance</h2>
          <p className="workspace__copy">
            Trainers can mark attendance for assigned sessions through the same REST endpoint used by the MVC roster.
          </p>
        </div>
      </header>

      <NoticeBanner notice={notice} />

      <section className="panel panel--list">
        {pageError ? <p className="state state--error">{pageError}</p> : null}
        {isLoading ? <p className="state">Loading assigned bookings...</p> : null}
        {!isLoading && bookings.length === 0 ? <p className="state">No assigned bookings are ready for attendance.</p> : null}

        <div className="record-list" role="list">
          {bookings.map((booking) => {
            const trainingSession = sessionById.get(booking.trainingSessionId);
            return (
              <article className="record-card record-card--wide" key={booking.id} role="listitem">
                <div className="record-card__body">
                  <strong>{trainingSession?.name ?? "Training session"}</strong>
                  <span>{trainingSession ? formatRange(trainingSession.startAtUtc, trainingSession.endAtUtc) : booking.trainingSessionId}</span>
                  <span>
                    Member {booking.memberId.slice(0, 8)} / current status {bookingStatusLabel(booking.status)}
                  </span>
                </div>
                <div className="inline-controls">
                  <label className="field field--compact">
                    <span>Attendance</span>
                    <select
                      disabled={submittingBookingId === booking.id}
                      onChange={(event) =>
                        setStatusByBookingId((current) => ({
                          ...current,
                          [booking.id]: Number(event.target.value) as BookingStatus,
                        }))
                      }
                      value={statusByBookingId[booking.id] ?? booking.status}
                    >
                      <option value={BookingStatus.Booked}>Booked</option>
                      <option value={BookingStatus.Attended}>Attended</option>
                      <option value={BookingStatus.NoShow}>No-show</option>
                      <option value={BookingStatus.Cancelled}>Cancelled</option>
                    </select>
                  </label>
                  <button
                    className="button"
                    disabled={submittingBookingId === booking.id}
                    onClick={() => void updateAttendance(booking.id)}
                    type="button"
                  >
                    {submittingBookingId === booking.id ? "Saving..." : "Update"}
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      </section>
    </section>
  );
}

function bookingStatusLabel(status: BookingStatus) {
  switch (status) {
    case BookingStatus.Cancelled:
      return "Cancelled";
    case BookingStatus.Attended:
      return "Attended";
    case BookingStatus.NoShow:
      return "No-show";
    default:
      return "Booked";
  }
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
