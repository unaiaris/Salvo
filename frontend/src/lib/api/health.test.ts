import { describe, expect, it, vi } from "vitest";

import { getHealth } from "./health";

describe("getHealth", () => {
  it("valida y devuelve el contrato de salud", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify({ status: "ok", service: "salvo-api" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(getHealth(fetcher)).resolves.toEqual({
      status: "ok",
      service: "salvo-api",
    });
    expect(fetcher).toHaveBeenCalledWith("/api/health");
  });

  it("rechaza respuestas que no respetan el contrato", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify({ status: "unknown" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(getHealth(fetcher)).rejects.toThrow(/expected contract/i);
  });
});
