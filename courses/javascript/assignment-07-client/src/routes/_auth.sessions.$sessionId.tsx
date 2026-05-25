import { createFileRoute, Link } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useAuth } from "@/lib/api/auth-context";
import { BookingStatus, TrainingSessionStatus, getErrorMessages } from "@/lib/api/types";
import { NoActiveGym, enumLabel, fmtDate, fmtMoney } from "@/lib/ui-helpers";

export const Route = createFileRoute("/_auth/sessions/$sessionId")({
  component: SessionDetailPage,
});

function SessionDetailPage() {
  const { sessionId } = Route.useParams();
  const auth = useAuth();
  const gym = auth.activeGym;
  const qc = useQueryClient();

  const sessionQ = useQuery({
    enabled: !!gym,
    queryKey: ["session", gym, sessionId],
    queryFn: () => auth.api.getTrainingSession(gym!, sessionId),
  });
  const meQ = useQuery({
    enabled: !!gym,
    queryKey: ["me-member", gym],
    queryFn: () => auth.api.getCurrentMember(gym!),
    retry: false,
  });
  const bookingsQ = useQuery({
    enabled: !!gym,
    queryKey: ["bookings", gym],
    queryFn: () => auth.api.getBookings(gym!),
  });

  const myBooking = (bookingsQ.data ?? []).find(
    (b) => b.trainingSessionId === sessionId && b.memberId === meQ.data?.id && b.status !== BookingStatus.Cancelled,
  );

  const book = useMutation({
    mutationFn: () =>
      auth.api.createBooking(gym!, { trainingSessionId: sessionId, memberId: meQ.data!.id }),
    onSuccess: () => {
      toast.success("Booked.");
      qc.invalidateQueries({ queryKey: ["bookings", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const cancel = useMutation({
    mutationFn: () =>
      auth.api.updateAttendance(gym!, myBooking!.id, { status: BookingStatus.Cancelled }),
    onSuccess: () => {
      toast.success("Cancelled.");
      qc.invalidateQueries({ queryKey: ["bookings", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  if (!gym) return <NoActiveGym />;
  if (sessionQ.isLoading) return <p className="text-sm text-muted-foreground">Loading…</p>;
  if (sessionQ.isError)
    return <p className="text-sm text-destructive">{getErrorMessages(sessionQ.error).join(" ")}</p>;

  const s = sessionQ.data!;

  return (
    <section className="max-w-3xl">
      <Link to="/sessions" className="text-xs text-muted-foreground underline">
        ← back to sessions
      </Link>
      <div className="mt-3 flex items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{s.name}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{s.description ?? "—"}</p>
        </div>
        <Badge variant="secondary">{enumLabel(TrainingSessionStatus, s.status)}</Badge>
      </div>

      <dl className="mt-6 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
        <dt className="text-muted-foreground">Start</dt>
        <dd>{fmtDate(s.startAtUtc)}</dd>
        <dt className="text-muted-foreground">End</dt>
        <dd>{fmtDate(s.endAtUtc)}</dd>
        <dt className="text-muted-foreground">Capacity</dt>
        <dd>{s.capacity}</dd>
        <dt className="text-muted-foreground">Price</dt>
        <dd>{fmtMoney(s.basePrice, s.currencyCode)}</dd>
        <dt className="text-muted-foreground">Trainer</dt>
        <dd>{s.trainerName ?? "—"}</dd>
      </dl>

      <div className="mt-8 rounded-md border border-border p-4">
        {meQ.isLoading ? (
          <p className="text-sm text-muted-foreground">Checking your member profile…</p>
        ) : !meQ.data ? (
          <p className="text-sm text-muted-foreground">
            Booking requires a member profile in this gym. Ask a gym admin to add you.
          </p>
        ) : myBooking ? (
          <div className="flex items-center justify-between">
            <p className="text-sm">
              You&apos;re booked.{" "}
              <span className="text-muted-foreground">
                Status: {enumLabel(BookingStatus, myBooking.status)}
              </span>
            </p>
            <Button variant="outline" disabled={cancel.isPending} onClick={() => cancel.mutate()}>
              {cancel.isPending ? "Cancelling…" : "Cancel booking"}
            </Button>
          </div>
        ) : (
          <div className="flex items-center justify-between">
            <p className="text-sm text-muted-foreground">Reserve a spot for this session.</p>
            <Button disabled={book.isPending} onClick={() => book.mutate()}>
              {book.isPending ? "Booking…" : "Book this session"}
            </Button>
          </div>
        )}
      </div>
    </section>
  );
}
