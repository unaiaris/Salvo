import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import { Provenance } from "@/components/provenance";
import {
  deploymentLanguage,
  fetchCapabilities,
  fetchDashboard,
  fetchEvaluationMetrics,
  languageOf,
} from "@/lib/api/console";
import type { Dashboard, Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";
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

export async function generateMetadata() {
  return { title: formatting(await deploymentLanguage()).t.meta.dashboard };
}

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
  const language = languageOf(capabilities);
  const f = formatting(language);

  if (!dashboard.ok) {
    return (
      <div className="flex flex-col gap-6">
        <Heading language={language} />
        <FailureNotice failure={dashboard.failure} language={language} />
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
      <Heading language={language} />

      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <Provenance run={state.scoringRun} language={language} />
        {state.scoringRun !== null && (
          <p className="text-sm text-slate-600">
            {f.t.dashboard.ordersInRun(f.formatCount(state.scoringRun.orderCount))}
          </p>
        )}
      </div>

      {state.ordersPendingScoring > 0 && state.scoringRun !== null && (
        <PendingOrdersNotice count={state.ordersPendingScoring} language={language} />
      )}

      {!capabilities.ok && <FailureNotice failure={capabilities.failure} language={language} />}

      <DashboardBody state={state} language={language} />

      {metrics !== null && <QualitySection metrics={metrics} language={language} />}
    </div>
  );
}

function Heading({ language }: { readonly language: Language }) {
  const { t } = formatting(language);

  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-3xl font-semibold tracking-tight text-slate-950">{t.dashboard.title}</h1>
      <p className="max-w-3xl text-sm leading-6 text-slate-600">{t.dashboard.lead}</p>
    </div>
  );
}

/**
 * The three empty states, told apart without a second request: with no run every order counts as
 * pending, so `ordersPendingScoring` is what separates an empty base from an unscored one.
 */
function DashboardBody({
  state,
  language,
}: {
  readonly state: Dashboard;
  readonly language: Language;
}) {
  const { t } = formatting(language);

  if (state.scoringRun === null) {
    return state.ordersPendingScoring === 0 ? (
      <NoOrdersDashboard language={language} />
    ) : (
      <NoScoringRunDashboard
        ordersPendingScoring={state.ordersPendingScoring}
        language={language}
      />
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {state.openAlerts.total === 0 && (
        <NoOpenAlertsNotice run={state.scoringRun} language={language} />
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <Panel id="open-alerts" title={t.dashboard.openAlertsTitle} hint={t.dashboard.openAlertsHint}>
          <OpenAlertsPanel openAlerts={state.openAlerts} language={language} />
        </Panel>
        <Panel
          id="amount-at-risk"
          title={t.dashboard.amountAtRiskTitle}
          hint={t.dashboard.amountAtRiskHint}
        >
          <AmountAtRiskPanel rows={state.amountAtRisk} language={language} />
        </Panel>
        <Panel
          id="reported-fraud"
          title={t.dashboard.reportedFraudTitle}
          hint={t.dashboard.reportedFraudHint}
        >
          <ReportedFraudPanel rows={state.reportedFraud} language={language} />
        </Panel>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Panel id="flag-rate" title={t.dashboard.flagRateTitle}>
          <FlagRatePanel
            flagRate={state.flagRate}
            scoredOrders={state.scoringRun.orderCount}
            language={language}
          />
        </Panel>
        <div className="lg:col-span-2">
          <Panel
            id="top-signals"
            title={t.dashboard.topSignalsTitle}
            hint={t.dashboard.topSignalsHint}
          >
            <TopSignalsPanel signals={state.topSignals} language={language} />
          </Panel>
        </div>
      </div>

      <Panel id="denials" title={t.dashboard.denialsTitle} hint={t.dashboard.denialsHint}>
        <ExternalDenialsPanel
          denials={state.externalDenialsWithoutAlert}
          language={language}
        />
      </Panel>

      <Panel
        id="risk-over-time"
        title={t.dashboard.riskOverTimeTitle}
        hint={t.dashboard.riskOverTimeHint}
      >
        {state.riskOverTime.length === 0 ? (
          <p className="text-sm text-slate-600">{t.dashboard.riskOverTimeNone}</p>
        ) : (
          <RiskOverTimeChart buckets={state.riskOverTime} language={language} />
        )}
      </Panel>
    </div>
  );
}

/**
 * Orders the current run does not cover. Without this the dashboard would present a stale run as if
 * it were the state of the corpus, which is precisely what an import after a run produces.
 */
function PendingOrdersNotice({
  count,
  language,
}: {
  readonly count: number;
  readonly language: Language;
}) {
  const f = formatting(language);

  return (
    <section
      role="status"
      aria-labelledby="pending-orders"
      className="rounded-lg border border-amber-300 bg-amber-50 p-4 text-amber-950"
    >
      <h2 id="pending-orders" className="text-sm font-semibold">
        {f.t.dashboard.pendingTitle(count, f.formatCount(count))}
      </h2>
      <p className="mt-1 text-sm leading-6">{f.t.dashboard.pendingBody}</p>
      <p className="mt-2 text-sm">
        <Link
          href="/import"
          className="font-semibold underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-900"
        >
          {f.t.common.runScoring}
        </Link>
      </p>
    </section>
  );
}
