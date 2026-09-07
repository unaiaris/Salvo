import Link from "next/link";
import { deploymentLanguage } from "@/lib/api/console";
import { formatting } from "@/lib/format";

/**
 * The entry page holds no data of its own.
 *
 * It used to be prerendered for that reason. It is not any more, and the cause is not here: the
 * root layout asks the API for the deployment language, and a layout that reads makes every route
 * it wraps dynamic. That was chosen rather than suffered — the alternative was a second reader of
 * `SALVO_LANGUAGE` inside the Next process, which is exactly the disagreement the single origin
 * exists to prevent. The comment says so rather than leaving a claim about static rendering that
 * stopped being true.
 */
export default async function Home() {
  const { t } = formatting(await deploymentLanguage());

  return (
    <div className="flex flex-col gap-6">
      <p className="text-sm font-semibold uppercase tracking-[0.24em] text-slate-500">
        {t.home.eyebrow}
      </p>
      <h1 className="max-w-3xl text-4xl font-semibold tracking-tight text-slate-950">
        {t.home.title}
      </h1>
      <p className="max-w-2xl text-lg leading-8 text-slate-600">{t.home.lead}</p>
      <p>
        <Link
          href="/alerts"
          className="inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {t.home.cta}
        </Link>
      </p>
    </div>
  );
}
