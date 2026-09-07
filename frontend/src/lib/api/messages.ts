import { IMPORT_MAX_FILE_BYTES } from "./contract";
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

const IMPORT_MAX_FILE_MIB = IMPORT_MAX_FILE_BYTES / (1024 * 1024);

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
  SCORING_RUN_CONFLICT: {
    title: "Otra corrida de scoring se ejecutó al mismo tiempo",
    body: "Dos corridas escribieron estado en conflicto, así que la tuya no se guardó. El corpus quedó como estaba antes de intentarlo: no hay evaluaciones ni alertas a medio escribir.",
    recovery: "Volvé a ejecutar la corrida. Si alguien más está usando la consola, esperá a que termine.",
    isFormError: false,
  },
  METRICS_UNAVAILABLE: {
    title: "Todavía no se pueden calcular las métricas de calidad",
    body: "Medir el criterio exige una corrida de scoring y pedidos etiquetados a ambos lados de la división temporal. Falta alguna de las dos cosas.",
    recovery: "Ejecutá una corrida de scoring sobre el corpus de demostración desde la pantalla de importación.",
    isFormError: false,
  },
  DEMO_DATA_CONFLICT: {
    title: "El corpus de demostración choca con pedidos que ya existen",
    body: "La base tiene pedidos importados con las mismas referencias que la fixture pero con datos distintos. Un pedido es inmutable, así que la carga se cancela entera antes que pisar nada.",
    recovery: "Usá una base vacía para cargar la demo, o seguí con los pedidos que ya están importados.",
    isFormError: false,
  },
  DEMO_DATA_PREVIOUS_CORPUS: {
    title: "Esta base tiene una versión anterior del corpus de demostración",
    body: "Las dos versiones usan las mismas referencias de comercio y un pedido es inmutable, así que no pueden convivir. No se escribió nada: la base quedó como estaba.",
    recovery: "Cargá el corpus en una base nueva. La base actual no se pierde; deja de ser la de demostración.",
    isFormError: false,
  },
  FILE_REQUIRED: {
    title: "No llegó ningún archivo",
    body: "El formulario se envió sin archivo, o con uno de cero bytes.",
    recovery: "Elegí un archivo CSV o JSON con al menos un pedido y volvé a enviar.",
    isFormError: true,
  },
  FILE_TOO_LARGE: {
    title: "El archivo supera el máximo admitido",
    body: `La importación acepta hasta ${String(IMPORT_MAX_FILE_MIB)} MiB por archivo. Nada de lo que enviaste se importó.`,
    recovery: "Partí el archivo en varios más chicos e importalos de a uno.",
    isFormError: true,
  },
  TOO_MANY_RECORDS: {
    title: "El archivo tiene demasiados registros",
    body: "La importación acepta hasta 10.000 pedidos por archivo. Se rechaza el documento entero: no se importa una parte y se descarta el resto en silencio.",
    recovery: "Partí el archivo en tandas de hasta 10.000 registros.",
    isFormError: true,
  },
  UNSUPPORTED_MEDIA_TYPE: {
    title: "La consola envió el formulario en un formato que la API no acepta",
    body: "La importación viaja como `multipart/form-data`. Es un error interno: no debería ocurrir desde esta pantalla.",
    recovery: "Recargá la página y volvé a intentarlo. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
  UNSUPPORTED_FORMAT: {
    title: "Ese formato no está soportado",
    body: "La importación admite CSV o JSON, y hay que declarar cuál es antes de enviar.",
    recovery: "Elegí CSV o JSON según el archivo y volvé a enviar.",
    isFormError: true,
  },
  EMPTY_FILE: {
    title: "El archivo no tiene ningún pedido",
    body: "Se leyó completo y no contiene registros: puede ser un CSV con solo la fila de encabezados, o un JSON con una lista vacía.",
    recovery: "Revisá el archivo y volvé a enviarlo con al menos un pedido.",
    isFormError: true,
  },
  INVALID_ENCODING: {
    title: "El archivo no está en UTF-8",
    body: "La importación lee UTF-8 y el archivo trae bytes que no lo son. No se importó nada.",
    recovery: "Volvé a exportar el archivo en UTF-8 y reintentá.",
    isFormError: true,
  },
  INVALID_CSV: {
    title: "El CSV está mal formado",
    body: "La estructura del archivo no se pudo leer —comillas sin cerrar, o una fila con más campos que el encabezado—, así que no se importó ninguna fila.",
    recovery: "Corregí la estructura del archivo y volvé a enviarlo. El detalle técnico de abajo dice dónde falló.",
    isFormError: true,
  },
  INVALID_JSON: {
    title: "El JSON está mal formado",
    body: "El archivo no es JSON válido, así que no se importó ningún pedido.",
    recovery: "Validá el archivo y volvé a enviarlo. El detalle técnico de abajo dice dónde falló.",
    isFormError: true,
  },
  INVALID_JSON_ROOT: {
    title: "El JSON no es una lista de pedidos",
    body: "La importación espera un arreglo en la raíz del documento, con un objeto por pedido.",
    recovery: "Envolvé los pedidos en un arreglo `[ … ]` y volvé a enviar.",
    isFormError: true,
  },
  MISSING_HEADER: {
    title: "Al CSV le falta un encabezado obligatorio",
    body: "El archivo no tiene fila de encabezados, o le falta alguna de las columnas que la importación exige.",
    recovery: "Agregá la fila de encabezados con todas las columnas obligatorias. El detalle técnico dice cuál falta.",
    isFormError: true,
  },
  INVALID_HEADER: {
    title: "Un encabezado del CSV está vacío",
    body: "Una columna del archivo no tiene nombre, así que no se puede saber qué campo es.",
    recovery: "Nombrá todas las columnas del encabezado y volvé a enviar.",
    isFormError: true,
  },
  DUPLICATE_HEADER: {
    title: "El CSV repite un encabezado",
    body: "Dos columnas del archivo tienen el mismo nombre y no hay forma de decidir cuál gana.",
    recovery: "Dejá una sola columna por campo y volvé a enviar.",
    isFormError: true,
  },
  UNKNOWN_HEADER: {
    title: "El CSV trae una columna que la importación no conoce",
    body: "El archivo se rechaza entero antes que ignorar datos en silencio: una columna desconocida suele ser un archivo equivocado o un campo mal escrito.",
    recovery: "Quitá o corregí la columna. El detalle técnico dice cuál es.",
    isFormError: true,
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
  EXTERNAL_EVALUATION_PENDING: {
    title: "Ya hay una evaluación externa esperando al proveedor",
    body: "Este pedido tiene una evaluación externa que el proveedor todavía no respondió. Solo puede haber una a la vez: pedir otra crearía una segunda evaluación del lado del proveedor por una pregunta que ya está hecha.",
    recovery: "Esperá el callback del proveedor, o ejecutá una reconciliación para volver a preguntarle.",
    isFormError: false,
  },
  EXTERNAL_EVALUATION_SETTLED: {
    title: "El proveedor ya se pronunció sobre este pedido",
    body: "La evaluación externa vigente tiene veredicto. Solo se puede volver a pedir cuando la anterior terminó en error.",
    recovery: "El veredicto del proveedor se muestra en el bloque de evaluación externa.",
    isFormError: false,
  },
  EXTERNAL_EVALUATION_CONFLICT: {
    title: "Otra escritura tocó la evaluación externa al mismo tiempo",
    body: "Alguien más movió esta evaluación externa mientras se procesaba tu pedido, así que el tuyo no se aplicó.",
    recovery: `${RELOAD} El estado que se muestra es el que quedó guardado.`,
    isFormError: false,
  },
  EXTERNAL_EVALUATION_NOT_FOUND: {
    title: "Esa evaluación externa ya no existe",
    body: "La evaluación externa que se quiso usar no está en la base. Puede haberse importado un corpus nuevo desde que abriste el enlace.",
    recovery: RELOAD,
    isFormError: false,
  },
  PROVIDER_NOT_REGISTERED: {
    title: "Esta instalación no tiene adaptador para ese proveedor",
    body: "El proveedor externo que se pidió no está registrado en esta API. El MVP trae únicamente el proveedor simulado; el sandbox de Koin es post-MVP.",
    recovery: "Revisá la configuración de la API. Con KOIN_MODE=mock queda registrado el proveedor simulado.",
    isFormError: false,
  },
  INVALID_PROVIDER: {
    title: "La consola pidió un proveedor que la API no reconoce",
    body: "El nombre de proveedor enviado no está en el catálogo del contrato. Es un error interno: no debería ocurrir desde esta pantalla.",
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
  RECONCILIATION_CONFLICT: {
    title: "Otra escritura se llevó todas las evaluaciones del barrido",
    body: "Cada evaluación que el barrido examinó fue movida por otra escritura antes de que pudiera guardar la suya, así que el barrido no aplicó nada.",
    recovery: "Volvé a ejecutarlo. Si alguien más está usando la consola, esperá a que termine.",
    isFormError: false,
  },
  CALLBACK_UNAUTHORIZED: {
    title: "La API rechazó el callback por autenticación",
    body: "El endpoint de callbacks exige un secreto compartido y no llegó, o no coincide. Es un error interno: esta consola nunca envía callbacks, porque el secreto no vive en el proceso de Next.",
    recovery: "Revisá SALVO_CALLBACK_SHARED_SECRET en la configuración de la API. Si vuelve a pasar desde esta pantalla, reportalo con la hora exacta.",
    isFormError: false,
  },
  CALLBACK_UNAVAILABLE: {
    title: "La evaluación externa está siendo escrita por otra petición",
    body: "El callback no se pudo guardar porque otra escritura ganó la carrera dos veces seguidas. No se aplicó nada a medias: o entra entero o no entra.",
    recovery: "Volvé a intentarlo en unos segundos.",
    isFormError: false,
  },
  INVALID_CALLBACK: {
    title: "El cuerpo del callback no es válido",
    body: "Al mensaje le falta con qué correlacionarlo, o su estado no es uno de los admitidos. Es un error interno: esta consola no compone callbacks.",
    recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    isFormError: false,
  },
  CALLBACK_TOO_LARGE: {
    title: "El cuerpo del callback supera el máximo admitido",
    body: "Un callback pesa unos pocos bytes y la API fija un límite explícito. El cuerpo enviado lo excede.",
    recovery: "Revisá qué está enviando el proveedor. Si vuelve a pasar desde esta pantalla, reportalo con la hora exacta.",
    isFormError: false,
  },
  EXPLANATION_PENDING: {
    title: "La explicación se está redactando ahora mismo",
    body: "Ya hay una petición en curso para esta evaluación y todavía no respondió. Pedir otra pagaría dos veces la misma redacción.",
    recovery: "Esperá unos segundos y recargá la alerta: el texto aparece en el bloque de explicación.",
    isFormError: false,
  },
  EXPLANATION_ALREADY_READY: {
    title: "Esta evaluación ya tiene su explicación escrita",
    body: "Una explicación escrita no se regenera. Es el registro de lo que se pudo leer al decidir, y reemplazarla borraría el texto que alguien pudo haber tenido delante.",
    recovery: "El texto vigente se muestra en el bloque de explicación. Una evaluación distinta tiene su propia explicación.",
    isFormError: false,
  },
  EXPLANATION_ATTEMPTS_EXHAUSTED: {
    title: "Se agotaron los intentos de explicar esta evaluación",
    body: "La redacción falló todas las veces que el presupuesto de intentos permite. El tope existe para que un proveedor que falla en cadena no se cobre indefinidamente.",
    recovery: "Revisá el motivo del último intento en el bloque de explicación. El veredicto no necesita una explicación para emitirse.",
    isFormError: false,
  },
  EXPLANATION_CONFLICT: {
    title: "Otra escritura tocó la explicación al mismo tiempo",
    body: "Alguien más movió esta explicación mientras se procesaba tu pedido, así que el tuyo no se aplicó.",
    recovery: `${RELOAD} El estado que se muestra es el que quedó guardado.`,
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
