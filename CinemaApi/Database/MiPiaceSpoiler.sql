-- Fase 2a: "mi piace" sulle recensioni e contenuti segnati come spoiler.
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Recensioni', 'Spoiler') IS NULL
    ALTER TABLE dbo.Recensioni ADD Spoiler BIT NOT NULL CONSTRAINT DF_Recensioni_Spoiler DEFAULT 0;
GO
IF COL_LENGTH('dbo.Commenti', 'Spoiler') IS NULL
    ALTER TABLE dbo.Commenti ADD Spoiler BIT NOT NULL CONSTRAINT DF_Commenti_Spoiler DEFAULT 0;
GO

-- Chiave composta: un utente mette al massimo un "mi piace" per recensione
IF OBJECT_ID('dbo.MiPiaceRecensioni', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MiPiaceRecensioni (
        IdUtente     INT       NOT NULL CONSTRAINT FK_MiPiace_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdRecensione INT       NOT NULL CONSTRAINT FK_MiPiace_Recensioni REFERENCES dbo.Recensioni(IdRecensione),
        DataMiPiace  DATETIME2 NOT NULL CONSTRAINT DF_MiPiace_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_MiPiaceRecensioni PRIMARY KEY (IdUtente, IdRecensione)
    );
    CREATE INDEX IX_MiPiace_Recensione ON dbo.MiPiaceRecensioni(IdRecensione);
END
GO
