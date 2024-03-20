using System;
using System.Collections.Generic;
using UnityEngine;

public class TileLibrary3D : MonoBehaviour {
    private static TileLibrary3D instance;
    public static TileLibrary3D T {
        get => instance;
    }

    public NoVariation grass;
    public TaperVariation meadow;
    public CornerVariation shrub;
    public InnerVariation forest;
    public NoVariation water;
    public TaperVariation ditch;
    public DirectionVariation dirtpile;
    public NoVariation woodpile;
    public InnerVariation hill;
    public NoVariation fence;
    public DirectionVariation woodBldg;

    public Dictionary<Land, TileVariation> byLand;
    public TileVariation this[Land land] { get => byLand[land]; }

    void Awake() {
        if (instance == null) instance = this;
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
    }
}

public abstract class TileVariation {
    public bool alwaysInclude;
    public Land thisLayer;
    public Land parentLayer;

    abstract protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
        out int hashCode, out bool doRenderLayerBeneath);

    public Action<Transform> Render(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, bool include = true) {
        Action<Transform> renderLayer =
            RenderThisLayer(xAdj, yAdj, cc, xNear, yNear, out int actualHashCode, out bool doRenderLayerBeneath);
        hashCode = 0;
        Action<Transform> renderLayerBeneath;
        if (parentLayer != thisLayer) {
            Debug.Log("xAdj " + xAdj);
            Debug.Log("new xAdj " + (xAdj == thisLayer ? parentLayer : xAdj));
            renderLayerBeneath = 
                TileLibrary3D.T[parentLayer].Render(
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
            Debug.Log("included " + thisLayer);
            hashCode += actualHashCode;
            return (transform) => {
                renderLayerBeneath(transform);
                renderLayer(transform);
            };
        } else {
            return renderLayerBeneath;
        }
    }

    protected static void InstantiateNoRotation(Component component, Transform parent) =>
        GameObject.Instantiate(component.gameObject, parent).transform.localRotation = Quaternion.identity;
}

[Serializable] public class NoVariation : TileVariation {
    public MeshRenderer basic;

    override protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, out bool doRenderLayerBeneath) {
        hashCode = 0;
        doRenderLayerBeneath = false;
        return (transform) => { InstantiateNoRotation(basic, transform); };
    }
}

[Serializable] public class CornerVariation : TileVariation {
    public MeshRenderer corner;
    public MeshRenderer basic;

    override protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, out bool doRenderLayerBeneath) {
        if (xAdj != thisLayer && yAdj != thisLayer && xNear == thisLayer && yNear == thisLayer) {
            hashCode = 1;
            doRenderLayerBeneath = true;
            return (transform) => { if (corner != null) InstantiateNoRotation(corner, transform); };
        } else {
            hashCode = 0;
            doRenderLayerBeneath = false;
            return (transform) => { InstantiateNoRotation(basic, transform); };
        }
    }
}

[Serializable] public class TaperVariation : TileVariation {
    public MeshRenderer taper;
    public MeshRenderer basic;

    override protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, out bool doRenderLayerBeneath) {
        if (xAdj != thisLayer && yAdj != thisLayer) {
            hashCode = 1;
            doRenderLayerBeneath = true;
            return (transform) => { if (taper != null) InstantiateNoRotation(taper, transform); };
        } else {
            hashCode = 0;
            doRenderLayerBeneath = false;
            return (transform) => { InstantiateNoRotation(basic, transform); };
        }
    }
}

[Serializable] public class InnerVariation : TileVariation {
    public MeshRenderer basic;
    public MeshRenderer inner;

    override protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, out bool doRenderLayerBeneath) {
        if (xAdj == thisLayer && yAdj == thisLayer && cc == thisLayer) {
            hashCode = 0;
            doRenderLayerBeneath = false;
            return (transform) => { InstantiateNoRotation(inner, transform); };
        } else {
            hashCode = 1;
            doRenderLayerBeneath = true;
            return (transform) => { InstantiateNoRotation(basic, transform); };
        }
    }
}

[Serializable] public class DirectionVariation : TileVariation {
    public MeshRenderer taper;
    public MeshRenderer side;
    public MeshRenderer insideCorner;

    override protected Action<Transform> RenderThisLayer(Land xAdj, Land yAdj, Land cc, Land xNear, Land yNear,
            out int hashCode, out bool doRenderLayerBeneath) {
        if (xAdj == thisLayer && yAdj == thisLayer) {
            hashCode = 0;
            doRenderLayerBeneath = false;
            return (transform) => { InstantiateNoRotation(insideCorner, transform); };
        } else if (xAdj != thisLayer && yAdj == thisLayer) {
            hashCode = 1;
            doRenderLayerBeneath = false;
            return (transform) => { InstantiateNoRotation(side, transform); };
        } else if (xAdj == thisLayer && yAdj != thisLayer) {
            hashCode = 2;
            doRenderLayerBeneath = false;
            return (transform) => {
                GameObject go = GameObject.Instantiate(side.gameObject, transform);
                go.transform.localScale = new Vector3(-1, 1, 1);
                go.transform.localEulerAngles = new Vector3(0, 90, 0);
            };
        } else {
            hashCode = 3;
            doRenderLayerBeneath = true;
            return (transform) => { InstantiateNoRotation(taper, transform); };
        }
    }
}