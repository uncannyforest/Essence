using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SpriteSorter : MonoBehaviour {
    public bool broadGirth;

    private Terrain terrain;
    private ScreenVector spriteDisplacement;
    private OrientableChild orientable;

    private const float broadGirthMeasure = 1/4f; // remember, height of tile (8 pixels)
    private const float thinGirthMeasure = 1/8f;  //     is one unit
    // This is exactly a 1/4 of a pixel (from testing).  I'm guessing my transform-doubling
    // interferes with PixelPerfect's rounding, otherwise it would be half a pixel.
    // But we want flooring, not rounding.  We want the bottom pixel of the sprite to always
    // *overlap* the object's position - rather than going above or below it depending on which
    // is closer (which would be a half-pixel difference) or the weird 1/4 adjustment PP is doing.
    // Whew!  This was a nightmare to debug.
    private const float pixelPerfectCorrection = 1/32f;

    private IEnumerable<OrientableChild> SortingGroups {
        get => orientable.children;
    }
    private IEnumerable<OrientableChild> Sprites {
        get => orientable.GetChildren<SpriteRenderer>();
    }
    public int Height {
        get => orientable.childCount;
    }

    void Start() {
        orientable = new OrientableChild(transform);
        terrain = GameObject.FindObjectOfType<Terrain>();
        spriteDisplacement =
            (broadGirth ? broadGirthMeasure : thinGirthMeasure)
            * new ScreenVector(0, -1);
    }

    void Update() {
        if (transform.hasChanged) {
            Vector2 worldSpriteDisplacement = Orientor.WorldFromScreen(spriteDisplacement);
            Vector2 sortingPosition = terrain.CellCenterAt(orientable.position + worldSpriteDisplacement);
            foreach (OrientableChild sortingGroup in SortingGroups)
                sortingGroup.position = sortingPosition;
            Vector2 ppSpriteDisplacement = Orientor.WorldFromScreen(pixelPerfectCorrection  * new ScreenVector(0, -1));
            foreach (OrientableChild sprite in Sprites)
                sprite.position = orientable.position + worldSpriteDisplacement + ppSpriteDisplacement;
            Debug.DrawLine(sortingPosition, orientable.position, Color.magenta);
            Debug.DrawLine(orientable.position + worldSpriteDisplacement, sortingPosition, Color.white);
        }
    }

    public void Enable() => this.enabled = true;

    public void Disable() {
        Vector2 worldSpriteDisplacement = Orientor.WorldFromScreen(spriteDisplacement);
        foreach (OrientableChild sortingGroup in SortingGroups)
            sortingGroup.position = orientable.position + worldSpriteDisplacement;
        Vector2 ppSpriteDisplacement = Orientor.WorldFromScreen(pixelPerfectCorrection  * new ScreenVector(0, -1));
        foreach (OrientableChild sprite in Sprites)
            sprite.position = orientable.position + worldSpriteDisplacement + ppSpriteDisplacement;
        this.enabled = false;
    }

    public SortingGroup AddGroup(SortingGroup prefab, float z) {
        SortingGroup result = GameObject.Instantiate(prefab, transform);
        result.transform.localPosition = new Vector3(0, 0, z);
        return result;
    }

    public Transform Character {
        get => orientable.rootParent.transform;
    }
    public T MaybeGetCharacterComponent<T>() where T : Component =>
        orientable.rootParent.GetComponent<T>();
    public T GetCharacterComponent<T>() where T : Component =>
        orientable.rootParent.GetComponentStrict<T>();

    new public T GetComponent<T>() where T : Component =>
        throw new InvalidOperationException("You probably didn't want to call GetComponent on a SpriteSorter.  Try GetCharacterComponent?");
    public T GetComponentStrict<T>() where T : Component =>
        throw new InvalidOperationException("You probably didn't want to call GetComponent on a SpriteSorter.  Try GetCharacterComponent?");
}