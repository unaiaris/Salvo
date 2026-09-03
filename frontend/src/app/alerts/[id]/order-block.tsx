import type { AlertOrder } from "@/lib/api/contract";
import { formatAmount, formatInstant } from "@/lib/format";

/** The order the alert is about. Buyer and device are pseudonymous references, never identities. */
export function OrderBlock({ order }: { readonly order: AlertOrder }) {
  const rows = [
    { label: "Referencia del comercio", value: order.merchantReferenceId },
    { label: "Comprador", value: order.buyerReferenceId },
    { label: "Monto", value: formatAmount(order.amountCents, order.currencyCode) },
    { label: "Ocurrió", value: formatInstant(order.occurredAt) },
    {
      label: "Origen",
      value: order.city === null ? order.countryCode : `${order.city} (${order.countryCode})`,
    },
    { label: "Sesión de dispositivo", value: order.deviceSessionId ?? "sin registrar" },
  ];

  return (
    <section
      aria-labelledby="order-title"
      className="rounded-lg border border-slate-200 bg-white p-5"
    >
      <h2 id="order-title" className="text-lg font-semibold text-slate-900">
        Pedido
      </h2>
      <dl className="mt-3 grid gap-x-8 gap-y-3 sm:grid-cols-2">
        {rows.map((row) => (
          <div key={row.label}>
            <dt className="text-xs uppercase tracking-wide text-slate-500">{row.label}</dt>
            <dd className="mt-0.5 text-sm text-slate-900">{row.value}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
