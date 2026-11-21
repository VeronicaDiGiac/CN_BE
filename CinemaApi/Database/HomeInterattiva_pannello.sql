-- Home interattiva: "Sondaggio della settimana" e "Film club della settimana".
-- Versione per il pannello "Run T-SQL" di MonsterASP (senza IF/GO): da lanciare UNA volta sola.
-- Il sondaggio in corso è l'ultimo con DataInizio già passata: i quattro qui sotto partono
-- uno a settimana da soli; altri si aggiungono dal pannello admin.

CREATE TABLE dbo.Sondaggi (
    IdSondaggio   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sondaggi PRIMARY KEY,
    Domanda       NVARCHAR(200)     NOT NULL,
    DataInizio    DATETIME2         NOT NULL,
    DataCreazione DATETIME2         NOT NULL CONSTRAINT DF_Sondaggi_DataCreazione DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Sondaggi_DataInizio ON dbo.Sondaggi(DataInizio);

CREATE TABLE dbo.OpzioniSondaggio (
    IdOpzione   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OpzioniSondaggio PRIMARY KEY,
    IdSondaggio INT               NOT NULL CONSTRAINT FK_OpzioniSondaggio_Sondaggi REFERENCES dbo.Sondaggi(IdSondaggio) ON DELETE CASCADE,
    Testo       NVARCHAR(100)     NOT NULL,
    IdFilm      INT               NULL CONSTRAINT FK_OpzioniSondaggio_Film REFERENCES dbo.Film(IdFilm),
    Ordine      INT               NOT NULL
);
CREATE INDEX IX_OpzioniSondaggio_Sondaggio ON dbo.OpzioniSondaggio(IdSondaggio);

CREATE TABLE dbo.VotiSondaggio (
    IdUtente    INT       NOT NULL CONSTRAINT FK_VotiSondaggio_Utenti REFERENCES dbo.Utenti(IdUtente),
    IdSondaggio INT       NOT NULL,
    IdOpzione   INT       NOT NULL CONSTRAINT FK_VotiSondaggio_Opzioni REFERENCES dbo.OpzioniSondaggio(IdOpzione) ON DELETE CASCADE,
    DataVoto    DATETIME2 NOT NULL,
    CONSTRAINT PK_VotiSondaggio PRIMARY KEY (IdUtente, IdSondaggio)
);
CREATE INDEX IX_VotiSondaggio_Opzione ON dbo.VotiSondaggio(IdOpzione);

CREATE TABLE dbo.FilmClub (
    IdFilmClub    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FilmClub PRIMARY KEY,
    IdFilm        INT               NOT NULL CONSTRAINT FK_FilmClub_Film REFERENCES dbo.Film(IdFilm),
    DataInizio    DATE              NOT NULL CONSTRAINT UQ_FilmClub_DataInizio UNIQUE,
    IdDiscussione INT               NULL,
    SceltoDaAdmin BIT               NOT NULL CONSTRAINT DF_FilmClub_SceltoDaAdmin DEFAULT 0,
    DataCreazione DATETIME2         NOT NULL CONSTRAINT DF_FilmClub_DataCreazione DEFAULT SYSDATETIME()
);

-- Scorta iniziale: un sondaggio a settimana, dal lunedì
INSERT INTO dbo.Sondaggi (Domanda, DataInizio) VALUES
    (N'Qual è il miglior film di Christopher Nolan?', '2026-11-16'),
    (N'Al cinema: popcorn dolci o salati?', '2026-11-23'),
    (N'Il film di Natale perfetto?', '2026-11-30'),
    (N'Serie TV: tutta in un weekend o un episodio alla volta?', '2026-12-07');

INSERT INTO dbo.OpzioniSondaggio (IdSondaggio, Testo, Ordine)
SELECT s.IdSondaggio, o.Testo, o.Ordine
FROM dbo.Sondaggi s
JOIN (VALUES
    (N'Qual è il miglior film di Christopher Nolan?', N'Inception', 1),
    (N'Qual è il miglior film di Christopher Nolan?', N'Interstellar', 2),
    (N'Qual è il miglior film di Christopher Nolan?', N'Il cavaliere oscuro', 3),
    (N'Qual è il miglior film di Christopher Nolan?', N'Oppenheimer', 4),
    (N'Al cinema: popcorn dolci o salati?', N'Dolci', 1),
    (N'Al cinema: popcorn dolci o salati?', N'Salati', 2),
    (N'Al cinema: popcorn dolci o salati?', N'Misti, metà e metà', 3),
    (N'Al cinema: popcorn dolci o salati?', N'Niente popcorn, grazie', 4),
    (N'Il film di Natale perfetto?', N'Mamma ho perso l''aereo', 1),
    (N'Il film di Natale perfetto?', N'Love Actually', 2),
    (N'Il film di Natale perfetto?', N'Una poltrona per due', 3),
    (N'Il film di Natale perfetto?', N'Nightmare Before Christmas', 4),
    (N'Serie TV: tutta in un weekend o un episodio alla volta?', N'Tutta in un weekend', 1),
    (N'Serie TV: tutta in un weekend o un episodio alla volta?', N'Un episodio alla volta', 2),
    (N'Serie TV: tutta in un weekend o un episodio alla volta?', N'Dipende dalla serie', 3)
) AS o(Domanda, Testo, Ordine) ON o.Domanda = s.Domanda;

-- Le risposte con lo stesso titolo di un film del catalogo mostrano la sua locandina
UPDATE o SET IdFilm = f.IdFilm
FROM dbo.OpzioniSondaggio o
JOIN dbo.Film f ON f.Titolo = o.Testo AND f.Stato = 'Approvato';
