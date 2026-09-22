# COBOL for Visual Studio Code

This extension provides COBOL file association, TextMate syntax highlighting,
editing rules, and starter snippets for `.cbl`, `.cob`, `.cobol`, and `.cpy`
files. It also publishes compiler diagnostics from the `cobolc` command-line
compiler.

## Compiler diagnostics

Diagnostics are enabled by default and run when a COBOL file is saved. Run
**COBOL: Validate Active File** to validate on demand.

The extension invokes the configured compiler directly, without a shell:

```text
cobolc <cobol.compiler.arguments...> <source-file>
```

Configure `cobol.compiler.path` when `cobolc` is not on `PATH`, and configure
`cobol.compiler.arguments` for compiler modes or options required by your
installation. The bridge recognizes compiler output in the conventional
`file:line:column: severity: message` form. A nonzero exit with no parseable
diagnostic is reported as an extension diagnostic rather than silently ignored.

Saved local files are passed to the compiler by their real path. For an untitled
or unsaved document, the current text is staged under VS Code's extension
storage and the staged-file diagnostics are mapped back to the editor. Relative
`COPY` paths from an unsaved document therefore resolve relative to the staging
directory; save the file before validating projects that rely on them.

### Deliberate limitation

This is a compiler-diagnostics bridge, **not an LSP client or language server**.
It does not start a JSON-RPC server and it has no completion, go-to-definition,
rename, or live-as-you-type semantic analysis. It launches `cobolc` only after
a save or the explicit validation command.

## Development and verification

Install dependencies with Node.js 22 or later:

```sh
npm install
npm run test:unit
npm run test:integration
npm run package
```

`test:unit` validates the manifest, grammar, snippets, language configuration,
and packaged file allow-list. `test:integration` uses `@vscode/test-electron`
to activate the extension in VS Code, configure a compiler bridge, and verify
diagnostics. On macOS, a very long checkout path can exceed the Unix-domain
socket limit used by VS Code; set `VSCODE_COBOL_TEST_USER_DATA_DIR` to a short,
writable path before running the integration test in that case. `package`
produces `dist/vscode-cobol.vsix`.
