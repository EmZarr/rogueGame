using UnityEngine;
using UnityEditor;
using System.Linq;


public class AutoCreateDirectionalClips
{
    [MenuItem("Tools/Animations/Create Directional Clips")]
    static void CreateClips()
    {
        Object[] selection = Selection.objects;
        if (selection.Length == 0)
        {
            Debug.LogError("Select a sliced spritesheet first.");
            return;
        }

        foreach (Object obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToArray();

            int framesPerDir = 15;
            string[] dirNames =
            {
                "Right", "DownRight", "Down", "DownLeft",
                "Left", "UpLeft", "Up", "UpRight"
            };

            for (int d = 0; d < dirNames.Length; d++)
            {
                AnimationClip clip = new AnimationClip
                {
                    frameRate = 12
                };

                EditorCurveBinding binding =
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

                ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[framesPerDir];

                for (int f = 0; f < framesPerDir; f++)
                {
                    keys[f] = new ObjectReferenceKeyframe
                    {
                        time = f / 12f,
                        value = sprites[d * framesPerDir + f]
                    };
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

                AssetDatabase.CreateAsset(
                    clip,
                    $"Assets/PlayerAnim/Knight/Special/Special_{dirNames[d]}.anim"
                );
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Directional animation clips created!");
    }
}
