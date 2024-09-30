using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orientor3D : MonoBehaviour {
    private static Orientor3D instance;
    public static Orientor3D I { get => instance; }
    void Awake() { if (instance == null) instance = this; }

    public Transform cardboardDirection;
    public float turnSpeed = 3;

    private float aimingForIso = 0;
    private bool rotKeyIsDown = false;

    private static float AngleClamp(float angle) {
        while (angle < -179) {
            angle += 360;
        }
        while (angle > 181) {
            angle -= 360;
        }
        return angle;
    }

    public static Vector2 WorldFromScreen(ScreenVector input) {
        return I.transform.localRotation * new Vector2(input.x, input.y);
    }

    // called on Update (inside InputManager)
    public void RotationKeyUpdate(int direction) {
        if (direction != 0) {
            if (!rotKeyIsDown) { // initial press
                aimingForIso += direction * 90;
                aimingForIso = AngleClamp(aimingForIso);
                rotKeyIsDown = true;
            }
        } else if (rotKeyIsDown) {
            rotKeyIsDown = false;
        }

        float currentAngle = transform.localRotation.eulerAngles.z - 45;
        currentAngle = AngleClamp(currentAngle);

        if (Mathf.Abs(aimingForIso - currentAngle) > .00001f) {
            float correction = aimingForIso - currentAngle;
            correction = AngleClamp(correction);

            currentAngle += (correction > 0) ? turnSpeed : -turnSpeed;

            float passCheck = aimingForIso - currentAngle;
            passCheck = AngleClamp(passCheck);
            if (correction * passCheck <= 0) {
                if (direction == 0) {
                    currentAngle = aimingForIso;
                } else {
                    aimingForIso += direction * 90;
                    aimingForIso = AngleClamp(aimingForIso);
                }
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle + 45);
            Cardboard.OrientAllCardboards(cardboardDirection);
        }

    }
}
