-- Titolo normalizzato (solo lettere e cifre, minuscolo, senza accenti: "Dune: Parte Due" -> "dunepartedue")
-- per riconoscere i doppioni con una ricerca indicizzata.
-- Lo calcola l'applicazione (CinemaContext.SaveChanges) e, per i film già presenti,
-- il riempimento all'avvio in Program.cs: in SQL togliere gli accenti non è affidabile.
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Film', 'TitoloChiave') IS NULL
    ALTER TABLE dbo.Film ADD TitoloChiave NVARCHAR(200) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Film_TitoloChiave')
    CREATE INDEX IX_Film_TitoloChiave ON dbo.Film(TitoloChiave);
GO
