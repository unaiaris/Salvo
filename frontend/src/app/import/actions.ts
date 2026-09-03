"use server";

import { revalidatePath } from "next/cache";
import { importOrders, runScoring, seedDemoOrders } from "@/lib/api/console";
import { IMPORT_FORMATS, type ImportFormat, type ImportRecordError } from "@/lib/api/contract";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { formatCount, formatInstant, importErrorLabel } from "@/lib/format";
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

function failed(failure: ApiFailure, submissionId: number): ActionState {
  const message = describeFailure(failure);

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
  const result = await seedDemoOrders();

  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidateConsole();
  const seed = result.value;
  const inserted = seed.insertedOrders;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title: inserted === 0 ? "El corpus de demostración ya estaba cargado" : "Corpus cargado",
    body:
      inserted === 0
        ? "La carga es idempotente: los pedidos ya estaban en la base y no se duplicó ninguno."
        : "Los pedidos quedaron en la base. Todavía no tienen evaluación ni alerta.",
    recovery: "Ejecutá una corrida de scoring para puntuarlos.",
    technicalDetail: "",
    facts: [
      `Versión de la fixture: ${seed.datasetVersion}`,
      `Pedidos insertados: ${formatCount(inserted)} de ${formatCount(seed.totalOrders)}`,
      `Pedidos ya presentes: ${formatCount(seed.duplicateOrders)}`,
      `Etiquetas insertadas: ${formatCount(seed.insertedLabels)} de ${formatCount(seed.totalLabels)}`,
    ],
    submissionId,
  };
}

export async function importOrderFile(
  previous: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const file = formData.get("file");
  const format = readFormat(formData.get("format"));

  // The file input can come back empty, and an empty `File` would travel to the API as a zero-byte
  // upload. Answering here uses the same catalogue entry the API would have produced, without the
  // round trip.
  if (!(file instanceof File) || file.size === 0) {
    return failed({ kind: "problem", status: 400, code: "FILE_REQUIRED", detail: null }, submissionId);
  }

  if (format === null) {
    return failed(
      { kind: "problem", status: 415, code: "UNSUPPORTED_FORMAT", detail: null },
      submissionId,
    );
  }

  const result = await importOrders(file, format);

  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidateConsole();
  const imported = result.value;

  return {
    outcome: "done",
    title:
      imported.importedCount === 0
        ? "No se importó ningún pedido"
        : `Se importaron ${formatCount(imported.importedCount)} pedidos`,
    body:
      "La importación es estricta por registro y atómica por archivo: los pedidos válidos se "
      + "escribieron todos juntos y los rechazados no se escribieron nunca.",
    recovery: "Los pedidos nuevos no tienen evaluación hasta que ejecutes una corrida de scoring.",
    technicalDetail: "",
    facts: [
      `Registros leídos: ${formatCount(imported.totalRecords)}`,
      `Importados: ${formatCount(imported.importedCount)}`,
      `Duplicados, ya presentes con los mismos datos: ${formatCount(imported.duplicateCount)}`,
      `Rechazados: ${formatCount(imported.invalidRecordCount)}`,
    ],
    recordErrors: imported.errors.map(describeRecordError),
    errorsTruncated: imported.errorsTruncated,
    submissionId,
  };
}

export async function executeScoringRun(previous: ActionState): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const result = await runScoring();

  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidateConsole();
  const run = result.value;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title: `Corrida #${formatCount(run.sequence)} completada`,
    body:
      "Ya existe una evaluación vigente por pedido y las alertas que correspondían quedaron "
      + "abiertas. Una evaluación reusada es una cuyo resultado no cambió: mismo corpus, misma "
      + "configuración, mismo resultado.",
    recovery: "Revisá la cola de alertas o mirá el dashboard.",
    technicalDetail: "",
    facts: [
      `Configuración de reglas: ${run.ruleConfigVersion}`,
      `Terminó: ${formatInstant(run.completedAt)}`,
      `Pedidos evaluados: ${formatCount(run.orderCount)}`,
      `Evaluaciones creadas: ${formatCount(run.evaluationsCreated)}`,
      `Evaluaciones reusadas: ${formatCount(run.evaluationsReused)}`,
      `Alertas abiertas: ${formatCount(run.alertsCreated)}`,
      `Omitidas por tener ya una alerta abierta: ${formatCount(run.alertsSkippedOpen)}`,
      `Omitidas por tener ya un veredicto: ${formatCount(run.alertsSkippedReviewed)}`,
    ],
    submissionId,
  };
}

/**
 * One rejected record as a sentence.
 *
 * Written here rather than in the component because the wording is the server's: the code is a
 * domain value from the importer and the analyst reads a translation of it, never the identifier.
 */
function describeRecordError(error: ImportRecordError): string {
  const where =
    error.lineNumber === null
      ? `Registro ${formatCount(error.recordNumber)}`
      : `Registro ${formatCount(error.recordNumber)}, línea ${formatCount(error.lineNumber)}`;
  const field = error.field === null ? "" : ` · campo ${error.field}`;

  return `${where}${field} · ${importErrorLabel(error.code)}: ${error.message}`;
}

function readFormat(value: FormDataEntryValue | null): ImportFormat | null {
  return IMPORT_FORMATS.find((format) => format === value) ?? null;
}
