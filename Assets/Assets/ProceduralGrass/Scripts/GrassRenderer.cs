using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TerrainTesselationTools;
namespace ProceduralGrass
{
    [ExecuteAlways]
    public class GrassRenderer : ProceduralRenderer
    {
        private GraphicsBuffer clumpPoints;
        [SerializeField][ReadOnly] private List<GrassOriginPoint> grassOriginPoints = new List<GrassOriginPoint>();
        [SerializeField] private List<ClumpPoint> clumpOriginPoints = new List<ClumpPoint>();
        private GraphicsBuffer grassBlades;
        private GraphicsBuffer grassBladesValid;
        private GraphicsBuffer defaultColorsBuffer;
        private GraphicsBuffer secondaryColorsBuffer;

        private Color[] defaultColors;
        private Color[] secondaryColors;

        private float[] uvsValues;
        private Texture2D heightmapTexture;

        private bool startExecuted;

        public static event EventHandler onRefresh;
        private ProceduralGrassSettingsSO settings;

        private void Start()
        {
            if (grassOriginPoints.Count <= 0) enabled = false;
            startExecuted = true;
            onRefresh += Grass_onRefresh;
            //Get The BirdViewUVs 
            BirdViewUVCalculator birdViewUVs = transform.parent.GetComponent<BirdViewUVCalculator>();
            uvsValues = birdViewUVs.GetBirdViewUVs();
            heightmapTexture = birdViewUVs.GetTexture();
            settings = (ProceduralGrassSettingsSO)terrainDecorationSO.proceduralSettings;
            InitIndirectArgs();

            InitCompute();

            PopulateInitUpdateCompute();

            PopulateShaderMaterial();
        }

        private void Grass_onRefresh(object sender, EventArgs e)
        {
            Refresh();
        }

        private void OnEnable()
        {
            if (grassOriginPoints.Count <= 0) enabled = false;
            if (startExecuted && enabled)
            {
                InitIndirectArgs();

                InitCompute();

                PopulateInitUpdateCompute();

                PopulateShaderMaterial();
            }
        }
        private void Update()
        {
            UpdateCompute();
        }
        private void OnValidate()
        {
            OnEnable();
        }
        public override void Refresh()
        {
            if (enabled) OnEnable();
        }
        protected override void PopulateIndirectArgsCompute()
        {
            // How many times the Thread Groups need to be dispatched.
            dispatchSize = settings.grassBladesPerPoint * ((settings.grassSegments - 1) * 3 * 2 + 3); // This must be calculated, so it knows how many threads it needs to dispatch. calculated again in the PopulateInitCompute.

            //First, create the buffers for the indirect Args, and triangle array for the instance. Used on the render function.
            instanceTriangles = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, settings.grassBladesPerPoint * ((settings.grassSegments - 1) * 3 * 2 + 3), sizeof(int)); // This is compulsory in all procedural shaders. Count and stride differ, so you must declare it.

            //Setting properties
            computeShaderRenderPrimitives.SetInt("grassOriginPointCount", grassOriginPoints.Count);
            computeShaderRenderPrimitives.SetInt("grassBladesPerPoint", settings.grassBladesPerPoint);
            computeShaderRenderPrimitives.SetInt("grassSegments", settings.grassSegments);
        }
        protected override void PopulateInitCompute()
        {
            // How many times the Thread Groups need to be dispatched.
            dispatchSize = Mathf.CeilToInt((float)grassOriginPoints.Count / threadGroupSize); // This must be calculated, so it knows how many threads it needs to dispatch. Used In both dispatches, but can be calculated again in the PopulateUpdateCompute if it is different.

            //Create the buffers for GrassBlades, used on the shader.
            originPoints = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, grassOriginPoints.Count, 5 * sizeof(float));
            clumpPoints = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, clumpOriginPoints.Count, 16 * sizeof(float));
            grassBlades = CreateGraphicsBuffer(GraphicsBuffer.Target.Append, grassOriginPoints.Count * settings.grassBladesPerPoint, 15 * sizeof(float));
            originPoints.SetData(grassOriginPoints);
            clumpPoints.SetData(clumpOriginPoints.Select(clumpPoint => new ClumpBuffer(clumpPoint.position, clumpPoint.properties)).ToList());

            //Setting properties
            computeShaderInit.SetInt("grassOriginPointCount", grassOriginPoints.Count);
            computeShaderInit.SetInt("grassBladesPerPoint", settings.grassBladesPerPoint);
            computeShaderInit.SetFloat("bladeInstanceRadius", settings.bladeInstanceRadius);

            computeShaderInit.SetTexture(0, "_HeightMap", heightmapTexture);
            computeShaderInit.SetFloat("_MinX", uvsValues[0]);
            computeShaderInit.SetFloat("_MinZ", uvsValues[1]);
            computeShaderInit.SetFloat("_MaxX", uvsValues[2]);
            computeShaderInit.SetFloat("_MaxZ", uvsValues[3]);
            computeShaderInit.SetFloat("_TerrainRotation", uvsValues[4]);
            computeShaderInit.SetFloat("_TerrainForce", uvsValues[5]);

            computeShaderInit.SetBuffer(0, "_GrassOriginPoints", originPoints);
            computeShaderInit.SetBuffer(0, "_ClumpPoints", clumpPoints);
            computeShaderInit.SetBuffer(0, "_GrassBlades", grassBlades);
        }
        protected override void PopulateInitUpdateCompute()
        {
            //Preparing UpdateCompute
            grassBladesValid = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, grassOriginPoints.Count * settings.grassBladesPerPoint, 15 * sizeof(float));
            instanceValidCount = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(uint));
            computeShader.SetInt("grassOriginPointCount", grassOriginPoints.Count);
            computeShader.SetInt("grassBladesPerPoint", settings.grassBladesPerPoint);
            computeShader.SetFloat("frustumTolerance", settings.renderSettings.frustumTolerance);
            computeShader.SetFloat("distanceCulling", settings.renderSettings.distanceCulling);
            computeShader.SetFloat("distanceCullingNumber", settings.renderSettings.distanceCullingNumber);
            computeShader.SetFloat("distanceCullingNone", settings.renderSettings.distanceCullingNone);
            computeShader.SetBuffer(0, "_GrassOriginPoints", originPoints);
            computeShader.SetBuffer(0, "_GrassBlades", grassBlades);
            computeShader.SetBuffer(0, "_GrassBladesValid", grassBladesValid);
            computeShader.SetBuffer(0, "_IndirectDrawIndexedArgs", indirectBuffer);

            //Preparing Wind
            if (GlobalWind.Instance != null)
            {
                GlobalWind globalWind = GlobalWind.Instance;
                computeShader.SetBuffer(0, "_WindValues", globalWind.GetWindBuffer());
                computeShader.SetTexture(0, "_WindTexture", globalWind.GetTexture2D());
            }
            else
            {
                GraphicsBuffer defaultBuffer = GlobalWind.GetDefaultWindBuffer();
                graphicsBuffers.Add(defaultBuffer);
                computeShader.SetBuffer(0, "_WindValues", defaultBuffer);
                computeShader.SetTexture(0, "_WindTexture", Texture2D.blackTexture);
            }
        }
        protected override void PopulateShaderMaterial()
        {
            //Setting Buffers and variables to shaderMat
            renderParams = new RenderParams(proceduralMaterial);

            // I dont think so, but this might give problems with rotationOffset in birdviewUv. If you are using it, and it gives problems: Comment the code bellow, and uncomment the one following it.
            renderParams.worldBounds = GenerateBounds(uvsValues[0], uvsValues[1], uvsValues[2], uvsValues[3], uvsValues[5], 5); // 5 units offset just in case. 
                                                                                                                                //renderParams.worldBounds = transform.parent.gameObject.GetComponent<MeshRenderer>().bounds;

            //Calculating Color Buffers based on Gradient
            defaultColorsBuffer = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, settings.grassSegments + 1, 4 * sizeof(float));
            secondaryColorsBuffer = CreateGraphicsBuffer(GraphicsBuffer.Target.Structured, settings.grassSegments + 1, 4 * sizeof(float));
            defaultColors = new Color[settings.grassSegments + 1];
            secondaryColors = new Color[settings.grassSegments + 1];

            for (int i = 0; i < settings.grassSegments + 1; i++)
            {
                float linearValue = i / (float)settings.grassSegments;
                float exponentialValue = Mathf.Pow(linearValue, settings.exponent);
                defaultColors[i] = settings.defaultColorsGradient.Evaluate(exponentialValue);
                secondaryColors[i] = settings.secondaryColorsGradient.Evaluate(exponentialValue);
            }

            defaultColorsBuffer.SetData(defaultColors);
            secondaryColorsBuffer.SetData(secondaryColors);

            //Set Material Properties
            renderParams.matProps = new MaterialPropertyBlock();
            renderParams.matProps.SetBuffer("_DefaultColors", defaultColorsBuffer);
            renderParams.matProps.SetBuffer("_SecondaryColors", secondaryColorsBuffer);
            renderParams.matProps.SetBuffer("_GrassBlades", grassBladesValid);
            renderParams.matProps.SetFloat("_GrassSegments", settings.grassSegments);
            renderParams.matProps.SetFloat("_GrassBladesPerPoint", settings.grassBladesPerPoint);
            renderParams.matProps.SetFloat("_VerticesPerBlade", settings.grassSegments * 2 + 1);
            renderParams.matProps.SetFloat("_Exponent", settings.exponent);
            renderParams.matProps.SetFloat("_MaxInteractors", GrassInteractorManager.GetMaxInteractors());

            //Grass Interactors
            renderParams.matProps.SetBuffer("_GrassInteractorPoints", GrassInteractorManager.GetBuffer());
        }
        protected override void PopulateUpdateCompute()
        {
            //Camera Values for culling.
            Matrix4x4 projectionMatrix = Camera.main.projectionMatrix;
            Matrix4x4 worldToCameraMatrix = Camera.main.worldToCameraMatrix;

            computeShader.SetVector("cameraPosition", Camera.main.gameObject.transform.position);
            computeShader.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
            computeShader.SetMatrix("_CameraToWorldMatrix", worldToCameraMatrix);
            computeShader.SetFloat("frustumTolerance", settings.renderSettings.frustumTolerance);
            // Rendering
            renderParams.matProps.SetVector("_WSpaceCameraPos", Camera.main.gameObject.transform.position);
        }
        public Bounds GenerateBounds(float minX, float minZ, float maxX, float maxZ, float maxHeight, float offset)
        {
            float width = Mathf.Abs(maxX - minX) + offset * 2;
            float height = maxHeight + offset * 2;
            float depth = Mathf.Abs(maxZ - minZ) + offset * 2;

            float centerX = (minX + maxX) / 2;
            float centerY = maxHeight / 2 + offset;
            float centerZ = (minZ + maxZ) / 2;

            Vector3 center = new Vector3(centerX, centerY, centerZ);
            Vector3 size = new Vector3(width, height, depth);

            Bounds bounds = new Bounds(center, size);
            return bounds;
        }
        public override void GeneratePoints(BirdViewUVCalculator birdViewUVCalculator, Texture2D grayscaleTexture, List<Texture2D> terrainDecorationsExtraData, float minDensityThreshold = 0.05f, float guaranteedDensityThreshold = 0.0f)
        {
            grassOriginPoints.Clear();
            terrainDecorationSO = transform.parent.GetComponent<TerrainTileDecorated>().GetTerrainDecorationSO(this);
            uvsValues = birdViewUVCalculator.GetBirdViewUVs();
            spacingAtMaxDensity = terrainDecorationSO.spacingAtMaxDensity;
            float jitterAmount = terrainDecorationSO.jitterAmount; // Use the local variable for jitter

            if (spacingAtMaxDensity != 0 && grayscaleTexture != null)
            {
                for (float x = uvsValues[0]; x <= uvsValues[2]; x += spacingAtMaxDensity)
                {
                    for (float z = uvsValues[1]; z <= uvsValues[3]; z += spacingAtMaxDensity)
                    {
                        // Sample grayscale value from texture
                        Color density = grayscaleTexture.GetPixelBilinear((x - uvsValues[0]) / (uvsValues[2] - uvsValues[0]),
                                                                            (z - uvsValues[1]) / (uvsValues[3] - uvsValues[1]));
                        // Set a threshold (e.g., 0.1) below which no points are placed
                        if (density.grayscale > minDensityThreshold)
                        {
                            Color ratio = terrainDecorationsExtraData[0].GetPixelBilinear((x - uvsValues[0]) / (uvsValues[2] - uvsValues[0]),
                                                        (z - uvsValues[1]) / (uvsValues[3] - uvsValues[1]));
                            // Implement rejection sampling
                            float rejectionProbability = 1.0f - density.grayscale - guaranteedDensityThreshold;
                            if (UnityEngine.Random.value > rejectionProbability)
                            {
                                // Apply jitter
                                float xOffset = UnityEngine.Random.Range(-spacingAtMaxDensity * 0.5f * jitterAmount, spacingAtMaxDensity * 0.5f * jitterAmount);
                                float zOffset = UnityEngine.Random.Range(-spacingAtMaxDensity * 0.5f * jitterAmount, spacingAtMaxDensity * 0.5f * jitterAmount);
                                AddPoint(birdViewUVCalculator, x + xOffset, z + zOffset, new float[] { 1, ratio.grayscale });
                            }
                        }
                    }
                }
            }
            if (grassOriginPoints.Count >= 0) enabled = true;
            Refresh();
        }
        public override void AddPoint(BirdViewUVCalculator birdViewUVCalculator, float x, float z, float[] arguments)
        {
            if (!birdViewUVCalculator.IsXZInsideSquare(x, z)) return;
            GrassOriginPoint grassStruct = new GrassOriginPoint
            {
                position = new Vector3(x, birdViewUVCalculator.GetHeightAtPosition(x, z), z),
                clumpSecondaryID = (uint)arguments[0],
                clumpBaseToSecondaryRatio = arguments[1]
            };
            grassOriginPoints.Add(grassStruct);
            if (grassOriginPoints.Count >= 0) enabled = true;
        }
        public override void RemovePoints(BirdViewUVCalculator birdViewUVCalculator, float x, float z, float halfBrushSize)
        {
            // Define a predicate for the RemoveAll method
            bool IsPointInsideCircle(GrassOriginPoint point)
            {
                return Vector2.Distance(new Vector2(point.position.x, point.position.z), new Vector2(x, z)) <= halfBrushSize;
            }

            // Remove points that satisfy the condition using RemoveAll
            grassOriginPoints.RemoveAll(IsPointInsideCircle);
            if (grassOriginPoints.Count <= 0)
            {
                Debug.Log("No points in the renderer, disabling script");
                enabled = false;
            }
        }
        public static void RefreshStatic(object sender)
        {
            onRefresh?.Invoke(sender, EventArgs.Empty);
        }
        private void OnDestroy()
        {
            onRefresh -= Grass_onRefresh;
        }
        public List<ClumpPoint> GetClumps()
        {
            return clumpOriginPoints;
        }
    }
    [Serializable]
    public struct GrassOriginPoint
    {
        public Vector3 position;
        public uint clumpSecondaryID;
        public float clumpBaseToSecondaryRatio;
    };
    [Serializable]
    public struct GrassBlade
    {
        public Vector3 position;
        public float height;
        public float width;
        public float rotationAngle;
        public float rotationAngleCurrent;
        public float hash;
        public float tilt;
        public float bend;
        public float windStrength;
        public Vector3 surfaceNorm;
        public float clumpBaseToSecondaryRatio;
    };
    [Serializable]
    public struct ClumpPoint
    {
        public Vector3 position;
        public ClumpPorpertiesSO properties;
    };
    public struct ClumpBuffer
    {
        public Vector3 position;
        public float moveToCenter;
        public float pointInSameDirection;
        public float pointInSameDirectionAngle;
        public float pointInSameDirectionRelativeCenter;
        public float pointInSameDirectionAngleRelativeCenter;
        public float height;
        public float heightRandom;
        public float width;
        public float widthRandom;
        public float tilt;
        public float tiltRandom;
        public float bend;
        public float bendRandom;
        public ClumpBuffer(Vector3 position, ClumpPorpertiesSO properties)
        {
            this.position = position;
            moveToCenter = properties.moveToCenter;
            pointInSameDirection = properties.pointInSameDirection;
            pointInSameDirectionAngle = properties.pointInSameDirectionAngle;
            pointInSameDirectionRelativeCenter = properties.pointInSameDirectionRelativeCenter;
            pointInSameDirectionAngleRelativeCenter = properties.pointInSameDirectionAngleRelativeCenter;
            height = properties.height;
            heightRandom = properties.heightRandom;
            width = properties.width;
            widthRandom = properties.widthRandom;
            tilt = properties.tilt;
            tiltRandom = properties.tiltRandom;
            bend = properties.bend;
            bendRandom = properties.bendRandom;
        }
    };
}
