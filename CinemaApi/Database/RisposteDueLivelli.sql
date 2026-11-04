-- Le conversazioni hanno due livelli (commento e risposte). Le risposte date a una risposta
-- (terzo livello o oltre) non comparivano: si spostano sotto il commento principale del filone.
SET QUOTED_IDENTIFIER ON;
GO

WHILE EXISTS (SELECT 1 FROM dbo.Commenti c JOIN dbo.Commenti p ON p.IdCommento = c.IdCommentoPadre
              WHERE p.IdCommentoPadre IS NOT NULL)
BEGIN
    UPDATE c SET c.IdCommentoPadre = p.IdCommentoPadre
    FROM dbo.Commenti c JOIN dbo.Commenti p ON p.IdCommento = c.IdCommentoPadre
    WHERE p.IdCommentoPadre IS NOT NULL;
END
GO
