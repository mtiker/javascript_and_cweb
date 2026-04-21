import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SessionsPage } from "./SessionsPage";
import { jsonResponse, renderWithAuth } from "../test/testUtils";

describe("SessionsPage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("shows session details and books an admin-selected member", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "session-1",
            categoryId: "category-1",
            name: "Strength Lab PM",
            description: null,
            startAtUtc: "2026-04-22T17:00:00Z",
            endAtUtc: "2026-04-22T18:00:00Z",
            capacity: 12,
            basePrice: 18,
            currencyCode: "EUR",
            status: 1,
            trainerContractIds: ["contract-1"],
          },
        ]),
      )
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
        jsonResponse({
          id: "session-1",
          categoryId: "category-1",
          name: "Strength Lab PM",
          description: "Coach-led strength session.",
          startAtUtc: "2026-04-22T17:00:00Z",
          endAtUtc: "2026-04-22T18:00:00Z",
          capacity: 12,
          basePrice: 18,
          currencyCode: "EUR",
          status: 1,
          trainerContractIds: ["contract-1"],
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "booking-1",
          trainingSessionId: "session-1",
          memberId: "member-1",
          status: 0,
          chargedPrice: 18,
          paymentRequired: true,
        }),
      );

    renderWithAuth(<SessionsPage />);

    expect(await screen.findByRole("button", { name: /Strength Lab PM/i })).toBeInTheDocument();
    expect(await screen.findByText("Coach-led strength session.")).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Payment reference"), "PAY-123");
    await userEvent.click(screen.getByRole("button", { name: "Book session" }));

    expect(await screen.findByText("Booking created")).toBeInTheDocument();
    expect(await screen.findByText("Payment reference accepted for 18.00 EUR.")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(4));
    expect(fetchMock.mock.calls[3]?.[0]).toContain("/api/v1/peak-forge/bookings");
  });
});
