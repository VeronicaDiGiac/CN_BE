CREATE TABLE dbo.StoGuardando (
    IdUtente INT       NOT NULL CONSTRAINT PK_StoGuardando PRIMARY KEY
                       CONSTRAINT FK_StoGuardando_Utenti REFERENCES dbo.Utenti(IdUtente),
    IdFilm   INT       NOT NULL CONSTRAINT FK_StoGuardando_Film REFERENCES dbo.Film(IdFilm),
    Inizio   DATETIME2 NOT NULL,
    Fine     DATETIME2 NOT NULL
);
CREATE INDEX IX_StoGuardando_Film ON dbo.StoGuardando(IdFilm);
ALTER TABLE dbo.Follow ADD Avvisi BIT NOT NULL CONSTRAINT DF_Follow_Avvisi DEFAULT 0;
ALTER TABLE dbo.Notifiche ADD Voto INT NULL;
ALTER TABLE dbo.Notifiche DROP CONSTRAINT CK_Notifiche_Tipo;
ALTER TABLE dbo.Notifiche ADD CONSTRAINT CK_Notifiche_Tipo
    CHECK (Tipo IN ('NuovoFollower', 'MiPiaceRecensione', 'CommentoRecensione', 'RispostaCommento', 'NuovoEpisodio',
                    'MiPiaceCommento', 'MiPiaceDiscussione', 'CommentoDiscussione',
                    'SeguitoGuarda', 'SeguitoVisto', 'SeguitoVoto', 'SeguitoRecensione'));
