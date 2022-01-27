using System;
using UnityEngine;

public class CharacterController {
    public const int subGridUnit = 8;
    
    private MonoBehaviour parentComponent;
    private Transform transform;
    private Rigidbody2D rigidbody;
    private Transform spriteSorterTransform;
    private Animator animator; // may be null

    private float? personalBubble {
        get {
            if (parentComponent is Creature creature)
                return creature.personalBubble;
            else return null;
        }
    }

    private Vector2 velocity;
    private float timeDoneMoving = 0;

    public CharacterController(Creature parentComponent) {
        this.parentComponent = parentComponent;
        transform = parentComponent.transform;
        rigidbody = parentComponent.GetComponentStrict<Rigidbody2D>();
        spriteSorterTransform = parentComponent.GetComponentInChildren<SpriteSorter>().transform;
        animator = parentComponent.GetComponent<Animator>();
        if (animator == null) animator = null; // *sigh* Unity . . .
    }

    public CharacterController Toward(Vector2 direction) {
        velocity = direction;
        SetAnimatorDirection(direction);
        animator?.SetBool("Moving", true);
        return this;
    }

    // Chain after Toward() to indicate direction faced to the animator.
    public CharacterController Idle() {
        velocity = Vector2.zero;
        Debug.Log("Where's the animator? " + animator);
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

    public void FixedUpdate() {
        if (Time.fixedTime > timeDoneMoving)
            if (velocity != Vector2Int.zero)
                if (Move() is Vector2 move)
                    rigidbody.MovePosition(move);
    }

    private Vector2? Move() {
        float timeToMove = 1f / (velocity.ChebyshevMagnitude() * subGridUnit);
        Vector2 newLocation = (Vector2)rigidbody.position + velocity * timeToMove;
        if (personalBubble is float realPersonalBubble) {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(newLocation, realPersonalBubble, LayerMask.GetMask("Player", "Creature", "HealthCreature"));
            if (overlaps.Length > 1) return null;
        }
        timeDoneMoving = timeToMove + Time.fixedTime;
        return newLocation;
    }
}
