import type { Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * What this instance is, said on the screen.
 *
 * <h3>Why it is here and not in the README</h3>
 *
 * Decision 70 lets a public demonstration run without authentication, and it attaches three
 * conditions to that. Two of them are properties of the deployment: the data is synthetic, and the
 * restart does not depend on anybody remembering. The third is this notice, and it is deliberately
 * a condition rather than a courtesy — a visitor who does not know the instance is shared has no
 * way to find out, because nothing else on the screen says so.
 *
 * <strong>The middle sentence is the one that is not optional.</strong> A review note is free text
 * typed by a person, anonymous, and visible to everyone who opens the console until the next
 * restart. Anyone writing into this console is writing in public, and that is exactly the kind of
 * thing a person has to be told <em>before</em> they type, not after.
 *
 * <h3>Why it renders in the layout</h3>
 *
 * Every screen, without a screen having to opt in. Putting it in each page would make a page added
 * later a page that quietly ships without it, and the failure would be invisible: the console would
 * look finished.
 *
 * It is not a live region and not an alert. Nothing about it changes while somebody reads, and a
 * status role would make a screen reader announce it over whatever the reader was doing. It is a
 * labelled landmark instead, so it can be found on purpose and skipped by everyone else.
 */
export function SharedInstanceNotice({
  language,
  resetMinutes,
}: {
  readonly language: Language;
  readonly resetMinutes: number | null;
}) {
  const { t, formatCount } = formatting(language);
  const notice = t.sharedInstance;

  return (
    <aside
      aria-label={notice.label}
      className="border-b border-amber-300 bg-amber-100 text-amber-950"
    >
      <p className="mx-auto max-w-6xl px-6 py-2 text-xs leading-5">
        <strong className="font-semibold">{notice.title}</strong>{" "}
        {notice.synthetic}{" "}
        <strong className="font-semibold">{notice.everyoneSees}</strong>{" "}
        {resetMinutes === null
          ? notice.resets
          : notice.resetsWithMaximum(formatCount(resetMinutes))}
      </p>
    </aside>
  );
}
