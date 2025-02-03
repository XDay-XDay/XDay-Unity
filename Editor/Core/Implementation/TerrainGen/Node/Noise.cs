

using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    class Noise : TerrainModifier
    {
        public enum HeightMode
        {
            Set,
            Add,
            Subtract,
            Multiply,
        };

        public class BasicParam
        {
            public int octave = 6;
            public float lacunarity = 2.0f;
            public float gain = 0.5f;
            public float frequency = 0.01f;
            public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.FBm;
        };

        public class NoiseParam
        {
            public BasicParam basicParam = new();
            //only apply noise when height map value >= this value
            public float applyNoiseHeightThreshold = 0;
            public float noiseMultiplier = 1.0f;
            public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.Perlin;
        };

        public class WarpParam
        {
            public BasicParam basicParam = new();
            public FastNoiseLite.DomainWarpType warpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
            public float amplitude = 50;
        };

        public class Setting : ITerrainModifierSetting
        {
            public int seed = 1337;
            public HeightMode heightMode = HeightMode.Set;
            public NoiseParam noise = new();
            public WarpParam warp = new();

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteInt32(seed, "Seed");
                writer.WriteEnum(heightMode, "Height Mode");
                writer.WriteStructure("Noise", () =>
            {
                SaveNoise(writer, noise);
            });

                writer.WriteStructure("Warp", () =>
            {
                SaveWarp(writer, warp);
            });
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                seed = reader.ReadInt32("Seed");
                heightMode = reader.ReadEnum<HeightMode>("Height Mode");
                reader.ReadStructure("Noise", () =>
            {
                LoadNoise(reader, noise);
            });

                reader.ReadStructure("Warp", () =>
            {
                LoadWarp(reader, warp);
            });
            }

            private void SaveBasicParam(ISerializer writer, BasicParam param)
            {
                writer.WriteSingle(param.frequency, "Frequency");
                writer.WriteSingle(param.gain, "Gain");
                writer.WriteSingle(param.lacunarity, "Lacunarity");
                writer.WriteInt32(param.octave, "Octave");
                writer.WriteEnum(param.fractalType, "Fractal Type");
            }

            private void LoadBasicParam(IDeserializer reader, BasicParam param)
            {
                param.frequency = reader.ReadSingle("Frequency");
                param.gain = reader.ReadSingle("Gain");
                param.lacunarity = reader.ReadSingle("Lacunarity");
                param.octave = reader.ReadInt32("Octave");
                param.fractalType = reader.ReadEnum<FastNoiseLite.FractalType>("Fractal Type");
            }

            private void SaveNoise(ISerializer writer, NoiseParam param)
            {
                writer.WriteStructure("Basic", () =>
                {
                    SaveBasicParam(writer, param.basicParam);
                });
                writer.WriteSingle(param.applyNoiseHeightThreshold, "Apply Noise Height Threshold");
                writer.WriteSingle(param.noiseMultiplier, "Noise Multiplier");
                writer.WriteEnum(param.noiseType, "Noise Type");
            }

            private void LoadNoise(IDeserializer reader, NoiseParam param)
            {
                reader.ReadStructure("Basic", () =>
            {
                LoadBasicParam(reader, param.basicParam);
            });
                param.applyNoiseHeightThreshold = reader.ReadSingle("Apply Noise Height Threshold");
                param.noiseMultiplier = reader.ReadSingle("Noise Multiplier");
                param.noiseType = reader.ReadEnum<FastNoiseLite.NoiseType>("Noise Type");
            }

            private void SaveWarp(ISerializer writer, WarpParam param)
            {
                writer.WriteStructure("Basic", () =>
                    {
                        SaveBasicParam(writer, param.basicParam);
                    });
                writer.WriteSingle(param.amplitude, "Amplitude");
                writer.WriteEnum(param.warpType, "Warp Type");
            }

            private void LoadWarp(IDeserializer reader, WarpParam param)
            {
                reader.ReadStructure("Basic", () =>
            {
                LoadBasicParam(reader, param.basicParam);
            });
                param.amplitude = reader.ReadSingle("Amplitude");
                param.warpType = reader.ReadEnum<FastNoiseLite.DomainWarpType>("Warp Type");
            }

            private const int m_Version = 1;
        };

        public Noise(int id) : base(id) { }
        public Noise() { }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            mHeightData.Clear();
            var heightData = GetInputModifier(0).GetHeightData();
            if (heightData != null)
            {
                mHeightData.AddRange(heightData);
            }

            mNoise.SetSeed(setting.seed);
            mNoise.SetDomainWarpAmp(setting.warp.amplitude);
            mNoise.SetNoiseType(setting.noise.noiseType);
            mNoise.SetDomainWarpType(setting.warp.warpType);

            var size = GetSize();
            float stepX = size.GetStepX();
            float stepY = size.GetStepY();

            for (var i = 0; i < size.Resolution; ++i)
            {
                for (var j = 0; j < size.Resolution; ++j)
                {
                    float value = GetNoise(j * stepX, i * stepY);
                    var idx = i * size.Resolution + j;
                    float height = mHeightData[idx];
                    if (Helper.GE(height, setting.noise.applyNoiseHeightThreshold))
                    {
                        if (setting.heightMode == HeightMode.Set)
                        {
                            mHeightData[idx] = value;
                        }
                        else if (setting.heightMode == HeightMode.Add)
                        {
                            mHeightData[idx] += value;
                        }
                        else if (setting.heightMode == HeightMode.Multiply)
                        {
                            mHeightData[idx] *= value;
                        }
                        else if (setting.heightMode == HeightMode.Subtract)
                        {
                            mHeightData[idx] -= value;
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                }
            }

            SetMaskDataToHeightMap();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.seed = EditorGUILayout.IntField("Seed", setting.seed);
            setting.heightMode = (HeightMode)EditorGUILayout.EnumPopup("Height Mode", setting.heightMode);

            DrawNoise();

            DrawWarp();
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            var setting = new Setting();
            setting.warp.basicParam.fractalType = FastNoiseLite.FractalType.DomainWarpProgressive;
            return setting;
        }

        public override string GetName()
        {
            return "Noise";
        }

        private float GetNoise(float x, float y)
        {
            var setting = GetSetting() as Setting;

            mNoise.SetFrequency(setting.warp.basicParam.frequency);
            mNoise.SetFractalGain(setting.warp.basicParam.gain);
            mNoise.SetFractalLacunarity(setting.warp.basicParam.lacunarity);
            mNoise.SetFractalOctaves(setting.warp.basicParam.octave);
            mNoise.SetFractalType(setting.warp.basicParam.fractalType);
            mNoise.DomainWarp(ref x, ref y);

            mNoise.SetFrequency(setting.noise.basicParam.frequency);
            mNoise.SetFractalGain(setting.noise.basicParam.gain);
            mNoise.SetFractalLacunarity(setting.noise.basicParam.lacunarity);
            mNoise.SetFractalOctaves(setting.noise.basicParam.octave);
            mNoise.SetFractalType(setting.noise.basicParam.fractalType);
            return mNoise.GetNoise(x, y) * setting.noise.noiseMultiplier;
        }

        private void DrawBasic(BasicParam param, bool warp)
        {
            param.octave = EditorGUILayout.IntField("Octave", param.octave);
            param.lacunarity = EditorGUILayout.FloatField("Lacunarity", param.lacunarity);
            param.gain = EditorGUILayout.FloatField("Gain", param.gain);
            param.frequency = EditorGUILayout.FloatField("Frequency", param.frequency);

            if (warp)
            {
                param.fractalType = (FastNoiseLite.FractalType)EditorGUILayout.EnumPopup("Fractal Type", param.fractalType);
            }
            else
            {
                param.fractalType = (FastNoiseLite.FractalType)EditorGUILayout.EnumPopup("Fractal Type", param.fractalType);
            }
        }

        private void DrawNoise()
        {
            m_ShowNoise = EditorGUILayout.Foldout(m_ShowNoise, "Noise");
            if (m_ShowNoise)
            {
                var setting = GetSetting() as Setting;

                DrawBasic(setting.noise.basicParam, false);

                setting.noise.applyNoiseHeightThreshold = EditorGUILayout.FloatField("Apply Noise Height Threshold", setting.noise.applyNoiseHeightThreshold);
                setting.noise.noiseMultiplier = EditorGUILayout.FloatField("Noise Multiplier", setting.noise.noiseMultiplier);
                setting.noise.noiseType = (FastNoiseLite.NoiseType)EditorGUILayout.EnumPopup("Noise Type", setting.noise.noiseType);
            }
        }

        private void DrawWarp()
        {
            m_ShowWarp = EditorGUILayout.Foldout(m_ShowWarp, "Warp");
            if (m_ShowWarp)
            {
                var setting = GetSetting() as Setting;

                DrawBasic(setting.warp.basicParam, true);

                setting.warp.amplitude = EditorGUILayout.FloatField("Amplitude", setting.warp.amplitude);
                setting.warp.warpType = (FastNoiseLite.DomainWarpType)EditorGUILayout.EnumPopup("Warp Type", setting.warp.warpType);
            }
        }
        private FastNoiseLite mNoise = new();
        private bool m_ShowNoise = true;
        private bool m_ShowWarp = true;
    };
}