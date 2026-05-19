import type { Notice } from "../lib/types";

interface NoticeBannerProps {
  notice: Notice | null;
}

export function NoticeBanner({ notice }: NoticeBannerProps) {
  if (!notice) {
    return null;
  }

  return (
    <section
      aria-live="polite"
      className={`notice notice--${notice.tone}`}
      role={notice.tone === "error" ? "alert" : "status"}
    >
      <strong>{notice.title}</strong>
      {notice.messages && notice.messages.length > 0 ? (
        <ul className="notice__list">
          {notice.messages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
