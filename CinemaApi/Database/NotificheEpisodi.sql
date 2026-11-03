-- Serie TV, fase C: notifica "nuovo episodio" delle serie che si stanno guardando.
-- È una notifica di sistema: non ha un mittente (IdMittente diventa facoltativo)
-- e ricorda stagione ed episodio.
SET QUOTED_IDENTIFIER ON;
GO

ALTER TABLE dbo.Notifiche ALTER COLUMN IdMittente INT NULL;
GO

IF COL_LENGTH('dbo.Notifiche', 'Stagione') IS NULL
    ALTER TABLE dbo.Notifiche ADD Stagione INT NULL, Episodio INT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Notifiche_Tipo')
    ALTER TABLE dbo.Notifiche DROP CONSTRAINT CK_Notifiche_Tipo;
GO
ALTER TABLE dbo.Notifiche ADD CONSTRAINT CK_Notifiche_Tipo
    CHECK (Tipo IN ('NuovoFollower', 'MiPiaceRecensione', 'CommentoRecensione', 'RispostaCommento', 'NuovoEpisodio'));
GO

-- Lo stesso episodio si notifica una volta sola a ogni utente
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Notifiche_NuovoEpisodio')
    CREATE UNIQUE INDEX UX_Notifiche_NuovoEpisodio ON dbo.Notifiche(IdDestinatario, IdFilm, Stagione, Episodio)
        WHERE Tipo = 'NuovoEpisodio';
GO
