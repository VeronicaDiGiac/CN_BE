-- 1. "Sto guardando" (StoGuardando): una riga per utente, finché non scade (Fine).
-- 2. Avvisi per una persona seguita (Follow.Avvisi): chi la segue con la campanella accesa riceve
--    una notifica quando guarda, vede, vota o recensisce qualcosa.
-- 3. Notifiche: colonna Voto (stelle nella notifica "ha dato 4 stelle a") e quattro tipi nuovi.
-- Le righe di StoGuardando si cancellano dal codice insieme a utente e film (UtentiService, FilmService).
-- Va lanciato PRIMA di pubblicare il BE. Si può lanciare più volte.
-- Pannello Run T-SQL di MonsterASP: usare Database/StoGuardandoEAvvisi_pannello.sql (senza IF e GO).
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.StoGuardando', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoGuardando (
        IdUtente INT       NOT NULL CONSTRAINT PK_StoGuardando PRIMARY KEY
                           CONSTRAINT FK_StoGuardando_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdFilm   INT       NOT NULL CONSTRAINT FK_StoGuardando_Film REFERENCES dbo.Film(IdFilm),
        Inizio   DATETIME2 NOT NULL,
        Fine     DATETIME2 NOT NULL
    );
    CREATE INDEX IX_StoGuardando_Film ON dbo.StoGuardando(IdFilm);
END
GO

IF COL_LENGTH('dbo.Follow', 'Avvisi') IS NULL
    ALTER TABLE dbo.Follow ADD Avvisi BIT NOT NULL CONSTRAINT DF_Follow_Avvisi DEFAULT 0;
GO

IF COL_LENGTH('dbo.Notifiche', 'Voto') IS NULL
    ALTER TABLE dbo.Notifiche ADD Voto INT NULL;
GO

IF OBJECT_ID('dbo.CK_Notifiche_Tipo', 'C') IS NOT NULL
    ALTER TABLE dbo.Notifiche DROP CONSTRAINT CK_Notifiche_Tipo;
GO
ALTER TABLE dbo.Notifiche ADD CONSTRAINT CK_Notifiche_Tipo
    CHECK (Tipo IN ('NuovoFollower', 'MiPiaceRecensione', 'CommentoRecensione', 'RispostaCommento', 'NuovoEpisodio',
                    'MiPiaceCommento', 'MiPiaceDiscussione', 'CommentoDiscussione',
                    'SeguitoGuarda', 'SeguitoVisto', 'SeguitoVoto', 'SeguitoRecensione'));
GO
