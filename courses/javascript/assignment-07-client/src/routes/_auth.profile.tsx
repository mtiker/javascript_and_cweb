import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/lib/api/auth-context";
import { MemberStatus, getErrorMessages } from "@/lib/api/types";
import { NoActiveGym, enumLabel, fmtDate } from "@/lib/ui-helpers";
import { PageBanner } from "@/components/page-banner";
import bannerImg from "@/assets/banner-profile.jpg";

export const Route = createFileRoute("/_auth/profile")({
  component: ProfilePage,
});

function ProfilePage() {
  const auth = useAuth();
  const gym = auth.activeGym;

  const meQ = useQuery({
    enabled: !!gym,
    queryKey: ["me-member", gym],
    queryFn: () => auth.api.getCurrentMember(gym!),
    retry: false,
  });

  return (
    <section className="max-w-3xl space-y-6">
      <PageBanner
        image={bannerImg}
        eyebrow="You"
        title="My profile"
        subtitle="Session info from your JWT and your member profile at the active gym."
        imagePosition="center 40%"
      />


      <div className="rounded-md border border-border p-4">
        <h2 className="text-sm font-semibold">Session</h2>
        <dl className="mt-3 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
          <dt className="text-muted-foreground">Active gym</dt>
          <dd>{auth.activeGym ?? "—"}</dd>
          <dt className="text-muted-foreground">Active role</dt>
          <dd>{auth.session?.activeRole ?? "—"}</dd>
          <dt className="text-muted-foreground">System roles</dt>
          <dd>{auth.session?.systemRoles?.join(", ") || "—"}</dd>
          <dt className="text-muted-foreground">Available tenants</dt>
          <dd>
            {auth.session?.availableTenants?.length
              ? auth.session.availableTenants.map((t) => `${t.gymCode} (${t.roles.join("/")})`).join(", ")
              : "—"}
          </dd>
        </dl>
      </div>

      {gym && (
        <div className="rounded-md border border-border p-4">
          <h2 className="text-sm font-semibold">Member profile @ {gym}</h2>
          {meQ.isLoading && <p className="mt-2 text-sm text-muted-foreground">Loading…</p>}
          {meQ.isError && (
            <p className="mt-2 text-sm text-muted-foreground">
              {getErrorMessages(meQ.error).join(" ")}
            </p>
          )}
          {meQ.data && (
            <dl className="mt-3 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
              <dt className="text-muted-foreground">Name</dt>
              <dd>{meQ.data.fullName}</dd>
              <dt className="text-muted-foreground">Member code</dt>
              <dd>{meQ.data.memberCode}</dd>
              <dt className="text-muted-foreground">Status</dt>
              <dd>{enumLabel(MemberStatus, meQ.data.status)}</dd>
              <dt className="text-muted-foreground">Date of birth</dt>
              <dd>{meQ.data.dateOfBirth ? fmtDate(meQ.data.dateOfBirth) : "—"}</dd>
              <dt className="text-muted-foreground">Personal code</dt>
              <dd>{meQ.data.personalCode ?? "—"}</dd>
            </dl>
          )}
        </div>
      )}

      {!gym && <NoActiveGym />}
    </section>
  );
}
