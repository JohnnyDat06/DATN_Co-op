using UnityEngine;
using UnityEditor;
using System.IO;

public class VFXTextureGenerator : EditorWindow
{
    [MenuItem("Tools/VFX/Generate Slash Textures")]
    public static void GenerateTextures()
    {
        string folderPath = "Assets/_Game/Art/Textures/VFX";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        CreateClawMask($"{folderPath}/T_Claw_Mask.png", 512, 512);
        CreateSlashNoise($"{folderPath}/T_Slash_Noise.png", 512, 512);
        
        AssetDatabase.Refresh();
        Debug.Log("<color=green>VFX Textures Generated Successfully at: </color>" + folderPath);
    }

    private static void CreateClawMask(string path, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                // Tạo hình lưỡi liềm sắc lẹm
                float centerDist = Mathf.Abs(v - 0.5f);
                float mask = Mathf.Pow(1.0f - centerDist * 4.0f, 2.0f);
                
                // Cắt nhọn hai đầu
                float fade = Mathf.SmoothStep(0, 0.2f, u) * Mathf.SmoothStep(1.0f, 0.7f, u);
                float final = Mathf.Clamp01(mask * fade);

                tex.SetPixel(x, y, new Color(1, 1, 1, final));
            }
        }
        SaveTexture(tex, path);
    }

    private static void CreateSlashNoise(string path, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float scale = 10.0f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                tex.SetPixel(x, y, new Color(sample, sample, sample, 1));
            }
        }
        SaveTexture(tex, path);
    }

    private static void SaveTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        DestroyImmediate(tex);
    }
}
