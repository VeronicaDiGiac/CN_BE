-- La data di una visione diventa facoltativa: l'utente può segnare un film come visto senza dire quando.
-- L'indice IX_Visioni_Utente_Data usa la colonna, quindi si toglie e si ricrea.
-- Si può lanciare più volte.
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Visioni') AND name = 'DataVisione' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Visioni_Utente_Data' AND object_id = OBJECT_ID('dbo.Visioni'))
        DROP INDEX IX_Visioni_Utente_Data ON dbo.Visioni;
    ALTER TABLE dbo.Visioni ALTER COLUMN DataVisione DATE NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Visioni_Utente_Data' AND object_id = OBJECT_ID('dbo.Visioni'))
    CREATE INDEX IX_Visioni_Utente_Data ON dbo.Visioni(IdUtente, DataVisione DESC);
GO
