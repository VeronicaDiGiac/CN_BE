-- Fase 3: liste personali di film (es. "I migliori horror anni '80").
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Liste', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Liste (
        IdLista       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Liste PRIMARY KEY,
        IdUtente      INT            NOT NULL CONSTRAINT FK_Liste_Utenti REFERENCES dbo.Utenti(IdUtente),
        Titolo        NVARCHAR(100)  NOT NULL,
        Descrizione   NVARCHAR(1000) NULL,
        -- Le liste private le vede solo chi le ha create
        Pubblica      BIT            NOT NULL CONSTRAINT DF_Liste_Pubblica DEFAULT 1,
        DataCreazione DATETIME2      NOT NULL CONSTRAINT DF_Liste_DataCreazione DEFAULT SYSDATETIME(),
        DataModifica  DATETIME2      NOT NULL CONSTRAINT DF_Liste_DataModifica DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_Liste_Utente ON dbo.Liste(IdUtente);
END
GO

-- Un film compare una sola volta per lista; Posizione dà l'ordine scelto dall'utente
IF OBJECT_ID('dbo.ListeFilm', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListeFilm (
        IdLista      INT           NOT NULL CONSTRAINT FK_ListeFilm_Liste REFERENCES dbo.Liste(IdLista),
        IdFilm       INT           NOT NULL CONSTRAINT FK_ListeFilm_Film REFERENCES dbo.Film(IdFilm),
        Posizione    INT           NOT NULL,
        Nota         NVARCHAR(300) NULL,
        DataAggiunta DATETIME2     NOT NULL CONSTRAINT DF_ListeFilm_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_ListeFilm PRIMARY KEY (IdLista, IdFilm)
    );
    CREATE INDEX IX_ListeFilm_Film ON dbo.ListeFilm(IdFilm);
END
GO
