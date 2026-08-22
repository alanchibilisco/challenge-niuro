import Link from "next/link";

export default async function ApprovedPage({
  searchParams,
}: PageProps<"/approved">) {
  const params = await searchParams;
  const applicationId = typeof params.applicationId === "string" ? params.applicationId : "";
  const isNew = params.isNew !== "false";

  return (
    <main className="flex-1 flex items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-green-100 text-2xl">
          ✓
        </div>
        <h1 className="mt-4 text-2xl font-bold text-slate-900">
          ¡Solicitud aprobada!
        </h1>
        <p className="mt-3 text-slate-600">
          {isNew
            ? "Hemos recibido tu solicitud y está en proceso de revisión."
            : "Hemos actualizado tu solicitud existente con los datos más recientes."}
        </p>

        {applicationId && (
          <p className="mt-4 inline-block rounded-lg bg-slate-100 px-3 py-1.5 text-sm text-slate-700">
            Solicitud #{applicationId}
          </p>
        )}

        <p className="mt-6 text-xs text-slate-400">
          Los datos se están enviando a nuestro sistema de validación en segundo plano.
        </p>

        <Link
          href="/"
          className="mt-4 inline-block rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700"
        >
          Hacer otra solicitud
        </Link>
      </div>
    </main>
  );
}
