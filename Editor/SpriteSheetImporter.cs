using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class SpriteSheetImporter : EditorWindow
{
    private Texture2D spriteSheet;
    private TextAsset jsonFile;
    private bool flipY = true; // Add option to flip Y coordinates

    [MenuItem("Tools/Sprite Sheet Importer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSheetImporter>("Sprite Sheet Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Sheet Importer", EditorStyles.boldLabel);
        
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
        jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", jsonFile, typeof(TextAsset), false);
        flipY = EditorGUILayout.Toggle("Flip Y Coordinates", flipY);

        if (GUILayout.Button("Import Sprites") && spriteSheet != null && jsonFile != null)
        {
            ImportSprites();
        }
    }

    private void ImportSprites()
    {
        string path = AssetDatabase.GetAssetPath(spriteSheet);
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (textureImporter == null) return;

        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        
        JObject jsonData = JObject.Parse(jsonFile.text);
        JObject frames;

        // Handle all JSON formats
        if (jsonData["textures"] != null)
        {
            // TexturePacker format with textures array
            frames = new JObject();
            var textureFrames = jsonData["textures"][0]["frames"] as JArray;
            foreach (var frame in textureFrames)
            {
                frames[frame["filename"].ToString()] = frame;
            }
        }
        else if (jsonData["sprites"] != null)
        {
            // Sprite array format
            frames = new JObject();
            var sprites = jsonData["sprites"] as JArray;
            foreach (var sprite in sprites)
            {
                var frameObj = new JObject();
                frameObj["frame"] = new JObject
                {
                    ["x"] = sprite["x"],
                    ["y"] = sprite["y"],
                    ["w"] = sprite["width"],
                    ["h"] = sprite["height"]
                };
                frames[sprite["fileName"].ToString()] = frameObj;
            }
        }
        else
        {
            // Direct frames format
            frames = jsonData["frames"] as JObject;
        }

        if (frames == null) return;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
        dataProvider.InitSpriteEditorDataProvider();

        float textureHeight = spriteSheet.height;
        List<SpriteRect> rects = new List<SpriteRect>();
        
        foreach (var prop in frames.Properties())
        {
            var spriteName = prop.Name.Replace(".png", ""); // Remove .png extension if present
            var frameData = prop.Value["frame"];
            
            float x = frameData["x"].Value<float>();
            float y = frameData["y"].Value<float>();
            float width = frameData["w"].Value<float>();
            float height = frameData["h"].Value<float>();

            // If flipY is true, convert Y coordinate from top-left to bottom-left origin
            if (flipY)
            {
                y = textureHeight - (y + height);
            }

            var rect = new SpriteRect
            {
                name = spriteName,
                alignment = SpriteAlignment.Center,
                pivot = Vector2.one * 0.5f,
                rect = new Rect(x, y, width, height),
                spriteID = GUID.Generate(),
                border = Vector4.zero
            };
            
            rects.Add(rect);
        }

        dataProvider.SetSpriteRects(rects.ToArray());
        dataProvider.Apply();

        EditorUtility.SetDirty(textureImporter);
        textureImporter.SaveAndReimport();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}