-- Tabella per i link "Password dimenticata?".
-- Nel DB si salva solo l'hash SHA-256 del token: il token in chiaro esiste solo nella mail.
IF OBJECT_ID('dbo.PasswordReset', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordReset (
        IdPasswordReset INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PasswordReset PRIMARY KEY,
        IdUtente        INT           NOT NULL,
        TokenHash       CHAR(64)      NOT NULL,
        DataCreazione   DATETIME2     NOT NULL CONSTRAINT DF_PasswordReset_DataCreazione DEFAULT SYSUTCDATETIME(),
        Scadenza        DATETIME2     NOT NULL,
        Usato           BIT           NOT NULL CONSTRAINT DF_PasswordReset_Usato DEFAULT 0,
        CONSTRAINT FK_PasswordReset_Utenti FOREIGN KEY (IdUtente) REFERENCES dbo.Utenti(IdUtente)
    );

    CREATE UNIQUE INDEX UX_PasswordReset_TokenHash ON dbo.PasswordReset(TokenHash);
    CREATE INDEX IX_PasswordReset_IdUtente ON dbo.PasswordReset(IdUtente);
END
