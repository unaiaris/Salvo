import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { FailureNotice } from "@/components/failure-notice";
import { StartingUpNotice } from "@/components/starting-up-notice";
import type { ApiFailure } from "@/lib/api/failures";
import { accessibilityReport } from "@/test/axe";

/**
 * La pantalla del arranque en frío, y la bifurcación que decide cuándo se ve.
 *
 * Lo que estas pruebas cuidan no es el texto sino **quién lo ve**. La misma condición que hace útil
 * a esta pantalla en la instancia pública la haría dañina en una máquina de desarrollo, donde una
 * API caída tiene que decirse y no esperarse.
 */

beforeEach(() => {
  vi.unstubAllEnvs();
});

afterEach(() => {
  vi.unstubAllEnvs();
});

const timeout: ApiFailure = { kind: "timeout" };
const unreachable: ApiFailure = { kind: "unreachable" };
const problem: ApiFailure = {
  kind: "problem",
  status: 409,
  code: "ORDER_LIMIT_REACHED",
  detail: null,
};

describe("la pantalla de arranque", () => {
  it("no se anuncia como una alerta", async () => {
    // Un `role="alert"` interrumpe a un lector de pantalla. Que la instancia esté despertando no
    // merece interrumpir a nadie, y es la diferencia de marcado que separa esta pantalla de un
    // error.
    render(<StartingUpNotice language="es" />);

    const region = screen.getByRole("status");
    expect(region).toHaveAttribute("aria-live", "polite");
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("dice qué pasa y cuánto suele tardar", async () => {
    render(<StartingUpNotice language="es" />);

    expect(screen.getByRole("heading", { name: /levantando/i })).toBeInTheDocument();
    expect(screen.getByText(/60 segundos/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /reintentar/i })).toBeInTheDocument();
  });

  it("existe en portugués con el mismo marcado", async () => {
    render(<StartingUpNotice language="pt" />);

    expect(screen.getByRole("heading", { name: /subindo/i })).toBeInTheDocument();
    expect(screen.getByText(/60 segundos/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /tentar de novo/i })).toBeInTheDocument();
  });

  it("el reintento no necesita JavaScript: es un GET contra la misma URL", async () => {
    // Sin `action` el formulario reemite la petición actual, y sin `method` va como GET. Las dos
    // ausencias son la funcionalidad: un POST contaría contra el cubo de mutaciones del limitador.
    const { container } = render(<StartingUpNotice language="es" />);
    const form = container.querySelector("form");

    expect(form).not.toBeNull();
    expect(form?.getAttribute("action")).toBeNull();
    expect(form?.getAttribute("method")).toBeNull();
  });

  it("no usa un meta refresh, que axe-core marca como violación", async () => {
    // La comprobación de accesibilidad de este proyecto corre sobre el contenedor, y React iza los
    // `<meta>` al `<head>`: un meta refresh habría pasado en verde sin que nadie viera la
    // violación. Este test mira el documento entero, que es donde sí se vería.
    render(<StartingUpNotice language="es" />);

    expect(document.head.innerHTML).not.toContain("refresh");
  });

  it("no tiene violaciones de accesibilidad, en los dos idiomas", async () => {
    for (const language of ["es", "pt"] as const) {
      const { container, unmount } = render(<StartingUpNotice language={language} />);
      expect(await accessibilityReport(container), language).toBe("");
      unmount();
    }
  });
});

describe("cuándo se ve la pantalla de arranque en vez del error", () => {
  it("en la instancia compartida, un timeout se muestra como arranque", async () => {
    vi.stubEnv("SharedInstance__Enabled", "true");

    render(<FailureNotice failure={timeout} language="es" />);

    expect(screen.getByRole("status")).toBeInTheDocument();
    expect(screen.queryByText(/tardó demasiado/i)).toBeNull();
  });

  it("en la instancia compartida, una API inalcanzable también", async () => {
    vi.stubEnv("SharedInstance__Enabled", "true");

    render(<FailureNotice failure={unreachable} language="es" />);

    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("fuera de la instancia compartida, un timeout sigue siendo un error", async () => {
    // Es el escenario 3 de `scripts/smoke-ui.sh`, que apaga la API a propósito. Si esto cambiara,
    // ese escenario dejaría de comprobar lo que comprueba.
    vi.stubEnv("SharedInstance__Enabled", "");

    render(<FailureNotice failure={timeout} language="es" />);

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText(/tardó demasiado/i)).toBeInTheDocument();
    expect(screen.queryByRole("status")).toBeNull();
  });

  it("en la instancia compartida, un problema de la API sigue siendo un error", async () => {
    // Un 409 es la API contestando. Disfrazarlo de «estamos arrancando» sería mentir sobre algo que
    // el visitante puede corregir.
    vi.stubEnv("SharedInstance__Enabled", "true");

    render(<FailureNotice failure={problem} language="es" />);

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.queryByRole("status")).toBeNull();
  });
});
