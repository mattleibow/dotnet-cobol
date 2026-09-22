import { access, readFile } from "node:fs/promises";

const root = new URL("../", import.meta.url);
const requiredFiles = [
  "package.json",
  "language-configuration.json",
  "syntaxes/cobol.tmLanguage.json",
  "snippets/cobol.code-snippets",
  "out/extension.js"
];

for (const path of requiredFiles) {
  await access(new URL(path, root));
}

const readJson = async (path) => JSON.parse(await readFile(new URL(path, root), "utf8"));
const manifest = await readJson("package.json");
const grammar = await readJson("syntaxes/cobol.tmLanguage.json");
const snippets = await readJson("snippets/cobol.code-snippets");

if (manifest.main !== "./out/extension.js") {
  throw new Error("The extension entry point must be ./out/extension.js.");
}
if (!manifest.contributes?.languages?.some((language) => language.id === "cobol")) {
  throw new Error("The manifest must contribute the cobol language.");
}
if (grammar.scopeName !== "source.cobol") {
  throw new Error("The COBOL grammar must use the source.cobol scope.");
}
if (Object.keys(snippets).length === 0) {
  throw new Error("At least one COBOL snippet is required.");
}

console.log("COBOL extension manifest, grammar, snippets, and entry point are valid.");
