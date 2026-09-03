import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import Home from "./page";

describe("Home", () => {
  it("explica la frontera entre el backend y la interfaz", () => {
    render(<Home />);

    expect(
      screen.getByRole("heading", {
        level: 1,
        name: /núcleo \.NET auditable/i,
      }),
    ).toBeInTheDocument();
    expect(screen.getByText(/contrato OpenAPI/i)).toBeInTheDocument();
  });

  it("lleva a la cola de alertas", () => {
    render(<Home />);

    expect(screen.getByRole("link", { name: /cola de alertas/i })).toHaveAttribute(
      "href",
      "/alerts",
    );
  });
});
