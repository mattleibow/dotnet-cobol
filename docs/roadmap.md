# Compliance and delivery roadmap

## Feature/compliance matrix

| Area | 0.1 status | Next verification milestone |
| --- | --- | --- |
| ISO/IEC 1989:2023 | Metadata-informed subset only | Clause-to-test matrix prepared from licensed access; no compliance claim before independent conformance evidence |
| Syntax APIs | Immutable red tree with parent/spans/full spans/trivia/missing-token recovery, factory, visitor, and base rewriter | Green-node cache, incremental parse reuse, typed visitor and replacement rewriter APIs |
| Source formats | Basic directive/free and heuristic fixed form | Full fixed columns, continuation, compiler directives, and source-map tests |
| Data division | Elementary working storage only | Groups, `OCCURS`, `REDEFINES`, `USAGE`, edited/packed/binary/national data, linkage |
| Procedures | Straight-line verbs and simple IF | Scope terminators, `PERFORM`, `EVALUATE`, strings, tables, intrinsics, exception/size-error handling |
| I-O and records | Not implemented | `SELECT`, FD/SD, sequential/indexed/relative organization and record-layout compatibility |
| Enterprise integrations | Not implemented | Vendor-gated CICS, IMS, Db2/embedded SQL, copybook and precompiler strategies |
| Code generation | Roslyn-backed bootstrap PE/PDB backend; deterministic bytes, managed entry point/MVID, PDB documents/sequence point blobs tested | Direct IL/metadata writer, COBOL source debug sequence points, runtime ABI, optimizations |
| Tooling | CLI, initial MSBuild task, syntax extension | LSP, incremental workspace, diagnostics/code actions, debugger support |
| Roslyn/upstream readiness | Architectural inspiration only | API review, performance/cancellation model, incremental green trees, IDE test matrix, contributor governance |

## Planned phases

1. **Language core:** complete syntax preservation and robust source formats; add copybook-aware preprocessing and comprehensive parser tests.
2. **Data and control flow:** model COBOL data representation faithfully and implement structured control flow with bound-tree lowering.
3. **Runtime and I-O:** create a tested runtime ABI plus portable file/record support; design opt-in mainframe interop adapters.
4. **Direct backend:** replace the bootstrap C# emitter with `System.Reflection.Metadata`/IL generation and portable-PDB sequence points.
5. **Developer experience:** ship LSP and MSBuild SDK packaging, then assess a Roslyn workspace bridge.
6. **Conformance:** use properly licensed standards access and permitted test suites to create reproducible, versioned evidence. Publish only feature results, never restricted standard text.

## Non-goals for 0.1

No claim of full COBOL 2023 compliance, mainframe binary compatibility, production-bank suitability, vendor endorsement, Roslyn upstream eligibility, or source-level compatibility with any existing compiler is made at this stage.
