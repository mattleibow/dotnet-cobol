import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import * as path from "node:path";
import test from "node:test";

const extensionRoot = path.resolve(__dirname, "../../..");

async function readJson(relativePath: string): Promise<unknown> {
  const content = await readFile(path.join(extensionRoot, relativePath), "utf8");
  return JSON.parse(content) as unknown;
}

test("manifest contributes a packageable COBOL language surface", async () => {
  const manifest = (await readJson("package.json")) as {
    main: string;
    engines: { vscode: string };
    files: string[];
    activationEvents: string[];
    contributes: {
      commands: Array<{ command: string }>;
      languages: Array<{ id: string; configuration: string; extensions: string[] }>;
      grammars: Array<{ language: string; scopeName: string; path: string }>;
      snippets: Array<{ language: string; path: string }>;
      configuration: { properties: Record<string, unknown> };
    };
  };

  assert.equal(manifest.main, "./out/src/extension.js");
  assert.match(manifest.engines.vscode, /^\^1\./);
  assert.ok(manifest.activationEvents.includes("onLanguage:cobol"));
  assert.ok(manifest.activationEvents.includes("onCommand:cobol.validate"));
  assert.equal(manifest.contributes.commands[0].command, "cobol.validate");
  assert.deepEqual(manifest.contributes.languages[0], {
    id: "cobol",
    aliases: ["COBOL", "cobol"],
    extensions: [".cbl", ".cob", ".cobol", ".cpy"],
    filenames: ["COPYLIB"],
    configuration: "./language-configuration.json"
  });
  assert.deepEqual(manifest.contributes.grammars, [{
    language: "cobol",
    scopeName: "source.cobol",
    path: "./syntaxes/cobol.tmLanguage.json"
  }]);
  assert.deepEqual(manifest.contributes.snippets, [{
    language: "cobol",
    path: "./snippets/cobol.code-snippets"
  }]);
  assert.ok("cobol.compiler.path" in manifest.contributes.configuration.properties);
  assert.ok("cobol.compiler.arguments" in manifest.contributes.configuration.properties);
  assert.ok("cobol.diagnostics.enabled" in manifest.contributes.configuration.properties);
  assert.ok(manifest.files.includes("out/src/**"));
  assert.ok(!manifest.files.some((entry) => entry.startsWith("test")));
});

test("grammar recognizes COBOL divisions, comments, strings, and keywords", async () => {
  const grammar = (await readJson("syntaxes/cobol.tmLanguage.json")) as {
    scopeName: string;
    repository: Record<string, { patterns: Array<{ match?: string; begin?: string; end?: string }> }>;
  };

  assert.equal(grammar.scopeName, "source.cobol");
  const comment = grammar.repository.comments.patterns[0].match;
  const division = grammar.repository.divisions.patterns[0].match;
  const keyword = grammar.repository.keywords.patterns[0].match;
  assert.ok(comment && new RegExp(comment).test("      * fixed-format comment"));
  assert.ok(comment && new RegExp(comment).test("*> free-format comment"));
  assert.ok(division && new RegExp(division).test("PROCEDURE DIVISION"));
  assert.ok(keyword && new RegExp(keyword).test("DISPLAY"));
  assert.equal(grammar.repository.strings.patterns[0].begin, "\"");
  assert.equal(grammar.repository.strings.patterns[1].begin, "'");
});

test("language configuration and snippets support expected COBOL editing", async () => {
  const languageConfiguration = (await readJson("language-configuration.json")) as {
    comments: { lineComment: string };
    brackets: string[][];
    wordPattern: string;
  };
  const snippets = (await readJson("snippets/cobol.code-snippets")) as Record<
    string,
    { prefix: string; body: string[] }
  >;

  assert.equal(languageConfiguration.comments.lineComment, "*>");
  assert.deepEqual(languageConfiguration.brackets, [["(", ")"]]);
  assert.ok(new RegExp(languageConfiguration.wordPattern).test("WORKING-STORAGE"));
  assert.equal(snippets["COBOL program"].prefix, "program");
  assert.ok(snippets["COBOL program"].body.some((line) => line.includes("PROGRAM-ID")));
  assert.equal(snippets["Working-storage item"].prefix, "ws");
  assert.ok(snippets["Working-storage item"].body[0].includes("PIC"));
});
