using System;
using System.Collections;
using UnityEngine;

public class CharacterController {
    public const int subGridUnit = 8;
    
    private Terrain terrain;
    public Transform transform;
    public Rigidbody2D rigidbody;
    public Collider2D collider;
    public SpriteSorter spriteSorter; // may be null if setAnimatorDirectionDirectly
    private Animator animator; // may be null
    private CoroutineWrapper MoveCoroutine;
    private float? personalBubble = null;
    private bool setAnimatorDirectionDirectly = false;
    private Action<Vector2Int> CrossedTile;
    
    private bool snap = false;
    private Vector2 velocityChebyshevSubgridUnit;
    private float timeToChebyshevSubgridUnit;
    private Vector2 animatorDirection;

    public CharacterController(MonoBehaviour parentComponent) {
        terrain = GameObject.FindObjectOfType<Terrain>();
        transform = parentComponent.transform;
        rigidbody = parentComponent.GetComponentStrict<Rigidbody2D>();
        collider = parentComponent.GetComponentStrict<Collider2D>();
        spriteSorter = parentComponent.GetComponentInChildren<SpriteSorter>();
        animator = parentComponent.GetComponent<Animator>();
        if (animator == null) animator = null; // *sigh* Unity . . .
        MoveCoroutine = new CoroutineWrapper(MoveCoroutineE, parentComponent);
        MoveCoroutine.Start();
    }
    public CharacterController WithPersonalBubble(float personalBubble) {
        this.personalBubble = personalBubble;
        return this;
    }
    public CharacterController SettingAnimatorDirectionDirectly() {
        this.setAnimatorDirectionDirectly = true;
        return this;
    }
    public CharacterController WithSnap() {
        snap = true;
        return this;
    }
    public CharacterController WithCrossedTileHandler(Action<Vector2Int> CrossedTile) {
        this.CrossedTile += CrossedTile;
        return this;
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
        Debug.Log(transform + " received Idle");
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
            if (velocityChebyshevSubgridUnit != Vector2Int.zero
                    && Move() is Vector2 move) {
                rigidbody.MovePosition(move);
                if (CrossedTile != null) {
                    Vector2Int oldTile = currentTile;
                    currentTile = terrain.CellAt(move);
                    if (oldTile != currentTile) CrossedTile(currentTile);
                }
                yield return new WaitForSeconds(timeToChebyshevSubgridUnit);
            } else yield return new WaitForFixedUpdate();
        }
    }

    private Vector2? Move() {
        Vector2 newLocation = (Vector2)rigidbody.position + velocityChebyshevSubgridUnit;
        if (personalBubble is float realPersonalBubble) {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(newLocation, realPersonalBubble, LayerMask.GetMask("Player", "Creature", "HealthCreature", "NoCreatures"));
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
        return newLocation;
    }

    public void OrientFurther() {
        SetAnimatorDirection(animatorDirection);
    }
}
