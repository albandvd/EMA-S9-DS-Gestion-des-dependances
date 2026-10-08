# ADR 0002 — Architecture hexagonale (ports & adaptateurs)

## Statut
Accepté.

## Contexte
L'exigence métier centrale est de pouvoir changer de fournisseur de musique ou de canal de notification sans recompiler, et sans qu'une panne d'un composant externe ne rende le système inutilisable. Il faut aussi que le Domain/Application restent testables sans aucune dépendance technique (pas de `HttpClient`, pas de nom de fournisseur).

## Décision
Cinq projets `src/`, dépendances pointant uniquement vers l'intérieur :

- `ReveilMusical.Domain` — 0 dépendance.
- `ReveilMusical.Application` — → Domain uniquement (+ abstractions de logging/options). Définit les ports (`ITrackProvider`, `ITrackResolver`, `INotificationChannel`, `INotificationDispatcher`, `IUserPreferencesProvider`, `IOutbox`).
- `ReveilMusical.Infrastructure.Music` / `.Notifications` / `.Users` — → Application. Implémentent les ports, exposent chacun **une seule** méthode d'extension publique d'enregistrement (`AddMusicProviders`, `AddNotificationChannels`, `AddUserPreferences`).
- `ReveilMusical.Api` — composition root, → tout le reste.

Toute classe d'implémentation et tout DTO d'infrastructure est `internal sealed` (`InternalsVisibleTo` vers les projets de test correspondants).

## Justification
C'est la traduction directe de l'exigence « aucune classe métier ne connaît un détail technique d'un fournisseur ou d'un canal ». Vérifié automatiquement par `ReveilMusical.Architecture.Tests` (ADR 0006) plutôt que laissé à la discipline des développeurs.

## Conséquences
- Chaque nouveau fournisseur/canal est un nouvel adaptateur + une ligne d'enregistrement, jamais une modification du Domain/Application (démontré par la branche `feature/whatsapp-channel`).
- Le coût : des DTO de mapping dans chaque adaptateur (`ItunesTrackDto`, `SmsPayload`, etc.) plutôt qu'un modèle unique partagé — c'est le prix du découplage, assumé.
