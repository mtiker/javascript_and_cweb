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
        jsonResponse([
          {
            id: "category-1",
            name: "Strength",
            description: null,
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
          trainingSessionName: "Strength Lab PM",
          memberId: "member-1",
          memberName: "Liis Lill",
          memberCode: "MEM-001",
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
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(5));
    expect(fetchMock.mock.calls[4]?.[0]).toContain("/api/v1/peak-forge/bookings");
  });

  it("schedules a new session from an existing category", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "category-1",
            name: "Mobility",
            description: null,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "session-2",
          categoryId: "category-1",
          name: "Morning Mobility",
          description: "Gentle mobility flow.",
          startAtUtc: "2026-04-23T07:00:00Z",
          endAtUtc: "2026-04-23T08:00:00Z",
          capacity: 16,
          basePrice: 12,
          currencyCode: "EUR",
          status: 1,
          trainerContractIds: [],
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "session-2",
          categoryId: "category-1",
          name: "Morning Mobility",
          description: "Gentle mobility flow.",
          startAtUtc: "2026-04-23T07:00:00Z",
          endAtUtc: "2026-04-23T08:00:00Z",
          capacity: 16,
          basePrice: 12,
          currencyCode: "EUR",
          status: 1,
          trainerContractIds: [],
        }),
      );

    renderWithAuth(<SessionsPage />);

    await screen.findByText("New training session");
    await userEvent.type(screen.getByLabelText("Name"), "Morning Mobility");
    await userEvent.type(screen.getByLabelText("Starts"), "2026-04-23T07:00");
    await userEvent.type(screen.getByLabelText("Ends"), "2026-04-23T08:00");
    await userEvent.clear(screen.getByLabelText("Base price"));
    await userEvent.type(screen.getByLabelText("Base price"), "12");
    await userEvent.type(screen.getByLabelText("Description"), "Gentle mobility flow.");
    await userEvent.click(screen.getByRole("button", { name: "Schedule session" }));

    expect(await screen.findByText("Session scheduled")).toBeInTheDocument();
    expect((await screen.findAllByText("Morning Mobility")).length).toBeGreaterThan(0);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(5));
    expect(fetchMock.mock.calls[3]?.[0]).toContain("/api/v1/peak-forge/training-sessions");
    expect(fetchMock.mock.calls[3]?.[1]).toMatchObject({ method: "POST" });
  });
});
