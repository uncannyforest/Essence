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

    public static Arrow Instantiate(Arrow arrowPrefab, Transform parent, Transform source, Vector2 to, int damage) {
        Arrow arrow = Instantiate<Arrow>(arrowPrefab, source.position, Quaternion.identity, parent);
        Displacement direction = Disp.FT(source.position, to);
        arrow.Fire(direction);
        Damage damageComponent = arrow.GetComponent<Damage>();
        damageComponent.Source = source.GetComponentStrict<Team>();
        damageComponent.power = damage;
        return arrow;
    }

    public void Fire(Displacement direction) {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = direction.ToVelocity(speed);
        transform.rotation = Quaternion.Euler(0, 0, direction.angle);
        this.Invoke(Land, reach / speed);
    }

    private void Land() {
        Collectible.Instantiate(itemPrefab, GameObject.FindObjectOfType<Terrain>().transform, transform.position, 1);
        Destroy(gameObject);
    }
}
