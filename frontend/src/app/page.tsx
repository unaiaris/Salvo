import Link from "next/link";

/**
 * The entry page holds no data on purpose, which is why it stays static in the build while every
 * screen that reads the API is dynamic.
 */
export default function Home() {
  return (
    <div className="flex flex-col gap-6">
      <p className="text-sm font-semibold uppercase tracking-[0.24em] text-slate-500">
        Consola antifraude
      </p>
      <h1 className="max-w-3xl text-4xl font-semibold tracking-tight text-slate-950">
        Salvo concentra el riesgo antifraude en un núcleo .NET auditable.
      </h1>
      <p className="max-w-2xl text-lg leading-8 text-slate-600">
        Las reglas, el scoring y las transacciones viven en el backend. Esta interfaz lee un contrato
        OpenAPI y no calcula riesgo por su cuenta.
      </p>
      <p>
        <Link
          href="/alerts"
          className="inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          Ir a la cola de alertas
        </Link>
      </p>
    </div>
  );
}
