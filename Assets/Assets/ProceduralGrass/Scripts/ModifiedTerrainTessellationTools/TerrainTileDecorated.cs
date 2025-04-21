using ProceduralGrass;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace TerrainTesselationTools
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BirdViewUVCalculator))]
    public class TerrainTileDecorated : TerrainTile
    {
        [SerializeField] private List<TerrainTileDecoration> tileDecorations = new List<TerrainTileDecoration>();
        private void OnBecameInvisible()
        {
            Deactivate();
        }
        private void OnBecameVisible()
        {
            Activate();
        }
        private void Activate()
        {
            foreach (TerrainTileDecoration tileDecoration in tileDecorations)
            {
                if (!tileDecoration.GetTerrainDecorationRenderer().gameObject.activeSelf)
                {
                    tileDecoration.GetTerrainDecorationRenderer().gameObject.SetActive(true);
                }
            }
        }
        private void Deactivate() 
        {
            foreach (TerrainTileDecoration tileDecoration in tileDecorations)
            {
                if (tileDecoration.GetTerrainDecorationRenderer().gameObject.activeSelf)
                {
                    tileDecoration.GetTerrainDecorationRenderer().gameObject.SetActive(false);
                }
            }
        }
        public void GeneratePoints()
        {
            foreach (TerrainTileDecoration tileDecoration in tileDecorations)
            {
                tileDecoration.GeneratePoints(GetComponent<BirdViewUVCalculator>());
            }
        }
        public List<TerrainTileDecoration> GetTileDecorations()
        {
            return tileDecorations;
        }
        public void SetDecorationMask(int decorationIndex, Texture2D mask)
        {
            if (decorationIndex >= 0 && decorationIndex < tileDecorations.Count)
            {
                tileDecorations[decorationIndex].SetTerrainDecorationMask(mask);
            }
            else
            {
                Debug.LogError("Invalid decoration index: " + decorationIndex);
            }
        }
        public void AddDecorationExtraData(Texture2D extraData)
        {
            foreach (var decoration in tileDecorations)
            {
                decoration.AddTerrainDecorationExtraData(extraData);
            }
        }
        public TerrainDecorationSO GetTerrainDecorationSO(ProceduralRenderer proceduralRenderer)
        {
            foreach(TerrainTileDecoration tileDecoration in tileDecorations)
            {
                if (tileDecoration.GetTerrainDecorationRenderer() == proceduralRenderer)
                {
                    return tileDecoration.GetTerrainDecorationSO();
                }
            }
            return null;
        }
    }
    [Serializable]
    public class TerrainTileDecoration
    {
        [SerializeField] private TerrainDecorationSO terrainDecorationSO;
        [SerializeField] private ProceduralRenderer terrainDecorationRenderer;
        [SerializeField][TextureDrawer(15.0f)] private Texture2D terrainDecorationsMask;
        [SerializeField][TextureDrawer(-7.5f)] private List<Texture2D> terrainDecorationsExtraData;

        public ProceduralRenderer GetTerrainDecorationRenderer()
        {
            return terrainDecorationRenderer;
        }
        public TerrainDecorationSO GetTerrainDecorationSO()
        {
            return terrainDecorationSO;
        }
        public void GeneratePoints(BirdViewUVCalculator birdViewUVCalculator)
        {
            terrainDecorationRenderer.GeneratePoints(birdViewUVCalculator, terrainDecorationsMask, terrainDecorationsExtraData);
            EditorUtility.SetDirty(terrainDecorationRenderer);
        }
        public void SetTerrainDecorationMask(Texture2D mask)
        {
            terrainDecorationsMask = mask;
        }
        public void AddTerrainDecorationExtraData(Texture2D extraData)
        {
            terrainDecorationsExtraData.Add(extraData);
        }
    }
}