# Schichtplaner

ASP.NET Core 8 MVC WebApp mit PostgreSQL, Docker und ASP.NET Core Identity.

## Start mit Docker

```bash
docker compose up --build -d
```

Danach erreichbar unter:

- http://localhost:8080

Standard-Admin:

- E-Mail: `admin@example.local`
- Passwort: `Admin1234`

## Lokal starten

Voraussetzungen:
- .NET 8 SDK
- PostgreSQL

Dann:

```bash
dotnet restore
dotnet run
```

## Hinweise

- Beim ersten Start werden Datenbank und Identity-Tabellen automatisch per `EnsureCreated()` erzeugt.
- Für einen späteren produktiven Ausbau solltest du auf EF-Core-Migrationen umstellen.
- Öffentliche Registrierung ist deaktiviert. Benutzer werden aktuell per Seed-Admin angelegt bzw. später über eine Admin-Funktion.


## Hinweise zum Update

- Nach dieser Änderung bitte Browser-Cookies für die App einmal löschen oder im Inkognito-Fenster testen, damit alte Antiforgery-Cookies nicht weiter stören.
- Falls die Datenbank bereits mit altem Schema existiert, Container und Volume der Datenbank für einen sauberen Neustart entfernen oder eine Migration auf den Typ `date` durchführen.
