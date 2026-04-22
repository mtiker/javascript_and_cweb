import { useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import { useLanguage, type AppLanguage } from "../lib/language";
import type { Notice } from "../lib/types";
import { getErrorMessages } from "../lib/types";

interface LocationState {
  from?: {
    pathname?: string;
  };
}

export function LoginPage() {
  const { isAuthenticated, login, session } = useAuth();
  const { language, setLanguage, t } = useLanguage();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@peakforge.local");
  const [password, setPassword] = useState("Gym123!");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notice, setNotice] = useState<Notice | null>(null);

  if (isAuthenticated) {
    return <Navigate replace to={session?.systemRoles.length ? "/platform" : "/members"} />;
  }

  const state = location.state as LocationState | null;
  const targetPath = state?.from?.pathname || "/";

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!email.trim() || !password.trim()) {
      setNotice({
        tone: "error",
        title: t("loginDetailsRequired"),
        messages: [t("loginDetailsRequiredMessage")],
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
        title: t("loginFailed"),
        messages: getErrorMessages(error),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <section className="login-card">
        <div className="login-card__topline">
          <p className="login-card__eyebrow">{t("loginEyebrow")}</p>
          <label className="language-select language-select--login">
            <span>{t("language")}</span>
            <select onChange={(event) => setLanguage(event.target.value as AppLanguage)} value={language}>
              <option value="en">EN</option>
              <option value="et-EE">ET</option>
            </select>
          </label>
        </div>
        <h1 className="login-card__title">{t("loginTitle")}</h1>
        <p className="login-card__copy">{t("loginCopy")}</p>
        <NoticeBanner notice={notice} />
        <form className="form" onSubmit={(event) => void handleSubmit(event)}>
          <label className="field">
            <span>{t("email")}</span>
            <input
              autoComplete="username"
              name="email"
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              value={email}
            />
          </label>
          <label className="field">
            <span>{t("password")}</span>
            <input
              autoComplete="current-password"
              name="password"
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              value={password}
            />
          </label>
          <button className="button" disabled={isSubmitting} type="submit">
            {isSubmitting ? t("signingIn") : t("signIn")}
          </button>
        </form>
        <div className="login-card__demo-grid">
          <article className="login-card__demo">
            <strong>{t("systemAdmin")}</strong>
            <span>`systemadmin@gym.local`</span>
            <span>`Gym123!`</span>
          </article>
          <article className="login-card__demo">
            <strong>{t("gymAdmin")}</strong>
            <span>`admin@peakforge.local`</span>
            <span>`Gym123!`</span>
          </article>
          <article className="login-card__demo">
            <strong>{t("member")}</strong>
            <span>`member@peakforge.local`</span>
            <span>`Gym123!`</span>
          </article>
          <article className="login-card__demo">
            <strong>{t("trainerCaretaker")}</strong>
            <span>`trainer@peakforge.local`</span>
            <span>`caretaker@peakforge.local`</span>
          </article>
        </div>
      </section>
    </div>
  );
}
