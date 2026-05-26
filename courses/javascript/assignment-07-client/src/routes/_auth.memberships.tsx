import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/lib/api/auth-context";
import { MembershipStatus, PaymentStatus, getErrorMessages } from "@/lib/api/types";
import { NoActiveGym, enumLabel, fmtDate, fmtMoney } from "@/lib/ui-helpers";
import { PageBanner } from "@/components/page-banner";
import bannerImg from "@/assets/banner-memberships.jpg";

export const Route = createFileRoute("/_auth/memberships")({
  component: MembershipsPage,
});

function MembershipsPage() {
  const auth = useAuth();
  const gym = auth.activeGym;

  const wsQ = useQuery({
    enabled: !!gym,
    queryKey: ["member-workspace", gym],
    queryFn: () => auth.api.getMemberWorkspace(gym!),
  });

  if (!gym) return <NoActiveGym />;
  if (wsQ.isLoading) return <p className="text-sm text-muted-foreground">Loading…</p>;
  if (wsQ.isError)
    return <p className="text-sm text-destructive">{getErrorMessages(wsQ.error).join(" ")}</p>;

  const ws = wsQ.data!;

  return (
    <section className="space-y-8">
      <PageBanner
        image={bannerImg}
        eyebrow="Account"
        title="Memberships & payments"
        subtitle={
          <>
            {ws.profile.fullName} — outstanding balance{" "}
            <strong className="text-foreground">
              {fmtMoney(ws.outstandingBalance, ws.memberships[0]?.currencyCode ?? "EUR")}
            </strong>
          </>
        }
        imagePosition="left center"
      />

      <div>
        <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          Memberships
        </h2>
        {ws.memberships.length === 0 ? (
          <p className="text-sm text-muted-foreground">No memberships.</p>
        ) : (
          <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-3 py-2">Start</th>
                  <th className="px-3 py-2">End</th>
                  <th className="px-3 py-2">Price</th>
                  <th className="px-3 py-2">Status</th>
                </tr>
              </thead>
              <tbody>
                {ws.memberships.map((m) => (
                  <tr key={m.id} className="border-t border-border">
                    <td className="px-3 py-2">{fmtDate(m.startDate)}</td>
                    <td className="px-3 py-2">{fmtDate(m.endDate)}</td>
                    <td className="px-3 py-2">{fmtMoney(m.priceAtPurchase, m.currencyCode)}</td>
                    <td className="px-3 py-2">{enumLabel(MembershipStatus, m.status)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div>
        <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          Payments
        </h2>
        {ws.payments.length === 0 ? (
          <p className="text-sm text-muted-foreground">No payments.</p>
        ) : (
          <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-3 py-2">Paid at</th>
                  <th className="px-3 py-2">Amount</th>
                  <th className="px-3 py-2">Status</th>
                  <th className="px-3 py-2">Reference</th>
                </tr>
              </thead>
              <tbody>
                {ws.payments.map((p) => (
                  <tr key={p.id} className="border-t border-border">
                    <td className="px-3 py-2">{fmtDate(p.paidAtUtc)}</td>
                    <td className="px-3 py-2">{fmtMoney(p.amount, p.currencyCode)}</td>
                    <td className="px-3 py-2">{enumLabel(PaymentStatus, p.status)}</td>
                    <td className="px-3 py-2 text-muted-foreground">{p.reference ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {ws.outstandingActions.length > 0 && (
        <div>
          <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Outstanding actions
          </h2>
          <ul className="space-y-2">
            {ws.outstandingActions.map((a) => (
              <li key={a.code} className="rounded-md border border-border bg-muted/30 p-3 text-sm">
                <p className="font-medium">{a.title}</p>
                <p className="text-muted-foreground">{a.detail}</p>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
