import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, ProtectedLayout, useAuth } from "./lib/auth";
import { AttendancePage } from "./pages/AttendancePage";
import { FinanceWorkspacePage } from "./pages/FinanceWorkspacePage";
import { LoginPage } from "./pages/LoginPage";
import { MaintenanceTasksPage } from "./pages/MaintenanceTasksPage";
import { MemberWorkspacePage } from "./pages/MemberWorkspacePage";
import { MembersPage } from "./pages/MembersPage";
import { MembershipPackagesPage } from "./pages/MembershipPackagesPage";
import { SaasConsolePage } from "./pages/SaasConsolePage";
import { SessionsPage } from "./pages/SessionsPage";
import { TrainerCoachingWorkspacePage } from "./pages/TrainerCoachingWorkspacePage";
import { TrainingCategoriesPage } from "./pages/TrainingCategoriesPage";
import { LanguageProvider } from "./lib/language";

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<LoginPage />} path="/login" />
      <Route element={<ProtectedLayout />}>
        <Route element={<RoleLandingRedirect />} index />
        <Route element={<SaasConsolePage />} path="/platform" />
        <Route element={<SaasConsolePage />} path="/console" />
        <Route element={<MembersPage />} path="/members" />
        <Route element={<SessionsPage />} path="/sessions" />
        <Route element={<AttendancePage />} path="/attendance" />
        <Route element={<MaintenanceTasksPage />} path="/maintenance" />
        <Route element={<MemberWorkspacePage />} path="/member-workspace" />
        <Route element={<TrainerCoachingWorkspacePage />} path="/coaching-workspace" />
        <Route element={<FinanceWorkspacePage />} path="/finance-workspace" />
        <Route element={<TrainingCategoriesPage />} path="/training-categories" />
        <Route element={<MembershipPackagesPage />} path="/membership-packages" />
      </Route>
      <Route element={<Navigate replace to="/" />} path="*" />
    </Routes>
  );
}

function RoleLandingRedirect() {
  const { session } = useAuth();

  if (session?.systemRoles.some((role) => role === "SystemAdmin" || role === "SystemSupport" || role === "SystemBilling")) {
    return <Navigate replace to="/platform" />;
  }

  if (session?.activeRole === "Trainer") {
    return <Navigate replace to="/coaching-workspace" />;
  }

  if (session?.activeRole === "Caretaker") {
    return <Navigate replace to="/maintenance" />;
  }

  if (session?.activeRole === "Member") {
    return <Navigate replace to="/member-workspace" />;
  }

  return <Navigate replace to="/members" />;
}

export default function App() {
  return (
    <LanguageProvider>
      <AuthProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </AuthProvider>
    </LanguageProvider>
  );
}
