import type { ReactNode } from "react";

/**
 * One of the three things this page offers, with the sentence that says what it does and — more to
 * the point — what it does not do. It is a server component: the explanatory copy never needs to
 * cross the boundary, only the form inside it does.
 */
export function ActionSection({
  id,
  title,
  description,
  children,
}: {
  /**
   * Clave de la sección, en el código y no en el diccionario.
   *
   * Se derivaba del título traducido, y eso ata un identificador del documento al idioma del
   * despliegue: dos títulos que difieran solo en acentos o en `ç` colapsan en el mismo `id`, y
   * entonces un `aria-labelledby` rotula una sección con el título de otra.
   */
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly children: ReactNode;
}) {
  const headingId = `section-${id}`;

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
