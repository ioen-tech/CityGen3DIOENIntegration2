using CityGen3D.EditorExtension;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using CityGen3D;
using System;
using Mono.Data.Sqlite;
using System.Data;

public class Menu : MonoBehaviour
{
    [MenuItem("Tools/CityGen3D/Integrations/Internet Of Energy Network/Add NanoGrids to Model")]
    public static void AddNanoGridsToModel()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        Transform ioenRoot = Array.Find(allTransforms, ele => ele.name == "IOEN");
        if (ioenRoot == null)
        {
            GameObject objIOEN = new GameObject("IOEN");
            ioenRoot = objIOEN.transform;
            GameObject gameManagerObject = Instantiate(Resources.Load("Prefabs/pfGameManager", typeof(GameObject))) as GameObject;
            gameManagerObject.name = "Game Manager";
            gameManagerObject.transform.parent = ioenRoot;
            GameObject ioenManagerObject = Instantiate(Resources.Load("Prefabs/pfIoenManager", typeof(GameObject))) as GameObject;
            ioenManagerObject.name = "Ioen Manager";
            ioenManagerObject.transform.parent = ioenRoot;
        }
        Transform nanoGrids = ioenRoot.Find("NanoGrids");
        if (nanoGrids != null)
        {
            DestroyImmediate(nanoGrids.gameObject);
        }
        nanoGrids = new GameObject("NanoGrids").transform;
        nanoGrids.parent = ioenRoot;
        int index = 0;
        List<Landscape> landscapes = Generator.GetLandscapes();
        foreach(Landscape landscape in landscapes)
        {
            Transform buildings = landscape.transform.Find("Buildings");
            foreach (MapBuilding mapBuilding in Map.Instance.mapBuildings.GetMapBuildings())
            {
                Transform building = buildings.Find(mapBuilding.name);
                // ignore if building not over this terrain (we'll process it when we are processing its Landscape)
                if (!landscape.IsOverLandscape(mapBuilding))
                {
                    continue;
                }
                if (building == null)
                {   
                    continue;
                }
                index++;

                GameObject nanoGridObject = Instantiate(Resources.Load("Prefabs/pfNanoGrid", typeof(GameObject))) as GameObject;
                nanoGridObject.transform.parent = nanoGrids.transform;
                nanoGridObject.transform.position = mapBuilding.GetCentre() + new Vector3(0, building.position.y + mapBuilding.height + 10, 0);
                NanoGrid nanoGrid = nanoGridObject.GetComponent<NanoGrid>();
                nanoGrid.size = mapBuilding.GetArea();
                nanoGrid.index = index;
                string buildingAddress = "";
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:housenumber") != null)
                {
                    nanoGrid.houseNumber = mapBuilding.way.tags.Find(tag => tag.key == "addr:housenumber").value;
                    buildingAddress += nanoGrid.houseNumber + " ";
                }
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:street") != null) {
                    nanoGrid.street = mapBuilding.way.tags.Find(tag => tag.key == "addr:street").value;
                    buildingAddress += nanoGrid.street + " ";
                }
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:suburb") != null) {
                    nanoGrid.suburb = mapBuilding.way.tags.Find(tag => tag.key == "addr:suburb").value;
                    buildingAddress += nanoGrid.suburb + " ";
                }
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:postcode") != null) {
                    nanoGrid.postCode = mapBuilding.way.tags.Find(tag => tag.key == "addr:postcode").value;
                    buildingAddress += nanoGrid.postCode + " ";
                }
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:state") != null) {
                    nanoGrid.state = mapBuilding.way.tags.Find(tag => tag.key == "addr:state").value;
                    buildingAddress += nanoGrid.street;
                }
                if (buildingAddress == "") buildingAddress = mapBuilding.name;
                nanoGridObject.name = "NanoGrid - " + buildingAddress;
                nanoGrid.buildingName = mapBuilding.name;
                if (mapBuilding.way.tags.Find(tag => tag.key == "power") != null)
                {
                    nanoGrid.power = mapBuilding.way.tags.Find(tag => tag.key == "power").value;
                }
                if (mapBuilding.way.tags.Find(tag => tag.key == "generator:source") != null)
                {
                    nanoGrid.source = mapBuilding.way.tags.Find(tag => tag.key == "generator:source").value;
                }
            }
        }
    }

    [MenuItem("Tools/CityGen3D/Integrations/Internet Of Energy Network/Create Database from Model")]
    public static void CreateDatabaseFromModel()
    {
        string path = EditorUtility.OpenFolderPanel("Select the folder to store IOENWorld databases", "", "");
        if (path.Length == 0) return;
        string dbUri = "URI=file:" + path + "/IOENWorld" + DateTime.Now.ToString("yyyyMMddHHmm") + ".db";
        Debug.Log(dbUri);
        IDbConnection dbConnection = new SqliteConnection(dbUri);
        dbConnection.Open();
        IDbCommand dbCommandCreateTable = dbConnection.CreateCommand();
        dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS Nanogrids (building_name TEXT PRIMARY KEY, house_number TEXT, street TEXT, suburb TEXT, state TEXT, postcode TEXT, network_name TEXT, networkIndex REAL, power TEXT, source TEXT)";
        dbCommandCreateTable.ExecuteReader();

        NanoGrid[] nanoGrids = FindObjectsOfType<NanoGrid>();
        foreach(NanoGrid nanoGrid in nanoGrids)
        {
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand(); // 9
            dbCommandInsertValue.CommandText = "INSERT INTO Nanogrids (building_name, house_number, street, suburb, state, postcode, network_name, networkIndex, power, source) VALUES (\"" + nanoGrid.buildingName + "\", \"" + nanoGrid.houseNumber + "\", \"" + nanoGrid.street + "\", \"" + nanoGrid.suburb + "\", \"" + nanoGrid.state + "\", \"" + nanoGrid.postCode + "\", \"" + nanoGrid.networkName + "\", " + nanoGrid.networkIndex + ", \"" + nanoGrid.power + "\", \"" + nanoGrid.source + "\")";
            dbCommandInsertValue.ExecuteNonQuery(); // 11
        }
    }

    [MenuItem("Tools/CityGen3D/Integrations/Internet Of Energy Network/Update Model from Database")]
    public static void UpdateModelFromDatabase()
    {
        string path = EditorUtility.OpenFilePanel("Select the folder to store IOENWorld databases", "", "db");
        if (path.Length == 0) return;
        string dbUri = "URI=file:" + path;
        Debug.Log(dbUri);
        IDbConnection dbConnection = new SqliteConnection(dbUri);
        dbConnection.Open();
        IDbCommand dbCommandReadValues = dbConnection.CreateCommand();
        dbCommandReadValues.CommandText = "SELECT * FROM Nanogrids";
        IDataReader dataReader = dbCommandReadValues.ExecuteReader();

        NanoGrid[] nanoGrids = FindObjectsOfType<NanoGrid>();

        while (dataReader.Read())
        {
            string buildingname = dataReader.GetString(0);
            string houseNumber = dataReader.GetString(1);
            string street = dataReader.GetString(2);
            string suburb = dataReader.GetString(3);
            string state = dataReader.GetString(4);
            string postCode = dataReader.GetString(5);
            string networkName = dataReader.GetString(6);
            //int networkIndex = dataReader.get(8);
            string power = dataReader.GetString(8);
            string source = dataReader.GetString(9);

            Debug.Log(buildingname);

            NanoGrid nanoGrid = Array.Find(nanoGrids, ele => ele.buildingName == buildingname.Replace("NanoGrid - ", ""));
            nanoGrid.buildingName = buildingname;
            nanoGrid.houseNumber = houseNumber;
            nanoGrid.street = street;
            nanoGrid.suburb = suburb;
            nanoGrid.state = state;
            nanoGrid.postCode = postCode;
            nanoGrid.networkName = networkName;
            //nanoGrid.networkIndex = networkIndex;
            nanoGrid.power = power;
            nanoGrid.source = source;
        }
    }
}