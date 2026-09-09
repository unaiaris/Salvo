/**
 * The console in Castilian Spanish.
 *
 * <strong>This file defines the key set.</strong> `Dictionary` is `typeof es`, so a key that exists
 * here and not in `pt.ts` is a compile error, and one that exists there and not here is too. Adding
 * a sentence means adding it twice, on purpose.
 *
 * The wording is the one the console has carried since stage 5 and it is reproduced to the
 * character: the smoke reads these strings out of the rendered HTML, and the point of moving them
 * into a dictionary was to change where they live rather than what they say.
 */
/**
 * The sentence six different conflicts end with. A local constant rather than a shared one: the
 * Portuguese file has its own, because a phrase reused across a language is a fact about that
 * language.
 */
const RELOAD = "Recargá la alerta para ver el estado registrado.";

export const es = {
  meta: {
    title: "Salvo",
    description: "Consola antifraude B2B con scoring determinista y auditable.",
    alerts: "Alertas · Salvo",
    alertDetail: "Alerta · Salvo",
    dashboard: "Dashboard · Salvo",
    import: "Importación · Salvo",
  },

  nav: {
    brand: "Salvo",
    label: "Secciones de la consola",
    alerts: "Alertas",
    import: "Importación",
    dashboard: "Dashboard",
    disclaimer: "Datos sintéticos · sin autenticación · uso local",
    // La misma línea, para un despliegue donde «uso local» sería falso.
    disclaimerShared: "Datos sintéticos · sin autenticación · instancia compartida",
  },

  /**
   * El cartel de la instancia pública, que la decisión 70 exige en pantalla y no en un README.
   *
   * Dice tres cosas y ninguna es decorativa. Que los datos son sintéticos, para que nadie crea que
   * está mirando pedidos de alguien. Que **lo que escriba lo ve todo el mundo**, porque la nota de
   * una revisión es texto libre, anónimo y público hasta el próximo reinicio. Y que todo vuelve a
   * cero, que es lo que hace aceptable a las dos anteriores.
   *
   * Hay dos versiones de la última frase porque el reinicio principal es el de la plataforma, por
   * inactividad, y no siempre hay además un máximo por antigüedad. Son frases enteras y no una
   * armada con fragmentos: una cláusula pegada al final es gramatical en un idioma y no en el otro.
   */
  /**
   * Lo que el visitante lee cuando el límite de tasa lo frena.
   *
   * Vive en los diccionarios aunque lo sirva `proxy.ts` en texto plano y en los dos idiomas a la
   * vez: componer la respuesta traducida costaría pedirle el idioma a la API y renderizar una
   * página, que es justo el trabajo que el límite existe para no hacer.
   */
  rateLimit: {
    title: "Demasiadas peticiones.",
    body:
      "Esta es una instancia de demostración compartida y pequeña. Esperá unos segundos y volvé a "
      + "intentarlo.",
  },

  sharedInstance: {
    label: "Sobre esta instancia",
    title: "Instancia de demostración compartida.",
    synthetic: "Los datos son sintéticos.",
    everyoneSees:
      "Lo que escribas acá lo ve todo el mundo: las notas de revisión, los veredictos y los pedidos "
      + "que importes.",
    resets: "Todo se reinicia cuando la instancia queda un rato sin visitas.",
    resetsWithMaximum: (minutes: string) =>
      `Todo se reinicia cuando la instancia queda un rato sin visitas, y como máximo cada ${minutes} `
      + "minutos.",
  },

  home: {
    eyebrow: "Consola antifraude",
    title: "Salvo concentra el riesgo antifraude en un núcleo .NET auditable.",
    lead:
      "Las reglas, el scoring y las transacciones viven en el backend. Esta interfaz lee un contrato "
      + "OpenAPI y no calcula riesgo por su cuenta.",
    cta: "Ir a la cola de alertas",
  },

  common: {
    technicalDetail: (detail: string) => `Detalle técnico de la API: ${detail}`,
    goToImport: "Ir a importación",
    runScoring: "Ejecutar scoring",
    viewDashboard: "Ver el dashboard",
  },

  provenance: {
    noRun: "El corpus todavía no se puntuó: no hay ninguna corrida de scoring.",
    run: (sequence: string, completedAt: string) => `corrida #${sequence}, ${completedAt}`,
    since: (run: string) => `Vigente desde la ${run}.`,
  },

  severity: {
    CRITICAL: "CRÍTICA",
    HIGH: "ALTA",
    MEDIUM: "MEDIA",
    none: "SIN BANDA",
  },

  status: {
    OPEN: "Abierta",
    CONFIRMED_SAFE: "Confirmada segura",
    REPORTED_FRAUD: "Fraude reportado",
  },

  /**
   * `amount_anomaly` is deliberately silent about whose median the amount was compared against: the
   * engine falls back from the buyer's to the merchant's, and the signal says which one, so a title
   * that named the buyer would contradict the sentence right below it.
   */
  rules: {
    amount_anomaly: "Monto atípico",
    velocity: "Ráfaga de pedidos",
    cross_border_velocity: "Ráfaga entre países",
    unusual_hour: "Hora inusual",
    new_buyer_high_value: "Comprador sin historia y monto alto",
    foreign_country: "País distinto del habitual",
  },

  /** What each rule found, composed from the fields `e3-v2` measured. */
  signals: {
    scopeBuyer: "del comprador",
    scopeMerchant: "del comercio",
    amountAnomaly: (
      amount: string,
      ratio: string,
      scope: string,
      median: string,
      history: string,
      windowDays: string,
    ) =>
      `El monto, ${amount}, es ${ratio} veces la mediana ${scope}, ${median}, calculada sobre `
      + `${history} pedidos previos de los últimos ${windowDays} días.`,
    velocity: (orders: string, windowMinutes: string, threshold: string) =>
      `Hubo ${orders} pedidos del mismo comprador dentro de ${windowMinutes} minutos; el umbral es `
      + `${threshold}.`,
    crossBorderVelocity: (from: string, to: string, minutes: string) =>
      `El país cambió de ${from} a ${to} en ${minutes} minutos, con el mismo comercio y el mismo `
      + "comprador.",
    unusualHour: (start: string, end: string, observed: string, total: string, share: string) =>
      `La franja de ${start} a ${end}, hora del comercio, aparece en ${observed} de ${total} `
      + `pedidos previos del comercio: un ${share} %.`,
    newBuyerHighValue: (amount: string, ratio: string, median: string, history: string) =>
      `El comprador no tenía pedidos previos con este comercio, y el monto, ${amount}, es ${ratio} `
      + `veces la mediana del comercio, ${median}, sobre ${history} pedidos previos.`,
    foreignCountry: (
      country: string,
      habitual: string,
      observed: string,
      total: string,
      share: string,
    ) =>
      `El país del pedido, ${country}, difiere del habitual del comercio, ${habitual}, observado en `
      + `${observed} de ${total} pedidos previos: un ${share} %.`,
  },

  /**
   * The per-record codes an import can report. They describe a row of the analyst's file rather
   * than a failure of the request, so they never travel through the failure catalogue.
   */
  importErrors: {
    REQUIRED: "Falta un campo obligatorio",
    INVALID_FORMAT: "El valor no tiene el formato esperado",
    OUT_OF_RANGE: "El valor está fuera del rango admitido",
    UNSUPPORTED_VALUE: "El valor no es uno de los admitidos",
    REFERENCE_CONFLICT: "La referencia ya existe con otros datos",
  },

  /**
   * `PENDING` is never «pendiente» on its own. Four different things wait in this console and a
   * screen that called them all the same word would be unreadable exactly when it matters.
   */
  externalStatus: {
    PENDING: "Esperando al proveedor",
    APPROVED: "Aprobado por el proveedor",
    DENIED: "Denegado por el proveedor",
    ERROR: "La consulta al proveedor falló",
  },

  externalSource: {
    SYNC: "en la misma respuesta del proveedor",
    CALLBACK: "por callback del proveedor",
    RECONCILIATION: "al reconciliar, preguntándole de nuevo",
  },

  externalError: {
    UNREACHABLE: "No se pudo contactar al proveedor: la consulta nunca salió",
    PROVIDER_REJECTED: "El proveedor rechazó la consulta",
    TIMEOUT: "El proveedor no respondió a tiempo",
    PROVIDER_ERROR: "El proveedor respondió con un error",
    INVALID_RESPONSE: "La respuesta del proveedor no se pudo leer",
  },

  /**
   * Why an explanation ended without text. These are values of a field inside a `200`, not
   * rejections of a request, so they live here and not in the failure catalogue — decision 57.
   * Two of them describe a rejection this system performed on its own provider, and they say so.
   */
  explanationFailure: {
    PROVIDER_UNAVAILABLE: "No se pudo redactar: el proveedor falló antes de responder",
    PROVIDER_TIMEOUT: "El proveedor no respondió dentro del tiempo permitido",
    PROVIDER_REFUSED: "El proveedor respondió sin texto",
    MALFORMED_OUTPUT: "El texto devuelto no era utilizable: vino vacío o con marcado",
    NOT_GROUNDED_NUMBER:
      "El texto traía una cifra que la evaluación no respalda, así que se descartó entero",
    NOT_GROUNDED_RULE:
      "El texto nombraba una regla que esta evaluación no disparó, así que se descartó entero",
    TOO_LONG: "El texto superó el largo máximo admitido",
    CANCELLED: "La petición se abandonó antes de que el proveedor respondiera",
    ATTEMPT_LIMIT_REACHED: "Se agotaron los intentos de redacción para esta evaluación",
    LEGACY_SIGNAL_FORMAT:
      "Esta evaluación guarda sus señales en el formato anterior del motor, que no se puede "
      + "verificar: no se le pidió nada a ningún proveedor",
  },

  explanationProvider: {
    MOCK: "plantilla determinista",
    ANTHROPIC: "Anthropic",
  },

  provider: {
    EXTERNAL_MOCK: "Proveedor simulado",
    KOIN_SANDBOX: "Koin sandbox",
  },

  alertsPage: {
    title: "Cola de alertas",
    lead:
      "Alertas abiertas, de mayor a menor score local de la evaluación vigente. La severidad de la "
      + "tabla es la del snapshot con el que se abrió cada alerta; el score vigente es lo que dice "
      + "el corpus ahora.",
    openCount: (count: number, formatted: string) =>
      count === 1 ? "1 alerta abierta" : `${formatted} alertas abiertas`,
    tableCaption: "Alertas abiertas, de mayor a menor score local de la evaluación vigente",
    columnOrder: "Pedido",
    columnSnapshotSeverity: "Severidad del snapshot",
    columnSnapshotScore: "Score del snapshot",
    columnCurrentScore: "Score vigente",
    columnAmount: "Monto",
    columnOccurred: "Ocurrió",
    buyerAndCountry: (buyer: string, country: string) => `Comprador ${buyer} · ${country}`,
    bandChanged: "La banda vigente cambió",
    noCurrentEvaluation: "sin evaluación vigente",
    emptyNoOrdersTitle: "Todavía no hay pedidos",
    emptyNoOrdersBody:
      "La base está vacía, así que no hay nada que puntuar ni nada que revisar. Importá un archivo "
      + "CSV o JSON, o cargá el corpus de demostración.",
    emptyNoRunTitle: "Hay pedidos importados y ninguna corrida de scoring",
    emptyNoRunBody: (orderCount: string) =>
      `Los ${orderCount} pedidos de la base todavía no se puntuaron, así que no existen `
      + "evaluaciones ni alertas. La cola se llena recién después de ejecutar una corrida.",
    emptyNoAlertsTitle: "No hay alertas abiertas",
    emptyNoAlertsBody: (run: string) =>
      `La ${run} no dejó ninguna alerta pendiente de revisión: ningún pedido alcanzó el umbral, o `
      + "todas las alertas abiertas ya tienen veredicto.",
  },

  alertDetail: {
    back: "← Volver a la cola de alertas",
    title: (reference: string) => `Alerta sobre ${reference}`,
    opened: (instant: string, policy: string) => `Abierta el ${instant} · política ${policy}`,
    supersedes: "escala una alerta anterior",

    orderTitle: "Pedido",
    orderReference: "Referencia del comercio",
    orderBuyer: "Comprador",
    orderAmount: "Monto",
    orderOccurred: "Ocurrió",
    orderOrigin: "Origen",
    orderDeviceSession: "Sesión de dispositivo",
    orderDeviceSessionAbsent: "sin registrar",

    noSignals: "Esta evaluación no disparó ninguna regla.",
    snapshotTitle: "Snapshot que abrió la alerta",
    snapshotHint: (instant: string) =>
      `Congelado el ${instant}. No se reescribe nunca: es la premisa sobre la que se forma el `
      + "veredicto.",
    currentTitle: "Evaluación vigente",
    currentNoRun: "El corpus no tiene ninguna corrida de scoring.",
    currentAbsent:
      "No hay evaluación vigente para este pedido. No es un score de cero: la corrida vigente no "
      + "dejó ninguna evaluación asociada a este pedido.",
    currentFlagged: "Marcada por el motor",
    currentBelowThreshold: "Por debajo del umbral",
    currentEvaluatedAt: (instant: string) =>
      `Calculada por primera vez el ${instant}. Una corrida posterior que no encuentra cambios `
      + "reutiliza esta misma evaluación y conserva su fecha, así que este instante no es el de la "
      + "corrida vigente.",

    divergenceAdvisory: (from: string, to: string) =>
      `La evaluación del pedido cambió (${from} → ${to}) sin cambiar de banda. Las señales vigentes `
      + "están en el bloque «Evaluación vigente».",
    divergenceAdvisorySignals: (score: string) =>
      `Las señales del pedido cambiaron, aunque el score sigue en ${score} y la banda tampoco se `
      + "movió. Las señales vigentes están en el bloque «Evaluación vigente».",
    divergenceOpenedAt: (severity: string, score: string) => `${severity} con score ${score}`,
    divergenceNoCurrent: (from: string) =>
      `La alerta se abrió en ${from}, pero el pedido ya no tiene evaluación vigente, así que no hay `
      + "nada con qué comparar el snapshot.",
    divergenceBandless: (score: string) =>
      `score ${score}, por debajo del umbral de alerta y sin banda`,
    divergenceBand: (from: string, to: string) =>
      `La alerta se abrió en ${from}. La evaluación vigente está en ${to}.`,

    externalTitle: "Evaluación externa",
    externalNever:
      "Una segunda opinión, de un proveedor antifraude externo. Todavía no se pidió ninguna para "
      + "este pedido.",
    externalProvider: (provider: string) =>
      `${provider}. El proveedor opina; la decisión sigue siendo del comercio.`,
    externalRequestedAt: (instant: string) => `Pedida el ${instant}`,
    externalSettledAt: (instant: string, source: string) =>
      `. Respondida el ${instant}, ${source}.`,
    externalScore: "Score del proveedor:",
    externalScoreHint:
      ". Está en la escala del proveedor y no se compara con el score local, que es otra escala de "
      + "otro sistema.",
    externalLastError: (error: string) =>
      `Último intento fallido: ${error}. La evaluación sigue esperando al proveedor: un intento que `
      + "no llegó a respuesta no es un veredicto.",
    externalDisagreementTitle: "Los dos criterios no coinciden",
    externalLocalLabel: "Motor local:",
    externalLocalFlagged: "marcó el pedido por encima del umbral.",
    externalLocalNotFlagged: "dejó el pedido por debajo del umbral.",
    externalLocalHint: "Reglas deterministas sobre la historia del comprador.",
    externalProviderLabel: "Proveedor externo:",
    externalProviderDenied: "denegó el pedido.",
    externalProviderApproved: "aprobó el pedido.",
    externalArrived: (source: string) => `Llegó ${source}.`,
    externalDisagreementHint:
      "No se combinan en un veredicto único ni se comparan sus scores. La discrepancia es "
      + "información para quien revisa, no una operación aritmética.",
    externalContradiction:
      "El proveedor envió un veredicto contradictorio: después de responder, mandó otro distinto "
      + "sobre la misma evaluación. Vale el primero —una evaluación con veredicto no se reabre— y "
      + "la contradicción queda registrada en vez de descartarse en silencio.",
    externalRequestButton: "Solicitar evaluación externa",
    externalRequestPending: "Consultando al proveedor…",
    externalDeliverButton: "Entregar el callback del proveedor",
    externalDeliverPending: "Entregando el callback…",
    externalDeliverHint:
      "Simula la llegada del callback que el proveedor enviaría por su cuenta. Existe solo porque "
      + "esta instancia se declara de demostración: quien lo pulsa elige qué evaluación, nunca qué "
      + "responde el proveedor.",

    explanationTitle: "Explicación",
    explanationLead:
      "El snapshot que abrió la alerta, contado en palabras. Describe la evaluación: no cambia el "
      + "score, ni la severidad, ni el veredicto.",
    explanationOutdatedTitle: "Esta explicación describe una evaluación que ya no es la vigente",
    explanationOutdatedBody:
      "Se redactó sobre el snapshot con el que se abrió la alerta, y desde entonces el pedido tiene "
      + "otra evaluación. Se conserva porque es el registro de lo que se pudo leer al decidir; las "
      + "señales de ahora están en el bloque «Evaluación vigente».",
    explanationOutdatedHasCurrent:
      " La evaluación vigente ya tiene además su propia explicación escrita.",
    explanationNeverAsked:
      "Todavía no se pidió una explicación de esta evaluación. Pedirla no cambia nada del pedido ni "
      + "de la alerta: se le pide el texto al proveedor de explicaciones y se verifica contra la "
      + "evaluación antes de guardarlo.",
    explanationWriting: "Redactando la explicación…",
    explanationWritingHint: (instant: string) =>
      `Pedida el ${instant}. Recargá la alerta en unos segundos. Si la petición quedó a medias, el `
      + "próximo pedido retoma la misma fila.",
    explanationWrittenBy: (provider: string, version: string, settledAt: string) =>
      `Redactada por una ${provider} (${version}), no por un modelo${settledAt}.`,
    explanationWrittenAt: (instant: string) => `, el ${instant}`,
    explanationVerified:
      "Cada cifra y cada regla del texto se verificaron contra esta evaluación antes de guardarlo: "
      + "un texto que no pasa esa comprobación no se guarda ni se muestra.",
    explanationCitedRules: (rules: string) => `Reglas citadas: ${rules}.`,
    explanationFailedWithoutCode: "La redacción terminó sin texto utilizable",
    explanationAttempts: (count: number, formatted: string) =>
      count === 1 ? "Se intentó una vez" : `Se intentó ${formatted} veces`,
    explanationLastAttempt: (instant: string) => `; el último, el ${instant}`,
    explanationExhausted:
      "Se agotó el presupuesto de intentos, así que no se vuelve a pedir. Un veredicto no necesita "
      + "explicación para emitirse.",
    explanationNotStored: "El texto que no se pudo verificar no se guarda ni llega a esta pantalla.",
    explanationAskFirst: "Explicar esta evaluación",
    explanationAskRetry: "Volver a intentar la explicación",
    explanationAskCurrentTemplate: "Redactar con la plantilla vigente",
    explanationAskPending: "Redactando…",

    reviewTitle: "Emitir veredicto",
    reviewLead:
      "El veredicto es definitivo: una alerta revisada no se reabre. Si el criterio cambia, hace "
      + "falta una alerta nueva sobre el pedido.",
    reviewAcknowledgeTitle: "El corpus cambió desde que se abrió esta alerta",
    reviewAcknowledgeLabel:
      "Leí en qué cambió la evaluación vigente y quiero emitir el veredicto igual.",
    reviewLegend: "Veredicto",
    reviewSafeLabel: "Confirmar segura",
    reviewSafeHint: "El pedido no es fraude.",
    reviewFraudLabel: "Reportar fraude",
    reviewFraudHint: "El pedido es fraude y queda registrado como tal.",
    reviewNoteLabel: "Nota de la revisión (opcional)",
    reviewNoteHint: (maxLength: string) =>
      `Hasta ${maxLength} caracteres. Queda en la auditoría junto al veredicto.`,
    reviewBlocked: "Marcá la casilla de arriba para poder enviar el veredicto.",
    reviewSubmit: "Registrar veredicto",
    reviewSubmitPending: "Registrando…",
    verdictAnnounced: "La alerta quedó revisada y el formulario de veredicto ya no está.",
    verdictTitle: (status: string) => `Veredicto registrado: ${status}`,
    verdictNoAudit: "La alerta está cerrada, pero no hay una entrada de auditoría asociada.",
    verdictTransition: (from: string, to: string, instant: string) =>
      `Pasó de ${from} a ${to} el ${instant}.`,
    verdictNoNote: "La revisión se registró sin nota.",
    verdictNoteTitle: "Nota registrada",
    verdictTerminal:
      "Un veredicto es terminal. Nada reabre una alerta revisada; una escalada crea una alerta "
      + "nueva enlazada a esta.",
  },

  dashboard: {
    title: "Dashboard",
    lead:
      "Estado operativo de la corrida vigente. Los montos se informan por moneda y nunca se suman "
      + "entre ellas. «Fraude reportado» es lo que decidió una analista, no la verdad de campo.",
    ordersInRun: (count: string) => `${count} pedidos en la corrida`,
    pendingTitle: (count: number, formatted: string) =>
      count === 1
        ? "Hay 1 pedido fuera de la corrida vigente"
        : `Hay ${formatted} pedidos fuera de la corrida vigente`,
    pendingBody:
      "No están puntuados, así que no cuentan en ninguna de las cifras de abajo y no pueden abrir "
      + "alertas.",

    emptyNoOrdersTitle: "Todavía no hay pedidos",
    emptyNoOrdersBody:
      "La base está vacía: no hay monto en riesgo, ni tasa de marcado, ni semanas que dibujar. "
      + "Importá un archivo CSV o JSON, o cargá el corpus de demostración.",
    emptyNoRunTitle: "Hay pedidos importados y ninguna corrida de scoring",
    emptyNoRunBody: (count: string) =>
      `Los ${count} pedidos de la base no tienen evaluación, así que el dashboard no tiene nada que `
      + "resumir: sin corrida no hay score, ni marcado, ni alertas. Nada de esto se calcula solo.",
    emptyNoAlertsTitle: "No hay alertas abiertas",
    emptyNoAlertsBody: (run: string) =>
      `La ${run} no dejó nada pendiente de revisión: ningún pedido alcanzó el umbral, o todas las `
      + "alertas que se abrieron ya tienen veredicto. Las cifras de riesgo de abajo se calculan "
      + "igual sobre la corrida vigente.",

    openAlertsTitle: "Alertas abiertas",
    openAlertsHint: "Pendientes de veredicto, por banda de severidad.",
    openAlertsNone: "sin alertas",
    amountAtRiskTitle: "Monto en riesgo",
    amountAtRiskHint:
      "Importe de los pedidos con alerta abierta. Una fila por moneda: no existe un total.",
    amountAtRiskNone: "No hay ninguna alerta abierta con monto asociado.",
    alertCount: (count: number, formatted: string) =>
      count === 1 ? "1 alerta" : `${formatted} alertas`,
    reportedFraudTitle: "Fraude reportado",
    reportedFraudHint:
      "Veredictos de la analista, agregados por pedido distinto. También por moneda.",
    reportedFraudNone: "Todavía nadie marcó una alerta como fraude en esta base.",
    orderCount: (count: number, formatted: string) =>
      count === 1 ? "1 pedido" : `${formatted} pedidos`,
    flagRateTitle: "Tasa de marcado",
    flagRateNone: "La corrida vigente no cubrió ningún pedido, así que no hay proporción que calcular.",
    flagRateHint: (count: string) =>
      `Proporción de los ${count} pedidos de la corrida vigente que las reglas denegaron.`,
    topSignalsTitle: "Señales principales",
    topSignalsHint:
      "Reglas presentes en el snapshot de las alertas abiertas, no en todas las evaluaciones.",
    topSignalsNone: "Ninguna alerta abierta, así que ninguna señal.",

    denialsTitle: "Denegados por el proveedor sin alerta local",
    denialsHint:
      "Pedidos que el proveedor externo denegó y que el motor local nunca marcó. Es la única "
      + "pantalla donde aparece un pedido sin alerta.",
    denialsNone:
      "Ningún pedido denegado por el proveedor quedó fuera de la cola. O nadie pidió todavía la "
      + "evaluación externa, o el proveedor y el motor local coincidieron en todo.",
    denialsOrderWord: (count: number) => (count === 1 ? "pedido" : "pedidos"),
    denialsListed: (count: string) => ` · se listan los ${count} más recientes`,
    denialsCaption:
      "Pedidos denegados por el proveedor externo que no abrieron ninguna alerta local",
    denialsColumnOrder: "Pedido",
    denialsColumnDate: "Fecha",
    denialsColumnAmount: "Monto",
    denialsColumnCountry: "País",
    denialsColumnScore: "Score local",
    denialsUnscored: "sin puntuar",

    riskOverTimeTitle: "Riesgo en el tiempo",
    riskOverTimeHint:
      "Por semana de ocurrencia del pedido, en hora de Montevideo: la misma zona con la que las "
      + "reglas deciden a qué día pertenece cada pedido.",
    riskOverTimeNone:
      "La corrida vigente no cubrió ningún pedido, así que no hay semanas que dibujar.",
    chartTitle: "Pedidos por semana y cuántos de ellos denegó la corrida vigente",
    chartDescription: (weeks: string, range: string, orders: string, flagged: string) =>
      `${weeks} semanas${range}. ${orders} pedidos en total, de los cuales ${flagged} quedaron `
      + "denegados. Los mismos números están en la tabla que sigue al gráfico.",
    chartRange: (from: string, to: string) => `, de ${from} a ${to}`,
    chartBar: (week: string, orders: string, flagged: string) =>
      `Semana del ${week}: ${orders} pedidos, ${flagged} denegados`,
    chartLegendOrders: "Pedidos de la semana",
    chartLegendFlagged: "Denegados por la corrida vigente",
    chartTableCaption: "Pedidos y denegados por semana, los mismos datos que dibuja el gráfico.",
    chartColumnWeek: "Semana",
    chartColumnOrders: "Pedidos",
    chartColumnFlagged: "Denegados",
    chartColumnShare: "Proporción",

    qualityTitle: "Calidad del criterio",
    qualityLead:
      "Medida contra las etiquetas del corpus de demostración. Ninguna cifra del resto de esta "
      + "pantalla usa esas etiquetas.",
    qualityCaveat:
      "La fixture demo tiene errores puestos a mano: tres falsos negativos que ninguna regla "
      + "puede ver y cuatro falsos positivos. Por eso F1 no vale 1,00, y por eso tampoco mide el "
      + "criterio: con los errores puestos a propósito, F1 es un parámetro elegido y no un "
      + "resultado. Estas métricas prueban el pipeline de evaluación —división temporal, holdout "
      + "sin retuning, cálculo correcto—, no la calidad de la detección.",
    qualityScoredOrders: "Pedidos puntuados",
    qualityLabeled: "Con etiqueta",
    qualityUnlabeled: "Sin etiqueta",
    qualityUnlabeledHint: "Se cuentan y se excluyen: un pedido importado nunca trae etiqueta.",
    qualityRuleConfig: "Configuración de reglas",
    qualityHoldoutTitle: (count: string) => `Holdout · ${count} pedidos`,
    qualityHoldoutHint: (threshold: string) =>
      `Umbral ${threshold}, elegido sobre la cohorte de calibración y aplicado acá sin retocar nada.`,
    qualityCalibrationTitle: (count: string) => `Calibración · ${count} pedidos`,
    qualityCalibrationHint:
      "La cohorte temprana, sobre la que se eligió el umbral. No es una medición independiente.",
    qualityUndefined: "sin definir",
    qualityPrecision: "Precisión",
    qualityRecall: "Recall",
    qualityF1: "F1",
    qualityFalsePositiveRate: "Tasa de falsos positivos",
    qualityMatrixCaption: "Matriz de confusión",
    qualityTruePositives: "Verdaderos positivos",
    qualityFalsePositives: "Falsos positivos",
    qualityFalseNegatives: "Falsos negativos",
    qualityTrueNegatives: "Verdaderos negativos",
    qualityRunFootnote: (sequence: string, instant: string) => `Corrida #${sequence}, ${instant}.`,
    qualitySweepSummary: (points: string) =>
      `Barrido de umbrales sobre la cohorte de calibración (${points} puntos)`,
    qualitySweepHint: (threshold: string) =>
      `Solo los umbrales donde la matriz de confusión cambia. El umbral ${threshold} es el que se `
      + "eligió y el que se aplicó al holdout.",
    qualitySweepCaption:
      "Precisión, recall, F1 y tasa de falsos positivos en cada umbral de la cohorte de calibración",
    qualitySweepThreshold: "Umbral",
    qualitySweepChosen: " · elegido",
  },

  importPage: {
    title: "Importación y scoring",
    lead:
      "Importar escribe pedidos y nada más. Las evaluaciones y las alertas las produce una corrida "
      + "de scoring, que se ejecuta desde acá: hasta que no la corras, la cola de alertas y el "
      + "dashboard siguen mostrando el estado de la corrida anterior.",
    corpusStatusTitle: "Estado del corpus",
    corpusPending: (count: number, formatted: string) =>
      count === 1
        ? "Hay 1 pedido sin puntuar por la corrida vigente."
        : `Hay ${formatted} pedidos sin puntuar por la corrida vigente.`,
    corpusPendingHint:
      "No aparecen en el dashboard ni pueden generar alertas hasta que ejecutes una corrida.",
    corpusEmpty: "No hay pedidos sin puntuar porque todavía no hay ninguno en la base.",
    corpusCovered: "Todos los pedidos de la base están cubiertos por la corrida vigente.",

    seedTitle: "Corpus de demostración",
    seedDescription:
      "Trescientos pedidos sintéticos con sus etiquetas de fraude, pensados para poder medir el "
      + "criterio: incluye fraude que las reglas locales no pueden ver y pedidos legítimos que sí "
      + "marcan. La carga es idempotente: repetirla no duplica nada. Esta sección existe solo "
      + "porque esta instancia se declara de demostración.",
    seedConflictPreviousTitle: "Esta base tiene una versión anterior del corpus de demostración",
    seedConflictPreviousBody: (version: string) =>
      `Cargar la versión ${version} exige una base nueva: un pedido es inmutable, así que las dos `
      + "versiones no pueden convivir bajo las mismas referencias de comercio. La base actual no se "
      + "toca ni se pierde: deja de ser la de demostración.",
    seedConflictImportedTitle: "Esta base tiene pedidos importados con las mismas referencias",
    seedConflictImportedBody:
      "Los pedidos que ya están usan las mismas referencias que la fixture y tienen otros datos. La "
      + "carga se cancela entera antes que pisar ninguno.",
    seedButton: "Cargar corpus de demostración",
    seedButtonPending: "Cargando corpus…",

    fileTitle: "Importar un archivo",
    fileDescription:
      "CSV o JSON. La validación es estricta por registro y la escritura atómica por archivo: los "
      + "registros rechazados se listan uno por uno y no se escribe ninguno de ellos.",
    fileLabel: "Archivo",
    fileHint: (maxMib: string) =>
      `Hasta ${maxMib} MiB y 10.000 pedidos por archivo. Los datos deben ser sintéticos.`,
    formatLegend: "Formato",
    fileButton: "Importar pedidos",
    fileButtonPending: "Importando…",

    scoringTitle: "Ejecutar scoring",
    scoringDescription:
      "Evalúa el corpus completo en orden temporal, con el baseline construido solo con la historia "
      + "anterior a cada pedido, y abre las alertas que correspondan. Es idempotente: una "
      + "evaluación cuyo resultado no cambió se reusa en vez de duplicarse, y una alerta ya abierta "
      + "o ya revisada no se vuelve a abrir.",
    scoringButton: "Ejecutar scoring",
    scoringButtonPending: "Ejecutando corrida…",
    scoringRunning:
      "La corrida evalúa el corpus en orden temporal. En un corpus de trescientos pedidos tarda "
      + "unos segundos.",

    externalTitle: "Proveedor antifraude externo",
    externalDescription:
      "Una segunda opinión sobre cada pedido, de un proveedor externo simulado. El detalle de una "
      + "alerta permite pedirla de a un pedido; acá se pide para el corpus entero, que es lo único "
      + "que alcanza a los pedidos que nunca abrieron una alerta. Entregar los callbacks simula la "
      + "respuesta que el proveedor mandaría por su cuenta: quien lo pulsa elige qué evaluación, "
      + "nunca qué responde el proveedor. Repetirlo no repite efectos.",
    externalRequestButton: "Solicitar evaluación externa del corpus",
    externalRequestPending: "Consultando al proveedor…",
    externalRequestRunning:
      "Se consulta pedido por pedido, reservando la fila antes de llamar. En un corpus de "
      + "trescientos pedidos tarda unos segundos.",
    externalDeliverButton: "Entregar los callbacks del proveedor",
    externalDeliverPending: "Entregando callbacks…",

    rejectedTitle: (count: string) => `Registros rechazados (${count})`,
    rejectedHint: "Ninguno de estos se escribió. Los pedidos válidos del mismo archivo sí.",
    rejectedTruncated:
      "La API dejó de enumerar errores en este punto: hay más registros rechazados de los que se "
      + "listan acá.",
    recordAt: (record: string) => `Registro ${record}`,
    recordAtLine: (record: string, line: string) => `Registro ${record}, línea ${line}`,
    recordField: (field: string) => ` · campo ${field}`,
    recordError: (where: string, field: string, label: string, message: string) =>
      `${where}${field} · ${label}: ${message}`,
  },

  /** What an attempt produced, per action. Never optimistic: these describe what was persisted. */
  outcomes: {
    seedAlreadyLoadedTitle: "El corpus de demostración ya estaba cargado",
    seedLoadedTitle: "Corpus cargado",
    seedAlreadyLoadedBody:
      "La carga es idempotente: los pedidos ya estaban en la base y no se duplicó ninguno.",
    seedLoadedBody: "Los pedidos quedaron en la base. Todavía no tienen evaluación ni alerta.",
    seedRecovery: "Ejecutá una corrida de scoring para puntuarlos.",
    seedFactVersion: (version: string) => `Versión de la fixture: ${version}`,
    seedFactInserted: (inserted: string, total: string) =>
      `Pedidos insertados: ${inserted} de ${total}`,
    seedFactDuplicates: (count: string) => `Pedidos ya presentes: ${count}`,
    seedFactLabels: (inserted: string, total: string) =>
      `Etiquetas insertadas: ${inserted} de ${total}`,

    importNoneTitle: "No se importó ningún pedido",
    importSomeTitle: (count: string) => `Se importaron ${count} pedidos`,
    importBody:
      "La importación es estricta por registro y atómica por archivo: los pedidos válidos se "
      + "escribieron todos juntos y los rechazados no se escribieron nunca.",
    importRecovery:
      "Los pedidos nuevos no tienen evaluación hasta que ejecutes una corrida de scoring.",
    importFactRead: (count: string) => `Registros leídos: ${count}`,
    importFactImported: (count: string) => `Importados: ${count}`,
    importFactDuplicates: (count: string) =>
      `Duplicados, ya presentes con los mismos datos: ${count}`,
    importFactRejected: (count: string) => `Rechazados: ${count}`,

    scoringTitle: (sequence: string) => `Corrida #${sequence} completada`,
    scoringBody:
      "Ya existe una evaluación vigente por pedido y las alertas que correspondían quedaron "
      + "abiertas. Una evaluación reusada es una cuyo resultado no cambió: mismo corpus, misma "
      + "configuración, mismo resultado.",
    scoringRecovery: "Revisá la cola de alertas o mirá el dashboard.",
    scoringFactConfig: (version: string) => `Configuración de reglas: ${version}`,
    scoringFactFinished: (instant: string) => `Terminó: ${instant}`,
    scoringFactOrders: (count: string) => `Pedidos evaluados: ${count}`,
    scoringFactCreated: (count: string) => `Evaluaciones creadas: ${count}`,
    scoringFactReused: (count: string) => `Evaluaciones reusadas: ${count}`,
    scoringFactAlerts: (count: string) => `Alertas abiertas: ${count}`,
    scoringFactSkippedOpen: (count: string) =>
      `Omitidas por tener ya una alerta abierta: ${count}`,
    scoringFactSkippedReviewed: (count: string) => `Omitidas por tener ya un veredicto: ${count}`,

    corpusExternalKnownTitle: "El proveedor ya conocía todos los pedidos",
    corpusExternalRequestedTitle: (count: string) => `Se consultaron ${count} pedidos`,
    corpusExternalBody:
      "Cada pedido pasa por el mismo caso de uso que una consulta suelta: la fila se reserva antes "
      + "de llamar al proveedor, así que dos consultas simultáneas no crean dos evaluaciones del "
      + "lado del proveedor.",
    corpusExternalSettledRecovery:
      "El veredicto de cada pedido aparece en el detalle de su alerta.",
    corpusExternalPendingRecovery:
      "Los que siguen esperando al proveedor se cierran entregando sus callbacks o reconciliando.",
    corpusExternalFactExamined: (count: string) => `Pedidos sin evaluación externa: ${count}`,
    corpusExternalFactRequested: (count: string) => `Consultados: ${count}`,
    corpusExternalFactSettled: (count: string) => `Con veredicto en el acto: ${count}`,
    corpusExternalFactPending: (count: string) => `Esperando al proveedor: ${count}`,
    corpusExternalFactSkipped: (count: string) => `Omitidos, ya tenían evaluación: ${count}`,

    deliverNoneTitle: "No hay ninguna evaluación externa esperando al proveedor",
    deliverReplayedTitle: (count: string) => `Los ${count} callbacks ya se habían recibido`,
    deliverDoneTitle: (count: string) => `Se entregaron ${count} callbacks`,
    deliverReplayedBody:
      "Cada mensaje es idéntico a uno ya registrado, así que no se repitió ningún efecto: se anotó "
      + "que el proveedor los volvió a enviar y nada más.",
    deliverDoneBody:
      "Los callbacks entran por el mismo caso de uso que usaría el proveedor: mismo recibo, misma "
      + "deduplicación, mismas reglas de transición. Quien pulsa elige qué evaluación, nunca qué "
      + "responde el proveedor.",
    deliverNoneRecovery:
      "Solicitá evaluaciones externas del corpus para que haya algo que entregar.",
    deliverDoneRecovery: "El veredicto del proveedor aparece en el detalle de cada alerta.",
    deliverFactExamined: (count: string) => `Evaluaciones esperando al proveedor: ${count}`,
    deliverFactDelivered: (count: string) => `Callbacks entregados: ${count}`,
    deliverFactSettled: (count: string) => `Cerraron con veredicto: ${count}`,
    deliverFactReplayed: (count: string) => `Ya se habían recibido: ${count}`,
    deliverFactUnavailable: (count: string) => `No se pudieron escribir por concurrencia: ${count}`,

    externalRequestedTitle: "Evaluación externa solicitada",
    externalAlreadyTitle: "Este pedido ya tenía una evaluación externa",
    externalWaitingBody:
      "El proveedor aceptó la consulta y todavía no decidió: la respuesta va a llegar por callback, "
      + "o al reconciliar.",
    externalSettledBody:
      "El veredicto del proveedor queda registrado junto al criterio local, sin combinarse con él.",
    externalAlreadyRecovery:
      "Pedirla de nuevo no crea una segunda evaluación del lado del proveedor.",
    externalCallbackReplayedTitle: "Ese callback ya se había recibido",
    externalCallbackDoneTitle: "Callback entregado",
    externalCallbackReplayedBody:
      "El mensaje es idéntico a uno ya registrado, así que no se repitió ningún efecto: se anotó "
      + "que el proveedor lo volvió a enviar y nada más.",
    externalCallbackDoneBody:
      "El callback entró por el mismo camino que usaría el proveedor: mismo recibo, misma "
      + "deduplicación, mismas reglas de transición.",
    externalCallbackRecovery: "El estado del proveedor ya figura arriba.",

    explanationWrittenFirstTitle: "Explicación redactada",
    explanationWrittenRetryTitle: "Explicación redactada en el nuevo intento",
    explanationWrittenCurrentTemplateTitle: "Redactada de nuevo con la plantilla vigente",
    explanationWrittenBody:
      "El texto quedó guardado junto a la evaluación y ya se muestra arriba. Una explicación "
      + "escrita no se reescribe: si el pedido vuelve a evaluarse, la evaluación nueva lleva la "
      + "suya.",
    explanationWrittenCurrentTemplateBody:
      "El texto nuevo se escribió al lado del anterior y es el que se muestra arriba. El anterior "
      + "sigue guardado sin cambios, porque es el registro de lo que se pudo leer mientras se "
      + "formaba el veredicto.",
    explanationUnchangedTitle: "Esta evaluación ya tenía su explicación",
    explanationUnchangedBody:
      "No se le pidió nada al proveedor: la que ya estaba escrita es la que se muestra arriba.",
    explanationFailedTitle: "No se pudo redactar la explicación",
    explanationFailedWithoutCode: "El proveedor no dejó ningún texto utilizable.",
    explanationFailedWithCode: (label: string) => `${label}.`,
    explanationFailedExhausted:
      "Se agotaron los intentos para esta evaluación. El veredicto no necesita una explicación para "
      + "emitirse.",
    explanationFailedRetry:
      "Podés volver a intentarlo. Un texto que no se pudo verificar no se guarda ni se muestra.",

    reviewUnchangedTitle: "La alerta ya tenía exactamente este veredicto",
    reviewUnchangedBody:
      "No se registró una revisión nueva porque el veredicto y la nota guardados coinciden con los "
      + "que enviaste.",
    reviewUnchangedRecovery: "El veredicto vigente es el que se muestra abajo.",
    reviewAppliedTitle: "Veredicto registrado",
    reviewAppliedBody:
      "La revisión quedó guardada junto con su auditoría. Un veredicto es definitivo: esta alerta "
      + "no se reabre.",
  },

  /**
   * What the analyst reads when something fails, and what she can do next.
   *
   * One entry per code the API can emit: a shared «algo salió mal» would hide precisely the
   * difference between «otra persona ya emitió un veredicto» and «el corpus cambió bajo tus
   * pies». `isFormError` is not here — whether a message belongs beside a field or at the top
   * of the page is a fact about the console, not about a language, and it stays in
   * `messages.ts` beside the list of codes its exactness test asserts.
   */
  failures: {
    ALERT_NOT_FOUND: {
      title: "Esta alerta ya no existe",
      body: "La alerta que pediste no está en la base. Puede haberse importado un corpus nuevo desde que abriste el enlace.",
      recovery: "Volvé al feed de alertas y elegí una de la cola vigente.",
    },
    INVALID_STATUS: {
      title: "El veredicto no es válido",
      body: "Solo se puede marcar una alerta como «Segura» o «Fraude reportado».",
      recovery: "Elegí uno de los dos veredictos y volvé a enviar.",
    },
    NOTE_TOO_LONG: {
      title: "La nota es demasiado larga",
      body: "La nota de revisión admite hasta 2000 caracteres.",
      recovery: "Recortá la nota y volvé a enviar. Lo que escribiste sigue en el campo.",
    },
    ALERT_ALREADY_REVIEWED: {
      title: "Otra persona ya revisó esta alerta",
      body: "La alerta quedó cerrada con un veredicto distinto del que enviaste. Un veredicto es definitivo: no se reabre.",
      recovery: `${RELOAD} Si el criterio cambió, hace falta una alerta nueva sobre el pedido.`,
    },
    ALERT_REVIEW_NOTE_CONFLICT: {
      title: "La alerta ya tiene este veredicto, con otra nota",
      body: "El veredicto registrado coincide con el que enviaste, pero la nota guardada es distinta y no se sobrescribe.",
      recovery: `${RELOAD} La nota registrada aparece en el bloque de revisión.`,
    },
    ALERT_DIVERGENCE_NOT_ACKNOWLEDGED: {
      title: "El corpus cambió desde que se abrió la alerta",
      body: "La evaluación vigente del pedido está en otra banda de severidad que la del snapshot con el que se abrió la alerta. La API no acepta un veredicto sin que lo reconozcas.",
      recovery: "Volvé al aviso de divergencia, leé qué cambió y marcá la casilla antes de enviar.",
    },
    ALERT_REVIEW_CONFLICT: {
      title: "Dos revisiones al mismo tiempo",
      body: "Otra revisión sobre esta alerta se guardó mientras se procesaba la tuya, así que la tuya no se aplicó.",
      recovery: `${RELOAD} Si seguís con el mismo criterio, volvé a enviarlo.`,
    },
    SCORING_RUN_CONFLICT: {
      title: "Otra corrida de scoring se ejecutó al mismo tiempo",
      body: "Dos corridas escribieron estado en conflicto, así que la tuya no se guardó. El corpus quedó como estaba antes de intentarlo: no hay evaluaciones ni alertas a medio escribir.",
      recovery: "Volvé a ejecutar la corrida. Si alguien más está usando la consola, esperá a que termine.",
    },
    METRICS_UNAVAILABLE: {
      title: "Todavía no se pueden calcular las métricas de calidad",
      body: "Medir el criterio exige una corrida de scoring y pedidos etiquetados a ambos lados de la división temporal. Falta alguna de las dos cosas.",
      recovery: "Ejecutá una corrida de scoring sobre el corpus de demostración desde la pantalla de importación.",
    },
    DEMO_DATA_CONFLICT: {
      title: "El corpus de demostración choca con pedidos que ya existen",
      body: "La base tiene pedidos importados con las mismas referencias que la fixture pero con datos distintos. Un pedido es inmutable, así que la carga se cancela entera antes que pisar nada.",
      recovery: "Usá una base vacía para cargar la demo, o seguí con los pedidos que ya están importados.",
    },
    DEMO_DATA_PREVIOUS_CORPUS: {
      title: "Esta base tiene una versión anterior del corpus de demostración",
      body: "Las dos versiones usan las mismas referencias de comercio y un pedido es inmutable, así que no pueden convivir. No se escribió nada: la base quedó como estaba.",
      recovery: "Cargá el corpus en una base nueva. La base actual no se pierde; deja de ser la de demostración.",
    },
    FILE_REQUIRED: {
      title: "No llegó ningún archivo",
      body: "El formulario se envió sin archivo, o con uno de cero bytes.",
      recovery: "Elegí un archivo CSV o JSON con al menos un pedido y volvé a enviar.",
    },
    FILE_TOO_LARGE: {
      title: "El archivo supera el máximo admitido",
      body: (maxMib: string) =>
        `La importación acepta hasta ${maxMib} MiB por archivo. Nada de lo que enviaste se `
        + "importó.",
      recovery: "Partí el archivo en varios más chicos e importalos de a uno.",
    },
    TOO_MANY_RECORDS: {
      title: "El archivo tiene demasiados registros",
      body: "La importación acepta hasta 10.000 pedidos por archivo. Se rechaza el documento entero: no se importa una parte y se descarta el resto en silencio.",
      recovery: "Partí el archivo en tandas de hasta 10.000 registros.",
    },
    ORDER_LIMIT_REACHED: {
      title: "Esta instancia no acepta más pedidos",
      body:
        "Es una instancia de demostración compartida y tiene un techo de pedidos, porque lo que "
        + "cuesta una corrida de scoring depende de cuántos hay. No es un problema de tu archivo: "
        + "el mismo entraría en una instancia con lugar.",
      recovery:
        "Probá con un archivo más chico, o esperá al próximo reinicio, que devuelve la base al "
        + "corpus de demostración.",
    },
    UNSUPPORTED_MEDIA_TYPE: {
      title: "La consola envió el formulario en un formato que la API no acepta",
      body: "La importación viaja como `multipart/form-data`. Es un error interno: no debería ocurrir desde esta pantalla.",
      recovery: "Recargá la página y volvé a intentarlo. Si vuelve a pasar, reportalo con la hora exacta.",
    },
    UNSUPPORTED_FORMAT: {
      title: "Ese formato no está soportado",
      body: "La importación admite CSV o JSON, y hay que declarar cuál es antes de enviar.",
      recovery: "Elegí CSV o JSON según el archivo y volvé a enviar.",
    },
    EMPTY_FILE: {
      title: "El archivo no tiene ningún pedido",
      body: "Se leyó completo y no contiene registros: puede ser un CSV con solo la fila de encabezados, o un JSON con una lista vacía.",
      recovery: "Revisá el archivo y volvé a enviarlo con al menos un pedido.",
    },
    INVALID_ENCODING: {
      title: "El archivo no está en UTF-8",
      body: "La importación lee UTF-8 y el archivo trae bytes que no lo son. No se importó nada.",
      recovery: "Volvé a exportar el archivo en UTF-8 y reintentá.",
    },
    INVALID_CSV: {
      title: "El CSV está mal formado",
      body: "La estructura del archivo no se pudo leer —comillas sin cerrar, o una fila con más campos que el encabezado—, así que no se importó ninguna fila.",
      recovery: "Corregí la estructura del archivo y volvé a enviarlo. El detalle técnico de abajo dice dónde falló.",
    },
    INVALID_JSON: {
      title: "El JSON está mal formado",
      body: "El archivo no es JSON válido, así que no se importó ningún pedido.",
      recovery: "Validá el archivo y volvé a enviarlo. El detalle técnico de abajo dice dónde falló.",
    },
    INVALID_JSON_ROOT: {
      title: "El JSON no es una lista de pedidos",
      body: "La importación espera un arreglo en la raíz del documento, con un objeto por pedido.",
      recovery: "Envolvé los pedidos en un arreglo `[ … ]` y volvé a enviar.",
    },
    MISSING_HEADER: {
      title: "Al CSV le falta un encabezado obligatorio",
      body: "El archivo no tiene fila de encabezados, o le falta alguna de las columnas que la importación exige.",
      recovery: "Agregá la fila de encabezados con todas las columnas obligatorias. El detalle técnico dice cuál falta.",
    },
    INVALID_HEADER: {
      title: "Un encabezado del CSV está vacío",
      body: "Una columna del archivo no tiene nombre, así que no se puede saber qué campo es.",
      recovery: "Nombrá todas las columnas del encabezado y volvé a enviar.",
    },
    DUPLICATE_HEADER: {
      title: "El CSV repite un encabezado",
      body: "Dos columnas del archivo tienen el mismo nombre y no hay forma de decidir cuál gana.",
      recovery: "Dejá una sola columna por campo y volvé a enviar.",
    },
    UNKNOWN_HEADER: {
      title: "El CSV trae una columna que la importación no conoce",
      body: "El archivo se rechaza entero antes que ignorar datos en silencio: una columna desconocida suele ser un archivo equivocado o un campo mal escrito.",
      recovery: "Quitá o corregí la columna. El detalle técnico dice cuál es.",
    },
    INVALID_SORT: {
      title: "La consola pidió un orden que la API no reconoce",
      body: "El feed pidió ordenar las alertas por un valor que el contrato no admite. Es un error interno: no debería ocurrir desde esta pantalla.",
      recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    },
    INVALID_SEVERITY: {
      title: "La consola pidió una severidad que la API no reconoce",
      body: "El feed filtró por una severidad que el contrato no admite. Es un error interno: no debería ocurrir desde esta pantalla.",
      recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    },
    INVALID_PAGE: {
      title: "La consola pidió una página inexistente",
      body: "El feed pidió un número de página fuera de rango. Es un error interno: no debería ocurrir desde esta pantalla.",
      recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    },
    EXTERNAL_EVALUATION_PENDING: {
      title: "Ya hay una evaluación externa esperando al proveedor",
      body: "Este pedido tiene una evaluación externa que el proveedor todavía no respondió. Solo puede haber una a la vez: pedir otra crearía una segunda evaluación del lado del proveedor por una pregunta que ya está hecha.",
      recovery: "Esperá el callback del proveedor, o ejecutá una reconciliación para volver a preguntarle.",
    },
    EXTERNAL_EVALUATION_SETTLED: {
      title: "El proveedor ya se pronunció sobre este pedido",
      body: "La evaluación externa vigente tiene veredicto. Solo se puede volver a pedir cuando la anterior terminó en error.",
      recovery: "El veredicto del proveedor se muestra en el bloque de evaluación externa.",
    },
    EXTERNAL_EVALUATION_CONFLICT: {
      title: "Otra escritura tocó la evaluación externa al mismo tiempo",
      body: "Alguien más movió esta evaluación externa mientras se procesaba tu pedido, así que el tuyo no se aplicó.",
      recovery: `${RELOAD} El estado que se muestra es el que quedó guardado.`,
    },
    EXTERNAL_EVALUATION_NOT_FOUND: {
      title: "Esa evaluación externa ya no existe",
      body: "La evaluación externa que se quiso usar no está en la base. Puede haberse importado un corpus nuevo desde que abriste el enlace.",
      recovery: RELOAD,
    },
    PROVIDER_NOT_REGISTERED: {
      title: "Esta instalación no tiene adaptador para ese proveedor",
      body: "El proveedor externo que se pidió no está registrado en esta API. El MVP trae únicamente el proveedor simulado; el sandbox de Koin es post-MVP.",
      recovery: "Revisá la configuración de la API. Con KOIN_MODE=mock queda registrado el proveedor simulado.",
    },
    INVALID_PROVIDER: {
      title: "La consola pidió un proveedor que la API no reconoce",
      body: "El nombre de proveedor enviado no está en el catálogo del contrato. Es un error interno: no debería ocurrir desde esta pantalla.",
      recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    },
    RECONCILIATION_CONFLICT: {
      title: "Otra escritura se llevó todas las evaluaciones del barrido",
      body: "Cada evaluación que el barrido examinó fue movida por otra escritura antes de que pudiera guardar la suya, así que el barrido no aplicó nada.",
      recovery: "Volvé a ejecutarlo. Si alguien más está usando la consola, esperá a que termine.",
    },
    CALLBACK_UNAUTHORIZED: {
      title: "La API rechazó el callback por autenticación",
      body: "El endpoint de callbacks exige un secreto compartido y no llegó, o no coincide. Es un error interno: esta consola nunca envía callbacks, porque el secreto no vive en el proceso de Next.",
      recovery: "Revisá SALVO_CALLBACK_SHARED_SECRET en la configuración de la API. Si vuelve a pasar desde esta pantalla, reportalo con la hora exacta.",
    },
    CALLBACK_UNAVAILABLE: {
      title: "La evaluación externa está siendo escrita por otra petición",
      body: "El callback no se pudo guardar porque otra escritura ganó la carrera dos veces seguidas. No se aplicó nada a medias: o entra entero o no entra.",
      recovery: "Volvé a intentarlo en unos segundos.",
    },
    INVALID_CALLBACK: {
      title: "El cuerpo del callback no es válido",
      body: "Al mensaje le falta con qué correlacionarlo, o su estado no es uno de los admitidos. Es un error interno: esta consola no compone callbacks.",
      recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    },
    CALLBACK_TOO_LARGE: {
      title: "El cuerpo del callback supera el máximo admitido",
      body: "Un callback pesa unos pocos bytes y la API fija un límite explícito. El cuerpo enviado lo excede.",
      recovery: "Revisá qué está enviando el proveedor. Si vuelve a pasar desde esta pantalla, reportalo con la hora exacta.",
    },
    EXPLANATION_PENDING: {
      title: "La explicación se está redactando ahora mismo",
      body: "Ya hay una petición en curso para esta evaluación y todavía no respondió. Pedir otra pagaría dos veces la misma redacción.",
      recovery: "Esperá unos segundos y recargá la alerta: el texto aparece en el bloque de explicación.",
    },
    EXPLANATION_ALREADY_READY: {
      title: "Esta evaluación ya tiene su explicación escrita",
      body: "Una explicación escrita no se regenera. Es el registro de lo que se pudo leer al decidir, y reemplazarla borraría el texto que alguien pudo haber tenido delante.",
      recovery: "El texto vigente se muestra en el bloque de explicación. Una evaluación distinta tiene su propia explicación.",
    },
    EXPLANATION_ATTEMPTS_EXHAUSTED: {
      title: "Se agotaron los intentos de explicar esta evaluación",
      body: "La redacción falló todas las veces que el presupuesto de intentos permite. El tope existe para que un proveedor que falla en cadena no se cobre indefinidamente.",
      recovery: "Revisá el motivo del último intento en el bloque de explicación. El veredicto no necesita una explicación para emitirse.",
    },
    EXPLANATION_CONFLICT: {
      title: "Otra escritura tocó la explicación al mismo tiempo",
      body: "Alguien más movió esta explicación mientras se procesaba tu pedido, así que el tuyo no se aplicó.",
      recovery: `${RELOAD} El estado que se muestra es el que quedó guardado.`,
    },
    INVALID_PAGE_SIZE: {
      title: "La consola pidió un tamaño de página fuera de rango",
      body: "El feed pidió más alertas por página de las que la API entrega. Es un error interno: no debería ocurrir desde esta pantalla.",
      recovery: "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta.",
    },
  },
  /** Failures that never carry a code: the API was not reached, or did not answer the contract. */
  transport: {
    timeoutTitle: "La API tardó demasiado en responder",
    timeoutBody:
      "La consulta se canceló para no dejar la pantalla colgada. No se llegó a leer ni a guardar "
      + "nada.",
    timeoutRecovery:
      "Volvé a intentarlo. Si persiste, revisá que el proceso de la API esté respondiendo.",
    unreachableTitle: "No se pudo contactar a la API",
    unreachableBody:
      "La consola no tiene con qué trabajar: todo el riesgo se calcula en el backend y ahora mismo "
      + "no responde.",
    unreachableRecovery: "Verificá que la API esté levantada en la dirección configurada y recargá.",
    malformedTitle: "La respuesta de la API no coincide con el contrato",
    malformedBody:
      "Llegó una respuesta que a esta consola le falta o le sobra algo respecto del contrato con el "
      + "que se construyó. No se muestra nada antes que mostrar algo mal leído.",
    malformedRecovery:
      "Es probable que la API y la consola estén en versiones distintas. Regenerá los tipos desde "
      + "OpenAPI y volvé a desplegar.",
    unknownTitle: "La API rechazó la operación",
    unknownBody: (status: string) =>
      `Respondió ${status} con un motivo que esta consola todavía no traduce.`,
    unknownRecovery:
      "Recargá la página. Si vuelve a pasar, reportalo con la hora exacta y el detalle técnico.",
  },
};
