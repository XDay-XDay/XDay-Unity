

using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    /// <summary>
    /// fault formation algorithm
    /// </summary>
    class Fault : TerrainModifier
    {
        public enum Mode
        {
            Step,
            Sin,
            Cos,
        };

        public class Setting : ITerrainModifierSetting
        {
            public int seed = 1337;
            public int iterationCount = 500;
            public float falloff = 10;
            public Mode mode = Mode.Sin;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteInt32(seed, "Seed");
                writer.WriteInt32(iterationCount, "Iteration Count");
                writer.WriteEnum(mode, "Mode");
                writer.WriteSingle(falloff, "Falloff");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                seed = reader.ReadInt32("Seed");
                iterationCount = reader.ReadInt32("Iteration Count");
                mode = reader.ReadEnum<Mode>("Mode");
                falloff = reader.ReadSingle("Falloff");
            }

            private const int m_Version = 1;
        };

        public Fault(int id) : base(id)
        {
        }

        public Fault()
        {
        }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            m_Random = IRandom.Create(setting.seed);
            mHeightData.Clear();

            var size = GetSize();

            var n = size.Resolution * size.Resolution;
            Helper.Resize(mHeightData, n);
            for (var i = 0; i < n; ++i)
            {
                mHeightData.Add(0);
            }

            for (var iter = 0; iter < setting.iterationCount; ++iter)
            {
                GetRandomPoints(out var start, out var end, size.Width, size.Height);
                Vector2 dir = end - start;
                dir.Normalize();

                float deltaHeight = 1 - (float)iter / setting.iterationCount;

                for (var i = 0; i < size.Resolution; ++i)
                {
                    for (var j = 0; j < size.Resolution; ++j)
                    {
                        var idx = i * size.Resolution + j;

                        Vector2 pos = new Vector2(j, i);

                        if (setting.mode == Mode.Cos)
                        {
                            CosFunc(idx, pos, start, dir, deltaHeight);
                        }
                        else if (setting.mode == Mode.Sin)
                        {
                            SinFunc(idx, pos, start, dir, deltaHeight);
                        }
                        else if (setting.mode == Mode.Step)
                        {
                            StepFunc(idx, pos, start, dir, deltaHeight);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                }
            }

            TerrainGenHelper.NormalizeHeights(mHeightData);

            SetMaskDataToHeightMap();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.seed = EditorGUILayout.IntField("Seed", setting.seed);
            setting.iterationCount = EditorGUILayout.IntField("Iteration Count", setting.iterationCount);
            setting.mode = (Mode)EditorGUILayout.EnumPopup("Mode", setting.mode);
            setting.falloff = EditorGUILayout.FloatField("Falloff", setting.falloff);
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override string GetName()
        {
            return "Fault";
        }

        private void GetRandomPoints(out Vector2 start, out Vector2 end, float width, float height)
        {
            while (true)
            {
                start = new Vector2(m_Random.Value * width, m_Random.Value * height);
                end = new Vector2(m_Random.Value * width, m_Random.Value * height);
                if (start != end)
                {
                    break;
                }
            }
        }

        private void StepFunc(int idx, Vector2 pos, Vector2 start, Vector2 dir, float deltaHeight)
        {
            var ps = pos - start;
            if (ps.x * dir.y - ps.y * dir.x > 0)
            {
                mHeightData[idx] += deltaHeight;
            }
        }

        private void SinFunc(int idx, Vector2 pos, Vector2 start, Vector2 dir, float deltaHeight)
        {
            var setting = GetSetting() as Setting;

            Vector2 normal = new Vector2(dir.y, -dir.x);
            var ps = pos - start;
            float dis = Vector2.Dot(normal, ps);
            if (ps.x * dir.y - ps.y * dir.x > 0)
            {
                float t = Mathf.Clamp01(dis / setting.falloff);
                mHeightData[idx] += Mathf.Sin(t * Mathf.PI * 0.5f) * deltaHeight;
            }
        }

        private void CosFunc(int idx, Vector2 pos, Vector2 start, Vector2 dir, float deltaHeight)
        {
            var setting = GetSetting() as Setting;

            Vector2 normal = new Vector2(dir.y, -dir.x);
            var ps = pos - start;
            float sign = (ps.x * dir.y - ps.y * dir.x > 0) ? 1.0f : -1.0f;
            float dis = Vector2.Dot(normal, ps) * sign;
            float t = Mathf.Clamp01(dis / setting.falloff);
            float c = Mathf.Cos(t * Mathf.PI * 0.5f);
            mHeightData[idx] += c * deltaHeight;
        }

        private IRandom m_Random;
    };
}