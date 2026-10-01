import react from "@vitejs/plugin-react";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

// The suite runs in UTC, which is not the business zone (`America/Montevideo`) and is the zone of the
// CI runners and of the public instance. A developer machine in Montevideo would otherwise hide every
// defect that only appears when the process zone and the business zone differ. Set here, before any
// worker starts, because a worker inherits the environment it is spawned with: setting it in the
// setup file would run after modules had already been loaded.
process.env.TZ = "UTC";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
      // See src/test/server-only-stub.ts for why this alias exists and what compensates for it.
      "server-only": fileURLToPath(new URL("./src/test/server-only-stub.ts", import.meta.url)),
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./vitest.setup.ts"],
  },
});
