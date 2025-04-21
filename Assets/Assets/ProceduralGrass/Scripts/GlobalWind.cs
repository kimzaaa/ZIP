using System;
using System.Collections.Generic;
using UnityEngine;
namespace ProceduralGrass
{
    public class GlobalWind : MonoBehaviour
    {
        public static GlobalWind Instance { get; private set; }
        [SerializeField] private Texture2D windTexture;
        [SerializeField] private Vector2 windDirectionSpeed = Vector2.right;
        [SerializeField] private float windTextureScale = 1;
        [SerializeField][Range(-1, 1)] private float windIntensity = 0;
        [SerializeField][Range(0, 0.5f)] private float windContrast = 0f;
        [SerializeField][Range(0, 1)] private float minWindStrength = 0f;
        [SerializeField][Range(0, 1)] private float maxWindStrength = 1f;

        private List<WindValues> windValues;
        private GraphicsBuffer windValuesBuffer;


        public float GetWindAt(float x, float z)
        {
            float elapsedTime = Time.time;

            // Calculate the scrolled wind direction based on elapsed time and wind speed
            Vector2 scrolledWindDirection = -windDirectionSpeed * elapsedTime;

            if (windTexture != null)
            {
                // Use the grayscale texture for wind if available
                float u = (x / windTextureScale + scrolledWindDirection.x) % 1;
                float v = (z / windTextureScale + scrolledWindDirection.y) % 1;

                Color pixel = windTexture.GetPixelBilinear(u, v);
                float windStrength = pixel.grayscale;
                return BubbleWind(windStrength, windIntensity, windContrast, minWindStrength, maxWindStrength);
            }
            else
            {
                // Use Perlin noise if no texture is set
                // Note: Not Compatible with grass.
                float windStrength = Mathf.PerlinNoise((x + scrolledWindDirection.x) / windTextureScale, (z + scrolledWindDirection.y) / windTextureScale);
                return BubbleWind(windStrength, windIntensity, windContrast, minWindStrength, maxWindStrength);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                windValuesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 7 * sizeof(float));
                UpdateBuffer();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void OnValidate()
        {
            if (Instance != null) UpdateBuffer();
        }

        private void UpdateBuffer()
        {
            windValues = new List<WindValues>()
        {
            new WindValues(windDirectionSpeed,windTextureScale,windIntensity,windContrast,minWindStrength,maxWindStrength)
        };
            windValuesBuffer.SetData(windValues);
        }
        private void OnDestroy()
        {
            windValuesBuffer?.Dispose();
        }
        public GraphicsBuffer GetWindBuffer()
        {
            return windValuesBuffer;
        }
        private float BubbleWind(float windStrength, float windIntensity, float windContrast, float minWindStrength, float maxWindStrength)
        {
            windStrength = Mathf.Clamp01(windStrength + windIntensity);
            if (windStrength > 1 - windContrast)
            {
                windStrength = 1;
            }
            else if (windStrength < windContrast)
            {
                windStrength = 0;
            }
            // Combine the time-dependent and time-independent components
            return Mathf.Lerp(minWindStrength, maxWindStrength, windStrength);
        }

        public Texture2D GetTexture2D()
        {
            return windTexture;
        }
        public Vector2 GetWindDirectionSpeed()
        {
            return windDirectionSpeed;
        }
        public static GraphicsBuffer GetDefaultWindBuffer()
        {
            GraphicsBuffer defaultBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 7 * sizeof(float));
            List<WindValues> defaultValues = new List<WindValues>() { new WindValues() };
            defaultBuffer.SetData(defaultValues);
            return defaultBuffer;
        }
    }

    [Serializable]
    public struct WindValues
    {
        public Vector2 windDirectionSpeed;
        public float windTextureScale;
        public float windIntensity;
        public float windContrast;
        public float minWindStrength;
        public float maxWindStrength;
        public WindValues(Vector2 windDirectionSpeed, float windTextureScale, float windIntensity, float windContrast, float minWindStrength, float maxWindStrength)
        {
            this.windDirectionSpeed = windDirectionSpeed;
            this.windTextureScale = windTextureScale;
            this.windIntensity = windIntensity;
            this.windContrast = windContrast;
            this.minWindStrength = minWindStrength;
            this.maxWindStrength = maxWindStrength;
        }
    };
}