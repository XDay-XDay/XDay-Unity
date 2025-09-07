using UnityEngine;

namespace XDay.GUIAPI
{
    public enum DynamicAtlasGroup
    {
        Size_256 = 256,
        Size_512 = 512,
        Size_1024 = 1024,
        Size_2048 = 2048
    }

    public delegate void OnCallBackTexRect(Texture tex, Rect rect, Vector2 alignmentMinOffset);

    internal static class AtlasHelper
    {
        public static int CeilRound(float v, int alignment)
        {
            return Mathf.CeilToInt(v / alignment) * alignment;
        }

        public static int CeilRound(int v, int alignment)
        {
            if (v % alignment == 0)
            {
                return v;
            }

            return Mathf.CeilToInt(v / alignment) * alignment;
        }

        public static int FloorRound(float v, int alignment)
        {
            return Mathf.FloorToInt(v / alignment) * alignment;
        }

        public static int FloorRound(int v, int alignment)
        {
            if (v % alignment == 0)
            {
                return v;
            }

            return Mathf.FloorToInt(v / alignment) * alignment;
        }

        public static TextureFormat GetTextureFormat()
        {
#if UNITY_EDITOR
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            if (target == UnityEditor.BuildTarget.Android ||
                target == UnityEditor.BuildTarget.iOS)
            {
                return TextureFormat.ASTC_6x6;
            }
            return TextureFormat.DXT5;
#else
            return TextureFormat.ASTC_6x6;
#endif
        }

        public static int GetAlignment()
        {
#if UNITY_EDITOR
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            if (target == UnityEditor.BuildTarget.Android ||
                target == UnityEditor.BuildTarget.iOS)
            {
                return 6;
            }
            return 4;
#else
            return 6;
#endif
        }
    }
}