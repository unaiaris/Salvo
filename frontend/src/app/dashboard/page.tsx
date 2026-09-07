import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import { Provenance } from "@/components/provenance";
import { fetchCapabilities, fetchDashboard, fetchEvaluationMetrics } from "@/lib/api/console";
import type { Dashboard } from "@/lib/api/contract";
import { formatCount } from "@/lib/format";
import {
  NoOpenAlertsNotice,
  NoOrdersDashboard,
  NoScoringRunDashboard,
} from "./empty-states";
import {
  AmountAtRiskPanel,
  ExternalDenialsPanel,
  FlagRatePanel,
  OpenAlertsPanel,
  Panel,
  ReportedFraudPanel,
  TopSignalsPanel,
} from "./panels";
import { QualitySection } from "./quality-section";
import { RiskOverTimeChart } from "./risk-chart";

export const dynamic = "force-dynamic";

export const metadata = { title: "Dashboard · Salvo" };

/**
 * The operational state of the current scoring run.
 *
 * Every figure on this page comes from the deterministic evaluations of that run and from the
 * verdicts an analyst recorded. None of them reads `OrderEvaluationLabel`, by any path — that is
 * decision 37, and the reason is that outside a demo corpus the ground truth simply does not exist.
 * The quality of the criterion is measured further down, in its own section, only where the API
 * declares the deployment a demo.
 */
export default async function DashboardPage() {
  const [capabilities, dashboard] = await Promise.all([fetchCapabilities(), fetchDashboard()]);

  if (!dashboard.ok) {
    return (
      <div className="flex flex-col gap-6">
        <Heading />
        <FailureNotice failure={dashboard.failure} />
      </div>
    );
  }

  const state = dashboard.value;
  // Asked for only where it exists. Probing the route otherwise would turn a deliberate
  // configuration into a 404 the console would then have to interpret.
  const demoEnabled = capabilities.ok && capabilities.value.demoDataEnabled;
  const metrics = demoEnabled ? await fetchEvaluationMetrics() : null;

  return (
    <div className="flex flex-col gap-6">
      <Heading />

      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <Provenance run={state.scoringRun} />
        {state.scoringRun !== null && (
          <p className="text-sm text-slate-600">
            {formatCount(state.scoringRun.orderCount)} pedidos en la corrida
          </p>
        )}
      </div>

      {state.ordersPendingScoring > 0 && state.scoringRun !== null && (
        <PendingOrdersNotice count={state.ordersPendingScoring} />
      )}

      {!capabilities.ok && <FailureNotice failure={capabilities.failure} />}

      <DashboardBody state={state} />

      {metrics !== null && <QualitySection metrics={metrics} />}
    </div>
  );
}

function Heading() {
  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-3xl font-semibold tracking-tight text-slate-950">Dashboard</h1>
      <p className="max-w-3xl text-sm leading-6 text-slate-600">
        Estado operativo de la corrida vigente. Los montos se informan por moneda y nunca se suman
        entre ellas. «Fraude reportado» es lo que decidió una analista, no la verdad de campo.
      </p>
    </div>
  );
}

/**
 * The three empty states, told apart without a second request: with no run every order counts as
 * pending, so `ordersPendingScoring` is what separates an empty base from an unscored one.
 */
function DashboardBody({ state }: { readonly state: Dashboard }) {
  if (state.scoringRun === null) {
    return state.ordersPendingScoring === 0 ? (
      <NoOrdersDashboard />
    ) : (
      <NoScoringRunDashboard ordersPendingScoring={state.ordersPendingScoring} />
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {state.openAlerts.total === 0 && <NoOpenAlertsNotice run={state.scoringRun} />}

      <div className="grid gap-6 lg:grid-cols-3">
        <Panel title="Alertas abiertas" hint="Pendientes de veredicto, por banda de severidad.">
          <OpenAlertsPanel openAlerts={state.openAlerts} />
        </Panel>
        <Panel
          title="Monto en riesgo"
          hint="Importe de los pedidos con alerta abierta. Una fila por moneda: no existe un total."
        >
          <AmountAtRiskPanel rows={state.amountAtRisk} />
        </Panel>
        <Panel
          title="Fraude reportado"
          hint="Veredictos de la analista, agregados por pedido distinto. También por moneda."
        >
          <ReportedFraudPanel rows={state.reportedFraud} />
        </Panel>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Panel title="Tasa de marcado">
          <FlagRatePanel flagRate={state.flagRate} scoredOrders={state.scoringRun.orderCount} />
        </Panel>
        <div className="lg:col-span-2">
          <Panel
            title="Señales principales"
            hint="Reglas presentes en el snapshot de las alertas abiertas, no en todas las evaluaciones."
          >
            <TopSignalsPanel signals={state.topSignals} />
          </Panel>
        </div>
      </div>

      <Panel
        title="Denegados por el proveedor sin alerta local"
        hint="Pedidos que el proveedor externo denegó y que el motor local nunca marcó. Es la única pantalla donde aparece un pedido sin alerta."
      >
        <ExternalDenialsPanel denials={state.externalDenialsWithoutAlert} />
      </Panel>

      <Panel
        title="Riesgo en el tiempo"
        hint="Por semana de ocurrencia del pedido, en hora de Montevideo: la misma zona con la que las reglas deciden a qué día pertenece cada pedido."
      >
        {state.riskOverTime.length === 0 ? (
          <p className="text-sm text-slate-600">
            La corrida vigente no cubrió ningún pedido, así que no hay semanas que dibujar.
          </p>
        ) : (
          <RiskOverTimeChart buckets={state.riskOverTime} />
        )}
      </Panel>
    </div>
  );
}

/**
 * Orders the current run does not cover. Without this the dashboard would present a stale run as if
 * it were the state of the corpus, which is precisely what an import after a run produces.
 */
function PendingOrdersNotice({ count }: { readonly count: number }) {
  return (
    <section
      role="status"
      aria-labelledby="pending-orders"
      className="rounded-lg border border-amber-300 bg-amber-50 p-4 text-amber-950"
    >
      <h2 id="pending-orders" className="text-sm font-semibold">
        {count === 1
          ? "Hay 1 pedido fuera de la corrida vigente"
          : `Hay ${formatCount(count)} pedidos fuera de la corrida vigente`}
      </h2>
      <p className="mt-1 text-sm leading-6">
        No están puntuados, así que no cuentan en ninguna de las cifras de abajo y no pueden abrir
        alertas.
      </p>
      <p className="mt-2 text-sm">
        <Link
          href="/import"
          className="font-semibold underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-900"
        >
          Ejecutar scoring
        </Link>
      </p>
    </section>
  );
}
