import * as vscode from "vscode";
import { CobolCompilerBridge } from "./compilerBridge";
import { CompilerSettings } from "./compilerSettings";

const diagnosticSource = "cobolc";

function isCobolDocument(document: vscode.TextDocument): boolean {
  return document.languageId === "cobol";
}

function readCompilerSettings(resource: vscode.Uri): CompilerSettings {
  const configuration = vscode.workspace.getConfiguration("cobol", resource);
  return {
    enabled: configuration.get<boolean>("diagnostics.enabled", true),
    command: configuration.get<string>("compiler.path", "cobolc").trim(),
    arguments: configuration.get<string[]>("compiler.arguments", []),
    timeoutMs: configuration.get<number>("compiler.timeout", 10_000)
  };
}

export function activate(context: vscode.ExtensionContext): void {
  const diagnostics = vscode.languages.createDiagnosticCollection(diagnosticSource);
  const bridge = new CobolCompilerBridge({
    diagnostics,
    globalStorageUri: context.globalStorageUri,
    getSettings: readCompilerSettings,
    showError: (message) => void vscode.window.showErrorMessage(message)
  });

  context.subscriptions.push(
    diagnostics,
    vscode.commands.registerTextEditorCommand("cobol.validate", (editor) => {
      void bridge.validate(editor.document);
    }),
    vscode.workspace.onDidSaveTextDocument((document) => {
      const configuration = vscode.workspace.getConfiguration("cobol", document.uri);
      if (isCobolDocument(document) && configuration.get<boolean>("diagnostics.onSave", true)) {
        void bridge.validate(document);
      }
    }),
    vscode.workspace.onDidCloseTextDocument((document) => diagnostics.delete(document.uri)),
    vscode.workspace.onDidChangeConfiguration((event) => {
      if (event.affectsConfiguration("cobol")) {
        for (const document of vscode.workspace.textDocuments) {
          if (isCobolDocument(document)) {
            const configuration = vscode.workspace.getConfiguration("cobol", document.uri);
            if (!configuration.get<boolean>("diagnostics.enabled", true)) {
              diagnostics.delete(document.uri);
            } else {
              void bridge.validate(document);
            }
          }
        }
      }
    }),
    { dispose: () => bridge.dispose() }
  );
}

export function deactivate(): void {
  // All resources are registered in the extension context.
}
