"use server";

import { revalidatePath } from "next/cache";
import { importOrders, runScoring, seedDemoOrders } from "@/lib/api/console";
import {
  deliverExternalCallbacks,
  requestCorpusExternalEvaluations,
} from "@/lib/api/external";
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
 * Asks the provider about every order it has never seen.
 *
 * This is the only place the orders without an alert can get an external opinion: the alert detail
 * only reaches the ones that produced an alert, and stage 6 deliberately adds no orders screen. It
 * lives here, behind the demo flag, for the same reason the seed does.
 */
export async function requestCorpusExternal(previous: ActionState): Promise<ActionState> {
  const submissionId = previous.submissionId + 1;
  const result = await requestCorpusExternalEvaluations();

  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidateConsole();
  const summary = result.value;

  return {
    ...INITIAL_ACTION_STATE,
    outcome: "done",
    title:
      summary.requested === 0
        ? "El proveedor ya conocía todos los pedidos"
        : `Se consultaron ${formatCount(summary.requested)} pedidos`,
    body:
      "Cada pedido pasa por el mismo caso de uso que una consulta suelta: la fila se reserva antes "
      + "de llamar al proveedor, así que dos consultas simultáneas no crean dos evaluaciones del "
      + "lado del proveedor.",
    recovery:
      summary.stillPending === 0
        ? "El veredicto de cada pedido aparece en el detalle de su alerta."
        : "Los que siguen esperando al proveedor se cierran entregando sus callbacks o reconciliando.",
    facts: [
      `Pedidos sin evaluación externa: ${formatCount(summary.examined)}`,
      `Consultados: ${formatCount(summary.requested)}`,
      `Con veredicto en el acto: ${formatCount(summary.settled)}`,
      `Esperando al proveedor: ${formatCount(summary.stillPending)}`,
      `Omitidos, ya tenían evaluación: ${formatCount(summary.skipped)}`,
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
  const result = await deliverExternalCallbacks(null);

  if (!result.ok) {
    return failed(result.failure, submissionId);
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
        ? "No hay ninguna evaluación externa esperando al proveedor"
        : allReplayed
          ? `Los ${formatCount(delivery.delivered)} callbacks ya se habían recibido`
          : `Se entregaron ${formatCount(delivery.delivered)} callbacks`,
    body: allReplayed
      ? "Cada mensaje es idéntico a uno ya registrado, así que no se repitió ningún efecto: se anotó "
        + "que el proveedor los volvió a enviar y nada más."
      : "Los callbacks entran por el mismo caso de uso que usaría el proveedor: mismo recibo, misma "
        + "deduplicación, mismas reglas de transición. Quien pulsa elige qué evaluación, nunca qué "
        + "responde el proveedor.",
    recovery:
      delivery.examined === 0
        ? "Solicitá evaluaciones externas del corpus para que haya algo que entregar."
        : "El veredicto del proveedor aparece en el detalle de cada alerta.",
    facts: [
      `Evaluaciones esperando al proveedor: ${formatCount(delivery.examined)}`,
      `Callbacks entregados: ${formatCount(delivery.delivered)}`,
      `Cerraron con veredicto: ${formatCount(delivery.settled)}`,
      `Ya se habían recibido: ${formatCount(delivery.replayed)}`,
      `No se pudieron escribir por concurrencia: ${formatCount(delivery.unavailable)}`,
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
