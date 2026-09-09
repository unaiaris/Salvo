# Glosario castellano — portugués

> **Este portugués no lo revisó un hablante nativo.** Lo escribió la misma persona que escribió
> el castellano, y está publicado así a propósito: una traducción sin revisar y declarada es
> honesta; una sin revisar y presentada como terminada, no.

Una fila por clave del diccionario. Para corregir una, hay que editar la clave con ese nombre en
`frontend/src/lib/i18n/pt.ts` y volver a generar esta tabla con
`node --experimental-strip-types frontend/src/lib/i18n/glosario.mjs`. No hace falta abrir ningún
otro archivo: el tipo `Dictionary` garantiza que las claves de los dos idiomas son exactamente
las mismas, así que esta tabla no puede quedar incompleta sin que el proyecto deje de compilar.

Las claves con `{algo}` entre llaves llevan un valor que se calcula: un monto, una cantidad, una
fecha. El texto de alrededor es lo traducible; el valor lo escribe el código y es el mismo en los
dos idiomas.

**Vocabulario elegido.** «comercio» es *estabelecimento*, la palabra del mercado adquirente
brasileño; «monto» es *valor*; «corrida» es *execução*; «veredicto» es *veredito*; «denegado» es
*negado*; «etiqueta» es *rótulo*.

Entradas: **538**.

| Clave | Castellano | Português |
| --- | --- | --- |
| `meta.title` | Salvo | Salvo |
| `meta.description` | Consola antifraude B2B con scoring determinista y auditable. | Console antifraude B2B com scoring determinístico e auditável. |
| `meta.alerts` | Alertas · Salvo | Alertas · Salvo |
| `meta.alertDetail` | Alerta · Salvo | Alerta · Salvo |
| `meta.dashboard` | Dashboard · Salvo | Painel · Salvo |
| `meta.import` | Importación · Salvo | Importação · Salvo |
| `nav.brand` | Salvo | Salvo |
| `nav.label` | Secciones de la consola | Seções do console |
| `nav.alerts` | Alertas | Alertas |
| `nav.import` | Importación | Importação |
| `nav.dashboard` | Dashboard | Painel |
| `nav.disclaimer` | Datos sintéticos · sin autenticación · uso local | Dados sintéticos · sem autenticação · uso local |
| `nav.disclaimerShared` | Datos sintéticos · sin autenticación · instancia compartida | Dados sintéticos · sem autenticação · instância compartilhada |
| `sharedInstance.label` | Sobre esta instancia | Sobre esta instância |
| `sharedInstance.title` | Instancia de demostración compartida. | Instância de demonstração compartilhada. |
| `sharedInstance.synthetic` | Los datos son sintéticos. | Os dados são sintéticos. |
| `sharedInstance.everyoneSees` | Lo que escribas acá lo ve todo el mundo: las notas de revisión, los veredictos y los pedidos que importes. | O que você escrever aqui todo mundo vê: as notas de revisão, os vereditos e os pedidos que você importar. |
| `sharedInstance.resets` | Todo se reinicia cuando la instancia queda un rato sin visitas. | Tudo é reiniciado quando a instância fica um tempo sem visitas. |
| `sharedInstance.resetsWithMaximum` | Todo se reinicia cuando la instancia queda un rato sin visitas, y como máximo cada {minutes} minutos. | Tudo é reiniciado quando a instância fica um tempo sem visitas, e no máximo a cada {minutes} minutos. |
| `home.eyebrow` | Consola antifraude | Console antifraude |
| `home.title` | Salvo concentra el riesgo antifraude en un núcleo .NET auditable. | O Salvo concentra o risco antifraude em um núcleo .NET auditável. |
| `home.lead` | Las reglas, el scoring y las transacciones viven en el backend. Esta interfaz lee un contrato OpenAPI y no calcula riesgo por su cuenta. | As regras, o scoring e as transações vivem no backend. Esta interface lê um contrato OpenAPI e não calcula risco por conta própria. |
| `home.cta` | Ir a la cola de alertas | Ir para a fila de alertas |
| `common.technicalDetail` | Detalle técnico de la API: {detail} | Detalhe técnico da API: {detail} |
| `common.goToImport` | Ir a importación | Ir para importação |
| `common.runScoring` | Ejecutar scoring | Executar scoring |
| `common.viewDashboard` | Ver el dashboard | Ver o painel |
| `provenance.noRun` | El corpus todavía no se puntuó: no hay ninguna corrida de scoring. | O corpus ainda não foi pontuado: não há nenhuma execução de scoring. |
| `provenance.run` | corrida #{sequence}, {completedAt} | execução nº {sequence}, {completedAt} |
| `provenance.since` | Vigente desde la {run}. | Vigente desde a {run}. |
| `severity.CRITICAL` | CRÍTICA | CRÍTICA |
| `severity.HIGH` | ALTA | ALTA |
| `severity.MEDIUM` | MEDIA | MÉDIA |
| `severity.none` | SIN BANDA | SEM FAIXA |
| `status.OPEN` | Abierta | Aberto |
| `status.CONFIRMED_SAFE` | Confirmada segura | Confirmado seguro |
| `status.REPORTED_FRAUD` | Fraude reportado | Fraude reportada |
| `rules.amount_anomaly` | Monto atípico | Valor atípico |
| `rules.velocity` | Ráfaga de pedidos | Rajada de pedidos |
| `rules.cross_border_velocity` | Ráfaga entre países | Rajada entre países |
| `rules.unusual_hour` | Hora inusual | Horário incomum |
| `rules.new_buyer_high_value` | Comprador sin historia y monto alto | Comprador sem histórico e valor alto |
| `rules.foreign_country` | País distinto del habitual | País diferente do habitual |
| `signals.scopeBuyer` | del comprador | do comprador |
| `signals.scopeMerchant` | del comercio | do estabelecimento |
| `signals.amountAnomaly` | El monto, {amount}, es {ratio} veces la mediana {scope}, {median}, calculada sobre {history} pedidos previos de los últimos {windowDays} días. | O valor, {amount}, é {ratio} vezes a mediana {scope}, {median}, calculada sobre {history} pedidos anteriores dos últimos {windowDays} dias. |
| `signals.velocity` | Hubo {orders} pedidos del mismo comprador dentro de {windowMinutes} minutos; el umbral es {threshold}. | Houve {orders} pedidos do mesmo comprador em {windowMinutes} minutos; o limiar é {threshold}. |
| `signals.crossBorderVelocity` | El país cambió de {from} a {to} en {minutes} minutos, con el mismo comercio y el mismo comprador. | O país mudou de {from} para {to} em {minutes} minutos, com o mesmo estabelecimento e o mesmo comprador. |
| `signals.unusualHour` | La franja de {start} a {end}, hora del comercio, aparece en {observed} de {total} pedidos previos del comercio: un {share} %. | A faixa das {start} às {end}, hora do estabelecimento, aparece em {observed} de {total} pedidos anteriores do estabelecimento: {share} %. |
| `signals.newBuyerHighValue` | El comprador no tenía pedidos previos con este comercio, y el monto, {amount}, es {ratio} veces la mediana del comercio, {median}, sobre {history} pedidos previos. | O comprador não tinha pedidos anteriores com este estabelecimento, e o valor, {amount}, é {ratio} vezes a mediana do estabelecimento, {median}, sobre {history} pedidos anteriores. |
| `signals.foreignCountry` | El país del pedido, {country}, difiere del habitual del comercio, {habitual}, observado en {observed} de {total} pedidos previos: un {share} %. | O país do pedido, {country}, difere do habitual do estabelecimento, {habitual}, observado em {observed} de {total} pedidos anteriores: {share} %. |
| `importErrors.REQUIRED` | Falta un campo obligatorio | Falta um campo obrigatório |
| `importErrors.INVALID_FORMAT` | El valor no tiene el formato esperado | O valor não tem o formato esperado |
| `importErrors.OUT_OF_RANGE` | El valor está fuera del rango admitido | O valor está fora da faixa admitida |
| `importErrors.UNSUPPORTED_VALUE` | El valor no es uno de los admitidos | O valor não é um dos admitidos |
| `importErrors.REFERENCE_CONFLICT` | La referencia ya existe con otros datos | A referência já existe com outros dados |
| `externalStatus.PENDING` | Esperando al proveedor | Aguardando o provedor |
| `externalStatus.APPROVED` | Aprobado por el proveedor | Aprovado pelo provedor |
| `externalStatus.DENIED` | Denegado por el proveedor | Negado pelo provedor |
| `externalStatus.ERROR` | La consulta al proveedor falló | A consulta ao provedor falhou |
| `externalSource.SYNC` | en la misma respuesta del proveedor | na própria resposta do provedor |
| `externalSource.CALLBACK` | por callback del proveedor | por callback do provedor |
| `externalSource.RECONCILIATION` | al reconciliar, preguntándole de nuevo | ao reconciliar, perguntando de novo |
| `externalError.UNREACHABLE` | No se pudo contactar al proveedor: la consulta nunca salió | Não foi possível contatar o provedor: a consulta nunca saiu |
| `externalError.PROVIDER_REJECTED` | El proveedor rechazó la consulta | O provedor rejeitou a consulta |
| `externalError.TIMEOUT` | El proveedor no respondió a tiempo | O provedor não respondeu a tempo |
| `externalError.PROVIDER_ERROR` | El proveedor respondió con un error | O provedor respondeu com um erro |
| `externalError.INVALID_RESPONSE` | La respuesta del proveedor no se pudo leer | A resposta do provedor não pôde ser lida |
| `explanationFailure.PROVIDER_UNAVAILABLE` | No se pudo redactar: el proveedor falló antes de responder | Não foi possível redigir: o provedor falhou antes de responder |
| `explanationFailure.PROVIDER_TIMEOUT` | El proveedor no respondió dentro del tiempo permitido | O provedor não respondeu dentro do tempo permitido |
| `explanationFailure.PROVIDER_REFUSED` | El proveedor respondió sin texto | O provedor respondeu sem texto |
| `explanationFailure.MALFORMED_OUTPUT` | El texto devuelto no era utilizable: vino vacío o con marcado | O texto devolvido não era utilizável: veio vazio ou com marcação |
| `explanationFailure.NOT_GROUNDED_NUMBER` | El texto traía una cifra que la evaluación no respalda, así que se descartó entero | O texto trazia um número que a avaliação não sustenta, então foi descartado inteiro |
| `explanationFailure.NOT_GROUNDED_RULE` | El texto nombraba una regla que esta evaluación no disparó, así que se descartó entero | O texto citava uma regra que esta avaliação não disparou, então foi descartado inteiro |
| `explanationFailure.TOO_LONG` | El texto superó el largo máximo admitido | O texto passou do comprimento máximo admitido |
| `explanationFailure.CANCELLED` | La petición se abandonó antes de que el proveedor respondiera | A requisição foi abandonada antes de o provedor responder |
| `explanationFailure.ATTEMPT_LIMIT_REACHED` | Se agotaron los intentos de redacción para esta evaluación | As tentativas de redação para esta avaliação se esgotaram |
| `explanationFailure.LEGACY_SIGNAL_FORMAT` | Esta evaluación guarda sus señales en el formato anterior del motor, que no se puede verificar: no se le pidió nada a ningún proveedor | Esta avaliação guarda seus sinais no formato anterior do motor, que não pode ser verificado: nada foi pedido a nenhum provedor |
| `explanationProvider.MOCK` | plantilla determinista | modelo determinístico |
| `explanationProvider.ANTHROPIC` | Anthropic | Anthropic |
| `provider.EXTERNAL_MOCK` | Proveedor simulado | Provedor simulado |
| `provider.KOIN_SANDBOX` | Koin sandbox | Koin sandbox |
| `alertsPage.title` | Cola de alertas | Fila de alertas |
| `alertsPage.lead` | Alertas abiertas, de mayor a menor score local de la evaluación vigente. La severidad de la tabla es la del snapshot con el que se abrió cada alerta; el score vigente es lo que dice el corpus ahora. | Alertas abertos, do maior para o menor score local da avaliação vigente. A severidade da tabela é a do snapshot com que cada alerta foi aberto; o score vigente é o que o corpus diz agora. |
| `alertsPage.openCount` | {formatted} alertas abiertas | {formatted} alertas abertos |
| `alertsPage.tableCaption` | Alertas abiertas, de mayor a menor score local de la evaluación vigente | Alertas abertos, do maior para o menor score local da avaliação vigente |
| `alertsPage.columnOrder` | Pedido | Pedido |
| `alertsPage.columnSnapshotSeverity` | Severidad del snapshot | Severidade do snapshot |
| `alertsPage.columnSnapshotScore` | Score del snapshot | Score do snapshot |
| `alertsPage.columnCurrentScore` | Score vigente | Score vigente |
| `alertsPage.columnAmount` | Monto | Valor |
| `alertsPage.columnOccurred` | Ocurrió | Ocorreu |
| `alertsPage.buyerAndCountry` | Comprador {buyer} · {country} | Comprador {buyer} · {country} |
| `alertsPage.bandChanged` | La banda vigente cambió | A faixa vigente mudou |
| `alertsPage.noCurrentEvaluation` | sin evaluación vigente | sem avaliação vigente |
| `alertsPage.emptyNoOrdersTitle` | Todavía no hay pedidos | Ainda não há pedidos |
| `alertsPage.emptyNoOrdersBody` | La base está vacía, así que no hay nada que puntuar ni nada que revisar. Importá un archivo CSV o JSON, o cargá el corpus de demostración. | A base está vazia, então não há nada a pontuar nem nada a revisar. Importe um arquivo CSV ou JSON, ou carregue o corpus de demonstração. |
| `alertsPage.emptyNoRunTitle` | Hay pedidos importados y ninguna corrida de scoring | Há pedidos importados e nenhuma execução de scoring |
| `alertsPage.emptyNoRunBody` | Los {orderCount} pedidos de la base todavía no se puntuaron, así que no existen evaluaciones ni alertas. La cola se llena recién después de ejecutar una corrida. | Os {orderCount} pedidos da base ainda não foram pontuados, então não existem avaliações nem alertas. A fila só se enche depois de executar uma corrida. |
| `alertsPage.emptyNoAlertsTitle` | No hay alertas abiertas | Não há alertas abertos |
| `alertsPage.emptyNoAlertsBody` | La {run} no dejó ninguna alerta pendiente de revisión: ningún pedido alcanzó el umbral, o todas las alertas abiertas ya tienen veredicto. | A {run} não deixou nenhum alerta pendente de revisão: nenhum pedido alcançou o limiar, ou todos os alertas abertos já têm veredito. |
| `alertDetail.back` | ← Volver a la cola de alertas | ← Voltar para a fila de alertas |
| `alertDetail.title` | Alerta sobre {reference} | Alerta sobre {reference} |
| `alertDetail.opened` | Abierta el {instant} · política {policy} | Aberto em {instant} · política {policy} |
| `alertDetail.supersedes` | escala una alerta anterior | escala um alerta anterior |
| `alertDetail.orderTitle` | Pedido | Pedido |
| `alertDetail.orderReference` | Referencia del comercio | Referência do estabelecimento |
| `alertDetail.orderBuyer` | Comprador | Comprador |
| `alertDetail.orderAmount` | Monto | Valor |
| `alertDetail.orderOccurred` | Ocurrió | Ocorreu |
| `alertDetail.orderOrigin` | Origen | Origem |
| `alertDetail.orderDeviceSession` | Sesión de dispositivo | Sessão do dispositivo |
| `alertDetail.orderDeviceSessionAbsent` | sin registrar | sem registro |
| `alertDetail.noSignals` | Esta evaluación no disparó ninguna regla. | Esta avaliação não disparou nenhuma regra. |
| `alertDetail.snapshotTitle` | Snapshot que abrió la alerta | Snapshot que abriu o alerta |
| `alertDetail.snapshotHint` | Congelado el {instant}. No se reescribe nunca: es la premisa sobre la que se forma el veredicto. | Congelado em {instant}. Nunca é reescrito: é a premissa sobre a qual o veredito se forma. |
| `alertDetail.currentTitle` | Evaluación vigente | Avaliação vigente |
| `alertDetail.currentNoRun` | El corpus no tiene ninguna corrida de scoring. | O corpus não tem nenhuma execução de scoring. |
| `alertDetail.currentAbsent` | No hay evaluación vigente para este pedido. No es un score de cero: la corrida vigente no dejó ninguna evaluación asociada a este pedido. | Não há avaliação vigente para este pedido. Não é um score zero: a execução vigente não deixou nenhuma avaliação associada a este pedido. |
| `alertDetail.currentFlagged` | Marcada por el motor | Marcado pelo motor |
| `alertDetail.currentBelowThreshold` | Por debajo del umbral | Abaixo do limiar |
| `alertDetail.currentEvaluatedAt` | Calculada por primera vez el {instant}. Una corrida posterior que no encuentra cambios reutiliza esta misma evaluación y conserva su fecha, así que este instante no es el de la corrida vigente. | Calculada pela primeira vez em {instant}. Uma execução posterior que não encontra mudanças reutiliza esta mesma avaliação e conserva sua data, então este instante não é o da execução vigente. |
| `alertDetail.divergenceAdvisory` | La evaluación del pedido cambió ({from} → {to}) sin cambiar de banda. Las señales vigentes están en el bloque «Evaluación vigente». | A avaliação do pedido mudou ({from} → {to}) sem mudar de faixa. Os sinais vigentes estão no bloco «Avaliação vigente». |
| `alertDetail.divergenceAdvisorySignals` | Las señales del pedido cambiaron, aunque el score sigue en {score} y la banda tampoco se movió. Las señales vigentes están en el bloque «Evaluación vigente». | Os sinais do pedido mudaram, embora o score continue em {score} e a faixa também não tenha mudado. Os sinais vigentes estão no bloco «Avaliação vigente». |
| `alertDetail.divergenceOpenedAt` | {severity} con score {score} | {severity} com score {score} |
| `alertDetail.divergenceNoCurrent` | La alerta se abrió en {from}, pero el pedido ya no tiene evaluación vigente, así que no hay nada con qué comparar el snapshot. | O alerta foi aberto em {from}, mas o pedido já não tem avaliação vigente, então não há nada com que comparar o snapshot. |
| `alertDetail.divergenceBandless` | score {score}, por debajo del umbral de alerta y sin banda | score {score}, abaixo do limiar de alerta e sem faixa |
| `alertDetail.divergenceBand` | La alerta se abrió en {from}. La evaluación vigente está en {to}. | O alerta foi aberto em {from}. A avaliação vigente está em {to}. |
| `alertDetail.externalTitle` | Evaluación externa | Avaliação externa |
| `alertDetail.externalNever` | Una segunda opinión, de un proveedor antifraude externo. Todavía no se pidió ninguna para este pedido. | Uma segunda opinião, de um provedor antifraude externo. Ainda não foi pedida nenhuma para este pedido. |
| `alertDetail.externalProvider` | {provider}. El proveedor opina; la decisión sigue siendo del comercio. | {provider}. O provedor opina; a decisão continua sendo do estabelecimento. |
| `alertDetail.externalRequestedAt` | Pedida el {instant} | Pedida em {instant} |
| `alertDetail.externalSettledAt` | . Respondida el {instant}, {source}. | . Respondida em {instant}, {source}. |
| `alertDetail.externalScore` | Score del proveedor: | Score do provedor: |
| `alertDetail.externalScoreHint` | . Está en la escala del proveedor y no se compara con el score local, que es otra escala de otro sistema. | . Está na escala do provedor e não se compara com o score local, que é outra escala de outro sistema. |
| `alertDetail.externalLastError` | Último intento fallido: {error}. La evaluación sigue esperando al proveedor: un intento que no llegó a respuesta no es un veredicto. | Última tentativa com falha: {error}. A avaliação continua aguardando o provedor: uma tentativa que não chegou a uma resposta não é um veredito. |
| `alertDetail.externalDisagreementTitle` | Los dos criterios no coinciden | Os dois critérios não coincidem |
| `alertDetail.externalLocalLabel` | Motor local: | Motor local: |
| `alertDetail.externalLocalFlagged` | marcó el pedido por encima del umbral. | marcou o pedido acima do limiar. |
| `alertDetail.externalLocalNotFlagged` | dejó el pedido por debajo del umbral. | deixou o pedido abaixo do limiar. |
| `alertDetail.externalLocalHint` | Reglas deterministas sobre la historia del comprador. | Regras determinísticas sobre o histórico do comprador. |
| `alertDetail.externalProviderLabel` | Proveedor externo: | Provedor externo: |
| `alertDetail.externalProviderDenied` | denegó el pedido. | negou o pedido. |
| `alertDetail.externalProviderApproved` | aprobó el pedido. | aprovou o pedido. |
| `alertDetail.externalArrived` | Llegó {source}. | Chegou {source}. |
| `alertDetail.externalDisagreementHint` | No se combinan en un veredicto único ni se comparan sus scores. La discrepancia es información para quien revisa, no una operación aritmética. | Não se combinam em um veredito único nem se comparam seus scores. A divergência é informação para quem revisa, não uma operação aritmética. |
| `alertDetail.externalContradiction` | El proveedor envió un veredicto contradictorio: después de responder, mandó otro distinto sobre la misma evaluación. Vale el primero —una evaluación con veredicto no se reabre— y la contradicción queda registrada en vez de descartarse en silencio. | O provedor enviou um veredito contraditório: depois de responder, mandou outro diferente sobre a mesma avaliação. Vale o primeiro —uma avaliação com veredito não é reaberta— e a contradição fica registrada em vez de ser descartada em silêncio. |
| `alertDetail.externalRequestButton` | Solicitar evaluación externa | Solicitar avaliação externa |
| `alertDetail.externalRequestPending` | Consultando al proveedor… | Consultando o provedor… |
| `alertDetail.externalDeliverButton` | Entregar el callback del proveedor | Entregar o callback do provedor |
| `alertDetail.externalDeliverPending` | Entregando el callback… | Entregando o callback… |
| `alertDetail.externalDeliverHint` | Simula la llegada del callback que el proveedor enviaría por su cuenta. Existe solo porque esta instancia se declara de demostración: quien lo pulsa elige qué evaluación, nunca qué responde el proveedor. | Simula a chegada do callback que o provedor enviaria por conta própria. Existe só porque esta instância se declara de demonstração: quem clica escolhe qual avaliação, nunca o que o provedor responde. |
| `alertDetail.explanationTitle` | Explicación | Explicação |
| `alertDetail.explanationLead` | El snapshot que abrió la alerta, contado en palabras. Describe la evaluación: no cambia el score, ni la severidad, ni el veredicto. | O snapshot que abriu o alerta, contado em palavras. Descreve a avaliação: não muda o score, nem a severidade, nem o veredito. |
| `alertDetail.explanationOutdatedTitle` | Esta explicación describe una evaluación que ya no es la vigente | Esta explicação descreve uma avaliação que já não é a vigente |
| `alertDetail.explanationOutdatedBody` | Se redactó sobre el snapshot con el que se abrió la alerta, y desde entonces el pedido tiene otra evaluación. Se conserva porque es el registro de lo que se pudo leer al decidir; las señales de ahora están en el bloque «Evaluación vigente». | Foi redigida sobre o snapshot com que o alerta foi aberto, e desde então o pedido tem outra avaliação. É conservada porque é o registro do que se pôde ler ao decidir; os sinais de agora estão no bloco «Avaliação vigente». |
| `alertDetail.explanationOutdatedHasCurrent` |  La evaluación vigente ya tiene además su propia explicación escrita. |  A avaliação vigente já tem, além disso, sua própria explicação escrita. |
| `alertDetail.explanationNeverAsked` | Todavía no se pidió una explicación de esta evaluación. Pedirla no cambia nada del pedido ni de la alerta: se le pide el texto al proveedor de explicaciones y se verifica contra la evaluación antes de guardarlo. | Ainda não foi pedida uma explicação desta avaliação. Pedi-la não muda nada do pedido nem do alerta: o texto é pedido ao provedor de explicações e verificado contra a avaliação antes de ser guardado. |
| `alertDetail.explanationWriting` | Redactando la explicación… | Redigindo a explicação… |
| `alertDetail.explanationWritingHint` | Pedida el {instant}. Recargá la alerta en unos segundos. Si la petición quedó a medias, el próximo pedido retoma la misma fila. | Pedida em {instant}. Recarregue o alerta em alguns segundos. Se a requisição ficou pela metade, o próximo pedido retoma a mesma linha. |
| `alertDetail.explanationWrittenBy` | Redactada por una {provider} ({version}), no por un modelo{settledAt}. | Redigida por um {provider} ({version}), não por um modelo de linguagem{settledAt}. |
| `alertDetail.explanationWrittenAt` | , el {instant} | , em {instant} |
| `alertDetail.explanationVerified` | Cada cifra y cada regla del texto se verificaron contra esta evaluación antes de guardarlo: un texto que no pasa esa comprobación no se guarda ni se muestra. | Cada número e cada regra do texto foram verificados contra esta avaliação antes de guardá-lo: um texto que não passa nessa checagem não é guardado nem exibido. |
| `alertDetail.explanationCitedRules` | Reglas citadas: {rules}. | Regras citadas: {rules}. |
| `alertDetail.explanationFailedWithoutCode` | La redacción terminó sin texto utilizable | A redação terminou sem texto utilizável |
| `alertDetail.explanationAttempts` | Se intentó {formatted} veces | Foi tentado {formatted} vezes |
| `alertDetail.explanationLastAttempt` | ; el último, el {instant} | ; a última, em {instant} |
| `alertDetail.explanationExhausted` | Se agotó el presupuesto de intentos, así que no se vuelve a pedir. Un veredicto no necesita explicación para emitirse. | O orçamento de tentativas se esgotou, então não é pedido de novo. Um veredito não precisa de explicação para ser emitido. |
| `alertDetail.explanationNotStored` | El texto que no se pudo verificar no se guarda ni llega a esta pantalla. | O texto que não pôde ser verificado não é guardado nem chega a esta tela. |
| `alertDetail.explanationAskFirst` | Explicar esta evaluación | Explicar esta avaliação |
| `alertDetail.explanationAskRetry` | Volver a intentar la explicación | Tentar a explicação de novo |
| `alertDetail.explanationAskCurrentTemplate` | Redactar con la plantilla vigente | Redigir com o modelo vigente |
| `alertDetail.explanationAskPending` | Redactando… | Redigindo… |
| `alertDetail.reviewTitle` | Emitir veredicto | Emitir veredito |
| `alertDetail.reviewLead` | El veredicto es definitivo: una alerta revisada no se reabre. Si el criterio cambia, hace falta una alerta nueva sobre el pedido. | O veredito é definitivo: um alerta revisado não é reaberto. Se o critério mudar, é preciso um alerta novo sobre o pedido. |
| `alertDetail.reviewAcknowledgeTitle` | El corpus cambió desde que se abrió esta alerta | O corpus mudou desde que este alerta foi aberto |
| `alertDetail.reviewAcknowledgeLabel` | Leí en qué cambió la evaluación vigente y quiero emitir el veredicto igual. | Li o que mudou na avaliação vigente e quero emitir o veredito mesmo assim. |
| `alertDetail.reviewLegend` | Veredicto | Veredito |
| `alertDetail.reviewSafeLabel` | Confirmar segura | Confirmar seguro |
| `alertDetail.reviewSafeHint` | El pedido no es fraude. | O pedido não é fraude. |
| `alertDetail.reviewFraudLabel` | Reportar fraude | Reportar fraude |
| `alertDetail.reviewFraudHint` | El pedido es fraude y queda registrado como tal. | O pedido é fraude e fica registrado como tal. |
| `alertDetail.reviewNoteLabel` | Nota de la revisión (opcional) | Nota da revisão (opcional) |
| `alertDetail.reviewNoteHint` | Hasta {maxLength} caracteres. Queda en la auditoría junto al veredicto. | Até {maxLength} caracteres. Fica na auditoria junto ao veredito. |
| `alertDetail.reviewBlocked` | Marcá la casilla de arriba para poder enviar el veredicto. | Marque a caixa acima para poder enviar o veredito. |
| `alertDetail.reviewSubmit` | Registrar veredicto | Registrar veredito |
| `alertDetail.reviewSubmitPending` | Registrando… | Registrando… |
| `alertDetail.verdictAnnounced` | La alerta quedó revisada y el formulario de veredicto ya no está. | O alerta ficou revisado e o formulário de veredito já não está. |
| `alertDetail.verdictTitle` | Veredicto registrado: {status} | Veredito registrado: {status} |
| `alertDetail.verdictNoAudit` | La alerta está cerrada, pero no hay una entrada de auditoría asociada. | O alerta está fechado, mas não há uma entrada de auditoria associada. |
| `alertDetail.verdictTransition` | Pasó de {from} a {to} el {instant}. | Passou de {from} para {to} em {instant}. |
| `alertDetail.verdictNoNote` | La revisión se registró sin nota. | A revisão foi registrada sem nota. |
| `alertDetail.verdictNoteTitle` | Nota registrada | Nota registrada |
| `alertDetail.verdictTerminal` | Un veredicto es terminal. Nada reabre una alerta revisada; una escalada crea una alerta nueva enlazada a esta. | Um veredito é terminal. Nada reabre um alerta revisado; uma escalada cria um alerta novo ligado a este. |
| `dashboard.title` | Dashboard | Painel |
| `dashboard.lead` | Estado operativo de la corrida vigente. Los montos se informan por moneda y nunca se suman entre ellas. «Fraude reportado» es lo que decidió una analista, no la verdad de campo. | Estado operacional da execução vigente. Os valores são informados por moeda e nunca somados entre si. «Fraude reportada» é o que uma analista decidiu, não a verdade de campo. |
| `dashboard.ordersInRun` | 2 pedidos en la corrida | 2 pedidos na execução |
| `dashboard.pendingTitle` | Hay {formatted} pedidos fuera de la corrida vigente | Há {formatted} pedidos fora da execução vigente |
| `dashboard.pendingBody` | No están puntuados, así que no cuentan en ninguna de las cifras de abajo y no pueden abrir alertas. | Não estão pontuados, então não contam em nenhum dos números abaixo e não podem abrir alertas. |
| `dashboard.emptyNoOrdersTitle` | Todavía no hay pedidos | Ainda não há pedidos |
| `dashboard.emptyNoOrdersBody` | La base está vacía: no hay monto en riesgo, ni tasa de marcado, ni semanas que dibujar. Importá un archivo CSV o JSON, o cargá el corpus de demostración. | A base está vazia: não há valor em risco, nem taxa de marcação, nem semanas a desenhar. Importe um arquivo CSV ou JSON, ou carregue o corpus de demonstração. |
| `dashboard.emptyNoRunTitle` | Hay pedidos importados y ninguna corrida de scoring | Há pedidos importados e nenhuma execução de scoring |
| `dashboard.emptyNoRunBody` | Los 2 pedidos de la base no tienen evaluación, así que el dashboard no tiene nada que resumir: sin corrida no hay score, ni marcado, ni alertas. Nada de esto se calcula solo. | Os 2 pedidos da base não têm avaliação, então o painel não tem o que resumir: sem execução não há score, nem marcação, nem alertas. Nada disso se calcula sozinho. |
| `dashboard.emptyNoAlertsTitle` | No hay alertas abiertas | Não há alertas abertos |
| `dashboard.emptyNoAlertsBody` | La {run} no dejó nada pendiente de revisión: ningún pedido alcanzó el umbral, o todas las alertas que se abrieron ya tienen veredicto. Las cifras de riesgo de abajo se calculan igual sobre la corrida vigente. | A {run} não deixou nada pendente de revisão: nenhum pedido alcançou o limiar, ou todos os alertas abertos já têm veredito. Os números de risco abaixo são calculados do mesmo jeito sobre a execução vigente. |
| `dashboard.openAlertsTitle` | Alertas abiertas | Alertas abertos |
| `dashboard.openAlertsHint` | Pendientes de veredicto, por banda de severidad. | Pendentes de veredito, por faixa de severidade. |
| `dashboard.openAlertsNone` | sin alertas | sem alertas |
| `dashboard.amountAtRiskTitle` | Monto en riesgo | Valor em risco |
| `dashboard.amountAtRiskHint` | Importe de los pedidos con alerta abierta. Una fila por moneda: no existe un total. | Valor dos pedidos com alerta aberto. Uma linha por moeda: não existe um total. |
| `dashboard.amountAtRiskNone` | No hay ninguna alerta abierta con monto asociado. | Não há nenhum alerta aberto com valor associado. |
| `dashboard.alertCount` | {formatted} alertas | {formatted} alertas |
| `dashboard.reportedFraudTitle` | Fraude reportado | Fraude reportada |
| `dashboard.reportedFraudHint` | Veredictos de la analista, agregados por pedido distinto. También por moneda. | Vereditos da analista, agregados por pedido distinto. Também por moeda. |
| `dashboard.reportedFraudNone` | Todavía nadie marcó una alerta como fraude en esta base. | Ainda ninguém marcou um alerta como fraude nesta base. |
| `dashboard.orderCount` | {formatted} pedidos | {formatted} pedidos |
| `dashboard.flagRateTitle` | Tasa de marcado | Taxa de marcação |
| `dashboard.flagRateNone` | La corrida vigente no cubrió ningún pedido, así que no hay proporción que calcular. | A execução vigente não cobriu nenhum pedido, então não há proporção a calcular. |
| `dashboard.flagRateHint` | Proporción de los 2 pedidos de la corrida vigente que las reglas denegaron. | Proporção dos 2 pedidos da execução vigente que as regras negaram. |
| `dashboard.topSignalsTitle` | Señales principales | Principais sinais |
| `dashboard.topSignalsHint` | Reglas presentes en el snapshot de las alertas abiertas, no en todas las evaluaciones. | Regras presentes no snapshot dos alertas abertos, não em todas as avaliações. |
| `dashboard.topSignalsNone` | Ninguna alerta abierta, así que ninguna señal. | Nenhum alerta aberto, então nenhum sinal. |
| `dashboard.denialsTitle` | Denegados por el proveedor sin alerta local | Negados pelo provedor sem alerta local |
| `dashboard.denialsHint` | Pedidos que el proveedor externo denegó y que el motor local nunca marcó. Es la única pantalla donde aparece un pedido sin alerta. | Pedidos que o provedor externo negou e que o motor local nunca marcou. É a única tela onde aparece um pedido sem alerta. |
| `dashboard.denialsNone` | Ningún pedido denegado por el proveedor quedó fuera de la cola. O nadie pidió todavía la evaluación externa, o el proveedor y el motor local coincidieron en todo. | Nenhum pedido negado pelo provedor ficou fora da fila. Ou ninguém pediu ainda a avaliação externa, ou o provedor e o motor local coincidiram em tudo. |
| `dashboard.denialsOrderWord` | pedidos | pedidos |
| `dashboard.denialsListed` |  · se listan los 2 más recientes |  · são listados os 2 mais recentes |
| `dashboard.denialsCaption` | Pedidos denegados por el proveedor externo que no abrieron ninguna alerta local | Pedidos negados pelo provedor externo que não abriram nenhum alerta local |
| `dashboard.denialsColumnOrder` | Pedido | Pedido |
| `dashboard.denialsColumnDate` | Fecha | Data |
| `dashboard.denialsColumnAmount` | Monto | Valor |
| `dashboard.denialsColumnCountry` | País | País |
| `dashboard.denialsColumnScore` | Score local | Score local |
| `dashboard.denialsUnscored` | sin puntuar | sem pontuar |
| `dashboard.riskOverTimeTitle` | Riesgo en el tiempo | Risco ao longo do tempo |
| `dashboard.riskOverTimeHint` | Por semana de ocurrencia del pedido, en hora de Montevideo: la misma zona con la que las reglas deciden a qué día pertenece cada pedido. | Por semana de ocorrência do pedido, no horário de Montevidéu: o mesmo fuso com que as regras decidem a que dia cada pedido pertence. |
| `dashboard.riskOverTimeNone` | La corrida vigente no cubrió ningún pedido, así que no hay semanas que dibujar. | A execução vigente não cobriu nenhum pedido, então não há semanas a desenhar. |
| `dashboard.chartTitle` | Pedidos por semana y cuántos de ellos denegó la corrida vigente | Pedidos por semana e quantos deles a execução vigente negou |
| `dashboard.chartDescription` | {weeks} semanas{range}. {orders} pedidos en total, de los cuales {flagged} quedaron denegados. Los mismos números están en la tabla que sigue al gráfico. | {weeks} semanas{range}. {orders} pedidos no total, dos quais {flagged} ficaram negados. Os mesmos números estão na tabela que segue o gráfico. |
| `dashboard.chartRange` | , de {from} a {to} | , de {from} a {to} |
| `dashboard.chartBar` | Semana del {week}: {orders} pedidos, {flagged} denegados | Semana de {week}: {orders} pedidos, {flagged} negados |
| `dashboard.chartLegendOrders` | Pedidos de la semana | Pedidos da semana |
| `dashboard.chartLegendFlagged` | Denegados por la corrida vigente | Negados pela execução vigente |
| `dashboard.chartTableCaption` | Pedidos y denegados por semana, los mismos datos que dibuja el gráfico. | Pedidos e negados por semana, os mesmos dados que o gráfico desenha. |
| `dashboard.chartColumnWeek` | Semana | Semana |
| `dashboard.chartColumnOrders` | Pedidos | Pedidos |
| `dashboard.chartColumnFlagged` | Denegados | Negados |
| `dashboard.chartColumnShare` | Proporción | Proporção |
| `dashboard.qualityTitle` | Calidad del criterio | Qualidade do critério |
| `dashboard.qualityLead` | Medida contra las etiquetas del corpus de demostración. Ninguna cifra del resto de esta pantalla usa esas etiquetas. | Medida contra os rótulos do corpus de demonstração. Nenhum número do resto desta tela usa esses rótulos. |
| `dashboard.qualityCaveat` | La fixture demo tiene errores puestos a mano: tres falsos negativos que ninguna regla puede ver y cuatro falsos positivos. Por eso F1 no vale 1,00, y por eso tampoco mide el criterio: con los errores puestos a propósito, F1 es un parámetro elegido y no un resultado. Estas métricas prueban el pipeline de evaluación —división temporal, holdout sin retuning, cálculo correcto—, no la calidad de la detección. | A fixture de demonstração tem erros colocados à mão: três falsos negativos que nenhuma regra consegue ver e quatro falsos positivos. Por isso o F1 não vale 1,00, e por isso também não mede o critério: com os erros colocados de propósito, o F1 é um parâmetro escolhido e não um resultado. Estas métricas provam o pipeline de avaliação —divisão temporal, holdout sem retuning, cálculo correto—, não a qualidade da detecção. |
| `dashboard.qualityScoredOrders` | Pedidos puntuados | Pedidos pontuados |
| `dashboard.qualityLabeled` | Con etiqueta | Com rótulo |
| `dashboard.qualityUnlabeled` | Sin etiqueta | Sem rótulo |
| `dashboard.qualityUnlabeledHint` | Se cuentan y se excluyen: un pedido importado nunca trae etiqueta. | São contados e excluídos: um pedido importado nunca traz rótulo. |
| `dashboard.qualityRuleConfig` | Configuración de reglas | Configuração de regras |
| `dashboard.qualityHoldoutTitle` | Holdout · 2 pedidos | Holdout · 2 pedidos |
| `dashboard.qualityHoldoutHint` | Umbral {threshold}, elegido sobre la cohorte de calibración y aplicado acá sin retocar nada. | Limiar {threshold}, escolhido sobre a coorte de calibração e aplicado aqui sem retocar nada. |
| `dashboard.qualityCalibrationTitle` | Calibración · 2 pedidos | Calibração · 2 pedidos |
| `dashboard.qualityCalibrationHint` | La cohorte temprana, sobre la que se eligió el umbral. No es una medición independiente. | A coorte inicial, sobre a qual o limiar foi escolhido. Não é uma medição independente. |
| `dashboard.qualityUndefined` | sin definir | sem definição |
| `dashboard.qualityPrecision` | Precisión | Precisão |
| `dashboard.qualityRecall` | Recall | Recall |
| `dashboard.qualityF1` | F1 | F1 |
| `dashboard.qualityFalsePositiveRate` | Tasa de falsos positivos | Taxa de falsos positivos |
| `dashboard.qualityMatrixCaption` | Matriz de confusión | Matriz de confusão |
| `dashboard.qualityTruePositives` | Verdaderos positivos | Verdadeiros positivos |
| `dashboard.qualityFalsePositives` | Falsos positivos | Falsos positivos |
| `dashboard.qualityFalseNegatives` | Falsos negativos | Falsos negativos |
| `dashboard.qualityTrueNegatives` | Verdaderos negativos | Verdadeiros negativos |
| `dashboard.qualityRunFootnote` | Corrida #{sequence}, {instant}. | Execução nº {sequence}, {instant}. |
| `dashboard.qualitySweepSummary` | Barrido de umbrales sobre la cohorte de calibración ({points} puntos) | Varredura de limiares sobre a coorte de calibração ({points} pontos) |
| `dashboard.qualitySweepHint` | Solo los umbrales donde la matriz de confusión cambia. El umbral {threshold} es el que se eligió y el que se aplicó al holdout. | Apenas os limiares onde a matriz de confusão muda. O limiar {threshold} é o que foi escolhido e o que foi aplicado ao holdout. |
| `dashboard.qualitySweepCaption` | Precisión, recall, F1 y tasa de falsos positivos en cada umbral de la cohorte de calibración | Precisão, recall, F1 e taxa de falsos positivos em cada limiar da coorte de calibração |
| `dashboard.qualitySweepThreshold` | Umbral | Limiar |
| `dashboard.qualitySweepChosen` |  · elegido |  · escolhido |
| `importPage.title` | Importación y scoring | Importação e scoring |
| `importPage.lead` | Importar escribe pedidos y nada más. Las evaluaciones y las alertas las produce una corrida de scoring, que se ejecuta desde acá: hasta que no la corras, la cola de alertas y el dashboard siguen mostrando el estado de la corrida anterior. | Importar escreve pedidos e nada mais. As avaliações e os alertas são produzidos por uma execução de scoring, que é disparada daqui: enquanto você não a executar, a fila de alertas e o painel continuam mostrando o estado da execução anterior. |
| `importPage.corpusStatusTitle` | Estado del corpus | Estado do corpus |
| `importPage.corpusPending` | Hay {formatted} pedidos sin puntuar por la corrida vigente. | Há {formatted} pedidos sem pontuar pela execução vigente. |
| `importPage.corpusPendingHint` | No aparecen en el dashboard ni pueden generar alertas hasta que ejecutes una corrida. | Não aparecem no painel nem podem gerar alertas até que você execute uma corrida. |
| `importPage.corpusEmpty` | No hay pedidos sin puntuar porque todavía no hay ninguno en la base. | Não há pedidos sem pontuar porque ainda não há nenhum na base. |
| `importPage.corpusCovered` | Todos los pedidos de la base están cubiertos por la corrida vigente. | Todos os pedidos da base estão cobertos pela execução vigente. |
| `importPage.seedTitle` | Corpus de demostración | Corpus de demonstração |
| `importPage.seedDescription` | Trescientos pedidos sintéticos con sus etiquetas de fraude, pensados para poder medir el criterio: incluye fraude que las reglas locales no pueden ver y pedidos legítimos que sí marcan. La carga es idempotente: repetirla no duplica nada. Esta sección existe solo porque esta instancia se declara de demostración. | Trezentos pedidos sintéticos com seus rótulos de fraude, pensados para que o critério possa ser medido: inclui fraude que as regras locais não conseguem ver e pedidos legítimos que marcam. A carga é idempotente: repeti-la não duplica nada. Esta seção existe só porque esta instância se declara de demonstração. |
| `importPage.seedConflictPreviousTitle` | Esta base tiene una versión anterior del corpus de demostración | Esta base tem uma versão anterior do corpus de demonstração |
| `importPage.seedConflictPreviousBody` | Cargar la versión {version} exige una base nueva: un pedido es inmutable, así que las dos versiones no pueden convivir bajo las mismas referencias de comercio. La base actual no se toca ni se pierde: deja de ser la de demostración. | Carregar a versão {version} exige uma base nova: um pedido é imutável, então as duas versões não podem conviver sob as mesmas referências de estabelecimento. A base atual não é tocada nem perdida: deixa de ser a de demonstração. |
| `importPage.seedConflictImportedTitle` | Esta base tiene pedidos importados con las mismas referencias | Esta base tem pedidos importados com as mesmas referências |
| `importPage.seedConflictImportedBody` | Los pedidos que ya están usan las mismas referencias que la fixture y tienen otros datos. La carga se cancela entera antes que pisar ninguno. | Os pedidos que já estão lá usam as mesmas referências da fixture e têm outros dados. A carga é cancelada inteira em vez de sobrescrever qualquer um deles. |
| `importPage.seedButton` | Cargar corpus de demostración | Carregar corpus de demonstração |
| `importPage.seedButtonPending` | Cargando corpus… | Carregando corpus… |
| `importPage.fileTitle` | Importar un archivo | Importar um arquivo |
| `importPage.fileDescription` | CSV o JSON. La validación es estricta por registro y la escritura atómica por archivo: los registros rechazados se listan uno por uno y no se escribe ninguno de ellos. | CSV ou JSON. A validação é estrita por registro e a escrita atômica por arquivo: os registros rejeitados são listados um a um e nenhum deles é escrito. |
| `importPage.fileLabel` | Archivo | Arquivo |
| `importPage.fileHint` | Hasta {maxMib} MiB y 10.000 pedidos por archivo. Los datos deben ser sintéticos. | Até {maxMib} MiB e 10.000 pedidos por arquivo. Os dados devem ser sintéticos. |
| `importPage.formatLegend` | Formato | Formato |
| `importPage.fileButton` | Importar pedidos | Importar pedidos |
| `importPage.fileButtonPending` | Importando… | Importando… |
| `importPage.scoringTitle` | Ejecutar scoring | Executar scoring |
| `importPage.scoringDescription` | Evalúa el corpus completo en orden temporal, con el baseline construido solo con la historia anterior a cada pedido, y abre las alertas que correspondan. Es idempotente: una evaluación cuyo resultado no cambió se reusa en vez de duplicarse, y una alerta ya abierta o ya revisada no se vuelve a abrir. | Avalia o corpus inteiro em ordem temporal, com o baseline construído apenas com o histórico anterior a cada pedido, e abre os alertas que couberem. É idempotente: uma avaliação cujo resultado não mudou é reaproveitada em vez de duplicada, e um alerta já aberto ou já revisado não é aberto de novo. |
| `importPage.scoringButton` | Ejecutar scoring | Executar scoring |
| `importPage.scoringButtonPending` | Ejecutando corrida… | Executando corrida… |
| `importPage.scoringRunning` | La corrida evalúa el corpus en orden temporal. En un corpus de trescientos pedidos tarda unos segundos. | A execução avalia o corpus em ordem temporal. Em um corpus de trezentos pedidos leva alguns segundos. |
| `importPage.externalTitle` | Proveedor antifraude externo | Provedor antifraude externo |
| `importPage.externalDescription` | Una segunda opinión sobre cada pedido, de un proveedor externo simulado. El detalle de una alerta permite pedirla de a un pedido; acá se pide para el corpus entero, que es lo único que alcanza a los pedidos que nunca abrieron una alerta. Entregar los callbacks simula la respuesta que el proveedor mandaría por su cuenta: quien lo pulsa elige qué evaluación, nunca qué responde el proveedor. Repetirlo no repite efectos. | Uma segunda opinião sobre cada pedido, de um provedor externo simulado. O detalhe de um alerta permite pedi-la para um pedido de cada vez; aqui ela é pedida para o corpus inteiro, que é a única coisa que alcança os pedidos que nunca abriram um alerta. Entregar os callbacks simula a resposta que o provedor mandaria por conta própria: quem clica escolhe qual avaliação, nunca o que o provedor responde. Repetir não repete efeitos. |
| `importPage.externalRequestButton` | Solicitar evaluación externa del corpus | Solicitar avaliação externa do corpus |
| `importPage.externalRequestPending` | Consultando al proveedor… | Consultando o provedor… |
| `importPage.externalRequestRunning` | Se consulta pedido por pedido, reservando la fila antes de llamar. En un corpus de trescientos pedidos tarda unos segundos. | A consulta é pedido a pedido, reservando a linha antes de chamar. Em um corpus de trezentos pedidos leva alguns segundos. |
| `importPage.externalDeliverButton` | Entregar los callbacks del proveedor | Entregar os callbacks do provedor |
| `importPage.externalDeliverPending` | Entregando callbacks… | Entregando callbacks… |
| `importPage.rejectedTitle` | Registros rechazados (2) | Registros rejeitados (2) |
| `importPage.rejectedHint` | Ninguno de estos se escribió. Los pedidos válidos del mismo archivo sí. | Nenhum destes foi escrito. Os pedidos válidos do mesmo arquivo sim. |
| `importPage.rejectedTruncated` | La API dejó de enumerar errores en este punto: hay más registros rechazados de los que se listan acá. | A API parou de enumerar erros neste ponto: há mais registros rejeitados do que os listados aqui. |
| `importPage.recordAt` | Registro {record} | Registro {record} |
| `importPage.recordAtLine` | Registro {record}, línea {line} | Registro {record}, linha {line} |
| `importPage.recordField` |  · campo {field} |  · campo {field} |
| `importPage.recordError` | {where}{field} · {label}: {message} | {where}{field} · {label}: {message} |
| `outcomes.seedAlreadyLoadedTitle` | El corpus de demostración ya estaba cargado | O corpus de demonstração já estava carregado |
| `outcomes.seedLoadedTitle` | Corpus cargado | Corpus carregado |
| `outcomes.seedAlreadyLoadedBody` | La carga es idempotente: los pedidos ya estaban en la base y no se duplicó ninguno. | A carga é idempotente: os pedidos já estavam na base e nenhum foi duplicado. |
| `outcomes.seedLoadedBody` | Los pedidos quedaron en la base. Todavía no tienen evaluación ni alerta. | Os pedidos ficaram na base. Ainda não têm avaliação nem alerta. |
| `outcomes.seedRecovery` | Ejecutá una corrida de scoring para puntuarlos. | Execute uma corrida de scoring para pontuá-los. |
| `outcomes.seedFactVersion` | Versión de la fixture: {version} | Versão da fixture: {version} |
| `outcomes.seedFactInserted` | Pedidos insertados: {inserted} de {total} | Pedidos inseridos: {inserted} de {total} |
| `outcomes.seedFactDuplicates` | Pedidos ya presentes: 2 | Pedidos já presentes: 2 |
| `outcomes.seedFactLabels` | Etiquetas insertadas: {inserted} de {total} | Rótulos inseridos: {inserted} de {total} |
| `outcomes.importNoneTitle` | No se importó ningún pedido | Nenhum pedido foi importado |
| `outcomes.importSomeTitle` | Se importaron 2 pedidos | Foram importados 2 pedidos |
| `outcomes.importBody` | La importación es estricta por registro y atómica por archivo: los pedidos válidos se escribieron todos juntos y los rechazados no se escribieron nunca. | A importação é estrita por registro e atômica por arquivo: os pedidos válidos foram escritos todos juntos e os rejeitados nunca foram escritos. |
| `outcomes.importRecovery` | Los pedidos nuevos no tienen evaluación hasta que ejecutes una corrida de scoring. | Os pedidos novos não têm avaliação até que você execute uma corrida de scoring. |
| `outcomes.importFactRead` | Registros leídos: 2 | Registros lidos: 2 |
| `outcomes.importFactImported` | Importados: 2 | Importados: 2 |
| `outcomes.importFactDuplicates` | Duplicados, ya presentes con los mismos datos: 2 | Duplicados, já presentes com os mesmos dados: 2 |
| `outcomes.importFactRejected` | Rechazados: 2 | Rejeitados: 2 |
| `outcomes.scoringTitle` | Corrida #{sequence} completada | Execução nº {sequence} concluída |
| `outcomes.scoringBody` | Ya existe una evaluación vigente por pedido y las alertas que correspondían quedaron abiertas. Una evaluación reusada es una cuyo resultado no cambió: mismo corpus, misma configuración, mismo resultado. | Já existe uma avaliação vigente por pedido e os alertas que couberam ficaram abertos. Uma avaliação reaproveitada é uma cujo resultado não mudou: mesmo corpus, mesma configuração, mesmo resultado. |
| `outcomes.scoringRecovery` | Revisá la cola de alertas o mirá el dashboard. | Revise a fila de alertas ou olhe o painel. |
| `outcomes.scoringFactConfig` | Configuración de reglas: {version} | Configuração de regras: {version} |
| `outcomes.scoringFactFinished` | Terminó: {instant} | Terminou: {instant} |
| `outcomes.scoringFactOrders` | Pedidos evaluados: 2 | Pedidos avaliados: 2 |
| `outcomes.scoringFactCreated` | Evaluaciones creadas: 2 | Avaliações criadas: 2 |
| `outcomes.scoringFactReused` | Evaluaciones reusadas: 2 | Avaliações reaproveitadas: 2 |
| `outcomes.scoringFactAlerts` | Alertas abiertas: 2 | Alertas abertos: 2 |
| `outcomes.scoringFactSkippedOpen` | Omitidas por tener ya una alerta abierta: 2 | Omitidos por já ter um alerta aberto: 2 |
| `outcomes.scoringFactSkippedReviewed` | Omitidas por tener ya un veredicto: 2 | Omitidos por já ter um veredito: 2 |
| `outcomes.corpusExternalKnownTitle` | El proveedor ya conocía todos los pedidos | O provedor já conhecia todos os pedidos |
| `outcomes.corpusExternalRequestedTitle` | Se consultaron 2 pedidos | Foram consultados 2 pedidos |
| `outcomes.corpusExternalBody` | Cada pedido pasa por el mismo caso de uso que una consulta suelta: la fila se reserva antes de llamar al proveedor, así que dos consultas simultáneas no crean dos evaluaciones del lado del proveedor. | Cada pedido passa pelo mesmo caso de uso de uma consulta avulsa: a linha é reservada antes de chamar o provedor, então duas consultas simultâneas não criam duas avaliações do lado do provedor. |
| `outcomes.corpusExternalSettledRecovery` | El veredicto de cada pedido aparece en el detalle de su alerta. | O veredito de cada pedido aparece no detalhe do seu alerta. |
| `outcomes.corpusExternalPendingRecovery` | Los que siguen esperando al proveedor se cierran entregando sus callbacks o reconciliando. | Os que continuam aguardando o provedor são fechados entregando seus callbacks ou reconciliando. |
| `outcomes.corpusExternalFactExamined` | Pedidos sin evaluación externa: 2 | Pedidos sem avaliação externa: 2 |
| `outcomes.corpusExternalFactRequested` | Consultados: 2 | Consultados: 2 |
| `outcomes.corpusExternalFactSettled` | Con veredicto en el acto: 2 | Com veredito na hora: 2 |
| `outcomes.corpusExternalFactPending` | Esperando al proveedor: 2 | Aguardando o provedor: 2 |
| `outcomes.corpusExternalFactSkipped` | Omitidos, ya tenían evaluación: 2 | Omitidos, já tinham avaliação: 2 |
| `outcomes.deliverNoneTitle` | No hay ninguna evaluación externa esperando al proveedor | Não há nenhuma avaliação externa aguardando o provedor |
| `outcomes.deliverReplayedTitle` | Los 2 callbacks ya se habían recibido | Os 2 callbacks já tinham sido recebidos |
| `outcomes.deliverDoneTitle` | Se entregaron 2 callbacks | Foram entregues 2 callbacks |
| `outcomes.deliverReplayedBody` | Cada mensaje es idéntico a uno ya registrado, así que no se repitió ningún efecto: se anotó que el proveedor los volvió a enviar y nada más. | Cada mensagem é idêntica a uma já registrada, então nenhum efeito foi repetido: apenas ficou anotado que o provedor as enviou de novo. |
| `outcomes.deliverDoneBody` | Los callbacks entran por el mismo caso de uso que usaría el proveedor: mismo recibo, misma deduplicación, mismas reglas de transición. Quien pulsa elige qué evaluación, nunca qué responde el proveedor. | Os callbacks entram pelo mesmo caso de uso que o provedor usaria: mesmo recibo, mesma deduplicação, mesmas regras de transição. Quem clica escolhe qual avaliação, nunca o que o provedor responde. |
| `outcomes.deliverNoneRecovery` | Solicitá evaluaciones externas del corpus para que haya algo que entregar. | Solicite avaliações externas do corpus para que haja algo a entregar. |
| `outcomes.deliverDoneRecovery` | El veredicto del proveedor aparece en el detalle de cada alerta. | O veredito do provedor aparece no detalhe de cada alerta. |
| `outcomes.deliverFactExamined` | Evaluaciones esperando al proveedor: 2 | Avaliações aguardando o provedor: 2 |
| `outcomes.deliverFactDelivered` | Callbacks entregados: 2 | Callbacks entregues: 2 |
| `outcomes.deliverFactSettled` | Cerraron con veredicto: 2 | Fecharam com veredito: 2 |
| `outcomes.deliverFactReplayed` | Ya se habían recibido: 2 | Já tinham sido recebidos: 2 |
| `outcomes.deliverFactUnavailable` | No se pudieron escribir por concurrencia: 2 | Não puderam ser escritos por concorrência: 2 |
| `outcomes.externalRequestedTitle` | Evaluación externa solicitada | Avaliação externa solicitada |
| `outcomes.externalAlreadyTitle` | Este pedido ya tenía una evaluación externa | Este pedido já tinha uma avaliação externa |
| `outcomes.externalWaitingBody` | El proveedor aceptó la consulta y todavía no decidió: la respuesta va a llegar por callback, o al reconciliar. | O provedor aceitou a consulta e ainda não decidiu: a resposta vai chegar por callback, ou ao reconciliar. |
| `outcomes.externalSettledBody` | El veredicto del proveedor queda registrado junto al criterio local, sin combinarse con él. | O veredito do provedor fica registrado junto ao critério local, sem se combinar com ele. |
| `outcomes.externalAlreadyRecovery` | Pedirla de nuevo no crea una segunda evaluación del lado del proveedor. | Pedi-la de novo não cria uma segunda avaliação do lado do provedor. |
| `outcomes.externalCallbackReplayedTitle` | Ese callback ya se había recibido | Esse callback já tinha sido recebido |
| `outcomes.externalCallbackDoneTitle` | Callback entregado | Callback entregue |
| `outcomes.externalCallbackReplayedBody` | El mensaje es idéntico a uno ya registrado, así que no se repitió ningún efecto: se anotó que el proveedor lo volvió a enviar y nada más. | A mensagem é idêntica a uma já registrada, então nenhum efeito foi repetido: apenas ficou anotado que o provedor a enviou de novo. |
| `outcomes.externalCallbackDoneBody` | El callback entró por el mismo camino que usaría el proveedor: mismo recibo, misma deduplicación, mismas reglas de transición. | O callback entrou pelo mesmo caminho que o provedor usaria: mesmo recibo, mesma deduplicação, mesmas regras de transição. |
| `outcomes.externalCallbackRecovery` | El estado del proveedor ya figura arriba. | O estado do provedor já consta acima. |
| `outcomes.explanationWrittenFirstTitle` | Explicación redactada | Explicação redigida |
| `outcomes.explanationWrittenRetryTitle` | Explicación redactada en el nuevo intento | Explicação redigida na nova tentativa |
| `outcomes.explanationWrittenCurrentTemplateTitle` | Redactada de nuevo con la plantilla vigente | Redigida de novo com o modelo vigente |
| `outcomes.explanationWrittenBody` | El texto quedó guardado junto a la evaluación y ya se muestra arriba. Una explicación escrita no se reescribe: si el pedido vuelve a evaluarse, la evaluación nueva lleva la suya. | O texto ficou guardado junto à avaliação e já é exibido acima. Uma explicação escrita não é reescrita: se o pedido for avaliado de novo, a avaliação nova leva a sua. |
| `outcomes.explanationWrittenCurrentTemplateBody` | El texto nuevo se escribió al lado del anterior y es el que se muestra arriba. El anterior sigue guardado sin cambios, porque es el registro de lo que se pudo leer mientras se formaba el veredicto. | O texto novo foi escrito ao lado do anterior e é o que aparece acima. O anterior continua guardado sem mudanças, porque é o registro do que se pôde ler enquanto o veredito se formava. |
| `outcomes.explanationUnchangedTitle` | Esta evaluación ya tenía su explicación | Esta avaliação já tinha sua explicação |
| `outcomes.explanationUnchangedBody` | No se le pidió nada al proveedor: la que ya estaba escrita es la que se muestra arriba. | Nada foi pedido ao provedor: a que já estava escrita é a que aparece acima. |
| `outcomes.explanationFailedTitle` | No se pudo redactar la explicación | Não foi possível redigir a explicação |
| `outcomes.explanationFailedWithoutCode` | El proveedor no dejó ningún texto utilizable. | O provedor não deixou nenhum texto utilizável. |
| `outcomes.explanationFailedWithCode` | {label}. | {label}. |
| `outcomes.explanationFailedExhausted` | Se agotaron los intentos para esta evaluación. El veredicto no necesita una explicación para emitirse. | As tentativas para esta avaliação se esgotaram. O veredito não precisa de uma explicação para ser emitido. |
| `outcomes.explanationFailedRetry` | Podés volver a intentarlo. Un texto que no se pudo verificar no se guarda ni se muestra. | Você pode tentar de novo. Um texto que não pôde ser verificado não é guardado nem exibido. |
| `outcomes.reviewUnchangedTitle` | La alerta ya tenía exactamente este veredicto | O alerta já tinha exatamente este veredito |
| `outcomes.reviewUnchangedBody` | No se registró una revisión nueva porque el veredicto y la nota guardados coinciden con los que enviaste. | Nenhuma revisão nova foi registrada porque o veredito e a nota guardados coincidem com os que você enviou. |
| `outcomes.reviewUnchangedRecovery` | El veredicto vigente es el que se muestra abajo. | O veredito vigente é o que aparece abaixo. |
| `outcomes.reviewAppliedTitle` | Veredicto registrado | Veredito registrado |
| `outcomes.reviewAppliedBody` | La revisión quedó guardada junto con su auditoría. Un veredicto es definitivo: esta alerta no se reabre. | A revisão ficou guardada junto com sua auditoria. Um veredito é definitivo: este alerta não é reaberto. |
| `failures.ALERT_NOT_FOUND.title` | Esta alerta ya no existe | Este alerta não existe mais |
| `failures.ALERT_NOT_FOUND.body` | La alerta que pediste no está en la base. Puede haberse importado un corpus nuevo desde que abriste el enlace. | O alerta que você pediu não está na base. Um corpus novo pode ter sido importado desde que você abriu o link. |
| `failures.ALERT_NOT_FOUND.recovery` | Volvé al feed de alertas y elegí una de la cola vigente. | Volte para a fila de alertas e escolha um da fila vigente. |
| `failures.INVALID_STATUS.title` | El veredicto no es válido | O veredito não é válido |
| `failures.INVALID_STATUS.body` | Solo se puede marcar una alerta como «Segura» o «Fraude reportado». | Um alerta só pode ser marcado como «Seguro» ou «Fraude reportada». |
| `failures.INVALID_STATUS.recovery` | Elegí uno de los dos veredictos y volvé a enviar. | Escolha um dos dois vereditos e envie de novo. |
| `failures.NOTE_TOO_LONG.title` | La nota es demasiado larga | A nota é longa demais |
| `failures.NOTE_TOO_LONG.body` | La nota de revisión admite hasta 2000 caracteres. | A nota de revisão admite até 2000 caracteres. |
| `failures.NOTE_TOO_LONG.recovery` | Recortá la nota y volvé a enviar. Lo que escribiste sigue en el campo. | Encurte a nota e envie de novo. O que você escreveu continua no campo. |
| `failures.ALERT_ALREADY_REVIEWED.title` | Otra persona ya revisó esta alerta | Outra pessoa já revisou este alerta |
| `failures.ALERT_ALREADY_REVIEWED.body` | La alerta quedó cerrada con un veredicto distinto del que enviaste. Un veredicto es definitivo: no se reabre. | O alerta foi fechado com um veredito diferente do que você enviou. Um veredito é definitivo: não é reaberto. |
| `failures.ALERT_ALREADY_REVIEWED.recovery` | Recargá la alerta para ver el estado registrado. Si el criterio cambió, hace falta una alerta nueva sobre el pedido. | Recarregue o alerta para ver o estado registrado. Se o critério mudou, é preciso um alerta novo sobre o pedido. |
| `failures.ALERT_REVIEW_NOTE_CONFLICT.title` | La alerta ya tiene este veredicto, con otra nota | O alerta já tem este veredito, com outra nota |
| `failures.ALERT_REVIEW_NOTE_CONFLICT.body` | El veredicto registrado coincide con el que enviaste, pero la nota guardada es distinta y no se sobrescribe. | O veredito registrado coincide com o que você enviou, mas a nota guardada é diferente e não é sobrescrita. |
| `failures.ALERT_REVIEW_NOTE_CONFLICT.recovery` | Recargá la alerta para ver el estado registrado. La nota registrada aparece en el bloque de revisión. | Recarregue o alerta para ver o estado registrado. A nota registrada aparece no bloco de revisão. |
| `failures.ALERT_DIVERGENCE_NOT_ACKNOWLEDGED.title` | El corpus cambió desde que se abrió la alerta | O corpus mudou desde que o alerta foi aberto |
| `failures.ALERT_DIVERGENCE_NOT_ACKNOWLEDGED.body` | La evaluación vigente del pedido está en otra banda de severidad que la del snapshot con el que se abrió la alerta. La API no acepta un veredicto sin que lo reconozcas. | A avaliação vigente do pedido está em outra faixa de severidade que a do snapshot com que o alerta foi aberto. A API não aceita um veredito sem que você reconheça isso. |
| `failures.ALERT_DIVERGENCE_NOT_ACKNOWLEDGED.recovery` | Volvé al aviso de divergencia, leé qué cambió y marcá la casilla antes de enviar. | Volte ao aviso de divergência, leia o que mudou e marque a caixa antes de enviar. |
| `failures.ALERT_REVIEW_CONFLICT.title` | Dos revisiones al mismo tiempo | Duas revisões ao mesmo tempo |
| `failures.ALERT_REVIEW_CONFLICT.body` | Otra revisión sobre esta alerta se guardó mientras se procesaba la tuya, así que la tuya no se aplicó. | Outra revisão sobre este alerta foi salva enquanto a sua era processada, então a sua não foi aplicada. |
| `failures.ALERT_REVIEW_CONFLICT.recovery` | Recargá la alerta para ver el estado registrado. Si seguís con el mismo criterio, volvé a enviarlo. | Recarregue o alerta para ver o estado registrado. Se você mantém o mesmo critério, envie de novo. |
| `failures.SCORING_RUN_CONFLICT.title` | Otra corrida de scoring se ejecutó al mismo tiempo | Outra execução de scoring rodou ao mesmo tempo |
| `failures.SCORING_RUN_CONFLICT.body` | Dos corridas escribieron estado en conflicto, así que la tuya no se guardó. El corpus quedó como estaba antes de intentarlo: no hay evaluaciones ni alertas a medio escribir. | Duas execuções escreveram estado em conflito, então a sua não foi salva. O corpus ficou como estava antes da tentativa: não há avaliações nem alertas pela metade. |
| `failures.SCORING_RUN_CONFLICT.recovery` | Volvé a ejecutar la corrida. Si alguien más está usando la consola, esperá a que termine. | Execute a corrida de novo. Se mais alguém estiver usando o console, espere terminar. |
| `failures.METRICS_UNAVAILABLE.title` | Todavía no se pueden calcular las métricas de calidad | Ainda não é possível calcular as métricas de qualidade |
| `failures.METRICS_UNAVAILABLE.body` | Medir el criterio exige una corrida de scoring y pedidos etiquetados a ambos lados de la división temporal. Falta alguna de las dos cosas. | Medir o critério exige uma execução de scoring e pedidos rotulados dos dois lados da divisão temporal. Falta uma das duas coisas. |
| `failures.METRICS_UNAVAILABLE.recovery` | Ejecutá una corrida de scoring sobre el corpus de demostración desde la pantalla de importación. | Execute uma corrida de scoring sobre o corpus de demonstração na tela de importação. |
| `failures.DEMO_DATA_CONFLICT.title` | El corpus de demostración choca con pedidos que ya existen | O corpus de demonstração colide com pedidos que já existem |
| `failures.DEMO_DATA_CONFLICT.body` | La base tiene pedidos importados con las mismas referencias que la fixture pero con datos distintos. Un pedido es inmutable, así que la carga se cancela entera antes que pisar nada. | A base tem pedidos importados com as mesmas referências da fixture mas com dados diferentes. Um pedido é imutável, então a carga é cancelada inteira em vez de sobrescrever qualquer coisa. |
| `failures.DEMO_DATA_CONFLICT.recovery` | Usá una base vacía para cargar la demo, o seguí con los pedidos que ya están importados. | Use uma base vazia para carregar a demonstração, ou siga com os pedidos que já estão importados. |
| `failures.DEMO_DATA_PREVIOUS_CORPUS.title` | Esta base tiene una versión anterior del corpus de demostración | Esta base tem uma versão anterior do corpus de demonstração |
| `failures.DEMO_DATA_PREVIOUS_CORPUS.body` | Las dos versiones usan las mismas referencias de comercio y un pedido es inmutable, así que no pueden convivir. No se escribió nada: la base quedó como estaba. | As duas versões usam as mesmas referências de estabelecimento e um pedido é imutável, então não podem conviver. Nada foi escrito: a base ficou como estava. |
| `failures.DEMO_DATA_PREVIOUS_CORPUS.recovery` | Cargá el corpus en una base nueva. La base actual no se pierde; deja de ser la de demostración. | Carregue o corpus em uma base nova. A base atual não se perde; deixa de ser a de demonstração. |
| `failures.FILE_REQUIRED.title` | No llegó ningún archivo | Nenhum arquivo chegou |
| `failures.FILE_REQUIRED.body` | El formulario se envió sin archivo, o con uno de cero bytes. | O formulário foi enviado sem arquivo, ou com um de zero bytes. |
| `failures.FILE_REQUIRED.recovery` | Elegí un archivo CSV o JSON con al menos un pedido y volvé a enviar. | Escolha um arquivo CSV ou JSON com ao menos um pedido e envie de novo. |
| `failures.FILE_TOO_LARGE.title` | El archivo supera el máximo admitido | O arquivo passa do máximo admitido |
| `failures.FILE_TOO_LARGE.body` | La importación acepta hasta {maxMib} MiB por archivo. Nada de lo que enviaste se importó. | A importação aceita até {maxMib} MiB por arquivo. Nada do que você enviou foi importado. |
| `failures.FILE_TOO_LARGE.recovery` | Partí el archivo en varios más chicos e importalos de a uno. | Divida o arquivo em outros menores e importe um a um. |
| `failures.TOO_MANY_RECORDS.title` | El archivo tiene demasiados registros | O arquivo tem registros demais |
| `failures.TOO_MANY_RECORDS.body` | La importación acepta hasta 10.000 pedidos por archivo. Se rechaza el documento entero: no se importa una parte y se descarta el resto en silencio. | A importação aceita até 10.000 pedidos por arquivo. O documento inteiro é rejeitado: não se importa uma parte e se descarta o resto em silêncio. |
| `failures.TOO_MANY_RECORDS.recovery` | Partí el archivo en tandas de hasta 10.000 registros. | Divida o arquivo em lotes de até 10.000 registros. |
| `failures.UNSUPPORTED_MEDIA_TYPE.title` | La consola envió el formulario en un formato que la API no acepta | O console enviou o formulário em um formato que a API não aceita |
| `failures.UNSUPPORTED_MEDIA_TYPE.body` | La importación viaja como `multipart/form-data`. Es un error interno: no debería ocurrir desde esta pantalla. | A importação viaja como `multipart/form-data`. É um erro interno: não deveria acontecer nesta tela. |
| `failures.UNSUPPORTED_MEDIA_TYPE.recovery` | Recargá la página y volvé a intentarlo. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página e tente de novo. Se acontecer outra vez, reporte com a hora exata. |
| `failures.UNSUPPORTED_FORMAT.title` | Ese formato no está soportado | Esse formato não é suportado |
| `failures.UNSUPPORTED_FORMAT.body` | La importación admite CSV o JSON, y hay que declarar cuál es antes de enviar. | A importação admite CSV ou JSON, e é preciso declarar qual antes de enviar. |
| `failures.UNSUPPORTED_FORMAT.recovery` | Elegí CSV o JSON según el archivo y volvé a enviar. | Escolha CSV ou JSON conforme o arquivo e envie de novo. |
| `failures.EMPTY_FILE.title` | El archivo no tiene ningún pedido | O arquivo não tem nenhum pedido |
| `failures.EMPTY_FILE.body` | Se leyó completo y no contiene registros: puede ser un CSV con solo la fila de encabezados, o un JSON con una lista vacía. | Foi lido inteiro e não contém registros: pode ser um CSV só com a linha de cabeçalhos, ou um JSON com uma lista vazia. |
| `failures.EMPTY_FILE.recovery` | Revisá el archivo y volvé a enviarlo con al menos un pedido. | Revise o arquivo e envie de novo com ao menos um pedido. |
| `failures.INVALID_ENCODING.title` | El archivo no está en UTF-8 | O arquivo não está em UTF-8 |
| `failures.INVALID_ENCODING.body` | La importación lee UTF-8 y el archivo trae bytes que no lo son. No se importó nada. | A importação lê UTF-8 e o arquivo traz bytes que não são. Nada foi importado. |
| `failures.INVALID_ENCODING.recovery` | Volvé a exportar el archivo en UTF-8 y reintentá. | Exporte o arquivo de novo em UTF-8 e tente outra vez. |
| `failures.INVALID_CSV.title` | El CSV está mal formado | O CSV está malformado |
| `failures.INVALID_CSV.body` | La estructura del archivo no se pudo leer —comillas sin cerrar, o una fila con más campos que el encabezado—, así que no se importó ninguna fila. | A estrutura do arquivo não pôde ser lida —aspas sem fechar, ou uma linha com mais campos que o cabeçalho—, então nenhuma linha foi importada. |
| `failures.INVALID_CSV.recovery` | Corregí la estructura del archivo y volvé a enviarlo. El detalle técnico de abajo dice dónde falló. | Corrija a estrutura do arquivo e envie de novo. O detalhe técnico abaixo diz onde falhou. |
| `failures.INVALID_JSON.title` | El JSON está mal formado | O JSON está malformado |
| `failures.INVALID_JSON.body` | El archivo no es JSON válido, así que no se importó ningún pedido. | O arquivo não é JSON válido, então nenhum pedido foi importado. |
| `failures.INVALID_JSON.recovery` | Validá el archivo y volvé a enviarlo. El detalle técnico de abajo dice dónde falló. | Valide o arquivo e envie de novo. O detalhe técnico abaixo diz onde falhou. |
| `failures.INVALID_JSON_ROOT.title` | El JSON no es una lista de pedidos | O JSON não é uma lista de pedidos |
| `failures.INVALID_JSON_ROOT.body` | La importación espera un arreglo en la raíz del documento, con un objeto por pedido. | A importação espera um arranjo na raiz do documento, com um objeto por pedido. |
| `failures.INVALID_JSON_ROOT.recovery` | Envolvé los pedidos en un arreglo `[ … ]` y volvé a enviar. | Envolva os pedidos em um arranjo `[ … ]` e envie de novo. |
| `failures.MISSING_HEADER.title` | Al CSV le falta un encabezado obligatorio | Falta um cabeçalho obrigatório no CSV |
| `failures.MISSING_HEADER.body` | El archivo no tiene fila de encabezados, o le falta alguna de las columnas que la importación exige. | O arquivo não tem linha de cabeçalhos, ou falta alguma das colunas que a importação exige. |
| `failures.MISSING_HEADER.recovery` | Agregá la fila de encabezados con todas las columnas obligatorias. El detalle técnico dice cuál falta. | Adicione a linha de cabeçalhos com todas as colunas obrigatórias. O detalhe técnico diz qual falta. |
| `failures.INVALID_HEADER.title` | Un encabezado del CSV está vacío | Um cabeçalho do CSV está vazio |
| `failures.INVALID_HEADER.body` | Una columna del archivo no tiene nombre, así que no se puede saber qué campo es. | Uma coluna do arquivo não tem nome, então não dá para saber que campo é. |
| `failures.INVALID_HEADER.recovery` | Nombrá todas las columnas del encabezado y volvé a enviar. | Nomeie todas as colunas do cabeçalho e envie de novo. |
| `failures.DUPLICATE_HEADER.title` | El CSV repite un encabezado | O CSV repete um cabeçalho |
| `failures.DUPLICATE_HEADER.body` | Dos columnas del archivo tienen el mismo nombre y no hay forma de decidir cuál gana. | Duas colunas do arquivo têm o mesmo nome e não há como decidir qual vale. |
| `failures.DUPLICATE_HEADER.recovery` | Dejá una sola columna por campo y volvé a enviar. | Deixe uma só coluna por campo e envie de novo. |
| `failures.UNKNOWN_HEADER.title` | El CSV trae una columna que la importación no conoce | O CSV traz uma coluna que a importação não conhece |
| `failures.UNKNOWN_HEADER.body` | El archivo se rechaza entero antes que ignorar datos en silencio: una columna desconocida suele ser un archivo equivocado o un campo mal escrito. | O arquivo é rejeitado inteiro em vez de ignorar dados em silêncio: uma coluna desconhecida costuma ser um arquivo errado ou um campo mal escrito. |
| `failures.UNKNOWN_HEADER.recovery` | Quitá o corregí la columna. El detalle técnico dice cuál es. | Remova ou corrija a coluna. O detalhe técnico diz qual é. |
| `failures.INVALID_SORT.title` | La consola pidió un orden que la API no reconoce | O console pediu uma ordenação que a API não reconhece |
| `failures.INVALID_SORT.body` | El feed pidió ordenar las alertas por un valor que el contrato no admite. Es un error interno: no debería ocurrir desde esta pantalla. | A fila pediu para ordenar os alertas por um valor que o contrato não admite. É um erro interno: não deveria acontecer nesta tela. |
| `failures.INVALID_SORT.recovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata. |
| `failures.INVALID_SEVERITY.title` | La consola pidió una severidad que la API no reconoce | O console pediu uma severidade que a API não reconhece |
| `failures.INVALID_SEVERITY.body` | El feed filtró por una severidad que el contrato no admite. Es un error interno: no debería ocurrir desde esta pantalla. | A fila filtrou por uma severidade que o contrato não admite. É um erro interno: não deveria acontecer nesta tela. |
| `failures.INVALID_SEVERITY.recovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata. |
| `failures.INVALID_PAGE.title` | La consola pidió una página inexistente | O console pediu uma página inexistente |
| `failures.INVALID_PAGE.body` | El feed pidió un número de página fuera de rango. Es un error interno: no debería ocurrir desde esta pantalla. | A fila pediu um número de página fora da faixa. É um erro interno: não deveria acontecer nesta tela. |
| `failures.INVALID_PAGE.recovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata. |
| `failures.EXTERNAL_EVALUATION_PENDING.title` | Ya hay una evaluación externa esperando al proveedor | Já há uma avaliação externa aguardando o provedor |
| `failures.EXTERNAL_EVALUATION_PENDING.body` | Este pedido tiene una evaluación externa que el proveedor todavía no respondió. Solo puede haber una a la vez: pedir otra crearía una segunda evaluación del lado del proveedor por una pregunta que ya está hecha. | Este pedido tem uma avaliação externa que o provedor ainda não respondeu. Só pode haver uma por vez: pedir outra criaria uma segunda avaliação do lado do provedor para uma pergunta que já foi feita. |
| `failures.EXTERNAL_EVALUATION_PENDING.recovery` | Esperá el callback del proveedor, o ejecutá una reconciliación para volver a preguntarle. | Espere o callback do provedor, ou execute uma reconciliação para perguntar de novo. |
| `failures.EXTERNAL_EVALUATION_SETTLED.title` | El proveedor ya se pronunció sobre este pedido | O provedor já se pronunciou sobre este pedido |
| `failures.EXTERNAL_EVALUATION_SETTLED.body` | La evaluación externa vigente tiene veredicto. Solo se puede volver a pedir cuando la anterior terminó en error. | A avaliação externa vigente tem veredito. Só é possível pedir de novo quando a anterior terminou em erro. |
| `failures.EXTERNAL_EVALUATION_SETTLED.recovery` | El veredicto del proveedor se muestra en el bloque de evaluación externa. | O veredito do provedor é exibido no bloco de avaliação externa. |
| `failures.EXTERNAL_EVALUATION_CONFLICT.title` | Otra escritura tocó la evaluación externa al mismo tiempo | Outra escrita tocou a avaliação externa ao mesmo tempo |
| `failures.EXTERNAL_EVALUATION_CONFLICT.body` | Alguien más movió esta evaluación externa mientras se procesaba tu pedido, así que el tuyo no se aplicó. | Mais alguém moveu esta avaliação externa enquanto o seu pedido era processado, então o seu não foi aplicado. |
| `failures.EXTERNAL_EVALUATION_CONFLICT.recovery` | Recargá la alerta para ver el estado registrado. El estado que se muestra es el que quedó guardado. | Recarregue o alerta para ver o estado registrado. O estado exibido é o que ficou guardado. |
| `failures.EXTERNAL_EVALUATION_NOT_FOUND.title` | Esa evaluación externa ya no existe | Essa avaliação externa não existe mais |
| `failures.EXTERNAL_EVALUATION_NOT_FOUND.body` | La evaluación externa que se quiso usar no está en la base. Puede haberse importado un corpus nuevo desde que abriste el enlace. | A avaliação externa que se quis usar não está na base. Um corpus novo pode ter sido importado desde que você abriu o link. |
| `failures.EXTERNAL_EVALUATION_NOT_FOUND.recovery` | Recargá la alerta para ver el estado registrado. | Recarregue o alerta para ver o estado registrado. |
| `failures.PROVIDER_NOT_REGISTERED.title` | Esta instalación no tiene adaptador para ese proveedor | Esta instalação não tem adaptador para esse provedor |
| `failures.PROVIDER_NOT_REGISTERED.body` | El proveedor externo que se pidió no está registrado en esta API. El MVP trae únicamente el proveedor simulado; el sandbox de Koin es post-MVP. | O provedor externo pedido não está registrado nesta API. O MVP traz apenas o provedor simulado; o sandbox da Koin é pós-MVP. |
| `failures.PROVIDER_NOT_REGISTERED.recovery` | Revisá la configuración de la API. Con KOIN_MODE=mock queda registrado el proveedor simulado. | Revise a configuração da API. Com KOIN_MODE=mock fica registrado o provedor simulado. |
| `failures.INVALID_PROVIDER.title` | La consola pidió un proveedor que la API no reconoce | O console pediu um provedor que a API não reconhece |
| `failures.INVALID_PROVIDER.body` | El nombre de proveedor enviado no está en el catálogo del contrato. Es un error interno: no debería ocurrir desde esta pantalla. | O nome de provedor enviado não está no catálogo do contrato. É um erro interno: não deveria acontecer nesta tela. |
| `failures.INVALID_PROVIDER.recovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata. |
| `failures.RECONCILIATION_CONFLICT.title` | Otra escritura se llevó todas las evaluaciones del barrido | Outra escrita levou todas as avaliações da varredura |
| `failures.RECONCILIATION_CONFLICT.body` | Cada evaluación que el barrido examinó fue movida por otra escritura antes de que pudiera guardar la suya, así que el barrido no aplicó nada. | Cada avaliação que a varredura examinou foi movida por outra escrita antes que pudesse salvar a sua, então a varredura não aplicou nada. |
| `failures.RECONCILIATION_CONFLICT.recovery` | Volvé a ejecutarlo. Si alguien más está usando la consola, esperá a que termine. | Execute de novo. Se mais alguém estiver usando o console, espere terminar. |
| `failures.CALLBACK_UNAUTHORIZED.title` | La API rechazó el callback por autenticación | A API rejeitou o callback por autenticação |
| `failures.CALLBACK_UNAUTHORIZED.body` | El endpoint de callbacks exige un secreto compartido y no llegó, o no coincide. Es un error interno: esta consola nunca envía callbacks, porque el secreto no vive en el proceso de Next. | O endpoint de callbacks exige um segredo compartilhado e ele não chegou, ou não confere. É um erro interno: este console nunca envia callbacks, porque o segredo não vive no processo do Next. |
| `failures.CALLBACK_UNAUTHORIZED.recovery` | Revisá SALVO_CALLBACK_SHARED_SECRET en la configuración de la API. Si vuelve a pasar desde esta pantalla, reportalo con la hora exacta. | Revise SALVO_CALLBACK_SHARED_SECRET na configuração da API. Se acontecer outra vez nesta tela, reporte com a hora exata. |
| `failures.CALLBACK_UNAVAILABLE.title` | La evaluación externa está siendo escrita por otra petición | A avaliação externa está sendo escrita por outra requisição |
| `failures.CALLBACK_UNAVAILABLE.body` | El callback no se pudo guardar porque otra escritura ganó la carrera dos veces seguidas. No se aplicó nada a medias: o entra entero o no entra. | O callback não pôde ser salvo porque outra escrita ganhou a corrida duas vezes seguidas. Nada foi aplicado pela metade: ou entra inteiro ou não entra. |
| `failures.CALLBACK_UNAVAILABLE.recovery` | Volvé a intentarlo en unos segundos. | Tente de novo em alguns segundos. |
| `failures.INVALID_CALLBACK.title` | El cuerpo del callback no es válido | O corpo do callback não é válido |
| `failures.INVALID_CALLBACK.body` | Al mensaje le falta con qué correlacionarlo, o su estado no es uno de los admitidos. Es un error interno: esta consola no compone callbacks. | Falta com o que correlacionar a mensagem, ou seu estado não é um dos admitidos. É um erro interno: este console não compõe callbacks. |
| `failures.INVALID_CALLBACK.recovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata. |
| `failures.CALLBACK_TOO_LARGE.title` | El cuerpo del callback supera el máximo admitido | O corpo do callback passa do máximo admitido |
| `failures.CALLBACK_TOO_LARGE.body` | Un callback pesa unos pocos bytes y la API fija un límite explícito. El cuerpo enviado lo excede. | Um callback pesa alguns poucos bytes e a API fixa um limite explícito. O corpo enviado o excede. |
| `failures.CALLBACK_TOO_LARGE.recovery` | Revisá qué está enviando el proveedor. Si vuelve a pasar desde esta pantalla, reportalo con la hora exacta. | Revise o que o provedor está enviando. Se acontecer outra vez nesta tela, reporte com a hora exata. |
| `failures.EXPLANATION_PENDING.title` | La explicación se está redactando ahora mismo | A explicação está sendo redigida agora |
| `failures.EXPLANATION_PENDING.body` | Ya hay una petición en curso para esta evaluación y todavía no respondió. Pedir otra pagaría dos veces la misma redacción. | Já há uma requisição em curso para esta avaliação e ela ainda não respondeu. Pedir outra pagaria duas vezes a mesma redação. |
| `failures.EXPLANATION_PENDING.recovery` | Esperá unos segundos y recargá la alerta: el texto aparece en el bloque de explicación. | Espere alguns segundos e recarregue o alerta: o texto aparece no bloco de explicação. |
| `failures.EXPLANATION_ALREADY_READY.title` | Esta evaluación ya tiene su explicación escrita | Esta avaliação já tem sua explicação escrita |
| `failures.EXPLANATION_ALREADY_READY.body` | Una explicación escrita no se regenera. Es el registro de lo que se pudo leer al decidir, y reemplazarla borraría el texto que alguien pudo haber tenido delante. | Uma explicação escrita não é regerada. É o registro do que se pôde ler ao decidir, e substituí-la apagaria o texto que alguém pode ter tido diante dos olhos. |
| `failures.EXPLANATION_ALREADY_READY.recovery` | El texto vigente se muestra en el bloque de explicación. Una evaluación distinta tiene su propia explicación. | O texto vigente é exibido no bloco de explicação. Uma avaliação diferente tem a sua própria explicação. |
| `failures.EXPLANATION_ATTEMPTS_EXHAUSTED.title` | Se agotaron los intentos de explicar esta evaluación | As tentativas de explicar esta avaliação se esgotaram |
| `failures.EXPLANATION_ATTEMPTS_EXHAUSTED.body` | La redacción falló todas las veces que el presupuesto de intentos permite. El tope existe para que un proveedor que falla en cadena no se cobre indefinidamente. | A redação falhou todas as vezes que o orçamento de tentativas permite. O teto existe para que um provedor que falha em cadeia não cobre indefinidamente. |
| `failures.EXPLANATION_ATTEMPTS_EXHAUSTED.recovery` | Revisá el motivo del último intento en el bloque de explicación. El veredicto no necesita una explicación para emitirse. | Revise o motivo da última tentativa no bloco de explicação. O veredito não precisa de uma explicação para ser emitido. |
| `failures.EXPLANATION_CONFLICT.title` | Otra escritura tocó la explicación al mismo tiempo | Outra escrita tocou a explicação ao mesmo tempo |
| `failures.EXPLANATION_CONFLICT.body` | Alguien más movió esta explicación mientras se procesaba tu pedido, así que el tuyo no se aplicó. | Mais alguém moveu esta explicação enquanto o seu pedido era processado, então o seu não foi aplicado. |
| `failures.EXPLANATION_CONFLICT.recovery` | Recargá la alerta para ver el estado registrado. El estado que se muestra es el que quedó guardado. | Recarregue o alerta para ver o estado registrado. O estado exibido é o que ficou guardado. |
| `failures.INVALID_PAGE_SIZE.title` | La consola pidió un tamaño de página fuera de rango | O console pediu um tamanho de página fora da faixa |
| `failures.INVALID_PAGE_SIZE.body` | El feed pidió más alertas por página de las que la API entrega. Es un error interno: no debería ocurrir desde esta pantalla. | A fila pediu mais alertas por página do que a API entrega. É um erro interno: não deveria acontecer nesta tela. |
| `failures.INVALID_PAGE_SIZE.recovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata. |
| `transport.timeoutTitle` | La API tardó demasiado en responder | A API demorou demais para responder |
| `transport.timeoutBody` | La consulta se canceló para no dejar la pantalla colgada. No se llegó a leer ni a guardar nada. | A consulta foi cancelada para não deixar a tela travada. Nada chegou a ser lido nem salvo. |
| `transport.timeoutRecovery` | Volvé a intentarlo. Si persiste, revisá que el proceso de la API esté respondiendo. | Tente de novo. Se persistir, verifique se o processo da API está respondendo. |
| `transport.unreachableTitle` | No se pudo contactar a la API | Não foi possível contatar a API |
| `transport.unreachableBody` | La consola no tiene con qué trabajar: todo el riesgo se calcula en el backend y ahora mismo no responde. | O console não tem com o que trabalhar: todo o risco é calculado no backend e agora mesmo ele não responde. |
| `transport.unreachableRecovery` | Verificá que la API esté levantada en la dirección configurada y recargá. | Verifique se a API está no ar no endereço configurado e recarregue. |
| `transport.malformedTitle` | La respuesta de la API no coincide con el contrato | A resposta da API não corresponde ao contrato |
| `transport.malformedBody` | Llegó una respuesta que a esta consola le falta o le sobra algo respecto del contrato con el que se construyó. No se muestra nada antes que mostrar algo mal leído. | Chegou uma resposta em que falta ou sobra algo a este console em relação ao contrato com que ele foi construído. Nada é exibido antes de exibir algo mal lido. |
| `transport.malformedRecovery` | Es probable que la API y la consola estén en versiones distintas. Regenerá los tipos desde OpenAPI y volvé a desplegar. | É provável que a API e o console estejam em versões diferentes. Gere os tipos de novo a partir do OpenAPI e faça o deploy outra vez. |
| `transport.unknownTitle` | La API rechazó la operación | A API rejeitou a operação |
| `transport.unknownBody` | Respondió {status} con un motivo que esta consola todavía no traduce. | Respondeu {status} com um motivo que este console ainda não traduz. |
| `transport.unknownRecovery` | Recargá la página. Si vuelve a pasar, reportalo con la hora exacta y el detalle técnico. | Recarregue a página. Se acontecer outra vez, reporte com a hora exata e o detalhe técnico. |
