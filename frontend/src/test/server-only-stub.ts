// Vitest resolves `server-only` to this empty module.
//
// The real package throws on import outside a server environment, which is the point of it, but that
// makes every module under test unimportable. The stub restores importability and loses the
// guarantee, so `boundary.test.ts` asserts by reading the sources that the modules that must carry
// `import "server-only"` still carry it.
export {};
