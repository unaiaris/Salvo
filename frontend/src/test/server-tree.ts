import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { cloneElement, isValidElement, type ReactElement, type ReactNode } from "react";

/**
 * Walks a server component tree the way the framework does, and stops where the browser begins.
 *
 * Under Vitest there is no RSC renderer, so this does the part that matters for the boundary: it
 * invokes server components, follows what they return, and records the props of every client
 * component it reaches. Those props are exactly what React would serialise into the RSC payload
 * inside the HTML, which is what makes them worth asserting on.
 */

export interface BoundaryCrossing {
  readonly componentName: string;
  readonly props: Readonly<Record<string, unknown>>;
}

/** Vitest runs from the frontend root, so `src/` is resolved from the working directory. */
const SOURCE_ROOT = join(process.cwd(), "src");

/** Every file under `src/` whose module is marked `"use client"`. */
export function clientComponentFiles(): readonly string[] {
  const found: string[] = [];

  const visit = (directory: string): void => {
    for (const entry of readdirSync(directory, { withFileTypes: true })) {
      const path = join(directory, entry.name);

      if (entry.isDirectory()) {
        visit(path);
        continue;
      }

      if (!/\.tsx?$/.test(entry.name) || /\.test\.tsx?$/.test(entry.name)) {
        continue;
      }

      if (/^\s*(?:\/\/[^\n]*\n|\/\*[\s\S]*?\*\/\s*)*["']use client["']/.test(readFileSync(path, "utf8"))) {
        found.push(path);
      }
    }
  };

  visit(SOURCE_ROOT);

  return found.sort();
}

/**
 * The functions exported by every `"use client"` module.
 *
 * Discovered rather than listed: a registry maintained by hand would silently stop covering the next
 * client component somebody adds, which is the case this check exists for.
 */
export async function clientComponents(): Promise<ReadonlySet<unknown>> {
  const components = new Set<unknown>();

  for (const file of clientComponentFiles()) {
    const imported: Record<string, unknown> = await import(
      /* @vite-ignore */ pathToFileURL(file).href
    );

    for (const exported of Object.values(imported)) {
      if (typeof exported === "function") {
        components.add(exported);
      }
    }
  }

  return components;
}

function componentName(type: unknown): string {
  return (typeof type === "function" && type.name !== "" ? type.name : null) ?? "anónimo";
}

export async function collectBoundaryCrossings(
  node: ReactNode | Promise<ReactNode>,
  isClient: ReadonlySet<unknown>,
): Promise<readonly BoundaryCrossing[]> {
  const resolved = await node;

  if (resolved === null || resolved === undefined || typeof resolved !== "object") {
    return [];
  }

  if (Array.isArray(resolved)) {
    const nested = await Promise.all(
      resolved.map((child) => collectBoundaryCrossings(child as ReactNode, isClient)),
    );

    return nested.flat();
  }

  if (!isValidElement(resolved)) {
    return [];
  }

  const element = resolved as ReactElement<Record<string, unknown>>;
  const props = element.props;

  if (isClient.has(element.type)) {
    const nested = await collectBoundaryCrossings(props.children as ReactNode, isClient);

    return [{ componentName: componentName(element.type), props }, ...nested];
  }

  if (typeof element.type === "function") {
    const render = element.type as (p: Record<string, unknown>) => ReactNode | Promise<ReactNode>;

    return collectBoundaryCrossings(render(props), isClient);
  }

  return collectBoundaryCrossings(props.children as ReactNode, isClient);
}

/**
 * Whether a value is safe to hand to a client component: a primitive, or an array of them.
 *
 * The rule the design fixes is "primitives, never API objects", and this is what enforces it. It is
 * intentionally blind to *which* object is being passed — an object that happens to hold nothing
 * sensitive today is still a shape that ships whole and grows fields later.
 */
export function isPrimitiveProp(value: unknown): boolean {
  if (Array.isArray(value)) {
    return value.every(isPrimitiveProp);
  }

  return value === null || ["string", "number", "boolean", "undefined"].includes(typeof value);
}

/**
 * Resolves a server component tree into something jsdom can render.
 *
 * Async server components are invoked and awaited here, because React on the client cannot render
 * them; client components are left untouched so they mount and behave normally. What comes back is
 * therefore the same tree the framework would produce, which is what makes assertions about the
 * rendered text meaningful rather than a restatement of the fixture.
 */
export async function resolveServerTree(
  node: ReactNode | Promise<ReactNode>,
  isClient: ReadonlySet<unknown>,
): Promise<ReactNode> {
  const resolved = await node;

  if (resolved === null || resolved === undefined || typeof resolved !== "object") {
    return resolved as ReactNode;
  }

  if (Array.isArray(resolved)) {
    return Promise.all(
      resolved.map((child) => resolveServerTree(child as ReactNode, isClient)),
    ) as Promise<ReactNode>;
  }

  if (!isValidElement(resolved)) {
    return resolved as ReactNode;
  }

  const element = resolved as ReactElement<Record<string, unknown>>;

  if (isClient.has(element.type)) {
    return element;
  }

  if (typeof element.type === "function") {
    const render = element.type as (p: Record<string, unknown>) => ReactNode | Promise<ReactNode>;

    return resolveServerTree(render(element.props), isClient);
  }

  if (element.props.children === undefined) {
    return element;
  }

  return cloneElement(
    element,
    undefined,
    await resolveServerTree(element.props.children as ReactNode, isClient),
  );
}

/** Resolves a page and hands back a tree ready for `render`. */
export async function renderableServerTree(
  node: ReactNode | Promise<ReactNode>,
): Promise<ReactNode> {
  return resolveServerTree(node, await clientComponents());
}
