using System;
using UnityEngine;

public class CharacterController {
    public const int subGridUnit = 8;
    
    private Transform transform;
    private Rigidbody2D rigidbody;
    private Transform spriteSorterTransform;
    private Animator animator; // may be null
    private float? personalBubble = null;

    private bool snap = false;
    private Vector2 velocityChebyshevSubgridUnit;
    private float timeToChebyshevSubgridUnit;
    private float timeDoneMoving = 0;

    public CharacterController(MonoBehaviour parentComponent) {
        transform = parentComponent.transform;
        rigidbody = parentComponent.GetComponentStrict<Rigidbody2D>();
        spriteSorterTransform = parentComponent.GetComponentInChildren<SpriteSorter>().transform;
        animator = parentComponent.GetComponent<Animator>();
        if (animator == null) animator = null; // *sigh* Unity . . .
    }

    public CharacterController WithPersonalBubble(float personalBubble) {
        this.personalBubble = personalBubble;
        return this;
    }

    public CharacterController WithSnap() {
        snap = true;
        return this;
    }

    public CharacterController Toward(Vector2 velocity) {
        velocityChebyshevSubgridUnit = velocity / velocity.ChebyshevMagnitude() / subGridUnit;
        timeToChebyshevSubgridUnit = 1f / velocity.ChebyshevMagnitude() / subGridUnit;
        SetAnimatorDirection(velocity);
        animator?.SetBool("Moving", true);
        return this;
    }

    public CharacterController Toward(Vector2 velocityChebyshevUnit, float speed) {
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
        int x = Math.Sign(direction.x);
        animator?.SetFloat("X", Math.Abs(x));
        spriteSorterTransform.localScale = new Vector3(x >= 0 ? 1 : -1, 2, 1);
        animator?.SetFloat("Y", Math.Sign(direction.y));
    }

    public CharacterController Trigger(string trigger) {
        animator?.SetTrigger(trigger);
        return this;
    }

    public Vector2? FixedUpdate() {
        if (Time.fixedTime > timeDoneMoving)
            if (velocityChebyshevSubgridUnit != Vector2Int.zero)
                if (Move() is Vector2 move) {
            rigidbody.MovePosition(move);
            return move;
        }
        return null;
    }

    private Vector2? Move() {
        Vector2 newLocation = (Vector2)rigidbody.position + velocityChebyshevSubgridUnit;
        if (personalBubble is float realPersonalBubble) {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(newLocation, realPersonalBubble, LayerMask.GetMask("Player", "Creature", "HealthCreature"));
            if (overlaps.Length > 1) return null;
        }
        if (snap) {
            newLocation = (Vector2)((newLocation * subGridUnit).RoundToInt()) / subGridUnit;
        }
        timeDoneMoving = timeToChebyshevSubgridUnit + Time.fixedTime;
        return newLocation;
    }
}
