import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireCapabilities } from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import Home from "./page";

/**
 * The entry page holds no data of its own and still reads one thing: the language of the
 * deployment, which decides the words on it. That makes it an async server component, so it is
 * resolved the way every other page is in these tests.
 */
let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal("fetch", fetchMock);
  vi.stubEnv("SALVO_API_BASE_URL", "http://127.0.0.1:5100");
  fetchMock.mockImplementation(() => Promise.resolve(jsonResponse(wireCapabilities())));
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

async function renderHome() {
  render(await renderableServerTree(Home()));
}

describe("Home", () => {
  it("explica la frontera entre el backend y la interfaz", async () => {
    await renderHome();

    expect(
      screen.getByRole("heading", {
        level: 1,
        name: /núcleo \.NET auditable/i,
      }),
    ).toBeInTheDocument();
    expect(screen.getByText(/contrato OpenAPI/i)).toBeInTheDocument();
  });

  it("lleva a la cola de alertas", async () => {
    await renderHome();

    expect(screen.getByRole("link", { name: /cola de alertas/i })).toHaveAttribute(
      "href",
      "/alerts",
    );
  });

  /**
   * The words come from the deployment, not from the file. It is the cheapest proof that the entry
   * page went through the dictionary rather than keeping its own Spanish.
   */
  it("se compone en el idioma del despliegue", async () => {
    fetchMock.mockImplementation(() =>
      Promise.resolve(jsonResponse(wireCapabilities({ language: "pt" }))),
    );

    await renderHome();

    expect(screen.getByRole("link", { name: /fila de alertas/i })).toBeInTheDocument();
  });
});
