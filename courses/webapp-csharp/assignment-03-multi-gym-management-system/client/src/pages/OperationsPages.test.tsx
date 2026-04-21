import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AttendancePage } from "./AttendancePage";
import { MaintenanceTasksPage } from "./MaintenanceTasksPage";
import { jsonResponse, renderWithAuth } from "../test/testUtils";

describe("role operation pages", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("updates trainer attendance through the booking endpoint", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "booking-1",
            trainingSessionId: "session-1",
            memberId: "member-1",
            status: 0,
            chargedPrice: 0,
            paymentRequired: false,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "session-1",
            categoryId: "category-1",
            name: "Upper Body Fundamentals",
            description: "Coach-led session.",
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
        jsonResponse({
          id: "booking-1",
          trainingSessionId: "session-1",
          memberId: "member-1",
          status: 2,
          chargedPrice: 0,
          paymentRequired: false,
        }),
      );

    renderWithAuth(<AttendancePage />, {
      session: {
        jwt: "trainer-jwt",
        refreshToken: "refresh-token",
        expiresInSeconds: 3600,
        activeGymCode: "peak-forge",
        activeRole: "Trainer",
        systemRoles: [],
      },
    });

    expect(await screen.findByText("Upper Body Fundamentals")).toBeInTheDocument();
    await userEvent.selectOptions(screen.getByLabelText("Attendance"), "2");
    await userEvent.click(screen.getByRole("button", { name: "Update" }));

    expect(await screen.findByText("Attendance updated")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    expect(fetchMock.mock.calls[2]?.[0]).toContain("/api/v1/peak-forge/bookings/booking-1/attendance");
  });

  it("updates caretaker maintenance task status", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "task-1",
            equipmentId: "equipment-1",
            assignedStaffId: "staff-1",
            createdByStaffId: "staff-2",
            taskType: 0,
            priority: 1,
            status: 0,
            dueAtUtc: "2026-04-30T12:00:00Z",
            startedAtUtc: null,
            completedAtUtc: null,
            notes: "Quarterly inspection",
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "task-1",
          equipmentId: "equipment-1",
          assignedStaffId: "staff-1",
          createdByStaffId: "staff-2",
          taskType: 0,
          priority: 1,
          status: 1,
          dueAtUtc: "2026-04-30T12:00:00Z",
          startedAtUtc: "2026-04-21T09:00:00Z",
          completedAtUtc: null,
          notes: "Started chain inspection",
        }),
      );

    renderWithAuth(<MaintenanceTasksPage />, {
      session: {
        jwt: "caretaker-jwt",
        refreshToken: "refresh-token",
        expiresInSeconds: 3600,
        activeGymCode: "peak-forge",
        activeRole: "Caretaker",
        systemRoles: [],
      },
    });

    expect(await screen.findByText("Scheduled maintenance")).toBeInTheDocument();
    await userEvent.selectOptions(screen.getByLabelText("Status"), "1");
    await userEvent.clear(screen.getByLabelText("Notes"));
    await userEvent.type(screen.getByLabelText("Notes"), "Started chain inspection");
    await userEvent.click(screen.getByRole("button", { name: "Update" }));

    expect(await screen.findByText("Maintenance task updated")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    expect(fetchMock.mock.calls[1]?.[0]).toContain("/api/v1/peak-forge/maintenance-tasks/task-1/status");
  });
});
