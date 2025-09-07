﻿using UnityEngine;
using static UnityEngine.UI.Image;

namespace XDay.RenderingAPI
{
    [ExecuteInEditMode]
    public class SpriteRendererEx : MonoBehaviour
    {
        public FillMethod FillMethod;
        public int RenderQueue = 0;
        public SpriteRenderer SpriteRenderer => m_SpriteRenderer;

        public float FillAmount
        {
            get => m_FillAmount;
            set
            {
                if (m_FillAmount != value)
                {
                    m_Dirty = true;
                    m_FillAmount = Mathf.Clamp01(value);
                }
            }
        }

        private void Awake()
        {
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (m_UseSRPBatcher)
            {
                var _ = m_SpriteRenderer.material;
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif

            Object.Destroy(m_SpriteRenderer.sharedMaterial);
        }

        private void Update()
        {
            if (!m_Dirty)
            {
                return;
            }

            m_Dirty = false;

            Vector4 uvOrigin = Vector4.zero;
            var sprite = m_SpriteRenderer.sprite;
            var width = sprite.texture.width;
            var height = sprite.texture.height;
            if (FillMethod == FillMethod.Radial360)
            {
                var center = sprite.textureRect.center;
                uvOrigin = new Vector4(center.x / width, center.y / height, 0, 0);
            }
            else if (FillMethod == FillMethod.Horizontal)
            {
                var min = sprite.textureRect.min;
                uvOrigin = new Vector4(min.x / width, min.y / height, sprite.textureRect.width / width, 0);
            }
            else
            {
                Debug.Assert(false, $"Todo: {FillMethod}");
            }

            if (m_UseSRPBatcher)
            {
                var mtl = m_SpriteRenderer.sharedMaterial;
                mtl.SetFloat("_FillAmount", m_FillAmount);
                mtl.SetVector("_UVRectCenter", uvOrigin);
                if (RenderQueue > 0)
                {
                    mtl.renderQueue = RenderQueue;
                }
            }
            else
            {
                m_Block ??= new();

                m_SpriteRenderer.GetPropertyBlock(m_Block);
                m_Block.SetFloat("_FillAmount", m_FillAmount);
                m_Block.SetVector("_UVRectCenter", uvOrigin);
                m_SpriteRenderer.SetPropertyBlock(m_Block);
            }
        }

        private SpriteRenderer m_SpriteRenderer;
        private MaterialPropertyBlock m_Block;
        private float m_FillAmount;
        private bool m_Dirty = true;
        private bool m_UseSRPBatcher = true;
    }
}
