import { cp, mkdir } from "node:fs/promises";

await mkdir(new URL("../out/", import.meta.url), { recursive: true });
await cp(
  new URL("../src/extension.ts", import.meta.url),
  new URL("../out/extension.js", import.meta.url)
);
