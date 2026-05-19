import type { FieldError, UseFormRegisterReturn } from "react-hook-form";

interface FormFieldProps {
  id: string;
  label: string;
  type?: React.HTMLInputTypeAttribute;
  placeholder?: string;
  autoComplete?: string;
  error?: FieldError;
  registration: UseFormRegisterReturn;
}

export function FormField({
  id,
  label,
  type = "text",
  placeholder,
  autoComplete,
  error,
  registration,
}: FormFieldProps) {
  return (
    <div className="mb-3">
      <label htmlFor={id} className="form-label fw-semibold">
        {label}
      </label>
      <input
        id={id}
        type={type}
        placeholder={placeholder}
        autoComplete={autoComplete}
        className={`form-control ${error ? "is-invalid" : ""}`}
        aria-invalid={error ? "true" : "false"}
        {...registration}
      />
      {error && <div className="invalid-feedback">{error.message}</div>}
    </div>
  );
}
