export type LoanRequest = {
  firstName: string;
  lastName: string;
  address: string;
  state: string;
  companyName: string;
  requestedAmount: number;
  ssn: string;
};

export type SubmitResult =
  | {
      status: "Approved";
      customerId: number;
      applicationId: number;
      isNewCustomer: boolean;
    }
  | { status: "Denied"; denialCode: string; denialReason: string };

 export type FormSubmitEvent = NonNullable<
  React.ComponentProps<"form">["onSubmit"]
> extends (event: infer E) => unknown
  ? E
  : never;

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export function formatSsn(value: string): string {
  return value.replace(/\D/g, "").slice(0, 9);
}

export function formatAmount(value: string | number): number {
  if (typeof value === "number") {
    return value;
  }

  const normalized = value.trim();

  if (!normalized) {
    return 0;
  }

  const lastComma = normalized.lastIndexOf(",");
  const lastDot = normalized.lastIndexOf(".");

  // Si ambos existen, el último es el separador decimal
  if (lastComma !== -1 && lastDot !== -1) {
    if (lastComma > lastDot) {
      // 1.234,99
      return Number(normalized.replace(/\./g, "").replace(",", "."));
    } else {
      // 1,234.99
      return Number(normalized.replace(/,/g, ""));
    }
  }

  // Solo coma: 1234,99
  if (lastComma !== -1) {
    return Number(normalized.replace(",", "."));
  }

  // Solo punto: 1234.99 o 1.234
  return Number(normalized);
}

export function isValidSsn(value: string): boolean {
  return /^(?:\d-?){8,9}$/.test(value);
}

export function isValidAmount(value: string | number): boolean {
  if (typeof value === "number") {
    return Number.isFinite(value) && value >= 1 && value <= 10_000_000_000;
  }

  const normalized = value.trim();

  if (!normalized) {
    return false;
  }

  // Solo permite números, coma y punto
  if (!/^[\d.,]+$/.test(normalized)) {
    return false;
  }

  const lastComma = normalized.lastIndexOf(",");
  const lastDot = normalized.lastIndexOf(".");

  let amount: number;

  // Ambos separadores
  if (lastComma !== -1 && lastDot !== -1) {
    if (lastComma > lastDot) {
      // 1.234,99
      amount = Number(
        normalized.replace(/\./g, "").replace(",", ".")
      );
    } else {
      // 1,234.99
      amount = Number(
        normalized.replace(/,/g, "")
      );
    }
  }
  // Solo coma
  else if (lastComma !== -1) {
    // 1234,99
    amount = Number(normalized.replace(",", "."));
  }
  // Solo punto
  else {
    // 1234.99
    amount = Number(normalized);
  }

  return (
    Number.isFinite(amount) &&
    amount >= 1 &&
    amount <= 10_000_000_000
  );
}

export async function submitLoan(request: LoanRequest): Promise<SubmitResult> {
  const response = await fetch(`${API_BASE_URL}/api/loan-applications`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (response.status === 400) {
    const problem = await response.json().catch(() => null);
    const message = extractValidationMessage(problem) ?? "Revisa los datos del formulario.";
    throw new Error(message);
  }

  if (!response.ok) {
    throw new Error("No se pudo enviar la solicitud. Inténtalo de nuevo.");
  }

  return response.json();
}

function extractValidationMessage(problem: unknown): string | null {
  if (!problem || typeof problem !== "object") return null;
  const errors = (problem as { errors?: Record<string, string[]> }).errors;
  if (!errors) return null;
  const first = Object.values(errors)[0]?.[0];
  return first ?? null;
}
