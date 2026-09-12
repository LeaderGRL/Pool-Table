# Pool Table modernization roadmap

Ce document est la source de vérité du plan de modernisation. Avant toute nouvelle issue, branche ou PR, l'étape concernée doit être relue ici et son état doit être vérifié. Une étape n'est marquée terminée qu'après validation réelle et merge de la PR correspondante.

## Règles d'exécution

- Une petite issue par changement cohérent.
- Une branche par issue.
- Une PR par issue.
- Le merge reste manuel : après ouverture d'une PR, le travail s'arrête jusqu'au merge par le propriétaire du repository.
- Les descriptions d'issues et de PR suivent le niveau de précision de FrogbyteEngine/Frogbyte.
- Les tests adaptés au changement sont exécutés avant ouverture de la PR.
- Les commentaires dans le code sont écrits en anglais.
- Les tâches peuvent être découpées en sous-issues plus petites sans changer l'objectif de la roadmap.

Branches recommandées :

- `infrastructure/<issue>-<slug>`
- `upgrade/<issue>-<slug>`
- `architecture/<issue>-<slug>`
- `feature/<issue>-<slug>`
- `bugfix/<issue>-<slug>`
- `rendering/<issue>-<slug>`
- `networking/<issue>-<slug>`
- `tests/<issue>-<slug>`
- `ci/<issue>-<slug>`

## État actuel

Légende : `DONE` = mergé et vérifié, `PARTIAL` = une partie est déjà couverte mais l'objectif complet reste ouvert, `TODO` = à faire, `PR` = PR ouverte en attente de merge.

Travaux déjà réalisés en support du backlog :

- `DONE` Repository cleanup / Git LFS / history rewrite.
- `DONE` Migration du projet vers Unity `6000.5.1f1` en conservant le Built-in Render Pipeline.
- `DONE` CI de validation du repository sans licence Unity (PR #9).
- `PR` Premier smoke test Unity EditMode de `PoolTable.unity` (issue #10 / PR #11).

## Phase 0 — remettre le repository en état

1. `DONE` **Infrastructure: Purge generated Unity artifacts from repository history**
   Purger `Library`, `Logs`, `obj`, `.vs`, `UserSettings`, builds Windows/WebGL, `.csproj`, `.sln` et archives générées. Faire une sauvegarde locale `git bundle` avant réécriture puis réécrire l'historique comme choisi.
   Référence : issue GitHub #5.

2. `DONE` **Infrastructure: Define Unity repository ignore rules**
   Ajouter un `.gitignore` Unity propre et supprimer les anciens systèmes `.collabignore` / `ignore.conf`.

3. `DONE` **Infrastructure: Define Git LFS asset policy**
   Configurer `.gitattributes`, normalisation des fichiers et LFS pour les gros fichiers source binaires réellement nécessaires.
   Référence : issue GitHub #2.

4. `TODO` **Infrastructure: Add repository contribution workflow**
   Ajouter conventions de branches, commits, issues et PR.

## Phase 1 — migration Unity sans toucher au rendu

5. `DONE` **Upgrade: Migrate project to Unity 6000.5.1f1**
   Ouvrir et resérialiser le projet tout en conservant temporairement le Built-in Render Pipeline.
   Référence : issue GitHub #6 / PR #7.

6. `PARTIAL` **Upgrade: Remove obsolete Unity editor integrations**
   Retirer Collab/Plastic legacy et les extensions editor anciennes sans utilité runtime, notamment ERP si elle est incompatible.
   ERP/Discord SDK a été retiré pendant la migration ; les autres intégrations legacy doivent encore être vérifiées.

7. `DONE` **Upgrade: Update Unity packages for Unity 6.5**
   Mettre Cinemachine et les autres packages aux versions publiées compatibles et figer leurs versions.

8. `PARTIAL` **Upgrade: Restore the playable PoolTable scene**
   Corriger références cassées, scripts manquants et sérialisation.
   La scène s'ouvre et ne contient pas de script manquant ; le caractère réellement jouable reste à valider.

9. `TODO` **Upgrade: Restore Windows and Web build targets**
   Recréer les paramètres de build puisque `EditorBuildSettings` ne contient actuellement aucune scène.

10. `PARTIAL` **Testing: Establish Unity 6 migration baseline**
    Compilation propre, zéro erreur console, ouverture de scène et premier PlayMode smoke test.
    Compilation et ouverture de scène sont validées. PR #11 ajoute un smoke test EditMode ; le PlayMode smoke test reste à faire.

## Phase 2 — fondations modernes

11. `TODO` **Architecture: Introduce project assembly boundaries**
    Ajouter les asmdef Core, Gameplay, Physics, Input, Networking, Presentation et Tests.

12. `TODO` **Architecture: Introduce application composition root**
    Remplacer progressivement les singletons globaux par un bootstrap explicite.

13. `TODO` **Architecture: Introduce typed ball identity**
    Remplacer les tags `white`, `black`, `filled`, `striped`, `ball` par `BallId` et `BallGroup`.

14. `TODO` **Architecture: Model immutable match state**
    Introduire `MatchState`, joueurs, groupes, tour courant et phase de match.

15. `TODO` **Architecture: Model shot intent and shot facts**
    Séparer les commandes du joueur des événements réellement observés pendant le tir.

## Phase 3 — règles 8-ball WPA

Chaque règle aura ses tests EditMode avant d'être branchée au gameplay.

16. `TODO` **Rules: Implement legal break resolution**
17. `TODO` **Rules: Implement open-table state**
18. `TODO` **Rules: Implement player group assignment**
19. `TODO` **Rules: Implement legal first-contact validation**
20. `TODO` **Rules: Implement rail and pocket requirements**
21. `TODO` **Rules: Implement scratch and foul resolution**
22. `TODO` **Rules: Implement ball-in-hand state**
23. `TODO` **Rules: Implement called-shot information**
24. `TODO` **Rules: Implement eight-ball win and loss conditions**

## Phase 4 — nouvelle physique de billard

25. `TODO` **Physics: Normalize table and ball physical scale**
    Utiliser des dimensions physiques cohérentes, avec une boule standard d'environ 57,15 mm.

26. `TODO` **Physics: Rebuild ball rigidbody configuration**
    Masse, collision detection, solver, sleep thresholds et fixed timestep adaptés au billard.

27. `TODO` **Physics: Rebuild cloth friction model**
28. `TODO` **Physics: Implement sliding-to-rolling transition**
29. `TODO` **Physics: Implement cue-ball spin**
30. `TODO` **Physics: Implement rail collision response**
31. `TODO` **Physics: Rebuild pocket detection and capture**
32. `TODO` **Physics: Add shot simulation instrumentation**
    Mesurer trajectoires, énergie, temps d'arrêt et collisions afin de calibrer le gameplay.

L'objectif n'est pas de rendre PhysX déterministe entre machines. En multijoueur, seule la simulation de l'hôte fera autorité.

## Phase 5 — contrôle du joueur

33. `TODO` **Input: Migrate project to Unity Input System**
34. `TODO` **Gameplay: Implement aiming state**
35. `TODO` **Gameplay: Implement shot power control**
36. `TODO` **Gameplay: Implement cue-ball spin control**
37. `TODO` **Gameplay: Add controller input support**
38. `TODO` **Gameplay: Implement ball-in-hand placement**
39. `TODO` **Camera: Rebuild aiming camera**
40. `TODO` **Camera: Implement shot and spectate cameras**

## Phase 6 — conversion URP

41. `TODO` **Rendering: Install and configure URP**
42. `TODO` **Rendering: Convert legacy materials to URP**
43. `TODO` **Rendering: Rebuild pool-table PBR materials**
44. `TODO` **Rendering: Rebuild lighting and reflection setup**
45. `TODO` **Rendering: Add post-processing quality profile**
46. `TODO` **Rendering: Add Windows and Web quality profiles**

URP introduira aussi les particularités de Render Graph de Unity 6, qui devront être prises en compte pour tout futur custom render feature.

## Phase 7 — présentation et juice

47. `TODO` **Audio: Rebuild impact audio from collision energy**
48. `TODO` **Audio: Add rail, pocket and cue impact layers**
49. `TODO` **VFX: Add chalk and cue impact feedback**
50. `TODO` **VFX: Add pocket feedback**
51. `TODO` **Camera: Add impact impulse and shot framing**
52. `TODO` **UI: Build modern match HUD**
53. `TODO` **UI: Add called-shot interaction**
54. `TODO` **UI: Add turn and foul feedback**

Le juice pourra être prononcé visuellement sans altérer la trajectoire physique réelle.

## Phase 8 — multijoueur

55. `TODO` **Networking: Install Multiplayer Services and NGO**
56. `TODO` **Networking: Add anonymous Unity authentication**
57. `TODO` **Networking: Implement Relay session creation**
58. `TODO` **Networking: Implement join-by-code flow**
59. `TODO` **Networking: Make match state host authoritative**
60. `TODO` **Networking: Send authoritative shot intents**
61. `TODO` **Networking: Replicate ball simulation snapshots**
62. `TODO` **Networking: Add client interpolation**
63. `TODO` **Networking: Synchronize turns and WPA rules state**
64. `TODO` **Networking: Handle disconnects and session shutdown**
65. `TODO` **Networking: Add WebGL WSS transport configuration**
66. `TODO` **Networking: Add multiplayer latency simulation tests**

Relay est prévu pour un modèle listen-server : l'hôte crée la session et les joueurs communiquent via Relay sans exposer leurs adresses. Pour la première version, si l'hôte quitte la partie, la partie se termine. La migration d'hôte est une fonctionnalité ultérieure : migrer le propriétaire de session ne suffit pas à reconstruire automatiquement l'état PhysX autoritaire.

## Phase 9 — qualité et livraison

67. `TODO` **Testing: Add Core EditMode test suite**
68. `TODO` **Testing: Add gameplay PlayMode tests**
69. `TODO` **Testing: Add physics calibration tests**
70. `TODO` **Testing: Add two-player Multiplayer Play Mode tests**
71. `PARTIAL` **CI: Add Unity pull-request validation**
    La validation structurelle du repository existe déjà. L'exécution Unity hébergée et la gestion de licence restent à ajouter.
72. `TODO` **CI: Add Windows build validation**
73. `TODO` **CI: Add WebGL build validation**
74. `TODO` **CI: Add automated artifact builds from main**
75. `TODO` **Performance: Establish Windows performance budget**
76. `TODO` **Performance: Establish WebGL performance budget**

## Ordre de reprise

Après merge de la PR #11, relire cette roadmap et choisir la prochaine petite issue qui complète la Phase 1 avant d'entamer le refactor d'architecture. Les statuts ci-dessus doivent être mis à jour uniquement à partir d'éléments vérifiés dans le repository, les tests ou les PR mergées.
