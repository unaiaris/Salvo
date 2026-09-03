#!/usr/bin/env node
// Captures the OpenAPI document the API serves and stores it under version control, so the gate can
// check the generated types for drift without a listening port.
//
//   SALVO_API_BASE_URL=http://127.0.0.1:5100 npm run api:capture
//
// Run the API with DemoData:Enabled=true: the demo seed and the evaluation metrics endpoints are
// only mapped under that flag, and the captured document has to be the superset. The `servers`
// entry is rewritten to a fixed placeholder because it echoes the port the capture happened to use,
// which is not part of the contract and would show up as drift on the next capture.

import { writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";

const baseUrl = process.env.SALVO_API_BASE_URL ?? "http://127.0.0.1:5100";
const target = fileURLToPath(new URL("../openapi/salvo-openapi.json", import.meta.url));

const response = await fetch(new URL("/openapi/v1.json", baseUrl), {
  signal: AbortSignal.timeout(15_000),
});

if (!response.ok) {
  throw new Error(`The API answered ${response.status} for /openapi/v1.json.`);
}

const document = await response.json();
document.servers = [{ url: "/" }];

if (!document.paths?.["/api/evaluation-metrics"]) {
  throw new Error(
    "The captured document has no /api/evaluation-metrics: capture against an API started with " +
      "DemoData__Enabled=true, otherwise the document is not the superset.",
  );
}

await writeFile(target, `${JSON.stringify(document, null, 2)}\n`, "utf8");
console.log(`Captured ${Object.keys(document.paths).length} paths into ${target}`);
