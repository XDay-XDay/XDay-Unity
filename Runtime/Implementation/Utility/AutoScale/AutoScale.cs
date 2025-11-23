using UnityEngine;
using XDay.CameraAPI;

namespace XDay.UtilityAPI
{
    public class AutoScale : MonoBehaviour
    {
        public ulong EntityID;
        public AutoScaleConfig[] ScaleConfigs;

        public void Init(ICameraManipulator camera, ulong entityID)
        {
            Debug.Assert(entityID != 0);
            if (entityID == 0)
            {
                int a = 1;
            }
            EntityID = entityID;
            m_ScaleCalculator = new(camera, ScaleConfigs);
        }

        public void ForceUpdate()
        {
            DoUpdate(true);
        }

        private void Update()
        {
            if (m_ScaleCalculator == null)
            {
                return;
            }

            DoUpdate(false);
        }

        private void DoUpdate(bool forceUpdate)
        {
            m_ScaleCalculator.Update(forceUpdate);
            if (m_ScaleCalculator.CurrentScale != 0)
            {
                transform.localScale = Vector3.one * m_ScaleCalculator.CurrentScale;
            }
            else
            {
                int a = 1;
            }
        }

        private AutoScaleCalculator m_ScaleCalculator;
    }
}
