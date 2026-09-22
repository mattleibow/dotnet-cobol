namespace Cobol.Tests;

internal static class TestProgram
{
    public const string Source =
        """
        >>SOURCE FORMAT FREE
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ACCOUNT.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 BALANCE PIC 9(5)V99 VALUE 125.50.
        01 OWNER PIC X(20) VALUE 'Ada'.
        PROCEDURE DIVISION.
        MAIN.
            ADD 10 TO BALANCE.
            DISPLAY OWNER BALANCE.
            IF BALANCE > 0 DISPLAY 'ACCOUNT ACTIVE'.
            STOP RUN.
        """;

    public const string NormalizedSource =
        """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ACCOUNT.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 BALANCE PIC 9(5)V99 VALUE 125.50.
        01 OWNER PIC X(20) VALUE 'Ada'.
        PROCEDURE DIVISION.
        MAIN.
            ADD 10 TO BALANCE.
            DISPLAY OWNER BALANCE.
            IF BALANCE > 0 DISPLAY 'ACCOUNT ACTIVE'.
            STOP RUN.
        """;
}
