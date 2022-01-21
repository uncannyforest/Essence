using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Damage))]
public class Arrow : MonoBehaviour {
    public float speed;
    public float reach;
    public Collectible itemPrefab;

    new private Rigidbody2D rigidbody;
    private Transform sprite;

    public static Arrow Instantiate(Arrow arrowPrefab, Transform parent, Transform source, Vector2 to) {
        Arrow arrow = Instantiate<Arrow>(arrowPrefab, source.position, Quaternion.identity, parent);
        arrow.Fire(to - (Vector2)source.position);
        arrow.GetComponent<Damage>().Source = source.GetComponentStrict<Team>();
        return arrow;
    }

    public void Fire(Vector2 direction) {
        rigidbody = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>().transform;
        rigidbody.velocity = direction.normalized * speed;
        sprite.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, direction));
        Invoke("Land", reach / speed);
    }

    private void Land() {
        Collectible.Instantiate(itemPrefab, GameObject.FindObjectOfType<Terrain>().transform, transform.position, 1);
        Destroy(gameObject);
    }
}
