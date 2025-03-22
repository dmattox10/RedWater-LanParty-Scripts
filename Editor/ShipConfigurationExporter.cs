using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class ShipConfigurationExporter : EditorWindow
{
    private ShipConfigurationSO smallShipConfig;
    private ShipConfigurationSO mediumShipConfig;
    private ShipConfigurationSO largeShipConfig;

    [MenuItem("Tools/Ship Configuration Manager")]
    public static void ShowWindow()
    {
        GetWindow<ShipConfigurationExporter>("Ship Config Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ship Configuration Manager", EditorStyles.boldLabel);

        smallShipConfig = (ShipConfigurationSO)EditorGUILayout.ObjectField("Small Ship Config", smallShipConfig, typeof(ShipConfigurationSO), false);
        mediumShipConfig = (ShipConfigurationSO)EditorGUILayout.ObjectField("Medium Ship Config", mediumShipConfig, typeof(ShipConfigurationSO), false);
        largeShipConfig = (ShipConfigurationSO)EditorGUILayout.ObjectField("Large Ship Config", largeShipConfig, typeof(ShipConfigurationSO), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Hardpoint Configurations"))
        {
            SaveHardpointConfigurations();
        }

        if (GUILayout.Button("Load Hardpoint Configurations"))
        {
            LoadHardpointConfigurations();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Export New Ship Configurations"))
        {
            ExportConfigurations();
        }
    }

    private void SaveHardpointConfigurations()
    {
        var configs = new ShipHardpointConfigs
        {
            SmallShipHardpoints = smallShipConfig?.hardpoints ?? new Hardpoint[0],
            MediumShipHardpoints = mediumShipConfig?.hardpoints ?? new Hardpoint[0],
            LargeShipHardpoints = largeShipConfig?.hardpoints ?? new Hardpoint[0]
        };

        string json = JsonConvert.SerializeObject(configs, Formatting.Indented);
        string path = EditorUtility.SaveFilePanel("Save Hardpoint Configurations", "Assets", "hardpoints", "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log($"Hardpoint configurations saved to: {path}");
        }
    }

    private void LoadHardpointConfigurations()
    {
        string path = EditorUtility.OpenFilePanel("Load Hardpoint Configurations", "Assets", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        var configs = JsonConvert.DeserializeObject<ShipHardpointConfigs>(json);

        if (smallShipConfig != null)
        {
            smallShipConfig.hardpoints = configs.SmallShipHardpoints;
            EditorUtility.SetDirty(smallShipConfig);
        }

        if (mediumShipConfig != null)
        {
            mediumShipConfig.hardpoints = configs.MediumShipHardpoints;
            EditorUtility.SetDirty(mediumShipConfig);
        }

        if (largeShipConfig != null)
        {
            largeShipConfig.hardpoints = configs.LargeShipHardpoints;
            EditorUtility.SetDirty(largeShipConfig);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Hardpoint configurations loaded successfully!");
    }

    [MenuItem("Tools/Export Ship Configurations")]
    public static void ExportConfigurations()
    {
        var path = EditorUtility.SaveFolderPanel("Save Ship Configurations", "Assets", "");
        if (string.IsNullOrEmpty(path)) return;

        // Convert to project relative path
        path = "Assets" + path.Substring(Application.dataPath.Length);

        // Create base configurations
        CreateShipConfiguration(ShipClass.Small, path);
        CreateShipConfiguration(ShipClass.Medium, path);
        CreateShipConfiguration(ShipClass.Large, path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Export Complete", "Ship configurations created successfully!", "OK");
    }

    private static void CreateShipConfiguration(ShipClass shipClass, string path)
    {
        var config = CreateInstance<ShipConfigurationSO>();
        config.shipClass = shipClass;
        
        // Set default values based on ship class
        config.stats = shipClass switch
        {
            ShipClass.Small => new ShipStats(5f, 0.1f, 0.05f, 2f, 0.8f, 1.0f),
            ShipClass.Medium => new ShipStats(4f, 0.08f, 0.03f, 1.5f, 0.6f, 1.5f),
            ShipClass.Large => new ShipStats(3f, 0.05f, 0.02f, 1f, 0.4f, 2.0f),
            _ => new ShipStats(4f, 0.08f, 0.03f, 1.5f, 0.6f, 1.5f)
        };

        // Set default hardpoints based on ship class
        config.hardpoints = shipClass switch
        {
            ShipClass.Small => new[]
            {
                new Hardpoint { 
                    id = "small_front", 
                    position = new Vector2(0, 0.5f), 
                    size = HardpointSize.Small 
                }
            },
            ShipClass.Medium => new[]
            {
                new Hardpoint { 
                    id = "med_front", 
                    position = new Vector2(0, 0.75f), 
                    size = HardpointSize.Medium 
                },
                new Hardpoint { 
                    id = "med_left", 
                    position = new Vector2(-0.5f, 0), 
                    size = HardpointSize.Small 
                },
                new Hardpoint { 
                    id = "med_right", 
                    position = new Vector2(0.5f, 0), 
                    size = HardpointSize.Small 
                }
            },
            ShipClass.Large => new[]
            {
                new Hardpoint { 
                    id = "large_front", 
                    position = new Vector2(0, 1f), 
                    size = HardpointSize.Large 
                },
                new Hardpoint { 
                    id = "large_left", 
                    position = new Vector2(-0.75f, 0.25f), 
                    size = HardpointSize.Medium 
                },
                new Hardpoint { 
                    id = "large_right", 
                    position = new Vector2(0.75f, 0.25f), 
                    size = HardpointSize.Medium 
                }
            },
            _ => new Hardpoint[0]
        };

        config.wakeForwardOffset = new Vector2(0, -0.14f);
        config.wakeReverseOffset = new Vector2(0, 0.25f);
        
        string className = shipClass.ToString().ToLower();
        AssetDatabase.CreateAsset(config, $"{path}/{className}ShipConfig.asset");
    }
}

[System.Serializable]
public class ShipHardpointConfigs
{
    public Hardpoint[] SmallShipHardpoints;
    public Hardpoint[] MediumShipHardpoints;
    public Hardpoint[] LargeShipHardpoints;
}