# Prompt — Projet « Réveil musical » (.NET)

---

## 1. Ton rôle

Tu es un développeur senior .NET et architecte logiciel. Tu réalises un TP évalué sur la **gestion des dépendances, le découplage, l'IoC/DI, la résilience et l'audit des licences**. Le correcteur lira le code, l'historique Git et le README. Ton objectif n'est pas seulement que « ça marche » : chaque choix doit traduire un besoin métier et être justifiable.

Travaille par étapes courtes, **une branche Git par fonctionnalité**. À chaque étape : tu codes, tu écris les tests, tu lances `dotnet build` et `dotnet test`, et **tu commites seulement quand tout est vert**. Une fois la fonctionnalité terminée, tu merges sa branche dans `main` (voir §12). Une CI GitHub Actions vérifie le build, les tests et les dépendances (voir §13). Ne fais jamais un commit géant à la fin.

---

## 2. Le produit et les exigences non négociables

**Réveil musical** réveille chaque utilisateur avec un morceau choisi selon le **jour de la semaine** et la **météo du jour**, puis le prévient sur **son canal préféré**.

Le TP porte uniquement sur **l'appel qui déclenche l'envoi** (l'ordonnancement n'est pas à coder). Cet appel reçoit :
- l'ID utilisateur ;
- le jour de la semaine ;
- le type de météo : `SOLEIL`, `PLUIE`, `NEIGE`, `NUAGEUX` (fourni en entrée, aucun appel météo externe).

Quatre exigences métier, toutes non négociables :

| Besoin métier | Traduction technique attendue |
|---|---|
| **Musique** : pouvoir tester plusieurs sources et en changer vite (prix, limites, panne) | Port `ITrackProvider` + un adaptateur par fournisseur (iTunes, MusicBrainz, local). Choix et ordre des fournisseurs **par configuration, sans recompiler**. |
| **Notification** : email / SMS / push aujourd'hui, WhatsApp / appel vocal demain | Port `INotificationChannel` + un adaptateur par canal. Ajouter un canal = 1 adaptateur + 1 enregistrement DI, **zéro modification du métier**. |
| **Légal** : aucun composant externe sans vérification de licence et de fraîcheur | README avec tableau licence / version installée / dernière version stable / justification, outil d'audit automatisé, SBOM. |
| **Fiabilité** : une panne ne doit jamais empêcher l'envoi (mode dégradé OK, silence KO) | Chaîne de repli musique (→ fallback local codé en dur), chaîne de repli canaux, timeouts courts, circuit breaker, cache. |

Contraintes d'architecture explicites :
1. **Isolation / faible couplage** : aucune classe métier ne connaît un détail technique d'un fournisseur ou d'un canal (pas de `HttpClient`, pas d'URL, pas de DTO, pas de nom « Itunes » / « Sms » dans le Domain ou l'Application).
2. **IoC / DI** : aucune implémentation concrète de service n'est instanciée avec `new` dans le code de production. Tout passe par le conteneur `Microsoft.Extensions.DependencyInjection`.
   - Autorisé : `new` de value objects, records, DTO, exceptions, collections.
   - Interdit : `new HttpClient()`, `new ItunesTrackProvider(...)`, `new EmailAdapter(...)`, singletons statiques, `DateTime.Now` (utiliser `TimeProvider` injecté).
   - Dans les tests, `new` est autorisé (c'est tout l'intérêt du seam).

---

## 3. Choix techniques (et pourquoi)

Le cours est orienté .NET (`Microsoft.Extensions.DependencyInjection`, `dotnet list package`) : on reste dans cet écosystème.

| Sujet | Choix | Justification |
|---|---|---|
| Runtime | **.NET 10 (LTS)**, SDK épinglé par `global.json` | Version LTS la plus récente : support long, fraîcheur maximale. Vérifie avec `dotnet --list-sdks` ; si .NET 10 n'est pas installé, utilise .NET 8 LTS et documente-le. |
| Point d'entrée | **ASP.NET Core Minimal API** : `POST /api/wake-ups` | Continuité avec les TP précédents, testable de bout en bout avec `WebApplicationFactory`, contrat HTTP clair pour l'ordonnanceur futur. |
| DI | `Microsoft.Extensions.DependencyInjection` + **keyed services** (.NET 8+) | Conteneur standard, MIT. Les keyed services permettent de choisir un fournisseur/canal par clé lue en configuration. |
| HTTP | `IHttpClientFactory` + **typed clients** | Pas de `new HttpClient`, gestion du pool de sockets, User-Agent et BaseAddress configurés à l'enregistrement. |
| Résilience | `Microsoft.Extensions.Http.Resilience` (Polly v8 dessous) | Timeout, retry limité, circuit breaker standardisés. Licences MIT / BSD-3. |
| Rate limiting | `System.Threading.RateLimiting` (inclus dans .NET) | iTunes ≈ 20 req/min, MusicBrainz ≈ 1 req/s. On **échoue vite** quand le quota est atteint (on ne fait pas attendre un réveil). |
| Cache | `Microsoft.Extensions.Caching.Memory` | Économise le quota iTunes, accélère, et sert de dernière valeur connue en cas de panne. |
| Horloge | `TimeProvider` (inclus dans .NET) | Supprime la dépendance cachée à l'horloge système, testable avec `FakeTimeProvider`. |
| Logs | `Microsoft.Extensions.Logging` (abstractions) | Aucune lib de logs tierce : une dépendance de moins. |
| Tests | **xUnit**, **NSubstitute**, **Shouldly** (ou AwesomeAssertions), `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing` | Toutes permissives. Voir les exclusions ci-dessous. |
| Tests d'architecture | **NetArchTest.Rules** (ou ArchUnitNET) | Prouve automatiquement que le Domain/Application ne dépendent pas de l'infrastructure. |
| Couverture | `coverlet.collector` + `ReportGenerator` (outil local) | Rapport HTML de couverture. |
| Versions | **Central Package Management** (`Directory.Packages.props`) | Toutes les versions au même endroit : audit et mise à jour triviaux. |
| Qualité | `Directory.Build.props` : `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, `.editorconfig`, `dotnet format` | Code homogène, warnings traités. |

**Composants volontairement écartés** (à mentionner dans le README, c'est exactement l'esprit de l'exigence légale) :
- **Moq** : épisode SponsorLink (collecte d'emails dans le build, 2023) → NSubstitute.
- **FluentAssertions v8+** : passée sous licence commerciale payante → Shouldly ou le fork AwesomeAssertions (Apache 2.0).
- **MediatR / AutoMapper** : passés sous licence commerciale ; de toute façon inutiles ici (mapping manuel dans les adaptateurs, plus explicite).
- **Tout SDK iTunes/MusicBrainz tiers** : les deux API sont de simples GET JSON ; `System.Net.Http.Json` suffit et évite une dépendance transitive non maîtrisée.

> Avant d'ajouter **chaque** package : vérifie sa licence sur nuget.org, sa dernière version stable, sa date de dernière publication, et ses dépendances transitives (`dotnet list package --include-transitive`). Vérifie les affirmations de licence ci-dessus au moment de l'installation et corrige le README si quelque chose a changé.

---

## 4. Architecture et structure de la solution

Architecture hexagonale (ports & adaptateurs) en couches. Les dépendances pointent **vers le Domain**, jamais l'inverse.

```
ReveilMusical/
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── .gitignore
├── ReveilMusical.sln
├── README.md
├── docs/
│   └── adr/                       # 1 fichier court par décision importante
├── src/
│   ├── ReveilMusical.Domain/                      # 0 dépendance
│   ├── ReveilMusical.Application/                 # → Domain (+ Logging.Abstractions)
│   ├── ReveilMusical.Infrastructure.Music/        # → Application
│   ├── ReveilMusical.Infrastructure.Notifications/# → Application
│   ├── ReveilMusical.Infrastructure.Users/        # → Application (mock du service interne)
│   └── ReveilMusical.Api/                         # composition root, → tout le monde
└── tests/
    ├── ReveilMusical.Domain.Tests/
    ├── ReveilMusical.Application.Tests/
    ├── ReveilMusical.Infrastructure.Tests/        # adaptateurs + tests de contrat
    ├── ReveilMusical.Api.Tests/                   # end-to-end WebApplicationFactory
    └── ReveilMusical.Architecture.Tests/
```

Règles :
- Le code est en **anglais** ; les valeurs du contrat d'entrée (`SOLEIL`, `LUNDI`…) sont en français et **mappées dans la couche Api** vers les types du Domain.
- Chaque projet d'infrastructure expose **une seule** méthode d'extension publique d'enregistrement (`AddMusicProviders(IConfiguration)`, `AddNotificationChannels(IConfiguration)`, `AddUserPreferences(IConfiguration)`). `Program.cs` ne contient que ces appels.
- Les classes d'implémentation et les DTO d'infrastructure sont **`internal sealed`** (`InternalsVisibleTo` pour les tests). Ainsi un DTO iTunes ne peut physiquement pas fuiter.
- Options typées via `IOptions<T>` + `ValidateDataAnnotations()` + `ValidateOnStart()`.

Ajoute dans le README un diagramme Mermaid des couches et un diagramme de séquence du réveil.

---

## 5. Modèle métier (Domain)

Uniquement des types purs, immuables, sans dépendance :

- `UserId` (value object, non vide).
- `WeatherType` : `Sunny`, `Rain`, `Snow`, `Cloudy`.
- `DayOfWeek` : réutiliser `System.DayOfWeek` (type BCL, pas une dépendance externe).
- `TrackQuery` : ce que l'utilisateur a choisi (texte de recherche, ex. « Here Comes the Sun »).
- `Track` : morceau résolu, `Title`, `Artist`. **Aucun champ propre à un fournisseur** (pas de `trackViewUrl`, pas d'ID MusicBrainz).
- `ChannelId` : value object basé sur une chaîne normalisée (`"email"`, `"sms"`, `"push"`…). Choix délibéré plutôt qu'un `enum` : ajouter WhatsApp ne modifie pas le Domain.
- `ContactPoint` : `ChannelId` + adresse (email, numéro, device token) sous forme de chaîne opaque pour le métier.
- `UserPreferences` : tracks par `WeatherType`, `FallbackTrack`, `PreferredChannel`, liste de `ContactPoint`.
- `WakeUpMessage` : destinataire, `Track`, jour, météo, texte personnalisé.
- `WakeUpOutcome` : morceau envoyé, canal effectivement utilisé, et **indicateurs de dégradation** (`MusicDegraded`, `ChannelDegraded`, liste de raisons).

Règle métier de sélection (Strategy `ITrackSelectionPolicy`, implémentation par défaut `WeatherBasedSelectionPolicy`) :
1. Si l'utilisateur a un morceau pour la météo du jour → ce morceau.
2. Sinon → son morceau de secours.

**Hypothèse sur le jour de la semaine** (à écrire dans le README, section « Hypothèses ») : le service de préférences ne fournit des morceaux que par météo ; le jour est donc utilisé (a) dans le message personnalisé (« Bon lundi pluvieux ! ») et (b) passé à la policy de sélection, ce qui permet d'ajouter une règle par jour (ex. playlist du week-end) en écrivant une nouvelle policy, sans toucher au cas d'usage. Couvre ce point par un test qui injecte une policy alternative.

---

## 6. Fonctionnalités détaillées

### F1 — Point d'entrée HTTP
- `POST /api/wake-ups` avec le corps `{ "userId": "u-42", "dayOfWeek": "LUNDI", "weather": "PLUIE" }`.
- Validation : jour ∈ `LUNDI…DIMANCHE` (accepter aussi `MONDAY…SUNDAY`, insensible à la casse), météo ∈ `SOLEIL|PLUIE|NEIGE|NUAGEUX`. Sinon `400` avec `ProblemDetails`.
- Utilisateur inconnu → `404` `ProblemDetails`.
- Succès → `200` avec : morceau (titre, artiste), canal utilisé, `degraded: true/false`, raisons de dégradation.
- Le endpoint ne fait qu'appeler le cas d'usage ; aucune logique dedans.
- Ajoute un fichier `.http` d'exemples et l'OpenAPI natif (`Microsoft.AspNetCore.OpenApi`) si sa licence/fraîcheur est vérifiée.

### F2 — Cas d'usage `SendWakeUpUseCase` (Application)
Dépend uniquement de ports : `IUserPreferencesProvider`, `ITrackSelectionPolicy`, `ITrackResolver`, `INotificationDispatcher`, `IWakeUpMessageComposer`, `ILogger`.
Déroulé :
1. Récupère les préférences de l'utilisateur.
2. Applique la policy → `TrackQuery`.
3. Résout le morceau via `ITrackResolver`. **Filet de sécurité final dans le cas d'usage** : si le resolver lève quand même une exception, utiliser le `FallbackTrack` brut de l'utilisateur, marquer `MusicDegraded`.
4. Compose le message.
5. Délègue l'envoi au dispatcher.
6. Retourne un `WakeUpOutcome`. Toute dégradation est loggée en `Warning` avec la raison.

### F3 — Préférences utilisateur (service interne mocké)
- Port `IUserPreferencesProvider.GetAsync(UserId, CancellationToken)`.
- Implémentation `InMemoryUserPreferencesProvider` alimentée par configuration (`appsettings.json`, section `Users`), avec 3 à 5 utilisateurs couvrant : canal email, SMS, push ; un utilisateur sans morceau pour la neige (→ fallback) ; un canal préféré inexistant (→ repli canal).
- Option de simulation de panne (`Users:SimulateFailure`) pour démontrer le mode dégradé.
- Décorateur `CachingUserPreferencesProvider` : garde la dernière valeur connue ; si le service tombe, on réveille avec les préférences en cache (dégradé) plutôt que pas du tout.

### F4 — Fournisseurs de musique
Port `ITrackProvider` : `Task<Track?> FindAsync(TrackQuery, CancellationToken)` (null = introuvable, exception = panne).

1. **`ItunesTrackProvider`** (typed HttpClient)
   - `GET https://itunes.apple.com/search?term=<query>&media=music&limit=5` (query URL-encodée).
   - DTO `internal` (`ItunesSearchResponse`, `ItunesTrackDto` avec `trackName`, `artistName`, `trackViewUrl`) → mappé vers `Track` **dans l'adaptateur uniquement** ; `trackViewUrl` n'est jamais exposé.
   - Rate limiter token bucket configuré à 20 req/min, mode « échouer vite » quand le quota est vide.
2. **`MusicBrainzTrackProvider`** (typed HttpClient)
   - `GET https://musicbrainz.org/ws/2/recording?query=<query>&fmt=json`.
   - **En-tête `User-Agent` obligatoire**, lu depuis la configuration (`ReveilMusical/1.0 ( contact@exemple.fr )`), validé au démarrage (`ValidateOnStart`) : l'app refuse de démarrer s'il manque.
   - DTO `internal` (`title`, `artist-credit[].name`) → mappé vers `Track`.
   - Rate limiter ≈ 1 req/s.
3. **`LocalFallbackTrackProvider`**
   - Petite liste codée en dur (5 à 8 morceaux, au moins un par météo). Ne fait aucun I/O, **ne peut pas échouer**, retourne toujours un morceau (match par météo/requête, sinon le premier).

Composition :
- Chaque provider HTTP est enregistré en **keyed service** (`"itunes"`, `"musicbrainz"`) et reçoit un pipeline de résilience : timeout par tentative court (~2 s), 1 retry max avec jitter, circuit breaker.
- Décorateur **`CachingTrackProvider`** (clé = requête normalisée, TTL configurable, ex. 24 h) autour des providers HTTP.
- **`FallbackChainTrackResolver`** (implémente `ITrackResolver`, pattern Chain of Responsibility / Composite) :
  - lit l'ordre dans `Music:Providers` (ex. `["itunes", "musicbrainz"]`) et résout les providers par clé ;
  - essaie chacun dans l'ordre, sous un **budget de temps global** (ex. 5 s via `CancellationTokenSource` + `TimeProvider`) ;
  - attrape exceptions/timeouts/introuvables, logge, passe au suivant ;
  - termine **toujours** par le provider local ; signale `Degraded` si le local a servi.
- Démonstration attendue : passer de iTunes à MusicBrainz, ou les inverser, ou n'utiliser que le local = **modifier `appsettings.json` uniquement**. Une clé inconnue en config fait échouer le démarrage avec un message clair.

### F5 — Canaux de notification (mocks + adaptateurs)
Trois faux SDK écrits par toi, **avec des interfaces volontairement différentes** (comme de vrais SDK hétérogènes) :

| Faux SDK | Signature (exemple) | Particularités |
|---|---|---|
| `FakeEmailClient` | `void SendMail(string to, string subject, string htmlBody)` | synchrone, HTML |
| `FakeSmsGateway` | `Task<SmsReceipt> PushSmsAsync(SmsPayload payload)` | async, texte limité à 160 caractères, reçu avec statut |
| `FakePushService` | `bool Notify(string deviceToken, PushNotification notification)` | renvoie `false` en cas d'échec au lieu de lever |

- Chacun écrit le message simulé via un port technique `IOutbox` (implémentations : console via `ILogger` et fichier `outbox/<canal>.log`). Pas de `Console.WriteLine` en dur.
- Chacun a une option `SimulateFailure` (config) pour démontrer les replis.
- Trois adaptateurs `EmailChannelAdapter`, `SmsChannelAdapter`, `PushChannelAdapter` implémentent le port commun `INotificationChannel { ChannelId Id { get; } Task SendAsync(WakeUpMessage, ContactPoint, CancellationToken); }` et **normalisent les comportements** : troncature SMS, conversion du `false` du push et du reçu SMS en échec, mise en forme HTML de l'email. C'est là, et seulement là, que vivent ces détails.

### F6 — Dispatcher et repli de canal
`NotificationDispatcher` (Application, implémente `INotificationDispatcher`) reçoit `IEnumerable<INotificationChannel>` par injection :
1. Essaie le canal préféré (si l'utilisateur a un `ContactPoint` pour ce canal).
2. En cas d'échec ou de canal inconnu → essaie les autres canaux dans l'ordre `Notifications:FallbackOrder` (config), pour lesquels l'utilisateur a un contact.
3. Si tout échoue → canal de dernier recours `LastResortChannel` (log `Critical` + fichier `outbox/undelivered.log`) : le réveil est tracé, jamais perdu silencieusement ; `ChannelDegraded = true`.

**Démonstration d'extensibilité** : ajoute un `FakeWhatsAppClient` + `WhatsAppChannelAdapter` dans un commit dédié, en montrant dans le message de commit / README que seuls 2 fichiers + 1 ligne d'enregistrement ont changé et qu'aucun test existant n'a cassé (mesure concrète du lock-in, comme dans le debrief du cours).

### F7 — Composition du message
`IWakeUpMessageComposer` (implémentation par défaut dans l'Application) : texte en français selon le jour et la météo, ex. « Bon lundi ! Il pleut aujourd'hui, on se réveille avec *Riders on the Storm* — The Doors. ». Mention discrète si le morceau vient du mode dégradé.

### F8 — Observabilité minimale
Logs structurés (`ILogger` avec placeholders, pas d'interpolation) sur : provider tenté/réussi/échoué, canal tenté/réussi/échoué, dégradations. Un health check `/health` (`Microsoft.Extensions.Diagnostics.HealthChecks`, inclus dans ASP.NET Core).

---

## 7. Matrice de résilience (à reproduire dans le README et à tester)

| Panne | Comportement attendu | Résultat |
|---|---|---|
| iTunes HS / timeout / quota | MusicBrainz tenté | morceau réel, non dégradé |
| iTunes + MusicBrainz HS | fallback local | `MusicDegraded` |
| Morceau introuvable partout | fallback local | `MusicDegraded` |
| Resolver lève malgré tout | `FallbackTrack` brut de l'utilisateur | `MusicDegraded` |
| Service préférences HS, cache présent | préférences en cache | dégradé |
| Service préférences HS, pas de cache | `503` + log `Critical` (impossible de savoir qui prévenir) | documenté |
| Canal préféré HS | canal suivant de `FallbackOrder` | `ChannelDegraded` |
| Tous les canaux HS | `LastResortChannel` | `ChannelDegraded`, trace persistée |
| Canal préféré inconnu (non enregistré) | repli | `ChannelDegraded` |

---

## 8. Configuration (exemple `appsettings.json`)

```json
{
  "Music": {
    "Providers": [ "itunes", "musicbrainz" ],
    "GlobalTimeoutSeconds": 5,
    "CacheTtlHours": 24,
    "Itunes": { "BaseUrl": "https://itunes.apple.com/", "RequestsPerMinute": 20, "TimeoutSeconds": 2 },
    "MusicBrainz": { "BaseUrl": "https://musicbrainz.org/ws/2/", "UserAgent": "ReveilMusical/1.0 ( contact@exemple.fr )", "TimeoutSeconds": 2 }
  },
  "Notifications": {
    "FallbackOrder": [ "push", "sms", "email" ],
    "OutboxDirectory": "outbox",
    "Email": { "SimulateFailure": false },
    "Sms":   { "SimulateFailure": false },
    "Push":  { "SimulateFailure": false }
  },
  "Users": { "SimulateFailure": false, "Profiles": [ /* 3 à 5 profils de démo */ ] }
}
```

Prévois `appsettings.Demo-Degraded.json` (toutes les pannes activées) pour montrer le mode dégradé en une commande : `dotnet run --project src/ReveilMusical.Api --environment Demo-Degraded`.

---

## 9. Tests

Objectif : **≥ 90 % de couverture de lignes sur Domain + Application**, **≥ 80 % global** (hors `Program.cs`). Aucun test ne fait d'appel réseau réel.

- **Domain.Tests** : value objects (validation, égalité), `WeatherBasedSelectionPolicy` (météo couverte / non couverte, tous les cas de `WeatherType` via `[Theory]`).
- **Application.Tests** (NSubstitute) : `SendWakeUpUseCase` sur chaque ligne de la matrice §7 ; `NotificationDispatcher` (préféré OK, repli, tout KO → last resort, canal sans contact ignoré) ; composer (jours × météos) ; injection d'une policy alternative (preuve du Strategy).
- **Infrastructure.Tests** :
  - adaptateurs HTTP testés avec un `HttpMessageHandler` factice et des **fixtures JSON réelles** enregistrées (`Fixtures/itunes-search.json`, `musicbrainz-recording.json`) : mapping correct, réponse vide → null, 500/timeout → exception, JSON malformé → exception ;
  - présence et valeur du `User-Agent` MusicBrainz ;
  - URL-encoding de la requête ;
  - rate limiter : la 21ᵉ requête dans la minute échoue vite (avec `FakeTimeProvider`) ;
  - cache : 2ᵉ appel identique ne touche pas le réseau ;
  - `FallbackChainTrackResolver` : ordre respecté, budget global respecté, clé inconnue rejetée ;
  - adaptateurs de canaux : troncature SMS 160, `false` du push → échec, reçu SMS en erreur → échec ;
  - **tests de contrat par abstraction** : une classe abstraite `TrackProviderContractTests` (et `NotificationChannelContractTests`) héritée une fois par implémentation, qui vérifie les invariants communs (jamais de `Track` avec titre vide, respect du `CancellationToken`, etc.).
- **Api.Tests** (`WebApplicationFactory`, services externes remplacés via `ConfigureTestServices`) : 200 nominal, 400 entrées invalides, 404 utilisateur inconnu, 200 dégradé quand tout est en panne, et **un test qui démarre l'app avec une config qui change de fournisseur** pour prouver le changement sans recompilation. Vérifie aussi que la réponse JSON ne contient aucun champ fournisseur (`trackViewUrl`, etc.).
- **Architecture.Tests** (NetArchTest) :
  - Domain ne dépend d'aucun autre projet ni de `System.Net.Http` ;
  - Application ne dépend pas des projets Infrastructure ni de `System.Net.Http` ;
  - aucun type public des projets Infrastructure en dehors des méthodes d'enregistrement ;
  - les types `*Dto` ne sont utilisés que dans leur namespace d'adaptateur ;
  - aucun type nommé `*Itunes*`, `*MusicBrainz*`, `*Sms*`, `*Email*`, `*Push*` dans Domain/Application.
- Nommage : `Method_State_ExpectedResult`, structure Arrange / Act / Assert.
- Script ou commande documentée pour générer le rapport : `dotnet test --collect:"XPlat Code Coverage"` puis ReportGenerator → `coverage/index.html` (ignoré par Git).

---

## 10. Dépendances, licences et README (livrable légal)

À la fin (et à mettre à jour à chaque ajout de package) :
1. Lance `dotnet list package --include-transitive`, `--outdated`, `--vulnerable`.
2. Installe un outil d'audit de licences en **outil local** (`dotnet new tool-manifest`), par exemple `nuget-license`, et génère le rapport des licences, transitives incluses.
3. Génère un **SBOM CycloneDX** (`CycloneDX` dotnet tool) dans `docs/sbom/`.

Le `README.md` doit contenir :
- présentation, prérequis, commandes (`build`, `test`, `run`, mode démo dégradé, couverture) ;
- architecture (Mermaid), pattern utilisé pour chaque besoin (Adapter, Strategy, Chain of Responsibility, Decorator, Facade éventuelle, Factory via DI) et le besoin métier qu'il sert ;
- **« Comment changer de fournisseur de musique »** (config uniquement) et **« Comment ajouter un canal »** (étapes, fichiers touchés) ;
- matrice de résilience (§7) ;
- hypothèses (jour de la semaine, utilisateur inconnu, service préférences HS sans cache) ;
- **tableau des dépendances** : `Package | Projet | Version installée | Dernière version stable | Date de publication | Licence | Type (permissive/copyleft/propriétaire) | Directe/transitive notable | Commentaire`. Inclure aussi les **services externes** (iTunes Search API : conditions d'utilisation Apple, quota ; MusicBrainz : données CC0 / CC BY-NC-SA selon les jeux, User-Agent et 1 req/s) ;
- section « Composants écartés et pourquoi » (§3) ;
- section « Points de vigilance » : toute version non à jour justifiée, toute licence non permissive justifiée (il ne doit y en avoir aucune en production) ;
- date de l'audit.

---

## 11. Qualité de code

- Classes `sealed` par défaut, records pour les données, `CancellationToken` propagé partout, `ConfigureAwait` inutile (ASP.NET Core).
- Pas de `async void`, pas de `.Result` / `.Wait()`.
- Pas de logique dans `Program.cs` au-delà de la composition.
- Pas de nombres magiques : options typées.
- Pas de `catch (Exception)` silencieux : chaque catch logge et transforme en dégradation explicite.
- Durées de vie DI réfléchies et commentées : providers HTTP via typed clients, cache et rate limiters en singleton, aucun service scoped capturé par un singleton (piège de la dépendance captive vu en cours).
- `dotnet format --verify-no-changes` doit passer.

---

## 12. Git : branches, commits au fur et à mesure et merges

### 12.1 Règles générales
- `git init -b main` dès la première étape ; `.gitignore` .NET (+ `outbox/`, `coverage/`, `TestResults/`).
- **`main` est toujours stable** : après le commit d'initialisation, on n'y commite plus jamais directement. Tout le travail se fait sur des branches.
- **Un commit par étape logique**, petit et atomique, **après** `dotnet build` + `dotnet test` verts. Jamais de commit qui casse le build.
- Format **Conventional Commits** (choisis anglais ou français et reste cohérent) : `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`, `build:`, `ci:`. Corps de message expliquant le *pourquoi* quand ce n'est pas évident.
- Ne commite jamais de secret, de log d'outbox ni de rapport de couverture.

### 12.2 Stratégie de branches (GitHub Flow)
- Une branche **par fonctionnalité**, créée depuis `main` à jour, nommée `<type>/<sujet-court>` : `feature/…`, `test/…`, `ci/…`, `docs/…`, `fix/…`, `chore/…`.
- Plusieurs commits atomiques par branche (pas un seul commit fourre-tout).
- Avant chaque merge :
  1. mets `main` à jour (`git switch main && git pull` si un remote existe), puis `git switch <branche> && git rebase main` ;
  2. vérifie en local que `dotnet build`, `dotnet test` et `dotnet format --verify-no-changes` passent ;
  3. **si un remote GitHub est configuré** : `git push -u origin <branche>`, ouvre une **Pull Request** (`gh pr create` si `gh` est disponible) avec une description (quoi, pourquoi, comment tester), attends que la **CI soit verte** (§13), puis merge ;
  4. **sans remote** : merge en local.
- Merge **toujours en `--no-ff`** (`git merge --no-ff <branche> -m "merge: <branche> — <résumé>"`) pour que chaque fonctionnalité apparaisse comme un bloc dans l'historique.
- Après le merge : relance `dotnet test` sur `main`, puis supprime la branche (`git branch -d`, et `git push origin --delete` si remote).
- En cas de conflit : résous-le, relance build + tests, explique la résolution dans le message de merge.
- À la fin : tag annoté `git tag -a v1.0.0 -m "Première version livrable"`.
- Si le dépôt est sur GitHub : documente dans le README la **protection de `main`** recommandée (PR obligatoire, CI obligatoire avant merge, pas de force-push).

### 12.3 Plan de branches et de commits (adapte si besoin, garde cette granularité)

**Sur `main` (seul commit direct autorisé)**
1. `chore: initialize repository with gitignore and README skeleton`

**`chore/solution-setup`**
2. `chore: add solution, global.json, Directory.Build.props, editorconfig`
3. `build: enable central package management and lock files`

**`ci/pipeline`** (tôt, pour que toutes les branches suivantes soient vérifiées)
4. `ci: add build, format and test workflow`
5. `ci: add dependency audit (vulnerabilities, freshness, licenses)`
6. `ci: add Dependabot configuration`

**`feature/domain-model`**
7. `feat(domain): add value objects and weather/track model` + tests
8. `feat(domain): add weather-based track selection policy` + tests

**`feature/wake-up-use-case`**
9. `feat(app): define ports for preferences, tracks and notifications`
10. `feat(app): implement SendWakeUpUseCase with safety net` + tests
11. `feat(app): add wake-up message composer` + tests

**`feature/notification-dispatcher`**
12. `feat(app): implement notification dispatcher with channel fallback` + tests

**`feature/user-preferences`**
13. `feat(users): add in-memory preferences provider` + tests
14. `feat(users): add last-known-value cache decorator` + tests

**`feature/music-providers`**
15. `feat(music): add local fallback track provider` + tests
16. `feat(music): add iTunes adapter with rate limiting` + fixtures + tests
17. `feat(music): add MusicBrainz adapter with mandatory User-Agent` + tests
18. `feat(music): add caching decorator and resilience pipeline` + tests
19. `feat(music): add configurable fallback chain resolver` + tests
20. `test(music): add provider contract tests`

**`feature/notification-channels`**
21. `feat(notifications): add fake email/sms/push SDKs and outbox`
22. `feat(notifications): add channel adapters` + contract tests

**`feature/api-endpoint`**
23. `feat(api): add POST /api/wake-ups endpoint and composition root`
24. `feat(api): add demo-degraded configuration and health check`
25. `test(api): add end-to-end tests including provider switch by config`

**`test/architecture`**
26. `test(arch): enforce layering and DTO isolation`

**`feature/whatsapp-channel`** (démo d'extensibilité : le diff du merge doit montrer 2 fichiers + 1 ligne d'enregistrement)
27. `feat(notifications): add WhatsApp channel adapter`

**`chore/audit-tooling`**
28. `chore: add local tools for license audit, coverage and SBOM`
29. `ci: publish SBOM and coverage report as artifacts`

**`docs/readme`**
30. `docs: write README with dependency/license audit and architecture`
31. `docs: add ADRs for key technical decisions`

Chaque bloc se termine par un merge `--no-ff` dans `main`. Tag `v1.0.0` à la fin.

---

## 13. Intégration continue (GitHub Actions)

Crée `.github/workflows/ci.yml`, déclenché sur `push` vers `main`, sur toute `pull_request` et en `workflow_dispatch`. Utilise `actions/setup-dotnet` avec la version de `global.json`, le cache NuGet, et des **lock files** (`RestorePackagesWithLockFile=true` dans `Directory.Build.props`, restauration en `--locked-mode` en CI) pour des builds reproductibles. Permissions minimales (`contents: read`), actions épinglées sur une version majeure.

Jobs :

1. **build-and-test** (bloquant)
   - `dotnet restore --locked-mode`
   - `dotnet build --no-restore -c Release` (warnings = erreurs)
   - `dotnet format --verify-no-changes`
   - `dotnet test --no-build -c Release --collect:"XPlat Code Coverage"` (inclut les tests d'architecture et les tests de contrat)
   - ReportGenerator : résumé de couverture dans `GITHUB_STEP_SUMMARY`, rapport HTML en artefact
   - **échec si la couverture passe sous les seuils** du §9.

2. **dependency-audit** (vérification des dépendances)
   - **Vulnérabilités — bloquant** : `NuGetAudit` activé dans `Directory.Build.props` (`NuGetAuditMode=all`, `NuGetAuditLevel=low`) pour que toute vulnérabilité connue, transitives comprises, casse le build ; plus `dotnet list package --vulnerable --include-transitive` affiché dans le résumé.
   - **Fraîcheur — informatif** : `dotnet list package --outdated --include-transitive` publié dans le résumé du job. Non bloquant : une mise à jour se décide humainement, elle ne doit pas casser le build. Le README justifie toute version en retard.
   - **Licences — bloquant** : `dotnet tool restore` puis l'outil d'audit (`nuget-license` ou équivalent) contre une **liste blanche versionnée** (`licenses-allowed.json` : MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, MS-PL…). Échec si une licence copyleft (GPL, AGPL, LGPL…), propriétaire ou inconnue apparaît, transitives comprises. Les exceptions éventuelles sont listées dans un fichier versionné, avec justification.
   - **Sur les pull requests** : `actions/dependency-review-action` signale toute nouvelle dépendance vulnérable ou à licence refusée introduite par la PR.

3. **sbom** (sur `main` uniquement) : génération du SBOM CycloneDX, publié en artefact.

À ajouter aussi :
- `.github/dependabot.yml` : mises à jour hebdomadaires pour `nuget` (groupées : `Microsoft.*`, outils de test) et pour `github-actions`. Chaque PR Dependabot passe par la même CI.
- Un **badge CI** en tête du README.
- Une section README « CI » qui explique chaque job, ce qui est bloquant et ce qui est informatif, et le lien avec l'exigence légale « aucun composant sans vérification de licence et de fraîcheur ».
- La CI ne fait **aucun appel réseau vers iTunes ou MusicBrainz** : tous les tests utilisent des fakes et des fixtures.

Avant de pousser, valide la syntaxe du workflow (`actionlint` s'il est disponible) et vérifie que chaque commande du workflow passe en local.

---

## 14. Definition of Done (vérifie chaque point avant de conclure)

- [ ] `dotnet build` sans warning, `dotnet test` 100 % vert, `dotnet format --verify-no-changes` OK.
- [ ] `grep -rn "new HttpClient\|new .*Provider(\|new .*Adapter(\|DateTime.Now" src/` ne renvoie rien de problématique.
- [ ] Aucun type d'infrastructure référencé dans Domain/Application (tests d'architecture verts).
- [ ] Changer `Music:Providers` dans la config change le fournisseur sans recompiler (testé).
- [ ] Le mode `Demo-Degraded` produit un réveil dégradé, jamais un silence.
- [ ] Couverture ≥ 90 % Domain+Application, ≥ 80 % global ; rapport généré.
- [ ] README complet avec tableau licences/versions/fraîcheur daté, SBOM présent.
- [ ] Historique Git propre : commits atomiques, messages conventionnels, build vert à chaque commit.
- [ ] Chaque fonctionnalité développée sur sa propre branche et mergée en `--no-ff` dans `main` ; aucune branche restante ; tag `v1.0.0` posé.
- [ ] Workflow CI présent et vert (build, format, tests, couverture, vulnérabilités, licences, SBOM) ; Dependabot configuré ; badge dans le README.

Quand tout est fait, donne-moi un résumé : architecture, patterns et besoin métier associé, résultat de couverture, résultat de l'audit de licences, la liste des branches mergées, l'historique (`git log --oneline --graph`) et l'état de la CI.
