import { createFileRoute } from "@tanstack/react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useAuth } from "@/lib/api/auth-context";
import { getErrorMessages } from "@/lib/api/types";
import { NoActiveGym } from "@/lib/ui-helpers";

export const Route = createFileRoute("/_auth/admin/categories")({
  component: AdminCategoriesPage,
});

function AdminCategoriesPage() {
  const auth = useAuth();
  const gym = auth.activeGym;
  const qc = useQueryClient();
  const listQ = useQuery({
    enabled: !!gym,
    queryKey: ["admin-categories", gym],
    queryFn: () => auth.api.getTrainingCategories(gym!),
  });
  const [form, setForm] = useState({ name: "", description: "" });

  const create = useMutation({
    mutationFn: () =>
      auth.api.createTrainingCategory(gym!, {
        name: form.name,
        description: form.description || null,
      }),
    onSuccess: () => {
      toast.success("Category added.");
      setForm({ name: "", description: "" });
      qc.invalidateQueries({ queryKey: ["admin-categories", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  const del = useMutation({
    mutationFn: (id: string) => auth.api.deleteTrainingCategory(gym!, id),
    onSuccess: () => {
      toast.success("Removed.");
      qc.invalidateQueries({ queryKey: ["admin-categories", gym] });
    },
    onError: (e) => getErrorMessages(e).forEach((m) => toast.error(m)),
  });

  if (!gym) return <NoActiveGym />;

  return (
    <section className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold">Training categories</h1>
      </header>

      <form
        onSubmit={(e: FormEvent) => {
          e.preventDefault();
          create.mutate();
        }}
        className="rounded-md border border-border p-4"
      >
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-1">
            <Label htmlFor="name">Name</Label>
            <Input
              id="name"
              required
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              rows={2}
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>
        </div>
        <Button type="submit" className="mt-3" disabled={create.isPending}>
          {create.isPending ? "Saving…" : "Add category"}
        </Button>
      </form>

      {listQ.isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
      {listQ.isError && (
        <p className="text-sm text-destructive">{getErrorMessages(listQ.error).join(" ")}</p>
      )}
      {listQ.data && (
        <ul className="space-y-2">
          {listQ.data.map((c) => (
            <li
              key={c.id}
              className="flex items-start justify-between rounded-md border border-border p-3"
            >
              <div>
                <p className="font-medium">{c.name}</p>
                <p className="text-sm text-muted-foreground">{c.description ?? "—"}</p>
              </div>
              <Button
                size="sm"
                variant="ghost"
                disabled={del.isPending}
                onClick={() => {
                  if (confirm(`Delete ${c.name}?`)) del.mutate(c.id);
                }}
              >
                Delete
              </Button>
            </li>
          ))}
          {listQ.data.length === 0 && (
            <p className="text-sm text-muted-foreground">No categories yet.</p>
          )}
        </ul>
      )}
    </section>
  );
}
