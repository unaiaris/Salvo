import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import { SeverityBadge } from "@/components/severity-badge";
import { fetchAlert } from "@/lib/api/alerts";
import { deploymentLanguage, fetchCapabilities, languageOf } from "@/lib/api/console";
import type { Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";
import { CurrentEvaluationBlock, SnapshotBlock } from "./evaluation-blocks";
import { ExplanationBlock } from "./explanation-block";
import { ExternalEvaluationBlock } from "./external-block";
import { OrderBlock } from "./order-block";
import { ReviewPanel } from "./review-panel";

/**
 * This route reads `params`, so Next would treat it as dynamic anyway. It declares itself dynamic
 * regardless: the guarantee has to be a property of the file, not a side effect of what it happens
 * to read today.
 */
export const dynamic = "force-dynamic";

export async function generateMetadata() {
  return { title: formatting(await deploymentLanguage()).t.meta.alertDetail };
}

export default async function AlertDetailPage({
  params,
}: {
  readonly params: Promise<{ readonly id: string }>;
}) {
  const { id } = await params;

  // Capabilities decide whether the demo trigger is offered at all, and a failure to read them is
  // not a reason to refuse the page: the alert is still readable without that one button. The
  // language comes from the same response, memoised for this render.
  const [alert, capabilities] = await Promise.all([fetchAlert(id), fetchCapabilities()]);
  const language = languageOf(capabilities);
  const f = formatting(language);

  if (!alert.ok) {
    return (
      <div className="flex flex-col gap-6">
        <BackToFeed language={language} />
        <FailureNotice failure={alert.failure} language={language} />
      </div>
    );
  }

  const detail = alert.value;

  return (
    <div className="flex flex-col gap-6">
      <BackToFeed language={language} />
      <div className="flex flex-col gap-2">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-3xl font-semibold tracking-tight text-slate-950">
            {f.t.alertDetail.title(detail.order.merchantReferenceId)}
          </h1>
          <SeverityBadge severity={detail.severity} language={language} />
          <span className="rounded-full border border-slate-300 bg-white px-2.5 py-0.5 text-xs font-semibold text-slate-700">
            {f.statusLabel(detail.status)}
          </span>
        </div>
        <p className="text-sm text-slate-600">
          {f.t.alertDetail.opened(f.formatInstant(detail.createdAt), detail.alertPolicyVersion)}
          {detail.supersedesAlertId !== null && (
            <>
              {" · "}
              <Link
                href={`/alerts/${detail.supersedesAlertId}`}
                className="underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
              >
                {f.t.alertDetail.supersedes}
              </Link>
            </>
          )}
        </p>
      </div>

      <OrderBlock order={detail.order} language={language} />

      {/*
        Three blocks now, and the third is not a variation of the first two. The snapshot and the
        current evaluation are two moments of the local criterion, so they stay paired at every
        width; the provider's opinion is a different criterion and sits on its own row rather than
        being squeezed into a third column that would read as "one more version of the same thing".
      */}
      <div className="grid gap-6 lg:grid-cols-2">
        <SnapshotBlock
          snapshot={detail.snapshot}
          createdAt={detail.createdAt}
          language={language}
        />
        <CurrentEvaluationBlock
          evaluation={detail.currentEvaluation}
          currentRun={detail.currentRun}
          language={language}
        />
      </div>

      <ExternalEvaluationBlock
        detail={detail}
        language={language}
        triggerEnabled={capabilities.ok && capabilities.value.externalCallbackTriggerEnabled}
      />

      <ExplanationBlock detail={detail} language={language} />

      <ReviewPanel detail={detail} language={language} />
    </div>
  );
}

function BackToFeed({ language }: { readonly language: Language }) {
  return (
    <p>
      <Link
        href="/alerts"
        className="text-sm font-medium text-slate-700 underline underline-offset-4 hover:text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
      >
        {formatting(language).t.alertDetail.back}
      </Link>
    </p>
  );
}
