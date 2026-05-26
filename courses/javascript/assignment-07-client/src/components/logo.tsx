import logoMark from "@/assets/logo.png";
import { cn } from "@/lib/utils";

interface LogoProps {
  className?: string;
  showText?: boolean;
  size?: "sm" | "md" | "lg" | "xl";
}

const sizeMap = {
  sm: { img: 28, text: "text-sm" },
  md: { img: 36, text: "text-base" },
  lg: { img: 48, text: "text-lg" },
  xl: { img: 64, text: "text-2xl" },
};

export function Logo({ className, showText = true, size = "md" }: LogoProps) {
  const s = sizeMap[size];
  return (
    <div className={cn("inline-flex items-center gap-2.5", className)}>
      <img
        src={logoMark}
        alt=""
        width={s.img}
        height={s.img}
        className="shrink-1 object-contain"
        style={{ width: s.img, height: s.img }}
      />
      {showText && (
        <span className={cn("font-bold tracking-tight leading-none", s.text)}>
          <span className="text-primary">CWeb</span>
          <span className="text-foreground">Gym</span>
        </span>
      )}
    </div>
  );
}
