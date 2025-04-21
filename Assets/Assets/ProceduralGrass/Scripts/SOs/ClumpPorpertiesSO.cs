using UnityEngine;

namespace ProceduralGrass
{
    [CreateAssetMenu]
    public class ClumpPorpertiesSO : ScriptableObject
    {
        public float height;
        public float heightRandom;
        public float width;
        public float widthRandom;
        public float tilt;
        public float tiltRandom;
        public float bend;
        public float bendRandom;
        [Range(0, 1)] public float moveToCenter;
        [Range(0, 1)] public float pointInSameDirection;
        public float pointInSameDirectionAngle;
        [Range(0, 1)] public float pointInSameDirectionRelativeCenter;
        public float pointInSameDirectionAngleRelativeCenter;
        private void OnValidate()
        {
            GrassRenderer.RefreshStatic(this);
        }
    }
}