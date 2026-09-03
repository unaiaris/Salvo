import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

// Vitest runs without global test hooks, so Testing Library never registers its own cleanup and each
// render would pile onto the document of the previous test.
afterEach(cleanup);

// `revalidatePath` needs the Next.js request store, which does not exist under Vitest. The server
// action is exercised for what it decides, not for the framework call it makes afterwards.
vi.mock("next/cache", () => ({
  revalidatePath: vi.fn(),
  revalidateTag: vi.fn(),
}));
