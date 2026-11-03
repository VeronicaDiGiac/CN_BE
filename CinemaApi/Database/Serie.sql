-- Serie TV: stanno nella tabella Film come gli altri titoli, distinte dalla colonna Tipo.
-- Così recensioni, liste, preferiti, "da vedere", notifiche… funzionano anche per le serie.
-- Stagioni ed episodi non si salvano: si leggono da TMDB quando servono.
-- Gli indici filtrati richiedono QUOTED_IDENTIFIER ON (sqlcmd lo lascia OFF di default)
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Film', 'Tipo') IS NULL
    ALTER TABLE dbo.Film ADD Tipo NVARCHAR(10) NOT NULL
        CONSTRAINT DF_Film_Tipo DEFAULT 'Film'
        CONSTRAINT CK_Film_Tipo CHECK (Tipo IN ('Film', 'Serie'));
GO

-- Solo per le serie: quante stagioni ha (da TMDB, si aggiorna a ogni importazione)
IF COL_LENGTH('dbo.Film', 'NumeroStagioni') IS NULL
    ALTER TABLE dbo.Film ADD NumeroStagioni INT NULL;
GO

-- Su TMDB film e serie hanno numerazioni separate (il film 238 e la serie 238 sono diversi):
-- l'unicità vale per la coppia (Tipo, TmdbId)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Film_TmdbId')
    DROP INDEX UX_Film_TmdbId ON dbo.Film;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Film_Tipo_TmdbId')
    CREATE UNIQUE INDEX UX_Film_Tipo_TmdbId ON dbo.Film(Tipo, TmdbId) WHERE TmdbId IS NOT NULL;
GO
