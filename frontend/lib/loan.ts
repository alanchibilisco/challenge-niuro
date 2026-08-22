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

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export const US_STATES = [
  "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA",
  "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA",
  "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY",
  "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX",
  "UT", "VT", "VA", "WA", "WV", "WI", "WY",
] as const;

export function formatSsn(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 9);
  const parts = [digits.slice(0, 3), digits.slice(3, 5), digits.slice(5, 9)];
  return parts.filter(Boolean).join("-");
}

export function isValidSsn(value: string): boolean {
  return /^\d{3}-?\d{2}-?\d{4}$/.test(value);
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
