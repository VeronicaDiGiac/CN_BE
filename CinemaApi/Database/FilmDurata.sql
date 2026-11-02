-- Fase 5: durata dei film (minuti), per le statistiche "ore di cinema".
-- Si riempie dall'importazione/collegamento TMDB; i film manuali possono restare senza.
IF COL_LENGTH('dbo.Film', 'DurataMinuti') IS NULL
    ALTER TABLE dbo.Film ADD DurataMinuti INT NULL;
GO
