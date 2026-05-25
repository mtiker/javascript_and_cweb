import type { ReactNode } from "react";

interface PageBannerProps {
  image: string;
  eyebrow?: string;
  title: string;
  subtitle?: ReactNode;
  actions?: ReactNode;
  /** object-position to crop out unwanted regions of the image */
  imagePosition?: string;
}

export function PageBanner({
  image,
  eyebrow,
  title,
  subtitle,
  actions,
  imagePosition = "center",
}: PageBannerProps) {
  return (
    <div className="relative mb-8 overflow-hidden rounded-2xl border border-border/60 shadow-[var(--shadow-elegant)]">
      <div className="absolute inset-0">
        <img
          src={image}
          alt=""
          loading="lazy"
          width={1920}
          height={512}
          className="h-full w-full object-cover"
          style={{ objectPosition: imagePosition }}
        />
        {/* Strong left-to-right gradient hides any AI text artifacts on the right */}
        <div
          className="absolute inset-0"
          style={{
            background:
              "linear-gradient(90deg, oklch(0.16 0.02 260) 0%, oklch(0.16 0.02 260 / 0.92) 35%, oklch(0.16 0.02 260 / 0.55) 100%)",
          }}
        />
        <div
          className="absolute inset-0"
          style={{
            background:
              "linear-gradient(180deg, transparent 60%, oklch(0.16 0.02 260) 100%)",
          }}
        />
      </div>

      <div className="relative flex flex-col gap-4 px-6 py-10 sm:px-10 sm:py-14 md:flex-row md:items-end md:justify-between">
        <div className="max-w-xl">
          {eyebrow && (
            <span className="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3 py-1 text-[10px] font-medium uppercase tracking-[0.2em] text-primary">
              {eyebrow}
            </span>
          )}
          <h1 className="mt-3 text-3xl font-bold tracking-tight sm:text-4xl">
            {title}
          </h1>
          {subtitle && (
            <p className="mt-2 text-sm text-muted-foreground sm:text-base">
              {subtitle}
            </p>
          )}
        </div>
        {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
      </div>
    </div>
  );
}
