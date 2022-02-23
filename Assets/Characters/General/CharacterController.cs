using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable] public class TileEvent : UnityEvent<Vector2Int> { }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CharacterController : MonoBehaviour {
    public const int subGridUnit = 8;

    public float personalBubble = 0;
    public bool setAnimatorDirectionDirectly = false;
    public bool snap = false;
    public TileEvent CrossedTile;

    public float waterMovement = 0;
    
    private Terrain terrain;
    [NonSerialized] new public Rigidbody2D rigidbody;
    [NonSerialized] new public Collider2D collider;
    [NonSerialized] public SpriteSorter spriteSorter; // may be null if setAnimatorDirectionDirectly
    private Animator animator; // may be null
    private CoroutineWrapper MoveCoroutine;
    
    private Vector2 velocityChebyshevSubgridUnit; // just the direction
    private float timeToChebyshevSubgridUnit;
    private Vector2 animatorDirection;

    void Awake() {
        terrain = GameObject.FindObjectOfType<Terrain>();
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        spriteSorter = GetComponentInChildren<SpriteSorter>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = null; // *sigh* Unity . . .
    }
    
    void Start() {
        MoveCoroutine = new CoroutineWrapper(MoveCoroutineE, this);
        MoveCoroutine.Start();
    }

    public CharacterController SetVelocity(Vector2 velocity) {
        if (velocity == Vector2.zero) return Idle();
        else return InDirection(velocity);
    }

    public CharacterController InDirection(Vector2 velocity) {
        velocityChebyshevSubgridUnit = velocity / velocity.ChebyshevMagnitude() / subGridUnit;
        timeToChebyshevSubgridUnit = 1f / velocity.ChebyshevMagnitude() / subGridUnit;
        SetAnimatorDirection(velocity);
        animator?.SetBool("Moving", true);
        return this;
    }

    public CharacterController InDirection(Vector2 velocityChebyshevUnit, float speed) {
        velocityChebyshevSubgridUnit = velocityChebyshevUnit / subGridUnit;
        timeToChebyshevSubgridUnit = velocityChebyshevUnit.magnitude / speed / subGridUnit;
        SetAnimatorDirection(velocityChebyshevUnit);
        animator?.SetBool("Moving", true);
        return this;
    }

    // Chain after Toward() to indicate direction faced to the animator.
    public CharacterController Idle() {
        velocityChebyshevSubgridUnit = Vector2.zero;
        animator?.SetBool("Moving", false);
        return this;
    }

    public CharacterController IdleFacing(Vector3 target) {
        Vector3 direction = target - transform.position;
        SetAnimatorDirection(direction);
        return Idle();
    }

    private void SetAnimatorDirection(Vector2 direction) {
        animatorDirection = direction;
        Vector2 orientedDirection = Quaternion.Euler(0, 0, 360 - (int)Orientor.Rotation) * direction;
        if (setAnimatorDirectionDirectly) {
            animator?.SetFloat("X", orientedDirection.x);
            animator?.SetFloat("Y", orientedDirection.y);
        } else {
            int x = Math.Sign(orientedDirection.x);
            animator?.SetFloat("X", Math.Abs(x));
            spriteSorter.transform.localScale = new Vector3(x >= 0 ? 1 : -1, 2, 1);
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
        spriteSorter.LegsVisible = !value;
        spriteSorter.VerticalDisplacement = value ? -.25f : 0;
        return this;
    }

    private Vector2Int currentTile = Vector2Int.zero;
    private IEnumerator MoveCoroutineE() {
        while (true) {
            if (velocityChebyshevSubgridUnit != Vector2Int.zero) {
                Vector2? maybeMove = Move();
                if (maybeMove is Vector2 move) {
                    rigidbody.MovePosition(move);
                    yield return new WaitForSeconds(timeToChebyshevSubgridUnit);
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
            CrossedTile?.Invoke(newTile);
            if (CanCrossTile(newTile)) currentTile = newTile;
            else return null;
        }
        return newLocation;
    }

    private bool CanCrossTile(Vector2Int tile) {
        return waterMovement != 0f || (terrain.GetLand(tile) ?? terrain.Depths) != Land.Water;
    }

    public void OrientFurther() {
        SetAnimatorDirection(animatorDirection);
    }
}
