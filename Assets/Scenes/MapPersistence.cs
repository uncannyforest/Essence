using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

[Serializable]
public struct MapData {
    [SerializeField] public Land[,] land;
    [SerializeField] public Construction[,] xWalls;
    [SerializeField] public Construction[,] yWalls;
    [SerializeField] public Construction[,] roofs;

    public override string ToString() {
        return land.GetUpperBound(0) + " " + land.GetUpperBound(1) + "\n"
            + xWalls.GetUpperBound(0) + " " + xWalls.GetUpperBound(1) + "\n"
            + yWalls.GetUpperBound(0) + " " + yWalls.GetUpperBound(1) + "\n"
            + roofs.GetUpperBound(0) + " " + roofs.GetUpperBound(1);
    }
}

public class MapPersistence : MonoBehaviour {

    public static void SaveGame() {
        MapData mapData = new MapData();

        Terrain terrain = Terrain.I;

        mapData.land = terrain.AllLandTiles;
        mapData.xWalls = terrain.AllXWallTiles;
        mapData.yWalls = terrain.AllYWallTiles;
        mapData.roofs = terrain.AllRoofTiles;

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
            Terrain.I.PopulateTerrainFromData(mapData.land, mapData.xWalls, mapData.yWalls, mapData.roofs);
            Debug.Log("Game data loaded!");
        } else Debug.LogError("There is no save data!");
    }
}
