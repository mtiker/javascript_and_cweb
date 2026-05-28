import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { useAuth } from "@/lib/api/auth-context";
import { TrainingSessionStatus, getErrorMessages } from "@/lib/api/types";
import { NoActiveGym, enumLabel, fmtDate, fmtMoney } from "@/lib/ui-helpers";
import { PageBanner } from "@/components/page-banner";
import bannerImg from "@/assets/banner-sessions.jpg";
import { Calendar, Users, Wallet, UserCircle2 } from "lucide-react";

export const Route = createFileRoute("/_auth/sessions/")({
  component: SessionsPage,
});

function SessionsPage() {
  const auth = useAuth();
  const gym = auth.activeGym;
  const [filter, setFilter] = useState("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [upcomingOnly, setUpcomingOnly] = useState(true);

  const sessionsQ = useQuery({
    enabled: !!gym,
    queryKey: ["sessions", gym],
    queryFn: () => auth.api.getTrainingSessions(gym!),
  });
  const categoriesQ = useQuery({
    enabled: !!gym,
    queryKey: ["categories", gym],
    queryFn: () => auth.api.getTrainingCategories(gym!),
  });

  const rows = useMemo(() => {
    const list = sessionsQ.data ?? [];
    const now = Date.now();
    return list.filter((s) => {
      if (categoryId && s.categoryId !== categoryId) return false;
      if (upcomingOnly && new Date(s.startAtUtc).getTime() < now) return false;
      if (filter) {
        const q = filter.toLowerCase();
        return s.name.toLowerCase().includes(q) || (s.description ?? "").toLowerCase().includes(q);
      }
      return true;
    });
  }, [sessionsQ.data, filter, categoryId, upcomingOnly]);

  if (!gym) return <NoActiveGym />;

  return (
    <section>
      <PageBanner
        image={bannerImg}
        eyebrow="Training"
        title="Sessions"
        subtitle={
          <>
            Browse and book sessions at <strong className="text-foreground">{gym}</strong>.
          </>
        }
        imagePosition="center"
      />

      <header className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-muted-foreground">
          {sessionsQ.data?.length ?? 0} session{(sessionsQ.data?.length ?? 0) === 1 ? "" : "s"}{" "}
          available
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <Input
            placeholder="Search…"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="w-48"
          />
          <select
            value={categoryId}
            onChange={(e) => setCategoryId(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-2 text-sm"
          >
            <option value="">All categories</option>
            {(categoriesQ.data ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
          <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <input
              type="checkbox"
              checked={upcomingOnly}
              onChange={(e) => setUpcomingOnly(e.target.checked)}
              className="size-3.5 accent-primary"
            />
            Upcoming only
          </label>
        </div>
      </header>

      {sessionsQ.isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
      {sessionsQ.isError && (
        <p className="text-sm text-destructive">{getErrorMessages(sessionsQ.error).join(" ")}</p>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {rows.map((s) => (
          <Link
            key={s.id}
            to="/sessions/$sessionId"
            params={{ sessionId: s.id }}
            className="group relative block overflow-hidden rounded-xl border border-border/60 bg-card p-5 shadow-[var(--shadow-elegant)] transition-all duration-300 hover:-translate-y-1 hover:border-primary/40"
          >
            <div
              aria-hidden
              className="pointer-events-none absolute -right-10 -top-10 size-32 rounded-full opacity-0 blur-3xl transition-opacity duration-500 group-hover:opacity-100"
              style={{ background: "var(--gradient-primary)" }}
            />
            <div className="relative flex items-start justify-between gap-2">
              <div className="flex items-center gap-2">
                <div className="flex size-9 items-center justify-center rounded-lg bg-primary/15 text-primary ring-1 ring-primary/30">
                  <Calendar className="size-4" />
                </div>
                <h3 className="font-semibold leading-tight">{s.name}</h3>
              </div>
              <Badge variant="secondary">{enumLabel(TrainingSessionStatus, s.status)}</Badge>
            </div>
            <p className="relative mt-3 line-clamp-2 text-xs text-muted-foreground">
              {s.description ?? "—"}
            </p>
            <dl className="relative mt-4 grid grid-cols-2 gap-x-3 gap-y-2 text-xs">
              <DataCell
                icon={<Calendar className="size-3" />}
                label="Start"
                value={fmtDate(s.startAtUtc)}
              />
              <DataCell
                icon={<Users className="size-3" />}
                label="Capacity"
                value={String(s.capacity)}
              />
              <DataCell
                icon={<Wallet className="size-3" />}
                label="Price"
                value={fmtMoney(s.basePrice, s.currencyCode)}
              />
              <DataCell
                icon={<UserCircle2 className="size-3" />}
                label="Trainer"
                value={s.trainerName ?? "—"}
              />
            </dl>
          </Link>
        ))}
        {!sessionsQ.isLoading && rows.length === 0 && (
          <p className="col-span-full text-sm text-muted-foreground">No sessions match.</p>
        )}
      </div>
    </section>
  );
}

function DataCell({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div>
      <dt className="flex items-center gap-1 text-muted-foreground">
        {icon}
        {label}
      </dt>
      <dd className="mt-0.5 truncate font-medium text-foreground">{value}</dd>
    </div>
  );
}
