import { useDeferredValue, useEffect, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import { useLanguage } from "../lib/language";
import type {
  DurationUnit,
  MembershipPackage,
  MembershipPackageType,
  Notice,
} from "../lib/types";
import { getErrorMessages } from "../lib/types";

interface MembershipPackageFormState {
  name: string;
  packageType: MembershipPackageType;
  durationValue: string;
  durationUnit: DurationUnit;
  basePrice: string;
  currencyCode: string;
  trainingDiscountPercent: string;
  isTrainingFree: boolean;
  description: string;
}

const packageTypeOptions = [
  { value: 0, label: "Single" },
  { value: 1, label: "Monthly" },
  { value: 2, label: "Yearly" },
  { value: 3, label: "Custom" },
];

const durationUnitOptions = [
  { value: 0, label: "Day" },
  { value: 1, label: "Month" },
  { value: 2, label: "Year" },
];

const emptyPackageForm = (): MembershipPackageFormState => ({
  name: "",
  packageType: 1,
  durationValue: "1",
  durationUnit: 1,
  basePrice: "79",
  currencyCode: "EUR",
  trainingDiscountPercent: "",
  isTrainingFree: false,
  description: "",
});

export function MembershipPackagesPage() {
  const { api, session } = useAuth();
  const { t } = useLanguage();
  const [packages, setPackages] = useState<MembershipPackage[]>([]);
  const [query, setQuery] = useState("");
  const [activePackageId, setActivePackageId] = useState<string | null>(null);
  const [form, setForm] = useState<MembershipPackageFormState>(() => emptyPackageForm());
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const deferredQuery = useDeferredValue(query);

  useEffect(() => {
    void loadPackages();
  }, []);

  const filteredPackages = packages.filter((membershipPackage) => {
    const normalizedQuery = deferredQuery.trim().toLowerCase();
    if (!normalizedQuery) {
      return true;
    }

    return (
      membershipPackage.name.toLowerCase().includes(normalizedQuery) ||
      (membershipPackage.description ?? "").toLowerCase().includes(normalizedQuery) ||
      membershipPackage.currencyCode.toLowerCase().includes(normalizedQuery)
    );
  });

  async function loadPackages() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      setPackages(await api.getMembershipPackages(session.activeGymCode));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load membership packages.");
    } finally {
      setIsLoading(false);
    }
  }

  function resetForm() {
    setActivePackageId(null);
    setForm(emptyPackageForm());
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!session?.activeGymCode) {
      return;
    }

    const validationErrors = validatePackageForm(form);
    if (validationErrors.length > 0) {
      setNotice({
        tone: "error",
        title: "Fix the package form before saving",
        messages: validationErrors,
      });
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      const payload = toPackageRequest(form);
      const savedPackage = activePackageId
        ? await api.updateMembershipPackage(session.activeGymCode, activePackageId, payload)
        : await api.createMembershipPackage(session.activeGymCode, payload);

      setActivePackageId(savedPackage.id);
      setForm(toPackageForm(savedPackage));
      await loadPackages();
      setNotice({
        tone: "success",
        title: activePackageId ? "Package updated" : "Package created",
        messages: [`${savedPackage.name} is ready for the sales flow.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not save package",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (packageId: string, name: string) => {
    if (!session?.activeGymCode) {
      return;
    }

    if (!window.confirm(`Delete ${name}?`)) {
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      await api.deleteMembershipPackage(session.activeGymCode, packageId);
      await loadPackages();

      if (activePackageId === packageId) {
        resetForm();
      }

      setNotice({
        tone: "success",
        title: "Package deleted",
        messages: [`${name} was removed from the pricing catalog.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not delete package",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">{t("CRUD area 3 / 3")}</p>
          <h2 className="workspace__title">{t("Membership Packages")}</h2>
          <p className="workspace__copy">
            Price and shape the memberships that power sales, billing, discounts, and member access windows.
          </p>
        </div>
        <button className="button button--secondary" onClick={resetForm} type="button">
          {t("New package")}
        </button>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        <section className="panel panel--list">
          <div className="toolbar">
            <label className="field">
              <span>{t("Search packages")}</span>
              <input
                onChange={(event) => setQuery(event.target.value)}
                placeholder={t("Name, currency, or description")}
                type="search"
                value={query}
              />
            </label>
            <button className="button button--ghost" disabled={!query} onClick={() => setQuery("")} type="button">
              {t("Clear filter")}
            </button>
          </div>

          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">{t("Loading membership packages...")}</p> : null}
          {!isLoading && packages.length === 0 ? (
            <p className="state">{t("No membership packages exist yet. Add the first offer from the editor.")}</p>
          ) : null}
          {!isLoading && packages.length > 0 && filteredPackages.length === 0 ? (
            <p className="state">{t("No packages match the current filter.")}</p>
          ) : null}

          <div className="record-list" role="list">
            {filteredPackages.map((membershipPackage) => (
              <article className="record-card" key={membershipPackage.id} role="listitem">
                <button
                  className="record-card__body"
                  onClick={() => {
                    setActivePackageId(membershipPackage.id);
                    setForm(toPackageForm(membershipPackage));
                  }}
                  type="button"
                >
                  <strong>{membershipPackage.name}</strong>
                  <span>
                    {packageTypeLabel(membershipPackage.packageType)} • {membershipPackage.durationValue}{" "}
                    {durationUnitLabel(membershipPackage.durationUnit)}
                  </span>
                  <span>
                    {membershipPackage.basePrice} {membershipPackage.currencyCode}
                  </span>
                </button>
                <button
                  className="button button--danger"
                  onClick={() => void handleDelete(membershipPackage.id, membershipPackage.name)}
                  type="button"
                >
                  {t("Delete")}
                </button>
              </article>
            ))}
          </div>
        </section>

        <section className="panel">
          <div className="editor-header">
            <div>
              <p className="workspace__eyebrow">{activePackageId ? "Editing package" : "Create a package"}</p>
              <h3>{activePackageId ? "Package editor" : "New package"}</h3>
            </div>
          </div>

          <form className="form" onSubmit={(event) => void handleSubmit(event)}>
            <label className="field">
              <span>{t("Name")}</span>
              <input
                name="name"
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                value={form.name}
              />
            </label>

            <div className="form__two-up">
              <label className="field">
                <span>{t("Package type")}</span>
                <select
                  name="packageType"
                  onChange={(event) =>
                    setForm((current) => ({ ...current, packageType: Number(event.target.value) as MembershipPackageType }))
                  }
                  value={form.packageType}
                >
                  {packageTypeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="field">
                <span>{t("Duration value")}</span>
                <input
                  min="1"
                  name="durationValue"
                  onChange={(event) => setForm((current) => ({ ...current, durationValue: event.target.value }))}
                  step="1"
                  type="number"
                  value={form.durationValue}
                />
              </label>
            </div>

            <div className="form__two-up">
              <label className="field">
                <span>{t("Duration unit")}</span>
                <select
                  name="durationUnit"
                  onChange={(event) =>
                    setForm((current) => ({ ...current, durationUnit: Number(event.target.value) as DurationUnit }))
                  }
                  value={form.durationUnit}
                >
                  {durationUnitOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="field">
                <span>{t("Base price")}</span>
                <input
                  min="0"
                  name="basePrice"
                  onChange={(event) => setForm((current) => ({ ...current, basePrice: event.target.value }))}
                  step="0.01"
                  type="number"
                  value={form.basePrice}
                />
              </label>
            </div>

            <div className="form__two-up">
              <label className="field">
                <span>{t("Currency code")}</span>
                <input
                  maxLength={8}
                  name="currencyCode"
                  onChange={(event) => setForm((current) => ({ ...current, currencyCode: event.target.value.toUpperCase() }))}
                  value={form.currencyCode}
                />
              </label>
              <label className="field">
                <span>{t("Training discount %")}</span>
                <input
                  max="100"
                  min="0"
                  name="trainingDiscountPercent"
                  onChange={(event) =>
                    setForm((current) => ({ ...current, trainingDiscountPercent: event.target.value }))
                  }
                  step="1"
                  type="number"
                  value={form.trainingDiscountPercent}
                />
              </label>
            </div>

            <label className="field field--checkbox">
              <input
                checked={form.isTrainingFree}
                name="isTrainingFree"
                onChange={(event) => setForm((current) => ({ ...current, isTrainingFree: event.target.checked }))}
                type="checkbox"
              />
              <span>{t("Training sessions are free with this package")}</span>
            </label>

            <label className="field">
              <span>{t("Description")}</span>
              <textarea
                name="description"
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                rows={6}
                value={form.description}
              />
            </label>

            <div className="form__actions">
              <button className="button" disabled={isSubmitting} type="submit">
                {isSubmitting ? t("Saving...") : activePackageId ? t("Save package") : t("Create package")}
              </button>
              <button className="button button--ghost" disabled={isSubmitting} onClick={resetForm} type="button">
                {t("Reset")}
              </button>
            </div>
          </form>
        </section>
      </div>
    </section>
  );
}

function toPackageForm(membershipPackage: MembershipPackage): MembershipPackageFormState {
  return {
    name: membershipPackage.name,
    packageType: membershipPackage.packageType,
    durationValue: String(membershipPackage.durationValue),
    durationUnit: membershipPackage.durationUnit,
    basePrice: String(membershipPackage.basePrice),
    currencyCode: membershipPackage.currencyCode,
    trainingDiscountPercent: membershipPackage.trainingDiscountPercent?.toString() ?? "",
    isTrainingFree: membershipPackage.isTrainingFree,
    description: membershipPackage.description ?? "",
  };
}

function toPackageRequest(form: MembershipPackageFormState) {
  return {
    name: form.name.trim(),
    packageType: form.packageType,
    durationValue: Number(form.durationValue),
    durationUnit: form.durationUnit,
    basePrice: Number(form.basePrice),
    currencyCode: form.currencyCode.trim().toUpperCase(),
    trainingDiscountPercent: form.trainingDiscountPercent.trim()
      ? Number(form.trainingDiscountPercent)
      : null,
    isTrainingFree: form.isTrainingFree,
    description: form.description.trim() || null,
  };
}

function validatePackageForm(form: MembershipPackageFormState): string[] {
  const errors: string[] = [];

  if (!form.name.trim()) {
    errors.push("Package name is required.");
  }

  if (!Number.isFinite(Number(form.durationValue)) || Number(form.durationValue) <= 0) {
    errors.push("Duration value must be greater than zero.");
  }

  if (!Number.isFinite(Number(form.basePrice)) || Number(form.basePrice) < 0) {
    errors.push("Base price must be zero or greater.");
  }

  if (!form.currencyCode.trim()) {
    errors.push("Currency code is required.");
  }

  if (form.trainingDiscountPercent.trim()) {
    const discountValue = Number(form.trainingDiscountPercent);
    if (!Number.isFinite(discountValue) || discountValue < 0 || discountValue > 100) {
      errors.push("Training discount must be between 0 and 100.");
    }
  }

  return errors;
}

function packageTypeLabel(packageType: MembershipPackageType) {
  return packageTypeOptions.find((option) => option.value === packageType)?.label ?? "Unknown";
}

function durationUnitLabel(durationUnit: DurationUnit) {
  return durationUnitOptions.find((option) => option.value === durationUnit)?.label ?? "Unknown";
}
