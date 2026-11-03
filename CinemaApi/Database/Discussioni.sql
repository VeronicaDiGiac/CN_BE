-- Discussioni: argomenti su un film o una serie (o su un singolo episodio) che non sono recensioni.
-- I commenti esistenti si riusano: un commento sta sotto una recensione OPPURE sotto una discussione.
-- In più "mi piace" su commenti e discussioni, con le relative notifiche e segnalazioni.
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Discussioni', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Discussioni (
        IdDiscussione      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Discussioni PRIMARY KEY,
        IdFilm             INT            NOT NULL CONSTRAINT FK_Discussioni_Film REFERENCES dbo.Film(IdFilm),
        IdUtente           INT            NOT NULL CONSTRAINT FK_Discussioni_Utenti REFERENCES dbo.Utenti(IdUtente),
        Titolo             NVARCHAR(200)  NOT NULL,
        Contenuto          NVARCHAR(4000) NULL,
        Spoiler            BIT            NOT NULL CONSTRAINT DF_Discussioni_Spoiler DEFAULT 0,
        -- Solo per le serie: discussione su un episodio (entrambi valorizzati o entrambi NULL)
        Stagione           INT            NULL,
        Episodio           INT            NULL,
        DataCreazione      DATETIME2      NOT NULL CONSTRAINT DF_Discussioni_DataCreazione DEFAULT SYSDATETIME(),
        -- Ultimo messaggio: ordina le discussioni "più recenti"
        DataUltimaAttivita DATETIME2      NOT NULL CONSTRAINT DF_Discussioni_DataUltimaAttivita DEFAULT SYSDATETIME(),
        CONSTRAINT CK_Discussioni_Episodio CHECK (
            (Stagione IS NULL AND Episodio IS NULL) OR (Stagione >= 0 AND Episodio >= 1))
    );
    CREATE INDEX IX_Discussioni_Film ON dbo.Discussioni(IdFilm, Stagione, Episodio);
END
GO

-- Commenti: sotto una recensione oppure sotto una discussione (esattamente uno dei due)
ALTER TABLE dbo.Commenti ALTER COLUMN IdRecensione INT NULL;
GO
IF COL_LENGTH('dbo.Commenti', 'IdDiscussione') IS NULL
    ALTER TABLE dbo.Commenti ADD IdDiscussione INT NULL
        CONSTRAINT FK_Commenti_Discussioni REFERENCES dbo.Discussioni(IdDiscussione);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Commenti_Contenitore')
    ALTER TABLE dbo.Commenti ADD CONSTRAINT CK_Commenti_Contenitore CHECK (
        (IdRecensione IS NOT NULL AND IdDiscussione IS NULL) OR (IdRecensione IS NULL AND IdDiscussione IS NOT NULL));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Commenti_Discussione')
    CREATE INDEX IX_Commenti_Discussione ON dbo.Commenti(IdDiscussione) WHERE IdDiscussione IS NOT NULL;
GO

-- "Mi piace": uno per utente su ogni commento e ogni discussione
IF OBJECT_ID('dbo.MiPiaceCommenti', 'U') IS NULL
    CREATE TABLE dbo.MiPiaceCommenti (
        IdUtente    INT       NOT NULL CONSTRAINT FK_MiPiaceCommenti_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdCommento  INT       NOT NULL CONSTRAINT FK_MiPiaceCommenti_Commenti REFERENCES dbo.Commenti(IdCommento),
        DataMiPiace DATETIME2 NOT NULL CONSTRAINT DF_MiPiaceCommenti_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_MiPiaceCommenti PRIMARY KEY (IdUtente, IdCommento)
    );
GO
IF OBJECT_ID('dbo.MiPiaceDiscussioni', 'U') IS NULL
    CREATE TABLE dbo.MiPiaceDiscussioni (
        IdUtente      INT       NOT NULL CONSTRAINT FK_MiPiaceDiscussioni_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdDiscussione INT       NOT NULL CONSTRAINT FK_MiPiaceDiscussioni_Discussioni REFERENCES dbo.Discussioni(IdDiscussione),
        DataMiPiace   DATETIME2 NOT NULL CONSTRAINT DF_MiPiaceDiscussioni_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_MiPiaceDiscussioni PRIMARY KEY (IdUtente, IdDiscussione)
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MiPiaceCommenti_Commento')
    CREATE INDEX IX_MiPiaceCommenti_Commento ON dbo.MiPiaceCommenti(IdCommento);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MiPiaceDiscussioni_Discussione')
    CREATE INDEX IX_MiPiaceDiscussioni_Discussione ON dbo.MiPiaceDiscussioni(IdDiscussione);
GO

-- Notifiche: collegamento alla discussione e nuovi tipi
IF COL_LENGTH('dbo.Notifiche', 'IdDiscussione') IS NULL
    ALTER TABLE dbo.Notifiche ADD IdDiscussione INT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Notifiche_Tipo')
    ALTER TABLE dbo.Notifiche DROP CONSTRAINT CK_Notifiche_Tipo;
GO
ALTER TABLE dbo.Notifiche ADD CONSTRAINT CK_Notifiche_Tipo CHECK (Tipo IN (
    'NuovoFollower', 'MiPiaceRecensione', 'CommentoRecensione', 'RispostaCommento', 'NuovoEpisodio',
    'MiPiaceCommento', 'MiPiaceDiscussione', 'CommentoDiscussione'));
GO

-- Segnalazioni: si possono segnalare anche le discussioni
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TipoContenuto')
    ALTER TABLE dbo.Segnalazioni DROP CONSTRAINT CK_TipoContenuto;
GO
ALTER TABLE dbo.Segnalazioni ADD CONSTRAINT CK_TipoContenuto
    CHECK (TipoContenuto IN ('Recensione', 'Commento', 'Discussione'));
GO
