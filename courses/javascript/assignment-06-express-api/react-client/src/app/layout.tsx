import type { Metadata } from "next";
import "bootstrap/dist/css/bootstrap.css";
import "@/styles/globals.css";

import { AuthProvider } from "@/context/AuthContext";
import { TodoProvider } from "@/context/TodoContext";
import BootstrapActivation from "@/components/BootstrapActivation";
import ErrorBoundary from "@/components/ErrorBoundary";
import NavBar from "@/components/NavBar";

export const metadata: Metadata = {
  title: "TaskFlow — Secure Todo",
  description:
    "JWT + refresh-token secured Todo client built on Next.js for the TalTech web-apps course.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" data-scroll-behavior="smooth">
      <body className="tf-app-shell">
        <ErrorBoundary>
          <AuthProvider>
            <TodoProvider>
              <NavBar />
              <main className="tf-main container py-4">{children}</main>
              <footer className="tf-footer">
                <div className="container small">
                  <span>TaskFlow · A06 Express.js backend</span>
                </div>
              </footer>
            </TodoProvider>
          </AuthProvider>
        </ErrorBoundary>
        <BootstrapActivation />
      </body>
    </html>
  );
}
