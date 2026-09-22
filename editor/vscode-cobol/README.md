# COBOL for Visual Studio Code

This MIT-licensed extension provides a standalone COBOL editor surface:

- COBOL file association and editing rules;
- TextMate syntax highlighting;
- starter snippets for programs, data items, and control flow; and
- configuration diagnostics for the reserved external language-server command.

## Local validation

This directory intentionally has no npm dependencies. With a current Node.js
runtime, run:

```sh
npm run package
```

`build` copies the JavaScript-compatible TypeScript entry point to `out/`,
`typecheck` performs Node syntax validation, and `validate` checks the
extension manifest and contributed JSON. The `package` script runs all three.

## Language server pathway

`cobol.lsp.command` is intentionally only a reserved configuration value. If
it is set, the extension emits an informational diagnostic making clear that
no language server is bundled or launched. The entry point exports
`readLspConfiguration` as the narrow hand-off point for a future LSP client.
