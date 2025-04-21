using System.Collections.Generic;
using UnityEngine;
using TerrainTesselationTools;

namespace ProceduralGrass
{
    public class ProceduralRenderer : MonoBehaviour
    {
        protected TerrainDecorationSO terrainDecorationSO;

        //Shaders for your computes and rendering
        protected Material proceduralMaterial;
        protected ComputeShader computeShaderRenderPrimitives;
        protected ComputeShader computeShaderInit;
        protected ComputeShader computeShader;

        //All buffers are here, for easier Release OnDisable
        protected List<GraphicsBuffer> graphicsBuffers = new List<GraphicsBuffer>();

        //Indirect Args relative Variables
        protected uint[] indirectArgsData = new uint[5];
        protected GraphicsBuffer indirectBuffer;
        protected GraphicsBuffer.IndirectDrawIndexedArgs[] indirectArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[]
        {
        new GraphicsBuffer.IndirectDrawIndexedArgs
        {
            indexCountPerInstance = 0,
            instanceCount = 0,
            startIndex = 0,
            baseVertexIndex = 0,
            startInstance = 0,
        }
        };

        //Buffers for creating instances with Render Primitives
        protected GraphicsBuffer instanceTriangles;     //Holds the int[] declaring indexed vertex triangles. Only once for a single instance.
        protected GraphicsBuffer instanceValidCount;    //Holds the count of instances that are actually seen, so Render Primitives doesnt creates more nstances that it should.
        protected GraphicsBuffer originPoints;          //Holds all the points that would represent your decorations. Used in compute shaders, you can use originPointsList to populate it.
        [SerializeField] protected List<Vector3> originPointsList = new List<Vector3>();       //Holds all the points that would represent your decorations, in case you want to make your own ProceduralRenderer, and only need the Positions. (I also use the Clump ID and the ratio which the point belongs)

        protected RenderParams renderParams;            // Used for setting properties to the material instance in rendering.
        protected int threadGroupSize = 128;            //The number of threads per group, defined in [numthreads(128,1,1)] in the compute. used to calculate dispatchSize;
        protected int dispatchSize;                     //How many thread groups to dispatch
        protected uint[] resetArray = { 0 };
        protected uint[] instanceCount = new uint[1];

        protected float spacingAtMaxDensity;
        protected float jitterAmount;


        private void Awake()
        {
            terrainDecorationSO = transform.parent.GetComponent<TerrainTileDecorated>().GetTerrainDecorationSO(this);
            proceduralMaterial = terrainDecorationSO.proceduralMaterial;
            computeShaderRenderPrimitives = Instantiate(terrainDecorationSO.computeShaderInitArgs);
            computeShaderInit = Instantiate(terrainDecorationSO.computeShaderInit);
            computeShader = Instantiate(terrainDecorationSO.computeShaderUpdate);
        }
        public virtual void GeneratePoints(BirdViewUVCalculator birdViewUVCalculator, Texture2D grayscaleTexture, List<Texture2D> terrainDecorationsExtraData, float minDensityThreshold = 0.05f, float guaranteedDensityThreshold = 0.0f)
        {
            originPointsList.Clear();
            terrainDecorationSO = transform.parent.GetComponent<TerrainTileDecorated>().GetTerrainDecorationSO(this);
            spacingAtMaxDensity = terrainDecorationSO.spacingAtMaxDensity;
            jitterAmount = terrainDecorationSO.jitterAmount;
            float[] uvsValues = birdViewUVCalculator.GetBirdViewUVs();
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
                            // Implement rejection sampling with the provided probability modifier
                            float rejectionProbability = 1.0f - density.grayscale - guaranteedDensityThreshold;
                            if (Random.value > rejectionProbability)
                            {
                                float xOffset = Random.Range(-spacingAtMaxDensity * 0.5f * jitterAmount, spacingAtMaxDensity * 0.5f * jitterAmount);
                                float zOffset = Random.Range(-spacingAtMaxDensity * 0.5f * jitterAmount, spacingAtMaxDensity * 0.5f * jitterAmount);
                                Vector3 newPoint = new Vector3(x + xOffset, birdViewUVCalculator.GetHeightAtPosition(x + xOffset, z + zOffset), z + zOffset);
                                originPointsList.Add(newPoint);
                            }
                        }
                    }
                }
            }
            if (originPointsList.Count >= 0) enabled = true;
            Refresh();
        }

        public virtual void AddPoint(BirdViewUVCalculator birdViewUVCalculator, float x, float z, float[] arguments)
        {
            if (!birdViewUVCalculator.IsXZInsideSquare(x, z)) return;
            Vector3 newPoint = new Vector3(x, birdViewUVCalculator.GetHeightAtPosition(x, z), z);
            originPointsList.Add(newPoint);
            if (originPointsList.Count >= 0) enabled = true;
        }
        public virtual void RemovePoints(BirdViewUVCalculator birdViewUVCalculator, float x, float z, float halfBrushSize)
        {
            // Define a predicate for the RemoveAll method
            bool IsPointInsideCircle(Vector3 point)
            {
                return Vector2.Distance(new Vector2(point.x, point.z), new Vector2(x, z)) <= halfBrushSize;
            }

            // Remove points that satisfy the condition using RemoveAll
            originPointsList.RemoveAll(IsPointInsideCircle);
            if (originPointsList.Count <= 0)
            {
                Debug.Log("No points in the renderer, disabling script");
                enabled = false;
            }
        }
        public virtual void Refresh()
        {

        }
        protected void InitIndirectArgs()
        {
            PopulateIndirectArgsCompute();
            indirectBuffer = CreateGraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            computeShaderRenderPrimitives.SetBuffer(0, "_IndirectDrawIndexedArgs", indirectBuffer); // This holds all the indirectArgs
            computeShaderRenderPrimitives.SetBuffer(0, "_InstanceTriangles", instanceTriangles); // This buffer defines the triangle array for every instance

            //Dispatching
            computeShaderRenderPrimitives.Dispatch(0, dispatchSize, 1, 1);

            indirectBuffer.GetData(indirectArgs);
            indirectArgs[0].instanceCount = 0;
        }
        protected void InitCompute()
        {
            PopulateInitCompute();

            //Dispatching
            computeShaderInit.Dispatch(0, dispatchSize, 1, 1);
        }
        protected void UpdateCompute()
        {
            PopulateUpdateCompute();

            //Reset Count
            indirectBuffer.SetData(indirectArgs);
            //Dispatching
            computeShader.Dispatch(0, dispatchSize, 1, 1);
            //Rendering
            Graphics.RenderPrimitivesIndexedIndirect(renderParams, MeshTopology.Triangles, instanceTriangles, indirectBuffer);
        }
        protected virtual void PopulateIndirectArgsCompute()
        {
            // here you can create and set your buffers
        }
        protected virtual void PopulateInitCompute()
        {
            // here you can create and set your buffers
        }
        protected virtual void PopulateInitUpdateCompute()
        {
            // here you can create and set your buffers
        }
        protected virtual void PopulateShaderMaterial()
        {
            // here you can create and set your buffers
        }
        protected virtual void PopulateUpdateCompute()
        {
            // here you can create and set your buffers
        }
        private void OnDisable()
        {
            foreach (GraphicsBuffer graphicsBuffer in graphicsBuffers)
            {
                graphicsBuffer.Release();
            }
            graphicsBuffers = new List<GraphicsBuffer>();
        }
        protected GraphicsBuffer CreateGraphicsBuffer(GraphicsBuffer.Target type, int size, int stride)
        {
            GraphicsBuffer graphicsBuffer = new GraphicsBuffer(type, size, stride);
            graphicsBuffers.Add(graphicsBuffer);
            return graphicsBuffer;
        }
    }
}