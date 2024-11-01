using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CharacterController : MonoBehaviour {
    public const int subGridUnit = 4;

    public float personalBubble = 0;
    public Transform body3d; // if not cardboard
    public bool setAnimatorDirectionDirectly = false;
    public bool snap = false;
    public Func<Vector2Int, bool> CrossingTile; // return false to cancel

    public float waterSpeed = 0;
    
    private Terrain terrain;
    [NonSerialized] new public Rigidbody2D rigidbody;
    [NonSerialized] new public Collider2D collider;
    [NonSerialized] public Character character;
    private Stats stats; // null if player
    [NonSerialized] public Cardboard cardboard; // may be null if setAnimatorDirectionDirectly
                                                // null checks also added for move to 3D
    private Animator animator; // may be null
    private TaskRunner MoveCoroutine;
    
    private Displacement velocityChebyshevSubgridUnit; // just the direction
    private float timeToChebyshevSubgridUnit;
    private Displacement animatorDirection;
    private float terrainSpeed;

    void Awake() {
        terrain = GameObject.FindObjectOfType<Terrain>();
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        character = GetComponent<Character>();
        stats = GetComponent<Stats>();
        cardboard = GetComponentInChildren<Cardboard>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = null; // *sigh* Unity . . .
    }
    
    void Start() {
        MoveCoroutine = new TaskRunner(MoveCoroutineE, this);
        MoveCoroutine.Start();
        terrainSpeed = 1;
    }

    public CharacterController SetRelativeVelocity(Displacement velocity) {
        if (velocity == Displacement.zero) return Idle();
        else return InDirection(velocity);
    }

    public float Speed { get => terrainSpeed * (stats != null ? stats.Spd : 2); }
    
    private void UpdateTileSpecificParams() {
        Land land = terrain.GetLand(currentTile) ?? terrain.Depths;

        terrainSpeed = (waterSpeed != 0 && land == Land.Water) ? waterSpeed : 1;
            
        float elevation = land == Land.Water ? -.375f : land == Land.Ditch ? -.25f : 0;

        if (!setAnimatorDirectionDirectly) {
            cardboard.VerticalDisplacement = elevation;
        }
    }

    public CharacterController InDirection(Displacement inputVelocity) {
        velocityChebyshevSubgridUnit = inputVelocity / inputVelocity.chebyshevMagnitude / subGridUnit;
        timeToChebyshevSubgridUnit = 1f / inputVelocity.chebyshevMagnitude / subGridUnit;
        SetAnimatorDirection(inputVelocity);
        animator?.SetBool("Moving", true);
        return this;
    }

    // Chain after Toward() to indicate direction faced to the animator.
    public CharacterController Idle() {
        velocityChebyshevSubgridUnit = Displacement.zero;
        animator?.SetBool("Moving", false);
        return this;
    }

    public CharacterController IdleFacing(Vector3 target) {
        Displacement direction = Disp.FT(transform.position, target);
        SetAnimatorDirection(direction);
        return Idle();
    }

    private void SetAnimatorDirection(Displacement direction) {
        if (body3d != null) {
            body3d.localRotation = direction.quaternion;
            return;
        }
        animatorDirection = direction;
        Displacement orientedDirection = Quaternion.Euler(0, 0, 360 - (int)Orientor.Rotation) * direction;
        if (setAnimatorDirectionDirectly) {
            animator?.SetFloat("X", orientedDirection.x);
            animator?.SetFloat("Y", orientedDirection.y);
        } else {
            int x = Math.Sign(orientedDirection.x);
            animator?.SetFloat("X", Math.Abs(x));
            animator?.SetFloat("Y", Math.Sign(orientedDirection.y));
        }
    }

    public CharacterController SetBool(string name, bool value) {
        animator?.SetBool(name, value);
        return this;
    }
    public CharacterController Trigger(string trigger) {
        animator?.SetTrigger(trigger);
        return this;
    }

    public CharacterController Sitting(bool value) {
        if (value) {
            if (cardboard != null) {
                cardboard.VerticalDisplacement = -.25f;
            }
        } else UpdateTileSpecificParams();
        return this;
    }

    private Vector2Int currentTile = Vector2Int.zero;
    private IEnumerator MoveCoroutineE() {
        yield return new WaitForFixedUpdate();
        while (true) {
            if (velocityChebyshevSubgridUnit != Displacement.zero) {
                Vector2? maybeMove = Move();
                if (maybeMove is Vector2 move) {
                    rigidbody.MovePosition(move);
                    yield return new WaitForSeconds(timeToChebyshevSubgridUnit / Speed);
                } else yield return new WaitForFixedUpdate();
            } else yield return new WaitForFixedUpdate();
        }
    }

    private Vector2? Move() {
        Vector2 newLocation = (Vector2)rigidbody.position + velocityChebyshevSubgridUnit;
        if (personalBubble != 0) {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(newLocation, personalBubble, LayerMask.GetMask("Player", "Creature", "HealthCreature", "NoCreatures"));
            bool doReturnNull = false;
            foreach (Collider2D overlap in overlaps) {
                if (overlap == collider) continue;
                else if (overlap.isTrigger) continue;
                else doReturnNull = true;
            }
            if (doReturnNull) return null;
        }
        if (snap) {
            newLocation = (Vector2)((newLocation * subGridUnit).RoundToInt()) / subGridUnit;
        }
        Vector2Int newTile = terrain.CellAt(newLocation);
        if (newTile != currentTile) {
            if (CanCrossTile(newTile)) {
                currentTile = newTile;
                UpdateTileSpecificParams();
                bool? continueCrossingTile = CrossingTile?.Invoke(newTile);
                if (continueCrossingTile == false) return null;
            }
            else return null;
        }
        return newLocation;
    }

    private bool CanCrossTile(Vector2Int tile) {
        Land land = terrain.GetLand(tile) ?? terrain.Depths;
        bool notWaterProhibited = waterSpeed != 0f || land != Land.Water;
        return notWaterProhibited && land.IsPassable();
    }

    public void OrientFurther() {
        SetAnimatorDirection(animatorDirection);
    }
}
