using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

public class SpriteSheetImporter : EditorWindow
{
    private Texture2D spriteSheet;
    private TextAsset jsonFile;
    private bool flipY = true;
    private bool flipX = false; // Add X flip option

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
        flipX = EditorGUILayout.Toggle("Flip X Coordinates", flipX); // Add X flip toggle

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

        // Handle different JSON formats
        if (jsonData["frames"] != null)
        {
            frames = jsonData["frames"] as JObject;
        }
        else if (jsonData["sprites"] != null)
        {
            frames = new JObject();
            var sprites = jsonData["sprites"] as JArray;
            
            foreach (var sprite in sprites)
            {
                var frameObj = new JObject();
                int spriteX = sprite["x"].Value<int>();
                int spriteY = sprite["y"].Value<int>();
                
                frameObj["frame"] = new JObject
                {
                    ["x"] = spriteX,
                    ["y"] = spriteY,
                    ["w"] = sprite["width"],
                    ["h"] = sprite["height"]
                };

                string spriteName = sprite["fileName"].ToString();
                frames[spriteName] = frameObj;
            }
        }
        else if (jsonData["textures"] != null)
        {
            frames = new JObject();
            var textureFrames = jsonData["textures"][0]["frames"] as JArray;
            foreach (var frame in textureFrames)
            {
                frames[frame["filename"].ToString()] = frame;
            }
        }
        else
        {
            Debug.LogError("Unsupported JSON format");
            return;
        }

        if (frames == null) return;

        float textureHeight = spriteSheet.height;
        float textureWidth = spriteSheet.width;
        List<SpriteRect> spriteRects = new List<SpriteRect>();

        foreach (var prop in frames.Properties())
        {
            var spriteName = prop.Name;
            var frameData = prop.Value["frame"];

            float x = frameData["x"].Value<float>();
            float y = frameData["y"].Value<float>();
            float width = frameData["w"].Value<float>();
            float height = frameData["h"].Value<float>();

            // Handle coordinate flipping
            if (flipY)
            {
                y = textureHeight - y - height;
            }
            if (flipX)
            {
                x = textureWidth - x - width;
            }

            var spriteRect = new SpriteRect
            {
                name = spriteName.Replace(".png", ""),
                alignment = SpriteAlignment.Center,
                pivot = Vector2.one * 0.5f,
                rect = new Rect(x, y, width, height),
                border = Vector4.zero,
                spriteID = GUID.Generate()
            };
            
            spriteRects.Add(spriteRect);
            Debug.Log($"Created sprite rect: {spriteRect.name} at X:{x}, Y:{y}, Width:{width}, Height:{height}");
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        EditorUtility.SetDirty(textureImporter);
        textureImporter.SaveAndReimport();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}