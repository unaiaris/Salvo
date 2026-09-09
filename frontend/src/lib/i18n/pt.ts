import type { Dictionary } from "./dictionary";

/**
 * O console em português do Brasil.
 *
 * <strong>Não foi revisado por um falante nativo.</strong> Foi escrito pela mesma pessoa que
 * escreveu o castelhano, e `frontend/src/lib/i18n/glosario-pt.md` traz cada chave com as duas versões lado a lado
 * para que alguém que fale a língua corrija uma linha de cada vez sem abrir código.
 *
 * O tipo é o que garante que este arquivo esteja completo: `Dictionary` é `typeof es`, então uma
 * chave que falte aqui não compila e uma chave a mais também não. Não existe fallback para o
 * castelhano, de propósito — meia tela traduzida com todos os testes verdes é pior do que nenhuma.
 *
 * Vocabulário: «comercio» é <em>estabelecimento</em>, que é a palavra do mercado adquirente
 * brasileiro; «monto» é <em>valor</em>; «corrida» é <em>execução</em>; «veredicto» é
 * <em>veredito</em>; «denegado» é <em>negado</em>.
 */
const RELOAD = "Recarregue o alerta para ver o estado registrado.";

export const pt: Dictionary = {
  meta: {
    title: "Salvo",
    description: "Console antifraude B2B com scoring determinístico e auditável.",
    alerts: "Alertas · Salvo",
    alertDetail: "Alerta · Salvo",
    dashboard: "Painel · Salvo",
    import: "Importação · Salvo",
  },

  nav: {
    brand: "Salvo",
    label: "Seções do console",
    alerts: "Alertas",
    import: "Importação",
    dashboard: "Painel",
    disclaimer: "Dados sintéticos · sem autenticação · uso local",
    disclaimerShared: "Dados sintéticos · sem autenticação · instância compartilhada",
  },

  sharedInstance: {
    label: "Sobre esta instância",
    title: "Instância de demonstração compartilhada.",
    synthetic: "Os dados são sintéticos.",
    everyoneSees:
      "O que você escrever aqui todo mundo vê: as notas de revisão, os vereditos e os pedidos que "
      + "você importar.",
    resets: "Tudo é reiniciado quando a instância fica um tempo sem visitas.",
    resetsWithMaximum: (minutes: string) =>
      `Tudo é reiniciado quando a instância fica um tempo sem visitas, e no máximo a cada ${minutes} `
      + "minutos.",
  },

  home: {
    eyebrow: "Console antifraude",
    title: "O Salvo concentra o risco antifraude em um núcleo .NET auditável.",
    lead:
      "As regras, o scoring e as transações vivem no backend. Esta interface lê um contrato OpenAPI "
      + "e não calcula risco por conta própria.",
    cta: "Ir para a fila de alertas",
  },

  common: {
    technicalDetail: (detail: string) => `Detalhe técnico da API: ${detail}`,
    goToImport: "Ir para importação",
    runScoring: "Executar scoring",
    viewDashboard: "Ver o painel",
  },

  provenance: {
    noRun: "O corpus ainda não foi pontuado: não há nenhuma execução de scoring.",
    run: (sequence: string, completedAt: string) => `execução nº ${sequence}, ${completedAt}`,
    since: (run: string) => `Vigente desde a ${run}.`,
  },

  severity: {
    CRITICAL: "CRÍTICA",
    HIGH: "ALTA",
    MEDIUM: "MÉDIA",
    none: "SEM FAIXA",
  },

  status: {
    OPEN: "Aberto",
    CONFIRMED_SAFE: "Confirmado seguro",
    REPORTED_FRAUD: "Fraude reportada",
  },

  rules: {
    amount_anomaly: "Valor atípico",
    velocity: "Rajada de pedidos",
    cross_border_velocity: "Rajada entre países",
    unusual_hour: "Horário incomum",
    new_buyer_high_value: "Comprador sem histórico e valor alto",
    foreign_country: "País diferente do habitual",
  },

  signals: {
    scopeBuyer: "do comprador",
    scopeMerchant: "do estabelecimento",
    amountAnomaly: (
      amount: string,
      ratio: string,
      scope: string,
      median: string,
      history: string,
      windowDays: string,
    ) =>
      `O valor, ${amount}, é ${ratio} vezes a mediana ${scope}, ${median}, calculada sobre `
      + `${history} pedidos anteriores dos últimos ${windowDays} dias.`,
    velocity: (orders: string, windowMinutes: string, threshold: string) =>
      `Houve ${orders} pedidos do mesmo comprador em ${windowMinutes} minutos; o limiar é `
      + `${threshold}.`,
    crossBorderVelocity: (from: string, to: string, minutes: string) =>
      `O país mudou de ${from} para ${to} em ${minutes} minutos, com o mesmo estabelecimento e o `
      + "mesmo comprador.",
    unusualHour: (start: string, end: string, observed: string, total: string, share: string) =>
      `A faixa das ${start} às ${end}, hora do estabelecimento, aparece em ${observed} de ${total} `
      + `pedidos anteriores do estabelecimento: ${share} %.`,
    newBuyerHighValue: (amount: string, ratio: string, median: string, history: string) =>
      `O comprador não tinha pedidos anteriores com este estabelecimento, e o valor, ${amount}, é `
      + `${ratio} vezes a mediana do estabelecimento, ${median}, sobre ${history} pedidos `
      + "anteriores.",
    foreignCountry: (
      country: string,
      habitual: string,
      observed: string,
      total: string,
      share: string,
    ) =>
      `O país do pedido, ${country}, difere do habitual do estabelecimento, ${habitual}, observado `
      + `em ${observed} de ${total} pedidos anteriores: ${share} %.`,
  },

  importErrors: {
    REQUIRED: "Falta um campo obrigatório",
    INVALID_FORMAT: "O valor não tem o formato esperado",
    OUT_OF_RANGE: "O valor está fora da faixa admitida",
    UNSUPPORTED_VALUE: "O valor não é um dos admitidos",
    REFERENCE_CONFLICT: "A referência já existe com outros dados",
  },

  externalStatus: {
    PENDING: "Aguardando o provedor",
    APPROVED: "Aprovado pelo provedor",
    DENIED: "Negado pelo provedor",
    ERROR: "A consulta ao provedor falhou",
  },

  externalSource: {
    SYNC: "na própria resposta do provedor",
    CALLBACK: "por callback do provedor",
    RECONCILIATION: "ao reconciliar, perguntando de novo",
  },

  externalError: {
    UNREACHABLE: "Não foi possível contatar o provedor: a consulta nunca saiu",
    PROVIDER_REJECTED: "O provedor rejeitou a consulta",
    TIMEOUT: "O provedor não respondeu a tempo",
    PROVIDER_ERROR: "O provedor respondeu com um erro",
    INVALID_RESPONSE: "A resposta do provedor não pôde ser lida",
  },

  explanationFailure: {
    PROVIDER_UNAVAILABLE: "Não foi possível redigir: o provedor falhou antes de responder",
    PROVIDER_TIMEOUT: "O provedor não respondeu dentro do tempo permitido",
    PROVIDER_REFUSED: "O provedor respondeu sem texto",
    MALFORMED_OUTPUT: "O texto devolvido não era utilizável: veio vazio ou com marcação",
    NOT_GROUNDED_NUMBER:
      "O texto trazia um número que a avaliação não sustenta, então foi descartado inteiro",
    NOT_GROUNDED_RULE:
      "O texto citava uma regra que esta avaliação não disparou, então foi descartado inteiro",
    TOO_LONG: "O texto passou do comprimento máximo admitido",
    CANCELLED: "A requisição foi abandonada antes de o provedor responder",
    ATTEMPT_LIMIT_REACHED: "As tentativas de redação para esta avaliação se esgotaram",
    LEGACY_SIGNAL_FORMAT:
      "Esta avaliação guarda seus sinais no formato anterior do motor, que não pode ser "
      + "verificado: nada foi pedido a nenhum provedor",
  },

  explanationProvider: {
    MOCK: "modelo determinístico",
    ANTHROPIC: "Anthropic",
  },

  provider: {
    EXTERNAL_MOCK: "Provedor simulado",
    KOIN_SANDBOX: "Koin sandbox",
  },

  alertsPage: {
    title: "Fila de alertas",
    lead:
      "Alertas abertos, do maior para o menor score local da avaliação vigente. A severidade da "
      + "tabela é a do snapshot com que cada alerta foi aberto; o score vigente é o que o corpus diz "
      + "agora.",
    openCount: (count: number, formatted: string) =>
      count === 1 ? "1 alerta aberto" : `${formatted} alertas abertos`,
    tableCaption: "Alertas abertos, do maior para o menor score local da avaliação vigente",
    columnOrder: "Pedido",
    columnSnapshotSeverity: "Severidade do snapshot",
    columnSnapshotScore: "Score do snapshot",
    columnCurrentScore: "Score vigente",
    columnAmount: "Valor",
    columnOccurred: "Ocorreu",
    buyerAndCountry: (buyer: string, country: string) => `Comprador ${buyer} · ${country}`,
    bandChanged: "A faixa vigente mudou",
    noCurrentEvaluation: "sem avaliação vigente",
    emptyNoOrdersTitle: "Ainda não há pedidos",
    emptyNoOrdersBody:
      "A base está vazia, então não há nada a pontuar nem nada a revisar. Importe um arquivo CSV ou "
      + "JSON, ou carregue o corpus de demonstração.",
    emptyNoRunTitle: "Há pedidos importados e nenhuma execução de scoring",
    emptyNoRunBody: (orderCount: string) =>
      `Os ${orderCount} pedidos da base ainda não foram pontuados, então não existem avaliações nem `
      + "alertas. A fila só se enche depois de executar uma corrida.",
    emptyNoAlertsTitle: "Não há alertas abertos",
    emptyNoAlertsBody: (run: string) =>
      `A ${run} não deixou nenhum alerta pendente de revisão: nenhum pedido alcançou o limiar, ou `
      + "todos os alertas abertos já têm veredito.",
  },

  alertDetail: {
    back: "← Voltar para a fila de alertas",
    title: (reference: string) => `Alerta sobre ${reference}`,
    opened: (instant: string, policy: string) => `Aberto em ${instant} · política ${policy}`,
    supersedes: "escala um alerta anterior",

    orderTitle: "Pedido",
    orderReference: "Referência do estabelecimento",
    orderBuyer: "Comprador",
    orderAmount: "Valor",
    orderOccurred: "Ocorreu",
    orderOrigin: "Origem",
    orderDeviceSession: "Sessão do dispositivo",
    orderDeviceSessionAbsent: "sem registro",

    noSignals: "Esta avaliação não disparou nenhuma regra.",
    snapshotTitle: "Snapshot que abriu o alerta",
    snapshotHint: (instant: string) =>
      `Congelado em ${instant}. Nunca é reescrito: é a premissa sobre a qual o veredito se forma.`,
    currentTitle: "Avaliação vigente",
    currentNoRun: "O corpus não tem nenhuma execução de scoring.",
    currentAbsent:
      "Não há avaliação vigente para este pedido. Não é um score zero: a execução vigente não "
      + "deixou nenhuma avaliação associada a este pedido.",
    currentFlagged: "Marcado pelo motor",
    currentBelowThreshold: "Abaixo do limiar",
    currentEvaluatedAt: (instant: string) =>
      `Calculada pela primeira vez em ${instant}. Uma execução posterior que não encontra mudanças `
      + "reutiliza esta mesma avaliação e conserva sua data, então este instante não é o da "
      + "execução vigente.",

    divergenceAdvisory: (from: string, to: string) =>
      `A avaliação do pedido mudou (${from} → ${to}) sem mudar de faixa. Os sinais vigentes estão no `
      + "bloco «Avaliação vigente».",
    divergenceAdvisorySignals: (score: string) =>
      `Os sinais do pedido mudaram, embora o score continue em ${score} e a faixa também não tenha `
      + "mudado. Os sinais vigentes estão no bloco «Avaliação vigente».",
    divergenceOpenedAt: (severity: string, score: string) => `${severity} com score ${score}`,
    divergenceNoCurrent: (from: string) =>
      `O alerta foi aberto em ${from}, mas o pedido já não tem avaliação vigente, então não há nada `
      + "com que comparar o snapshot.",
    divergenceBandless: (score: string) => `score ${score}, abaixo do limiar de alerta e sem faixa`,
    divergenceBand: (from: string, to: string) =>
      `O alerta foi aberto em ${from}. A avaliação vigente está em ${to}.`,

    externalTitle: "Avaliação externa",
    externalNever:
      "Uma segunda opinião, de um provedor antifraude externo. Ainda não foi pedida nenhuma para "
      + "este pedido.",
    externalProvider: (provider: string) =>
      `${provider}. O provedor opina; a decisão continua sendo do estabelecimento.`,
    externalRequestedAt: (instant: string) => `Pedida em ${instant}`,
    externalSettledAt: (instant: string, source: string) =>
      `. Respondida em ${instant}, ${source}.`,
    externalScore: "Score do provedor:",
    externalScoreHint:
      ". Está na escala do provedor e não se compara com o score local, que é outra escala de outro "
      + "sistema.",
    externalLastError: (error: string) =>
      `Última tentativa com falha: ${error}. A avaliação continua aguardando o provedor: uma `
      + "tentativa que não chegou a uma resposta não é um veredito.",
    externalDisagreementTitle: "Os dois critérios não coincidem",
    externalLocalLabel: "Motor local:",
    externalLocalFlagged: "marcou o pedido acima do limiar.",
    externalLocalNotFlagged: "deixou o pedido abaixo do limiar.",
    externalLocalHint: "Regras determinísticas sobre o histórico do comprador.",
    externalProviderLabel: "Provedor externo:",
    externalProviderDenied: "negou o pedido.",
    externalProviderApproved: "aprovou o pedido.",
    externalArrived: (source: string) => `Chegou ${source}.`,
    externalDisagreementHint:
      "Não se combinam em um veredito único nem se comparam seus scores. A divergência é informação "
      + "para quem revisa, não uma operação aritmética.",
    externalContradiction:
      "O provedor enviou um veredito contraditório: depois de responder, mandou outro diferente "
      + "sobre a mesma avaliação. Vale o primeiro —uma avaliação com veredito não é reaberta— e a "
      + "contradição fica registrada em vez de ser descartada em silêncio.",
    externalRequestButton: "Solicitar avaliação externa",
    externalRequestPending: "Consultando o provedor…",
    externalDeliverButton: "Entregar o callback do provedor",
    externalDeliverPending: "Entregando o callback…",
    externalDeliverHint:
      "Simula a chegada do callback que o provedor enviaria por conta própria. Existe só porque "
      + "esta instância se declara de demonstração: quem clica escolhe qual avaliação, nunca o que "
      + "o provedor responde.",

    explanationTitle: "Explicação",
    explanationLead:
      "O snapshot que abriu o alerta, contado em palavras. Descreve a avaliação: não muda o score, "
      + "nem a severidade, nem o veredito.",
    explanationOutdatedTitle: "Esta explicação descreve uma avaliação que já não é a vigente",
    explanationOutdatedBody:
      "Foi redigida sobre o snapshot com que o alerta foi aberto, e desde então o pedido tem outra "
      + "avaliação. É conservada porque é o registro do que se pôde ler ao decidir; os sinais de "
      + "agora estão no bloco «Avaliação vigente».",
    explanationOutdatedHasCurrent:
      " A avaliação vigente já tem, além disso, sua própria explicação escrita.",
    explanationNeverAsked:
      "Ainda não foi pedida uma explicação desta avaliação. Pedi-la não muda nada do pedido nem do "
      + "alerta: o texto é pedido ao provedor de explicações e verificado contra a avaliação antes "
      + "de ser guardado.",
    explanationWriting: "Redigindo a explicação…",
    explanationWritingHint: (instant: string) =>
      `Pedida em ${instant}. Recarregue o alerta em alguns segundos. Se a requisição ficou pela `
      + "metade, o próximo pedido retoma a mesma linha.",
    explanationWrittenBy: (provider: string, version: string, settledAt: string) =>
      `Redigida por um ${provider} (${version}), não por um modelo de linguagem${settledAt}.`,
    explanationWrittenAt: (instant: string) => `, em ${instant}`,
    explanationVerified:
      "Cada número e cada regra do texto foram verificados contra esta avaliação antes de guardá-lo: "
      + "um texto que não passa nessa checagem não é guardado nem exibido.",
    explanationCitedRules: (rules: string) => `Regras citadas: ${rules}.`,
    explanationFailedWithoutCode: "A redação terminou sem texto utilizável",
    explanationAttempts: (count: number, formatted: string) =>
      count === 1 ? "Foi tentado uma vez" : `Foi tentado ${formatted} vezes`,
    explanationLastAttempt: (instant: string) => `; a última, em ${instant}`,
    explanationExhausted:
      "O orçamento de tentativas se esgotou, então não é pedido de novo. Um veredito não precisa de "
      + "explicação para ser emitido.",
    explanationNotStored:
      "O texto que não pôde ser verificado não é guardado nem chega a esta tela.",
    explanationAskFirst: "Explicar esta avaliação",
    explanationAskRetry: "Tentar a explicação de novo",
    explanationAskCurrentTemplate: "Redigir com o modelo vigente",
    explanationAskPending: "Redigindo…",

    reviewTitle: "Emitir veredito",
    reviewLead:
      "O veredito é definitivo: um alerta revisado não é reaberto. Se o critério mudar, é preciso um "
      + "alerta novo sobre o pedido.",
    reviewAcknowledgeTitle: "O corpus mudou desde que este alerta foi aberto",
    reviewAcknowledgeLabel:
      "Li o que mudou na avaliação vigente e quero emitir o veredito mesmo assim.",
    reviewLegend: "Veredito",
    reviewSafeLabel: "Confirmar seguro",
    reviewSafeHint: "O pedido não é fraude.",
    reviewFraudLabel: "Reportar fraude",
    reviewFraudHint: "O pedido é fraude e fica registrado como tal.",
    reviewNoteLabel: "Nota da revisão (opcional)",
    reviewNoteHint: (maxLength: string) =>
      `Até ${maxLength} caracteres. Fica na auditoria junto ao veredito.`,
    reviewBlocked: "Marque a caixa acima para poder enviar o veredito.",
    reviewSubmit: "Registrar veredito",
    reviewSubmitPending: "Registrando…",
    verdictAnnounced: "O alerta ficou revisado e o formulário de veredito já não está.",
    verdictTitle: (status: string) => `Veredito registrado: ${status}`,
    verdictNoAudit: "O alerta está fechado, mas não há uma entrada de auditoria associada.",
    verdictTransition: (from: string, to: string, instant: string) =>
      `Passou de ${from} para ${to} em ${instant}.`,
    verdictNoNote: "A revisão foi registrada sem nota.",
    verdictNoteTitle: "Nota registrada",
    verdictTerminal:
      "Um veredito é terminal. Nada reabre um alerta revisado; uma escalada cria um alerta novo "
      + "ligado a este.",
  },

  dashboard: {
    title: "Painel",
    lead:
      "Estado operacional da execução vigente. Os valores são informados por moeda e nunca somados "
      + "entre si. «Fraude reportada» é o que uma analista decidiu, não a verdade de campo.",
    ordersInRun: (count: string) => `${count} pedidos na execução`,
    pendingTitle: (count: number, formatted: string) =>
      count === 1
        ? "Há 1 pedido fora da execução vigente"
        : `Há ${formatted} pedidos fora da execução vigente`,
    pendingBody:
      "Não estão pontuados, então não contam em nenhum dos números abaixo e não podem abrir alertas.",

    emptyNoOrdersTitle: "Ainda não há pedidos",
    emptyNoOrdersBody:
      "A base está vazia: não há valor em risco, nem taxa de marcação, nem semanas a desenhar. "
      + "Importe um arquivo CSV ou JSON, ou carregue o corpus de demonstração.",
    emptyNoRunTitle: "Há pedidos importados e nenhuma execução de scoring",
    emptyNoRunBody: (count: string) =>
      `Os ${count} pedidos da base não têm avaliação, então o painel não tem o que resumir: sem `
      + "execução não há score, nem marcação, nem alertas. Nada disso se calcula sozinho.",
    emptyNoAlertsTitle: "Não há alertas abertos",
    emptyNoAlertsBody: (run: string) =>
      `A ${run} não deixou nada pendente de revisão: nenhum pedido alcançou o limiar, ou todos os `
      + "alertas abertos já têm veredito. Os números de risco abaixo são calculados do mesmo jeito "
      + "sobre a execução vigente.",

    openAlertsTitle: "Alertas abertos",
    openAlertsHint: "Pendentes de veredito, por faixa de severidade.",
    openAlertsNone: "sem alertas",
    amountAtRiskTitle: "Valor em risco",
    amountAtRiskHint:
      "Valor dos pedidos com alerta aberto. Uma linha por moeda: não existe um total.",
    amountAtRiskNone: "Não há nenhum alerta aberto com valor associado.",
    alertCount: (count: number, formatted: string) =>
      count === 1 ? "1 alerta" : `${formatted} alertas`,
    reportedFraudTitle: "Fraude reportada",
    reportedFraudHint: "Vereditos da analista, agregados por pedido distinto. Também por moeda.",
    reportedFraudNone: "Ainda ninguém marcou um alerta como fraude nesta base.",
    orderCount: (count: number, formatted: string) =>
      count === 1 ? "1 pedido" : `${formatted} pedidos`,
    flagRateTitle: "Taxa de marcação",
    flagRateNone:
      "A execução vigente não cobriu nenhum pedido, então não há proporção a calcular.",
    flagRateHint: (count: string) =>
      `Proporção dos ${count} pedidos da execução vigente que as regras negaram.`,
    topSignalsTitle: "Principais sinais",
    topSignalsHint:
      "Regras presentes no snapshot dos alertas abertos, não em todas as avaliações.",
    topSignalsNone: "Nenhum alerta aberto, então nenhum sinal.",

    denialsTitle: "Negados pelo provedor sem alerta local",
    denialsHint:
      "Pedidos que o provedor externo negou e que o motor local nunca marcou. É a única tela onde "
      + "aparece um pedido sem alerta.",
    denialsNone:
      "Nenhum pedido negado pelo provedor ficou fora da fila. Ou ninguém pediu ainda a avaliação "
      + "externa, ou o provedor e o motor local coincidiram em tudo.",
    denialsOrderWord: (count: number) => (count === 1 ? "pedido" : "pedidos"),
    denialsListed: (count: string) => ` · são listados os ${count} mais recentes`,
    denialsCaption: "Pedidos negados pelo provedor externo que não abriram nenhum alerta local",
    denialsColumnOrder: "Pedido",
    denialsColumnDate: "Data",
    denialsColumnAmount: "Valor",
    denialsColumnCountry: "País",
    denialsColumnScore: "Score local",
    denialsUnscored: "sem pontuar",

    riskOverTimeTitle: "Risco ao longo do tempo",
    riskOverTimeHint:
      "Por semana de ocorrência do pedido, no horário de Montevidéu: o mesmo fuso com que as regras "
      + "decidem a que dia cada pedido pertence.",
    riskOverTimeNone:
      "A execução vigente não cobriu nenhum pedido, então não há semanas a desenhar.",
    chartTitle: "Pedidos por semana e quantos deles a execução vigente negou",
    chartDescription: (weeks: string, range: string, orders: string, flagged: string) =>
      `${weeks} semanas${range}. ${orders} pedidos no total, dos quais ${flagged} ficaram negados. `
      + "Os mesmos números estão na tabela que segue o gráfico.",
    chartRange: (from: string, to: string) => `, de ${from} a ${to}`,
    chartBar: (week: string, orders: string, flagged: string) =>
      `Semana de ${week}: ${orders} pedidos, ${flagged} negados`,
    chartLegendOrders: "Pedidos da semana",
    chartLegendFlagged: "Negados pela execução vigente",
    chartTableCaption: "Pedidos e negados por semana, os mesmos dados que o gráfico desenha.",
    chartColumnWeek: "Semana",
    chartColumnOrders: "Pedidos",
    chartColumnFlagged: "Negados",
    chartColumnShare: "Proporção",

    qualityTitle: "Qualidade do critério",
    qualityLead:
      "Medida contra os rótulos do corpus de demonstração. Nenhum número do resto desta tela usa "
      + "esses rótulos.",
    qualityCaveat:
      "A fixture de demonstração tem erros colocados à mão: três falsos negativos que nenhuma "
      + "regra consegue ver e quatro falsos positivos. Por isso o F1 não vale 1,00, e por isso "
      + "também não mede o critério: com os erros colocados de propósito, o F1 é um parâmetro "
      + "escolhido e não um resultado. Estas métricas provam o pipeline de avaliação —divisão "
      + "temporal, holdout sem retuning, cálculo correto—, não a qualidade da detecção.",
    qualityScoredOrders: "Pedidos pontuados",
    qualityLabeled: "Com rótulo",
    qualityUnlabeled: "Sem rótulo",
    qualityUnlabeledHint: "São contados e excluídos: um pedido importado nunca traz rótulo.",
    qualityRuleConfig: "Configuração de regras",
    qualityHoldoutTitle: (count: string) => `Holdout · ${count} pedidos`,
    qualityHoldoutHint: (threshold: string) =>
      `Limiar ${threshold}, escolhido sobre a coorte de calibração e aplicado aqui sem retocar nada.`,
    qualityCalibrationTitle: (count: string) => `Calibração · ${count} pedidos`,
    qualityCalibrationHint:
      "A coorte inicial, sobre a qual o limiar foi escolhido. Não é uma medição independente.",
    qualityUndefined: "sem definição",
    qualityPrecision: "Precisão",
    qualityRecall: "Recall",
    qualityF1: "F1",
    qualityFalsePositiveRate: "Taxa de falsos positivos",
    qualityMatrixCaption: "Matriz de confusão",
    qualityTruePositives: "Verdadeiros positivos",
    qualityFalsePositives: "Falsos positivos",
    qualityFalseNegatives: "Falsos negativos",
    qualityTrueNegatives: "Verdadeiros negativos",
    qualityRunFootnote: (sequence: string, instant: string) => `Execução nº ${sequence}, ${instant}.`,
    qualitySweepSummary: (points: string) =>
      `Varredura de limiares sobre a coorte de calibração (${points} pontos)`,
    qualitySweepHint: (threshold: string) =>
      `Apenas os limiares onde a matriz de confusão muda. O limiar ${threshold} é o que foi `
      + "escolhido e o que foi aplicado ao holdout.",
    qualitySweepCaption:
      "Precisão, recall, F1 e taxa de falsos positivos em cada limiar da coorte de calibração",
    qualitySweepThreshold: "Limiar",
    qualitySweepChosen: " · escolhido",
  },

  importPage: {
    title: "Importação e scoring",
    lead:
      "Importar escreve pedidos e nada mais. As avaliações e os alertas são produzidos por uma "
      + "execução de scoring, que é disparada daqui: enquanto você não a executar, a fila de alertas "
      + "e o painel continuam mostrando o estado da execução anterior.",
    corpusStatusTitle: "Estado do corpus",
    corpusPending: (count: number, formatted: string) =>
      count === 1
        ? "Há 1 pedido sem pontuar pela execução vigente."
        : `Há ${formatted} pedidos sem pontuar pela execução vigente.`,
    corpusPendingHint:
      "Não aparecem no painel nem podem gerar alertas até que você execute uma corrida.",
    corpusEmpty: "Não há pedidos sem pontuar porque ainda não há nenhum na base.",
    corpusCovered: "Todos os pedidos da base estão cobertos pela execução vigente.",

    seedTitle: "Corpus de demonstração",
    seedDescription:
      "Trezentos pedidos sintéticos com seus rótulos de fraude, pensados para que o critério possa "
      + "ser medido: inclui fraude que as regras locais não conseguem ver e pedidos legítimos que "
      + "marcam. A carga é idempotente: repeti-la não duplica nada. Esta seção existe só porque "
      + "esta instância se declara de demonstração.",
    seedConflictPreviousTitle: "Esta base tem uma versão anterior do corpus de demonstração",
    seedConflictPreviousBody: (version: string) =>
      `Carregar a versão ${version} exige uma base nova: um pedido é imutável, então as duas `
      + "versões não podem conviver sob as mesmas referências de estabelecimento. A base atual não "
      + "é tocada nem perdida: deixa de ser a de demonstração.",
    seedConflictImportedTitle: "Esta base tem pedidos importados com as mesmas referências",
    seedConflictImportedBody:
      "Os pedidos que já estão lá usam as mesmas referências da fixture e têm outros dados. A carga "
      + "é cancelada inteira em vez de sobrescrever qualquer um deles.",
    seedButton: "Carregar corpus de demonstração",
    seedButtonPending: "Carregando corpus…",

    fileTitle: "Importar um arquivo",
    fileDescription:
      "CSV ou JSON. A validação é estrita por registro e a escrita atômica por arquivo: os registros "
      + "rejeitados são listados um a um e nenhum deles é escrito.",
    fileLabel: "Arquivo",
    fileHint: (maxMib: string) =>
      `Até ${maxMib} MiB e 10.000 pedidos por arquivo. Os dados devem ser sintéticos.`,
    formatLegend: "Formato",
    fileButton: "Importar pedidos",
    fileButtonPending: "Importando…",

    scoringTitle: "Executar scoring",
    scoringDescription:
      "Avalia o corpus inteiro em ordem temporal, com o baseline construído apenas com o histórico "
      + "anterior a cada pedido, e abre os alertas que couberem. É idempotente: uma avaliação cujo "
      + "resultado não mudou é reaproveitada em vez de duplicada, e um alerta já aberto ou já "
      + "revisado não é aberto de novo.",
    scoringButton: "Executar scoring",
    scoringButtonPending: "Executando corrida…",
    scoringRunning:
      "A execução avalia o corpus em ordem temporal. Em um corpus de trezentos pedidos leva alguns "
      + "segundos.",

    externalTitle: "Provedor antifraude externo",
    externalDescription:
      "Uma segunda opinião sobre cada pedido, de um provedor externo simulado. O detalhe de um "
      + "alerta permite pedi-la para um pedido de cada vez; aqui ela é pedida para o corpus inteiro, "
      + "que é a única coisa que alcança os pedidos que nunca abriram um alerta. Entregar os "
      + "callbacks simula a resposta que o provedor mandaria por conta própria: quem clica escolhe "
      + "qual avaliação, nunca o que o provedor responde. Repetir não repete efeitos.",
    externalRequestButton: "Solicitar avaliação externa do corpus",
    externalRequestPending: "Consultando o provedor…",
    externalRequestRunning:
      "A consulta é pedido a pedido, reservando a linha antes de chamar. Em um corpus de trezentos "
      + "pedidos leva alguns segundos.",
    externalDeliverButton: "Entregar os callbacks do provedor",
    externalDeliverPending: "Entregando callbacks…",

    rejectedTitle: (count: string) => `Registros rejeitados (${count})`,
    rejectedHint: "Nenhum destes foi escrito. Os pedidos válidos do mesmo arquivo sim.",
    rejectedTruncated:
      "A API parou de enumerar erros neste ponto: há mais registros rejeitados do que os listados "
      + "aqui.",
    recordAt: (record: string) => `Registro ${record}`,
    recordAtLine: (record: string, line: string) => `Registro ${record}, linha ${line}`,
    recordField: (field: string) => ` · campo ${field}`,
    recordError: (where: string, field: string, label: string, message: string) =>
      `${where}${field} · ${label}: ${message}`,
  },

  outcomes: {
    seedAlreadyLoadedTitle: "O corpus de demonstração já estava carregado",
    seedLoadedTitle: "Corpus carregado",
    seedAlreadyLoadedBody:
      "A carga é idempotente: os pedidos já estavam na base e nenhum foi duplicado.",
    seedLoadedBody: "Os pedidos ficaram na base. Ainda não têm avaliação nem alerta.",
    seedRecovery: "Execute uma corrida de scoring para pontuá-los.",
    seedFactVersion: (version: string) => `Versão da fixture: ${version}`,
    seedFactInserted: (inserted: string, total: string) =>
      `Pedidos inseridos: ${inserted} de ${total}`,
    seedFactDuplicates: (count: string) => `Pedidos já presentes: ${count}`,
    seedFactLabels: (inserted: string, total: string) =>
      `Rótulos inseridos: ${inserted} de ${total}`,

    importNoneTitle: "Nenhum pedido foi importado",
    importSomeTitle: (count: string) => `Foram importados ${count} pedidos`,
    importBody:
      "A importação é estrita por registro e atômica por arquivo: os pedidos válidos foram escritos "
      + "todos juntos e os rejeitados nunca foram escritos.",
    importRecovery:
      "Os pedidos novos não têm avaliação até que você execute uma corrida de scoring.",
    importFactRead: (count: string) => `Registros lidos: ${count}`,
    importFactImported: (count: string) => `Importados: ${count}`,
    importFactDuplicates: (count: string) =>
      `Duplicados, já presentes com os mesmos dados: ${count}`,
    importFactRejected: (count: string) => `Rejeitados: ${count}`,

    scoringTitle: (sequence: string) => `Execução nº ${sequence} concluída`,
    scoringBody:
      "Já existe uma avaliação vigente por pedido e os alertas que couberam ficaram abertos. Uma "
      + "avaliação reaproveitada é uma cujo resultado não mudou: mesmo corpus, mesma configuração, "
      + "mesmo resultado.",
    scoringRecovery: "Revise a fila de alertas ou olhe o painel.",
    scoringFactConfig: (version: string) => `Configuração de regras: ${version}`,
    scoringFactFinished: (instant: string) => `Terminou: ${instant}`,
    scoringFactOrders: (count: string) => `Pedidos avaliados: ${count}`,
    scoringFactCreated: (count: string) => `Avaliações criadas: ${count}`,
    scoringFactReused: (count: string) => `Avaliações reaproveitadas: ${count}`,
    scoringFactAlerts: (count: string) => `Alertas abertos: ${count}`,
    scoringFactSkippedOpen: (count: string) =>
      `Omitidos por já ter um alerta aberto: ${count}`,
    scoringFactSkippedReviewed: (count: string) => `Omitidos por já ter um veredito: ${count}`,

    corpusExternalKnownTitle: "O provedor já conhecia todos os pedidos",
    corpusExternalRequestedTitle: (count: string) => `Foram consultados ${count} pedidos`,
    corpusExternalBody:
      "Cada pedido passa pelo mesmo caso de uso de uma consulta avulsa: a linha é reservada antes de "
      + "chamar o provedor, então duas consultas simultâneas não criam duas avaliações do lado do "
      + "provedor.",
    corpusExternalSettledRecovery:
      "O veredito de cada pedido aparece no detalhe do seu alerta.",
    corpusExternalPendingRecovery:
      "Os que continuam aguardando o provedor são fechados entregando seus callbacks ou "
      + "reconciliando.",
    corpusExternalFactExamined: (count: string) => `Pedidos sem avaliação externa: ${count}`,
    corpusExternalFactRequested: (count: string) => `Consultados: ${count}`,
    corpusExternalFactSettled: (count: string) => `Com veredito na hora: ${count}`,
    corpusExternalFactPending: (count: string) => `Aguardando o provedor: ${count}`,
    corpusExternalFactSkipped: (count: string) => `Omitidos, já tinham avaliação: ${count}`,

    deliverNoneTitle: "Não há nenhuma avaliação externa aguardando o provedor",
    deliverReplayedTitle: (count: string) => `Os ${count} callbacks já tinham sido recebidos`,
    deliverDoneTitle: (count: string) => `Foram entregues ${count} callbacks`,
    deliverReplayedBody:
      "Cada mensagem é idêntica a uma já registrada, então nenhum efeito foi repetido: apenas ficou "
      + "anotado que o provedor as enviou de novo.",
    deliverDoneBody:
      "Os callbacks entram pelo mesmo caso de uso que o provedor usaria: mesmo recibo, mesma "
      + "deduplicação, mesmas regras de transição. Quem clica escolhe qual avaliação, nunca o que o "
      + "provedor responde.",
    deliverNoneRecovery:
      "Solicite avaliações externas do corpus para que haja algo a entregar.",
    deliverDoneRecovery: "O veredito do provedor aparece no detalhe de cada alerta.",
    deliverFactExamined: (count: string) => `Avaliações aguardando o provedor: ${count}`,
    deliverFactDelivered: (count: string) => `Callbacks entregues: ${count}`,
    deliverFactSettled: (count: string) => `Fecharam com veredito: ${count}`,
    deliverFactReplayed: (count: string) => `Já tinham sido recebidos: ${count}`,
    deliverFactUnavailable: (count: string) =>
      `Não puderam ser escritos por concorrência: ${count}`,

    externalRequestedTitle: "Avaliação externa solicitada",
    externalAlreadyTitle: "Este pedido já tinha uma avaliação externa",
    externalWaitingBody:
      "O provedor aceitou a consulta e ainda não decidiu: a resposta vai chegar por callback, ou ao "
      + "reconciliar.",
    externalSettledBody:
      "O veredito do provedor fica registrado junto ao critério local, sem se combinar com ele.",
    externalAlreadyRecovery:
      "Pedi-la de novo não cria uma segunda avaliação do lado do provedor.",
    externalCallbackReplayedTitle: "Esse callback já tinha sido recebido",
    externalCallbackDoneTitle: "Callback entregue",
    externalCallbackReplayedBody:
      "A mensagem é idêntica a uma já registrada, então nenhum efeito foi repetido: apenas ficou "
      + "anotado que o provedor a enviou de novo.",
    externalCallbackDoneBody:
      "O callback entrou pelo mesmo caminho que o provedor usaria: mesmo recibo, mesma "
      + "deduplicação, mesmas regras de transição.",
    externalCallbackRecovery: "O estado do provedor já consta acima.",

    explanationWrittenFirstTitle: "Explicação redigida",
    explanationWrittenRetryTitle: "Explicação redigida na nova tentativa",
    explanationWrittenCurrentTemplateTitle: "Redigida de novo com o modelo vigente",
    explanationWrittenBody:
      "O texto ficou guardado junto à avaliação e já é exibido acima. Uma explicação escrita não é "
      + "reescrita: se o pedido for avaliado de novo, a avaliação nova leva a sua.",
    explanationWrittenCurrentTemplateBody:
      "O texto novo foi escrito ao lado do anterior e é o que aparece acima. O anterior continua "
      + "guardado sem mudanças, porque é o registro do que se pôde ler enquanto o veredito se "
      + "formava.",
    explanationUnchangedTitle: "Esta avaliação já tinha sua explicação",
    explanationUnchangedBody:
      "Nada foi pedido ao provedor: a que já estava escrita é a que aparece acima.",
    explanationFailedTitle: "Não foi possível redigir a explicação",
    explanationFailedWithoutCode: "O provedor não deixou nenhum texto utilizável.",
    explanationFailedWithCode: (label: string) => `${label}.`,
    explanationFailedExhausted:
      "As tentativas para esta avaliação se esgotaram. O veredito não precisa de uma explicação para "
      + "ser emitido.",
    explanationFailedRetry:
      "Você pode tentar de novo. Um texto que não pôde ser verificado não é guardado nem exibido.",

    reviewUnchangedTitle: "O alerta já tinha exatamente este veredito",
    reviewUnchangedBody:
      "Nenhuma revisão nova foi registrada porque o veredito e a nota guardados coincidem com os que "
      + "você enviou.",
    reviewUnchangedRecovery: "O veredito vigente é o que aparece abaixo.",
    reviewAppliedTitle: "Veredito registrado",
    reviewAppliedBody:
      "A revisão ficou guardada junto com sua auditoria. Um veredito é definitivo: este alerta não é "
      + "reaberto.",
  },

  failures: {
    ALERT_NOT_FOUND: {
      title: "Este alerta não existe mais",
      body: "O alerta que você pediu não está na base. Um corpus novo pode ter sido importado desde que você abriu o link.",
      recovery: "Volte para a fila de alertas e escolha um da fila vigente.",
    },
    INVALID_STATUS: {
      title: "O veredito não é válido",
      body: "Um alerta só pode ser marcado como «Seguro» ou «Fraude reportada».",
      recovery: "Escolha um dos dois vereditos e envie de novo.",
    },
    NOTE_TOO_LONG: {
      title: "A nota é longa demais",
      body: "A nota de revisão admite até 2000 caracteres.",
      recovery: "Encurte a nota e envie de novo. O que você escreveu continua no campo.",
    },
    ALERT_ALREADY_REVIEWED: {
      title: "Outra pessoa já revisou este alerta",
      body: "O alerta foi fechado com um veredito diferente do que você enviou. Um veredito é definitivo: não é reaberto.",
      recovery: `${RELOAD} Se o critério mudou, é preciso um alerta novo sobre o pedido.`,
    },
    ALERT_REVIEW_NOTE_CONFLICT: {
      title: "O alerta já tem este veredito, com outra nota",
      body: "O veredito registrado coincide com o que você enviou, mas a nota guardada é diferente e não é sobrescrita.",
      recovery: `${RELOAD} A nota registrada aparece no bloco de revisão.`,
    },
    ALERT_DIVERGENCE_NOT_ACKNOWLEDGED: {
      title: "O corpus mudou desde que o alerta foi aberto",
      body: "A avaliação vigente do pedido está em outra faixa de severidade que a do snapshot com que o alerta foi aberto. A API não aceita um veredito sem que você reconheça isso.",
      recovery: "Volte ao aviso de divergência, leia o que mudou e marque a caixa antes de enviar.",
    },
    ALERT_REVIEW_CONFLICT: {
      title: "Duas revisões ao mesmo tempo",
      body: "Outra revisão sobre este alerta foi salva enquanto a sua era processada, então a sua não foi aplicada.",
      recovery: `${RELOAD} Se você mantém o mesmo critério, envie de novo.`,
    },
    SCORING_RUN_CONFLICT: {
      title: "Outra execução de scoring rodou ao mesmo tempo",
      body: "Duas execuções escreveram estado em conflito, então a sua não foi salva. O corpus ficou como estava antes da tentativa: não há avaliações nem alertas pela metade.",
      recovery: "Execute a corrida de novo. Se mais alguém estiver usando o console, espere terminar.",
    },
    METRICS_UNAVAILABLE: {
      title: "Ainda não é possível calcular as métricas de qualidade",
      body: "Medir o critério exige uma execução de scoring e pedidos rotulados dos dois lados da divisão temporal. Falta uma das duas coisas.",
      recovery: "Execute uma corrida de scoring sobre o corpus de demonstração na tela de importação.",
    },
    DEMO_DATA_CONFLICT: {
      title: "O corpus de demonstração colide com pedidos que já existem",
      body: "A base tem pedidos importados com as mesmas referências da fixture mas com dados diferentes. Um pedido é imutável, então a carga é cancelada inteira em vez de sobrescrever qualquer coisa.",
      recovery: "Use uma base vazia para carregar a demonstração, ou siga com os pedidos que já estão importados.",
    },
    DEMO_DATA_PREVIOUS_CORPUS: {
      title: "Esta base tem uma versão anterior do corpus de demonstração",
      body: "As duas versões usam as mesmas referências de estabelecimento e um pedido é imutável, então não podem conviver. Nada foi escrito: a base ficou como estava.",
      recovery: "Carregue o corpus em uma base nova. A base atual não se perde; deixa de ser a de demonstração.",
    },
    FILE_REQUIRED: {
      title: "Nenhum arquivo chegou",
      body: "O formulário foi enviado sem arquivo, ou com um de zero bytes.",
      recovery: "Escolha um arquivo CSV ou JSON com ao menos um pedido e envie de novo.",
    },
    FILE_TOO_LARGE: {
      title: "O arquivo passa do máximo admitido",
      body: (maxMib: string) =>
        `A importação aceita até ${maxMib} MiB por arquivo. Nada do que você enviou foi `
        + "importado.",
      recovery: "Divida o arquivo em outros menores e importe um a um.",
    },
    TOO_MANY_RECORDS: {
      title: "O arquivo tem registros demais",
      body: "A importação aceita até 10.000 pedidos por arquivo. O documento inteiro é rejeitado: não se importa uma parte e se descarta o resto em silêncio.",
      recovery: "Divida o arquivo em lotes de até 10.000 registros.",
    },
    UNSUPPORTED_MEDIA_TYPE: {
      title: "O console enviou o formulário em um formato que a API não aceita",
      body: "A importação viaja como `multipart/form-data`. É um erro interno: não deveria acontecer nesta tela.",
      recovery: "Recarregue a página e tente de novo. Se acontecer outra vez, reporte com a hora exata.",
    },
    UNSUPPORTED_FORMAT: {
      title: "Esse formato não é suportado",
      body: "A importação admite CSV ou JSON, e é preciso declarar qual antes de enviar.",
      recovery: "Escolha CSV ou JSON conforme o arquivo e envie de novo.",
    },
    EMPTY_FILE: {
      title: "O arquivo não tem nenhum pedido",
      body: "Foi lido inteiro e não contém registros: pode ser um CSV só com a linha de cabeçalhos, ou um JSON com uma lista vazia.",
      recovery: "Revise o arquivo e envie de novo com ao menos um pedido.",
    },
    INVALID_ENCODING: {
      title: "O arquivo não está em UTF-8",
      body: "A importação lê UTF-8 e o arquivo traz bytes que não são. Nada foi importado.",
      recovery: "Exporte o arquivo de novo em UTF-8 e tente outra vez.",
    },
    INVALID_CSV: {
      title: "O CSV está malformado",
      body: "A estrutura do arquivo não pôde ser lida —aspas sem fechar, ou uma linha com mais campos que o cabeçalho—, então nenhuma linha foi importada.",
      recovery: "Corrija a estrutura do arquivo e envie de novo. O detalhe técnico abaixo diz onde falhou.",
    },
    INVALID_JSON: {
      title: "O JSON está malformado",
      body: "O arquivo não é JSON válido, então nenhum pedido foi importado.",
      recovery: "Valide o arquivo e envie de novo. O detalhe técnico abaixo diz onde falhou.",
    },
    INVALID_JSON_ROOT: {
      title: "O JSON não é uma lista de pedidos",
      body: "A importação espera um arranjo na raiz do documento, com um objeto por pedido.",
      recovery: "Envolva os pedidos em um arranjo `[ … ]` e envie de novo.",
    },
    MISSING_HEADER: {
      title: "Falta um cabeçalho obrigatório no CSV",
      body: "O arquivo não tem linha de cabeçalhos, ou falta alguma das colunas que a importação exige.",
      recovery: "Adicione a linha de cabeçalhos com todas as colunas obrigatórias. O detalhe técnico diz qual falta.",
    },
    INVALID_HEADER: {
      title: "Um cabeçalho do CSV está vazio",
      body: "Uma coluna do arquivo não tem nome, então não dá para saber que campo é.",
      recovery: "Nomeie todas as colunas do cabeçalho e envie de novo.",
    },
    DUPLICATE_HEADER: {
      title: "O CSV repete um cabeçalho",
      body: "Duas colunas do arquivo têm o mesmo nome e não há como decidir qual vale.",
      recovery: "Deixe uma só coluna por campo e envie de novo.",
    },
    UNKNOWN_HEADER: {
      title: "O CSV traz uma coluna que a importação não conhece",
      body: "O arquivo é rejeitado inteiro em vez de ignorar dados em silêncio: uma coluna desconhecida costuma ser um arquivo errado ou um campo mal escrito.",
      recovery: "Remova ou corrija a coluna. O detalhe técnico diz qual é.",
    },
    INVALID_SORT: {
      title: "O console pediu uma ordenação que a API não reconhece",
      body: "A fila pediu para ordenar os alertas por um valor que o contrato não admite. É um erro interno: não deveria acontecer nesta tela.",
      recovery: "Recarregue a página. Se acontecer outra vez, reporte com a hora exata.",
    },
    INVALID_SEVERITY: {
      title: "O console pediu uma severidade que a API não reconhece",
      body: "A fila filtrou por uma severidade que o contrato não admite. É um erro interno: não deveria acontecer nesta tela.",
      recovery: "Recarregue a página. Se acontecer outra vez, reporte com a hora exata.",
    },
    INVALID_PAGE: {
      title: "O console pediu uma página inexistente",
      body: "A fila pediu um número de página fora da faixa. É um erro interno: não deveria acontecer nesta tela.",
      recovery: "Recarregue a página. Se acontecer outra vez, reporte com a hora exata.",
    },
    EXTERNAL_EVALUATION_PENDING: {
      title: "Já há uma avaliação externa aguardando o provedor",
      body: "Este pedido tem uma avaliação externa que o provedor ainda não respondeu. Só pode haver uma por vez: pedir outra criaria uma segunda avaliação do lado do provedor para uma pergunta que já foi feita.",
      recovery: "Espere o callback do provedor, ou execute uma reconciliação para perguntar de novo.",
    },
    EXTERNAL_EVALUATION_SETTLED: {
      title: "O provedor já se pronunciou sobre este pedido",
      body: "A avaliação externa vigente tem veredito. Só é possível pedir de novo quando a anterior terminou em erro.",
      recovery: "O veredito do provedor é exibido no bloco de avaliação externa.",
    },
    EXTERNAL_EVALUATION_CONFLICT: {
      title: "Outra escrita tocou a avaliação externa ao mesmo tempo",
      body: "Mais alguém moveu esta avaliação externa enquanto o seu pedido era processado, então o seu não foi aplicado.",
      recovery: `${RELOAD} O estado exibido é o que ficou guardado.`,
    },
    EXTERNAL_EVALUATION_NOT_FOUND: {
      title: "Essa avaliação externa não existe mais",
      body: "A avaliação externa que se quis usar não está na base. Um corpus novo pode ter sido importado desde que você abriu o link.",
      recovery: RELOAD,
    },
    PROVIDER_NOT_REGISTERED: {
      title: "Esta instalação não tem adaptador para esse provedor",
      body: "O provedor externo pedido não está registrado nesta API. O MVP traz apenas o provedor simulado; o sandbox da Koin é pós-MVP.",
      recovery: "Revise a configuração da API. Com KOIN_MODE=mock fica registrado o provedor simulado.",
    },
    INVALID_PROVIDER: {
      title: "O console pediu um provedor que a API não reconhece",
      body: "O nome de provedor enviado não está no catálogo do contrato. É um erro interno: não deveria acontecer nesta tela.",
      recovery: "Recarregue a página. Se acontecer outra vez, reporte com a hora exata.",
    },
    RECONCILIATION_CONFLICT: {
      title: "Outra escrita levou todas as avaliações da varredura",
      body: "Cada avaliação que a varredura examinou foi movida por outra escrita antes que pudesse salvar a sua, então a varredura não aplicou nada.",
      recovery: "Execute de novo. Se mais alguém estiver usando o console, espere terminar.",
    },
    CALLBACK_UNAUTHORIZED: {
      title: "A API rejeitou o callback por autenticação",
      body: "O endpoint de callbacks exige um segredo compartilhado e ele não chegou, ou não confere. É um erro interno: este console nunca envia callbacks, porque o segredo não vive no processo do Next.",
      recovery: "Revise SALVO_CALLBACK_SHARED_SECRET na configuração da API. Se acontecer outra vez nesta tela, reporte com a hora exata.",
    },
    CALLBACK_UNAVAILABLE: {
      title: "A avaliação externa está sendo escrita por outra requisição",
      body: "O callback não pôde ser salvo porque outra escrita ganhou a corrida duas vezes seguidas. Nada foi aplicado pela metade: ou entra inteiro ou não entra.",
      recovery: "Tente de novo em alguns segundos.",
    },
    INVALID_CALLBACK: {
      title: "O corpo do callback não é válido",
      body: "Falta com o que correlacionar a mensagem, ou seu estado não é um dos admitidos. É um erro interno: este console não compõe callbacks.",
      recovery: "Recarregue a página. Se acontecer outra vez, reporte com a hora exata.",
    },
    CALLBACK_TOO_LARGE: {
      title: "O corpo do callback passa do máximo admitido",
      body: "Um callback pesa alguns poucos bytes e a API fixa um limite explícito. O corpo enviado o excede.",
      recovery: "Revise o que o provedor está enviando. Se acontecer outra vez nesta tela, reporte com a hora exata.",
    },
    EXPLANATION_PENDING: {
      title: "A explicação está sendo redigida agora",
      body: "Já há uma requisição em curso para esta avaliação e ela ainda não respondeu. Pedir outra pagaria duas vezes a mesma redação.",
      recovery: "Espere alguns segundos e recarregue o alerta: o texto aparece no bloco de explicação.",
    },
    EXPLANATION_ALREADY_READY: {
      title: "Esta avaliação já tem sua explicação escrita",
      body: "Uma explicação escrita não é regerada. É o registro do que se pôde ler ao decidir, e substituí-la apagaria o texto que alguém pode ter tido diante dos olhos.",
      recovery: "O texto vigente é exibido no bloco de explicação. Uma avaliação diferente tem a sua própria explicação.",
    },
    EXPLANATION_ATTEMPTS_EXHAUSTED: {
      title: "As tentativas de explicar esta avaliação se esgotaram",
      body: "A redação falhou todas as vezes que o orçamento de tentativas permite. O teto existe para que um provedor que falha em cadeia não cobre indefinidamente.",
      recovery: "Revise o motivo da última tentativa no bloco de explicação. O veredito não precisa de uma explicação para ser emitido.",
    },
    EXPLANATION_CONFLICT: {
      title: "Outra escrita tocou a explicação ao mesmo tempo",
      body: "Mais alguém moveu esta explicação enquanto o seu pedido era processado, então o seu não foi aplicado.",
      recovery: `${RELOAD} O estado exibido é o que ficou guardado.`,
    },
    INVALID_PAGE_SIZE: {
      title: "O console pediu um tamanho de página fora da faixa",
      body: "A fila pediu mais alertas por página do que a API entrega. É um erro interno: não deveria acontecer nesta tela.",
      recovery: "Recarregue a página. Se acontecer outra vez, reporte com a hora exata.",
    },
  },

  transport: {
    timeoutTitle: "A API demorou demais para responder",
    timeoutBody:
      "A consulta foi cancelada para não deixar a tela travada. Nada chegou a ser lido nem salvo.",
    timeoutRecovery:
      "Tente de novo. Se persistir, verifique se o processo da API está respondendo.",
    unreachableTitle: "Não foi possível contatar a API",
    unreachableBody:
      "O console não tem com o que trabalhar: todo o risco é calculado no backend e agora mesmo ele "
      + "não responde.",
    unreachableRecovery:
      "Verifique se a API está no ar no endereço configurado e recarregue.",
    malformedTitle: "A resposta da API não corresponde ao contrato",
    malformedBody:
      "Chegou uma resposta em que falta ou sobra algo a este console em relação ao contrato com que "
      + "ele foi construído. Nada é exibido antes de exibir algo mal lido.",
    malformedRecovery:
      "É provável que a API e o console estejam em versões diferentes. Gere os tipos de novo a "
      + "partir do OpenAPI e faça o deploy outra vez.",
    unknownTitle: "A API rejeitou a operação",
    unknownBody: (status: string) =>
      `Respondeu ${status} com um motivo que este console ainda não traduz.`,
    unknownRecovery:
      "Recarregue a página. Se acontecer outra vez, reporte com a hora exata e o detalhe técnico.",
  },
};
