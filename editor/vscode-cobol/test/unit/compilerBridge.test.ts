import assert from "node:assert/strict";
import { EventEmitter } from "node:events";
import test from "node:test";

class Range {
  public readonly start: { line: number; character: number };

  public constructor(
    startLine: number,
    startCharacter: number,
    _endLine: number,
    _endCharacter: number
  ) {
    this.start = { line: startLine, character: startCharacter };
  }
}

class Diagnostic {
  public source: string | undefined;

  public constructor(
    public readonly range: Range,
    public readonly message: string,
    public readonly severity: number
  ) {}
}

const vscodeMock = {
  Range,
  Diagnostic,
  DiagnosticSeverity: {
    Error: 0,
    Warning: 1,
    Information: 2
  }
};

const nodeModule = require("module") as {
  _load: (request: string, parent: unknown, isMain: boolean) => unknown;
};
const originalLoad = nodeModule._load;
nodeModule._load = (request, parent, isMain) =>
  request === "vscode" ? vscodeMock : originalLoad(request, parent, isMain);

const { CobolCompilerBridge, parseCompilerDiagnostics } =
  require("../../src/compilerBridge") as typeof import("../../src/compilerBridge");

test("parses compiler locations and maps staged paths to the editor", () => {
  const sourceUri = { fsPath: "/project/program.cbl", path: "/project/program.cbl" };
  const diagnostics = parseCompilerDiagnostics(
    [
      "/storage/edited.cbl:3:8: error: invalid verb",
      "/project/other.cbl:2:1: warning: ignored"
    ].join("\n"),
    sourceUri as never,
    "/storage/edited.cbl"
  );

  assert.equal(diagnostics.length, 1);
  assert.equal(diagnostics[0].range.start.line, 2);
  assert.equal(diagnostics[0].range.start.character, 7);
  assert.equal(diagnostics[0].severity, vscodeMock.DiagnosticSeverity.Error);
  assert.equal(diagnostics[0].source, "cobolc");
  assert.equal(diagnostics[0].message, "invalid verb");
});

test("runs configured compiler arguments and publishes parsed diagnostics", async () => {
  const published = new Map<string, readonly InstanceType<typeof Diagnostic>[]>();
  const child = new EventEmitter() as EventEmitter & {
    stdout: EventEmitter;
    stderr: EventEmitter;
    kill(): void;
  };
  child.stdout = new EventEmitter();
  child.stderr = new EventEmitter();
  child.kill = () => undefined;
  const spawnCalls: Array<{ command: string; arguments: string[] }> = [];
  const bridge = new CobolCompilerBridge({
    diagnostics: {
      set: (
        uri: { toString(): string },
        diagnostics: readonly InstanceType<typeof Diagnostic>[]
      ) => published.set(uri.toString(), diagnostics),
      delete: (uri: { toString(): string }) => published.delete(uri.toString())
    } as never,
    globalStorageUri: { fsPath: "/storage" } as never,
    getSettings: () => ({
      enabled: true,
      command: "cobolc",
      arguments: ["check"],
      timeoutMs: 1_000
    }),
    showError: () => undefined,
    spawnProcess: ((command: string, compilerArguments: string[]) => {
      spawnCalls.push({ command, arguments: compilerArguments });
      queueMicrotask(() => {
        child.stderr.emit("data", Buffer.from("/project/program.cbl:4:2: warning: legacy syntax\n"));
        child.emit("close", 1);
      });
      return child;
    }) as never
  });
  const uri = {
    scheme: "file",
    fsPath: "/project/program.cbl",
    toString: () => "file:///project/program.cbl"
  };

  await bridge.validate({
    languageId: "cobol",
    uri,
    isDirty: false
  } as never);

  assert.deepEqual(spawnCalls, [{
    command: "cobolc",
    arguments: ["check", "/project/program.cbl"]
  }]);
  const diagnostics = published.get(uri.toString());
  assert.equal(diagnostics?.length, 1);
  assert.equal(diagnostics?.[0].severity, vscodeMock.DiagnosticSeverity.Warning);
  assert.equal(diagnostics?.[0].range.start.line, 3);
  assert.equal(diagnostics?.[0].range.start.character, 1);
});
