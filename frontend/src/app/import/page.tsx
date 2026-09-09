import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import type { ApiResult } from "@/lib/api/failures";
import type { Language, SeedPreview } from "@/lib/api/contract";
import { Provenance } from "@/components/provenance";
import {
  deploymentLanguage,
  fetchCapabilities,
  fetchDashboard,
  fetchSeedPreview,
  languageOf,
} from "@/lib/api/console";
import { IMPORT_MAX_FILE_BYTES, SEED_CONFLICT } from "@/lib/api/contract";
import { formatting } from "@/lib/format";
import { ActionSection } from "./action-section";
import {
  DeliverCallbacksButton,
  RequestCorpusExternalButton,
  RunScoringButton,
  SeedDemoButton,
} from "./corpus-actions";
import { ImportForm } from "./import-form";

/**
 * Same reason as every other screen that reads the API: prerendering this route during `next build`
 * would run its fetches with nothing listening and freeze the result into static HTML.
 */
export const dynamic = "force-dynamic";

export async function generateMetadata() {
  return { title: formatting(await deploymentLanguage()).t.meta.import };
}

const MAX_FILE_MIB = IMPORT_MAX_FILE_BYTES / (1024 * 1024);

/**
 * Where the corpus comes from and where it gets processed.
 *
 * The page is built around the fact that those are two different steps. `POST /api/order-imports`
 * writes orders and produces nothing else; `POST /api/risk-evaluations:run` is what turns them into
 * evaluations and alerts. Stage 5's design calls this out as decision 39 because the earlier draft
 * of the interface had no screen that ran the scoring at all, which left an analyst importing a file
 * and then waiting forever for a queue that nothing was going to fill.
 */
export default async function ImportPage() {
  const [capabilities, dashboard] = await Promise.all([fetchCapabilities(), fetchDashboard()]);
  const language = languageOf(capabilities);
  const { t } = formatting(language);
  // Two questions, not one. Whether this deployment is a demonstration decides the provider
  // triggers below; whether it registers the seed route decides this section. The public instance
  // says yes to the first and no to the second, because its corpus arrives baked into the image.
  //
  // The preview is asked for only where the route exists, like the quality metrics of the
  // dashboard. It says what loading the corpus would do, so a database that cannot take it is
  // announced here rather than discovered by pressing the button.
  const seedEnabled = capabilities.ok && capabilities.value.demoSeedEnabled;
  const seedPreview = seedEnabled ? await fetchSeedPreview() : null;

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-semibold tracking-tight text-slate-950">
          {t.importPage.title}
        </h1>
        <p className="max-w-3xl text-sm leading-6 text-slate-600">{t.importPage.lead}</p>
      </div>

      {!capabilities.ok && <FailureNotice failure={capabilities.failure} language={language} />}

      {dashboard.ok ? (
        <CorpusStatus
          run={dashboard.value.scoringRun}
          ordersPendingScoring={dashboard.value.ordersPendingScoring}
          language={language}
        />
      ) : (
        <FailureNotice failure={dashboard.failure} language={language} />
      )}

      {seedEnabled && (
        <ActionSection
          id="seed"
          title={t.importPage.seedTitle}
          description={t.importPage.seedDescription}
        >
          <SeedConflictNotice preview={seedPreview} language={language} />
          <SeedDemoButton language={language} />
        </ActionSection>
      )}

      <ActionSection
        id="file"
        title={t.importPage.fileTitle}
        description={t.importPage.fileDescription}
      >
        <ImportForm maxFileMib={MAX_FILE_MIB} language={language} />
      </ActionSection>

      <ActionSection
        id="scoring"
        title={t.importPage.scoringTitle}
        description={t.importPage.scoringDescription}
      >
        <RunScoringButton language={language} />
      </ActionSection>

      {capabilities.ok && capabilities.value.externalCallbackTriggerEnabled && (
        <ActionSection
          id="external"
          title={t.importPage.externalTitle}
          description={t.importPage.externalDescription}
        >
          <div className="flex flex-col gap-6">
            <RequestCorpusExternalButton language={language} />
            <DeliverCallbacksButton language={language} />
          </div>
        </ActionSection>
      )}
    </div>
  );
}

/**
 * What loading the corpus would run into, said before the button is pressed.
 *
 * The two causes need different words. An earlier version of this same corpus cannot coexist with
 * the current one — an order is immutable and both versions use the same merchant references — so
 * the answer is a new database. Imported orders that happen to collide are somebody's file, and the
 * answer is to leave the corpus alone.
 *
 * A preview that failed renders nothing: it is a courtesy, and the load itself still refuses with
 * its own message. Announcing "we could not check" would be noise on a screen that has none.
 */
function SeedConflictNotice({
  preview,
  language,
}: {
  readonly preview: ApiResult<SeedPreview> | null;
  readonly language: Language;
}) {
  const { t } = formatting(language);

  if (preview === null || !preview.ok || preview.value.conflict === null) {
    return null;
  }

  const previous = preview.value.conflict === SEED_CONFLICT.previousCorpus;

  return (
    <section
      role="status"
      aria-labelledby="seed-conflict-title"
      className="rounded-md border-l-4 border-amber-500 bg-amber-50 p-4 text-sm leading-6 text-amber-950"
    >
      <h3 id="seed-conflict-title" className="font-semibold">
        {previous
          ? t.importPage.seedConflictPreviousTitle
          : t.importPage.seedConflictImportedTitle}
      </h3>
      <p className="mt-1">
        {previous
          ? t.importPage.seedConflictPreviousBody(preview.value.datasetVersion)
          : t.importPage.seedConflictImportedBody}
      </p>
    </section>
  );
}

/**
 * What the console is currently working from: which run, and how much of the corpus it does not
 * cover. `ordersPendingScoring` is the number that turns "the queue looks empty" into "nobody has
 * scored these yet", so it is stated here rather than left to be inferred.
 */
function CorpusStatus({
  run,
  ordersPendingScoring,
  language,
}: {
  readonly run: { readonly sequence: number; readonly completedAt: string } | null;
  readonly ordersPendingScoring: number;
  readonly language: Language;
}) {
  const f = formatting(language);
  const { t } = f;

  return (
    <section
      aria-labelledby="corpus-status"
      className="rounded-lg border border-slate-200 bg-white p-5"
    >
      <h2 id="corpus-status" className="text-base font-semibold text-slate-900">
        {t.importPage.corpusStatusTitle}
      </h2>
      <div className="mt-2">
        <Provenance run={run} language={language} />
      </div>
      {ordersPendingScoring > 0 ? (
        <p className="mt-2 text-sm font-medium leading-6 text-amber-900">
          {t.importPage.corpusPending(ordersPendingScoring, f.formatCount(ordersPendingScoring))}{" "}
          {t.importPage.corpusPendingHint}
        </p>
      ) : (
        <p className="mt-2 text-sm leading-6 text-slate-600">
          {run === null ? t.importPage.corpusEmpty : t.importPage.corpusCovered}
        </p>
      )}
      <p className="mt-3 text-sm">
        <Link
          href="/dashboard"
          className="font-medium text-slate-900 underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {t.common.viewDashboard}
        </Link>
      </p>
    </section>
  );
}
