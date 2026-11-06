-- Commenti di un singolo episodio: per ogni episodio di una serie c'è un "filo" di commenti senza titolo,
-- creato al primo commento. È una Discussione con FiloEpisodio = 1 (stagione ed episodio valorizzati):
-- così i commenti riusano risposte, "mi piace", notifiche, segnalazioni e moderazione delle discussioni.
-- Non compare nell'elenco delle discussioni. Si può lanciare più volte.
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Discussioni', 'FiloEpisodio') IS NULL
    ALTER TABLE dbo.Discussioni ADD FiloEpisodio BIT NOT NULL CONSTRAINT DF_Discussioni_FiloEpisodio DEFAULT 0;
GO

-- Un solo filo per episodio (anche se due utenti commentano per primi nello stesso momento)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Discussioni_FiloEpisodio' AND object_id = OBJECT_ID('dbo.Discussioni'))
    CREATE UNIQUE INDEX UX_Discussioni_FiloEpisodio ON dbo.Discussioni(IdFilm, Stagione, Episodio) WHERE FiloEpisodio = 1;
GO
