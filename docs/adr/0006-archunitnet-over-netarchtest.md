# ADR 0006 — ArchUnitNET plutôt que NetArchTest.Rules

## Statut
Accepté.

## Contexte
Le cahier des charges suggère `NetArchTest.Rules` (avec `ArchUnitNET` comme alternative explicite) pour vérifier automatiquement les règles de couches (§9, §12.3 étape 26). Avant d'ajouter la dépendance, vérification de sa licence et de sa fraîcheur — exigence légale non négociable du projet.

## Décision
`TngTech.ArchUnitNET` + `TngTech.ArchUnitNET.xUnit` (0.13.4) à la place de `NetArchTest.Rules`.

## Justification
`NetArchTest.Rules` 1.3.2 (dernière version NuGet, publiée le **23 mai 2021**, plus de 4 ans avant cet audit) ne porte **aucune métadonnée de licence** dans son `.nuspec` (`licenseExpression` et `licenseUrl` tous deux absents — vérifié via l'API de registration NuGet). Son dépôt GitHub est bien sous licence MIT, mais aucun outil d'audit automatisé ne peut le déduire du paquet publié : l'ajouter aurait exigé une exception non vérifiable par la CI dans `licenses-allowed.json`/`license-exceptions.json`, contraire à l'esprit même de l'exigence légale du TP.

`TngTech.ArchUnitNET` est activement maintenu (dernière publication le 20 août 2026), sous licence **Apache-2.0** déclarée proprement (`licenseExpression` dans le nuspec), et figure explicitement comme alternative acceptée dans le cahier des charges.

## Conséquences
- API fluide différente (`Types().That().ResideInAssembly(...).Should().NotDependOnAny(...)` plutôt que la syntaxe `NetArchTest.Rules`), sans impact sur la couverture des règles exigées (voir `tests/ReveilMusical.Architecture.Tests/LayeringTests.cs`).
- Piège rencontré et documenté dans le code : `ResideInAssembly(string)` compare au nom **pleinement qualifié** de l'assembly (incluant version/culture), pas au nom simple — utiliser systématiquement l'overload `ResideInAssembly(System.Reflection.Assembly)` à la place, résolu via l'architecture chargée plutôt que par comparaison de chaîne.
- Le namespace du projet de test (`ReveilMusical.Architecture.Tests`) entre en collision avec le type `ArchUnitNET.Domain.Architecture` (le segment `Architecture` du namespace masque le type importé) : la variable est déclarée avec le type pleinement qualifié (`ArchUnitNET.Domain.Architecture`) pour lever l'ambiguïté plutôt que de renommer le projet.
