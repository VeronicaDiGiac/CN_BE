-- 1. Voto al film separato dalla recensione (VotiFilm): si può dare stelle senza scrivere.
--    I voti delle recensioni esistenti vengono copiati qui; la colonna Recensioni.Voto resta
--    ma il codice non la usa più (il voto mostrato accanto a una recensione è quello in VotiFilm).
-- 2. Stelle date alle recensioni degli altri (VotiRecensioni).
-- 3. Token di rinnovo della sessione (TokenRinnovo): si resta collegati per 30 giorni dall'ultimo uso.
-- Le righe si cancellano dal codice insieme a utente, film e recensione (UtentiService, FilmService,
-- EliminazioneHelper). Va lanciato PRIMA di pubblicare il BE. Si può lanciare più volte: crea solo
-- ciò che manca e copia solo i voti che non ci sono ancora.
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.VotiFilm', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VotiFilm (
        IdUtente  INT       NOT NULL CONSTRAINT FK_VotiFilm_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdFilm    INT       NOT NULL CONSTRAINT FK_VotiFilm_Film REFERENCES dbo.Film(IdFilm),
        Voto      INT       NOT NULL CONSTRAINT CK_VotiFilm_Voto CHECK (Voto BETWEEN 1 AND 5),
        DataVoto  DATETIME2 NOT NULL CONSTRAINT DF_VotiFilm_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_VotiFilm PRIMARY KEY (IdUtente, IdFilm)
    );
    -- Voto medio di un film e ordinamento del catalogo per voto
    CREATE INDEX IX_VotiFilm_Film ON dbo.VotiFilm(IdFilm) INCLUDE (Voto);
END
GO

INSERT INTO dbo.VotiFilm (IdUtente, IdFilm, Voto, DataVoto)
SELECT r.IdUtente, r.IdFilm, r.Voto, ISNULL(r.DataRecensione, SYSDATETIME())
FROM dbo.Recensioni r
WHERE r.Voto BETWEEN 1 AND 5
  AND NOT EXISTS (SELECT 1 FROM dbo.VotiFilm v WHERE v.IdUtente = r.IdUtente AND v.IdFilm = r.IdFilm);
GO

IF OBJECT_ID('dbo.VotiRecensioni', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VotiRecensioni (
        IdUtente      INT       NOT NULL CONSTRAINT FK_VotiRecensioni_Utenti REFERENCES dbo.Utenti(IdUtente),
        IdRecensione  INT       NOT NULL CONSTRAINT FK_VotiRecensioni_Recensioni REFERENCES dbo.Recensioni(IdRecensione),
        Voto          INT       NOT NULL CONSTRAINT CK_VotiRecensioni_Voto CHECK (Voto BETWEEN 1 AND 5),
        DataVoto      DATETIME2 NOT NULL CONSTRAINT DF_VotiRecensioni_Data DEFAULT SYSDATETIME(),
        CONSTRAINT PK_VotiRecensioni PRIMARY KEY (IdUtente, IdRecensione)
    );
    CREATE INDEX IX_VotiRecensioni_Recensione ON dbo.VotiRecensioni(IdRecensione) INCLUDE (Voto);
END
GO

IF OBJECT_ID('dbo.TokenRinnovo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TokenRinnovo (
        IdTokenRinnovo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TokenRinnovo PRIMARY KEY,
        IdUtente       INT          NOT NULL CONSTRAINT FK_TokenRinnovo_Utenti REFERENCES dbo.Utenti(IdUtente),
        TokenHash      NVARCHAR(64) NOT NULL CONSTRAINT UQ_TokenRinnovo_Hash UNIQUE,
        DataCreazione  DATETIME2    NOT NULL,
        Scadenza       DATETIME2    NOT NULL
    );
    CREATE INDEX IX_TokenRinnovo_Utente ON dbo.TokenRinnovo(IdUtente);
END
GO
