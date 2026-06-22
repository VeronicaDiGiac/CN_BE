# CinemaAPI

## Descrizione
Web API REST sviluppata in ASP.NET Core (.NET 8) con Entity Framework Core.
Espone endpoint per gestire film, recensioni, commenti e preferiti del database CinemaDB.
Il frontend è separato (HTML + JavaScript) e comunica con il backend tramite HTTP.

## Requisiti
- .NET 8 SDK
- SQL Server Express
- Database CinemaDB (script SQL incluso nel progetto)

## Come avviarlo
1. Configura la connection string in `appsettings.json`
2. Esegui lo script SQL per creare e popolare il database
3. Avvia il progetto con Visual Studio o `dotnet run`
4. Swagger disponibile su `https://localhost:{porta}/swagger`

---

## Contratto JSON

> Il contratto definisce la forma dei dati scambiati tra backend e frontend.
> Se un campo cambia nome nel backend, il frontend si rompe.

---

### Film

#### GET /api/film
Parametri opzionali: `titolo`, `regista`, `anno`, `genere`

Risposta `200 OK`:
```json
[
  {
    "idFilm": 1,
    "titolo": "Oppenheimer",
    "regista": "Christopher Nolan",
    "annoUscita": 2023,
    "genere": "Biografico",
    "attori": "Cillian Murphy",
    "immagineUrl": "https://esempio.com/oppenheimer.jpg"
  }
]
```

#### GET /api/film/{id}
Risposta `200 OK` — singolo film (stessa forma sopra)
Risposta `404 Not Found` — se il film non esiste

#### POST /api/film
Body richiesta:
```json
{
  "titolo": "Inception",
  "regista": "Christopher Nolan",
  "annoUscita": 2010,
  "genere": "Fantascienza",
  "attori": "Leonardo DiCaprio",
  "immagineUrl": "https://esempio.com/inception.jpg"
}
```
Risposta `201 Created`

#### PUT /api/film/{id}
Body richiesta: stessa forma del POST
Risposta `204 No Content`
Risposta `404 Not Found` — se il film non esiste

#### DELETE /api/film/{id}
Risposta `204 No Content`
Risposta `404 Not Found` — se il film non esiste

---

### Recensioni

#### GET /api/recensioni?idFilm={n}
Risposta `200 OK`:
```json
[
  {
    "idRecensione": 1,
    "userName": "ElenaNeri",
    "titoloFilm": "Oppenheimer",
    "voto": 5,
    "contenuto": "Un capolavoro assoluto.",
    "dataRecensione": "2024-03-10T00:00:00"
  }
]
```

#### POST /api/recensioni
Body richiesta:
```json
{
  "idUtente": 1,
  "idFilm": 1,
  "voto": 5,
  "contenuto": "Film straordinario."
}
```
Risposta `201 Created`

#### PUT /api/recensioni/{id}
Body richiesta: stessa forma del POST
Risposta `204 No Content`

#### DELETE /api/recensioni/{id}
Risposta `204 No Content`

---

### Commenti

#### GET /api/commenti?idRecensione={n}
Risposta `200 OK` — commenti radice con risposte annidate:
```json
[
  {
    "idCommento": 1,
    "userName": "MarcoBianchi",
    "contenuto": "Concordo su tutto.",
    "dataCommento": "2024-03-11T00:00:00",
    "idCommentoPadre": null,
    "risposte": [
      {
        "idCommento": 5,
        "userName": "SofiaRossi",
        "contenuto": "Anche io!",
        "dataCommento": "2024-03-12T00:00:00"
      }
    ]
  }
]
```

#### POST /api/commenti
Body richiesta:
```json
{
  "idUtente": 1,
  "idRecensione": 1,
  "contenuto": "Bellissima recensione.",
  "idCommentoPadre": null
}
```
Per rispondere a un commento, `idCommentoPadre` deve contenere l'id del commento padre.
Risposta `201 Created`

#### PUT /api/commenti/{id}
Body richiesta: solo `contenuto` viene aggiornato
Risposta `204 No Content`

#### DELETE /api/commenti/{id}
Risposta `204 No Content`

---

### Preferiti

#### GET /api/preferiti?idUtente={n}
Risposta `200 OK`:
```json
[
  {
    "idPreferito": 1,
    "idUtente": 1,
    "titoloFilm": "Oppenheimer",
    "regista": "Christopher Nolan",
    "dataPreferito": "2024-03-20T00:00:00"
  }
]
```

#### POST /api/preferiti
Body richiesta:
```json
{
  "idUtente": 1,
  "idFilm": 1
}
```
Risposta `201 Created`

#### DELETE /api/preferiti/{id}
Risposta `204 No Content`

---

## Errori
Tutti gli endpoint restituiscono `500 Internal Server Error` in caso di errore lato server (es. DB non raggiungibile).
