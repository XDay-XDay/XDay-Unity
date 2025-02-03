

using UnityEngine;

namespace XDay.UtilityAPI
{
    internal class Mover : IMover
    {
        public Vector2 OldPosition => m_OldPosition;
        public Vector2 NewPosition => m_NewPosition;

        public Mover()
        {
            Reset();
        }

        public void Update(Vector2 newPos)
        {
            if (m_OldPosition.x < INVALID_VALUE + 10.0f)
            {
                m_OldPosition = newPos;
            }
            else
            {
                m_OldPosition = m_NewPosition;
            }
            m_NewPosition = newPos;
        }

        public void Reset()
        {
            m_OldPosition.Set(INVALID_VALUE, INVALID_VALUE);
            m_NewPosition = m_OldPosition;
        }

        public Vector2 GetMovement()
        {
            if (m_OldPosition.x == INVALID_VALUE)
            {
                return Vector2.zero;
            }
            return m_NewPosition - m_OldPosition;
        }

        private Vector2 m_OldPosition;
        private Vector2 m_NewPosition;
        private const float INVALID_VALUE = -100000;
    };
}
