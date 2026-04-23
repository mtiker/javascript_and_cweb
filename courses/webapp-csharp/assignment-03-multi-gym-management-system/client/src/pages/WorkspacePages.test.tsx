import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FinanceWorkspacePage } from "./FinanceWorkspacePage";
import { TrainerCoachingWorkspacePage } from "./TrainerCoachingWorkspacePage";
import { jsonResponse, renderWithAuth } from "../test/testUtils";

describe("batch 4 workspace pages", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("posts an invoice payment from finance workspace", async () => {
    const fetchMock = vi.mocked(fetch);
    const workspacePayload = {
      memberId: "member-1",
      memberName: "Liis Lill",
      memberCode: "MEM-001",
      outstandingBalance: 45,
      totalRefundCredits: 0,
      overdueInvoiceCount: 0,
      invoices: [
        {
          id: "invoice-1",
          memberId: "member-1",
          memberName: "Liis Lill",
          invoiceNumber: "INV-20260423-0001",
          issuedAtUtc: "2026-04-23T08:00:00Z",
          dueAtUtc: "2026-05-01T08:00:00Z",
          currencyCode: "EUR",
          subtotalAmount: 45,
          creditAmount: 0,
          totalAmount: 45,
          paidAmount: 0,
          outstandingAmount: 45,
          isOverdue: false,
          status: 1,
          notes: null,
          lines: [],
          payments: [],
        },
      ],
      paymentHistory: [],
    };

    fetchMock
      .mockResolvedValueOnce(jsonResponse(workspacePayload))
      .mockResolvedValueOnce(
        jsonResponse({
          ...workspacePayload.invoices[0],
          paidAmount: 10,
          outstandingAmount: 35,
          status: 2,
          payments: [
            {
              id: "invoice-payment-1",
              amount: 10,
              isRefund: false,
              appliedAtUtc: "2026-04-23T08:30:00Z",
              reference: "PAY-1",
              notes: null,
            },
          ],
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          ...workspacePayload,
          outstandingBalance: 35,
          invoices: [
            {
              ...workspacePayload.invoices[0],
              paidAmount: 10,
              outstandingAmount: 35,
              status: 2,
            },
          ],
        }),
      );

    renderWithAuth(<FinanceWorkspacePage />, {
      session: {
        jwt: "member-jwt",
        refreshToken: "refresh-token",
        expiresInSeconds: 3600,
        activeGymCode: "peak-forge",
        activeRole: "Member",
        systemRoles: [],
      },
    });

    expect(await screen.findByText("INV-20260423-0001")).toBeInTheDocument();
    await userEvent.type(screen.getByLabelText("Payment amount"), "10");
    await userEvent.type(screen.getByLabelText("Reference"), "PAY-1");
    await userEvent.click(screen.getByRole("button", { name: "Add payment" }));

    expect(await screen.findByText("Payment posted")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    expect(fetchMock.mock.calls[1]?.[0]).toContain("/api/v1/peak-forge/invoices/invoice-1/payments");
  });

  it("saves coaching plan item decisions", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "member-1",
            memberCode: "MEM-001",
            fullName: "Liis Lill",
            status: 0,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "staff-1",
            staffCode: "STF-1",
            fullName: "Tanel Trainer",
            status: 0,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "plan-1",
            memberId: "member-1",
            memberName: "Liis Lill",
            trainerStaffId: "staff-1",
            trainerStaffName: "Tanel Trainer",
            createdByStaffId: "staff-1",
            title: "Strength progression",
            notes: null,
            status: 1,
            publishedAtUtc: "2026-04-22T08:00:00Z",
            activatedAtUtc: null,
            completedAtUtc: null,
            cancelledAtUtc: null,
            items: [
              {
                id: "item-1",
                sequence: 1,
                title: "Deadlift progression",
                notes: null,
                targetDate: "2026-05-03",
                decision: null,
                decisionAtUtc: null,
                decisionByStaffName: null,
                decisionNotes: null,
              },
            ],
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "plan-1",
          memberId: "member-1",
          memberName: "Liis Lill",
          trainerStaffId: "staff-1",
          trainerStaffName: "Tanel Trainer",
          createdByStaffId: "staff-1",
          title: "Strength progression",
          notes: null,
          status: 2,
          publishedAtUtc: "2026-04-22T08:00:00Z",
          activatedAtUtc: "2026-04-23T08:00:00Z",
          completedAtUtc: null,
          cancelledAtUtc: null,
          items: [
            {
              id: "item-1",
              sequence: 1,
              title: "Deadlift progression",
              notes: null,
              targetDate: "2026-05-03",
              decision: 0,
              decisionAtUtc: "2026-04-23T08:00:00Z",
              decisionByStaffName: "Tanel Trainer",
              decisionNotes: null,
            },
          ],
        }),
      );

    renderWithAuth(<TrainerCoachingWorkspacePage />, {
      session: {
        jwt: "trainer-jwt",
        refreshToken: "refresh-token",
        expiresInSeconds: 3600,
        activeGymCode: "peak-forge",
        activeRole: "Trainer",
        systemRoles: [],
      },
    });

    expect(await screen.findByText(/Deadlift progression/i)).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Save decision" }));

    expect(await screen.findByText("Coaching plan item decision saved")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(4));
    expect(fetchMock.mock.calls[3]?.[0]).toContain("/api/v1/peak-forge/coaching-plans/plan-1/items/item-1/decision");
  });
});
