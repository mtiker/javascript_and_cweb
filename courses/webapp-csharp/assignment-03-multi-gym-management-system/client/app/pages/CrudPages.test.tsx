import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MembersPage } from "./MembersPage";
import { MembershipPackagesPage } from "./MembershipPackagesPage";
import { TrainingCategoriesPage } from "./TrainingCategoriesPage";
import { AppShell } from "../components/AppShell";
import { setCurrentLanguage } from "../lib/language";
import { jsonResponse, renderWithAuth } from "../test/testUtils";

describe("CRUD pages", () => {
  beforeEach(() => {
    setCurrentLanguage("en");
    vi.stubGlobal("fetch", vi.fn());
  });

  it("shows the loading state while members are fetched", async () => {
    vi.mocked(fetch).mockImplementationOnce(
      () =>
        new Promise<Response>((resolve) => {
          setTimeout(() => resolve(jsonResponse([])), 20);
        }),
    );

    renderWithAuth(<MembersPage />);

    expect(screen.getByText("Loading members...")).toBeInTheDocument();
    expect(await screen.findByText("No members exist in this gym yet. Create the first member from the form.")).toBeInTheDocument();
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

  it("surfaces validation errors when required member fields are empty", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    renderWithAuth(<MembersPage />);

    await screen.findByText("No members exist in this gym yet. Create the first member from the form.");
    await userEvent.click(screen.getByRole("button", { name: "Create member" }));

    expect(await screen.findByText("Fix the member form before saving")).toBeInTheDocument();
    expect(screen.getByText("First name is required.")).toBeInTheDocument();
    expect(screen.getByText("Last name is required.")).toBeInTheDocument();
    expect(screen.getByText("Member code is required.")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("deletes a member after confirmation", async () => {
    const fetchMock = vi.mocked(fetch);
    vi.spyOn(window, "confirm").mockReturnValue(true);
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "member-1",
            memberCode: "MEM-100",
            fullName: "Ada Trainer",
            status: 0,
          },
        ]),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(jsonResponse([]));

    renderWithAuth(<MembersPage />);

    await userEvent.click(await screen.findByRole("button", { name: "Delete" }));

    expect(await screen.findByText("Member deleted")).toBeInTheDocument();
    expect(await screen.findByText("Ada Trainer was removed from the active gym.")).toBeInTheDocument();
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

  it("sends the selected shell language on training category requests", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse({
          id: "cat-1",
          name: "Joud",
          description: "Tugevustreening",
        }),
      )
      .mockResolvedValueOnce(jsonResponse([]));

    renderWithAuth(
      <AppShell>
        <TrainingCategoriesPage />
      </AppShell>,
    );

    await screen.findByText("No training categories exist yet. Add the first one from the editor.");
    await userEvent.selectOptions(screen.getByLabelText("Language"), "et-EE");
    await userEvent.type(screen.getByLabelText("Nimi"), "Joud");
    await userEvent.click(screen.getByRole("button", { name: "Loo kategooria" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    const createRequest = fetchMock.mock.calls[1]?.[1] as RequestInit;
    expect(new Headers(createRequest.headers).get("Accept-Language")).toBe("et-EE");
  });

  it("shows the loading state while membership packages are fetched", async () => {
    vi.mocked(fetch).mockImplementationOnce(
      () =>
        new Promise<Response>((resolve) => {
          setTimeout(() => resolve(jsonResponse([])), 20);
        }),
    );

    renderWithAuth(<MembershipPackagesPage />);

    expect(screen.getByText("Loading membership packages...")).toBeInTheDocument();
    expect(await screen.findByText("No membership packages exist yet. Add the first offer from the editor.")).toBeInTheDocument();
  });

  it("creates a membership package and reloads the list", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(
        jsonResponse({
          id: "pkg-1",
          name: "Monthly Plus",
          packageType: 1,
          durationValue: 1,
          durationUnit: 1,
          basePrice: 99,
          currencyCode: "EUR",
          trainingDiscountPercent: null,
          isTrainingFree: false,
          description: "Premium access.",
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "pkg-1",
            name: "Monthly Plus",
            packageType: 1,
            durationValue: 1,
            durationUnit: 1,
            basePrice: 99,
            currencyCode: "EUR",
            trainingDiscountPercent: null,
            isTrainingFree: false,
            description: "Premium access.",
          },
        ]),
      );

    renderWithAuth(<MembershipPackagesPage />);

    await screen.findByText("No membership packages exist yet. Add the first offer from the editor.");
    await userEvent.type(screen.getByLabelText("Name"), "Monthly Plus");
    const basePriceInput = screen.getByLabelText("Base price");
    await userEvent.clear(basePriceInput);
    await userEvent.type(basePriceInput, "99");
    await userEvent.click(screen.getByRole("button", { name: "Create package" }));

    expect(await screen.findByText("Package created")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("updates a membership package", async () => {
    const fetchMock = vi.mocked(fetch);
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
            trainingDiscountPercent: null,
            isTrainingFree: false,
            description: "Unlimited access.",
          },
        ]),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          id: "pkg-1",
          name: "Monthly Unlimited Pro",
          packageType: 1,
          durationValue: 1,
          durationUnit: 1,
          basePrice: 89,
          currencyCode: "EUR",
          trainingDiscountPercent: null,
          isTrainingFree: false,
          description: "Unlimited access.",
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "pkg-1",
            name: "Monthly Unlimited Pro",
            packageType: 1,
            durationValue: 1,
            durationUnit: 1,
            basePrice: 89,
            currencyCode: "EUR",
            trainingDiscountPercent: null,
            isTrainingFree: false,
            description: "Unlimited access.",
          },
        ]),
      );

    renderWithAuth(<MembershipPackagesPage />);

    await userEvent.click(await screen.findByRole("button", { name: /Monthly Unlimited/i }));
    const nameInput = screen.getByLabelText("Name");
    await userEvent.clear(nameInput);
    await userEvent.type(nameInput, "Monthly Unlimited Pro");
    const basePriceInput = screen.getByLabelText("Base price");
    await userEvent.clear(basePriceInput);
    await userEvent.type(basePriceInput, "89");
    await userEvent.click(screen.getByRole("button", { name: "Save package" }));

    expect(await screen.findByText("Package updated")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("surfaces validation errors when membership package fields are invalid", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockResolvedValueOnce(jsonResponse([]));

    renderWithAuth(<MembershipPackagesPage />);

    await screen.findByText("No membership packages exist yet. Add the first offer from the editor.");
    const currencyInput = screen.getByLabelText("Currency code");
    await userEvent.clear(currencyInput);
    const basePriceInput = screen.getByLabelText("Base price");
    await userEvent.clear(basePriceInput);
    await userEvent.type(basePriceInput, "-1");
    await userEvent.click(screen.getByRole("button", { name: "Create package" }));

    expect(await screen.findByText("Fix the package form before saving")).toBeInTheDocument();
    expect(screen.getByText("Package name is required.")).toBeInTheDocument();
    expect(screen.getByText("Base price must be zero or greater.")).toBeInTheDocument();
    expect(screen.getByText("Currency code is required.")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
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
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
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
