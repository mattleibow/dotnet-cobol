# DotNetCobol 0.1 core profile

## Source forms

Free format is selected with `>>SOURCE FORMAT FREE`. A source without that directive is treated as fixed format only when its non-empty lines begin in the traditional Area A/B column region; sequence columns are ignored and indicator-column `*`/`/` lines are skipped. Ambiguous, left-aligned source is treated as free format. This is a pragmatic input rule, not a complete ISO source-format implementation.

## Accepted subset

| Surface | Implemented |
| --- | --- |
| Divisions | `IDENTIFICATION`, `ENVIRONMENT` (parsed/ignored), `DATA`, `PROCEDURE` |
| Program identity | one `PROGRAM-ID` per compilation |
| Storage | elementary `WORKING-STORAGE` declarations with levels, `PIC X(n)`, `PIC 9(n)`, `PIC 9(n)V9(n)`, and `VALUE` |
| Procedure | paragraphs are recognized; `DISPLAY`, `MOVE`, `ACCEPT`, `ADD`, `SUBTRACT`, `MULTIPLY`, `DIVIDE`, simple `IF`, `STOP RUN`, `GOBACK` |
| Output | deterministic managed PE and portable PDB; console application by default or a library with `--library` |

Names are case-insensitive. `PIC X` maps to `string`, integer `PIC 9` to `int`, and implied-decimal pictures to `decimal`. This is a safety-oriented approximation, not COBOL storage semantics.

## Rejected or not yet modeled

Unsupported procedure verbs report `COB2001`; unresolved data items report `COB3001`. The following are intentionally absent: `COPY`, `FILE`/`SELECT`/I-O, `LINKAGE`, group and redefined items, `OCCURS`, `USAGE`, editing pictures, `PERFORM`, `EVALUATE`, `STRING`, national/DBCS data, sort/merge, report writer, intrinsic functions, `EXEC CICS`, embedded SQL, condition names, scope terminators, and dialect extensions.

`ACCEPT` is limited to a console line. `IF` supports a single nested statement and comparison operators. Arithmetic has no COBOL rounding, size-error, or overflow semantics. `ENVIRONMENT DIVISION` is retained to establish syntax shape but has no semantic effect.

## Stable diagnostic IDs

| ID | Meaning |
| --- | --- |
| `COB1001` | malformed expected token |
| `COB2001` | profile-excluded construct |
| `COB2002` | absent `PROGRAM-ID` |
| `COB3001` | undeclared working-storage name |
| `COB9001` | backend emission failure |
