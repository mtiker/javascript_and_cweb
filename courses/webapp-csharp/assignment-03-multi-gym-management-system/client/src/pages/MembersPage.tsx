import { useDeferredValue, useEffect, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { MemberDetail, MemberStatus, MemberSummary, Notice } from "../lib/types";
import { getErrorMessages } from "../lib/types";

interface MemberFormState {
  firstName: string;
  lastName: string;
  personalCode: string;
  dateOfBirth: string;
  memberCode: string;
  status: MemberStatus;
}

const emptyMemberForm = (): MemberFormState => ({
  firstName: "",
  lastName: "",
  personalCode: "",
  dateOfBirth: "",
  memberCode: "",
  status: 0,
});

const memberStatusOptions = [
  { value: 0, label: "Active" },
  { value: 1, label: "Suspended" },
  { value: 2, label: "Left" },
];

export function MembersPage() {
  const { api, session } = useAuth();
  const [members, setMembers] = useState<MemberSummary[]>([]);
  const [query, setQuery] = useState("");
  const [activeMemberId, setActiveMemberId] = useState<string | null>(null);
  const [form, setForm] = useState<MemberFormState>(() => emptyMemberForm());
  const [isLoading, setIsLoading] = useState(true);
  const [isEditorLoading, setIsEditorLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const deferredQuery = useDeferredValue(query);

  useEffect(() => {
    void loadMembers();
  }, []);

  const filteredMembers = members.filter((member) => {
    const normalizedQuery = deferredQuery.trim().toLowerCase();
    if (!normalizedQuery) {
      return true;
    }

    return (
      member.fullName.toLowerCase().includes(normalizedQuery) ||
      member.memberCode.toLowerCase().includes(normalizedQuery) ||
      memberStatusLabel(member.status).toLowerCase().includes(normalizedQuery)
    );
  });

  async function loadMembers() {
    if (!session?.activeGymCode) {
      return;
    }

    setIsLoading(true);
    setPageError(null);

    try {
      setMembers(await api.getMembers(session.activeGymCode));
    } catch (error) {
      setPageError(getErrorMessages(error)[0] ?? "Could not load members.");
    } finally {
      setIsLoading(false);
    }
  }

  async function startEditing(memberId: string) {
    if (!session?.activeGymCode) {
      return;
    }

    setIsEditorLoading(true);
    setNotice(null);

    try {
      const detail = await api.getMember(session.activeGymCode, memberId);
      setActiveMemberId(detail.id);
      setForm(toMemberForm(detail));
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not open member",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsEditorLoading(false);
    }
  }

  function resetForm() {
    setActiveMemberId(null);
    setForm(emptyMemberForm());
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!session?.activeGymCode) {
      return;
    }

    const validationErrors = validateMemberForm(form);
    if (validationErrors.length > 0) {
      setNotice({
        tone: "error",
        title: "Fix the member form before saving",
        messages: validationErrors,
      });
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      const savedMember = activeMemberId
        ? await api.updateMember(session.activeGymCode, activeMemberId, toMemberRequest(form))
        : await api.createMember(session.activeGymCode, toMemberRequest(form));

      setActiveMemberId(savedMember.id);
      setForm(toMemberForm(savedMember));
      await loadMembers();
      setNotice({
        tone: "success",
        title: activeMemberId ? "Member updated" : "Member created",
        messages: [`${savedMember.fullName} is now synced with the API.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not save member",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (memberId: string, fullName: string) => {
    if (!session?.activeGymCode) {
      return;
    }

    if (!window.confirm(`Delete ${fullName}?`)) {
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      await api.deleteMember(session.activeGymCode, memberId);
      await loadMembers();

      if (activeMemberId === memberId) {
        resetForm();
      }

      setNotice({
        tone: "success",
        title: "Member deleted",
        messages: [`${fullName} was removed from the active gym.`],
      });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not delete member",
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
          <p className="workspace__eyebrow">CRUD area 1 / 3</p>
          <h2 className="workspace__title">Members</h2>
          <p className="workspace__copy">
            Manage member profiles through the same tenant-scoped API that the ASP.NET Core MVC admin uses.
          </p>
        </div>
        <button className="button button--secondary" onClick={resetForm} type="button">
          New member
        </button>
      </header>

      <NoticeBanner notice={notice} />

      <div className="workspace__grid">
        <section className="panel panel--list">
          <div className="toolbar">
            <label className="field">
              <span>Search members</span>
              <input
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Name, code, or status"
                type="search"
                value={query}
              />
            </label>
            <button className="button button--ghost" disabled={!query} onClick={() => setQuery("")} type="button">
              Clear filter
            </button>
          </div>

          {pageError ? <p className="state state--error">{pageError}</p> : null}
          {isLoading ? <p className="state">Loading members...</p> : null}
          {!isLoading && members.length === 0 ? (
            <p className="state">No members exist in this gym yet. Create the first member from the form.</p>
          ) : null}
          {!isLoading && members.length > 0 && filteredMembers.length === 0 ? (
            <p className="state">No members match the current filter.</p>
          ) : null}

          <div className="record-list" role="list">
            {filteredMembers.map((member) => (
              <article className="record-card" key={member.id} role="listitem">
                <button className="record-card__body" onClick={() => void startEditing(member.id)} type="button">
                  <strong>{member.fullName}</strong>
                  <span>{member.memberCode}</span>
                  <span>{memberStatusLabel(member.status)}</span>
                </button>
                <button
                  className="button button--danger"
                  onClick={() => void handleDelete(member.id, member.fullName)}
                  type="button"
                >
                  Delete
                </button>
              </article>
            ))}
          </div>
        </section>

        <section className="panel">
          <div className="editor-header">
            <div>
              <p className="workspace__eyebrow">{activeMemberId ? "Editing existing member" : "Create a new member"}</p>
              <h3>{activeMemberId ? "Member editor" : "New member"}</h3>
            </div>
            {isEditorLoading ? <span className="badge">Loading details...</span> : null}
          </div>

          <form className="form" onSubmit={(event) => void handleSubmit(event)}>
            <div className="form__two-up">
              <label className="field">
                <span>First name</span>
                <input
                  name="firstName"
                  onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))}
                  value={form.firstName}
                />
              </label>
              <label className="field">
                <span>Last name</span>
                <input
                  name="lastName"
                  onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))}
                  value={form.lastName}
                />
              </label>
            </div>

            <div className="form__two-up">
              <label className="field">
                <span>Member code</span>
                <input
                  name="memberCode"
                  onChange={(event) => setForm((current) => ({ ...current, memberCode: event.target.value }))}
                  value={form.memberCode}
                />
              </label>
              <label className="field">
                <span>Status</span>
                <select
                  name="status"
                  onChange={(event) =>
                    setForm((current) => ({ ...current, status: Number(event.target.value) as MemberStatus }))
                  }
                  value={form.status}
                >
                  {memberStatusOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <div className="form__two-up">
              <label className="field">
                <span>Personal code</span>
                <input
                  name="personalCode"
                  onChange={(event) => setForm((current) => ({ ...current, personalCode: event.target.value }))}
                  value={form.personalCode}
                />
              </label>
              <label className="field">
                <span>Date of birth</span>
                <input
                  name="dateOfBirth"
                  onChange={(event) => setForm((current) => ({ ...current, dateOfBirth: event.target.value }))}
                  type="date"
                  value={form.dateOfBirth}
                />
              </label>
            </div>

            <div className="form__actions">
              <button className="button" disabled={isSubmitting || isEditorLoading} type="submit">
                {isSubmitting ? "Saving..." : activeMemberId ? "Save member" : "Create member"}
              </button>
              <button className="button button--ghost" disabled={isSubmitting} onClick={resetForm} type="button">
                Reset
              </button>
            </div>
          </form>
        </section>
      </div>
    </section>
  );
}

function toMemberForm(member: MemberDetail): MemberFormState {
  return {
    firstName: member.firstName,
    lastName: member.lastName,
    personalCode: member.personalCode ?? "",
    dateOfBirth: member.dateOfBirth ?? "",
    memberCode: member.memberCode,
    status: member.status,
  };
}

function toMemberRequest(form: MemberFormState) {
  return {
    firstName: form.firstName.trim(),
    lastName: form.lastName.trim(),
    personalCode: form.personalCode.trim() || null,
    dateOfBirth: form.dateOfBirth || null,
    memberCode: form.memberCode.trim(),
    status: form.status,
  };
}

function validateMemberForm(form: MemberFormState): string[] {
  const errors: string[] = [];

  if (!form.firstName.trim()) {
    errors.push("First name is required.");
  }

  if (!form.lastName.trim()) {
    errors.push("Last name is required.");
  }

  if (!form.memberCode.trim()) {
    errors.push("Member code is required.");
  }

  return errors;
}

function memberStatusLabel(status: MemberStatus) {
  return memberStatusOptions.find((option) => option.value === status)?.label ?? "Unknown";
}
