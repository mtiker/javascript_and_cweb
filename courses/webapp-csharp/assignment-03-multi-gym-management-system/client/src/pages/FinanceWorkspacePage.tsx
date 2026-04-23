import { type FormEvent, useEffect, useMemo, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { FinanceWorkspace, Invoice, MemberSummary, Notice } from "../lib/types";
import { InvoiceStatus, getErrorMessages } from "../lib/types";

interface AdjustmentFormState {
  amount: string;
  reference: string;
  notes: string;
}

export function FinanceWorkspacePage() {
  const { api, session } = useAuth();
  const [workspace, setWorkspace] = useState<FinanceWorkspace | null>(null);
  const [members, setMembers] = useState<MemberSummary[]>([]);
  const [selectedMemberId, setSelectedMemberId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isCreatingInvoice, setIsCreatingInvoice] = useState(false);
  const [activeAdjustmentInvoiceId, setActiveAdjustmentInvoiceId] = useState<string | null>(null);
  const [invoiceDueDate, setInvoiceDueDate] = useState("");
  const [invoiceLineDescription, setInvoiceLineDescription] = useState("Membership and training services");
  const [invoiceLineAmount, setInvoiceLineAmount] = useState("0");
  const [invoiceLineQuantity, setInvoiceLineQuantity] = useState("1");
  const [invoiceNotes, setInvoiceNotes] = useState("");
  const [paymentByInvoiceId, setPaymentByInvoiceId] = useState<Record<string, AdjustmentFormState>>({});
  const [refundByInvoiceId, setRefundByInvoiceId] = useState<Record<string, AdjustmentFormState>>({});
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);

  const canManageFinance = session?.activeRole === "GymAdmin" || session?.activeRole === "GymOwner";

  useEffect(() => {
    void initializeWorkspace();
  }, []);

  useEffect(() => {
    if (!canManageFinance || !selectedMemberId) {
      return;
    }

    void loadWorkspaceForMember(selectedMemberId);
  }, [selectedMemberId]);

  const invoices = useMemo(() => workspace?.invoices ?? [], [workspace]);

  async function initializeWorkspace() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      if (canManageFinance) {
        const loadedMembers = await api.getMembers(session.activeGymCode);
        setMembers(loadedMembers);
        const firstMemberId = loadedMembers[0]?.id ?? "";
        setSelectedMemberId(firstMemberId);

        if (firstMemberId) {
          await loadWorkspaceForMember(firstMemberId);
        } else {
          setWorkspace(null);
        }
      } else {
        setWorkspace(await api.getFinanceWorkspace(session.activeGymCode));
      }
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load finance workspace.");
    } finally {
      setIsLoading(false);
    }
  }

  async function loadWorkspaceForMember(memberId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      setWorkspace(await api.getFinanceWorkspaceForMember(session.activeGymCode, memberId));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load finance workspace.");
    } finally {
      setIsLoading(false);
    }
  }

  async function createInvoice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session?.activeGymCode) {
      return;
    }

    const memberId = canManageFinance ? selectedMemberId : workspace?.memberId;
    if (!memberId || !invoiceLineDescription.trim() || !invoiceDueDate || Number(invoiceLineQuantity) <= 0 || Number(invoiceLineAmount) < 0) {
      setNotice({
        tone: "error",
        title: "Could not create invoice",
        messages: ["Member, due date, description, quantity, and a non-negative amount are required."],
      });
      return;
    }

    setIsCreatingInvoice(true);
    setNotice(null);

    try {
      await api.createInvoice(session.activeGymCode, {
        memberId,
        dueAtUtc: new Date(invoiceDueDate).toISOString(),
        currencyCode: "EUR",
        notes: invoiceNotes.trim() || null,
        lines: [
          {
            description: invoiceLineDescription.trim(),
            quantity: Number(invoiceLineQuantity),
            unitPrice: Number(invoiceLineAmount),
            isCredit: false,
            notes: null,
          },
        ],
      });

      setInvoiceDueDate("");
      setInvoiceNotes("");
      setInvoiceLineDescription("Membership and training services");
      setInvoiceLineAmount("0");
      setInvoiceLineQuantity("1");
      setNotice({
        tone: "success",
        title: "Invoice created",
      });

      if (canManageFinance && selectedMemberId) {
        await loadWorkspaceForMember(selectedMemberId);
      } else {
        setWorkspace(await api.getFinanceWorkspace(session.activeGymCode));
      }
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not create invoice",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsCreatingInvoice(false);
    }
  }

  async function submitAdjustment(invoiceId: string, isRefund: boolean) {
    if (!session?.activeGymCode) {
      return;
    }

    const source = isRefund ? refundByInvoiceId[invoiceId] : paymentByInvoiceId[invoiceId];
    const amount = Number(source?.amount ?? "0");
    if (!source || amount <= 0) {
      setNotice({
        tone: "error",
        title: isRefund ? "Could not post refund" : "Could not post payment",
        messages: ["Amount must be greater than zero."],
      });
      return;
    }

    setActiveAdjustmentInvoiceId(invoiceId);
    setNotice(null);

    try {
      if (isRefund) {
        await api.addInvoiceRefund(session.activeGymCode, invoiceId, {
          amount,
          reference: source.reference.trim() || null,
          notes: source.notes.trim() || null,
        });
      } else {
        await api.addInvoicePayment(session.activeGymCode, invoiceId, {
          amount,
          reference: source.reference.trim() || null,
          notes: source.notes.trim() || null,
        });
      }

      setNotice({
        tone: "success",
        title: isRefund ? "Refund posted" : "Payment posted",
      });

      if (canManageFinance && selectedMemberId) {
        await loadWorkspaceForMember(selectedMemberId);
      } else {
        setWorkspace(await api.getFinanceWorkspace(session.activeGymCode));
      }
    } catch (error) {
      setNotice({
        tone: "error",
        title: isRefund ? "Could not post refund" : "Could not post payment",
        messages: getErrorMessages(error),
      });
    } finally {
      setActiveAdjustmentInvoiceId(null);
    }
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">Finance workflow</p>
          <h2 className="workspace__title">Finance workspace</h2>
          <p className="workspace__copy">Track invoices, outstanding balances, and invoice-level payment or refund history.</p>
        </div>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        <section className="panel">
          <div className="editor-header">
            <div>
              <p className="workspace__eyebrow">Summary</p>
              <h3>{workspace?.memberName ?? "Finance snapshot"}</h3>
            </div>
          </div>

          {canManageFinance ? (
            <label className="field">
              <span>Member</span>
              <select onChange={(event) => setSelectedMemberId(event.target.value)} value={selectedMemberId}>
                {members.map((member) => (
                  <option key={member.id} value={member.id}>
                    {member.fullName} / {member.memberCode}
                  </option>
                ))}
              </select>
            </label>
          ) : null}

          {workspace ? (
            <div className="metric-grid">
              <article className="metric">
                <span>Outstanding balance</span>
                <strong>{workspace.outstandingBalance.toFixed(2)} EUR</strong>
              </article>
              <article className="metric">
                <span>Overdue invoices</span>
                <strong>{workspace.overdueInvoiceCount}</strong>
              </article>
              <article className="metric">
                <span>Refund credits</span>
                <strong>{workspace.totalRefundCredits.toFixed(2)} EUR</strong>
              </article>
            </div>
          ) : null}

          <form className="form" onSubmit={(event) => void createInvoice(event)}>
            <div className="editor-header">
              <div>
                <p className="workspace__eyebrow">Create</p>
                <h3>New invoice</h3>
              </div>
            </div>

            <label className="field">
              <span>Line description</span>
              <input disabled={isCreatingInvoice} onChange={(event) => setInvoiceLineDescription(event.target.value)} value={invoiceLineDescription} />
            </label>

            <div className="form__row">
              <label className="field">
                <span>Quantity</span>
                <input
                  disabled={isCreatingInvoice}
                  min={0.01}
                  onChange={(event) => setInvoiceLineQuantity(event.target.value)}
                  step="0.01"
                  type="number"
                  value={invoiceLineQuantity}
                />
              </label>
              <label className="field">
                <span>Unit price</span>
                <input
                  disabled={isCreatingInvoice}
                  min={0}
                  onChange={(event) => setInvoiceLineAmount(event.target.value)}
                  step="0.01"
                  type="number"
                  value={invoiceLineAmount}
                />
              </label>
            </div>

            <label className="field">
              <span>Due date</span>
              <input disabled={isCreatingInvoice} onChange={(event) => setInvoiceDueDate(event.target.value)} type="datetime-local" value={invoiceDueDate} />
            </label>

            <label className="field">
              <span>Notes</span>
              <textarea disabled={isCreatingInvoice} onChange={(event) => setInvoiceNotes(event.target.value)} rows={3} value={invoiceNotes} />
            </label>

            <div className="form__actions">
              <button className="button" disabled={isCreatingInvoice || !workspace} type="submit">
                {isCreatingInvoice ? "Creating..." : "Create invoice"}
              </button>
            </div>
          </form>
        </section>

        <section className="panel panel--list">
          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">Loading finance workspace...</p> : null}
          {!isLoading && invoices.length === 0 ? <p className="state">No invoices found for the selected member.</p> : null}

          <div className="record-list" role="list">
            {invoices.map((invoice) => (
              <article className="record-card record-card--wide" key={invoice.id} role="listitem">
                <div className="record-card__body">
                  <strong>{invoice.invoiceNumber}</strong>
                  <span>
                    {invoice.memberName} / due {formatDate(invoice.dueAtUtc)}
                  </span>
                  <span>
                    Outstanding {invoice.outstandingAmount.toFixed(2)} {invoice.currencyCode}
                  </span>
                  <span>
                    {invoiceStatusLabel(invoice.status)}
                    {invoice.isOverdue ? " / Overdue" : ""}
                  </span>
                </div>
                <div className="inline-controls inline-controls--wide">
                  <label className="field field--compact">
                    <span>Payment amount</span>
                    <input
                      disabled={activeAdjustmentInvoiceId === invoice.id}
                      min={0}
                      onChange={(event) => updatePaymentForm(invoice.id, { amount: event.target.value })}
                      step="0.01"
                      type="number"
                      value={paymentByInvoiceId[invoice.id]?.amount ?? ""}
                    />
                  </label>
                  <label className="field field--compact">
                    <span>Reference</span>
                    <input
                      disabled={activeAdjustmentInvoiceId === invoice.id}
                      onChange={(event) => updatePaymentForm(invoice.id, { reference: event.target.value })}
                      value={paymentByInvoiceId[invoice.id]?.reference ?? ""}
                    />
                  </label>
                  <button
                    className="button"
                    disabled={activeAdjustmentInvoiceId === invoice.id}
                    onClick={() => void submitAdjustment(invoice.id, false)}
                    type="button"
                  >
                    {activeAdjustmentInvoiceId === invoice.id ? "Saving..." : "Add payment"}
                  </button>
                </div>
                {canManageFinance ? (
                  <div className="inline-controls inline-controls--wide">
                    <label className="field field--compact">
                      <span>Refund amount</span>
                      <input
                        disabled={activeAdjustmentInvoiceId === invoice.id}
                        min={0}
                        onChange={(event) => updateRefundForm(invoice.id, { amount: event.target.value })}
                        step="0.01"
                        type="number"
                        value={refundByInvoiceId[invoice.id]?.amount ?? ""}
                      />
                    </label>
                    <label className="field field--compact">
                      <span>Refund reference</span>
                      <input
                        disabled={activeAdjustmentInvoiceId === invoice.id}
                        onChange={(event) => updateRefundForm(invoice.id, { reference: event.target.value })}
                        value={refundByInvoiceId[invoice.id]?.reference ?? ""}
                      />
                    </label>
                    <button
                      className="button button--secondary"
                      disabled={activeAdjustmentInvoiceId === invoice.id}
                      onClick={() => void submitAdjustment(invoice.id, true)}
                      type="button"
                    >
                      {activeAdjustmentInvoiceId === invoice.id ? "Saving..." : "Add refund"}
                    </button>
                  </div>
                ) : null}
              </article>
            ))}
          </div>
        </section>
      </div>
    </section>
  );

  function updatePaymentForm(invoiceId: string, update: Partial<AdjustmentFormState>) {
    setPaymentByInvoiceId((current) => ({
      ...current,
      [invoiceId]: {
        amount: current[invoiceId]?.amount ?? "",
        reference: current[invoiceId]?.reference ?? "",
        notes: current[invoiceId]?.notes ?? "",
        ...update,
      },
    }));
  }

  function updateRefundForm(invoiceId: string, update: Partial<AdjustmentFormState>) {
    setRefundByInvoiceId((current) => ({
      ...current,
      [invoiceId]: {
        amount: current[invoiceId]?.amount ?? "",
        reference: current[invoiceId]?.reference ?? "",
        notes: current[invoiceId]?.notes ?? "",
        ...update,
      },
    }));
  }
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}

function invoiceStatusLabel(status: InvoiceStatus) {
  switch (status) {
    case InvoiceStatus.Issued:
      return "Issued";
    case InvoiceStatus.PartiallyPaid:
      return "Partially paid";
    case InvoiceStatus.Paid:
      return "Paid";
    case InvoiceStatus.Overdue:
      return "Overdue";
    case InvoiceStatus.Cancelled:
      return "Cancelled";
    case InvoiceStatus.Refunded:
      return "Refunded";
    default:
      return "Draft";
  }
}

function _invoiceTotalLabel(invoice: Invoice) {
  return `${invoice.totalAmount.toFixed(2)} ${invoice.currencyCode}`;
}
