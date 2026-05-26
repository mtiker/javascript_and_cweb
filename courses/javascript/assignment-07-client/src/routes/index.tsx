import { createFileRoute, Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/api/auth-context";
import {
  ArrowRight,
  Dumbbell,
  CalendarCheck,
  BarChart3,
  ShieldCheck,
  Zap,
  Users,
} from "lucide-react";
import heroImg from "@/assets/hero-gym.jpg";

export const Route = createFileRoute("/")({
  component: Index,
});

function Index() {
  const auth = useAuth();
  return (
    <div className="-mx-4 -my-6">
      {/* HERO */}
      <section className="relative overflow-hidden pt-20 pb-24 px-6">
        {/* Decorative glow */}
        <div
          className="pointer-events-none absolute top-0 left-1/2 -translate-x-1/2 h-[600px] w-full max-w-7xl rounded-full opacity-60 blur-[120px]"
          style={{
            background:
              "linear-gradient(to bottom, color-mix(in oklab, var(--primary) 18%, transparent), transparent)",
          }}
        />

        <div className="relative z-10 mx-auto max-w-6xl">
          <div className="inline-flex items-center gap-2 rounded-full border border-border bg-card/50 px-3 py-1 backdrop-blur-sm">
            <span className="size-2 animate-pulse rounded-full bg-gradient-to-r from-primary to-destructive" />
            <span className="font-mono text-[10px] font-bold uppercase tracking-widest text-muted-foreground">
              Multi-Gym SaaS Platform
            </span>
          </div>

          <div className="mt-8 flex flex-col items-center gap-12 lg:flex-row">
            <div className="flex-1 text-center lg:text-left">
              <h1 className="text-5xl font-extrabold leading-[1.1] tracking-tight md:text-7xl">
                Train harder.
                <br />
                <span
                  className="bg-clip-text text-transparent"
                  style={{ backgroundImage: "var(--gradient-primary)" }}
                >
                  Run smarter.
                </span>
              </h1>
              <p className="mx-auto mt-6 max-w-xl text-lg leading-relaxed text-muted-foreground lg:mx-0">
                One client for every gym in the network. Book sessions, manage memberships, and run
                your fitness business — all wired to the high-performance CWeb API.
              </p>
              <div className="mt-10 flex flex-col justify-center gap-4 sm:flex-row lg:justify-start">
                {auth.isAuthenticated ? (
                  <Button asChild size="lg" className="shadow-[var(--shadow-glow)]">
                    <Link to="/sessions">
                      Browse sessions <ArrowRight className="ml-1" />
                    </Link>
                  </Button>
                ) : (
                  <>
                    <Button asChild size="lg" className="shadow-[var(--shadow-glow)]">
                      <Link to="/login">
                        Get started <ArrowRight className="ml-1" />
                      </Link>
                    </Button>
                    <Button asChild size="lg" variant="outline">
                      <Link to="/register">Create account</Link>
                    </Button>
                  </>
                )}
              </div>
            </div>

            <div className="group relative flex-1">
              <div
                className="absolute -inset-1 rounded-3xl opacity-60 blur-2xl transition-all group-hover:blur-3xl"
                style={{ background: "var(--gradient-primary)" }}
              />
              <div className="relative overflow-hidden rounded-2xl border border-border bg-card/50 p-2">
                <img
                  src={heroImg}
                  alt="Athlete training in a dark industrial gym"
                  width={800}
                  height={600}
                  className="w-full rounded-xl opacity-80 grayscale contrast-125 transition-all duration-700 group-hover:grayscale-0 group-hover:opacity-100"
                />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* STATS */}
      <section className="border-y border-border bg-card/20 backdrop-blur-md">
        <div className="mx-auto max-w-6xl px-6 py-12">
          <div className="grid grid-cols-2 gap-12 md:grid-cols-3">
            <Stat value="12+" label="Partner Gyms" />
            <Stat value="2.5k" label="Sessions / MO" />
            <Stat value="98%" label="Show-up Rate" className="col-span-2 md:col-span-1" />
          </div>
        </div>
      </section>

      {/* FEATURES — Bento */}
      <section className="px-6 py-24">
        <div className="mx-auto max-w-6xl">
          <div className="mb-16">
            <h2 className="text-3xl font-bold md:text-4xl">Everything you need under one roof</h2>
            <p className="mt-4 max-w-2xl text-muted-foreground">
              A complete frontend for the cweb multi-gym backend — built for members and the people
              running the gym.
            </p>
          </div>

          <div className="mb-6 grid gap-6 md:grid-cols-3">
            {/* Wide card */}
            <article className="group relative overflow-hidden rounded-3xl border border-border/60 bg-card/40 transition-all hover:border-primary/40 md:col-span-2">
              <div className="absolute inset-0 bg-gradient-to-br from-primary/5 to-transparent opacity-0 transition-opacity group-hover:opacity-100" />
              <div className="relative z-10 p-8">
                <div className="mb-6 flex size-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
                  <CalendarCheck className="size-6" />
                </div>
                <h3 className="mb-2 text-xl font-bold">Classes & sessions</h3>
                <p className="max-w-sm text-sm leading-relaxed text-muted-foreground">
                  Filter by category, reserve a spot, and cancel in one tap. Capacity tracked in
                  real time across the entire network.
                </p>
              </div>
              <div className="pointer-events-none absolute bottom-0 right-0 p-4 opacity-20 grayscale transition-all group-hover:opacity-60 group-hover:grayscale-0">
                <div className="h-32 w-48 rounded-tl-2xl border-l border-t border-border bg-muted" />
              </div>
            </article>

            {/* Narrow card */}
            <article className="group relative overflow-hidden rounded-3xl border border-border/60 bg-card/40 transition-all hover:border-destructive/40">
              <div className="p-8">
                <div className="mb-6 flex size-12 items-center justify-center rounded-xl bg-destructive/10 text-destructive">
                  <BarChart3 className="size-6" />
                </div>
                <h3 className="mb-2 text-xl font-bold">Memberships</h3>
                <p className="text-sm leading-relaxed text-muted-foreground">
                  Active plans, renewal dates, and payment history — all read straight from the API.
                </p>
              </div>
            </article>
          </div>

          {/* Tall feature row */}
          <div className="mb-6 grid gap-6 md:grid-cols-3">
            <article className="group relative overflow-hidden rounded-3xl border border-border/60 bg-card/40 p-8 transition-all hover:border-primary/40">
              <div className="mb-6 flex size-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
                <Dumbbell className="size-6" />
              </div>
              <h3 className="mb-2 text-xl font-bold">Strength & equipment</h3>
              <p className="text-sm leading-relaxed text-muted-foreground">
                See what's available, who's training, and what's bookable across every connected
                gym.
              </p>
            </article>

            <article className="group relative overflow-hidden rounded-3xl border border-border/60 bg-card/40 p-8 transition-all hover:border-primary/40 md:col-span-2">
              <div className="mb-6 flex size-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
                <Zap className="size-6" />
              </div>
              <h3 className="mb-2 text-xl font-bold">Realtime everywhere</h3>
              <p className="max-w-md text-sm leading-relaxed text-muted-foreground">
                Bookings, cancellations, capacity counters and payment events stream live — no
                refresh required.
              </p>
            </article>
          </div>

          {/* Mini technical features */}
          <div className="grid gap-6 sm:grid-cols-3">
            <TechFeature
              icon={<ShieldCheck className="size-5" />}
              title="JWT + Refresh"
              code="SECURE_AUTH_LAYER"
            />
            <TechFeature
              icon={<Users className="size-5" />}
              title="Role aware"
              code="DYNAMIC_RBAC"
            />
            <TechFeature
              icon={<Zap className="size-5" />}
              title="Multi-tenant"
              code="CROSS_NETWORK_HUB"
            />
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="relative px-6 py-24">
        <div className="mx-auto max-w-4xl text-center">
          <h2 className="mb-8 text-4xl font-extrabold md:text-5xl">Ready to lift?</h2>
          <div className="flex flex-col items-center gap-6">
            <div className="flex flex-wrap items-center justify-center gap-3 rounded-lg border border-border bg-card px-4 py-2">
              <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                API Endpoint
              </span>
              <code className="text-sm text-primary">
                {import.meta.env.VITE_API_BASE_URL ?? "https://mtiker-cweb-4.proxy.itcollege.ee"}
              </code>
            </div>
            <Button asChild size="lg" className="shadow-[var(--shadow-glow)]">
              <Link to={auth.isAuthenticated ? "/sessions" : "/login"}>
                {auth.isAuthenticated ? "Go to sessions" : "Sign in"}{" "}
                <ArrowRight className="ml-1" />
              </Link>
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}

function Stat({
  value,
  label,
  className = "",
}: {
  value: string;
  label: string;
  className?: string;
}) {
  return (
    <div className={`space-y-1 ${className}`}>
      <div className="text-4xl font-extrabold tracking-tighter">{value}</div>
      <div className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
        {label}
      </div>
    </div>
  );
}

function TechFeature({
  icon,
  title,
  code,
}: {
  icon: React.ReactNode;
  title: string;
  code: string;
}) {
  return (
    <div className="flex items-start gap-4 rounded-2xl border border-border/60 bg-card/40 p-6">
      <div className="rounded-lg bg-muted p-2 text-primary">{icon}</div>
      <div>
        <h4 className="mb-1 text-sm font-bold">{title}</h4>
        <p className="font-mono text-xs text-muted-foreground">{code}</p>
      </div>
    </div>
  );
}
