import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/lib/api/auth-context";
import { MemberStatus, getErrorMessages, type MemberSummary } from "@/lib/api/types";
import { NoActiveGym, enumLabel } from "@/lib/ui-helpers";

export const Route = createFileRoute("/_auth/admin/members")({
  component: AdminMembersPage,
});

const CODE_PREFIX = "M";
const CODE_PAD = 4;

function suggestMemberCode(existing: MemberSummary[]): string {
  let maxN = 0;
  for (const m of existing) {
    const match = /^([A-Za-z]*)(\d+)$/.exec(m.memberCode ?? "");
    if (match) {
      const n = parseInt(match[2], 10);
      if (n > maxN) maxN = n;
    }
  }
  return `${CODE_PREFIX}${String(maxN + 1).padStart(CODE_PAD, "0")}`;
}

type EditForm = {
  firstName: string;
  lastName: string;
  personalCode: string;
  dateOfBirth: string;
  status: MemberStatus;
  memberCode: string;
};

function AdminMembersPage() {
  const auth = useAuth();
  const gym = auth.activeGym;
  const qc = useQueryClient();
  const listQ = useQuery({
    enabled: !!gym,
    queryKey: ["admin-members", gym],
    queryFn: () => auth.api.getMembers(gym!),
  });

  const suggestedCode = useMemo(() => suggestMemberCode(listQ.data ?? []), [listQ.data]);

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    personalCode: "",
    dateOfBirth: "",
  });

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<EditForm | null>(null);

  const create = useMutation({
    mutationFn: () =>
      auth.api.createMember(gym!, {
        firstName: form.firstName,
        lastName: form.lastName,
        memberCode: suggestedCode,
        personalCode: form.personalCode || null,
        dateOfBirth: form.dateOfBirth || null,
        status: MemberStatus.Active,
      }),
    onSuccess: () => {
      toast.success("Member added.");
      setForm({ firstName: "", lastName: "", personalCode: "", dateOfBirth: "" });
      qc.invalidateQueries({ queryKey: ["admin-members", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const del = useMutation({
    mutationFn: (id: string) => auth.api.deleteMember(gym!, id),
    onSuccess: () => {
      toast.success("Removed.");
      qc.invalidateQueries({ queryKey: ["admin-members", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const update = useMutation({
    mutationFn: async () => {
      if (!editingId || !editForm) throw new Error("No member selected");
      return auth.api.updateMember(gym!, editingId, {
        firstName: editForm.firstName,
        lastName: editForm.lastName,
        memberCode: editForm.memberCode,
        personalCode: editForm.personalCode || null,
        dateOfBirth: editForm.dateOfBirth || null,
        status: editForm.status,
      });
    },
    onSuccess: () => {
      toast.success("Member updated.");
      setEditingId(null);
      setEditForm(null);
      qc.invalidateQueries({ queryKey: ["admin-members", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const openEdit = async (id: string) => {
    try {
      const detail = await auth.api.getMember(gym!, id);
      setEditingId(id);
      setEditForm({
        firstName: detail.firstName,
        lastName: detail.lastName,
        personalCode: detail.personalCode ?? "",
        dateOfBirth: detail.dateOfBirth ? detail.dateOfBirth.slice(0, 10) : "",
        status: detail.status,
        memberCode: detail.memberCode,
      });
    } catch (e) {
      getErrorMessages(e).forEach((m) => toast.error(m));
    }
  };

  if (!gym) return <NoActiveGym />;

  return (
    <section className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold">Members</h1>
        <p className="text-sm text-muted-foreground">Gym {gym} — admin view.</p>
      </header>

      <form
        onSubmit={(e: FormEvent) => {
          e.preventDefault();
          create.mutate();
        }}
        className="rounded-md border border-border p-4"
      >
        <h2 className="text-sm font-semibold">Add member</h2>
        <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
          <div className="space-y-1">
            <Label htmlFor="firstName">First name</Label>
            <Input
              id="firstName"
              required
              value={form.firstName}
              onChange={(e) => setForm({ ...form, firstName: e.target.value })}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="lastName">Last name</Label>
            <Input
              id="lastName"
              required
              value={form.lastName}
              onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            />
          </div>
          <div className="space-y-1">
            <Label>Member code</Label>
            <div className="flex h-10 items-center rounded-md border border-border bg-muted/40 px-3 text-sm font-mono text-muted-foreground">
              {suggestedCode}
            </div>
            <p className="text-xs text-muted-foreground">Auto-generated in order.</p>
          </div>
          <div className="space-y-1">
            <Label htmlFor="personalCode">Personal code</Label>
            <Input
              id="personalCode"
              value={form.personalCode}
              onChange={(e) => setForm({ ...form, personalCode: e.target.value })}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="dob">DoB</Label>
            <Input
              id="dob"
              type="date"
              value={form.dateOfBirth}
              onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
            />
          </div>
        </div>
        <Button type="submit" className="mt-3" disabled={create.isPending}>
          {create.isPending ? "Saving…" : "Add"}
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
                <th className="px-3 py-2">Code</th>
                <th className="px-3 py-2">Name</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {listQ.data.map((m: MemberSummary) => (
                <tr key={m.id} className="border-t border-border">
                  <td className="px-3 py-2 font-mono">{m.memberCode}</td>
                  <td className="px-3 py-2">{m.fullName}</td>
                  <td className="px-3 py-2">{enumLabel(MemberStatus, m.status)}</td>
                  <td className="px-3 py-2 text-right">
                    <Button size="sm" variant="ghost" onClick={() => openEdit(m.id)}>
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={del.isPending}
                      onClick={() => {
                        if (confirm(`Delete ${m.fullName}?`)) del.mutate(m.id);
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
                    No members yet.
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
            <DialogTitle>Edit member</DialogTitle>
          </DialogHeader>
          {editForm && (
            <form
              onSubmit={(e: FormEvent) => {
                e.preventDefault();
                update.mutate();
              }}
              className="space-y-3"
            >
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label htmlFor="ef-firstName">First name</Label>
                  <Input
                    id="ef-firstName"
                    required
                    value={editForm.firstName}
                    onChange={(e) => setEditForm({ ...editForm, firstName: e.target.value })}
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="ef-lastName">Last name</Label>
                  <Input
                    id="ef-lastName"
                    required
                    value={editForm.lastName}
                    onChange={(e) => setEditForm({ ...editForm, lastName: e.target.value })}
                  />
                </div>
                <div className="space-y-1">
                  <Label>Member code</Label>
                  <div className="flex h-10 items-center rounded-md border border-border bg-muted/40 px-3 text-sm font-mono text-muted-foreground">
                    {editForm.memberCode}
                  </div>
                </div>
                <div className="space-y-1">
                  <Label htmlFor="ef-status">Status</Label>
                  <select
                    id="ef-status"
                    value={editForm.status}
                    onChange={(e) =>
                      setEditForm({ ...editForm, status: Number(e.target.value) as MemberStatus })
                    }
                    className="h-10 w-full rounded-md border border-input bg-background px-2 text-sm"
                  >
                    {Object.entries(MemberStatus)
                      .filter(([, v]) => typeof v === "number")
                      .map(([k, v]) => (
                        <option key={k} value={v as number}>
                          {k}
                        </option>
                      ))}
                  </select>
                </div>
                <div className="space-y-1">
                  <Label htmlFor="ef-personalCode">Personal code</Label>
                  <Input
                    id="ef-personalCode"
                    value={editForm.personalCode}
                    onChange={(e) => setEditForm({ ...editForm, personalCode: e.target.value })}
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="ef-dob">DoB</Label>
                  <Input
                    id="ef-dob"
                    type="date"
                    value={editForm.dateOfBirth}
                    onChange={(e) => setEditForm({ ...editForm, dateOfBirth: e.target.value })}
                  />
                </div>
              </div>
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
