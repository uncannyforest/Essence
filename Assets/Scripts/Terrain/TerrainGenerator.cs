
using System;
using System.Collections.Generic;
using System.Linq;
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
    static T[] clone<T>(T[] oldArray) => (T[])oldArray.Clone();

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

    class Parameters {
        public float[] v;
        public Parameters(params float[] values) {
            this.v = values;
        }
        public Parameters clone() => new Parameters(TerrainGenerator.clone(v));
        public Land primaryLand => (Land)this.v[0];
        public Land secondaryLand => (Land)this.v[1];
        public float modFactor { get => this.v[8]; set => this.v[8] = value; }
        public float numRivers => this.v[9];
        public bool keepLakes => this.v[10] > .50f;
        public bool keepRivers => this.v[11] > .50f;

        public Land
        randomLand() {
            var seed = Random.value;
            seed -= this.v[2];
            if (seed < 0) return Land.Grass;
            seed -= this.v[3];
            if (seed < 0) return Land.Meadow;
            seed -= this.v[4];
            if (seed < 0) return Land.Shrub;
            seed -= this.v[5];
            if (seed < 0) return Land.Forest;
            seed -= this.v[6];
            if (seed < 0) return Land.Water;
            return Land.Hill;
        }
    }

    const int DIM = 256;
    const int CELLS_DIM = 4;

    const int INIT_DIM = 16;

    const int MAX_RIVER_SOURCES = 8;

    static Parameters[] PARAMETERS = new Parameters[] {
        // Ocean
        new Parameters((int)Land.Water, (int)Land.Hill, .000f, .000f, .000f, .000f, .550f, .450f, 0, 8, 1, 0), // islands
        new Parameters((int)Land.Water, (int)Land.Forest, .000f, .000f, .000f, .550f, .450f, .000f, 0, 8, 1, 1), // forest islands
        new Parameters((int)Land.Water, (int)Land.Meadow, .000f, .250f, .150f, .150f, .450f, .000f, 0, 8, 1, 1), // meadow islands
        // Grassland
        new Parameters((int)Land.Grass, (int)Land.Hill, .000f, .000f, .000f, .100f, .350f, .550f, 0, 2, 0, 0), // grassland
        // Forest
        new Parameters((int)Land.Forest, (int)Land.Water, .000f, .000f, .000f, .600f, .200f, .200f, 0, 8, 1, 0), // forest rivers
        new Parameters((int)Land.Shrub, (int)Land.Hill, .000f, .000f, .300f, .000f, .350f, .350f, 0, 1, 1, 1), // shrubland
        new Parameters((int)Land.Forest, (int)Land.Meadow, .000f, .600f, .050f, .250f, .100f, .000f, 0, 1, 0, 0), // forest meadow
        // Mountains
        new Parameters((int)Land.Hill, (int)Land.Water, .000f, .000f, .000f, .000f, .200f, .800f, 0, 2, 0, 0), // mountain lakes
        new Parameters((int)Land.Hill, (int)Land.Forest, .000f, .000f, .000f, .400f, .200f, .400f, 0, 1, 0, 0), // mountain forest
        new Parameters((int)Land.Hill, (int)Land.Shrub, .000f, .000f, .400f, .000f, .200f, .400f, 0, 1, 0, 0), // mountain shrubs
     };

    static Func<Land, Parameters>
    getAlgoByDepths = (land) => {
        if (land == Land.Water) return PARAMETERS[Random.Range(0, 3)];
        if (land == Land.Grass) return PARAMETERS[3];
        if (land == Land.Forest) return PARAMETERS[Random.Range(4, 7)];
        return PARAMETERS[Random.Range(7, 10)];
    };

    static Parameters[] DEPTHS_PARAMETERS = new Parameters[] {
        new Parameters((int)Land.Water, (int)Land.Water, .000f, .000f, .000f, .000f, 1.00f, .000f, .50f, 0, 1, 1),
        new Parameters((int)Land.Grass, (int)Land.Grass, 1.00f, .000f, .000f, .000f, .000f, .000f, .50f, 0, 0, 0),
        new Parameters((int)Land.Hill, (int)Land.Hill, .000f, .000f, .000f, .000f, .000f, .000f, .50f, 0, .50f, 0),
        new Parameters((int)Land.Forest, (int)Land.Forest, .000f, .000f, .000f, 1.00f, .000f, 1.00f, .50f, 0, .50f, 0),
     };

    // END Heavy translation required here

    static Func<ImageData, int, int, Land>
    getDepths = (image, xPixel, yPixel) => {
        return Land.Hill;
    };

    static Func<int, int, Parameters[][], Parameters>
    getCellAlgo = (x, y, cells) => {
        if (y < 0) return DEPTHS_PARAMETERS[x <= CELLS_DIM / 2 ? 0 : 1];
        if (y >= CELLS_DIM) return DEPTHS_PARAMETERS[x <= CELLS_DIM / 2 ? 2 : 3];
        if (x < 0) return DEPTHS_PARAMETERS[y <= CELLS_DIM / 2 ? 0 : 2];
        if (x >= CELLS_DIM) return DEPTHS_PARAMETERS[y <= CELLS_DIM / 2 ? 1 : 3];
        return cells[y][x];
    };

    static Func<ImageData, int, int, Parameters[][], Parameters>
    getCell = (image, xPixel, yPixel, cells) => {
        var x = CELLS_DIM * xPixel / image.width - .50f;
        var y = CELLS_DIM * yPixel / image.height - .50f;
        var x0 = Mathf.FloorToInt(x);
        var y0 = Mathf.FloorToInt(y);
        var xF = x - x0;
        var yF = y - y0;
        var result = new float[PARAMETERS[0].v.Length];
        result[0] = getCellAlgo(Mathf.RoundToInt(x), Mathf.RoundToInt(y), cells).v[0];
        result[1] = getCellAlgo(Mathf.RoundToInt(x), Mathf.RoundToInt(y), cells).v[1];
        for (var i = 2; i < PARAMETERS[0].v.Length; i++) {
            result[i] = Mathf.Lerp(
                Mathf.Lerp(getCellAlgo(x0, y0, cells).v[i], getCellAlgo(x0 + 1, y0, cells).v[i], 2 * xF - .50f),
                Mathf.Lerp(getCellAlgo(x0, y0 + 1, cells).v[i], getCellAlgo(x0 + 1, y0 + 1, cells).v[i], 2 * xF - .50f),
                2 * yF - .50f
            );
        }
        return new Parameters(result);
    };

    static Func<Parameters[][], Land[]>
    getInitImage = (cells) => {
        var result = new Land[INIT_DIM * INIT_DIM];
        for (var outerY = 0; outerY < CELLS_DIM; outerY++) {
            for (var outerX = 0; outerX < CELLS_DIM; outerX++) {
                var algo = cells[outerY][outerX];
                var primary = algo.primaryLand;
                var secondary = algo.secondaryLand;
                for (var innerY = 0; innerY < INIT_DIM / CELLS_DIM; innerY++) {
                    for (var innerX = 0; innerX < INIT_DIM / CELLS_DIM; innerX++) {
                        result[outerY * 64 + innerY * 16 + outerX * 4 + innerX] =
                            (innerX >= 1 && innerX <= 2 && innerY >= 1 && innerY <= 2) ?
                            secondary : primary;
                    }
                }
            }
        }
        return result;
    };

    static Action<Land[], Land[]>
    copyArrayToNewArray = (oldArray, newArray) => {
        for (var i = 0; i < oldArray.Length; i++) {
            newArray[i] = oldArray[i];
        }
    };

    // https://stackoverflow.com/a/12646864
    static Action<Land[]>
    shuffleArray = (array) => {
        for (var i = array.Length - 1; i >= 0; i--) {
            var j = Random.Range(0, i + 1);
            var temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    };

    static Func<Parameters[][]>
    genCells = () => {
        var result = new Parameters[][] { new Parameters[4], new Parameters[4], new Parameters[4], new Parameters[4] };
        result[1][1] = PARAMETERS[0];
        result[0][2] = getAlgoByDepths(Land.Grass);
        result[3][2] = getAlgoByDepths(Land.Forest);
        result[3][0] = getAlgoByDepths(Land.Hill);
        var either = Randoms.Order(getAlgoByDepths(Land.Water), getAlgoByDepths(Land.Grass)).ToArray();
        result[0][1] = either[0];
        result[1][2] = either[1];
        either = Randoms.Order(getAlgoByDepths(Land.Water), getAlgoByDepths(Land.Forest)).ToArray();
        result[1][0] = either[0];
        result[2][2] = either[1];
        either = Randoms.Order(getAlgoByDepths(Land.Water), getAlgoByDepths(Land.Hill)).ToArray();
        result[0][0] = either[0];
        result[2][1] = either[1];
        either = Randoms.Order(getAlgoByDepths(Land.Grass), getAlgoByDepths(Land.Forest)).ToArray();
        result[1][3] = either[0];
        result[2][3] = either[1];
        either = Randoms.Order(getAlgoByDepths(Land.Grass), getAlgoByDepths(Land.Hill)).ToArray();
        result[0][3] = either[0];
        result[2][0] = either[1];
        either = Randoms.Order(getAlgoByDepths(Land.Forest), getAlgoByDepths(Land.Hill)).ToArray();
        result[3][1] = either[0];
        result[3][3] = either[1];
        for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) {
                result[y][x] = result[y][x].clone();
                result[y][x].modFactor = Random.value;
            }
        result[1][1].modFactor = 0;
        return result;
    };

    static Action<ImageData, ImageData, Parameters[][]>
    magWithNoise = (oldImage, newImage, cells) => {
        for (var x = 0; x < oldImage.height; x++) { // x is old image x
            for (var y = 0; y < oldImage.width; y++) { // y is old image y
                var cell = getCell(oldImage, x, y, cells);
                var mod = cell.modFactor;
                // var mutateX = Random.Range(0, 2);
                // var mutateY = Random.Range(0, 2);
                for (var i = 0; i < 2; i++) { // i is sub x in new image
                    for (var j = 0; j < 2; j++) { // j is sub y in new image
                        var doMutate = 1 < Random.value * (1+mod*mod);
                        var useAdjX = Mathf.FloorToInt(Random.value * (1+(1-mod)*(1-mod)*(1-mod)));
                        var useAdjY = Mathf.FloorToInt(Random.value * (1+(1-mod)*(1-mod)*(1-mod)));

                        if (doMutate) {
                            setPixel(newImage, 2*x+i, 2*y+j, cell.randomLand());
                        } else {
                            copyPixel(oldImage, x + useAdjX*(i*2-1), y + useAdjY*(j*2-1), newImage, 2*x+i, 2*y+j, getDepths(oldImage, x, y));
                        }
                    }
                }
            }
        }
    };

    // static Func<float, float, float, Land?>
    // tutorialIsland = (x, y, dim) => {
    //     if (x >= dim / 8 && x < dim * 7 / 8 && y >= dim / 8 && y < dim * 7 / 8 && Random.Range(0, 3) == 0) {
    //         return Random.Range(0, 2) == 0 ? Land.Hill : Land.Grass;
    //     } else if (nearEdge(x, dim, dim) || nearEdge(y, dim, dim)) {
    //         return Land.Water;
    //     } else {
    //         return null;
    //     }
    // };

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
            return from.includes(getDepths(image, x, y)) ? 1 : 0;
        }
        return from.includes(getPixel(image, x, y)) ? 1 : 0;
    };
    static int neighborCount(ImageData image, int x, int y, params Land[] from) {
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

    static Func<ImageData, int, int, Land[], int, Vector2Int?>
    select = (image, cellX, cellY, check, maxTries) => {
        for (var i = 0; i < maxTries; i++) {
            var cellSize = DIM / CELLS_DIM;
            var xMin = cellSize * cellX;
            var yMin = cellSize * cellY;
            var x = Random.Range(xMin, xMin + cellSize);
            var y = Random.Range(yMin, yMin + cellSize);
            if (!check.includes(getPixel(image, x, y))) continue;
            return new Vector2Int(x, y);
        }
        return null;
    };

    static Func<ImageData, int, int, Land, int, Vector2Int?>
    selectEmptyArea = (image, cellX, cellY, check, maxTries) => {
        for (var i = 0; i < maxTries; i++) {
            var cellSize = DIM / CELLS_DIM;
            var xMin = cellSize * cellX;
            var yMin = cellSize * cellY;
            var x = Random.Range(xMin, xMin + cellSize);
            var y = Random.Range(yMin, yMin + cellSize);
            if (neighborCount(image, x, y, check) > 0) continue;
            return new Vector2Int(x, y);
        }
        return null;
    };

    static Func<ImageData>
    GenerateIntArray = () => {
        var ctx = new IntermediateDebugStep[11];
        for (var i = 0; i < 11; i++) {
            ctx[i] = getDebugImage(i);
        }
        var debugStep = 1;

        var id = ctx[0].createImageData(INIT_DIM, INIT_DIM);
        ImageData idn;

        var cells = genCells();

        var fancyArray = id.data;
        var initImg = getInitImage(cells);
        //initImg = landToPixelArray(initImg);
        copyArrayToNewArray(initImg, fancyArray);
        //id.data.set(fancyArray);
        ctx[debugStep++].putImageData(id, 0, 0);

        var dim = INIT_DIM;

        for (; dim < DIM; dim *= 2) {
            idn = ctx[0].createImageData(dim * 2, dim * 2);
            magWithNoise(id, idn, cells);
            id = idn;
            if (dim == INIT_DIM) ctx[debugStep++].putImageData(id, 0, 0);
        }
        Debug.Log(debugStep + " init noise");
        ctx[debugStep++].putImageData(id, 0, 0);

        // unbiased smoothing
        for (var i = 0; i < 4; i++) {
            idn = ctx[0].createImageData(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (neighborCount(id, x, y, Land.Hill) > 4) {
                        setPixel(idn, x, y, Land.Hill);
                    } else if (neighborCount(id, x, y, Land.Water) > 4) {
                        setPixel(idn, x, y, Land.Water);
                    } else if (neighborCount(id, x, y, Land.Forest) > 4) {
                        setPixel(idn, x, y, Land.Forest);
                    } else if (neighborCount(id, x, y, Land.Shrub) > 4) {
                        setPixel(idn, x, y, Land.Shrub);
                    } else if (neighborCount(id, x, y, Land.Meadow) > 4) {
                        setPixel(idn, x, y, Land.Meadow);
                    } else if (neighborCount(id, x, y, Land.Grass) > 4) {
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

        // smooth water into everything (5+)
        // remove one-tile waters
        // thin grass line around trees
        // wide grass line around hills (i+)
        for (var i = 0; i < 6; i++) {
            idn = ctx[0].createImageData(DIM, DIM);
            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (i == 5 && neighborCount(id, x, y, Land.Water) > 4) {
                        setPixel(idn, x, y, Land.Water);
                    } else if (i == 5 && getPixel(id, x, y) == Land.Water && neighborCount(id, x, y, Land.Water) == 0) {
                        setPixel(idn, x, y, Land.Grass);
                    } else if (i == 5 && (getPixel(id, x, y)).IsPlanty() && neighborCount(id, x, y, Land.Water) > 0) {
                        setPixel(idn, x, y, Land.Grass);
                    } else if (getPixel(id, x, y) == Land.Hill && neighborCount(id, x, y, Land.Grass, Land.Water) > 2) {
                        setPixel(idn, x, y, Land.Grass);
                    } else {
                        copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " biased cleanup");
        ctx[debugStep++].putImageData(id, 0, 0);

        // generate rivers
        for (var x = 0; x < DIM; x++)
            for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == Land.Water)
                    setPixel(id, x, y, Land.Ditch);
        for (var cellX = 0; cellX < CELLS_DIM; cellX++) {
            for (var cellY = 0; cellY < CELLS_DIM; cellY++) {
                var cellSize = DIM / CELLS_DIM;
                var xMin = cellSize * cellX;
                var yMin = cellSize * cellY;
                for (var i = 0; i < 4; i++) {
                    var x = Random.Range(xMin, xMin + cellSize);
                    var y = Random.Range(yMin, yMin + cellSize);
                    if (getPixel(id, x, y) == Land.Ditch) {
                        floodFill(id, x, y, Land.Ditch, Land.Water);
                        break;
                    }
                }
            }
        }
        Debug.Log(debugStep + " identify outside");
        ctx[debugStep++].putImageData(id, 0, 0);

        var vel = new Vector2Int(0, Randoms.Sign);
        for (var i = 0; i < MAX_RIVER_SOURCES; i++) {
            idn = new ImageData(clone(id.data), DIM, DIM);
            for (var x = 0; x < CELLS_DIM; x++) {
                for (var y = 0; y < CELLS_DIM; y++) {
                    if (i < cells[y][x].numRivers) {
                        var possCoord = selectEmptyArea(id, x, y, Land.Water, 8);
                        if (possCoord == null) continue;
                        var coord = (Vector2Int) possCoord;
                        while (neighborCount(idn, coord.x, coord.y, Land.Water) == 0) {
                            var pos = new Vector2Int(coord.x, coord.y);
                            while (neighborCount(idn, pos.x, pos.y, Land.Water) == 0) {
                                if (Random.value < .50f)
                                    vel = new Vector2Int(vel.y * Randoms.Sign, vel.x * Randoms.Sign);
                                pos.x = bound(pos.x + vel.x, DIM);
                                pos.y = bound(pos.y + vel.y, DIM);
                            }
                            if (getPixel(idn, pos.x, pos.y) == Land.Ditch) {
                                floodFill(idn, pos.x, pos.y, Land.Ditch, Land.Water);
                            } else {
                                setPixel(idn, pos.x, pos.y, Land.Water);
                            }
                        }
                        setPixel(idn, coord.x, coord.y, Land.Dirtpile);
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " make rivers");
        ctx[debugStep++].putImageData(id, 0, 0);

        for (var x = 0; x < DIM; x++) for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == Land.Ditch && getCell(id, x, y, cells).keepLakes) floodFill(id, x, y, Land.Ditch, Land.Water);
        for (var x = 0; x < CELLS_DIM; x++) {
            for (var y = 0; y < CELLS_DIM; y++) {
                if (!cells[y][x].keepLakes) {
                    var prizes = MAX_RIVER_SOURCES - cells[y][x].numRivers;
                    for (var i = 0; i < prizes; i++) {
                        var possCoord = select(id, x, y, ARRAY(Land.Ditch), 16);
                        if (possCoord == null) continue;
                        var coord = (Vector2Int) possCoord;
                        setPixel(id, coord.x, coord.y, Land.Dirtpile);
                    }
                }
            }
        }
        for (var x = 0; x < DIM; x++) for (var y = 0; y < DIM; y++)
                if (getPixel(id, x, y) == Land.Ditch) setPixel(id, x, y, Land.Grass);
        Debug.Log(debugStep + " fill lakes in hills and grass areas");
        ctx[debugStep++].putImageData(id, 0, 0);

        // thin line of grass around hills
        idn = ctx[0].createImageData(DIM, DIM);
        for (var x = 0; x < DIM; x++) {
            for (var y = 0; y < DIM; y++) {
                if (getPixel(id, x, y) == Land.Hill && neighborCount(id, x, y, Land.Water) > 0) {
                    setPixel(idn, x, y, Land.Grass);
                } else {
                    copyPixel(id, x, y, idn, x, y, Land.Woodpile);
                }
            }
        }
        id = idn;
        Debug.Log(debugStep + " create edges around hills");
        ctx[debugStep++].putImageData(id, 0, 0);

        // reduce rivers
        for (var i = 0; i < 32; i++) {
            idn = new ImageData(clone(id.data), DIM, DIM);

            for (var x = 0; x < DIM; x++) {
                for (var y = 0; y < DIM; y++) {
                    if (!getCell(id, x, y, cells).keepRivers && getPixel(id, x, y) == Land.Water) {
                        if (neighborCount(id, x, y, Land.Grass, Land.Dirtpile) >= 7) {
                            setPixel(idn, x, y, Land.Grass);
                        } else if (neighborCount(id, x, y, Land.Water) <= 1) {
                            setPixel(idn, x, y, Land.Forest);
                        }
                    }
                }
            }
            id = idn;
        }
        Debug.Log(debugStep + " reduce rivers");
        ctx[debugStep++].putImageData(id, 0, 0);

        // fix rivers
        idn = new ImageData(clone(id.data), DIM, DIM);
        for (var x = 0; x < DIM - 1; x++) {
            for (var y = 0; y < DIM - 1; y++) {
                if (getPixel(id, x, y) == Land.Water && getPixel(id, x+1, y) != Land.Water &&
                        getPixel(id, x, y+1) != Land.Water &&  getPixel(id, x+1, y+1) == Land.Water) {
                    setPixel(idn, x, y+1, Land.Water);
                    setPixel(idn, x+1, y, Land.Water);
                } else if (getPixel(id, x, y) != Land.Water && getPixel(id, x+1, y) == Land.Water &&
                        getPixel(id, x, y+1) == Land.Water && getPixel(id, x+1, y+1) != Land.Water) {
                    setPixel(idn, x, y, Land.Water);
                    setPixel(idn, x+1, y+1, Land.Water);
                }
            }
        }
        id = idn;
        idn = new ImageData(clone(id.data), DIM, DIM);
        for (var x = 0; x < DIM; x++) {
            for (var y = 0; y < DIM; y++) {
                if (getPixel(id, x, y) != Land.Water && getPixel(id, x, y) != Land.Dirtpile && neighborCount(id, x, y, Land.Water) >= 5) {
                    setPixel(idn, x, y, Land.Water);
                }
            }
        }
        id = idn;
        Debug.Log(debugStep + " fix rivers");
        ctx[0].putImageData(id, 0, 0);

        return id;
    };

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
        Vector2Int location;
        Vector2Int startLocation = Vector2Int.zero;
        int gridInc = Terrain.Dim / CELLS_DIM;
        for (int x = 0; x < Terrain.Dim; x += gridInc) for (int y = 0; y < Terrain.Dim; y += gridInc) {
            Vector2Int? lastLoc = null;
            for (int t = 0; t < 999; t++) {
                location = Randoms.Vector2Int(x, y, x + gridInc, y + gridInc);
                if (lastLoc is Vector2Int lastLocConfirmed) {
                    location = Randoms.Midpoint(lastLocConfirmed, location);
                    lastLoc = location;
                    if (terrain.Land[location] == Land.Water || terrain.Land[location] == Land.Hill) continue;
                } else {
                    lastLoc = location;
                    continue;
                }
                terrain.Land[location] = Land.Grass;
                Feature fountain = terrain.BuildFeature(location, FeatureLibrary.C.fountain);
                if (x == gridInc && y == gridInc) {
                    startLocation = location;
                    fountain.hooks.GetComponentStrict<Fountain>().Team = 1;
                }
                break;
            }
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
            terrain.BuildFeature(location, FeatureLibrary.C.windmill);
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
        terrain.BuildFeature(farthestShore, FeatureLibrary.C.boat);
    }
}