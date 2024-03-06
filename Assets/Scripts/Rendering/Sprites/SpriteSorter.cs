using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SpriteSorter : MonoBehaviour {
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

    public bool BroadGirth {
        get{
            if (Character != null) return Character.broadGirth;
            if (GetComponentInParent<Collectible>() != null) return false;
            throw new InvalidOperationException("SpriteSorter child of neither character nor collectible");
        }
    }
    public bool LegsVisible {
        set => SortingGroups.First<OrientableChild>().gameObject.SetActive(value);
    }
    // Set by boats and CharacterController on terrain
    [SerializeField] private float verticalDisplacement = 0;
    public float VerticalDisplacement {
        get => verticalDisplacement;
        set {
            verticalDisplacement = value;
            ForceUpdate(this.enabled);
        }
    }

    private IEnumerable<OrientableChild> SortingGroups {
        get => orientable.children;
    }
    private IEnumerable<OrientableChild> Sprites {
        get => orientable.GetChildren<SpriteRenderer>();
    }
    public int Height {
        get => orientable.childCount;
    }

    void Awake() {
        orientable = new OrientableChild(transform);
        terrain = GameObject.FindObjectOfType<Terrain>();
        spriteDisplacement =
            (BroadGirth ? broadGirthMeasure : thinGirthMeasure)
            * new ScreenVector(0, -1);
    }

    void Update() {
        if (transform.hasChanged) ForceUpdate(true);
    }

    private void ForceUpdate(bool centerInCell) {
        Vector2 worldSpriteDisplacement = Orientor.WorldFromScreen(spriteDisplacement + verticalDisplacement * new ScreenVector(0, 1));
        Vector2 sortingPosition = centerInCell
            ? terrain.CellCenterAt(orientable.position + worldSpriteDisplacement)
            : orientable.position + worldSpriteDisplacement;
        foreach (OrientableChild sortingGroup in SortingGroups)
            sortingGroup.position = sortingPosition;
        Vector2 ppSpriteDisplacement = Orientor.WorldFromScreen(pixelPerfectCorrection * new ScreenVector(0, -1));
        foreach (OrientableChild sprite in Sprites)
            sprite.position = orientable.position + worldSpriteDisplacement + ppSpriteDisplacement;
        Debug.DrawLine(sortingPosition, orientable.position, Color.magenta);
        Debug.DrawLine(orientable.position + worldSpriteDisplacement, sortingPosition, Color.white);    
    }

    public void Enable() => this.enabled = true;

    public void Disable() {
        ForceUpdate(false);
        this.enabled = false;
    }
    
    // null if collectible
    public Character Character {
        get => MaybeGetCharacterComponent<Character>();
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