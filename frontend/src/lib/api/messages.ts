import type { ApiFailure } from "./failures";

/**
 * What the analyst reads when something fails, and what she can do next.
 *
 * Two rules the design fixes and this module enforces. First, the main text is the console's own,
 * in Spanish and actionable: "Error 409" is a fact about HTTP, not about the alert, and the API's
 * `detail` is written for whoever integrates against it. `detail` still travels, as secondary
 * information, because it is often the only thing that says *which* value was rejected. Second,
 * every code the API can emit has its own text: a shared "algo salió mal" would hide precisely the
 * difference between "otra persona ya emitió un veredicto" and "el corpus cambió bajo tus pies".
 */
export interface FailureMessage {
  /** One line, in the analyst's language, naming what happened. */
  readonly title: string;
  /** What it means for the alert in front of her. */
  readonly body: string;
  /** The way forward. */
  readonly recovery: string;
  /** Whether the message belongs beside a form field rather than at the top of the page. */
  readonly isFormError: boolean;
}

const RELOAD = "Recargá la alerta para ver el estado registrado.";

const BY_CODE: Readonly<Record<string, FailureMessage>> = {
  ALERT_NOT_FOUND: {
    title: "Esta alerta ya no existe",
    body: "La alerta que pediste no está en la base. Puede haberse importado un corpus nuevo desde que abriste el enlace.",
    recovery: "Volvé al feed de alertas y elegí una de la cola vigente.",
    isFormError: false,
  },
  INVALID_STATUS: {
    title: "El veredicto no es válido",
    body: "Solo se puede marcar una alerta como «Segura» o «Fraude reportado».",
    recovery: "Elegí uno de los dos veredictos y volvé a enviar.",
    isFormError: true,
  },
  NOTE_TOO_LONG: {
    title: "La nota es demasiado larga",
    body: "La nota de revisión admite hasta 2000 caracteres.",
    recovery: "Recortá la nota y volvé a enviar. Lo que escribiste sigue en el campo.",
    isFormError: true,
  },
  ALERT_ALREADY_REVIEWED: {
    title: "Otra persona ya revisó esta alerta",
    body: "La alerta quedó cerrada con un veredicto distinto del que enviaste. Un veredicto es definitivo: no se reabre.",
    recovery: `${RELOAD} Si el criterio cambió, hace falta una alerta nueva sobre el pedido.`,
    isFormError: false,
  },
  ALERT_REVIEW_NOTE_CONFLICT: {
    title: "La alerta ya tiene este veredicto, con otra nota",
    body: "El veredicto registrado coincide con el que enviaste, pero la nota guardada es distinta y no se sobrescribe.",
    recovery: `${RELOAD} La nota registrada aparece en el bloque de revisión.`,
    isFormError: false,
  },
  ALERT_DIVERGENCE_NOT_ACKNOWLEDGED: {
    title: "El corpus cambió desde que se abrió la alerta",
    body: "La evaluación vigente del pedido está en otra banda de severidad que la del snapshot con el que se abrió la alerta. La API no acepta un veredicto sin que lo reconozcas.",
    recovery: "Volvé al aviso de divergencia, leé qué cambió y marcá la casilla antes de enviar.",
    isFormError: false,
  },
  ALERT_REVIEW_CONFLICT: {
    title: "Dos revisiones al mismo tiempo",
    body: "Otra revisión sobre esta alerta se guardó mientras se procesaba la tuya, así que la tuya no se aplicó.",
    recovery: `${RELOAD} Si seguís con el mismo criterio, volvé a enviarlo.`,
    isFormError: false,
  },
  INVALID_SORT: {
    title: "La consola pidió un orden que la API no reconoce",
    body: "El feed pidió ordenar las alertas por un valor que el contrato no admite. Es un error interno: no debería ocurrir desde esta pantalla.",
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
  INVALID_SEVERITY: {
    title: "La consola pidió una severidad que la API no reconoce",
    body: "El feed filtró por una severidad que el contrato no admite. Es un error interno: no debería ocurrir desde esta pantalla.",
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
  INVALID_PAGE: {
    title: "La consola pidió una página inexistente",
    body: "El feed pidió un número de página fuera de rango. Es un error interno: no debería ocurrir desde esta pantalla.",
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
  INVALID_PAGE_SIZE: {
    title: "La consola pidió un tamaño de página fuera de rango",
    body: "El feed pidió más alertas por página de las que la API entrega. Es un error interno: no debería ocurrir desde esta pantalla.",
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
};

export function isKnownFailureCode(code: string): boolean {
  return Object.hasOwn(BY_CODE, code);
}

export function knownFailureCodes(): readonly string[] {
  return Object.keys(BY_CODE);
}

export function describeFailure(failure: ApiFailure): FailureMessage {
  switch (failure.kind) {
    case "timeout":
      return {
        title: "La API tardó demasiado en responder",
        body: "La consulta se canceló para no dejar la pantalla colgada. No se llegó a leer ni a guardar nada.",
        recovery: "Volvé a intentarlo. Si persiste, revisá que el proceso de la API esté respondiendo.",
        isFormError: false,
      };
    case "unreachable":
      return {
        title: "No se pudo contactar a la API",
        body: "La consola no tiene con qué trabajar: todo el riesgo se calcula en el backend y ahora mismo no responde.",
        recovery: "Verificá que la API esté levantada en la dirección configurada y recargá.",
        isFormError: false,
      };
    case "malformed":
      return {
        title: "La respuesta de la API no coincide con el contrato",
        body: "Llegó una respuesta que a esta consola le falta o le sobra algo respecto del contrato con el que se construyó. No se muestra nada antes que mostrar algo mal leído.",
        recovery: "Es probable que la API y la consola estén en versiones distintas. Regenerá los tipos desde OpenAPI y volvé a desplegar.",
        isFormError: false,
      };
    case "problem":
      return describeProblem(failure);
  }
}

function describeProblem(failure: Extract<ApiFailure, { kind: "problem" }>): FailureMessage {
  const known = failure.code === null ? undefined : BY_CODE[failure.code];
  if (known !== undefined) {
    return known;
  }

  return {
    title: "La API rechazó la operación",
    body: `Respondió ${String(failure.status)} con un motivo que esta consola todavía no traduce.`,
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta y el detalle técnico.",
    isFormError: false,
  };
}
