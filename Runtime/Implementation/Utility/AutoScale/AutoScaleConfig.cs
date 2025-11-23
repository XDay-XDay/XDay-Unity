using UnityEngine;

namespace XDay.UtilityAPI
{
    [CreateAssetMenu(fileName = "AutoScaleConfig", menuName = "XDay/AutoScaleConfig")]
    public class AutoScaleConfig : ScriptableObject
    {
        /// <summary>
        /// 开始缩放的相机高度
        /// </summary>
        public float StartScaleCameraAltitude = 550;
        /// <summary>
        /// 结束缩放的相机高度
        /// </summary>
        public float EndScaleCameraAltitude = 700;
        public float BaseScale = 1f;
        public float CameraAltitudeWhenScaleIsBaseScale = 18;
        public float CameraFOVWhenScaleIsBaseScale = 30;
        public float ExtraScaleMultiplier = 0;
    }
}
