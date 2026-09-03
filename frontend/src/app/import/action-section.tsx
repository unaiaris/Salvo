import type { ReactNode } from "react";

/**
 * One of the three things this page offers, with the sentence that says what it does and — more to
 * the point — what it does not do. It is a server component: the explanatory copy never needs to
 * cross the boundary, only the form inside it does.
 */
export function ActionSection({
  title,
  description,
  children,
}: {
  readonly title: string;
  readonly description: string;
  readonly children: ReactNode;
}) {
  const headingId = `section-${title.toLowerCase().replaceAll(/[^a-záéíóúñ]+/g, "-")}`;

  return (
    <section
      aria-labelledby={headingId}
      className="rounded-lg border border-slate-200 bg-white p-6"
    >
      <h2 id={headingId} className="text-lg font-semibold text-slate-900">
        {title}
      </h2>
      <p className="mt-1 mb-5 max-w-3xl text-sm leading-6 text-slate-600">{description}</p>
      {children}
    </section>
  );
}
