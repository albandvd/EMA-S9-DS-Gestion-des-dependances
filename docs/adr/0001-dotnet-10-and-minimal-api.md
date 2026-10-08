# ADR 0001 — .NET 10 (LTS) et ASP.NET Core Minimal API

## Statut
Accepté.

## Contexte
Le TP est orienté `Microsoft.Extensions.DependencyInjection` et `dotnet list package`, donc l'écosystème .NET s'impose. Il reste à choisir la version du runtime et le style d'API HTTP pour le point d'entrée (`POST /api/wake-ups`).

## Décision
- Runtime : **.NET 10**, la version LTS la plus récente disponible (`global.json` épingle le SDK `10.0.401`, `rollForward: latestFeature`), pour maximiser le support et la fraîcheur.
- Point d'entrée : **ASP.NET Core Minimal API**, pas de contrôleurs MVC.

## Justification
- LTS = fraîcheur maximale sans sacrifier la stabilité (exigence légale du TP : « aucune version en retard non justifiée »).
- Minimal API est testable de bout en bout avec `WebApplicationFactory<Program>` exactement comme une API à contrôleurs, sans la cérémonie des attributs `[ApiController]`/`[Route]` pour un seul endpoint.
- Le contrat HTTP (`POST /api/wake-ups`, JSON) reste clair pour un futur ordonnanceur, sans lier ce TP à un framework de planification de tâches.

## Conséquences
- `Program.cs` utilise des top-level statements ; `InternalsVisibleTo("ReveilMusical.Api.Tests")` est nécessaire car la classe `Program` générée est `internal`.
- Microsoft.AspNetCore.OpenApi (natif depuis .NET 9) fournit la génération OpenAPI sans dépendance tierce (Swashbuckle, etc.).
