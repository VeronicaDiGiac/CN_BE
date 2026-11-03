-- Serie TV, fase B: episodi visti. Gli episodi non sono nel DB (si leggono da TMDB):
-- qui c'è solo chi ha visto quale episodio (stagione + numero) di quale serie, e quando.
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.EpisodiVisti', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EpisodiVisti (
        IdEpisodioVisto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EpisodiVisti PRIMARY KEY,
        IdUtente        INT       NOT NULL CONSTRAINT FK_EpisodiVisti_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdFilm          INT       NOT NULL CONSTRAINT FK_EpisodiVisti_Film REFERENCES dbo.Film(IdFilm),
        Stagione        INT       NOT NULL CONSTRAINT CK_EpisodiVisti_Stagione CHECK (Stagione >= 0),
        Episodio        INT       NOT NULL CONSTRAINT CK_EpisodiVisti_Episodio CHECK (Episodio >= 1),
        DataVisione     DATE      NOT NULL,
        -- Durata dell'episodio al momento della visione (per le statistiche); NULL se TMDB non la conosce
        DurataMinuti    INT       NULL,
        DataCreazione   DATETIME2 NOT NULL CONSTRAINT DF_EpisodiVisti_DataCreazione DEFAULT SYSDATETIME(),
        -- Un episodio si segna una volta sola per utente
        CONSTRAINT UX_EpisodiVisti_Utente_Episodio UNIQUE (IdUtente, IdFilm, Stagione, Episodio)
    );
    CREATE INDEX IX_EpisodiVisti_Film ON dbo.EpisodiVisti(IdFilm);
    CREATE INDEX IX_EpisodiVisti_Utente_Data ON dbo.EpisodiVisti(IdUtente, DataVisione DESC);
END
GO
