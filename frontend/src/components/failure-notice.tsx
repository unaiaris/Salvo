import type { Language } from "@/lib/api/contract";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { formatting } from "@/lib/format";

/**
 * How a failure looks on screen: the console's own sentence first, the API's `detail` underneath as
 * secondary information. Never "Error 409", and never the raw `detail` as the only text.
 */
export function FailureNotice({
  failure,
  language,
  children,
}: {
  readonly failure: ApiFailure;
  readonly language: Language;
  readonly children?: React.ReactNode;
}) {
  const message = describeFailure(failure, language);
  const technicalDetail = failure.kind === "problem" ? failure.detail : null;

  return (
    <section
      role="alert"
      aria-labelledby="failure-title"
      className="rounded-lg border border-rose-200 bg-rose-50 p-5 text-rose-950"
    >
      <h2 id="failure-title" className="text-base font-semibold">
        {message.title}
      </h2>
      <p className="mt-2 text-sm leading-6">{message.body}</p>
      <p className="mt-2 text-sm font-medium leading-6">{message.recovery}</p>
      {technicalDetail !== null && (
        <p className="mt-3 border-t border-rose-200 pt-3 text-xs text-rose-800">
          {formatting(language).t.common.technicalDetail(technicalDetail)}
        </p>
      )}
      {children}
    </section>
  );
}
