import { ChildProcess, spawn } from "node:child_process";
import { createHash } from "node:crypto";
import { mkdir, rm, writeFile } from "node:fs/promises";
import * as path from "node:path";
import * as vscode from "vscode";
import { CobolCompilerBridgeOptions } from "./cobolCompilerBridgeOptions";
import { CompilerOutput } from "./compilerOutput";
import { CompilerSettings } from "./compilerSettings";

const diagnosticPattern =
  /^(?<file>.+?):(?<line>\d+)(?::(?<column>\d+))?:\s*(?:(?<severity>error|warning|info|note)\s*:?\s*)?(?<message>.+)$/i;

export function parseCompilerDiagnostics(
  output: string,
  sourceUri: vscode.Uri,
  stagedPath?: string
): vscode.Diagnostic[] {
  const diagnostics: vscode.Diagnostic[] = [];

  for (const line of output.split(/\r?\n/)) {
    const match = diagnosticPattern.exec(line);
    if (!match?.groups) {
      continue;
    }

    const reportedPath = match.groups.file;
    if (
      reportedPath !== sourceUri.fsPath &&
      reportedPath !== sourceUri.path &&
      (!stagedPath || path.resolve(reportedPath) !== path.resolve(stagedPath))
    ) {
      continue;
    }

    const lineNumber = Math.max(0, Number.parseInt(match.groups.line, 10) - 1);
    const columnNumber = Math.max(0, Number.parseInt(match.groups.column ?? "1", 10) - 1);
    const message = match.groups.message.trim();
    const diagnostic = new vscode.Diagnostic(
      new vscode.Range(lineNumber, columnNumber, lineNumber, columnNumber),
      message,
      toDiagnosticSeverity(match.groups.severity)
    );
    diagnostic.source = "cobolc";
    diagnostics.push(diagnostic);
  }

  return diagnostics;
}

function toDiagnosticSeverity(severity?: string): vscode.DiagnosticSeverity {
  switch (severity?.toLowerCase()) {
    case "warning":
      return vscode.DiagnosticSeverity.Warning;
    case "info":
    case "note":
      return vscode.DiagnosticSeverity.Information;
    default:
      return vscode.DiagnosticSeverity.Error;
  }
}

export class CobolCompilerBridge implements vscode.Disposable {
  private readonly runningProcesses = new Map<string, ChildProcess>();
  private readonly validationVersions = new Map<string, number>();
  private readonly spawnProcess: typeof spawn;

  public constructor(private readonly options: CobolCompilerBridgeOptions) {
    this.spawnProcess = options.spawnProcess ?? spawn;
  }

  public async validate(document: vscode.TextDocument): Promise<void> {
    if (document.languageId !== "cobol") {
      return;
    }

    const key = document.uri.toString();
    const validationVersion = (this.validationVersions.get(key) ?? 0) + 1;
    this.validationVersions.set(key, validationVersion);
    this.runningProcesses.get(key)?.kill();
    const settings = this.options.getSettings(document.uri);
    if (!settings.enabled) {
      this.options.diagnostics.delete(document.uri);
      return;
    }

    if (!settings.command) {
      this.options.diagnostics.set(document.uri, [
        this.createDiagnostic("cobol.compiler.path must name a compiler executable.")
      ]);
      return;
    }

    let stagedPath: string | undefined;
    try {
      const sourcePath = await this.getSourcePath(document, validationVersion);
      stagedPath = sourcePath === document.uri.fsPath ? undefined : sourcePath;
      const result = await this.runCompiler(settings, sourcePath, key);
      if (this.validationVersions.get(key) !== validationVersion) {
        return;
      }

      const diagnostics = parseCompilerDiagnostics(result.output, document.uri, stagedPath);
      if (diagnostics.length > 0) {
        this.options.diagnostics.set(document.uri, diagnostics);
      } else if (result.exitCode === 0) {
        this.options.diagnostics.delete(document.uri);
      } else {
        this.options.diagnostics.set(document.uri, [
          this.createDiagnostic(
            "cobolc failed without a parseable diagnostic. Configure cobol.compiler.arguments if this compiler requires a validation command."
          )
        ]);
      }
    } catch (error) {
      if (this.validationVersions.get(key) !== validationVersion) {
        return;
      }
      const detail = error instanceof Error ? error.message : String(error);
      this.options.diagnostics.set(document.uri, [
        this.createDiagnostic(`Unable to run cobolc: ${detail}`)
      ]);
    } finally {
      if (stagedPath) {
        await rm(stagedPath, { force: true });
      }
    }
  }

  public dispose(): void {
    for (const process of this.runningProcesses.values()) {
      process.kill();
    }
    this.runningProcesses.clear();
    this.validationVersions.clear();
  }

  private async getSourcePath(document: vscode.TextDocument, validationVersion: number): Promise<string> {
    if (document.uri.scheme === "file" && !document.isDirty) {
      return document.uri.fsPath;
    }

    const sourceDirectory = path.join(this.options.globalStorageUri.fsPath, "diagnostics");
    const extension = path.extname(document.fileName) || ".cbl";
    const id = createHash("sha256").update(document.uri.toString()).digest("hex");
    const sourcePath = path.join(sourceDirectory, `${id}-${validationVersion}${extension}`);
    await mkdir(sourceDirectory, { recursive: true });
    await writeFile(sourcePath, document.getText(), "utf8");
    return sourcePath;
  }

  private async runCompiler(
    settings: CompilerSettings,
    sourcePath: string,
    key: string
  ): Promise<CompilerOutput> {
    return new Promise<CompilerOutput>((resolve, reject) => {
      let output = "";
      let settled = false;
      const process = this.spawnProcess(settings.command, [...settings.arguments, sourcePath], {
        shell: false,
        windowsHide: true
      });
      this.runningProcesses.set(key, process);

      const timeout = setTimeout(() => {
        process.kill();
        finish(new Error(`timed out after ${settings.timeoutMs} ms`));
      }, Math.max(1_000, settings.timeoutMs));

      const finish = (result: CompilerOutput | Error): void => {
        if (settled) {
          return;
        }
        settled = true;
        clearTimeout(timeout);
        this.runningProcesses.delete(key);
        if (result instanceof Error) {
          reject(result);
        } else {
          resolve(result);
        }
      };

      process.stdout?.on("data", (chunk: Buffer) => {
        output += chunk.toString();
      });
      process.stderr?.on("data", (chunk: Buffer) => {
        output += chunk.toString();
      });
      process.on("error", finish);
      process.on("close", (exitCode) => finish({ exitCode, output }));
    });
  }

  private createDiagnostic(message: string): vscode.Diagnostic {
    const diagnostic = new vscode.Diagnostic(
      new vscode.Range(0, 0, 0, 0),
      message,
      vscode.DiagnosticSeverity.Error
    );
    diagnostic.source = "cobolc";
    return diagnostic;
  }
}
