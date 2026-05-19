const dateTimeFormatter = new Intl.DateTimeFormat("en-GB", {
  dateStyle: "medium",
  timeStyle: "short",
});

export function formatDateTime(value: string | null) {
  if (!value) {
    return "No due date";
  }

  return dateTimeFormatter.format(new Date(value));
}

export function toDateTimeLocalValue(value: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  const offset = date.getTimezoneOffset();
  const localDate = new Date(date.getTime() - offset * 60_000);
  return localDate.toISOString().slice(0, 16);
}

export function fromDateTimeLocalValue(value: string) {
  return value ? new Date(value).toISOString() : null;
}

export function isPastDue(value: string | null) {
  return Boolean(value && new Date(value).getTime() < Date.now());
}
