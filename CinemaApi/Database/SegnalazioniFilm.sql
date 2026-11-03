-- Gli utenti possono segnalare un problema su una scheda film (dati sbagliati, doppione da
-- eliminare…): solo l'admin modifica ed elimina i film approvati, le richieste passano da qui.
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TipoContenuto')
    ALTER TABLE dbo.Segnalazioni DROP CONSTRAINT CK_TipoContenuto;
GO
ALTER TABLE dbo.Segnalazioni ADD CONSTRAINT CK_TipoContenuto
    CHECK (TipoContenuto IN ('Recensione', 'Commento', 'Discussione', 'Film'));
GO
