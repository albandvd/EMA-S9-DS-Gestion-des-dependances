# ADR 0004 — Keyed services pour la sélection du fournisseur de musique

## Statut
Accepté.

## Contexte
`Music:Providers` dans la configuration donne un ordre de fournisseurs par **nom** (`["itunes", "musicbrainz"]`). `FallbackChainTrackResolver` doit résoudre l'implémentation `ITrackProvider` correspondant à chaque nom, dans l'ordre donné, sans `if/else` sur des chaînes codées en dur et sans `IServiceProvider` brut (anti-pattern service locator).

## Décision
Chaque fournisseur HTTP est enregistré comme **keyed service** .NET 8+ (`services.AddKeyedSingleton<ITrackProvider>("itunes", ...)`, `"musicbrainz"`). `FallbackChainTrackResolver` reçoit les deux services par clé directement en paramètres de constructeur (`[FromKeyedServices("itunes")] ITrackProvider itunesProvider`, idem MusicBrainz), les range dans un petit dictionnaire interne, puis parcourt `Music:Providers` pour choisir l'ordre d'essai.

Une validation dédiée (`MusicOptionsValidation : IValidateOptions<MusicOptions>`) refuse le démarrage si `Music:Providers` contient une clé qui n'est pas `"itunes"` ou `"musicbrainz"` — le fournisseur local n'est pas listable, il est toujours ajouté implicitement en bout de chaîne.

## Justification
- Pas de service locator : les dépendances du resolver restent explicites et testables (les tests construisent `FallbackChainTrackResolver` directement avec des doublons, sans conteneur DI).
- L'échec sur une clé inconnue est détecté **au démarrage**, pas à la première requête (cohérent avec `ValidateOnStart()` utilisé partout ailleurs dans le projet).

## Conséquences
- Ajouter un troisième fournisseur HTTP nécessite d'étendre `MusicOptionsValidation.KnownProviderKeys` et le constructeur de `FallbackChainTrackResolver` (deux points de changement, contrairement à `INotificationChannel` qui se contente d'une résolution `IEnumerable<T>` sans clé — voir `NotificationDispatcher`). C'est un choix délibéré : les fournisseurs de musique ont un ordre explicite piloté par configuration, alors que les canaux de notification n'ont besoin que d'être présents dans la collection, leur ordre étant géré séparément par `Notifications:FallbackOrder`.
