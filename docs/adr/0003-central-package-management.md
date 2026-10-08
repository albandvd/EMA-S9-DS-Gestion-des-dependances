# ADR 0003 — Central Package Management

## Statut
Accepté.

## Contexte
Le projet compte 11 projets (`src/` + `tests/`). Sans coordination, la même dépendance (`Microsoft.Extensions.Logging.Abstractions`, `xunit`, …) risque d'être référencée à des versions différentes selon le projet, rendant l'audit de licences et de fraîcheur peu fiable (« quelle version est *vraiment* utilisée ? »).

## Décision
`Directory.Packages.props` à la racine, `ManagePackageVersionsCentrally=true` et `CentralPackageTransitivePinningEnabled=true`. Chaque `.csproj` référence un paquet par son nom seul (`<PackageReference Include="xunit" />`), la version vient uniquement du fichier central. `RestorePackagesWithLockFile=true` (dans `Directory.Build.props`) génère un `packages.lock.json` par projet, restauré en `--locked-mode` en CI.

## Justification
Une seule source de vérité pour chaque version = audit et mise à jour triviaux (Dependabot ouvre une PR qui modifie un seul fichier). Les lock files garantissent des builds reproductibles (CI et poste de développement restaurent exactement les mêmes versions résolues).

## Conséquences
- `dotnet-project-licenses` (l'outil d'audit de licences) lit les versions par défaut dans le `.csproj` lui-même ; avec CPM, les `PackageReference` n'y portent pas de version, et l'outil les ignore silencieusement (`Skipping invalid entry`, 0 paquet audité, échec silencieux de l'étape la plus importante légalement). La correction — `--use-project-assets-json` (lit les versions résolues dans `project.assets.json`, déjà écrit par `dotnet restore`) — est documentée dans la section CI du README et dans le commit `ci: publish SBOM and coverage report as artifacts`.
- Les fichiers `packages.lock.json` (un par projet) sont commités : c'est voulu (reproductibilité), pas un oubli de `.gitignore`.
