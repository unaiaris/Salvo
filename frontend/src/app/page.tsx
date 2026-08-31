export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen max-w-5xl flex-col justify-center gap-6 px-6 py-16">
      <p className="text-sm font-semibold uppercase tracking-[0.24em] text-slate-500">
        Fundaciones reproducibles
      </p>
      <h1 className="max-w-3xl text-5xl font-semibold tracking-tight text-slate-950">
        Salvo concentra el riesgo antifraude en un núcleo .NET auditable.
      </h1>
      <p className="max-w-2xl text-lg leading-8 text-slate-600">
        La interfaz Next.js consume un contrato OpenAPI. Las reglas, transacciones e integraciones
        permanecen en el backend y todavía no hay lógica de fraude en esta etapa.
      </p>
      <section
        aria-label="Estado de la fundación"
        className="w-fit rounded-full border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800"
      >
        API base y cliente web preparados
      </section>
    </main>
  );
}
