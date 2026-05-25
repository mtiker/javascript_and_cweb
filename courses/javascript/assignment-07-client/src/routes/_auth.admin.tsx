import { createFileRoute, Outlet, Navigate, Link, useLocation } from "@tanstack/react-router";
import { useAuth } from "@/lib/api/auth-context";
import { PageBanner } from "@/components/page-banner";
import bannerImg from "@/assets/banner-admin.jpg";
import { Users, Tags, CalendarCog } from "lucide-react";

export const Route = createFileRoute("/_auth/admin")({
  component: AdminLayout,
});

function AdminLayout() {
  const auth = useAuth();
  const location = useLocation();

  if (!auth.isAdmin) {
    return (
      <div className="py-12 text-center">
        <p className="text-sm text-muted-foreground">
          You don&apos;t have access to gym admin tools. Sign in with a GymAdmin / GymOwner role.
        </p>
        <Navigate to="/sessions" />
      </div>
    );
  }

  const tabs = [
    { to: "/admin/members", label: "Members", icon: Users },
    { to: "/admin/categories", label: "Categories", icon: Tags },
    { to: "/admin/sessions", label: "Sessions", icon: CalendarCog },
  ];

  return (
    <div>
      <PageBanner
        image={bannerImg}
        eyebrow="Gym admin"
        title="Control room"
        subtitle={
          <>
            Manage members, training catalogue and scheduled sessions for{" "}
            <strong className="text-foreground">{auth.activeGym}</strong>.
          </>
        }
        imagePosition="center 30%"
      />

      <nav className="mb-6 flex flex-wrap gap-1 rounded-xl border border-border/60 bg-card/60 p-1 backdrop-blur">
        {tabs.map((t) => {
          const active = location.pathname.startsWith(t.to);
          const Icon = t.icon;
          return (
            <Link
              key={t.to}
              to={t.to}
              className={
                "inline-flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm transition-colors " +
                (active
                  ? "bg-primary text-primary-foreground shadow-[var(--shadow-glow)]"
                  : "text-muted-foreground hover:bg-accent hover:text-foreground")
              }
            >
              <Icon className="size-4" />
              {t.label}
            </Link>
          );
        })}
      </nav>

      <Outlet />
    </div>
  );
}
