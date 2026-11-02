-- Fase "stile TV Time": diario delle visioni e lista "Da vedere".
SET QUOTED_IDENTIFIER ON;
GO

-- Ogni riga è una visione: lo stesso film si può rivedere più volte (diario)
IF OBJECT_ID('dbo.Visioni', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Visioni (
        IdVisione     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Visioni PRIMARY KEY,
        IdUtente      INT       NOT NULL CONSTRAINT FK_Visioni_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdFilm        INT       NOT NULL CONSTRAINT FK_Visioni_Film REFERENCES dbo.Film(IdFilm),
        DataVisione   DATE      NOT NULL,
        DataCreazione DATETIME2 NOT NULL CONSTRAINT DF_Visioni_DataCreazione DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Visioni_Utente_Data ON dbo.Visioni(IdUtente, DataVisione DESC);
    CREATE INDEX IX_Visioni_Film ON dbo.Visioni(IdFilm);
END
GO

-- Un film compare al massimo una volta nella lista "Da vedere" di un utente
IF OBJECT_ID('dbo.DaVedere', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DaVedere (
        IdDaVedere   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DaVedere PRIMARY KEY,
        IdUtente     INT       NOT NULL CONSTRAINT FK_DaVedere_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdFilm       INT       NOT NULL CONSTRAINT FK_DaVedere_Film REFERENCES dbo.Film(IdFilm),
        DataAggiunta DATETIME2 NOT NULL CONSTRAINT DF_DaVedere_DataAggiunta DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_DaVedere_Utente_Film UNIQUE (IdUtente, IdFilm)
    );
    CREATE INDEX IX_DaVedere_Film ON dbo.DaVedere(IdFilm);
END
GO
