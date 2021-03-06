using System;
using UnityEngine;

public class Ranged {
    [Serializable] public class Config {
        [SerializeField] public float aimMinRate;
        [SerializeField] public float aimMaxRate;
        [SerializeField] public float aimTransitionAngle;
        [SerializeField] public Transform target;
        [SerializeField] public float targetRange;
        [SerializeField] public float targetZ;
        public Config(float aimMinRate, float aimMaxRate, float aimTransitionAngle, Transform target, float targetRange, float targetZ) {
            this.aimMinRate = aimMinRate;
            this.aimMaxRate = aimMaxRate;
            this.aimTransitionAngle = aimTransitionAngle;
            this.target = target;
            this.targetRange = targetRange;
            this.targetZ = targetZ;
        }
    }
    private Config config;
    private OrientableChild target;

    private float? inputDirection = null;
    private float? direction = null;
    private float? angleKeypressChanged = null;
    private bool keysArePressed = false; // muxes between keys and pointer
    private float pointerDirection = 0;

    public Vector2 InputVelocity {
        set {
            if (!keysArePressed && value == Vector2.zero) return;
            keysArePressed = value != Vector2.zero;
            UpdateInputDirection(value.VelocityToDirection());
        }
    }


    public void UpdateInputDirection(float? newDirection) {
        if (newDirection == null) inputDirection = null;
        else if (newDirection != inputDirection) {
            angleKeypressChanged = direction;
            inputDirection = newDirection;
        }
    }

    public Vector2 DirectionVector {
        get {
            if (direction is float d) return Quaternion.Euler(0, 0, d) * Vector2.right;
            else return Vector2.zero;
        }
    }

    public Ranged(Config config) {
        this.config = config;
        this.target = new OrientableChild(config.target);
    }

    public void Reset() {
        direction = null;
        config.target.localScale = Vector3.zero;
    }

    private float Interpolate(float currentAngle, float targetAngle) {
        if (angleKeypressChanged == null) return config.aimMinRate; // I don't care
        float startAngle = (float) angleKeypressChanged;
        float rampUp = Mathf.Clamp01(1 + (Mathf.Abs(Mathf.DeltaAngle(startAngle, currentAngle)) - 45) / config.aimTransitionAngle);
        float rampDown = Mathf.Clamp01(Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) / config.aimTransitionAngle);
        float ramp = rampUp * rampDown;
        float gradient = config.aimMaxRate - config.aimMinRate;
        return (config.aimMinRate + ramp * gradient) * Time.deltaTime;
    }

    public void Update() {
        if (!keysArePressed && inputDirection != null) {
            UpdatePointerToKeys();
        }
        if (inputDirection is float input) {
            if (direction is float d) {
                direction = Mathf.MoveTowardsAngle(d, input, Interpolate(d, input));
            } else {
                direction = input;
            }
        }
        target.position3 = 
            (target.rootParent.position + DirectionVector * config.targetRange).WithZ(config.targetZ);
    }

    public void PointerToKeys(Vector2 pointer) {
        if (keysArePressed) return;
        float? inputAngle = pointer.VelocityToDirection();
        if (inputAngle is float realAngle) {
            pointerDirection = realAngle;
            UpdatePointerToKeys();
        } else UpdateInputDirection(null);
    }

    public void UpdatePointerToKeys() {
        inputDirection = null;
        direction = pointerDirection;
        config.target.localScale = Vector3.one;
    }
}
