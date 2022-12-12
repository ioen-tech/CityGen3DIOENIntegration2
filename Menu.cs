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
using System.IO;
using System.Globalization;

public class Menu : MonoBehaviour
{

    [MenuItem("Tools/CityGen3D/Integrations/Internet Of Energy Network/Upload Interval Data")]
    static async void UploadIntervalData()
    {
        string path = EditorUtility.OpenFilePanel("Upload Interval Data", "", "csv");
        string rootPath = "/Users/philipbeadle/IOEN/AEMO/IntervalData";

        if (path.Length != 0)
        {
            long count = 0;
            DateTime startDate = new DateTime(2013, 3, 2);
            DateTime endDate = new DateTime(2014, 3, 2);

            using (var sr = new StreamReader(path))
            {
                await sr.ReadLineAsync(); // skip first line
                while (true && count < 1000000)
                {
                    var line = await sr.ReadLineAsync();
                    if (line == null) break;
                    var values = line.Split(",");
                    var nanogridId = values[0];
                    var generalSupplyKwh = values[4];
                    DateTime readingDateTime = DateTime.ParseExact(values[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (startDate <= readingDateTime && readingDateTime <= endDate)
                    {
                        using (StreamWriter outputFile = new StreamWriter(Path.Combine(rootPath, nanogridId + ".csv"), true))
                        {
                            var dayOfYear = readingDateTime.DayOfYear;
                            var timeOfDay = readingDateTime.Hour + ":" + readingDateTime.Minute;
                            outputFile.WriteLine(dayOfYear + "," + timeOfDay + "," + generalSupplyKwh);
                        }
                    }

                    if (count % 1000 == 0)
                    {
                        DirectoryInfo dir = new DirectoryInfo(Path.Combine(rootPath));
                        FileInfo[] fis = dir.GetFiles();
                        int i = 0;
                        foreach (FileInfo fi in fis)
                        {
                            i++;
                        }
                        Debug.Log("Processed " + i + " nanoGrids so far ");
                        Debug.Log("Processed " + count + " intervals so far " + readingDateTime);
                    }
                    count++;
                }
            }
            Debug.Log("Finished Uploading");
        }
    }
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
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:housenumber") != null){
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
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:state") != null) {
                    nanoGrid.state = mapBuilding.way.tags.Find(tag => tag.key == "addr:state").value;
                    buildingAddress += nanoGrid.state + " ";
                }
                if (mapBuilding.way.tags.Find(tag => tag.key == "addr:postcode") != null)
                {
                    nanoGrid.postCode = mapBuilding.way.tags.Find(tag => tag.key == "addr:postcode").value;
                    buildingAddress += nanoGrid.postCode;
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
        dbCommandCreateTable.CommandText = "CREATE TABLE IF NOT EXISTS Nanogrids (building_name TEXT PRIMARY KEY, house_number TEXT, street TEXT, suburb TEXT, state TEXT, postcode TEXT, network_name TEXT, power TEXT, source TEXT, system_generation_capacity REAL, system_storage_capacity REAL)";
        dbCommandCreateTable.ExecuteReader();
        IDbCommand dbCommandCreateSupplyAgreementsTable = dbConnection.CreateCommand();
        dbCommandCreateSupplyAgreementsTable.CommandText = "CREATE TABLE IF NOT EXISTS SupplyAgreements (supply_agreement_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, consumer_building_name TEXT, supplier_building_name TEXT, tariff_ioen_fuel REAL, transaction_energy_limit REAL, credit_limit REAL)";
        dbCommandCreateSupplyAgreementsTable.ExecuteReader();

        Transform[] allTransforms = FindObjectsOfType<Transform>();
        Transform[] nanoGrids = Array.FindAll(allTransforms, ele => ele.name.StartsWith("NanoGrid - "));

        foreach (Transform nanoGridTransform in nanoGrids)
        {
            NanoGrid nanoGrid = nanoGridTransform.GetComponent<NanoGrid>();
            IDbCommand dbCommandInsertValue = dbConnection.CreateCommand(); // 9
            dbCommandInsertValue.CommandText = "INSERT INTO Nanogrids (building_name, house_number, street, suburb, state, postcode, network_name, power, source, system_generation_capacity, system_storage_capacity) VALUES (\"" + nanoGrid.buildingName + "\", \"" + nanoGrid.houseNumber + "\", \"" + nanoGrid.street + "\", \"" + nanoGrid.suburb + "\", \"" + nanoGrid.state + "\", \"" + nanoGrid.postCode + "\", \"" + nanoGrid.networkName + "\", \"" + nanoGrid.power + "\", \"" + nanoGrid.source + "\", \"" + nanoGrid.systemGenerationCapacity + "\", \"" + nanoGrid.systemStorageCapacity + "\")";
            dbCommandInsertValue.ExecuteNonQuery(); // 11

            foreach (Transform child in nanoGridTransform)
            {
                if(child.name.StartsWith("SupplyAgreement - "))
                {
                    SupplyAgreement supplyAgreement = child.GetComponent<SupplyAgreement>();
                    IDbCommand dbCommandInsertSupplyAgreements = dbConnection.CreateCommand(); // 9
                    dbCommandInsertSupplyAgreements.CommandText = "INSERT INTO SupplyAgreements (consumer_building_name, supplier_building_name, tariff_ioen_fuel, transaction_energy_limit, credit_limit) VALUES (\"" + supplyAgreement.GetConsumerNanoGrid().buildingName + "\", \"" + supplyAgreement.GetSupplierNanoGrid().buildingName + "\", " + supplyAgreement.GetTariffIoenFuel() + ", " + supplyAgreement.GetTransactionEnergyLimit() + ", " + supplyAgreement.GetCreditLimit() + ")";
                    dbCommandInsertSupplyAgreements.ExecuteNonQuery(); // 11
                }
            }
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

        Transform[] allTransforms = FindObjectsOfType<Transform>();
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
            string power = dataReader.GetString(7);
            string source = dataReader.GetString(8);
            float systemGenerationCapacity = float.Parse(dataReader.GetValue(9).ToString());
            float systemStorageCapacity = float.Parse(dataReader.GetValue(10).ToString());

            NanoGrid nanoGrid = Array.Find(nanoGrids, ele => ele.buildingName == buildingname.Replace("NanoGrid - ", ""));
            nanoGrid.buildingName = buildingname;
            nanoGrid.houseNumber = houseNumber;
            nanoGrid.street = street;
            nanoGrid.suburb = suburb;
            nanoGrid.state = state;
            nanoGrid.postCode = postCode;
            nanoGrid.networkName = networkName;
            nanoGrid.power = power;
            nanoGrid.source = source;
            nanoGrid.systemGenerationCapacity = systemGenerationCapacity;
            nanoGrid.systemStorageCapacity = systemStorageCapacity;
            nanoGrid.supplyAgreements = new List<SupplyAgreement>();

            string buildingAddress = "";
            if (houseNumber != "") buildingAddress = houseNumber + " ";
            if (street != "") buildingAddress += street + " ";
            if (state != "") buildingAddress += state + " ";
            if (postCode != "") buildingAddress += postCode;;
            string id = buildingname;
            if (buildingAddress != "") id = buildingAddress;
            Transform nanoGridTransform = Array.Find(allTransforms, ele => ele.name == "NanoGrid - " + id);
            GameObject[] existingSupplyAgreements = new GameObject[nanoGridTransform.childCount];
            int i = 0;
            foreach (Transform existingSupplyAgreement in nanoGridTransform)
            {
                existingSupplyAgreements[i] = existingSupplyAgreement.gameObject;
                i += 1;
            }
            foreach (GameObject existingSupplyAgreement in existingSupplyAgreements)
            {
                if (existingSupplyAgreement.name.StartsWith("SupplyAgreement - "))
                {
                    DestroyImmediate(existingSupplyAgreement.gameObject);
                }
            }

            IDbCommand dbCommandReadSupplyAgreementValues = dbConnection.CreateCommand();
            dbCommandReadSupplyAgreementValues.CommandText = "SELECT * FROM SupplyAgreements WHERE consumer_building_name = \"" + nanoGrid.buildingName + "\"";
            IDataReader supplyAgreementDataReader = dbCommandReadSupplyAgreementValues.ExecuteReader();
            while (supplyAgreementDataReader.Read())
            {
                string supplierBuildingName = supplyAgreementDataReader.GetString(2);
                float tariffIoenFuel = float.Parse(supplyAgreementDataReader.GetValue(3).ToString());
                float transactionEnergyLimit = float.Parse(supplyAgreementDataReader.GetValue(4).ToString());
                float creditLimit = float.Parse(supplyAgreementDataReader.GetValue(5).ToString());

                NanoGrid supplierNanoGrid = Array.Find(nanoGrids, ele => ele.buildingName == supplierBuildingName.Replace("NanoGrid - ", ""));
                GameObject supplyAgreementObject = Instantiate(Resources.Load("Prefabs/pfSupplyAgreement", typeof(GameObject))) as GameObject;
                supplyAgreementObject.transform.parent = nanoGridTransform.transform;
                supplyAgreementObject.name = "SupplyAgreement - " + supplierBuildingName;
                supplyAgreementObject.SetActive(true);
                SupplyAgreement supplyAgreement = supplyAgreementObject.GetComponent<SupplyAgreement>();

                supplyAgreement.SetConsumerNanoGrid(nanoGrid);
                supplyAgreement.SetSupplierNanoGrid(supplierNanoGrid);
                supplyAgreement.SetTariffIoenFuel(tariffIoenFuel);
                supplyAgreement.SetTransactionEnergyLimit(transactionEnergyLimit);
                supplyAgreement.SetCreditLimit(creditLimit);
                nanoGrid.supplyAgreements.Add(supplyAgreement);
            }
        }
    }
}