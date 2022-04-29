using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;

public struct Interaction {
    public WorldInteraction.Mode mode;
    public Creature creature;
    public int creatureAction;
    private Interaction(WorldInteraction.Mode mode, Creature creature, int creatureAction) {
        this.mode = mode;
        this.creature = creature;
        this.creatureAction = creatureAction;
    }

    public static Interaction Player(WorldInteraction.Mode mode) {
        if (mode == WorldInteraction.Mode.Directing)
            throw new ArgumentException("Don't pass Directing into Player Interaction");
        return new Interaction(mode, null, -1);
    }
    public static Interaction Creature(Creature creature, int action) =>
        new Interaction(WorldInteraction.Mode.Directing, creature, action);

    public CreatureAction CreatureAction {
        get {
            if (!IsCreatureAction)
                throw new InvalidOperationException("No CreatureAction, in mode " + mode);
            return creature.action[creatureAction];
        }
    }
    public bool IsCreatureAction {
        get => mode == WorldInteraction.Mode.Directing;
    }

    public override bool Equals(object obj) {
        if (obj is Interaction other) {
            if (this.mode != other.mode) return false;
            else if (!IsCreatureAction) return true;
            else {
                if (this.creature.creatureName != other.creature.creatureName) return false;
                if (this.creatureAction != other.creatureAction) return false;
                return true;
            }
        } else return false;
    }
    public override int GetHashCode() =>
        mode.GetHashCode() + creature.creatureName.GetHashCode() + creatureAction.GetHashCode();
    
    public static bool operator==(Interaction one, Interaction two) => one.Equals(two);
    public static bool operator!=(Interaction one, Interaction two) => !one.Equals(two);
}

public class WorldInteraction : MonoBehaviour {
    public Melee.Config meleeConfig = new Melee.Config(1, .5f, 10);
    public Ranged.Config rangedConfig = new Ranged.Config(1f, 6f, 15, null, 6, 100);
    public MeleeSquare.Config praxelSelectConfig = new MeleeSquare.Config(.5f, .0625f);
    public float swordRate = 1/2f;
    public float arrowRate = 1/3f;
    public ExpandableInfo noArrowsTip;
    public int sodCost = 1;
    public int dirtPileCost = 2;
    public Arrow flyingArrowPrefab;
    public GameObject swordSwipePrefab;
    public Color followingCharacterColor;

    new public Camera camera;
    public Transform player;
    public Inventory inventory;
    public Grid grid;
    public Terrain terrain;
    public SortingGroup largeCharacterSelectPrefab;
    public SortingGroup smallCharacterSelectPrefab;
    public Tilemap uiMap;
    public Tilemap uiMapEdgeX;
    public Tilemap uiMapEdgeY;
    public TileBase hoverTile;
    public TileBase edgeHoverTileX;
    public TileBase edgeHoverTileY;
    public LineRenderer lineSelect;

    public Collectible wood;
    public Collectible soil;

    public float signalFrontOfPlayer = 1f;
    public float signalMeleeRadius = 2f;
    public float signalRangedRadius = 1f;
    public float signalRangedDistance = 4f;

    public bool freeTamingCheat = false;

    private Ranged rangedSelect;
    private Melee meleeSelect;
    private MeleeSquare meleeSquare;
    private Tele teleSelect;
    private Vector3Int activeTile = Vector3Int.zero;
    private Tilemap activeGrid;
    private bool lineStarterUpdate;
    private SpriteSorter activeCharacter;
    private SortingGroup activeCharacterHighlight;
    private DeStack<SpriteSorter> followingCharacters = new DeStack<SpriteSorter>();
    private SortingGroup followingCharacterHighlight;
    private Dictionary<Terrain.Grid, TileBase> hoverTiles;
    private Dictionary<Terrain.Grid, Tilemap> uiMaps;

    public Action<Interaction, Creature> interactionChanged;
    public Action<Interaction, Creature> creatureChanged;

    public enum Mode {
        Sword,
        Arrow,
        Praxel,
        WoodBuilding,
        Sod,
        Taming,
        Directing
    }
    private Interaction currentAction = Interaction.Player(Mode.Sword);

    public Mode PlayerAction {
        get => currentAction.mode;
        set => CurrentAction = Interaction.Player(value);
    }
    public Interaction CurrentAction {
        get => currentAction;
        set {
            ClearTile();
            ClearLine();
            if (currentAction.mode == Mode.Arrow && value.mode != Mode.Arrow) rangedSelect.Reset();
            currentAction = value;
            activeGrid?.SetTile(activeTile, null);
            if (interactionChanged != null) interactionChanged(currentAction, PeekFollowing());
        }
    }
    public CreatureAction CreatureAction {
        get => currentAction.CreatureAction;
    }
    private void Direct(Creature creature, int action) {
        this.teleSelect.DynamicFilter = creature.action[action].dynamicFilter;
        CurrentAction = Interaction.Creature(creature, action);
    }
    public void MaybeUseCreatureAction(int actionIndex) {
        Creature creature = PeekFollowing();
        if (creature == null || actionIndex >= creature.action.Count) return;
        CreatureAction action = creature.action[actionIndex];
        if (action.IsInstant) {
            ForcePopFollowing();
            action.instantDirective(creature);
        }
        else Direct(creature, actionIndex);
    }
    public List<Interaction> Actions() {
        List<Interaction> actions = new List<Interaction>();
        actions.Add(Interaction.Player(Mode.Sword));
        actions.Add(Interaction.Player(Mode.Arrow));
        actions.Add(Interaction.Player(Mode.Praxel));
        actions.Add(Interaction.Player(Mode.WoodBuilding));
        actions.Add(Interaction.Player(Mode.Sod));
        actions.Add(Interaction.Player(Mode.Taming));
        Creature creature = PeekFollowing();
        if (creature != null) for (int i = 0; i < creature.action.Count; i++) 
            actions.Add(Interaction.Creature(creature, i));
        Debug.Log("Creature " + creature + ", number of actions " + actions.Count);
        return actions;
    }

    void Start() {
        rangedSelect = new Ranged(rangedConfig);
        meleeSelect = new Melee(meleeConfig, player);
        meleeSquare = new MeleeSquare(praxelSelectConfig, player, grid);
        teleSelect = new Tele(terrain);
        hoverTiles = new Dictionary<Terrain.Grid, TileBase>() {
            [Terrain.Grid.XWalls] = edgeHoverTileX,
            [Terrain.Grid.YWalls] = edgeHoverTileY,
            [Terrain.Grid.Roof] = hoverTile
        };
        uiMaps = new Dictionary<Terrain.Grid, Tilemap>() {
            [Terrain.Grid.XWalls] = uiMapEdgeX,
            [Terrain.Grid.YWalls] = uiMapEdgeY,
            [Terrain.Grid.Roof] = uiMap
        };
        ConfirmOngoing = new TaskRunner(ConfirmOngoingE, this);
        inventory = player.GetComponentStrict<Inventory>();
        Orientor.I.onRotation += ClearTile;
        Orientor.I.onRotation += ClearLine;
    }

    public void ClearTile() => activeGrid?.SetTile(activeTile, null);
    public void ClearCharacter() {
        if (activeCharacterHighlight != null) Destroy(activeCharacterHighlight.gameObject);
        activeCharacter = null;
    }
    public void ClearLine() {
        lineSelect.positionCount = 0;
        lineStarterUpdate = false;
    }
    public void ActiveCharacterToFollowing() {
        followingCharacters.Push(activeCharacter);
        Debug.Log("Updating UI with active character " + activeCharacter);
        if (creatureChanged != null) creatureChanged(CurrentAction, activeCharacter.GetCharacterComponent<Creature>());
        if (followingCharacterHighlight != null) Destroy(followingCharacterHighlight.gameObject);
        followingCharacterHighlight = activeCharacterHighlight;
        followingCharacterHighlight.GetComponentInChildren<SpriteRenderer>().color = followingCharacterColor;
        activeCharacterHighlight = null;
        activeCharacter = null;
        Debug.Log("Pushed, num following characters: " + followingCharacters.Count);
    }
    /** Output: make sure to check for null in case stack was empty */
    public Creature ForcePopFollowing() {
        Debug.Log("Popping, num following characters: " + followingCharacters.Count);
        Creature result = followingCharacters.Pop().GetCharacterComponent<Creature>();
        if (creatureChanged != null) creatureChanged(CurrentAction, PeekFollowing());
        if (followingCharacterHighlight != null) Destroy(followingCharacterHighlight.gameObject);
        if (followingCharacters.Count > 0) {
            SpriteSorter followingCharacter = followingCharacters.Peek();
            followingCharacterHighlight = NewCharacterHighlight(followingCharacter, false);
        }
        return result;
    }
    public Creature PeekFollowing() {
        // TODO: check for death
        SpriteSorter possCreature = null;
        while (possCreature == null && followingCharacters.Count > 0) {
            Debug.Log("Peeking top character from the stack.  If multiple in a row, some died");
            possCreature = followingCharacters.Peek();
            if (possCreature == null) followingCharacters.Pop();
        } // loop if it died
        if (possCreature == null) // none left
            return null;
        Creature result = possCreature.GetCharacterComponent<Creature>();
        return result;
    }
    public void EnqueueFollowing(Creature creature) {
        SpriteSorter characterSprite = creature.GetComponentInChildren<SpriteSorter>();
        QuickCleanUpFollowingCharacters();
        followingCharacters.PushToBottom(characterSprite);
        if (followingCharacters.Count == 1) {
            if (creatureChanged != null) creatureChanged(CurrentAction, creature);
            followingCharacterHighlight = NewCharacterHighlight(characterSprite, false);
        }
    }
    public SortingGroup NewCharacterHighlight(SpriteSorter character, bool active) {
        SortingGroup result = character.AddGroup(
            character.broadGirth ? largeCharacterSelectPrefab : smallCharacterSelectPrefab,
            GlobalConfig.I.elevation.groundLevelHighlight);
        if (!active) result.GetComponentInChildren<SpriteRenderer>().color = followingCharacterColor;
        return result;
    }

    public void PlayerMove(Vector2 velocity) {
        switch (PlayerAction) {
            case Mode.Sword:
                meleeSelect.InputVelocity = velocity;
            break;
            case Mode.Arrow:
                rangedSelect.InputVelocity = velocity;
            break;
            case Mode.Praxel:
                meleeSquare.InputVelocity = velocity;
            break;
        }

    }

    public void PointerMove(Vector2 pointer, bool duringPress) {
        switch (PlayerAction) {
            case Mode.Arrow:
                rangedSelect.PointerToKeys(PointerForAim(pointer));
            break;
            case Mode.Sod:
                ClearTile();
                activeTile = (Vector3Int)teleSelect.SelectSquareOnly(pointer);
                activeGrid = uiMap;
                uiMap.SetTile(activeTile, hoverTile);
            break;
            case Mode.WoodBuilding:
                ClearTile();
                Terrain.Position? buildWhere = teleSelect.SelectBuildLoc(pointer);
                if (buildWhere is Terrain.Position buildHere) {
                    activeTile = (Vector3Int)buildHere.Coord;
                    activeGrid = uiMaps[buildHere.grid];
                    activeGrid.SetTile(activeTile, hoverTiles[buildHere.grid]);
                }
            break;
            case Mode.Taming:
                if (duringPress) break;
                SpriteSorter newActiveCharacter = teleSelect.SelectCharacterOnly(pointer);
                if (newActiveCharacter == activeCharacter) break;
                if (activeCharacter != null) ClearCharacter();
                activeCharacter = newActiveCharacter;
                if (activeCharacter != null)
                    activeCharacterHighlight = NewCharacterHighlight(activeCharacter, true);
            break;
            case Mode.Directing:
                if (duringPress) break;
                ClearTile();
                ClearCharacter();
                ClearLine();
                OneOf<Terrain.Position, SpriteSorter> target = teleSelect.SelectDynamic(pointer);
                if (target.Is(out Terrain.Position tile)) {
                    activeTile = (Vector3Int)tile.Coord;
                    activeGrid = uiMaps[tile.grid];
                    if (teleSelect.Line == null) activeGrid.SetTile(activeTile, hoverTiles[tile.grid]);
                    else {
                        List<Vector3> line = teleSelect.Line();
                        lineStarterUpdate = line == null;
                        if (!lineStarterUpdate) {
                            line.Add(terrain.CellCenter(tile));
                            lineSelect.positionCount = line.Count;
                            lineSelect.SetPositions(line.ToArray());
                        }
                    }
                } else if (target.Is(out SpriteSorter character)) {
                    activeCharacter = character;
                    activeCharacterHighlight = NewCharacterHighlight(activeCharacter, true);
                }
            break;
        }
    }

    public void SelectPraxel(Vector2Int praxelSelectLocation) {
        ClearTile();
        activeTile = (Vector3Int)praxelSelectLocation;
        activeGrid = uiMap;
        uiMap.SetTile(activeTile, hoverTile);
    }

    public void Confirm(Vector2 worldPoint) {
        Vector2Int coord;
        Creature creature;
        switch (PlayerAction) {
            case Mode.Sword:
            case Mode.Arrow:
                if (!ConfirmOngoing.isRunning) ConfirmOngoing.Start();
            break;
            case Mode.Praxel:
                coord = (Vector2Int)activeTile;
                Vector2Int playerSquare = terrain.CellAt(player.position);
                Vector2Int relativeToPlayer = coord - playerSquare;
                if (relativeToPlayer.x != 0 && terrain.YWall[Math.Max(coord.x, playerSquare.x), coord.y] == Construction.Wood) {
                    terrain.YWall[Math.Max(coord.x, playerSquare.x), coord.y] = Construction.None;
                    if (terrain.Roof[coord] == Construction.Wood) {
                        terrain.Land[coord] = Land.Woodpile;
                        terrain.Roof[coord] = Construction.None;
                    }
                } else if (relativeToPlayer.y != 0 && terrain.XWall[coord.x, Math.Max(coord.y, playerSquare.y)] == Construction.Wood) {
                    terrain.XWall[coord.x, Math.Max(coord.y, playerSquare.y)] = Construction.None;
                    if (terrain.Roof[coord] == Construction.Wood) {
                        terrain.Land[coord] = Land.Woodpile;
                        terrain.Roof[coord] = Construction.None;
                    }
                } else if (terrain.Feature[coord] != null) {
                    terrain.Feature[coord].Attack(player);
                } else if (terrain.GetLand(coord) == Land.Grass) {
                    terrain.Land[coord] = Land.Ditch;
                    Collectible.Instantiate(soil, grid.transform, terrain.CellCenter(coord).WithZ(GlobalConfig.I.elevation.collectibles), sodCost);
                } else if (terrain.GetLand(coord) == Land.Dirtpile) {
                    terrain.Land[coord] = Land.Grass;
                    Collectible.Instantiate(soil, grid.transform, terrain.CellCenter(coord).WithZ(GlobalConfig.I.elevation.collectibles), dirtPileCost);
                } else if (terrain.GetLand(coord)?.IsPlanty() == true) {
                    int woodQuantity = terrain.GetLand(coord) == Land.Meadow ? 1 :
                        terrain.GetLand(coord) == Land.Shrub ? 3 : 6;
                    terrain.Land[coord] = Land.Grass;
                    Collectible.Instantiate(wood, grid.transform, terrain.CellCenter(coord).WithZ(GlobalConfig.I.elevation.collectibles), woodQuantity);
                }
            break;
            case Mode.WoodBuilding:
                Terrain.Position? buildWhere = teleSelect.SelectBuildLoc(worldPoint);
                if (buildWhere is Terrain.Position buildHere)
                    terrain[buildHere] = Construction.Wood;
            break;
            case Mode.Sod:
                coord = teleSelect.SelectSquareOnly(worldPoint);
                if (terrain.Land[coord] == Land.Ditch || terrain.Land[coord] == Land.Water) {
                    if (inventory.Retrieve(Material.Type.Soil, sodCost))
                        terrain.Land[coord] = Land.Grass;
                    else TextDisplay.I.ShowMiniText("You don't have any soil to place");
                } else if (terrain.Land[coord] == Land.Grass) {
                    if (inventory.Retrieve(Material.Type.Soil, dirtPileCost))
                        terrain.Land[coord] = Land.Dirtpile;
                    else TextDisplay.I.ShowMiniText("You don't have enough soil to place pile");
                }
            break;
            case Mode.Taming:
                if (activeCharacter == null) break;
                if (activeCharacter.GetCharacterComponent<Team>().SameTeam(player)) {
                    activeCharacter.GetCharacterComponent<Creature>().Follow(player);
                    ActiveCharacterToFollowing();
                } else {
                    creature = activeCharacter.GetCharacterComponent<Creature>();
                    if (freeTamingCheat) {
                        creature.ForceTame(player);
                        ActiveCharacterToFollowing();
                    } else if (creature.CanTame(player)) {
                        GoodTaste taste = activeCharacter.MaybeGetCharacterComponent<GoodTaste>();
                        if (taste != null && !freeTamingCheat) {
                            taste.StartTaming(player, ActiveCharacterToFollowing);
                        } else {
                            bool tamed = creature.TryTame(player);
                            if (tamed) ActiveCharacterToFollowing();
                            else TextDisplay.I.ShowTip(creature.TamingInfo);
                        }
                    } else TextDisplay.I.ShowTip(creature.TamingInfo);
                }
            break;
            case Mode.Directing:
                Target target = teleSelect.SelectDynamic(worldPoint);
                if (target.IsNeither) return;
                ClearCharacter();
                ClearTile();
                ClearLine();
                creature = PeekFollowing();
                if (creature != null) {
                    CreatureAction creatureAction = CurrentAction.CreatureAction;
                    if (!creatureAction.canQueue) {
                        ForcePopFollowing();
                        PlayerAction = Mode.Taming;
                    }
                    creatureAction.pendingDirective(creature, target);
                }
            break;
        }
    }

    private TaskRunner ConfirmOngoing;
    private IEnumerator ConfirmOngoingE() {
        bool toolChanged = false;
        int i = 0;
        for (i = 0; i < 100_000; i++) { // prevent infinite loop bugs
            // these end conditions are on separate lines for clairity
            if (!InputManager.Firing) break;
            if (toolChanged) break;

            Vector2 worldPoint = InputManager.PointerPosition;
            switch (PlayerAction) {
                case Mode.Sword:
                    if (Input.GetMouseButton(0)) // click not space
                        meleeSelect.PointerToKeys(PointerForAim(worldPoint));
                    meleeSelect.Damage(player.GetComponent<Team>().TeamId);
                    SignalOffensiveTarget(meleeSelect.InputVelocity,
                        signalMeleeRadius, signalFrontOfPlayer, 0);
                    Transform swordSwipe = GameObject.Instantiate(swordSwipePrefab, grid.transform).transform;
                    swordSwipe.position = meleeSelect.DamageCenter.WithZ(GlobalConfig.I.elevation.groundLevelHighlight);
                    swordSwipe.localScale = new Vector3(meleeSelect.DamageRadius * 2, meleeSelect.DamageRadius * 2, 1);
                    yield return new WaitForSeconds(swordRate);
                break;
                case Mode.Arrow:
                    if (Input.GetMouseButton(0)) // click not space
                        rangedSelect.PointerToKeys(PointerForAim(worldPoint));
                    if (inventory.Retrieve(Material.Type.Arrow, 1)) {
                        Arrow.Instantiate(
                            flyingArrowPrefab,
                            grid.transform,
                            player,
                            (Vector2)player.position + (Vector2)rangedSelect.DirectionVector);
                        SignalOffensiveTarget(rangedSelect.DirectionVector,
                            signalRangedRadius, signalFrontOfPlayer, signalRangedDistance);
                    } else TextDisplay.I.ShowTip(noArrowsTip);
                    yield return new WaitForSeconds(arrowRate);
                break;
                default:
                    toolChanged = true;
                break;
            }
        }
        if (i >= 100_000) Debug.Log("Oops, infinite loop");
        Debug.Log("WorldInteration: exiting ConfirmOngoing");
        ConfirmOngoing.Stop();
        yield break;
    }

    public void ConfirmComplete(Vector2 worldPoint) {
        switch (PlayerAction) {
            case Mode.Taming:
                if (activeCharacter == null) break;
                GoodTaste taste = activeCharacter.MaybeGetCharacterComponent<GoodTaste>();
                if (taste != null) {
                    ExpandableInfo? result = taste.StopTaming(player);
                    if (result is ExpandableInfo info) TextDisplay.I.ShowTip(info);
                }
            break;
        }
    }

    private void QuickCleanUpFollowingCharacters() {
        followingCharacters.RemoveAll((c) => c == null);
    }
    private void ThroroughCleanUpTopFollowingCharacter() {
        bool displayNeedsUpdate = false;
        while (followingCharacters.Count > 0 && followingCharacters.Peek() == null) {
            displayNeedsUpdate = true;
            followingCharacters.Pop();
        }
        if (displayNeedsUpdate) {
            if (CurrentAction.mode == Mode.Directing) CurrentAction = Interaction.Player(Mode.Taming);
            if (followingCharacters.Count > 0) {
                followingCharacterHighlight = NewCharacterHighlight(followingCharacters.Peek(), false);
                if (creatureChanged != null) creatureChanged(CurrentAction,
                        followingCharacters.Peek().GetCharacterComponent<Creature>());
            } else if (creatureChanged != null) creatureChanged(CurrentAction, null);
        }
    }

    void FixedUpdate() {
        if (PlayerAction == Mode.Praxel) {
            Vector2Int? mNewPraxelSelectLocation = meleeSquare.GetResultForFixedUpdate();
            if (mNewPraxelSelectLocation is Vector2Int newPraxelSelectLocation) {
                SelectPraxel(newPraxelSelectLocation);
            }
        }
    }

    void Update() {
        if (PlayerAction == Mode.Arrow) {
            rangedSelect.Update();
        }
        if (lineStarterUpdate == true) {
            lineSelect.positionCount = 2;
            lineSelect.SetPositions(new Vector3[] {
                PeekFollowing().transform.position,
                terrain.CellCenter((Vector2Int)activeTile)
            });
        }
        ThroroughCleanUpTopFollowingCharacter();
    }

    public void SignalOffensiveTarget(Vector2 direction, float castRadius, float castStart, float castDistance) {
        Transform offensiveTarget = Creature.FindOffensiveTarget(
            player.GetComponent<Team>().TeamId, player.position, direction,
            castRadius, castStart, castDistance);
        Debug.Log("Signaling to attack: " + offensiveTarget);
        foreach (SpriteSorter character in followingCharacters) if (character != null)
            character.GetCharacterComponent<Creature>().FollowOffensive(offensiveTarget);
    }

    private Vector2 PointerForAim(Vector2 pointer) =>
        player.GetComponent<PlayerCharacter>().InputVelocity == Vector2Int.zero
                    ? pointer - (Vector2)player.position : Vector2.zero;

}
