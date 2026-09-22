"use strict";

let configurationDiagnostics;
let configurationChangeSubscription;

function isCobolDocument(document) {
  return document.languageId === "cobol";
}

function refreshConfigurationDiagnostic(vscode, document) {
  if (!isCobolDocument(document)) {
    return;
  }

  const configuration = vscode.workspace.getConfiguration("cobol", document.uri);
  if (!configuration.get("diagnostics.configuration", true)) {
    configurationDiagnostics.delete(document.uri);
    return;
  }

  const command = configuration.get("lsp.command", "").trim();
  if (!command) {
    configurationDiagnostics.delete(document.uri);
    return;
  }

  const message =
    "cobol.lsp.command is configured, but this extension does not include or launch a language server.";
  const range = new vscode.Range(0, 0, 0, 0);
  const diagnostic = new vscode.Diagnostic(
    range,
    message,
    vscode.DiagnosticSeverity.Information
  );
  diagnostic.source = "COBOL";
  configurationDiagnostics.set(document.uri, [diagnostic]);
}

function refreshOpenCobolDocuments(vscode) {
  for (const document of vscode.workspace.textDocuments) {
    refreshConfigurationDiagnostic(vscode, document);
  }
}

function readLspConfiguration(vscode, resource) {
  const configuration = vscode.workspace.getConfiguration("cobol", resource);
  return {
    command: configuration.get("lsp.command", "").trim()
  };
}

function activate(context) {
  const vscode = require("vscode");
  configurationDiagnostics = vscode.languages.createDiagnosticCollection("cobol-configuration");
  context.subscriptions.push(configurationDiagnostics);

  refreshOpenCobolDocuments(vscode);
  context.subscriptions.push(
    vscode.workspace.onDidOpenTextDocument((document) =>
      refreshConfigurationDiagnostic(vscode, document)
    )
  );

  configurationChangeSubscription = vscode.workspace.onDidChangeConfiguration((event) => {
    if (event.affectsConfiguration("cobol")) {
      refreshOpenCobolDocuments(vscode);
    }
  });
  context.subscriptions.push(configurationChangeSubscription);
}

function deactivate() {
  if (configurationChangeSubscription) {
    configurationChangeSubscription.dispose();
  }
  if (configurationDiagnostics) {
    configurationDiagnostics.dispose();
  }
}

module.exports = {
  activate,
  deactivate,
  readLspConfiguration
};
