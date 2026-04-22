import { createContext, type PropsWithChildren, useContext, useEffect, useMemo, useState } from "react";

export const LANGUAGE_STORAGE_KEY = "multi-gym-client-language";

export type AppLanguage = "en" | "et-EE";

type TranslationKey =
  | "activeGym"
  | "adminTools"
  | "appTitle"
  | "attendance"
  | "caretaker"
  | "console"
  | "email"
  | "language"
  | "loading"
  | "logOut"
  | "loginCopy"
  | "loginEyebrow"
  | "loginFailed"
  | "member"
  | "members"
  | "membershipPackages"
  | "password"
  | "platform"
  | "role"
  | "sessionExpired"
  | "sessions"
  | "signIn"
  | "signingIn"
  | "systemRoles"
  | "trainingCategories";

const translations: Record<AppLanguage, Record<TranslationKey, string>> = {
  en: {
    activeGym: "Active gym",
    adminTools: "Admin tools",
    appTitle: "Gym Operations Workspace",
    attendance: "Attendance",
    caretaker: "Caretaker",
    console: "Function Console",
    email: "Email",
    language: "Language",
    loading: "Loading...",
    logOut: "Log out",
    loginCopy:
      "Sign in with a seeded demo account to manage platform, billing, support, tenant, trainer, or member workflows.",
    loginEyebrow: "Assignment 03 SaaS client",
    loginFailed: "Sign-in failed",
    member: "Member",
    members: "Members",
    membershipPackages: "Membership Packages",
    password: "Password",
    platform: "Platform",
    role: "Role",
    sessionExpired: "Session expired. Please sign in again.",
    sessions: "Sessions",
    signIn: "Sign in",
    signingIn: "Signing in...",
    systemRoles: "System roles",
    trainingCategories: "Training Categories",
  },
  "et-EE": {
    activeGym: "Aktiivne jõusaal",
    adminTools: "Admini tööriistad",
    appTitle: "Jõusaali töölaud",
    attendance: "Kohalolek",
    caretaker: "Hooldus",
    console: "Funktsioonide konsool",
    email: "E-post",
    language: "Keel",
    loading: "Laadin...",
    logOut: "Logi välja",
    loginCopy:
      "Logi sisse demokontoga, et hallata platvormi, arveldust, tuge, tenant'i, treeneri või liikme töövooge.",
    loginEyebrow: "Assignment 03 SaaS klient",
    loginFailed: "Sisselogimine ebaõnnestus",
    member: "Liige",
    members: "Liikmed",
    membershipPackages: "Liikmepaketid",
    password: "Parool",
    platform: "Platvorm",
    role: "Roll",
    sessionExpired: "Sessioon aegus. Palun logi uuesti sisse.",
    sessions: "Treeningud",
    signIn: "Logi sisse",
    signingIn: "Login sisse...",
    systemRoles: "Süsteemi rollid",
    trainingCategories: "Treeningu kategooriad",
  },
};

interface LanguageContextValue {
  language: AppLanguage;
  setLanguage: (language: AppLanguage) => void;
  t: (key: TranslationKey) => string;
}

const LanguageContext = createContext<LanguageContextValue | null>(null);
let currentLanguage: AppLanguage = normalizeLanguage(readStoredLanguage());

export function LanguageProvider({ children }: PropsWithChildren) {
  const [language, setLanguageState] = useState<AppLanguage>(() => currentLanguage);

  const setLanguage = (nextLanguage: AppLanguage) => {
    setCurrentLanguage(nextLanguage);
    setLanguageState(nextLanguage);
  };

  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  const value = useMemo<LanguageContextValue>(
    () => ({
      language,
      setLanguage,
      t: (key) => translations[language][key],
    }),
    [language],
  );

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error("useLanguage must be used inside LanguageProvider.");
  }

  return context;
}

export function getCurrentLanguage() {
  return currentLanguage;
}

export function setCurrentLanguage(language: AppLanguage) {
  currentLanguage = language;
  localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
}

export function normalizeLanguage(value: string | null | undefined): AppLanguage {
  return value?.toLowerCase().startsWith("et") ? "et-EE" : "en";
}

function readStoredLanguage() {
  if (typeof localStorage === "undefined") {
    return "en";
  }

  return localStorage.getItem(LANGUAGE_STORAGE_KEY) ?? "en";
}
