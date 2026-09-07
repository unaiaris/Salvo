"use server";

import { revalidatePath } from "next/cache";
import { deploymentLanguage, importOrders, runScoring, seedDemoOrders } from "@/lib/api/console";
import {
  deliverExternalCallbacks,
  requestCorpusExternalEvaluations,
} from "@/lib/api/external";
import {
  IMPORT_FORMATS,
  type ImportFormat,
  type ImportRecordError,
  type Language,
} from "@/lib/api/contract";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { formatting, type Formatting } from "@/lib/format";
import { type ActionState, INITIAL_ACTION_STATE } from "./action-state";

/**
 * The three things an analyst can do to the corpus, and none of them is optimistic.
 *
 * Each action posts, revalidates every screen whose reading of the corpus just changed, and reports
 * what the API actually did. Importing and scoring are separate on purpose and the wording says so:
 * an import that writes three hundred orders produces no evaluation and no alert until somebody
 * runs the scoring.
 */

/** Every screen that reads the corpus. All three actions change what all three would show. */
function revalidateConsole(): void {
  revalidatePath("/import");
  revalidatePath("/dashboard");
  revalidatePath("/alerts");
}

function failed(failure: ApiFailure, language: Language, submissionId: number): ActionState {
  const message = describeFailure(failure, language);

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "failed",
    title: message.title,
    body: message.body,
    recovery: message.recovery,
    technicalDetail: failure.kind === "problem" ? (failure.detail ?? "") : "",
    submissionId,
  };
}

export async function seedDemoCorpus(previous: ActionState): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const language = await deploymentLanguage();
  const f = formatting(language);
  const { outcomes } = f.t;
  const result = await seedDemoOrders();

  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidateConsole();
  const seed = result.value;
  const inserted = seed.insertedOrders;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title: inserted === 0 ? outcomes.seedAlreadyLoadedTitle : outcomes.seedLoadedTitle,
    body: inserted === 0 ? outcomes.seedAlreadyLoadedBody : outcomes.seedLoadedBody,
    recovery: outcomes.seedRecovery,
    technicalDetail: "",
    facts: [
      outcomes.seedFactVersion(seed.datasetVersion),
      outcomes.seedFactInserted(f.formatCount(inserted), f.formatCount(seed.totalOrders)),
      outcomes.seedFactDuplicates(f.formatCount(seed.duplicateOrders)),
      outcomes.seedFactLabels(
        f.formatCount(seed.insertedLabels),
        f.formatCount(seed.totalLabels),
      ),
    ],
    submissionId,
  };
}

export async function importOrderFile(
  previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const language = await deploymentLanguage();
  const f = formatting(language);
  const { outcomes } = f.t;
  const file = formData.get("file");
  const format = readFormat(formData.get("format"));

  // The file input can come back empty, and an empty `File` would travel to the API as a zero-byte
  // upload. Answering here uses the same catalogue entry the API would have produced, without the
  // round trip.
  if (!(file instanceof File) || file.size === 0) {
    return failed(
      { kind: "problem", status: 400, code: "FILE_REQUIRED", detail: null },
      language,
      submissionId,
    );
  }

  if (format === null) {
    return failed(
      { kind: "problem", status: 415, code: "UNSUPPORTED_FORMAT", detail: null },
      language,
      submissionId,
    );
  }

  const result = await importOrders(file, format);

  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidateConsole();
  const imported = result.value;

  return {
    outcome: "done",
    title:
      imported.importedCount === 0
        ? outcomes.importNoneTitle
        : outcomes.importSomeTitle(f.formatCount(imported.importedCount)),
    body: outcomes.importBody,
    recovery: outcomes.importRecovery,
    technicalDetail: "",
    facts: [
      outcomes.importFactRead(f.formatCount(imported.totalRecords)),
      outcomes.importFactImported(f.formatCount(imported.importedCount)),
      outcomes.importFactDuplicates(f.formatCount(imported.duplicateCount)),
      outcomes.importFactRejected(f.formatCount(imported.invalidRecordCount)),
    ],
    recordErrors: imported.errors.map((error) => describeRecordError(error, f)),
    errorsTruncated: imported.errorsTruncated,
    submissionId,
  };
}

export async function executeScoringRun(previous: ActionState): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const language = await deploymentLanguage();
  const f = formatting(language);
  const { outcomes } = f.t;
  const result = await runScoring();

  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidateConsole();
  const run = result.value;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title: outcomes.scoringTitle(f.formatCount(run.sequence)),
    body: outcomes.scoringBody,
    recovery: outcomes.scoringRecovery,
    technicalDetail: "",
    facts: [
      outcomes.scoringFactConfig(run.ruleConfigVersion),
      outcomes.scoringFactFinished(f.formatInstant(run.completedAt)),
      outcomes.scoringFactOrders(f.formatCount(run.orderCount)),
      outcomes.scoringFactCreated(f.formatCount(run.evaluationsCreated)),
      outcomes.scoringFactReused(f.formatCount(run.evaluationsReused)),
      outcomes.scoringFactAlerts(f.formatCount(run.alertsCreated)),
      outcomes.scoringFactSkippedOpen(f.formatCount(run.alertsSkippedOpen)),
      outcomes.scoringFactSkippedReviewed(f.formatCount(run.alertsSkippedReviewed)),
    ],
    submissionId,
  };
}

/**
 * Asks the provider about every order it has never seen.
 *
 * This is the only place the orders without an alert can get an external opinion: the alert detail
 * only reaches the ones that produced an alert, and stage 6 deliberately adds no orders screen. It
 * lives here, behind the demo flag, for the same reason the seed does.
 */
export async function requestCorpusExternal(previous: ActionState): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const language = await deploymentLanguage();
  const f = formatting(language);
  const { outcomes } = f.t;
  const result = await requestCorpusExternalEvaluations();

  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidateConsole();
  const summary = result.value;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title:
      summary.requested === 0
        ? outcomes.corpusExternalKnownTitle
        : outcomes.corpusExternalRequestedTitle(f.formatCount(summary.requested)),
    body: outcomes.corpusExternalBody,
    recovery:
      summary.stillPending === 0
        ? outcomes.corpusExternalSettledRecovery
        : outcomes.corpusExternalPendingRecovery,
    facts: [
      outcomes.corpusExternalFactExamined(f.formatCount(summary.examined)),
      outcomes.corpusExternalFactRequested(f.formatCount(summary.requested)),
      outcomes.corpusExternalFactSettled(f.formatCount(summary.settled)),
      outcomes.corpusExternalFactPending(f.formatCount(summary.stillPending)),
      outcomes.corpusExternalFactSkipped(f.formatCount(summary.skipped)),
    ],
    submissionId,
  };
}

/**
 * Delivers the callback of every evaluation still waiting for the provider.
 *
 * The console never composes a callback and never holds the shared secret: it asks the API, and the
 * API asks the provider what it would have sent. Repeating it is harmless by construction — the
 * deduplication key of a redelivered message is the same one, so the second press is recorded as a
 * replay instead of doing anything twice.
 */
export async function deliverAllCallbacks(previous: ActionState): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const language = await deploymentLanguage();
  const f = formatting(language);
  const { outcomes } = f.t;
  const result = await deliverExternalCallbacks(null);

  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidateConsole();
  const delivery = result.value;

  // A delivery where every message was already recorded is a replay, not a second round of effects,
  // and reporting it as "delivered N" would claim work that deliberately did not happen.
  const allReplayed = delivery.delivered > 0 && delivery.replayed === delivery.delivered;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title:
      delivery.examined === 0
        ? outcomes.deliverNoneTitle
        : allReplayed
          ? outcomes.deliverReplayedTitle(f.formatCount(delivery.delivered))
          : outcomes.deliverDoneTitle(f.formatCount(delivery.delivered)),
    body: allReplayed ? outcomes.deliverReplayedBody : outcomes.deliverDoneBody,
    recovery:
      delivery.examined === 0
        ? outcomes.deliverNoneRecovery
        : outcomes.deliverDoneRecovery,
    facts: [
      outcomes.deliverFactExamined(f.formatCount(delivery.examined)),
      outcomes.deliverFactDelivered(f.formatCount(delivery.delivered)),
      outcomes.deliverFactSettled(f.formatCount(delivery.settled)),
      outcomes.deliverFactReplayed(f.formatCount(delivery.replayed)),
      outcomes.deliverFactUnavailable(f.formatCount(delivery.unavailable)),
    ],
    submissionId,
  };
}

/**
 * One rejected record as a sentence.
 *
 * Written here rather than in the component because the wording is the server's: the code is a
 * domain value from the importer and the analyst reads a translation of it, never the identifier.
 *
 * <strong>`error.message` is the API's own, and it stays in English.</strong> It is the only part
 * of this sentence the console does not own: it names the value that was rejected, and it is the
 * technical detail rather than the explanation. The code beside it is what carries the meaning, and
 * that is translated — which is the checklist item «códigos de error de fila traducidos».
 */
function describeRecordError(error: ImportRecordError, f: Formatting): string {
  const { importPage } = f.t;
  const where =
    error.lineNumber === null
      ? importPage.recordAt(f.formatCount(error.recordNumber))
      : importPage.recordAtLine(
          f.formatCount(error.recordNumber),
          f.formatCount(error.lineNumber),
        );

  return importPage.recordError(
    where,
    error.field === null ? "" : importPage.recordField(error.field),
    f.importErrorLabel(error.code),
    error.message,
  );
}

function readFormat(value: FormDataEntryValue | null): ImportFormat | null {
  return IMPORT_FORMATS.find((format) => format === value) ?? null;
}
