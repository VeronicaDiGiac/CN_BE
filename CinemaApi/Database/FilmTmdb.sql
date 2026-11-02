-- Integrazione TMDB: id del film su TMDB e colonne più larghe per i dati importati
-- (titoli lunghi, più registi, trame complete).
-- Gli indici filtrati richiedono QUOTED_IDENTIFIER ON (sqlcmd lo lascia OFF di default)
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Film', 'TmdbId') IS NULL
    ALTER TABLE dbo.Film ADD TmdbId INT NULL;
GO

ALTER TABLE dbo.Film ALTER COLUMN Titolo NVARCHAR(200) NULL;
ALTER TABLE dbo.Film ALTER COLUMN Regista NVARCHAR(200) NULL;
ALTER TABLE dbo.Film ALTER COLUMN Genere NVARCHAR(200) NULL;
ALTER TABLE dbo.Film ALTER COLUMN Descrizione NVARCHAR(4000) NULL;
GO

-- Un film TMDB può stare nel catalogo una sola volta (i film inseriti a mano hanno TmdbId NULL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Film_TmdbId')
    CREATE UNIQUE INDEX UX_Film_TmdbId ON dbo.Film(TmdbId) WHERE TmdbId IS NOT NULL;
GO
