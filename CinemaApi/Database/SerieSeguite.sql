-- Avvisi delle nuove puntate: chi vuole (o non vuole più) la notifica "nuovo episodio" di una serie.
-- Senza riga vale la regola di sempre: avvisi a chi ha visto almeno un episodio.
-- Avvisi = 1: li vuole anche senza aver segnato episodi ("Segui la serie").
-- Avvisi = 0: non li vuole anche se ha visto episodi.
-- Le righe si cancellano dal codice insieme all'utente o al film (UtentiService, FilmService).
-- Si può lanciare più volte: crea la tabella solo se non c'è.
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SerieSeguite', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SerieSeguite (
        IdUtente      INT       NOT NULL CONSTRAINT FK_SerieSeguite_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdFilm        INT       NOT NULL CONSTRAINT FK_SerieSeguite_Film REFERENCES dbo.Film(IdFilm),
        Avvisi        BIT       NOT NULL,
        DataModifica  DATETIME2 NOT NULL CONSTRAINT DF_SerieSeguite_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_SerieSeguite PRIMARY KEY (IdUtente, IdFilm)
    );
    -- Per il controllo dei nuovi episodi (per serie) e per l'eliminazione di un film
    CREATE INDEX IX_SerieSeguite_Film ON dbo.SerieSeguite(IdFilm);
END
GO
