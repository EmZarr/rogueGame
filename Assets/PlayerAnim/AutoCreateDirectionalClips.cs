using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class AutoCreateDirectionalClips : EditorWindow
{
    //change this depending on where you want clips to be created
    private const string BasePath = "Assets/EnemyAnim/Guardian";

    // Folders directly under: Assets/EnemyAnim/Melee/
    private static readonly string[] MeleeFolders =
    {
        "attack",
        "dash",
        "defensive",
        "die",
        "hurt",
        "idle",
        "running",
        "runningAttack",
        "special"
    };

    // Subfolders under: Assets/EnemyAnim/Melee/running/
    private static readonly string[] RunningSubfolders =
    {
        "RunF",
        "RunB",
        "StrafeL",
        "StrafeR"
    };

    // Direction names (8-dir)
    private static readonly string[] DirNames =
    {
        "Right", "DownRight", "Down", "DownLeft",
        "Left", "UpLeft", "Up", "UpRight"
    };

    

    // UI state
    private int folderIndex = 0;
    private int runningSubfolderIndex = 0;

    // Settings
    private int tileSize = 64;
    private int framesPerDir = 15;
    private float clipFrameRate = 12f;

    [MenuItem("Tools/Animations/Create Directional Clips...")]
    private static void Open()
    {
        var w = GetWindow<AutoCreateDirectionalClips>("Directional Clips");
        w.minSize = new Vector2(420, 270);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        folderIndex = EditorGUILayout.Popup("Melee Folder", folderIndex, MeleeFolders);

        bool isRunning = MeleeFolders[folderIndex] == "running";
        if (isRunning)
        {
            runningSubfolderIndex = EditorGUILayout.Popup("Running Subfolder", runningSubfolderIndex, RunningSubfolders);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Slicing", EditorStyles.boldLabel);

        tileSize = EditorGUILayout.IntField("Tile Size (px)", tileSize);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

        framesPerDir = EditorGUILayout.IntField("Frames Per Direction", framesPerDir);
        clipFrameRate = EditorGUILayout.FloatField("Clip Framerate", clipFrameRate);

        EditorGUILayout.Space(10);

        string chosenFolder = MeleeFolders[folderIndex];
        string outputFolder = Path.Combine(BasePath, chosenFolder);
        if (chosenFolder == "running")
            outputFolder = Path.Combine(outputFolder, RunningSubfolders[runningSubfolderIndex]);

        outputFolder = outputFolder.Replace("\\", "/");

        EditorGUILayout.HelpBox(
            "Select one or more spritesheets (textures) in Project view.\n\n" +
            $"This tool will:\n" +
            $"1) Auto-slice into {tileSize}x{tileSize} sprites\n" +
            $"2) Create 8-direction clips ({framesPerDir} frames each)\n" +
            $"3) Save clips to:\n   {outputFolder}\n\n" +
            "Clip prefix uses the exact folder name (e.g. attack_Right.anim).",
            MessageType.Info
        );

        bool canRun = Selection.objects != null && Selection.objects.Length > 0;

        using (new EditorGUI.DisabledScope(!canRun))
        {
            if (GUILayout.Button("Slice + Create Clips From Selection", GUILayout.Height(34)))
            {
                CreateClipsForSelection();
            }
        }
    }

    private void CreateClipsForSelection()
    {
        Object[] selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogError("Select at least one spritesheet texture in the Project view.");
            return;
        }

        string chosenFolder = MeleeFolders[folderIndex];

        // Output folder
        string outputFolder = Path.Combine(BasePath, chosenFolder);
        if (chosenFolder == "running")
            outputFolder = Path.Combine(outputFolder, RunningSubfolders[runningSubfolderIndex]);

        outputFolder = outputFolder.Replace("\\", "/");

        // Clip prefix = exact folder name (as requested)
        string clipPrefix = chosenFolder == "running" ? RunningSubfolders[runningSubfolderIndex]: chosenFolder;

        EnsureFolderExists(BasePath);
        EnsureFolderExists(Path.Combine(BasePath, chosenFolder));
        if (chosenFolder == "running")
            EnsureFolderExists(outputFolder);
        else
            EnsureFolderExists(outputFolder);

        int requiredSpriteCount = framesPerDir * DirNames.Length;

        foreach (Object obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            // Only operate on textures
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"Skipping '{path}' (not a Texture2D).");
                continue;
            }

            // 1) Auto-slice
            if (!SliceTextureIntoGrid(path, tileSize))
            {
                Debug.LogError($"Failed slicing: {path}");
                continue;
            }

            // 2) Load sliced sprites
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name) // stable order; you can remove if you rely on Unity import order
                .ToArray();

            if (sprites.Length < requiredSpriteCount)
            {
                Debug.LogError(
                    $"Not enough sprites in '{path}'. Found {sprites.Length}, need at least {requiredSpriteCount} " +
                    $"({framesPerDir} frames x {DirNames.Length} directions)."
                );
                continue;
            }

            // 3) Create 8 directional clips
            for (int d = 0; d < DirNames.Length; d++)
            {
                AnimationClip clip = new AnimationClip
                {
                    frameRate = clipFrameRate
                };

                var binding = EditorCurveBinding.PPtrCurve(
                    "",
                    typeof(SpriteRenderer),
                    "m_Sprite"
                );

                ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[framesPerDir];

                for (int f = 0; f < framesPerDir; f++)
                {
                    int spriteIndex = d * framesPerDir + f;

                    keys[f] = new ObjectReferenceKeyframe
                    {
                        time = f / clipFrameRate,
                        value = sprites[spriteIndex]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

                string clipName = $"{clipPrefix}_{DirNames[d]}.anim";
                string assetPath = Path.Combine(outputFolder, clipName).Replace("\\", "/");

                // Overwrite if exists (so re-running updates clips)
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(clip, existing);
                    EditorUtility.SetDirty(existing);
                }
                else
                {
                    AssetDatabase.CreateAsset(clip, assetPath);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Sliced spritesheets + created directional animation clips!");
    }

    /// <summary>
    /// Slices the texture at assetPath into a grid (tileSize x tileSize) and reimports.
    /// Returns false if invalid size or importer missing.
    /// </summary>
    private static bool SliceTextureIntoGrid(string assetPath, int tileSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Could not get TextureImporter for: " + assetPath);
            return false;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            Debug.LogError("Could not load Texture2D at: " + assetPath);
            return false;
        }

        int texWidth = tex.width;
        int texHeight = tex.height;

        if (tileSize <= 0)
        {
            Debug.LogError("Tile size must be > 0.");
            return false;
        }

        if (texWidth % tileSize != 0 || texHeight % tileSize != 0)
        {
            Debug.LogError(
                $"Texture '{assetPath}' size ({texWidth}x{texHeight}) is not divisible by tileSize {tileSize}."
            );
            return false;
        }

        // Configure import settings for pixel-art slicing
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Optional: set this to your usual PPU if you want; 64 makes 1 tile = 1 unit if tile is 64px
        importer.spritePixelsPerUnit = tileSize;

        int cols = texWidth / tileSize;
        int rows = texHeight / tileSize;

        var metas = new List<SpriteMetaData>(cols * rows);

        // Unity SpriteMetaData rects are in texture pixel space with origin at bottom-left.
        // We generate top-to-bottom rows, left-to-right columns.
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var meta = new SpriteMetaData
                {
                    rect = new Rect(
                        x * tileSize,
                        texHeight - ((y + 1) * tileSize),
                        tileSize,
                        tileSize
                    ),
                    name = $"sprite_{y:D2}_{x:D2}",
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };

                metas.Add(meta);
            }
        }

        importer.spritesheet = metas.ToArray();

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return true;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string name = Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolderExists(parent);

        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
