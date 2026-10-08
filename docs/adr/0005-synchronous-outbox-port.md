# ADR 0005 — `IOutbox` synchrone plutôt qu'asynchrone

## Statut
Accepté (révision d'une décision initiale).

## Contexte
F5 impose des faux SDK volontairement hétérogènes pour simuler des SDK tiers réalistes : `FakeEmailClient.SendMail(...)` doit être **synchrone**, `FakeSmsGateway.PushSmsAsync(...)` async avec reçu, `FakePushService.Notify(...)` synchrone avec `bool`. Les trois doivent écrire leur envoi simulé via le port `IOutbox`. `IOutbox` a d'abord été défini comme `Task WriteAsync(string channel, string content, CancellationToken ct)`.

Appeler une méthode async depuis `FakeEmailClient.SendMail` (synchrone) oblige soit à bloquer (`.GetAwaiter().GetResult()` / `.Result`), soit à faire du fire-and-forget. Les deux sont interdits ou dangereux : la règle qualité du projet interdit explicitement `.Result`/`.Wait()` (risque de deadlock), et un fire-and-forget perd la garantie d'écriture avant retour.

## Décision
`IOutbox` est redéfini en **`void Write(string channel, string content)`**, sans `Task` ni `CancellationToken`. `FileAndConsoleOutbox` fait un appel `ILogger` (sync) et un `File.AppendAllText` (sync) — aucune des deux opérations n'a de raison réelle d'être asynchrone pour ce volume et cette fréquence d'écriture.

## Justification
L'écriture dans l'outbox est un append local (fichier + log), pas un appel réseau ni une opération longue : il n'y a pas de bénéfice de scalabilité à la rendre asynchrone, seulement un coût de complexité qui se répercute en cascade sur trois signatures de SDK volontairement hétérogènes.

## Conséquences
- `NotificationDispatcher.DispatchAsync` (qui reste asynchrone, lui, à cause des appels réseau simulés des adaptateurs SMS) appelle `_outbox.Write(...)` en synchrone au milieu d'une méthode async — parfaitement valide, un appel synchrone depuis du code async ne pose problème que dans l'autre sens (bloquer sur de l'async).
- Ce changement a été fait tôt (branche `feature/notification-channels`, avant que `FakeEmailClient` existe) : `NotificationDispatcher` et son test (`NotificationDispatcherTests`), déjà mergés, ont dû être ajustés dans le même commit plutôt que de garder une interface qui n'aurait jamais pu être implémentée proprement par les trois fakes.
