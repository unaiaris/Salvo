#!/usr/bin/env node
// Fails when `src/lib/api/schema.d.ts` is not what `openapi-typescript` produces from the captured
// document under `openapi/`. It regenerates into a temporary file and compares; nothing here opens a
// socket, so it runs inside the gate with no API process anywhere.
//
// This catches the drift that happens in practice: someone edits the generated file by hand, or
// recaptures the document and forgets to regenerate. It does not catch a captured document that has
// fallen behind a changed API — that one is caught by `tsc`, because the guards project every
// required key by name and a contract change breaks compilation, and by recapturing before a release.

import { execFileSync } from "node:child_process";
import { mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const frontendRoot = fileURLToPath(new URL("..", import.meta.url));
const documentPath = join(frontendRoot, "openapi", "salvo-openapi.json");
const committedPath = join(frontendRoot, "src", "lib", "api", "schema.d.ts");
const scratchPath = join(mkdtempSync(join(tmpdir(), "salvo-openapi-")), "schema.d.ts");

execFileSync(
  process.execPath,
  [join(frontendRoot, "node_modules", "openapi-typescript", "bin", "cli.js"), documentPath, "-o", scratchPath],
  { cwd: frontendRoot, stdio: ["ignore", "ignore", "inherit"] },
);

const regenerated = readFileSync(scratchPath, "utf8");
const committed = readFileSync(committedPath, "utf8");

if (regenerated === committed) {
  console.log("OpenAPI types are up to date with openapi/salvo-openapi.json.");
  process.exit(0);
}

writeFileSync(scratchPath, regenerated, "utf8");
console.error(
  [
    "src/lib/api/schema.d.ts does not match the captured OpenAPI document.",
    "",
    "Run `npm run api:types` to regenerate it, and commit the result.",
    `Regenerated file kept for inspection at: ${scratchPath}`,
  ].join("\n"),
);
process.exit(1);
