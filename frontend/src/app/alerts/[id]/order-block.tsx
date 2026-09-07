import type { AlertOrder, Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/** The order the alert is about. Buyer and device are pseudonymous references, never identities. */
export function OrderBlock({
  order,
  language,
}: {
  readonly order: AlertOrder;
  readonly language: Language;
}) {
  const f = formatting(language);
  const { t } = f;
  const rows = [
    { label: t.alertDetail.orderReference, value: order.merchantReferenceId },
    { label: t.alertDetail.orderBuyer, value: order.buyerReferenceId },
    { label: t.alertDetail.orderAmount, value: f.formatAmount(order.amountCents, order.currencyCode) },
    { label: t.alertDetail.orderOccurred, value: f.formatInstant(order.occurredAt) },
    {
      label: t.alertDetail.orderOrigin,
      value: order.city === null ? order.countryCode : `${order.city} (${order.countryCode})`,
    },
    {
      label: t.alertDetail.orderDeviceSession,
      value: order.deviceSessionId ?? t.alertDetail.orderDeviceSessionAbsent,
    },
  ];

  return (
    <section
      aria-labelledby="order-title"
      className="rounded-lg border border-slate-200 bg-white p-5"
    >
      <h2 id="order-title" className="text-lg font-semibold text-slate-900">
        {t.alertDetail.orderTitle}
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
