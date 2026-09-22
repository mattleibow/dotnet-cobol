import * as assert from "node:assert/strict";
import * as path from "node:path";
import * as vscode from "vscode";

const timeoutMs = 10_000;

async function waitForDiagnostics(uri: vscode.Uri, expectedCount: number): Promise<readonly vscode.Diagnostic[]> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const diagnostics = vscode.languages
      .getDiagnostics(uri)
      .filter((diagnostic) => diagnostic.source === "cobolc");
    if (diagnostics.length === expectedCount) {
      return diagnostics;
    }
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  return vscode.languages
    .getDiagnostics(uri)
    .filter((diagnostic) => diagnostic.source === "cobolc");
}

export async function run(): Promise<void> {
  const extension = vscode.extensions.getExtension("mattleibow.vscode-cobol");
  if (!extension) {
    throw new Error("The COBOL extension should be available to the extension host.");
  }
  await extension.activate();
  assert.equal(extension.isActive, true);
  assert.ok(await vscode.commands.getCommands(true).then((commands) => commands.includes("cobol.validate")));

  const extensionPath = extension.extensionPath;
  const fixturePath = path.join(extensionPath, "test/fixtures/workspace/diagnostic.cbl");
  const compilerPath = path.join(extensionPath, "test/fixtures/fake-compiler.cjs");
  const document = await vscode.workspace.openTextDocument(vscode.Uri.file(fixturePath));
  await vscode.window.showTextDocument(document);
  assert.equal(document.languageId, "cobol", "The .cbl fixture must activate COBOL language support.");

  const configuration = vscode.workspace.getConfiguration("cobol", document.uri);
  assert.equal(configuration.get<string>("compiler.path"), "node");
  assert.deepEqual(configuration.get<string[]>("compiler.arguments"), [compilerPath]);

  await vscode.commands.executeCommand("cobol.validate");
  const diagnostics = await waitForDiagnostics(document.uri, 1);
  assert.equal(diagnostics.length, 1, "The compiler bridge should publish a parsed compiler error.");
  assert.equal(diagnostics[0].range.start.line, 2);
  assert.equal(diagnostics[0].range.start.character, 7);
  assert.match(diagnostics[0].message, /simulated compiler failure/);

}
