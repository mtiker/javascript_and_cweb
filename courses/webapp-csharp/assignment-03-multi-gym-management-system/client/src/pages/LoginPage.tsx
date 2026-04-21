import { useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type { Notice } from "../lib/types";
import { getErrorMessages } from "../lib/types";

interface LocationState {
  from?: {
    pathname?: string;
  };
}

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@peakforge.local");
  const [password, setPassword] = useState("Gym123!");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notice, setNotice] = useState<Notice | null>(null);

  if (isAuthenticated) {
    return <Navigate replace to="/members" />;
  }

  const state = location.state as LocationState | null;
  const targetPath = state?.from?.pathname || "/members";

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!email.trim() || !password.trim()) {
      setNotice({
        tone: "error",
        title: "Enter your login details",
        messages: ["Email and password are required."],
      });
      return;
    }

    setIsSubmitting(true);
    setNotice(null);

    try {
      await login({
        email: email.trim(),
        password: password.trim(),
      });
      navigate(targetPath, { replace: true });
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Sign-in failed",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <section className="login-card">
        <p className="login-card__eyebrow">Assignment 03 correction track</p>
        <h1 className="login-card__title">React admin client</h1>
        <p className="login-card__copy">
          This separate client proves that the backend REST API works outside the ASP.NET Core MVC shell, using JWT access
          tokens and refresh-token rotation.
        </p>
        <NoticeBanner notice={notice} />
        <form className="form" onSubmit={(event) => void handleSubmit(event)}>
          <label className="field">
            <span>Email</span>
            <input
              autoComplete="username"
              name="email"
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              value={email}
            />
          </label>
          <label className="field">
            <span>Password</span>
            <input
              autoComplete="current-password"
              name="password"
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              value={password}
            />
          </label>
          <button className="button" disabled={isSubmitting} type="submit">
            {isSubmitting ? "Signing in..." : "Sign in"}
          </button>
        </form>
        <div className="login-card__demo-grid">
          <article className="login-card__demo">
            <strong>Gym admin</strong>
            <span>`admin@peakforge.local`</span>
            <span>`Gym123!`</span>
          </article>
          <article className="login-card__demo">
            <strong>Gym owner</strong>
            <span>`systemadmin@gym.local`</span>
            <span>`Gym123!`</span>
          </article>
        </div>
      </section>
    </div>
  );
}
