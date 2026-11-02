-- Data dell'ultimo cambio password: i token JWT emessi prima di questa data non valgono più
-- (dopo un cambio o un reset della password si scollegano tutti i dispositivi).
IF COL_LENGTH('dbo.Utenti', 'DataCambioPassword') IS NULL
    ALTER TABLE dbo.Utenti ADD DataCambioPassword DATETIME2 NULL;
