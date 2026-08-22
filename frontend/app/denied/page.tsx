import Link from "next/link";

const DENIAL_MESSAGES: Record<string, string> = {
  ny_state:
    "No se pueden procesar solicitudes desde el estado de Nueva York (NY).",
  ssn_blacklisted:
    "El número de Seguro Social indicado está en la lista negra y no puede solicitar un préstamo.",
};

export default async function DeniedPage({
  searchParams,
}: PageProps<"/denied">) {
  const params = await searchParams;
  const reason = typeof params.reason === "string" ? params.reason : "";
  const message =
    DENIAL_MESSAGES[reason] ?? "Tu solicitud no pudo ser aprobada.";

  return (
    <main className="flex-1 flex items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-red-100 text-2xl">
          ✕
        </div>
        <h1 className="mt-4 text-2xl font-bold text-slate-900">
          Solicitud denegada
        </h1>
        <p className="mt-3 text-slate-600">{message}</p>
        <Link
          href="/"
          className="mt-6 inline-block rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700"
        >
          Volver al formulario
        </Link>
      </div>
    </main>
  );
}
