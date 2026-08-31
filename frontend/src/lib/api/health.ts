export interface HealthResponse {
  readonly status: "ok";
  readonly service: string;
}

export async function getHealth(
  fetcher: typeof fetch = fetch,
  signal?: AbortSignal,
): Promise<HealthResponse> {
  const response = signal
    ? await fetcher("/api/health", { signal })
    : await fetcher("/api/health");

  if (!response.ok) {
    throw new Error(`Health request failed with status ${response.status}.`);
  }

  const payload: unknown = await response.json();

  if (!isHealthResponse(payload)) {
    throw new Error("Health response does not match the expected contract.");
  }

  return payload;
}

function isHealthResponse(value: unknown): value is HealthResponse {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return candidate.status === "ok" && typeof candidate.service === "string";
}
