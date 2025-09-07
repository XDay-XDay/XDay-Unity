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

using UnityEngine;
using UnityEngine.UI;
using XDay.DisplayKeyAPI;

namespace XDay.GUIAPI
{
    [AddComponentMenu("XDay/UI/Image", 0)]
    public class UIImage : Image
    {
        public bool EnableDynamicAtlas
        {
            get
            {
                return m_EnableDynamicAtlas;
            }
            set
            {
                m_EnableDynamicAtlas = value;
            }
        }

        public DynamicAtlasGroup AtlasGroup { get => m_AtlasGroup; set => m_AtlasGroup = value; }

        protected override void Awake()
        {
            base.Awake();
            m_LastSprite = sprite;
        }

        internal void ResetSprite()
        {
            DynamicAtlasMgr.S.RemoveFromQueue(this);

            if (m_AtlasSprite != null)
            {
                sprite = m_SpriteWhenBuildAtlas;
                m_LastSprite = sprite;
                UnityEngine.Object.Destroy(m_AtlasSprite);
                m_AtlasSprite = null;
                m_Atlas = null;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (m_EnableDynamicAtlas && CanPack())
            {
                DynamicAtlasMgr.S.AddToQueue(this);
            }
            m_LastSprite = sprite;
        }

        private bool CanPack()
        {
            return
                (ReferenceEquals(m_AtlasSprite, null) ||
                !ReferenceEquals(m_LastSprite, sprite)) &&
                sprite != null &&
                sprite.packed;
        }

        internal void Process()
        {
            if (!m_EnableDynamicAtlas)
            {
                return;
            }

            if (sprite != null && sprite.packed == true)
            {
                var atlas = GetAtlas();
                if (atlas == null || !atlas.EnableDynamicAtlas)
                {
                    m_EnableDynamicAtlas = false;
                    return;
                }

                SetGroup(m_AtlasGroup, atlas);
                SetImage();
            }
        }

        private void SetGroup(DynamicAtlasGroup group, DynamicAtlasController atlas)
        {
            if (m_Atlas != null)
            {
                return;
            }

            m_Atlas = DynamicAtlasMgr.S.GetDynamicAtlas(atlas, group);
        }

        private DynamicAtlasController GetAtlas()
        {
            return transform.GetComponentInParent<DynamicAtlasController>();
        }

        private void SetImage()
        {
            m_SpriteWhenBuildAtlas = sprite;
            m_Atlas.SetTexture(sprite, OnGetImageCallBack);
        }

        private void OnGetImageCallBack(Texture tex, Rect rect, Vector2 alignmentMinOffset)
        {
            if (m_SpriteWhenBuildAtlas != null)
            {
                var min = rect.min + alignmentMinOffset;
                var max = min + sprite.textureRect.size;
                max.x = Mathf.Clamp(max.x, 0, tex.width - 1);
                max.y = Mathf.Clamp(max.y, 0, tex.height - 1);
                sprite = Sprite.Create((Texture2D)tex, new Rect(min, max - min), m_SpriteWhenBuildAtlas.pivot, m_SpriteWhenBuildAtlas.pixelsPerUnit, 1, SpriteMeshType.Tight, m_SpriteWhenBuildAtlas.border);
                m_AtlasSprite = sprite;
            }
            else
            {
                sprite = Sprite.Create((Texture2D)tex, rect, new Vector2(rect.width * .5f, rect.height * .5f), 100, 1, SpriteMeshType.Tight, new Vector4(0, 0, 0, 0));
                m_AtlasSprite = sprite;
            }
        }

        private bool m_Grey = false;
        private DynamicAtlas m_Atlas;
        private Sprite m_AtlasSprite;
        private Sprite m_LastSprite;
        private Sprite m_SpriteWhenBuildAtlas;
        [SerializeField]
        private DynamicAtlasGroup m_AtlasGroup = DynamicAtlasGroup.Size_2048;
        //是否使用动态图集
        bool m_EnableDynamicAtlas = true;
    }

    public static class ImageExtension
    {
        public static void SetImage(this Image image, int key)
        {
            var path = key.GetIconPath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"SetImage failed, Load DisplayKey {key} failed");
            }
            else
            {
                image.sprite = UIWindowManager.Instance.AssetLoader.Load<Sprite>(path);
            }
        }
    }
}
