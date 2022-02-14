
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class HandyExtensionsFromJavascript {
    public static bool includes(this int[] array, int value) {
        return System.Array.IndexOf(array, value) >= 0;
    }
}

// Minimally edited copy from javascript
public class TerrainGenerator {
    class ImageData {
        public readonly int[] data;
        public readonly int width;
        public readonly int height;
        public ImageData(int[] data, int width, int height) {
            this.data = data;
            this.width = width;
            this.height = height;
        }
        public static ImageData Create(int width, int height) {
            return new ImageData(new int[width * height], width, height);
        }
    }

    const int DIM = 128;
    int[] DEPTHS = {0, 64, 192};

    // MAGNIFICATION
    const int INIT_DIM = 2;
    int[] INIT_IMG = {64, 255, 0, 128};
    void copyArrayToNewArray(int[] oldArray, int[] newArray) {
        for (var i = 0; i < oldArray.Length; i++) {
            newArray[i] = oldArray[i];
        }
    }
    void magWithNoise(ImageData oldImage, ImageData newImage, int modPos) {
        for (var x = 0; x < oldImage.height; x++) { // x is old image x
            for (var y = 0; y < oldImage.width; y++) { // y is old image y
                var mutateX = Random.Range(0, 3);
                var mutateY = Random.Range(0, 2);
                for (var i = 0; i < 2; i++) { // i is sub x in new image
                    for (var j = 0; j < 2; j++) { // j is sub y in new image
                        var useAdjX = Mathf.FloorToInt(Random.value * (1+modPos));
                        var useAdjY = Mathf.FloorToInt(Random.value * (1+modPos));
                        if (mutateX == i && mutateY == j) {
                            if ((nearEdge(2*x+i, 16, newImage.width) || nearEdge(2*y+j, 16, newImage.height))
                                && Random.value < .33f) {
                                setPixel(newImage, 2*x+i, 2*y+j, DEPTHS[0], DEPTHS[1], DEPTHS[2]);
                            } else if (Random.value < .49f) {
                                setPixel(newImage, 2*x+i, 2*y+j, 192, 0, 0);
                            } else {
                                setPixel(newImage, 2*x+i, 2*y+j, 0, 64, 192);
                            }
                        } else {
                            copyPixel(oldImage, x + useAdjX*(i*2-1), y + useAdjY*(j*2-1), newImage, 2*x+i, 2*y+j, DEPTHS[1]);
                        }
                    }
                }
            }
        }
    }
    bool nearEdge(int n, int factor, int dim) => dim >= factor && (n < dim / factor || n >= dim - dim / factor);
    bool nearCorners(int x, int y, int dim) => Mathf.Min(x, dim-x-1) + Mathf.Min(y, dim-y-1) < dim/4 - 1;

    // IMAGE PROCESSING
    void setPixel(ImageData image, int x, int y, int r, int g, int b) {
        var rPos = image.width * y + x;
        image.data[rPos] = g;
    }
    static int getPixel(ImageData image, int x, int y) {
        var rPos = image.width * y + x;
        return image.data[rPos];
    }
    void copyPixel(ImageData oldImage, int oldX, int oldY, ImageData newImage, int newX, int newY, int def) {
        if (oldX < 0 || oldY < 0 || oldX >= oldImage.width || oldY >= oldImage.height) {
            setPixel(newImage, newX, newY, 0, def, 0);
            return;
        }
        var oPos = oldImage.width * oldY + oldX;
        var nPos = newImage.width * newY + newX;
        newImage.data[nPos] = oldImage.data[oPos];
    }

    // FLOOD FILL
    void floodFill(ImageData image, int x, int y, int oldColor, int newColor) {
        var q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(x, y));
        while (q.Count > 0) floodFillStep(image, q, oldColor, newColor);
    }
    void floodFillStep(ImageData image, Queue<Vector2Int> q, int oldColor, int newColor) {
        var next = q.Dequeue();
        var x = next.x;
        var y = next.y;
        if (x < 0 || y < 0 || x >= image.width || y >= image.height) return;
        var oldPixel = getPixel(image, x, y);
        if (oldPixel == oldColor) {
            setPixel(image, x, y, 0, newColor, 0);
            q.Enqueue(new Vector2Int(x + 1, y));
            q.Enqueue(new Vector2Int(x, y + 1));
            q.Enqueue(new Vector2Int(x - 1, y));
            q.Enqueue(new Vector2Int(x, y - 1));
        }
    }

    // CELLULAR AUTOMATA
    int neighborCheck(ImageData image, int x, int y, int[] from) {
        if (x < 0 || y < 0 || x >= DIM || y >= DIM) {
            return from.includes(DEPTHS[1]) ? 1 : 0;
        }
        return from.includes(getPixel(image, x, y)) ? 1 : 0;
    }
    int neighborCount(ImageData image, int x, int y, int[] from) {
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
    int neighborCount(ImageData image, int x, int y, int from) {
        return neighborCount(image, x, y, new int[] {from});
    }

    int bound(int n, int dim) => n < 0 ? 0 : n >= dim ? dim-1 : n;

    int randomSign() => Random.Range(0, 2) * 2 - 1;

    Vector2Int? selectEmptyArea(ImageData image, int[] check, int maxTries) {
        for (var i = 0; i < maxTries; i++) {
            var x = Random.Range(0, DIM);
            var y = Random.Range(0, DIM);
            if (neighborCount(image, x, y, check) < 8) continue;
            return new Vector2Int(x, y);
        }
        return null;
    }

    ImageData GenerateIntArray() {
        var id = ImageData.Create(INIT_DIM, INIT_DIM);
        ImageData idn;

        copyArrayToNewArray(INIT_IMG, id.data);

        var dim = INIT_DIM;

        for ( ; dim < DIM; dim *= 2) {
            idn = ImageData.Create(dim * 2, dim * 2);
            magWithNoise(id, idn, dim * 4 / DIM);
            id = idn;
        }
        Debug.Log("1");

        // unbiased smoothing
        for (var i = 0; i < 16; i++) {
            idn = ImageData.Create(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (neighborCount(id, x, y, 0) > 4) {
                        setPixel(idn, x, y, 192, 0, 0);
                    } else if (neighborCount(id, x, y, 64) > 4) {
                        setPixel(idn, x, y, 0, 64, 192);
                    } else if (neighborCount(id, x, y, new int[] {128,0}) > 4) {
                        setPixel(idn, x, y, 0, 128, 0);
                    } else if (neighborCount(id, x, y, new int[] {255,0}) > 4) {
                        setPixel(idn, x, y, 0, 255, 0);
                    } else {
                        copyPixel(id, x, y, idn, x, y, -1);
                    }
                }
            }
            id = idn;
        }
        Debug.Log("2");

        // add thin line of water between trees and hills
        // smooth grass and hills into water
        // expand hills into grass
        // expand trees into everything else
        // but keep water smooth
        for (var j = 0; j < 3; j++) {
            idn = ImageData.Create(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (neighborCount(id, x, y, 0) > 0 && neighborCount(id, x, y, 128) > 0 && neighborCount(id, x, y, 64) > 0) {
                        setPixel(idn, x, y, 0, 64, 192);
                    } else if (neighborCount(id, x, y, 255) > 3 && getPixel(id, x, y) == 64) {
                        setPixel(idn, x, y, 0, 255, 0);
                    } else if (getPixel(id, x, y) == 64 && neighborCount(id, x, y, 0) > 3) {
                        setPixel(idn, x, y, 192, 0, 0);
                    } else if (getPixel(id, x, y) == 255 && neighborCount(id, x, y, 0) > 2) {
                        setPixel(idn, x, y, 192, 0, 0);
                    } else if (getPixel(id, x, y) == 64 && neighborCount(id, x, y, 128) > 1) {
                        setPixel(idn, x, y, 0, 128, 0);
                    } else if (getPixel(id, x, y) != 0 && neighborCount(id, x, y, 128) > 2) {
                        setPixel(idn, x, y, 0, 128, 0);
                    } else if (neighborCount(id, x, y, 64) > 5) {
                        setPixel(idn, x, y, 0, 64, 192);
                    } else {
                        copyPixel(id, x, y, idn, x, y, -1);
                    }
                }
            }
            id = idn;
        }
        Debug.Log("3");

        // water expand into trees
        // trees expand into grass
        // hills expand into grass (and trees)
        // water smooth into everything
        for (var i = 0; i < 4; i++) {
            idn = ImageData.Create(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (getPixel(id, x, y) == 128 && neighborCount(id, x, y, 64) > i) {
                        setPixel(idn, x, y, 0, 64, 192);
                    } else if (getPixel(id, x, y) == 255 && neighborCount(id, x, y, 128) > i) {
                        setPixel(idn, x, y, 0, 128, 0);
                    } else if (new int[] {128,255}.includes(getPixel(id, x, y)) && neighborCount(id, x, y, 0) > 2) {
                        setPixel(idn, x, y, 192, 0, 0);
                    } else if (neighborCount(id, x, y, 64) > 3) {
                        setPixel(idn, x, y, 0, 64, 192);
                    } else {
                        copyPixel(id, x, y, idn, x, y, -1);
                    }
                }
            }
            id = idn;
        }
        Debug.Log("4a");
        // line of grass around trees
        idn = new ImageData((int[]) id.data.Clone(), DIM, DIM);
        for (var x = 0; x < DIM; x++)
            for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == 128 && neighborCount(id, x, y, 64) > 0)
                    setPixel(idn, x, y, 0, 255, 0);
        id = idn;
        Debug.Log("4");

        // generate rivers
        for (var x = 0; x < DIM; x++)
            for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == 64)
                    setPixel(id, x, y, 96, 96, 96);
        Debug.Log("5a");
        if (DEPTHS[1] != 64) while (true) {
            var x = Random.Range(0, 128);
            var y = Random.Range(128, 256);
            if (getPixel(id, x, y) == 96) {
                Debug.Log("5b");
                floodFill(id, x, y, 96, 64);
                break;
            }
        }
        Debug.Log("5");
        var vel = new Vector2Int(0, randomSign());
        for (var i = 0; i < 1000; i++) {
            idn = new ImageData((int[]) id.data.Clone(), DIM, DIM);
            var possCoord = selectEmptyArea(id, new int[] {96, 255, 128, 0}, 9);
            if (possCoord == null) break;
            var coord = (Vector2Int) possCoord;
            while (neighborCount(idn, coord[0], coord[1], new int[] {96, 255, 128, 0}) == 8) {
                var pos = new Vector2Int(coord[0], coord[1]);
                while (neighborCount(idn, pos[0], pos[1], new int[] {96, 255, 128, 0}) == 8) {
                    if (Random.value < .5)
                    vel = new Vector2Int(vel[1] * randomSign(), vel[0] * randomSign());
                    pos[0] = bound(pos[0] + vel[0], DIM);
                    pos[1] = bound(pos[1] + vel[1], DIM);
                }
                if (getPixel(idn, pos[0], pos[1]) == 96) {
                    floodFill(idn, pos[0], pos[1], 96, 64);
                } else {
                    setPixel(idn, pos[0], pos[1], 0, 64, 192);
                }
            }
            id = idn;
        }
        Debug.Log("6b");
        for (var x = 0; x < DIM/2; x++) for (var y = 0; y < DIM/2; y++)
            if (getPixel(id, x, y) == 96) floodFill(id, x, y, 96, 64);
        for (var x = DIM/2; x < DIM; x++) for (var y = DIM/2; y < DIM; y++)
            if (getPixel(id, x, y) == 96) floodFill(id, x, y, 96, 64);
        for (var x = 0; x < DIM; x++) for (var y = 0; y < DIM; y++)
            if (getPixel(id, x, y) == 96) setPixel(id, x, y, 0, 255, 0);
        Debug.Log("6");


        // update hills
        idn = ImageData.Create(DIM, DIM);
        for (var x = 0; x < DIM; x++) {
            for (var y = 0; y < DIM; y++) {
                if (getPixel(id, x, y) == 128 && neighborCount(id, x, y, 128) == 8) {
                    setPixel(idn, x, y, 192, 0, 0);
                } else if (getPixel(id, x, y) == 0 && neighborCount(id, x, y, 64) > 0) {
                    setPixel(idn, x, y, 0, 255, 0);
                } else {
                    copyPixel(id, x, y, idn, x, y, -1);
                }
            }
        }
        id = idn;
        Debug.Log("7");

        // reduce rivers
        for (var i = 0; i < 32; i++) {
            idn = new ImageData((int[]) id.data.Clone(), DIM, DIM);

            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (getPixel(id, x, y) == 64) {
                        if (neighborCount(id, x, y, 255) == 7) {
                            setPixel(idn, x, y, 0, 255, 0);
                        } else if (neighborCount(id, x, y, new int[] {128, 255}) == 7) {
                            setPixel(idn, x, y, 0, 128, 0);
                        } else if (neighborCount(id, x, y, 0) == 7) {
                            setPixel(idn, x, y, 192, 0, 0);
                        }
                    }
                }
            }
            id = idn;
        }
        Debug.Log("8");

        // clean up hills
        for (var i = 0; i < 6; i++) {
            idn = ImageData.Create(DIM, DIM);

            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (getPixel(id, x, y) == 0 && neighborCount(id, x, y, 128) > (i < 4 ? 1 : 3)) {
                        setPixel(idn, x, y, 0, 128, 0);
                    } else if (getPixel(id, x, y) == 0 && neighborCount(id, x, y, new int[] {255, 128}) > 3 && i < 3) {
                        setPixel(idn, x, y, 0, 255, 0);
                    } else if (neighborCount(id, x, y, 0) >= 6) {
                        setPixel(idn, x, y, 192, 0, 0);
                    } else {
                        copyPixel(id, x, y, idn, x, y, -1);
                    }
                }
            }
            id = idn;
        }
        Debug.Log("9");

        // fix rivers
        idn = new ImageData((int[])id.data.Clone(), DIM, DIM);
        for (var x = 0; x < DIM - 1; x++) {
            for (var y = 0; y < DIM - 1; y++) {
                if (getPixel(id, x, y) == 64 && new int[] {255,128}.includes(getPixel(id, x+1, y)) &&
                        new int[] {255,128}.includes(getPixel(id, x, y+1)) &&  getPixel(id, x+1, y+1) == 64) {
                    setPixel(idn, x, y+1, 0, 64, 192);
                    setPixel(idn, x+1, y, 0, 64, 192);
                } else if (new int[] {255,128}.includes(getPixel(id, x, y)) && getPixel(id, x+1, y) == 64 &&
                        getPixel(id, x, y+1) == 64 &&  new int[] {255,128}.includes(getPixel(id, x+1, y+1))) {
                    setPixel(idn, x, y, 0, 64, 192);
                    setPixel(idn, x+1, y+1, 0, 64, 192);
                }
            }
        }
        id = idn;

        return id;
    }

    public static void GenerateTerrain(Terrain terrain) {
        ImageData intArray = new TerrainGenerator().GenerateIntArray();
        for (int x = 0; x < intArray.width; x++) {
            for (int y = 0; y < intArray.height; y++) {
                switch(getPixel(intArray, x, y)) {
                    case 0:
                        terrain.Land[x, y] = Land.Hill;
                        break;
                    case 64:
                        terrain.Land[x, y] = Land.Water;
                        break;
                    case 128:
                        terrain.Land[x, y] = Land.Forest;
                        break;
                    case 255:
                        terrain.Land[x, y] = Land.Grass;
                        break;
                    default:
                        Debug.Log("Uh oh, " + x + "," + y + ": color " + getPixel(intArray, x, y));
                        break;
                }
            }
        }
    }

    public static Vector2Int PlaceFountains(Terrain terrain) {
        int subDim = terrain.Bounds.x / 2;
        Vector2Int location;
        Vector2Int startLocation = Vector2Int.zero;
        // water
        for (int t = 0; t < 1000; t++) {
            startLocation = Randoms.Vector2Int(0, 0, subDim, subDim);
            if (terrain.Land[startLocation] == Land.Water || terrain.Land[startLocation] == Land.Hill) continue;
            terrain.Land[startLocation] = Land.Grass;
            Feature first = terrain.BuildFeature(startLocation, FeatureLibrary.P.fountain);
            first.GetComponentStrict<Fountain>().Team = 1;
            break;
        }
        // grass
        for (int t = 0; t < 1000; t++) {
            location = Randoms.Vector2Int(subDim, 0, 2*subDim, subDim);
            if (terrain.Land[location] == Land.Water || terrain.Land[location] == Land.Hill) continue;
            terrain.Land[location] = Land.Grass;
            terrain.BuildFeature(location, FeatureLibrary.P.fountain);
            break;
        }
        // forest
        Vector2Int? lastLoc = null;
        for (int t = 0; t < 1000; t++) {
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
        for (int t = 0; t < 1000; t++) {
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
            for (int t = 0; t < 1000; t++) {
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