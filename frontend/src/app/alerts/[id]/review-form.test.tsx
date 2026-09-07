import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { ReviewForm } from "./review-form";
import { INITIAL_REVIEW_STATE, type ReviewFormState } from "./review-state";

/**
 * The form is exercised against a stubbed action so the assertions are about what the analyst sees,
 * not about the transport. `review-action.test.ts` covers the other half.
 *
 * Interaction goes through `fireEvent` rather than `user-event`: the latter is not a dependency of
 * this project and the stage authorises only one new one. The fields are controlled, so a `change`
 * event is exactly what a keystroke would produce, and submission is dispatched on the form because
 * jsdom does not implement native form submission — React intercepts the event either way.
 */

function choose(name: RegExp) {
  fireEvent.click(screen.getByRole("radio", { name }));
}

function typeNote(text: string) {
  fireEvent.change(screen.getByRole("textbox"), { target: { value: text } });
}

function submit() {
  const form = screen.getByRole("button", { name: /Registrar veredicto/i }).closest("form");
  if (form === null) {
    throw new Error("El botón de envío no está dentro de un formulario.");
  }

  fireEvent.submit(form);
}
const reviewAlert = vi.hoisted(() => vi.fn());

vi.mock("./review-action", () => ({ reviewAlert }));

function conflict(overrides: Partial<ReviewFormState> = {}): ReviewFormState {
  return {
    ...INITIAL_REVIEW_STATE,
    outcome: "failed",
    title: "Otra persona ya revisó esta alerta",
    body: "La alerta quedó cerrada con un veredicto distinto.",
    recovery: "Recargá la alerta para ver el estado registrado.",
    technicalDetail: "Alert was already reviewed.",
    submissionId: 1,
    ...overrides,
  };
}

beforeEach(() => {
  reviewAlert.mockReset();
});

afterEach(() => {
  vi.clearAllMocks();
});

describe("formulario de revisión", () => {
  it("no envía sin veredicto elegido", () => {
    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    expect(screen.getByRole("button", { name: /Registrar veredicto/i })).toBeDisabled();
  });

  it("habilita el envío al elegir un veredicto", async () => {
    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    choose(/Confirmar segura/i);

    expect(screen.getByRole("button", { name: /Registrar veredicto/i })).toBeEnabled();
  });

  it("con divergencia de banda bloquea el envío hasta reconocerla", async () => {
    render(
      <ReviewForm
        alertId="a1"
        explanationId=""
        requiresAcknowledgement
        divergenceSummary="La alerta se abrió en CRÍTICA con score 100. La evaluación vigente está en MEDIA con score 45."
      language="es"
      />,
    );

    choose(/Reportar fraude/i);

    const submitButton = screen.getByRole("button", { name: /Registrar veredicto/i });
    expect(submitButton).toBeDisabled();
    expect(screen.getByText(/Marcá la casilla de arriba/i)).toBeInTheDocument();
    expect(screen.getByText(/score 100/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("checkbox"));

    expect(submitButton).toBeEnabled();
  });

  it("sin divergencia de banda no hay casilla que bloquee", async () => {
    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    choose(/Confirmar segura/i);

    expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Registrar veredicto/i })).toBeEnabled();
  });

  it("la nota sobrevive a un 409", async () => {
    // React 19 restablece los campos no controlados de un <form action> al terminar la acción,
    // también con error. Sin esto, un 409 borraría lo que la analista escribió.
    const note = "El comprador tiene tres pedidos previos entregados sin contracargo.";
    reviewAlert.mockResolvedValue(conflict({ submittedNote: note, submittedStatus: "CONFIRMED_SAFE" }));

    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    choose(/Confirmar segura/i);
    typeNote(note);
    submit();

    expect(await screen.findByText(/Otra persona ya revisó esta alerta/i)).toBeInTheDocument();
    expect(screen.getByRole("textbox")).toHaveValue(note);
    expect(screen.getByRole("radio", { name: /Confirmar segura/i })).toBeChecked();
  });

  it("mantiene marcado el reconocimiento tras un conflicto", async () => {
    reviewAlert.mockResolvedValue(
      conflict({ submittedNote: "", submittedStatus: "REPORTED_FRAUD", acknowledged: true }),
    );

    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement divergenceSummary="Cambió la banda." language="es" />);

    choose(/Reportar fraude/i);
    fireEvent.click(screen.getByRole("checkbox"));
    submit();

    expect(await screen.findByRole("alert")).toBeInTheDocument();
    expect(screen.getByRole("checkbox")).toBeChecked();
  });

  it("muestra el detalle de la API como información secundaria, no como el texto principal", async () => {
    reviewAlert.mockResolvedValue(conflict({ submittedStatus: "CONFIRMED_SAFE" }));

    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    choose(/Confirmar segura/i);
    submit();

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent(/Otra persona ya revisó esta alerta/i);
    expect(alert).toHaveTextContent(/Detalle técnico de la API: Alert was already reviewed\./);
  });

  it("limita la nota a 2000 caracteres en el propio campo", () => {
    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    expect(screen.getByRole("textbox")).toHaveAttribute("maxlength", "2000");
  });

  it("manda el id de la alerta en el formulario, no como prop de servidor", async () => {
    reviewAlert.mockResolvedValue(conflict());

    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    choose(/Confirmar segura/i);
    submit();

    await screen.findByRole("alert");

    const [, formData] = reviewAlert.mock.calls[0] as [unknown, FormData];
    expect(formData.get("alertId")).toBe("a1");
  });

  /**
   * D10: the review records which explanation was on screen, and nothing more than that.
   *
   * The id travels; the text does not. Seeding the note with the summary would put a provider's
   * prose into the audit trail under a human signature, which is the one thing the design forbids by
   * name — and the form could not do it even by accident, because it never receives the summary.
   */
  it("manda el id de la explicación que la analista tenía delante", async () => {
    reviewAlert.mockResolvedValue(conflict());

    render(
      <ReviewForm
        alertId="a1"
        explanationId="6f6b7f3e-0000-4000-8000-000000000006"
        requiresAcknowledgement={false}
        divergenceSummary=""
        language="es"
      />,
    );

    choose(/Confirmar segura/i);
    submit();

    await screen.findByRole("alert");

    const [, formData] = reviewAlert.mock.calls[0] as [unknown, FormData];
    expect(formData.get("explanationId")).toBe("6f6b7f3e-0000-4000-8000-000000000006");
  });

  it("sin explicación manda el campo vacío y la revisión sigue funcionando igual", async () => {
    reviewAlert.mockResolvedValue(conflict());

    render(<ReviewForm alertId="a1" explanationId="" requiresAcknowledgement={false} divergenceSummary="" language="es" />);

    choose(/Confirmar segura/i);
    submit();

    await screen.findByRole("alert");

    const [, formData] = reviewAlert.mock.calls[0] as [unknown, FormData];
    expect(formData.get("explanationId")).toBe("");
    expect(formData.get("newStatus")).toBe("CONFIRMED_SAFE");
  });

  it("nunca precarga la nota con el resumen de la explicación", () => {
    render(
      <ReviewForm
        alertId="a1"
        explanationId="6f6b7f3e-0000-4000-8000-000000000006"
        requiresAcknowledgement={false}
        divergenceSummary=""
        language="es"
      />,
    );

    expect(screen.getByRole("textbox")).toHaveValue("");
  });
});
