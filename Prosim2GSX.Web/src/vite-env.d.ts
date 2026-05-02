/// <reference types="vite/client" />

// Ambient module declaration for CSS Modules so `import styles from "./Foo.module.css"`
// type-checks. Vite handles the runtime side of the import; this just tells TypeScript
// the import yields a string-keyed style object. Restricted to *.module.css so plain
// CSS imports (which don't return a value) don't accidentally type-check the same way.
declare module "*.module.css" {
  const classes: Readonly<Record<string, string>>;
  export default classes;
}
