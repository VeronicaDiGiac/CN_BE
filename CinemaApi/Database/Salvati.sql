-- Recensioni e discussioni salvate da un utente ("Salvati", segnalibro).
-- IdContenuto non ha FK (punta a Recensioni o a Discussioni secondo il tipo, come Segnalazioni.IdContenuto):
-- le righe si cancellano dal codice insieme al contenuto o all'utente (EliminazioneHelper, UtentiService).
-- Si può lanciare più volte: crea la tabella solo se non c'è.
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Salvati', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Salvati (
        IdUtente         INT          NOT NULL CONSTRAINT FK_Salvati_Utenti REFERENCES dbo.Utenti(IdUtente),
        TipoContenuto    NVARCHAR(20) NOT NULL CONSTRAINT CK_Salvati_Tipo CHECK (TipoContenuto IN ('Recensione', 'Discussione')),
        IdContenuto      INT          NOT NULL,
        DataSalvataggio  DATETIME2    NOT NULL CONSTRAINT DF_Salvati_Data DEFAULT SYSDATETIME(),
        -- Lo stesso contenuto si salva una volta sola
        CONSTRAINT PK_Salvati PRIMARY KEY (IdUtente, TipoContenuto, IdContenuto)
    );
    -- Per togliere le righe quando si elimina una recensione o una discussione
    CREATE INDEX IX_Salvati_Contenuto ON dbo.Salvati(TipoContenuto, IdContenuto);
END
GO
