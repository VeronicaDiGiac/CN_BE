-- Fase 2c: notifiche (nuovo follower, "mi piace", commenti e risposte).
-- IdFilm/IdRecensione/IdCommento non hanno FK (come Segnalazioni.IdContenuto):
-- le notifiche collegate si cancellano dal codice insieme al contenuto (EliminazioneHelper).
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Notifiche', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifiche (
        IdNotifica     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifiche PRIMARY KEY,
        IdDestinatario INT          NOT NULL CONSTRAINT FK_Notifiche_Destinatario REFERENCES dbo.Utenti(IdUtente),
        IdMittente     INT          NOT NULL CONSTRAINT FK_Notifiche_Mittente REFERENCES dbo.Utenti(IdUtente),
        Tipo           NVARCHAR(30) NOT NULL CONSTRAINT CK_Notifiche_Tipo
                       CHECK (Tipo IN ('NuovoFollower', 'MiPiaceRecensione', 'CommentoRecensione', 'RispostaCommento')),
        IdFilm         INT          NULL,
        IdRecensione   INT          NULL,
        IdCommento     INT          NULL,
        Letta          BIT          NOT NULL CONSTRAINT DF_Notifiche_Letta DEFAULT 0,
        DataCreazione  DATETIME2    NOT NULL CONSTRAINT DF_Notifiche_Data DEFAULT SYSDATETIME()
    );
    -- Lista e conteggio delle non lette per destinatario
    CREATE INDEX IX_Notifiche_Destinatario ON dbo.Notifiche(IdDestinatario, Letta, DataCreazione DESC);
END
GO
