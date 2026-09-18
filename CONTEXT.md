# Pool Table

Domain language shared by gameplay, rules, presentation, and project documentation for the pool game.

## Language

**Tour**:
A continuous period during which the same player keeps the table. It begins when that player receives the turn and increments only when control passes to the other player, even if several legal shots are played in succession.
_Avoid_: Manche, coup

**Table ouverte**:
The phase before solids and stripes are assigned to the two players. The HUD must not imply that either group already belongs to a player during this phase.

**Joueur actif**:
The player who currently owns the tour and is allowed to prepare the next shot.
_Avoid_: Joueur sélectionné

**Groupe**:
The assigned family of object balls, either solids or stripes, associated with a player after the table is no longer open.
_Avoid_: Type de boule

**Coup**:
A single cue strike by the active player. Several consecutive legal coups may belong to the same tour when the shooter keeps control of the table.
_Avoid_: Tour

**Poche libre**:
The match rule in which object balls may be legally pocketed in any pocket without declaring a ball-and-pocket pair before the shot.
_Avoid_: Coup annoncé, called-shot

**Deux coups**:
A temporary entitlement granted to the incoming player after specific fouls. The player may receive up to two shot opportunities within the same tour: a legal miss or a clean own-group success can preserve the second opportunity, while an ordinary turn-ending outcome or a foul can end the entitlement early; after the second opportunity, normal continuation rules resume.
_Avoid_: Deux tours, deux manches

**Bille en main**:
A foul consequence that lets the incoming player reposition the cue ball before resuming play. In this ruleset it may be combined with the Deux coups entitlement for cue-ball pocket/off-table fouls.

**Fin de tour**:
The transition where control of the table passes to the other player. A fin de tour can happen without a foul, for example after a legal shot that pockets only an opponent ball or no ball at all.
_Avoid_: Faute

**Casse**:
The opening shot of the game. It has no minimum rail-contact, power, or spread requirement: the cue ball only needs to make legal contact with the racked object balls; otherwise the ordinary no-object-contact foul applies. A single legally pocketed family can assign groups immediately; pocketing both families keeps the table open, and pocketing the eight ball during the break is a loss in this ruleset.
_Avoid_: Premier tour, casse invalide

**Attribution de groupe**:
The moment a player becomes associated with solids or stripes and the opponent receives the opposite group.
_Avoid_: Choix de groupe

**Coup mixte**:
A shot, after groups are assigned, that pockets at least one ball from the active player's group and at least one ball from the opponent's group.
_Avoid_: Faute mixte
