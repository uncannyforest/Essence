using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Biome")]
public class Biome : ScriptableObject {
    public UnityEngine.Material material;
    public TileVariation grass = new TileVariation(TileMaterial.Level.Land);
    public TileVariation meadow = new TileVariation(TileMaterial.Level.Land);
    public TileVariation shrub = new TileVariation(TileMaterial.Level.Land);
    public TileVariation forest = new TileVariation(TileMaterial.Level.Land);
    public TileVariation water = new TileVariation(TileMaterial.Level.Land);
    public TileVariation ditch = new TileVariation(TileMaterial.Level.Land);
    public TileVariation dirtpile = new TileVariation(TileMaterial.Level.Land);
    public TileVariation woodpile = new TileVariation(TileMaterial.Level.Land);
    public TileVariation hill = new TileVariation(TileMaterial.Level.Land);

    private Dictionary<TileMaterial, TileVariation> byLand;
    public TileVariation this[TileMaterial land] { get => byLand[land]; }

    void OnEnable() {
        byLand = new Dictionary<TileMaterial, TileVariation> {
            [Land.Grass] = grass,
            [Land.Meadow] = meadow,
            [Land.Shrub] = shrub,
            [Land.Forest] = forest,
            [Land.Water] = water,
            [Land.Ditch] = ditch,
            [Land.Dirtpile] = dirtpile,
            [Land.Woodpile] = woodpile,
            [Land.Hill] = hill,
        };

        foreach (TileMaterial land in byLand.Keys) {
            byLand[land].biome = this;
            byLand[land].thisLayer = land;
        }
    }
}

[Serializable] public class TileVariation {
    [NonSerialized] public Biome biome; 
    public bool alwaysInclude;
    [NonSerialized] public TileMaterial thisLayer;
    public TileMaterial parentLayer;

    public MeshRenderer corner;
    public MeshRenderer taper;
    public MeshRenderer side;
    public MeshRenderer insideCorner;
    public MeshRenderer inner;

    public TileVariation(TileMaterial.Level level) {
        this.parentLayer = new TileMaterial(level);
    }

    // has no parent (signified by setting parent to self) e.g. Grass, Ditch
    public bool IsAdam { get => parentLayer == thisLayer; }

    // Determines orientation, then stores commands to instantiate graphics object children
    // xAdh, yAdh, cc, xNear, yNear: nearby lands to determine orientation. May be simplified using OriginalOrAdam()
    // hashCode: unique int for a given set of graphics object children, different code means rerender
    // exposedSides: how many of the 2 adjacent lands are different
    protected Action<Transform> RenderThisLayer(TileMaterial xAdj, TileMaterial yAdj, TileMaterial cc, TileMaterial xNear, TileMaterial yNear,
            out int hashCode, out int exposedSides) {
        if (xAdj == thisLayer && yAdj == thisLayer) { // INSIDECORNER or INNER
            exposedSides = 0;
            if (cc == thisLayer) {
                hashCode = 0;
                return (transform) => { InstantiateNoRotation(inner, transform); };
            } else {
                hashCode = 1;
                return (transform) => { InstantiateNoRotation(insideCorner, transform); };
            }
        } else if (xAdj == thisLayer && yAdj != thisLayer) { // SIDE
            exposedSides = 1;
            hashCode = 2;
            return (transform) => { InstantiateNoRotation(side, transform); };
        } else if (xAdj != thisLayer && yAdj == thisLayer) { // other SIDE
            exposedSides = 1;
            hashCode = 3;
            return (transform) => {
                if (side == null) return;
                GameObject go = GameObject.Instantiate(side.gameObject, transform);
                go.transform.localScale = new Vector3(-1, 1, 1);
                go.transform.localEulerAngles = new Vector3(0, 0, 270);
                go.GetComponentStrict<Renderer>().material = biome.material;
            };
        } else { // TAPER or CORNER - all three nearby corners are different land
            exposedSides = 2;
            if (xNear == thisLayer && yNear == thisLayer) { // CORNER
                hashCode = 5;
                return (transform) => { InstantiateNoRotation(corner, transform); };
            } else { // TAPER
                hashCode = 4;
                return (transform) => { InstantiateNoRotation(taper, transform); };
            }
        }
    }

    // returns THIS LAND if it is an ancestor, otherwise ADAM e.g. Grass
    // For example, if THIS LAND is Shrub:
    // ADJ is Forest -> return Shrub; ADJ is Meadow -> return Grass
    private TileMaterial OriginalOrAdam(TileMaterial adj) {
        int infiniteLoopCatch = 100;
        while (adj != thisLayer && !biome[adj].IsAdam && infiniteLoopCatch --> 0) {
            adj = biome[adj].parentLayer;
        }
        if (infiniteLoopCatch == 0)
            Debug.LogError("Infinite loop in ThisOrAdam");
        return adj;
    }

    // Returns commands to render layer and all layers beneath it (parents)
    // xAdh, yAdh, cc, xNear, yNear: nearby lands to determine orientation
    // hashCode: unique int for a given set of graphics object children, different code means rerender
    // potentialExposedSides: max possible exposedSides returned from RenderThisLayer, which we know in advance
    //   - if RenderThisLayer returns this after all, we may ignore this intermediate layer (see next comment)
    // isOrig: whether this is the original child layer that triggered the render
    public Action<Transform> Render(TileMaterial xAdj, TileMaterial yAdj, TileMaterial cc, TileMaterial xNear, TileMaterial yNear,
            out int hashCode, int potentialExposedSides = 2, bool isOrig = true) {
        Action<Transform> renderLayer =
            RenderThisLayer(OriginalOrAdam(xAdj), OriginalOrAdam(yAdj), OriginalOrAdam(cc),
                OriginalOrAdam(xNear), OriginalOrAdam(yNear),
                out int actualHashCode, out int exposedSides);
        hashCode = 0;
        Action<Transform> renderLayerBeneath;
        if (parentLayer != thisLayer) {
            renderLayerBeneath = 
                biome[parentLayer].Render(xAdj, yAdj, cc, xNear, yNear,
                    out int parentHashCode, exposedSides, false);
            hashCode += parentHashCode * 10;
        } else {
            renderLayerBeneath = (transform) => {};
        }

        // Skip rendering this layer unless - 
        // alwaysInclude - critical layer e.g. Grass
        // isOrig - topmost child that triggered render
        // IsAdam unless exposedSides == 0 - bottommost layer with exposed sides e.g. Ditch in Water on shore edge
        // exposedSides < potentialExposedSides - this is an intermediate layer that needs a transition render
        //   e.g. the Meadow layer of Forest when adjacent lands are Grass and Meadow, we render this Meadow
        //   (even though we don't normally render Meadow under Forest)
        if (alwaysInclude || isOrig || IsAdam && exposedSides >= 1 || exposedSides < potentialExposedSides) {
            hashCode += actualHashCode;
            return (transform) => {
                renderLayerBeneath(transform);
                renderLayer(transform);
            };
        } else {
            return renderLayerBeneath;
        }
    }

    protected void InstantiateNoRotation(MeshRenderer renderer, Transform parent) {
        if (renderer == null) return;
        GameObject go = GameObject.Instantiate(renderer.gameObject, parent);
        go.transform.localRotation = Quaternion.identity;
        go.GetComponentStrict<Renderer>().material = biome.material;
    }
}
