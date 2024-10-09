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
    private static WorldInteraction instance;
    public static WorldInteraction I { get => instance; }
    void Awake() { if (instance == null) instance = this; }

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
    public Inventory inventory;
    public Grid grid; // TODO remove
    public Terrain terrain;
    public Transform bag;
    public SortingGroup largeCharacterSelectPrefab; // will delete
    public SortingGroup smallCharacterSelectPrefab; // will delete
    public Cardboard largeCharacterSelectPrefab_new;
    public Cardboard smallCharacterSelectPrefab_new;
    public Tilemap uiMap; // will delete
    public Tilemap uiMapEdgeX; // will delete
    public Tilemap uiMapEdgeY; // will delete
    public TileBase hoverTile; // will delete
    public TileBase edgeHoverTileX; // will delete
    public TileBase edgeHoverTileY; // will delete
    public GameObject hoverSquare;
    public GameObject hoverEdge;
    public LineRenderer lineSelect;

    public Collectible wood;
    public Collectible soil;

    public float signalFrontOfPlayer = 1f;
    public float signalMeleeRadius = 2f;
    public float signalRangedRadius = 1f;
    public float signalRangedDistance = 4f;

    public bool freeTamingCheat = false;

    private Transform player;
    private Ranged rangedSelect;
    private Melee meleeSelect;
    private MeleeSquare meleeSquare;
    private Tele teleSelect;
    private Vector2Int activeTile = Vector2Int.zero;
    private Tilemap activeGrid; // will delete
    private bool lineStarterUpdate;
    private Character activeCharacter; // hover
    private GameObject activeCharacterHighlight;
    private DeStack<Character> followingCharacters = new DeStack<Character>();
    private GameObject followingCharacterHighlight;
    private Dictionary<Terrain.Grid, TileBase> hoverTiles; // will delete
    private Dictionary<Terrain.Grid, Tilemap> uiMaps; // will delete

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
        player = GameManager.I.YourPlayer.transform;
        Debug.Log("PLAYER  = " + player);
        rangedSelect = new Ranged(rangedConfig);
        meleeSelect = new Melee(meleeConfig, player);
        meleeSquare = new MeleeSquare(praxelSelectConfig, player);
        teleSelect = new Tele(terrain);
        // hoverTiles = new Dictionary<Terrain.Grid, TileBase>() {
        //     [Terrain.Grid.XWalls] = edgeHoverTileX,
        //     [Terrain.Grid.YWalls] = edgeHoverTileY,
        //     [Terrain.Grid.Roof] = hoverTile
        // };
        // uiMaps = new Dictionary<Terrain.Grid, Tilemap>() {
        //     [Terrain.Grid.XWalls] = uiMapEdgeX,
        //     [Terrain.Grid.YWalls] = uiMapEdgeY,
        //     [Terrain.Grid.Roof] = uiMap
        // };
        ConfirmOngoing = new TaskRunner(ConfirmOngoingE, this);
        inventory = player.GetComponentStrict<Inventory>();
        Orientor.I.onRotation += ClearTile;
        Orientor.I.onRotation += ClearLine;
    }

    public void ClearTile() {
        hoverEdge.SetActive(false);
        hoverSquare.SetActive(false);
    }
    public void ClearCharacter() {
        if (activeCharacterHighlight != null) Destroy(activeCharacterHighlight);
        activeCharacterHighlight = null;
        activeCharacter = null;
    }
    public void ClearLine() {
        lineSelect.positionCount = 0;
        lineStarterUpdate = false;
    }
    public void SetTile(Terrain.Position position) {
        activeTile = position.Coord;
        if (position.grid == Terrain.Grid.Roof) {
            hoverEdge.SetActive(false);
            hoverSquare.SetActive(true);
            hoverSquare.transform.position = MapRenderer3D.ToWorld(activeTile);
        } else {
            hoverSquare.SetActive(false);
            hoverEdge.SetActive(true);
            hoverEdge.transform.position = MapRenderer3D.ToWorld(activeTile);
            hoverEdge.transform.rotation = position.grid == Terrain.Grid.XWalls ? Quaternion.identity : Quaternion.Euler(0, 0, 90);
        }
    }
    public void NewActiveCharacter(Character character) {
        activeCharacter = character;
        if (activeCharacter != null) {
            activeCharacterHighlight = CharacterHighlight3D.New(activeCharacter, true).gameObject;
        } else if (activeCharacterHighlight != null) {
            GameObject.Destroy(activeCharacterHighlight);
            activeCharacterHighlight = null;
        }
    }
    public void ActiveCharacterToFollowing() {
        Debug.Log("Updating UI with active character " + activeCharacter);
        if (followingCharacterHighlight != null) Destroy(followingCharacterHighlight);
        followingCharacterHighlight = activeCharacterHighlight;
        CharacterHighlight3D.SetHighlightHoverToFollowing(followingCharacterHighlight);
        followingCharacters.Push(activeCharacter);
        if (creatureChanged != null) creatureChanged(CurrentAction, activeCharacter.GetComponentStrict<Creature>());
        activeCharacterHighlight = null;
        activeCharacter = null;
        Debug.Log("Pushed, num following characters: " + followingCharacters.Count);
    }
    /** Output: make sure to check for null in case stack was empty */
    public Creature ForcePopFollowing() {
        Debug.Log("Popping, num following characters: " + followingCharacters.Count);
        Character oldFollowingCharacter = followingCharacters.Pop();
        Creature result = oldFollowingCharacter.GetComponentStrict<Creature>();
        if (creatureChanged != null) creatureChanged(CurrentAction, PeekFollowing());
        if (followingCharacterHighlight != null) Destroy(followingCharacterHighlight);

        Character newFollowingCharacter = PeekFollowingCharacter();
        if (newFollowingCharacter != null)
            followingCharacterHighlight = CharacterHighlight3D.New(newFollowingCharacter, false).gameObject;
        else followingCharacterHighlight = null;
        return result;
    }
    public Character PeekFollowingCharacter() {
        // TODO: check for death
        Character possCharacter = null;
        while (possCharacter == null && followingCharacters.Count > 0) {
            Debug.Log("Peeking top character from the stack.  If multiple in a row, some died");
            possCharacter = followingCharacters.Peek();
            if (possCharacter == null) followingCharacters.Pop();
        } // loop if it died
        if (possCharacter == null) // none left
            return null;
        return possCharacter;
    }
    public Creature PeekFollowing() => PeekFollowingCharacter()?.GetComponentStrict<Creature>();

    public void EnqueueFollowing(Creature creature) {
        Character character = creature.GetComponentStrict<Character>();
        QuickCleanUpFollowingCharacters();
        followingCharacters.PushToBottom(character);
        if (followingCharacters.Count == 1) {
            if (creatureChanged != null) creatureChanged(CurrentAction, creature);
            followingCharacterHighlight = CharacterHighlight3D.New(character, false).gameObject;
        }
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
                activeTile = teleSelect.SelectSquareOnly(pointer);
                hoverSquare.transform.position = MapRenderer3D.ToWorld(activeTile);
            break;
            case Mode.WoodBuilding:
                Terrain.Position? buildWhere = teleSelect.SelectBuildLoc(pointer);
                if (buildWhere is Terrain.Position buildHere) SetTile(buildHere);
                else ClearTile();
            break;
            case Mode.Taming:
                if (duringPress) break;
                Character newActiveCharacter = teleSelect.SelectCharacterOnly(pointer);
                if (newActiveCharacter == activeCharacter) break;
                if (activeCharacter != null) ClearCharacter();
                NewActiveCharacter(newActiveCharacter);
            break;
            case Mode.Directing:
                if (duringPress) break;
                ClearTile();
                ClearCharacter();
                ClearLine();
                OneOf<Terrain.Position, Character> target = teleSelect.SelectDynamic(pointer);
                if (target.Is(out Terrain.Position tile)) {
                    // activeTile = tile.Coord; // I don't think this is needed? - during 3D refactor
                    if (teleSelect.Line == null) SetTile(tile);
                    else {
                        List<Vector3> line = teleSelect.Line();
                        lineStarterUpdate = line == null;
                        if (!lineStarterUpdate) {
                            line.Add(terrain.CellCenter(tile));
                            lineSelect.positionCount = line.Count;
                            lineSelect.SetPositions(line.ToArray());
                        }
                    }
                } else if (target.Is(out Character character)) {
                    NewActiveCharacter(character);
                }
            break;
        }
    }

    public void SelectPraxel(Vector2Int praxelSelectLocation) =>
        SetTile(new Terrain.Position(Terrain.Grid.Roof, praxelSelectLocation));

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
                    Collectible.Instantiate(soil, bag, terrain.CellCenter(coord).WithZ(GlobalConfig.I.elevation.collectibles), sodCost);
                } else if (terrain.GetLand(coord) == Land.Dirtpile) {
                    terrain.Land[coord] = Land.Grass;
                    Collectible.Instantiate(soil, bag, terrain.CellCenter(coord).WithZ(GlobalConfig.I.elevation.collectibles), dirtPileCost);
                } else if (terrain.GetLand(coord)?.IsPlanty() == true) {
                    int woodQuantity = terrain.GetLand(coord) == Land.Meadow ? 1 :
                        terrain.GetLand(coord) == Land.Shrub ? 3 : 6;
                    terrain.Land[coord] = Land.Grass;
                    Collectible.Instantiate(wood, bag, terrain.CellCenter(coord).WithZ(GlobalConfig.I.elevation.collectibles), woodQuantity);
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
                if (activeCharacter.Team.SameTeam(player)) {
                    activeCharacter.GetComponentStrict<Creature>().Follow(player);
                    ActiveCharacterToFollowing();
                } else {
                    creature = activeCharacter.GetComponentStrict<Creature>();
                    if (freeTamingCheat) {
                        creature.ForceTame(player);
                        ActiveCharacterToFollowing();
                    } else if (creature.CanTame(player)) {
                        GoodTaste taste = activeCharacter.GetComponent<GoodTaste>();
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
                    Transform swordSwipe = GameObject.Instantiate(swordSwipePrefab, bag).transform;
                    swordSwipe.position = meleeSelect.DamageCenter.WithZ(GlobalConfig.I.elevation.groundLevelHighlight);
                    swordSwipe.localScale = new Vector3(meleeSelect.DamageRadius * 2, meleeSelect.DamageRadius * 2, 1);
                    yield return new WaitForSeconds(swordRate);
                break;
                case Mode.Arrow:
                    if (Input.GetMouseButton(0)) // click not space
                        rangedSelect.PointerToKeys(PointerForAim(worldPoint));
                    if (inventory.Retrieve(Material.Type.Arrow, 1)) {
                        Debug.Log("Player " + player);
                        Arrow.Instantiate(
                            flyingArrowPrefab,
                            bag,
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
                GoodTaste taste = activeCharacter.GetComponent<GoodTaste>();
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
                followingCharacterHighlight = CharacterHighlight3D.New(followingCharacters.Peek(), false).gameObject;
                if (creatureChanged != null) creatureChanged(CurrentAction,
                        followingCharacters.Peek().GetComponentStrict<Creature>());
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
        foreach (Character character in followingCharacters) if (character != null)
            character.GetComponentStrict<Creature>().FollowOffensive(offensiveTarget);
    }

    private Vector2 PointerForAim(Vector2 pointer) =>
        player.GetComponent<PlayerCharacter>().InputVelocity == Vector2Int.zero
                    ? pointer - (Vector2)player.position : Vector2.zero;

}
