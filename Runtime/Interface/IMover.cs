

using UnityEngine;

namespace XDay.UtilityAPI
{
    public interface IMover
    {
        static IMover Create()
        {
            return new Mover();
        }

        Vector2 OldPosition { get; }
        Vector2 NewPosition { get; }

        void Update(Vector2 newPos);
        void Reset();
        Vector2 GetMovement();
    }
}
