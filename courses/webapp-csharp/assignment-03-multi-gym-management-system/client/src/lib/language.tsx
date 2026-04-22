import { createContext, type PropsWithChildren, useContext, useEffect, useMemo, useState } from "react";

export const LANGUAGE_STORAGE_KEY = "multi-gym-client-language";

export type AppLanguage = "en" | "et-EE";

type TranslationKey = string;

const translations: Record<AppLanguage, Record<TranslationKey, string>> = {
  en: {
    activeGym: "Active gym",
    activeRole: "Active role",
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
    loginTitle: "React SaaS client",
    loginDetailsRequired: "Enter your login details",
    loginDetailsRequiredMessage: "Email and password are required.",
    systemAdmin: "System admin",
    gymAdmin: "Gym admin",
    trainerCaretaker: "Trainer / caretaker",
    member: "Member",
    members: "Members",
    membershipPackages: "Membership Packages",
    password: "Password",
    platform: "Platform",
    platformTenantConsole: "Platform and Tenant Console",
    refresh: "Refresh",
    refreshing: "Refreshing...",
    role: "Role",
    sessionExpired: "Session expired. Please sign in again.",
    sessions: "Sessions",
    signIn: "Sign in",
    signingIn: "Signing in...",
    systemRoles: "System roles",
    trainingCategories: "Training Categories",
    switchTenant: "Switch active tenant",
    shellSubtitle: "JWT, refresh tokens, platform administration, tenant operations, and role workspaces.",
    system: "System",
    tenant: "Tenant",
  },
  "et-EE": {
    activeGym: "Aktiivne jõusaal",
    activeRole: "Aktiivne roll",
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
    loginTitle: "Reacti SaaS klient",
    loginDetailsRequired: "Sisesta sisselogimisandmed",
    loginDetailsRequiredMessage: "E-post ja parool on kohustuslikud.",
    systemAdmin: "Süsteemiadmin",
    gymAdmin: "Jõusaali admin",
    trainerCaretaker: "Treener / hooldaja",
    member: "Liige",
    members: "Liikmed",
    membershipPackages: "Liikmepaketid",
    password: "Parool",
    platform: "Platvorm",
    platformTenantConsole: "Platvormi ja tenant'i konsool",
    refresh: "Värskenda",
    refreshing: "Värskendan...",
    role: "Roll",
    sessionExpired: "Sessioon aegus. Palun logi uuesti sisse.",
    sessions: "Treeningud",
    signIn: "Logi sisse",
    signingIn: "Login sisse...",
    systemRoles: "Süsteemi rollid",
    trainingCategories: "Treeningu kategooriad",
    switchTenant: "Vaheta aktiivset tenant'i",
    shellSubtitle: "JWT, värskendustokenid, platvormi haldus, tenant'i operatsioonid ja rollipõhised töölauad.",
    system: "Süsteem",
    tenant: "Tenant",
    "CRUD area 1 / 3": "CRUD ala 1 / 3",
    "CRUD area 2 / 3": "CRUD ala 2 / 3",
    "CRUD area 3 / 3": "CRUD ala 3 / 3",
    "New member": "Uus liige",
    "Search members": "Otsi liikmeid",
    "Name, code, or status": "Nimi, kood või staatus",
    "Clear filter": "Tühjenda filter",
    "Loading members...": "Laadin liikmeid...",
    "No members exist in this gym yet. Create the first member from the form.": "Selles jõusaalis ei ole veel liikmeid. Lisa esimene liige vormist.",
    "No members match the current filter.": "Ükski liige ei vasta filtrile.",
    "First name": "Eesnimi",
    "Last name": "Perekonnanimi",
    "Member code": "Liikmekood",
    "Personal code": "Isikukood",
    "Date of birth": "Sünniaeg",
    "Create member": "Loo liige",
    "Save member": "Salvesta liige",
    "Saving...": "Salvestan...",
    "Reset": "Lähtesta",
    "Delete": "Kustuta",
    "REST workflow": "REST töövoog",
    "Loading sessions...": "Laadin treeninguid...",
    "No training sessions have been published yet.": "Avaldatud treeninguid veel ei ole.",
    "Select a session to open booking details.": "Vali treening, et avada broneeringu detailid.",
    "Loading session details...": "Laadin treeningu detaile...",
    "Session detail": "Treeningu detail",
    "When": "Aeg",
    "Capacity": "Mahutavus",
    "Price": "Hind",
    "Trainers": "Treenerid",
    "Member": "Liige",
    "Payment reference": "Makse viide",
    "Book session": "Broneeri treening",
    "Booking...": "Broneerin...",
    "No member profile is available for this role.": "Sellel rollil ei ole liikmeprofiili.",
    "Only published sessions can be booked.": "Broneerida saab ainult avaldatud treeninguid.",
    "Trainer workflow": "Treeneri töövoog",
    "Loading assigned bookings...": "Laadin määratud broneeringuid...",
    "No assigned bookings are ready for attendance.": "Kohaloleku märkimiseks pole broneeringuid.",
    "Attendance": "Kohalolek",
    "Update": "Uuenda",
    "Caretaker workflow": "Hooldaja töövoog",
    "Maintenance Tasks": "Hooldustööd",
    "Loading maintenance tasks...": "Laadin hooldustöid...",
    "No maintenance tasks are assigned in this gym.": "Selles jõusaalis ei ole hooldustöid määratud.",
    "Status": "Staatus",
    "Notes": "Märkmed",
    "New category": "Uus kategooria",
    "Training Categories": "Treeningu kategooriad",
    "Search categories": "Otsi kategooriaid",
    "Name or description": "Nimi või kirjeldus",
    "Loading categories...": "Laadin kategooriaid...",
    "No training categories exist yet. Add the first one from the editor.": "Treeningu kategooriaid veel ei ole. Lisa esimene redaktorist.",
    "No categories match the current filter.": "Ükski kategooria ei vasta filtrile.",
    "Name": "Nimi",
    "Description": "Kirjeldus",
    "Create category": "Loo kategooria",
    "Save category": "Salvesta kategooria",
    "New package": "Uus pakett",
    "Membership Packages": "Liikmepaketid",
    "Search packages": "Otsi pakette",
    "Name, currency, or description": "Nimi, valuuta või kirjeldus",
    "Loading membership packages...": "Laadin liikmepakette...",
    "No membership packages exist yet. Add the first offer from the editor.": "Liikmepakette veel ei ole. Lisa esimene pakkumine redaktorist.",
    "No packages match the current filter.": "Ükski pakett ei vasta filtrile.",
    "Package type": "Paketi tüüp",
    "Duration value": "Kestuse väärtus",
    "Duration unit": "Kestuse ühik",
    "Base price": "Baashind",
    "Currency code": "Valuutakood",
    "Training discount %": "Treeningu soodustus %",
    "Training sessions are free with this package": "Treeningud on selle paketiga tasuta",
    "Create package": "Loo pakett",
    "Save package": "Salvesta pakett",
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
      t: (key) => translations[language][key] ?? translations.en[key] ?? key,
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
