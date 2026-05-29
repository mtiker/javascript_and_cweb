import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/lib/api/auth-context";
import { StaffStatus, getErrorMessages, type Staff } from "@/lib/api/types";
import { NoActiveGym, enumLabel } from "@/lib/ui-helpers";

export const Route = createFileRoute("/_auth/admin/staff")({
  component: AdminStaffPage,
});

type StaffForm = {
  firstName: string;
  lastName: string;
  staffCode: string;
  status: StaffStatus;
};

const emptyForm: StaffForm = {
  firstName: "",
  lastName: "",
  staffCode: "",
  status: StaffStatus.Active,
};

function splitName(fullName: string): { firstName: string; lastName: string } {
  const trimmed = fullName.trim();
  const space = trimmed.indexOf(" ");
  if (space < 0) return { firstName: trimmed, lastName: "" };
  return { firstName: trimmed.slice(0, space), lastName: trimmed.slice(space + 1) };
}

function AdminStaffPage() {
  const auth = useAuth();
  const gym = auth.activeGym;
  const qc = useQueryClient();

  const listQ = useQuery({
    enabled: !!gym,
    queryKey: ["admin-staff", gym],
    queryFn: () => auth.api.getStaff(gym!),
  });

  const [form, setForm] = useState<StaffForm>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<StaffForm | null>(null);

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["admin-staff", gym] });
    qc.invalidateQueries({ queryKey: ["staff", gym] });
  };

  const create = useMutation({
    mutationFn: () => auth.api.createStaff(gym!, form),
    onSuccess: () => {
      toast.success("Trainer added.");
      setForm(emptyForm);
      invalidate();
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const update = useMutation({
    mutationFn: () => {
      if (!editingId || !editForm) throw new Error("No trainer selected");
      return auth.api.updateStaff(gym!, editingId, editForm);
    },
    onSuccess: () => {
      toast.success("Trainer updated.");
      setEditingId(null);
      setEditForm(null);
      invalidate();
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const setStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: StaffStatus }) =>
      auth.api.updateStaffStatus(gym!, id, { status }),
    onSuccess: () => {
      toast.success("Status updated.");
      invalidate();
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const del = useMutation({
    mutationFn: (id: string) => auth.api.deleteStaff(gym!, id),
    onSuccess: () => {
      toast.success("Trainer removed.");
      invalidate();
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const openEdit = (s: Staff) => {
    setEditingId(s.id);
    setEditForm({ ...splitName(s.fullName), staffCode: s.staffCode, status: s.status });
  };

  if (!gym) return <NoActiveGym />;

  return (
    <section className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold">Trainers &amp; staff</h1>
        <p className="text-sm text-muted-foreground">
          Add the trainers you can then assign to training sessions.
        </p>
      </header>

      <form
        onSubmit={(e: FormEvent) => {
          e.preventDefault();
          create.mutate();
        }}
        className="rounded-md border border-border p-4"
      >
        <h2 className="text-sm font-semibold">New trainer</h2>
        <StaffFields form={form} setForm={setForm} />
        <Button type="submit" className="mt-3" disabled={create.isPending}>
          {create.isPending ? "Saving…" : "Add trainer"}
        </Button>
      </form>

      {listQ.isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
      {listQ.isError && (
        <p className="text-sm text-destructive">{getErrorMessages(listQ.error).join(" ")}</p>
      )}

      {listQ.data && (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Name</th>
                <th className="px-3 py-2">Code</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {listQ.data.map((s) => (
                <tr key={s.id} className="border-t border-border">
                  <td className="px-3 py-2 font-medium">{s.fullName}</td>
                  <td className="px-3 py-2 font-mono text-xs">{s.staffCode}</td>
                  <td className="px-3 py-2">
                    <Badge variant="secondary">{enumLabel(StaffStatus, s.status)}</Badge>
                  </td>
                  <td className="px-3 py-2 text-right whitespace-nowrap">
                    <Button size="sm" variant="ghost" onClick={() => openEdit(s)}>
                      Edit
                    </Button>
                    {s.status === StaffStatus.Active ? (
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={setStatus.isPending}
                        onClick={() => setStatus.mutate({ id: s.id, status: StaffStatus.Inactive })}
                      >
                        Deactivate
                      </Button>
                    ) : (
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={setStatus.isPending}
                        onClick={() => setStatus.mutate({ id: s.id, status: StaffStatus.Active })}
                      >
                        Activate
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={del.isPending}
                      onClick={() => {
                        if (confirm(`Delete ${s.fullName}?`)) del.mutate(s.id);
                      }}
                    >
                      Delete
                    </Button>
                  </td>
                </tr>
              ))}
              {listQ.data.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-3 py-6 text-center text-muted-foreground">
                    No trainers yet. Add one above to assign them to sessions.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog
        open={editingId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditingId(null);
            setEditForm(null);
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit trainer</DialogTitle>
          </DialogHeader>
          {editForm && (
            <form
              onSubmit={(e: FormEvent) => {
                e.preventDefault();
                update.mutate();
              }}
              className="space-y-3"
            >
              <StaffFields form={editForm} setForm={(next) => setEditForm(next)} showStatus />
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
    </section>
  );
}

function StaffFields({
  form,
  setForm,
  showStatus,
}: {
  form: StaffForm;
  setForm: (next: StaffForm) => void;
  showStatus?: boolean;
}) {
  return (
    <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <div className="space-y-1">
        <Label>First name</Label>
        <Input
          required
          value={form.firstName}
          onChange={(e) => setForm({ ...form, firstName: e.target.value })}
        />
      </div>
      <div className="space-y-1">
        <Label>Last name</Label>
        <Input
          required
          value={form.lastName}
          onChange={(e) => setForm({ ...form, lastName: e.target.value })}
        />
      </div>
      <div className="space-y-1">
        <Label>Staff code</Label>
        <Input
          required
          value={form.staffCode}
          onChange={(e) => setForm({ ...form, staffCode: e.target.value })}
        />
      </div>
      {showStatus && (
        <div className="space-y-1">
          <Label>Status</Label>
          <select
            value={form.status}
            onChange={(e) => setForm({ ...form, status: Number(e.target.value) as StaffStatus })}
            className="h-9 w-full rounded-md border border-input bg-background px-2 text-sm"
          >
            {Object.entries(StaffStatus)
              .filter(([, v]) => typeof v === "number")
              .map(([k, v]) => (
                <option key={k} value={v as number}>
                  {k}
                </option>
              ))}
          </select>
        </div>
      )}
    </div>
  );
}
