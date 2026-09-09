import { render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireCapabilities } from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import RootLayout from "./layout";

/**
 * The notice of the public instance, and why it is tested on the layout rather than on the notice.
 *
 * Decision 70 asks for it on <em>every</em> screen, and the only way to promise that without
 * touching every page is to render it in the frame every page is wrapped in. So what has to be
 * asserted is not that the component composes its sentences — it would, alone, forever — but that
 * the frame puts it there, takes its figure from the deployment, and leaves it out where the
 * deployment did not ask for it.
 *
 * The layout renders `<html>` and `<body>`, which React will not mount inside a jsdom document
 * that already has them. The tree is asked for its children instead: what is under test is what
 * the frame decides to render, not the document shell it renders into.
 */
let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal("fetch", fetchMock);
  vi.stubEnv("SALVO_API_BASE_URL", "http://127.0.0.1:5100");
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

function respondWith(overrides: Record<string, unknown> = {}) {
  fetchMock.mockImplementation(() =>
    Promise.resolve(jsonResponse(wireCapabilities(overrides))),
  );
}

/**
 * The layout, rendered without its `<html>`/`<body>` shell.
 *
 * `renderableServerTree` resolves the async server components; the shell is then unwrapped so that
 * jsdom is not asked to nest a second document inside its own.
 */
async function renderLayout() {
  const tree = await renderableServerTree(
    RootLayout({ children: <p>contenido de la pantalla</p> }),
  );
  const html = tree as React.ReactElement<{ children: React.ReactElement }>;
  const body = html.props.children as React.ReactElement<{ children: React.ReactNode }>;

  render(<>{body.props.children}</>);
}

describe("el aviso de la instancia compartida", () => {
  it("dice las tres cosas, con el máximo que declara el despliegue", async () => {
    respondWith({ sharedInstance: true, resetMinutes: 30 });
    await renderLayout();

    const notice = screen.getByRole("complementary", { name: "Sobre esta instancia" });

    expect(within(notice).getByText(/Instancia de demostración compartida/)).toBeInTheDocument();
    expect(within(notice).getByText(/Los datos son sintéticos/)).toBeInTheDocument();
    expect(within(notice).getByText(/lo ve todo el mundo/)).toBeInTheDocument();
    expect(notice).toHaveTextContent(/como máximo cada 30 minutos/);
  });

  /**
   * The main restart is the platform sleeping the instance, and a deployment that relies only on
   * that has no maximum to promise. Promising one anyway would be the console making a claim the
   * container does not honour.
   */
  it("no promete un máximo cuando el despliegue no declara ninguno", async () => {
    respondWith({ sharedInstance: true, resetMinutes: null });
    await renderLayout();

    const notice = screen.getByRole("complementary", { name: "Sobre esta instancia" });

    expect(notice).toHaveTextContent(/queda un rato sin visitas/);
    expect(notice).not.toHaveTextContent(/como máximo/);
  });

  it("se compone en el idioma del despliegue", async () => {
    respondWith({ sharedInstance: true, resetMinutes: 30, language: "pt" });
    await renderLayout();

    const notice = screen.getByRole("complementary", { name: "Sobre esta instância" });

    expect(within(notice).getByText(/todo mundo vê/)).toBeInTheDocument();
    expect(notice).toHaveTextContent(/no máximo a cada 30 minutos/);
  });

  it("no aparece en un despliegue que no se declara compartido", async () => {
    respondWith({ sharedInstance: false, resetMinutes: null });
    await renderLayout();

    expect(screen.queryByRole("complementary")).not.toBeInTheDocument();
    expect(screen.queryByText(/lo ve todo el mundo/)).not.toBeInTheDocument();
  });
});

describe("la línea del encabezado", () => {
  /**
   * It used to say «uso local» always, in both languages. On a public instance that is not a
   * nuance: it is false, and it sits next to a console anybody can write into.
   */
  it("deja de decir «uso local» en una instancia compartida", async () => {
    respondWith({ sharedInstance: true, resetMinutes: 30 });
    await renderLayout();

    expect(screen.getByText(/instancia compartida/)).toBeInTheDocument();
    expect(screen.queryByText(/uso local/)).not.toBeInTheDocument();
  });

  it("sigue diciendo «uso local» donde lo es", async () => {
    respondWith({ sharedInstance: false });
    await renderLayout();

    expect(screen.getByText(/uso local/)).toBeInTheDocument();
  });
});
