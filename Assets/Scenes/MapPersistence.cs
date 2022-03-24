using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

[Serializable]
public struct MapData {
    [SerializeField] public Land[,] land;
    [SerializeField] public Construction[,] xWalls;
    [SerializeField] public Construction[,] yWalls;
    [SerializeField] public Construction[,] roofs;
    [SerializeField] public Feature.Data[] features;
}

public class MapPersistence : MonoBehaviour {

    public static void SaveGame() {
        MapData mapData = new MapData();

        Terrain terrain = Terrain.I;

        mapData.land = terrain.AllLandTiles;
        mapData.xWalls = terrain.AllXWallTiles;
        mapData.yWalls = terrain.AllYWallTiles;
        mapData.roofs = terrain.AllRoofTiles;

        List<Feature.Data> features = new List<Feature.Data>();
        for (int x = 0; x < terrain.Bounds.x; x++) for (int y = 0; y < terrain.Bounds.y; y++) {
            if (terrain.Feature[x, y] != null) features.Add(terrain.Feature[x, y].Serialize());
        }
        mapData.features = features.ToArray();

        BinaryFormatter bf = new BinaryFormatter(); 
        FileStream file = File.Create(Application.persistentDataPath 
                    + "/MySaveData.dat");
        bf.Serialize(file, mapData);
        file.Close();
        Debug.Log("Game data saved to " + Application.persistentDataPath + "/MySaveData.dat");
    }

    public static void LoadGame() {
        if (File.Exists(Application.persistentDataPath 
                    + "/MySaveData.dat")) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath 
                    + "/MySaveData.dat", FileMode.Open);
            MapData mapData = (MapData)bf.Deserialize(file);
            Debug.Log(mapData);
            file.Close();
            Terrain.I.PopulateTerrainFromData(mapData);
            Debug.Log("Game data loaded!");
        } else Debug.LogError("There is no save data!");
    }
}
