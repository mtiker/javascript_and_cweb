import { createFileRoute, useNavigate, Link } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/api/auth-context";
import { getErrorMessages } from "@/lib/api/types";
import { DEV_ACCOUNTS, DEV_BYPASS_ENABLED } from "@/lib/dev-accounts";
import { Zap, ShieldCheck } from "lucide-react";
import { Logo } from "@/components/logo";
import heroImg from "@/assets/hero-gym.jpg";

export const Route = createFileRoute("/login")({
  component: LoginPage,
});

function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState<string | null>(null);

  async function doLogin(creds: { email: string; password: string }, tag: string) {
    setSubmitting(tag);
    try {
      await auth.login(creds);
      toast.success("Signed in.");
      navigate({ to: "/sessions" });
    } catch (err) {
      getErrorMessages(err).forEach((m) => toast.error(m));
    } finally {
      setSubmitting(null);
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    await doLogin({ email, password }, "form");
  }

  return (
    <div className="-mx-4 -my-6 grid min-h-[calc(100vh-4rem)] lg:grid-cols-2">
      {/* Visual side */}
      <aside className="relative hidden overflow-hidden lg:block">
        <img
          src={heroImg}
          alt="Athlete training"
          className="h-full w-full object-cover"
          width={1600}
          height={1024}
        />
        <div
          className="absolute inset-0"
          style={{
            background:
              "linear-gradient(135deg, oklch(0.16 0.02 260 / 0.55), oklch(0.16 0.02 260 / 0.9))",
          }}
        />
        <div className="absolute inset-0 flex flex-col justify-between p-12">
          <Link to="/" className="hover:opacity-90 transition-opacity">
            <Logo size="lg" />
          </Link>
          <div className="max-w-md">
            <span className="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3 py-1 text-xs font-medium uppercase tracking-wider text-primary">
              <Zap className="size-3" /> Welcome back
            </span>
            <h2 className="mt-4 text-4xl font-bold leading-tight">
              Your next session is{" "}
              <span
                className="bg-clip-text text-transparent"
                style={{ backgroundImage: "var(--gradient-primary)" }}
              >
                one tap away.
              </span>
            </h2>
            <p className="mt-4 text-muted-foreground">
              Sign in to book classes, manage your membership, and track everything
              across every connected gym.
            </p>
          </div>
        </div>
      </aside>

      {/* Form side */}
      <section className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-md">
          <h1 className="text-3xl font-bold tracking-tight">Log in</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Use your gym account credentials to continue.
          </p>

          <form onSubmit={onSubmit} className="mt-8 space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
            <Button
              type="submit"
              disabled={submitting !== null}
              className="w-full shadow-[var(--shadow-glow)]"
              size="lg"
            >
              {submitting === "form" ? "Signing in…" : "Sign in"}
            </Button>
            <p className="text-center text-sm text-muted-foreground">
              No account?{" "}
              <Link to="/register" className="text-primary underline-offset-4 hover:underline">
                Register
              </Link>
            </p>
          </form>

          {DEV_BYPASS_ENABLED && (
            <div className="mt-8 space-y-4">
              <div className="rounded-xl border border-dashed border-primary/40 bg-primary/5 p-4">
                <div className="mb-3 flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-primary">
                  <ShieldCheck className="size-3.5" />
                  Dev shortcuts
                </div>
                <p className="mb-3 text-xs text-muted-foreground">
                  Skip the form using a seeded backend account.
                </p>
                <div className="grid gap-2">
                  {DEV_ACCOUNTS.map((a) => (
                    <Button
                      key={a.email}
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={submitting !== null}
                      onClick={() => doLogin({ email: a.email, password: a.password }, a.email)}
                      className="justify-between"
                      title={a.hint}
                    >
                      <span>{submitting === a.email ? "Signing in…" : a.label}</span>
                      <span className="truncate text-xs text-muted-foreground">{a.email}</span>
                    </Button>
                  ))}
                </div>
              </div>

              <div className="rounded-xl border border-dashed border-chart-3/50 bg-chart-3/5 p-4">
                <div className="mb-3 flex items-center gap-2 text-xs font-medium uppercase tracking-wider text-chart-3">
                  <Zap className="size-3.5" />
                  Preview mode (no backend)
                </div>
                <p className="mb-3 text-xs text-muted-foreground">
                  Fakes a session locally so you can tour the design without the
                  cweb API. Data pages will show empty / error states.
                </p>
                <div className="grid grid-cols-2 gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => {
                      auth.mockLogin("admin");
                      toast.success("Preview mode: Gym admin");
                      navigate({ to: "/sessions" });
                    }}
                  >
                    Preview as Admin
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => {
                      auth.mockLogin("member");
                      toast.success("Preview mode: Member");
                      navigate({ to: "/sessions" });
                    }}
                  >
                    Preview as Member
                  </Button>
                </div>
              </div>
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
