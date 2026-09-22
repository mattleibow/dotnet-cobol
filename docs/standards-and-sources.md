# Standards and dialect sources

This bibliography records design inputs, not copied specification text. ISO standards and vendor documentation retain their respective copyright; this repository contains only original summaries and links.

| Source | Role | Access and licensing constraint |
| --- | --- | --- |
| [ISO/IEC 1989:2023 metadata](https://www.iso.org/standard/82273.html) | Current published COBOL standard baseline | Metadata is public; the standard text is ISO copyrighted and licensed/purchased separately. Do not copy it into this repository. |
| [ISO/IEC JTC 1/SC 22/WG 4](https://www.open-std.org/jtc1/sc22/wg4/) | COBOL working-group materials and standards process | Public items vary; working drafts and published standards may have separate rights. |
| [IBM Enterprise COBOL for z/OS 6.4 documentation](https://www.ibm.com/docs/en/cobol-zos/6.4) | Mainframe and banking dialect expectations | IBM documentation is accessible online but remains IBM copyrighted. |
| [Micro Focus Visual COBOL documentation](https://www.microfocus.com/documentation/visual-cobol/) | Enterprise managed-COBOL compatibility expectations | Vendor documentation remains Micro Focus copyrighted. |
| [GnuCOBOL project](https://gnucobol.sourceforge.io/) and [manual index](https://gnucobol.sourceforge.io/HTML/gnucobpg.html) | Open implementation behavior and portability expectations | GnuCOBOL code is GPL; its runtime is LGPL; documentation has its own stated licensing. No GPL source or GFDL text is incorporated here. |
| [NIST COBOL test suite archive](https://www.itl.nist.gov/div897/ctg/cobol_form.htm) | Historical, publicly described validation direction | Assess individual test-package notices before importing tests; none are vendored yet. |

## Dialect choice

The current target is **DotNetCobol 0.1 core profile**, informed by ISO/IEC 1989:2023 metadata and common Enterprise COBOL, Visual COBOL, and GnuCOBOL vocabulary. It is intentionally its own profile, not an IBM, Micro Focus, GnuCOBOL, or ISO conformance mode.

Banking/mainframe priorities for the next profiles are fixed source format, copybooks, packed-decimal and edited numeric pictures, records, indexed/sequential file I-O, report-style formatting, `PERFORM`, condition names, and controlled interoperation with CICS/SQL. They are roadmap items, not implied capabilities.
