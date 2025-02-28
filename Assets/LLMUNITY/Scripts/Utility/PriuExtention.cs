using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Priu.LlmUnity
{
    public static class Utility
    {
        public static string LogColor(this string log, Color color)
        {
            return $"<color=#{ColorToHexString(color)}>{log}</color>";
        }

        public static string ColorToHexString(Color color)
        {
            int r = (int)(color.r * 255);
            int g = (int)(color.g * 255);
            int b = (int)(color.b * 255);

            return string.Format("{0:X2}{1:X2}{2:X2}", r, g, b);
        }


        public static void OpenPathInExplorer(string path)
        {
            string folderPath = Path.GetDirectoryName(path); // 폴더 경로 추출

            if (Directory.Exists(folderPath))
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Debug.LogError($"폴더를 찾을 수 없습니다: {folderPath}");
            }
        }
    }


    public static class ColorSamples
    {
        public static readonly Color Orange = new Color(1.0f, 0.5f, 0.0f);
        public static readonly Color Red = new Color(1.0f, 0.0f, 0.0f);
        public static readonly Color Green = new Color(0.0f, 1.0f, 0.0f);
        public static readonly Color Blue = new Color(0.0f, 0.0f, 1.0f);
        public static readonly Color Yellow = new Color(1.0f, 1.0f, 0.0f);
        public static readonly Color Purple = new Color(0.5f, 0.0f, 1.0f);
        public static readonly Color Pink = new Color(1.0f, 0.0f, 0.5f);
        public static readonly Color SkyBlue = new Color(0.53f, 0.81f, 0.92f);
        public static readonly Color Gray = new Color(0.5f, 0.5f, 0.5f);
        public static readonly Color Brown = new Color(0.6f, 0.4f, 0.2f);
        public static readonly Color Cyan = new Color(0.0f, 1.0f, 1.0f);
        public static readonly Color Magenta = new Color(1.0f, 0.0f, 1.0f);
        public static readonly Color Lime = new Color(0.75f, 1.0f, 0.0f);
        public static readonly Color Maroon = new Color(0.5f, 0.0f, 0.0f);
        public static readonly Color Olive = new Color(0.5f, 0.5f, 0.0f);
        public static readonly Color Teal = new Color(0.0f, 0.5f, 0.5f);
        public static readonly Color Navy = new Color(0.0f, 0.0f, 0.5f);
        public static readonly Color Coral = new Color(1.0f, 0.5f, 0.31f);
        public static readonly Color Turquoise = new Color(0.25f, 0.88f, 0.82f);
        public static readonly Color Indigo = new Color(0.29f, 0.0f, 0.51f);
        public static readonly Color Violet = new Color(0.56f, 0.0f, 1.0f);
        public static readonly Color Salmon = new Color(0.98f, 0.5f, 0.45f);
        public static readonly Color Khaki = new Color(0.76f, 0.69f, 0.57f);
        public static readonly Color Gold = new Color(1.0f, 0.84f, 0.0f);
        public static readonly Color Beige = new Color(0.96f, 0.96f, 0.86f);
        public static readonly Color Mint = new Color(0.24f, 0.71f, 0.54f);
        public static readonly Color Lavender = new Color(0.71f, 0.49f, 0.86f);
        public static readonly Color Peach = new Color(1.0f, 0.8f, 0.64f);
        public static readonly Color Ivory = new Color(1.0f, 1.0f, 0.94f);
        public static readonly Color Azure = new Color(0.0f, 0.5f, 1.0f);
    }
}