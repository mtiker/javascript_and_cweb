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
            trainingSessionName: "Upper Body Fundamentals",
            memberId: "member-1",
            memberName: "Liis Lill",
            memberCode: "MEM-001",
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
          trainingSessionName: "Upper Body Fundamentals",
          memberId: "member-1",
          memberName: "Liis Lill",
          memberCode: "MEM-001",
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
    expect(await screen.findByText("Liis Lill is now Attended.")).toBeInTheDocument();
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
            equipmentAssetTag: "EQ-ROW-001",
            equipmentName: "Concept2 rower",
            assignedStaffId: "staff-1",
            assignedStaffName: "Tanel Tamme",
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
        jsonResponse([
          {
            id: "equipment-1",
            equipmentModelId: "model-1",
            assetTag: "EQ-ROW-001",
            serialNumber: "ROW-001",
            currentStatus: 0,
            commissionedAt: "2026-01-10",
            decommissionedAt: null,
            notes: null,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "task-1",
          equipmentId: "equipment-1",
          equipmentAssetTag: "EQ-ROW-001",
          equipmentName: "Concept2 rower",
          assignedStaffId: "staff-1",
          assignedStaffName: "Tanel Tamme",
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
    const taskNotes = screen.getAllByLabelText("Notes")[1];
    await userEvent.clear(taskNotes);
    await userEvent.type(taskNotes, "Started chain inspection");
    await userEvent.click(screen.getByRole("button", { name: "Update" }));

    expect(await screen.findByText("Maintenance task updated")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    expect(fetchMock.mock.calls[2]?.[0]).toContain("/api/v1/peak-forge/maintenance-tasks/task-1/status");
  });

  it("schedules a maintenance task with equipment and staff assignment", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "equipment-1",
            equipmentModelId: "model-1",
            assetTag: "EQ-TREAD-001",
            serialNumber: "TR-001",
            currentStatus: 0,
            commissionedAt: "2026-01-10",
            decommissionedAt: null,
            notes: null,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "staff-1",
            staffCode: "STF-CARE-001",
            fullName: "Tanel Tamme",
            status: 0,
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "task-2",
          equipmentId: "equipment-1",
          equipmentAssetTag: "EQ-TREAD-001",
          equipmentName: "Treadmill",
          assignedStaffId: "staff-1",
          assignedStaffName: "Tanel Tamme",
          createdByStaffId: null,
          taskType: 0,
          priority: 2,
          status: 0,
          dueAtUtc: "2026-04-25T12:00:00Z",
          startedAtUtc: null,
          completedAtUtc: null,
          notes: "Inspect belt",
        }),
      );

    renderWithAuth(<MaintenanceTasksPage />);

    await screen.findByText("New maintenance task");
    await userEvent.selectOptions(screen.getByLabelText("Priority"), "2");
    await userEvent.type(screen.getByLabelText("Due"), "2026-04-25T12:00");
    await userEvent.type(screen.getByLabelText("Notes"), "Inspect belt");
    await userEvent.click(screen.getByRole("button", { name: "Schedule maintenance" }));

    expect(await screen.findByText("Maintenance scheduled")).toBeInTheDocument();
    expect(await screen.findByText("EQ-TREAD-001 / Treadmill")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(4));
    expect(fetchMock.mock.calls[3]?.[0]).toContain("/api/v1/peak-forge/maintenance-tasks");
    expect(fetchMock.mock.calls[3]?.[1]).toMatchObject({ method: "POST" });
  });
});
