
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

static class HandyExtensionsFromJavascript {
    public static bool includes(this Land[] array, Land value) {
        return System.Array.IndexOf(array, value) >= 0;
    }
    public static bool includes(this Construction[] array, Construction value) {
        return System.Array.IndexOf(array, value) >= 0;
    }
}

// Minimally edited copy from javascript
public class TerrainGenerator {
    // C# ONLY SECTION For cross-language compatibility
    class ImageData {
        public readonly Land[] data;
        public readonly int width;
        public readonly int height;
        public ImageData(Land[] data, int width, int height) {
            this.data = data;
            this.width = width;
            this.height = height;
        }
        public static ImageData Create(int width, int height) {
            return new ImageData(new Land[width * height], width, height);
        }
    }
    class IntermediateDebugStep {
        public ImageData createImageData(int width, int height) => ImageData.Create(width, height);
        public void putImageData(ImageData id, int x, int y) { }
    }
    static IntermediateDebugStep getDebugImage(int num) => new IntermediateDebugStep();
    static T[] ARRAY<T>(params T[] elems) => elems;
    static bool RandomChance(float value) => Random.value < value;

    // IMAGE PROCESSING
    static void setPixel(ImageData image, int x, int y, Land land) {
        int rPos = image.width * y + x;
        image.data[rPos] = land;
    }
    static Land getPixel(ImageData image, int x, int y) {
        int rPos = image.width * y + x;
        return image.data[rPos];
    }
    static void copyPixel(ImageData oldImage, int oldX, int oldY, ImageData newImage, int newX, int newY, Land def) {
        if (oldX < 0 || oldY < 0 || oldX >= oldImage.width || oldY >= oldImage.height) {
            setPixel(newImage, newX, newY, def);
            return;
        }
        int oPos = oldImage.width * oldY + oldX;
        int nPos = newImage.width * newY + newX;
        newImage.data[nPos] = oldImage.data[oPos];
    }
    // END C# ONLY SECTION
    // Heavy translation required here

    const int DIM = 128;
    const Land DEPTHS = Land.Water;

    const int INIT_DIM = 2;
    static Land[] INIT_IMG = new Land[] {Land.Water, Land.Grass, Land.Hill, Land.Forest};

    // END Heavy translation required here

    static Action<Land[], Land[]>
    copyArrayToNewArray = (oldArray, newArray) => {
        for (var i = 0; i < oldArray.Length; i++) {
            newArray[i] = oldArray[i];
        }
    };

    static Action<ImageData, ImageData, int>
    magWithNoise = (oldImage, newImage, modPos) => {
        for (var x = 0; x < oldImage.height; x++) { // x is old image x
            for (var y = 0; y < oldImage.width; y++) { // y is old image y
                var mutateX = Random.Range(0, 3);
                var mutateY = Random.Range(0, 2);
                for (var i = 0; i < 2; i++) { // i is sub x in new image
                    for (var j = 0; j < 2; j++) { // j is sub y in new image
                        var useAdjX = Random.Range(0, (1+modPos));
                        var useAdjY = Random.Range(0, (1+modPos));
                        var tiPixel = null as Land?;
                        if (newImage.width >= 16 && 2*x+i <= newImage.width / 4.00f && 2*y+j <= newImage.width / 4.00f) {
                            tiPixel = tutorialIsland(2*x+i, 2*y+j, newImage.width / 4.00f);
                        }
                        if (tiPixel is Land actualTiPixel) {
                            setPixel(newImage, 2*x+i, 2*y+j, actualTiPixel);
                        } else if (mutateX == i && mutateY == j) {
                            if ((nearEdge(2*x+i, 16, newImage.width) || nearEdge(2*y+j, 16, newImage.height))
                                && RandomChance(.3300f)) {
                                setPixel(newImage, 2*x+i, 2*y+j, DEPTHS);
                            } else if (RandomChance(.4900f)) {
                                setPixel(newImage, 2*x+i, 2*y+j, Land.Hill);
                            } else {
                                setPixel(newImage, 2*x+i, 2*y+j, Land.Water);
                            }
                        } else {
                            copyPixel(oldImage, x + useAdjX*(i*2-1), y + useAdjY*(j*2-1), newImage, 2*x+i, 2*y+j, DEPTHS);
                        }
                    }
                }
            }
        }
    };

    static Func<float, float, float, Land?>
    tutorialIsland = (x, y, dim) => {
        if (x >= dim / 8 && x < dim * 7 / 8 && y >= dim / 8 && y < dim * 7 / 8 && Random.Range(0, 3) == 0) {
            return Random.Range(0, 2) == 0 ? Land.Hill : Land.Grass;
        } else if (nearEdge(x, dim, dim) || nearEdge(y, dim, dim)) {
            return Land.Water;
        } else {
            return null;
        }
    };

    static Func<float, float, float, bool>
    nearEdge = (n, factor, dim) => dim >= factor && (n < dim / factor || n >= dim - dim / factor);

    static Func<float, float, float, bool>
    nearCorners = (x, y, dim) => Mathf.Min(x, dim-x-1) + Mathf.Min(y, dim-y-1) < dim/4 - 1;

    // FLOOD FILL
    static Action<ImageData, int, int, Land, Land>
    floodFill = (image, x, y, oldColor, newColor) => {
        var q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(x, y));
        while (q.Count > 0) floodFillStep(image, q, oldColor, newColor);
    };
    static Action<ImageData, Queue<Vector2Int>, Land, Land>
    floodFillStep = (image, q, oldColor, newColor) => {
        var next = q.Dequeue();
        var x = next.x;
        var y = next.y;
        if (x < 0 || y < 0 || x >= image.width || y >= image.height) return;
        var oldPixel = getPixel(image, x, y);
        if (oldPixel == oldColor) {
            setPixel(image, x, y, newColor);
            q.Enqueue(new Vector2Int(x + 1, y));
            q.Enqueue(new Vector2Int(x, y + 1));
            q.Enqueue(new Vector2Int(x - 1, y));
            q.Enqueue(new Vector2Int(x, y - 1));
        }
    };

    // CELLULAR AUTOMATA
    static Func<ImageData, int, int, Land[], int>
    neighborCheck = (image, x, y, from) => {
        if (x < 0 || y < 0 || x >= DIM || y >= DIM) {
            return from.includes(DEPTHS) ? 1 : 0;
        }
        return from.includes(getPixel(image, x, y)) ? 1 : 0;
    };
    static int // ImageData image, int x, int y, params Land[] from
    neighborCount(ImageData image, int x, int y, params Land[] from) {
        var count = 0;
            count += neighborCheck(image, x+1, y, from);
            count += neighborCheck(image, x+1, y+1, from);
            count += neighborCheck(image, x, y+1, from);
            count += neighborCheck(image, x-1, y+1, from);
            count += neighborCheck(image, x-1, y, from);
            count += neighborCheck(image, x-1, y-1, from);
            count += neighborCheck(image, x, y-1, from);
            count += neighborCheck(image, x+1, y-1, from);
        return count;
    }

    static Func<int, int, int>
    bound = (n, dim) => n < 0 ? 0 : n >= dim ? dim-1 : n;

    static int
    RandomSign() => Random.Range(0, 2) * 2 - 1;

    static Func<ImageData, Land[], int, Vector2Int?>
    selectEmptyArea = (image, check, maxTries) => {
        for (var i = 0; i < maxTries; i++) {
            var x = Random.Range(0, DIM);
            var y = Random.Range(0, DIM);
            if (neighborCount(image, x, y, check) < 8) continue;
            return new Vector2Int(x, y);
        }
        return null;
    };

    static ImageData
    GenerateIntArray() {
        var ctx = new IntermediateDebugStep[11];
        for (var i = 0; i < 11; i++) {
            ctx[i] = getDebugImage(i);
        }
        var debugStep = 1;

        var id = ctx[0].createImageData(INIT_DIM, INIT_DIM);
        ImageData idn;

        var fancyArray = id.data;
        copyArrayToNewArray(INIT_IMG, fancyArray);
        //id.data.set(fancyArray);

        var dim = INIT_DIM;

        for ( ; dim < DIM; dim *= 2) {
            idn = ctx[0].createImageData(dim * 2, dim * 2);
            magWithNoise(id, idn, dim * 4 / DIM);
            id = idn;
        }
        Debug.Log(debugStep + " init noise");
        ctx[debugStep++].putImageData(id, 0, 0);

        // unbiased smoothing
        for (var i = 0; i < 16; i++) {
            idn = ctx[0].createImageData(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (neighborCount(id, x, y, Land.Hill) > 4) {
                        setPixel(idn, x, y, Land.Hill);
                    } else if (neighborCount(id, x, y, Land.Water) > 4) {
                        setPixel(idn, x, y, Land.Water);
                    } else if (neighborCount(id, x, y, Land.Forest,Land.Water) > 4) {
                        setPixel(idn, x, y, Land.Forest);
                    } else if (neighborCount(id, x, y, Land.Grass,Land.Water) > 4) {
                        setPixel(idn, x, y, Land.Grass);
                    } else {
                        copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " unbiased smoothing");
        ctx[debugStep++].putImageData(id, 0, 0);

        // add thin line of water between trees and hills
        // smooth grass and hills into water
        // expand hills into grass
        // expand trees into everything else
        // but keep water smooth
        for (var j = 0; j < 3; j++) {
            idn = ctx[0].createImageData(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (neighborCount(id, x, y, Land.Hill) > 0 && neighborCount(id, x, y, Land.Forest) > 0 && neighborCount(id, x, y, Land.Water) > 0) {
                        setPixel(idn, x, y, Land.Water);
                    } else if (neighborCount(id, x, y, Land.Grass) > 3 && getPixel(id, x, y) == Land.Water) {
                        setPixel(idn, x, y, Land.Grass);
                    } else if (getPixel(id, x, y) == Land.Water && neighborCount(id, x, y, Land.Hill) > 3) {
                        setPixel(idn, x, y, Land.Hill);
                    } else if (getPixel(id, x, y) == Land.Grass && neighborCount(id, x, y, Land.Hill) > 2) {
                        setPixel(idn, x, y, Land.Hill);
                    } else if (getPixel(id, x, y) == Land.Water && neighborCount(id, x, y, Land.Forest) > 1) {
                        setPixel(idn, x, y, Land.Forest);
                    } else if (getPixel(id, x, y) != 0 && neighborCount(id, x, y, Land.Forest) > 2) {
                        setPixel(idn, x, y, Land.Forest);
                    } else if (neighborCount(id, x, y, Land.Water) > 5) {
                        setPixel(idn, x, y, Land.Water);
                    } else {
                        copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " misc");
        ctx[debugStep++].putImageData(id, 0, 0);

        // water expand into trees
        // trees expand into grass
        // hills expand into grass (and trees)
        // water smooth into everything
        for (var i = 0; i < 4; i++) {
            idn = ctx[0].createImageData(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (getPixel(id, x, y) == Land.Forest && neighborCount(id, x, y, Land.Water) > i) {
                        setPixel(idn, x, y, Land.Water);
                    } else if (getPixel(id, x, y) == Land.Grass && neighborCount(id, x, y, Land.Forest) > i) {
                        setPixel(idn, x, y, Land.Forest);
                    } else if (ARRAY(Land.Forest,Land.Grass).includes(getPixel(id, x, y)) && neighborCount(id, x, y, Land.Hill) > 2) {
                        setPixel(idn, x, y, Land.Hill);
                    } else if (neighborCount(id, x, y, Land.Water) > 3) {
                        setPixel(idn, x, y, Land.Water);
                    } else {
                        copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                    }
                }
            }
            id = idn;
        }
        // line of grass around trees
        idn = new ImageData(id.data.Clone() as Land[], DIM, DIM);
        for (var x = 0; x < DIM; x++)
            for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == Land.Forest && neighborCount(id, x, y, Land.Water) > 0)
                    setPixel(idn, x, y, Land.Grass);
        id = idn;
        Debug.Log(debugStep + " prune trees");
        ctx[debugStep++].putImageData(id, 0, 0);

        // generate rivers
        for (var x = 0; x < DIM; x++)
            for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == Land.Water)
                    setPixel(id, x, y, Land.Ditch);
#pragma warning disable 0162
        if (DEPTHS != Land.Water) while (true) {
#pragma warning restore 0162
            var x = Random.Range(0, 128);
            var y = Random.Range(128, 256);
            if (getPixel(id, x, y) == Land.Ditch) {
                floodFill(id, x, y, Land.Ditch, Land.Water);
                break;
            }
        }
        Debug.Log(debugStep + " identify ocean");
        ctx[debugStep++].putImageData(id, 0, 0);

        var vel = new Vector2Int(0, RandomSign());
        for (var i = 0; i < 999; i++) {
            idn = new ImageData(id.data.Clone() as Land[], DIM, DIM);
            var possCoord = selectEmptyArea(id, ARRAY(Land.Ditch, Land.Grass, Land.Forest, Land.Hill), 9);
            if (possCoord == null) break;
            var coord = (Vector2Int) possCoord;
            while (neighborCount(idn, coord.x, coord.y, Land.Ditch, Land.Grass, Land.Forest, Land.Hill) == 8) {
                var pos = new Vector2Int(coord.x, coord.y);
                while (neighborCount(idn, pos.x, pos.y, Land.Ditch, Land.Grass, Land.Forest, Land.Hill) == 8) {
                    if (RandomChance(.500f))
                        vel = new Vector2Int(vel.y * RandomSign(), vel.x * RandomSign());
                    pos.x = bound(pos.x + vel.x, DIM);
                    pos.y = bound(pos.y + vel.y, DIM);
                }
                if (getPixel(idn, pos.x, pos.y) == Land.Ditch) {
                    floodFill(idn, pos.x, pos.y, Land.Ditch, Land.Water);
                } else {
                    setPixel(idn, pos.x, pos.y, Land.Water);
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " make rivers");
        ctx[debugStep++].putImageData(id, 0, 0);

        for (var x = 0; x < DIM/2; x++) for (var y = 0; y < DIM/2; y++)
            if (getPixel(id, x, y) == Land.Ditch) floodFill(id, x, y, Land.Ditch, Land.Water);
        for (var x = DIM/2; x < DIM; x++) for (var y = DIM/2; y < DIM; y++)
            if (getPixel(id, x, y) == Land.Ditch) floodFill(id, x, y, Land.Ditch, Land.Water);
        for (var x = 0; x < DIM; x++) for (var y = 0; y < DIM; y++)
            if (getPixel(id, x, y) == Land.Ditch) setPixel(id, x, y, Land.Grass);
        Debug.Log(debugStep + " fill lakes in hills and grass areas");
        ctx[debugStep++].putImageData(id, 0, 0);


        // update hills
        idn = ctx[0].createImageData(DIM, DIM);
        for (var x = 0; x < DIM; x++) {
            for (var y = 0; y < DIM; y++) {
                if (getPixel(id, x, y) == Land.Forest && neighborCount(id, x, y, Land.Forest) == 8) {
                    setPixel(idn, x, y, Land.Hill);
                } else if (getPixel(id, x, y) == Land.Hill && neighborCount(id, x, y, Land.Water) > 0) {
                    setPixel(idn, x, y, Land.Grass);
                } else {
                    copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                }
            }
        }
        id = idn;
        Debug.Log(debugStep + " update hills");
        ctx[debugStep++].putImageData(id, 0, 0);

        // reduce rivers
        for (var i = 0; i < 32; i++) {
            idn = new ImageData(id.data.Clone() as Land[], DIM, DIM);

            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (getPixel(id, x, y) == Land.Water) {
                        if (neighborCount(id, x, y, Land.Grass) == 7) {
                            setPixel(idn, x, y, Land.Grass);
                        } else if (neighborCount(id, x, y, Land.Forest, Land.Grass) == 7) {
                            setPixel(idn, x, y, Land.Forest);
                        } else if (neighborCount(id, x, y, Land.Hill) == 7) {
                            setPixel(idn, x, y, Land.Hill);
                        }
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " reduce rivers");
        ctx[debugStep++].putImageData(id, 0, 0);

        // clean up hills
        for (var i = 0; i < 6; i++) {
            idn = ctx[0].createImageData(DIM, DIM);

            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (getPixel(id, x, y) == Land.Hill && neighborCount(id, x, y, Land.Forest) > (i < 4 ? 1 : 3)) {
                        setPixel(idn, x, y, Land.Forest);
                    } else if (getPixel(id, x, y) == Land.Hill && neighborCount(id, x, y, Land.Grass, Land.Forest) > 3 && i < 3) {
                        setPixel(idn, x, y, Land.Grass);
                    } else if (neighborCount(id, x, y, Land.Hill) >= 6) {
                        setPixel(idn, x, y, Land.Hill);
                    } else {
                        copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " clean up hills");
        ctx[debugStep++].putImageData(id, 0, 0);

        // fix rivers
        idn = new ImageData(id.data.Clone() as Land[], DIM, DIM);
        for (var x = 0; x < DIM - 1; x++) {
            for (var y = 0; y < DIM - 1; y++) {
                if (getPixel(id, x, y) == Land.Water && ARRAY(Land.Grass,Land.Forest).includes(getPixel(id, x+1, y)) &&
                        ARRAY(Land.Grass,Land.Forest).includes(getPixel(id, x, y+1)) &&  getPixel(id, x+1, y+1) == Land.Water) {
                    setPixel(idn, x, y+1, Land.Water);
                    setPixel(idn, x+1, y, Land.Water);
                } else if (ARRAY(Land.Grass,Land.Forest).includes(getPixel(id, x, y)) && getPixel(id, x+1, y) == Land.Water &&
                        getPixel(id, x, y+1) == Land.Water && ARRAY(Land.Grass,Land.Forest).includes(getPixel(id, x+1, y+1))) {
                    setPixel(idn, x, y, Land.Water);
                    setPixel(idn, x+1, y+1, Land.Water);
                }
            }
        }
        id = idn;
        Debug.Log(debugStep + " fix rivers");
        ctx[0].putImageData(id, 0, 0);

        return id;
    }

    // BEGIN C# ONLY SECTION

    public static void GenerateTerrain(Terrain terrain) {
        ImageData intArray = GenerateIntArray();
        for (int x = 0; x < intArray.width; x++) {
            for (int y = 0; y < intArray.height; y++) {
                terrain.Land[x, y] = getPixel(intArray, x, y);
            }
        }
    }

    public static Vector2Int PlaceFountains(Terrain terrain) {
        int subDim = terrain.Bounds.x / 2;
        Vector2Int location;
        Vector2Int startLocation = Vector2Int.zero;
        // water
        for (int x = DIM / 16 - 1; x >= 0; x--) {
            startLocation = new Vector2Int(x, x);
            if (terrain.Land[startLocation] == Land.Hill || terrain.Land[startLocation + new Vector2Int(1, 1)] == Land.Hill) continue;
            terrain.Land[startLocation] = Land.Grass;
            Feature first = terrain.BuildFeature(startLocation, FeatureLibrary.P.fountain);
            first.GetComponentStrict<Fountain>().Team = 1;
            break;
        }
        // grass
        for (int t = 0; t < 999; t++) {
            location = Randoms.Vector2Int(subDim, 0, 2*subDim, subDim);
            if (terrain.Land[location] == Land.Water || terrain.Land[location] == Land.Hill) continue;
            terrain.Land[location] = Land.Grass;
            terrain.BuildFeature(location, FeatureLibrary.P.fountain);
            break;
        }
        // forest
        Vector2Int? lastLoc = null;
        for (int t = 0; t < 999; t++) {
            location = Randoms.Vector2Int(subDim, subDim, 2*subDim, 2*subDim);
            if (lastLoc is Vector2Int lastLocConfirmed) {
                location = Randoms.Midpoint(lastLocConfirmed, location);
                lastLoc = location;
            } else {
                lastLoc = location;
                continue;
            }
            if (terrain.Land[location] == Land.Hill) continue;
            terrain.Land[location] = Land.Grass;
            terrain.BuildFeature(location, FeatureLibrary.P.fountain);
            break;
        }
        // hills
        Vector2Int? lastHillLoc = null;
        for (int t = 0; t < 999; t++) {
            location = Randoms.Vector2Int(0, subDim, subDim, 2*subDim);
            if (terrain.Land[location] == Land.Hill) {
                if (lastHillLoc is Vector2Int lastHillLocConfirmed) {
                    location = Randoms.Midpoint(lastHillLocConfirmed, location);
                    lastHillLoc = location;
                    if (terrain.Land[location] == Land.Water || terrain.Land[location] == Land.Hill) continue;
                } else {
                    lastHillLoc = location;
                    continue;
                }
            }
            if (terrain.Land[location] == Land.Water) continue;
            terrain.Land[location] = Land.Grass;
            terrain.BuildFeature(location, FeatureLibrary.P.fountain);
            break;
        }

        return startLocation;
    }

    public static void FinalDecor(Terrain terrain, Vector2Int startLocation) {
        for (int i = 0; i < 16; i++) {
            for (int t = 0; t < 999; t++) {
                Vector2Int location = Randoms.Vector2Int(0, 0, terrain.Bounds.x, terrain.Bounds.x);
                if (terrain.Land[location] == Land.Water || terrain.Land[location] == Land.Hill
                    || terrain.Feature[location] != null) continue;
                terrain.Land[location] = Land.Grass;
            terrain.BuildFeature(location, FeatureLibrary.P.windmill);
                break;
            }
        }

        bool[] continueInDirection = new bool[] {true, true, true, true};
        Vector2Int farthestShore = Vector2Int.zero;
        int distance = 1;
        for (int shoresEncountered = 0; shoresEncountered < 4; distance++) {
            if (continueInDirection[0] && terrain.Land[startLocation + Vector2Int.right * distance] == Land.Water) {
                continueInDirection[0] = false;
                shoresEncountered++;
                farthestShore = startLocation + Vector2Int.right * distance;
                Debug.Log(farthestShore);
            }
            if (continueInDirection[1] && terrain.Land[startLocation + Vector2Int.up * distance] == Land.Water) {
                continueInDirection[1] = false;
                shoresEncountered++;
                farthestShore = startLocation + Vector2Int.up * distance;
                Debug.Log(farthestShore);
            }
            if (continueInDirection[2] && terrain.Land[startLocation + Vector2Int.left * distance] == Land.Water) {
                continueInDirection[2] = false;
                shoresEncountered++;
                farthestShore = startLocation + Vector2Int.left * distance;
                Debug.Log(farthestShore);
            }
            if (continueInDirection[3] && terrain.Land[startLocation + Vector2Int.down * distance] == Land.Water) {
                continueInDirection[3] = false;
                shoresEncountered++;
                farthestShore = startLocation + Vector2Int.down * distance;
                Debug.Log(farthestShore);
            }
        }
        terrain.BuildFeature(farthestShore, FeatureLibrary.P.boat);
    }
}