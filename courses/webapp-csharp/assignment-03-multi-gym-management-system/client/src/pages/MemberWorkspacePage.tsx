import { useEffect, useMemo, useState } from "react";
import { useAuth } from "../lib/auth";
import type { MemberWorkspace } from "../lib/types";
import { getErrorMessages, MembershipStatus } from "../lib/types";

export function MemberWorkspacePage() {
  const { api, session } = useAuth();
  const [workspace, setWorkspace] = useState<MemberWorkspace | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);

  useEffect(() => {
    void loadWorkspace();
  }, []);

  const activeMembership = useMemo(
    () => workspace?.memberships.find((membership) => membership.status === MembershipStatus.Active || membership.status === MembershipStatus.Renewed),
    [workspace],
  );

  async function loadWorkspace() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      setWorkspace(await api.getMemberWorkspace(session.activeGymCode));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load member workspace.");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">Member workspace</p>
          <h2 className="workspace__title">My profile and actions</h2>
          <p className="workspace__copy">Track memberships, bookings, payments, invoices, and outstanding follow-up tasks from one view.</p>
        </div>
      </header>

      {pageError ? <p className="state state--error">{pageError}</p> : null}
      {isLoading ? <p className="state">Loading member workspace...</p> : null}

      {!isLoading && workspace ? (
        <div className="workspace__grid">
          <section className="panel">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Profile</p>
                <h3>{workspace.profile.fullName}</h3>
              </div>
            </div>
            <dl className="definition-list">
              <div>
                <dt>Member code</dt>
                <dd>{workspace.profile.memberCode}</dd>
              </div>
              <div>
                <dt>Status</dt>
                <dd>{membershipStatusLabel(activeMembership?.status)}</dd>
              </div>
              <div>
                <dt>Attended sessions</dt>
                <dd>{workspace.attendedSessionCount}</dd>
              </div>
              <div>
                <dt>Upcoming bookings</dt>
                <dd>{workspace.upcomingBookingCount}</dd>
              </div>
              <div>
                <dt>Outstanding balance</dt>
                <dd>{workspace.outstandingBalance.toFixed(2)} EUR</dd>
              </div>
            </dl>
          </section>

          <section className="panel panel--list">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Outstanding actions</p>
                <h3>Needs attention</h3>
              </div>
            </div>

            {workspace.outstandingActions.length === 0 ? <p className="state">No outstanding actions.</p> : null}

            <div className="record-list" role="list">
              {workspace.outstandingActions.map((action) => (
                <article className="record-card" key={action.code} role="listitem">
                  <div className="record-card__body">
                    <strong>{action.title}</strong>
                    <span>{action.detail}</span>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel panel--list">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Invoices</p>
                <h3>Finance snapshot</h3>
              </div>
            </div>

            {workspace.invoices.length === 0 ? <p className="state">No invoices found.</p> : null}

            <div className="record-list" role="list">
              {workspace.invoices.map((invoice) => (
                <article className="record-card" key={invoice.id} role="listitem">
                  <div className="record-card__body">
                    <strong>{invoice.invoiceNumber}</strong>
                    <span>
                      Due {formatDate(invoice.dueAtUtc)} / Outstanding {invoice.outstandingAmount.toFixed(2)} {invoice.currencyCode}
                    </span>
                    <span>{invoice.isOverdue ? "Overdue" : invoice.status.toString()}</span>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel panel--list">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Memberships</p>
                <h3>Lifecycle and access</h3>
              </div>
            </div>

            {workspace.memberships.length === 0 ? <p className="state">No memberships found.</p> : null}

            <div className="record-list" role="list">
              {workspace.memberships.map((membership) => (
                <article className="record-card" key={membership.id} role="listitem">
                  <div className="record-card__body">
                    <strong>{membershipStatusLabel(membership.status)}</strong>
                    <span>
                      {formatDate(membership.startDate)} - {formatDate(membership.endDate)}
                    </span>
                    <span>
                      {membership.priceAtPurchase.toFixed(2)} {membership.currencyCode}
                    </span>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel panel--list">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Bookings</p>
                <h3>Upcoming and attendance</h3>
              </div>
            </div>

            {workspace.bookings.length === 0 ? <p className="state">No bookings found.</p> : null}

            <div className="record-list" role="list">
              {workspace.bookings.map((booking) => (
                <article className="record-card" key={booking.bookingId} role="listitem">
                  <div className="record-card__body">
                    <strong>{booking.trainingSessionName}</strong>
                    <span>
                      {formatDateTime(booking.startAtUtc)} - {formatTime(booking.endAtUtc)}
                    </span>
                    <span>{booking.paymentRequired ? "Payment required" : "Covered by membership"}</span>
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="panel panel--list">
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Payments</p>
                <h3>Recent history</h3>
              </div>
            </div>

            {workspace.payments.length === 0 ? <p className="state">No payments found.</p> : null}

            <div className="record-list" role="list">
              {workspace.payments.map((payment) => (
                <article className="record-card" key={payment.id} role="listitem">
                  <div className="record-card__body">
                    <strong>
                      {payment.amount.toFixed(2)} {payment.currencyCode}
                    </strong>
                    <span>{formatDateTime(payment.paidAtUtc)}</span>
                    <span>{payment.reference || "No reference"}</span>
                  </div>
                </article>
              ))}
            </div>
          </section>
        </div>
      ) : null}
    </section>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { timeStyle: "short" }).format(new Date(value));
}

function membershipStatusLabel(status?: MembershipStatus) {
  if (status === undefined) {
    return "No active membership";
  }

  return MembershipStatus[status] ?? "Unknown";
}
