// Seeded backend credentials used by the dev bypass buttons.
// Override via Vite env (.env.local) without committing real secrets:
//   VITE_DEV_ADMIN_EMAIL=...    VITE_DEV_ADMIN_PASSWORD=...
//   VITE_DEV_MEMBER_EMAIL=...   VITE_DEV_MEMBER_PASSWORD=...

const env = import.meta.env;

export interface DevAccount {
  label: string;
  email: string;
  password: string;
  hint: string;
}

export const DEV_ACCOUNTS: DevAccount[] = [
  {
    label: "Dev login · Gym admin",
    email: (env.VITE_DEV_ADMIN_EMAIL as string) ?? "admin@itcollege.ee",
    password: (env.VITE_DEV_ADMIN_PASSWORD as string) ?? "Foo.Bar.1",
    hint: "Seeded GymAdmin/SystemAdmin account from the cweb backend",
  },
  {
    label: "Dev login · Member",
    email: (env.VITE_DEV_MEMBER_EMAIL as string) ?? "member@itcollege.ee",
    password: (env.VITE_DEV_MEMBER_PASSWORD as string) ?? "Foo.Bar.1",
    hint: "Seeded regular Member account",
  },
];

export const DEV_BYPASS_ENABLED =
  import.meta.env.DEV || (env.VITE_ENABLE_DEV_LOGIN as string) === "true";
