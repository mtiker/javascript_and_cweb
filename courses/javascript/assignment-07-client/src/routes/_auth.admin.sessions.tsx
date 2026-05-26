import { createFileRoute, Link } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/lib/api/auth-context";
import {
  BookingStatus,
  TrainingSessionStatus,
  getErrorMessages,
  type TrainingSession,
} from "@/lib/api/types";
import { NoActiveGym, enumLabel, fmtDate, fmtMoney } from "@/lib/ui-helpers";

export const Route = createFileRoute("/_auth/admin/sessions")({
  component: AdminSessionsPage,
});

type SessionForm = {
  categoryId: string;
  name: string;
  description: string;
  startAtUtc: string;
  endAtUtc: string;
  capacity: number;
  basePrice: number;
  currencyCode: string;
  status: TrainingSessionStatus;
  trainerStaffId: string;
};

const emptyForm: SessionForm = {
  categoryId: "",
  name: "",
  description: "",
  startAtUtc: "",
  endAtUtc: "",
  capacity: 10,
  basePrice: 0,
  currencyCode: "EUR",
  status: TrainingSessionStatus.Published,
  trainerStaffId: "",
};

function toLocalInput(iso: string): string {
  // datetime-local needs YYYY-MM-DDTHH:mm in local time
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function AdminSessionsPage() {
  const auth = useAuth();
  const gym = auth.activeGym;
  const qc = useQueryClient();

  const sessionsQ = useQuery({
    enabled: !!gym,
    queryKey: ["admin-sessions", gym],
    queryFn: () => auth.api.getTrainingSessions(gym!),
  });
  const categoriesQ = useQuery({
    enabled: !!gym,
    queryKey: ["categories", gym],
    queryFn: () => auth.api.getTrainingCategories(gym!),
  });
  const bookingsQ = useQuery({
    enabled: !!gym,
    queryKey: ["bookings", gym],
    queryFn: () => auth.api.getBookings(gym!),
  });
  const staffQ = useQuery({
    enabled: !!gym,
    queryKey: ["staff", gym],
    queryFn: () => auth.api.getStaff(gym!),
  });

  const [form, setForm] = useState<SessionForm>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<SessionForm | null>(null);
  const [rosterSessionId, setRosterSessionId] = useState<string | null>(null);

  const buildUpsert = (f: SessionForm) => ({
    categoryId: f.categoryId,
    name: f.name,
    description: f.description || null,
    startAtUtc: new Date(f.startAtUtc).toISOString(),
    endAtUtc: new Date(f.endAtUtc).toISOString(),
    capacity: Number(f.capacity),
    basePrice: Number(f.basePrice),
    currencyCode: f.currencyCode,
    status: f.status,
    trainerStaffId: f.trainerStaffId || null,
  });

  const create = useMutation({
    mutationFn: () => auth.api.createTrainingSession(gym!, buildUpsert(form)),
    onSuccess: () => {
      toast.success("Session created.");
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["admin-sessions", gym] });
      qc.invalidateQueries({ queryKey: ["sessions", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const update = useMutation({
    mutationFn: () => {
      if (!editingId || !editForm) throw new Error("No session selected");
      return auth.api.updateTrainingSession(gym!, editingId, buildUpsert(editForm));
    },
    onSuccess: () => {
      toast.success("Session updated.");
      setEditingId(null);
      setEditForm(null);
      qc.invalidateQueries({ queryKey: ["admin-sessions", gym] });
      qc.invalidateQueries({ queryKey: ["sessions", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const attendance = useMutation({
    mutationFn: ({ bookingId, status }: { bookingId: string; status: BookingStatus }) =>
      auth.api.updateAttendance(gym!, bookingId, { status }),
    onSuccess: () => {
      toast.success("Attendance updated.");
      qc.invalidateQueries({ queryKey: ["bookings", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const openEdit = (s: TrainingSession) => {
    setEditingId(s.id);
    setEditForm({
      categoryId: s.categoryId,
      name: s.name,
      description: s.description ?? "",
      startAtUtc: toLocalInput(s.startAtUtc),
      endAtUtc: toLocalInput(s.endAtUtc),
      capacity: s.capacity,
      basePrice: s.basePrice,
      currencyCode: s.currencyCode,
      status: s.status,
      trainerStaffId: s.trainerStaffId ?? "",
    });
  };

  if (!gym) return <NoActiveGym />;

  const bookingCount = (sessionId: string) =>
    (bookingsQ.data ?? []).filter(
      (b) => b.trainingSessionId === sessionId && b.status !== BookingStatus.Cancelled,
    ).length;

  const rosterSession = sessionsQ.data?.find((s) => s.id === rosterSessionId) ?? null;
  const rosterBookings = (bookingsQ.data ?? []).filter(
    (b) => b.trainingSessionId === rosterSessionId,
  );

  return (
    <section className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold">Manage sessions</h1>
        <p className="text-sm text-muted-foreground">
          Create, edit and mark attendance for training sessions.
        </p>
      </header>

      <form
        onSubmit={(e: FormEvent) => {
          e.preventDefault();
          create.mutate();
        }}
        className="rounded-md border border-border p-4"
      >
        <h2 className="text-sm font-semibold">New session</h2>
        <SessionFields
          form={form}
          setForm={setForm}
          categories={categoriesQ.data ?? []}
          staff={staffQ.data ?? []}
        />
        <Button type="submit" className="mt-3" disabled={create.isPending}>
          {create.isPending ? "Saving…" : "Create session"}
        </Button>
      </form>

      {sessionsQ.isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
      {sessionsQ.isError && (
        <p className="text-sm text-destructive">{getErrorMessages(sessionsQ.error).join(" ")}</p>
      )}

      {sessionsQ.data && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Name</th>
                <th className="px-3 py-2">Start</th>
                <th className="px-3 py-2">Capacity</th>
                <th className="px-3 py-2">Bookings</th>
                <th className="px-3 py-2">Price</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {sessionsQ.data.map((s) => (
                <tr key={s.id} className="border-t border-border">
                  <td className="px-3 py-2 font-medium">{s.name}</td>
                  <td className="px-3 py-2">{fmtDate(s.startAtUtc)}</td>
                  <td className="px-3 py-2">{s.capacity}</td>
                  <td className="px-3 py-2">{bookingCount(s.id)}</td>
                  <td className="px-3 py-2">{fmtMoney(s.basePrice, s.currencyCode)}</td>
                  <td className="px-3 py-2">
                    <Badge variant="secondary">{enumLabel(TrainingSessionStatus, s.status)}</Badge>
                  </td>
                  <td className="px-3 py-2 text-right whitespace-nowrap">
                    <Button size="sm" variant="ghost" onClick={() => setRosterSessionId(s.id)}>
                      Roster
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => openEdit(s)}>
                      Edit
                    </Button>
                    <Link
                      to="/sessions/$sessionId"
                      params={{ sessionId: s.id }}
                      className="ml-2 text-xs underline text-muted-foreground"
                    >
                      view
                    </Link>
                  </td>
                </tr>
              ))}
              {sessionsQ.data.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-3 py-6 text-center text-muted-foreground">
                    No sessions yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Edit dialog */}
      <Dialog
        open={editingId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditingId(null);
            setEditForm(null);
          }
        }}
      >
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Edit session</DialogTitle>
          </DialogHeader>
          {editForm && (
            <form
              onSubmit={(e: FormEvent) => {
                e.preventDefault();
                update.mutate();
              }}
              className="space-y-3"
            >
              <SessionFields
                form={editForm}
                setForm={(next) => setEditForm(next)}
                categories={categoriesQ.data ?? []}
                staff={staffQ.data ?? []}
                showStatus
              />
              <DialogFooter>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setEditingId(null);
                    setEditForm(null);
                  }}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={update.isPending}>
                  {update.isPending ? "Saving…" : "Save changes"}
                </Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>

      {/* Roster dialog */}
      <Dialog
        open={rosterSessionId !== null}
        onOpenChange={(open) => {
          if (!open) setRosterSessionId(null);
        }}
      >
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Roster — {rosterSession?.name ?? ""}</DialogTitle>
          </DialogHeader>
          {rosterBookings.length === 0 ? (
            <p className="text-sm text-muted-foreground">No bookings for this session.</p>
          ) : (
            <div className="space-y-2">
              {rosterBookings.map((b) => (
                <div
                  key={b.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border p-3"
                >
                  <div>
                    <div className="text-sm font-medium">{b.memberName}</div>
                    <div className="text-xs text-muted-foreground font-mono">{b.memberCode}</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary">{enumLabel(BookingStatus, b.status)}</Badge>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={attendance.isPending || b.status === BookingStatus.Attended}
                      onClick={() =>
                        attendance.mutate({ bookingId: b.id, status: BookingStatus.Attended })
                      }
                    >
                      Attended
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={attendance.isPending || b.status === BookingStatus.NoShow}
                      onClick={() =>
                        attendance.mutate({ bookingId: b.id, status: BookingStatus.NoShow })
                      }
                    >
                      No show
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={attendance.isPending || b.status === BookingStatus.Cancelled}
                      onClick={() =>
                        attendance.mutate({ bookingId: b.id, status: BookingStatus.Cancelled })
                      }
                    >
                      Cancel
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </section>
  );
}

function SessionFields({
  form,
  setForm,
  categories,
  staff,
  showStatus,
}: {
  form: SessionForm;
  setForm: (next: SessionForm) => void;
  categories: { id: string; name: string }[];
  staff: { id: string; fullName: string }[];
  showStatus?: boolean;
}) {
  return (
    <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <div className="space-y-1">
        <Label>Category</Label>
        <select
          required
          value={form.categoryId}
          onChange={(e) => setForm({ ...form, categoryId: e.target.value })}
          className="h-9 w-full rounded-md border border-input bg-background px-2 text-sm"
        >
          <option value="">Pick…</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>
      <div className="space-y-1">
        <Label>Name</Label>
        <Input
          required
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />
      </div>
      <div className="space-y-1">
        <Label>Trainer</Label>
        <select
          value={form.trainerStaffId}
          onChange={(e) => setForm({ ...form, trainerStaffId: e.target.value })}
          className="h-9 w-full rounded-md border border-input bg-background px-2 text-sm"
        >
          <option value="">— none —</option>
          {staff.map((s) => (
            <option key={s.id} value={s.id}>
              {s.fullName}
            </option>
          ))}
        </select>
      </div>
      {showStatus && (
        <div className="space-y-1">
          <Label>Status</Label>
          <select
            value={form.status}
            onChange={(e) =>
              setForm({ ...form, status: Number(e.target.value) as TrainingSessionStatus })
            }
            className="h-9 w-full rounded-md border border-input bg-background px-2 text-sm"
          >
            {Object.entries(TrainingSessionStatus)
              .filter(([, v]) => typeof v === "number")
              .map(([k, v]) => (
                <option key={k} value={v as number}>
                  {k}
                </option>
              ))}
          </select>
        </div>
      )}
      <div className="space-y-1">
        <Label>Start</Label>
        <Input
          type="datetime-local"
          required
          value={form.startAtUtc}
          onChange={(e) => setForm({ ...form, startAtUtc: e.target.value })}
        />
      </div>
      <div className="space-y-1">
        <Label>End</Label>
        <Input
          type="datetime-local"
          required
          value={form.endAtUtc}
          onChange={(e) => setForm({ ...form, endAtUtc: e.target.value })}
        />
      </div>
      <div className="space-y-1">
        <Label>Capacity</Label>
        <Input
          type="number"
          min={1}
          required
          value={form.capacity}
          onChange={(e) => setForm({ ...form, capacity: Number(e.target.value) })}
        />
      </div>
      <div className="space-y-1">
        <Label>Price</Label>
        <Input
          type="number"
          min={0}
          step="0.01"
          required
          value={form.basePrice}
          onChange={(e) => setForm({ ...form, basePrice: Number(e.target.value) })}
        />
      </div>
      <div className="space-y-1">
        <Label>Currency</Label>
        <Input
          required
          value={form.currencyCode}
          onChange={(e) => setForm({ ...form, currencyCode: e.target.value.toUpperCase() })}
        />
      </div>
      <div className="space-y-1 sm:col-span-2 lg:col-span-4">
        <Label>Description</Label>
        <Textarea
          value={form.description}
          onChange={(e) => setForm({ ...form, description: e.target.value })}
          rows={2}
          placeholder="Optional details about this session"
        />
      </div>
    </div>
  );
}
