using UnityEngine;

namespace XDay.UtilityAPI
{
    public class AutoScale : MonoBehaviour
    {
        public bool Inited => m_ScaleCalculator != null;

        public void Init(AutoScaleCalculator calculator)
        {
            m_ScaleCalculator = calculator;
        }

        private void Update()
        {
            if (m_ScaleCalculator == null)
            {
                return;
            }

            if (m_ScaleCalculator.CurrentScale != 0)
            {
                transform.localScale = m_ScaleCalculator.CurrentScale * Vector3.one;
            }
        }

        public void Uninit()
        {
            m_ScaleCalculator = null;
        }

        private AutoScaleCalculator m_ScaleCalculator;
    }
}
