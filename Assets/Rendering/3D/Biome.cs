using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Biome")]
public class Biome : ScriptableObject {
    public UnityEngine.Material material;
    public TileVariation grass;
    public TileVariation meadow;
    public TileVariation shrub;
    public TileVariation forest;
    public TileVariation water;
    public TileVariation ditch;
    public TileVariation dirtpile;
    public TileVariation woodpile;
    public TileVariation hill;

    private Dictionary<Land, TileVariation> byLand;
    public TileVariation this[Land land] { get => byLand[land]; }

    void OnEnable() {
        byLand = new Dictionary<Land, TileVariation> {
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

        foreach (Land land in byLand.Keys) {
            byLand[land].biome = this;
            byLand[land].thisLayer = land;
        }
    }
}

[Serializable] public class TileVariation {
    [NonSerialized] public Biome biome; 
    public bool alwaysInclude;
    [NonSerialized] public Land thisLayer;
    public Land parentLayer;

    public MeshRenderer corner;
    public MeshRenderer taper;
    public MeshRenderer side;
    public MeshRenderer insideCorner;
    public MeshRenderer inner;

    protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, out bool doRenderLayerBeneath) {
        if (xAdj == thisLayer && yAdj == thisLayer) { // INSIDECORNER or INNER
            doRenderLayerBeneath = false;
            if (cc == thisLayer) {
                hashCode = 0;
                return (transform) => { InstantiateNoRotation(inner, transform); };
            } else {
                hashCode = 1;
                return (transform) => { InstantiateNoRotation(insideCorner, transform); };
            }
        } else if (xAdj != thisLayer && yAdj == thisLayer) { // SIDE
            doRenderLayerBeneath = false;
            hashCode = 2;
            return (transform) => { InstantiateNoRotation(side, transform); };
        } else if (xAdj == thisLayer && yAdj != thisLayer) { // other SIDE
            doRenderLayerBeneath = false;
            hashCode = 3;
            return (transform) => {
                if (side == null) return;
                GameObject go = GameObject.Instantiate(side.gameObject, transform);
                go.transform.localScale = new Vector3(-1, 1, 1);
                go.transform.localEulerAngles = new Vector3(0, 0, 270);
                go.GetComponentStrict<Renderer>().material = biome.material;
            };
        } else { // TAPER or CORNER
            doRenderLayerBeneath = true;
            if (xNear == thisLayer && yNear == thisLayer) { // CORNER
                hashCode = 5;
                return (transform) => { InstantiateNoRotation(corner, transform); };
            } else { // TAPER
                hashCode = 4;
                return (transform) => { InstantiateNoRotation(taper, transform); };
            }
        }
    }

    public Action<Transform> Render(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, bool include = true) {
        Action<Transform> renderLayer =
            RenderThisLayer(xAdj, yAdj, cc, xNear, yNear, out int actualHashCode, out bool doRenderLayerBeneath);
        hashCode = 0;
        Action<Transform> renderLayerBeneath;
        if (parentLayer != thisLayer) {
            renderLayerBeneath = 
                biome[parentLayer].Render(
                    xAdj == thisLayer ? parentLayer : xAdj,
                    yAdj == thisLayer ? parentLayer : yAdj,
                    xNear == thisLayer ? parentLayer : xNear,
                    yNear == thisLayer ? parentLayer : yNear,
                    cc == thisLayer ? parentLayer : cc,
                    out int parentHashCode, 
                    doRenderLayerBeneath);
            hashCode += parentHashCode;
        } else {
            renderLayerBeneath = (transform) => {};
        }

        if (include || alwaysInclude) {
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
