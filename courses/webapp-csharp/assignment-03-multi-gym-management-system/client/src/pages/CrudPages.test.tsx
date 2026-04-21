import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MembersPage } from "./MembersPage";
import { MembershipPackagesPage } from "./MembershipPackagesPage";
import { TrainingCategoriesPage } from "./TrainingCategoriesPage";
import { jsonResponse, renderWithAuth } from "../test/testUtils";

describe("CRUD pages", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("creates a member and reloads the list", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse({
          id: "member-1",
          memberCode: "MEM-100",
          firstName: "Ada",
          lastName: "Trainer",
          fullName: "Ada Trainer",
          personalCode: "50001010001",
          dateOfBirth: "2000-01-01",
          status: 0,
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "member-1",
            memberCode: "MEM-100",
            fullName: "Ada Trainer",
            status: 0,
          },
        ]),
      );

    renderWithAuth(<MembersPage />);

    await screen.findByText("No members exist in this gym yet. Create the first member from the form.");
    await userEvent.type(screen.getByLabelText("First name"), "Ada");
    await userEvent.type(screen.getByLabelText("Last name"), "Trainer");
    await userEvent.type(screen.getByLabelText("Member code"), "MEM-100");
    await userEvent.click(screen.getByRole("button", { name: "Create member" }));

    expect(await screen.findByText("Member created")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("shows a load error for members when the API fails", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        {
          title: "Forbidden",
          detail: "The requested gym does not match the active gym context.",
        },
        { status: 403 },
      ),
    );

    renderWithAuth(<MembersPage />);

    expect(await screen.findByText("The requested gym does not match the active gym context.")).toBeInTheDocument();
  });

  it("updates a training category", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "cat-1",
            name: "Strength Lab",
            description: "Coach-led barbell work.",
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "cat-1",
          name: "Strength Lab Pro",
          description: "Coach-led barbell work.",
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "cat-1",
            name: "Strength Lab Pro",
            description: "Coach-led barbell work.",
          },
        ]),
      );

    renderWithAuth(<TrainingCategoriesPage />);

    await userEvent.click(await screen.findByRole("button", { name: /Strength Lab/i }));
    const nameInput = screen.getByLabelText("Name");
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "Strength Lab Pro");
    await userEvent.click(screen.getByRole("button", { name: "Save category" }));

    expect(await screen.findByText("Category updated")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("shows an API save error for training categories", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse(
          {
            title: "Validation Failed",
            detail: "Category name already exists.",
          },
          { status: 400 },
        ),
      );

    renderWithAuth(<TrainingCategoriesPage />);

    await userEvent.type(await screen.findByLabelText("Name"), "Strength Lab");
    await userEvent.click(screen.getByRole("button", { name: "Create category" }));

    expect(await screen.findByText("Could not save category")).toBeInTheDocument();
    expect(await screen.findByText("Category name already exists.")).toBeInTheDocument();
  });

  it("deletes a membership package", async () => {
    const fetchMock = vi.mocked(fetch);
    vi.spyOn(window, "confirm").mockReturnValue(true);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "pkg-1",
            name: "Monthly Unlimited",
            packageType: 1,
            durationValue: 1,
            durationUnit: 1,
            basePrice: 79,
            currencyCode: "EUR",
            trainingDiscountPercent: 100,
            isTrainingFree: true,
            description: "Unlimited access.",
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          messages: ["Membership package deleted."],
        }),
      )
      .mockResolvedValueOnce(jsonResponse([]));

    renderWithAuth(<MembershipPackagesPage />);

    await userEvent.click(await screen.findByRole("button", { name: "Delete" }));

    expect(await screen.findByText("Package deleted")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("shows an API save error for membership packages", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse(
          {
            title: "Validation Failed",
            detail: "Package pricing is invalid.",
          },
          { status: 400 },
        ),
      );

    renderWithAuth(<MembershipPackagesPage />);

    await userEvent.type(await screen.findByLabelText("Name"), "Monthly Plus");
    const basePriceInput = screen.getByLabelText("Base price");
    await userEvent.clear(basePriceInput);
    await userEvent.type(basePriceInput, "99");
    await userEvent.click(screen.getByRole("button", { name: "Create package" }));

    expect(await screen.findByText("Could not save package")).toBeInTheDocument();
    expect(await screen.findByText("Package pricing is invalid.")).toBeInTheDocument();
  });
});
