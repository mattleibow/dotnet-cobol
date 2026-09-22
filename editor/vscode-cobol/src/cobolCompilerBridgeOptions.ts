import { spawn } from "node:child_process";
import * as vscode from "vscode";
import { CompilerSettings } from "./compilerSettings";

export interface CobolCompilerBridgeOptions {
  diagnostics: vscode.DiagnosticCollection;
  globalStorageUri: vscode.Uri;
  getSettings(resource: vscode.Uri): CompilerSettings;
  showError(message: string): void;
  spawnProcess?: typeof spawn;
}
