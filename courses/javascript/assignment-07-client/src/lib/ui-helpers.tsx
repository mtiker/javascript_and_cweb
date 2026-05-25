import { useAuth } from "@/lib/api/auth-context";

export function NoActiveGym() {
  const auth = useAuth();
  return (
    <div className="rounded-md border border-dashed border-border p-6 text-center">
      <p className="text-sm text-muted-foreground">
        No active gym selected on your account. Ask a gym admin to grant access, or use{" "}
        <code>/account/switch-gym</code> via the API.
      </p>
      <p className="mt-2 text-xs text-muted-foreground">
        Available tenants on your session:{" "}
        {auth.session?.availableTenants?.length
          ? auth.session.availableTenants.map((t) => t.gymCode).join(", ")
          : "none"}
      </p>
    </div>
  );
}

export function fmtDate(iso: string | null | undefined): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

export function fmtMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

export function enumLabel<T extends Record<string, string | number>>(e: T, value: number): string {
  const found = (Object.entries(e) as [string, string | number][]).find(
    ([, v]) => typeof v === "number" && v === value,
  );
  return found ? found[0] : String(value);
}
