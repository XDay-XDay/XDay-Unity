/*
 * Copyright (c) 2024 XDay
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

using XDay.RenderingAPI.BRG;
using UnityEngine;

public class BatchTest : MonoBehaviour
{
    public Mesh Mesh;
    public Material Material;

    void Start()
    {
        m_GPUBatchManager = IGPUBatchManager.Create();

        CreateStaticBatch();
        CreateDynamicBatch();

        m_Debugger = new GameObject("Debugger");
        m_Debugger.transform.parent = transform;
        var debugger = m_Debugger.AddComponent<GPUBatchManagerDebugger>();
        debugger.Init(m_GPUBatchManager);
    }

    // EnableClampXZ is called once per frame
    void Update()
    {
        m_GPUBatchManager.Sync();

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            var batch = m_GPUBatchManager.GetBatch(0);
            if (batch != null)
            {
                batch.RemoveInstance(1023);
            }
        }
    }

    private void OnDestroy()
    {
        m_GPUBatchManager.OnDestroy();
        Object.Destroy(m_Debugger);
    }

    private void CreateStaticBatch()
    {
        IGPUBatchCreateInfo createInfo = IGPUBatchCreateInfo.Create(1024, Mesh, 0, Material, properties: null);
        var batch = m_GPUBatchManager.CreateBatch<GPUStaticBatch>(createInfo);

        for (var i = 0; i < 10; ++i)
        {
            var scale = Random.value * Vector3.one * 2;
            batch.AddInstance(new Vector3(Random.value * 10, 2, 0), Random.rotation, scale);
        }
    }

    private void CreateDynamicBatch()
    {
        IGPUBatchCreateInfo createInfo = IGPUBatchCreateInfo.Create(1024, Mesh, 0, Material, properties: null);
        var batch = m_GPUBatchManager.CreateBatch<GPUDynamicBatch>(createInfo);

        for (var i = 0; i < 10; ++i)
        {
            var pos = new Vector3(Random.value * 10, 6, 0);
            var scale = Random.value * Vector3.one * 4;
            var rot = Random.rotation;

            batch.AddInstance(pos, rot, scale);
        }
    }

    private IGPUBatchManager m_GPUBatchManager;
    private GameObject m_Debugger;
}
