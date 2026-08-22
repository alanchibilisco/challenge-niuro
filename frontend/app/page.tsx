"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { formatSsn, isValidSsn, submitLoan, US_STATES } from "@/lib/loan";

type FormData = {
  firstName: string;
  lastName: string;
  address: string;
  state: string;
  companyName: string;
  amount: string;
  ssn: string;
};

const emptyForm: FormData = {
  firstName: "",
  lastName: "",
  address: "",
  state: "",
  companyName: "",
  amount: "",
  ssn: "",
};

export default function LoanFormPage() {
  const router = useRouter();
  const [form, setForm] = useState<FormData>(emptyForm);
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function update<K extends keyof FormData>(key: K, value: FormData[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
    setSubmitError(null);
  }

  function validate(): boolean {
    const next: Partial<Record<keyof FormData, string>> = {};

    if (!form.firstName.trim()) next.firstName = "El nombre es obligatorio.";
    if (!form.lastName.trim()) next.lastName = "El apellido es obligatorio.";
    if (!form.address.trim()) next.address = "La dirección es obligatoria.";
    if (!form.state) next.state = "Selecciona un estado.";
    if (!form.companyName.trim()) next.companyName = "La empresa es obligatoria.";
    if (!form.amount || Number(form.amount) <= 0) {
      next.amount = "Indica un monto mayor a cero.";
    }
    if (!form.ssn.trim() || !isValidSsn(form.ssn)) {
      next.ssn = "El SSN debe tener 9 dígitos (p. ej. 123-45-6789).";
    }

    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!validate()) return;

    setSubmitting(true);
    setSubmitError(null);

    try {
      const result = await submitLoan({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        address: form.address.trim(),
        state: form.state,
        companyName: form.companyName.trim(),
        requestedAmount: Number(form.amount),
        ssn: form.ssn,
      });

      if (result.status === "Approved") {
        router.push(
          `/approved?applicationId=${result.applicationId}&customerId=${result.customerId}&isNew=${result.isNewCustomer}`,
        );
      } else {
        router.push(`/denied?reason=${encodeURIComponent(result.denialCode)}`);
      }
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Ocurrió un error inesperado.");
      setSubmitting(false);
    }
  }

  return (
    <main className="flex-1 flex items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-2xl">
        <header className="mb-8 text-center">
          <p className="text-sm font-semibold uppercase tracking-widest text-indigo-600">
            Préstamo Rápido
          </p>
          <h1 className="mt-2 text-3xl font-bold text-slate-900">
            Solicita tu préstamo
          </h1>
          <p className="mt-2 text-slate-600">
            Completa el formulario y recibirás una respuesta inmediata.
          </p>
        </header>

        <form
          onSubmit={handleSubmit}
          noValidate
          className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8"
        >
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <Field label="Nombre" error={errors.firstName}>
              <input
                type="text"
                autoComplete="given-name"
                value={form.firstName}
                onChange={(e) => update("firstName", e.target.value)}
                placeholder="Ana"
                className={inputClass(!!errors.firstName)}
              />
            </Field>

            <Field label="Apellido" error={errors.lastName}>
              <input
                type="text"
                autoComplete="family-name"
                value={form.lastName}
                onChange={(e) => update("lastName", e.target.value)}
                placeholder="Gómez"
                className={inputClass(!!errors.lastName)}
              />
            </Field>

            <div className="sm:col-span-2">
              <Field label="Dirección" error={errors.address}>
                <input
                  type="text"
                  autoComplete="street-address"
                  value={form.address}
                  onChange={(e) => update("address", e.target.value)}
                  placeholder="Calle, número, ciudad, código postal"
                  className={inputClass(!!errors.address)}
                />
              </Field>
            </div>

            <Field label="Estado" error={errors.state}>
              <select
                value={form.state}
                onChange={(e) => update("state", e.target.value)}
                className={inputClass(!!errors.state)}
              >
                <option value="">Selecciona un estado</option>
                {US_STATES.map((state) => (
                  <option key={state} value={state}>
                    {state}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="Nombre de la empresa" error={errors.companyName}>
              <input
                type="text"
                autoComplete="organization"
                value={form.companyName}
                onChange={(e) => update("companyName", e.target.value)}
                placeholder="Acme Inc."
                className={inputClass(!!errors.companyName)}
              />
            </Field>

            <Field label="Monto solicitado (USD)" error={errors.amount}>
              <div className="relative">
                <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-slate-500">
                  $
                </span>
                <input
                  type="text"
                  inputMode="decimal"
                  value={form.amount}
                  onChange={(e) => update("amount", e.target.value)}
                  placeholder="10000"
                  className={inputClass(!!errors.amount, "pl-7")}
                />
              </div>
            </Field>

            <Field label="Número de Seguro Social" error={errors.ssn}>
              <input
                type="text"
                inputMode="numeric"
                autoComplete="off"
                value={form.ssn}
                onChange={(e) => update("ssn", formatSsn(e.target.value))}
                placeholder="123-45-6789"
                className={inputClass(!!errors.ssn)}
              />
            </Field>
          </div>

          {submitError && (
            <p
              role="alert"
              className="mt-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              {submitError}
            </p>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="mt-6 w-full rounded-lg bg-indigo-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? "Enviando solicitud…" : "Enviar solicitud"}
          </button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-400">
          Demo: los SSN 111-11-1111 y 222-22-2222 están en la lista negra; NY está excluido.
        </p>
      </div>
    </main>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {error && <span className="mt-1 block text-sm text-red-600">{error}</span>}
    </label>
  );
}

function inputClass(hasError: boolean, extra = ""): string {
  return `w-full rounded-lg border px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 ${
    hasError
      ? "border-red-300 focus:ring-red-400"
      : "border-slate-300 focus:ring-indigo-500"
  } ${extra}`;
}
