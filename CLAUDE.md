# Salvo — Instrucciones de proyecto para Claude Code

Entrada automática de Claude Code. Importa las reglas y el estado compartidos; no duplica el
Blueprint ni la política de autonomía.

@AGENTS.md
@DesignAgent/Salvo-Overview.md
@DesignAgent/Salvo-Progress.md
@Coordination/Workboard.md
@ClaudeAgent/Claude-Workflow.md

## Directivas específicas

- Trabajar solo desde un task brief completo basado en
  `Coordination/Task-Brief-Template.md`.
- Identificarse como `Claude` en handoffs y registros de entrega.
- No ejecutar `/init` ni regenerar este archivo.
- En trabajo paralelo, no modificar Workboard ni Progress; entregar evidencia en
  `Coordination/Handoffs/Claude.md`.
- No leer `.env` ni almacenes de credenciales; usar `.env.example`. No copiar secretos, PII,
  tokens, payloads reales ni logs sin redactar en prompts o handoffs.

Las reglas técnicas, de seguridad, autonomía y verificación se heredan de `AGENTS.md`. Si el task
brief las contradice, detenerse y reportar la contradicción.
