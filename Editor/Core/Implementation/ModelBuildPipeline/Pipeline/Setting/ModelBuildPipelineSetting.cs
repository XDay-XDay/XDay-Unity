/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Collections.Generic;
using UnityEngine;

namespace XDay.ModelBuildPipeline.Editor
{
    [System.Serializable]
    public class GameObjectParameter
    {
        public string Key;
        public GameObject Prefab;
    }

    [System.Serializable]
    public class FloatParameter
    {
        public string Key;
        public float Value;
    }

    [System.Serializable]
    public class IntParameter
    {
        public string Key;
        public int Value;
    }

    [System.Serializable]
    public class BooleanParameter
    {
        public string Key;
        public bool Value;
    }

    [System.Serializable]
    public class StringParameter
    {
        public string Key;
        public string Value;
    }

    /// <summary>
    /// model build pipeline使用的全局设置,当某些stage没有设置参数时可从这里获取
    /// </summary>
    [CreateAssetMenu(menuName = "XDay/Model/Model Build Pipeline Setting")]
    public class ModelBuildPipelineSetting : ScriptableObject
    {
        public List<GameObjectParameter> GameObjectParameters = new();
        public List<FloatParameter> FloatParameters = new();
        public List<IntParameter> IntParameters = new();
        public List<BooleanParameter> BooleanParameters = new();
        public List<StringParameter> StringParameters = new();
        public Shader DefaultVertexAnimationGPUInstancingBakeShader;
        public Shader DefaultVertexAnimationBRGBakeShader;
        public Shader DefaultRigAnimationGPUInstancingBakeShader;
        public Shader DefaultRigAnimationBRGBakeShader;

        public GameObject GetGameObject(string key)
        {
            foreach (var parameter in GameObjectParameters)
            {
                if (parameter.Key == key) 
                {
                    return parameter.Prefab;
                }
            }
            return null;
        }

        public float GetFloat(string key)
        {
            foreach (var parameter in FloatParameters)
            {
                if (parameter.Key == key)
                {
                    return parameter.Value;
                }
            }
            return 0;
        }

        public int GetInt(string key)
        {
            foreach (var parameter in IntParameters)
            {
                if (parameter.Key == key)
                {
                    return parameter.Value;
                }
            }
            return 0;
        }

        public bool GetBoolean(string key)
        {
            foreach (var parameter in BooleanParameters)
            {
                if (parameter.Key == key)
                {
                    return parameter.Value;
                }
            }
            return false;
        }

        public string GetString(string key)
        {
            foreach (var parameter in StringParameters)
            {
                if (parameter.Key == key)
                {
                    return parameter.Value;
                }
            }
            return "";
        }
    }
}
