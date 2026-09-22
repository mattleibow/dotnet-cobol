import { mkdir, rm, writeFile } from "node:fs/promises";
import * as path from "node:path";
import { runTests } from "@vscode/test-electron";

async function main(): Promise<void> {
  const extensionDevelopmentPath = path.resolve(__dirname, "../..");
  const extensionTestsPath = path.resolve(__dirname, "suite/index");
  const workspacePath = path.resolve(extensionDevelopmentPath, "test/fixtures/workspace");
  const userDataDirectory = process.env.VSCODE_COBOL_TEST_USER_DATA_DIR;
  const settingsDirectory = path.join(workspacePath, ".vscode");
  await mkdir(settingsDirectory, { recursive: true });
  await writeFile(path.join(settingsDirectory, "settings.json"), JSON.stringify({
    "cobol.diagnostics.enabled": true,
    "cobol.compiler.path": "node",
    "cobol.compiler.arguments": [path.resolve(extensionDevelopmentPath, "test/fixtures/fake-compiler.cjs")]
  }), "utf8");
  try {
    await runTests({
      extensionDevelopmentPath,
      extensionTestsPath,
      launchArgs: [
        ...(userDataDirectory ? [`--user-data-dir=${userDataDirectory}`] : []),
        workspacePath
      ]
    });
  } finally {
    await rm(settingsDirectory, { recursive: true, force: true });
  }
}

void main().catch((error: unknown) => {
  console.error(error);
  process.exitCode = 1;
});
