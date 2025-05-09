﻿using TMPro;
using UnityEngine;
namespace MapGenerationProject.Base
{
    public class HexGrid : MonoBehaviour
    {
        private int cellCountX = 5;
        private int cellCountZ = 5;
        
        public int chunkCountX = 4;
        public int chunkCountZ = 3;

        public HexCell cellPrefab;
        public HexGridChunk chunkPrefab;
        
        HexCell[] cells;
        HexGridChunk[] chunks;
        
        public TMP_Text cellLabelPrefab;
        
        public Color defaultColor = Color.white;
        
        public Texture2D noiseSource;

        private void Awake() 
        {
            HexMetrics.noiseSource = noiseSource;

            cellCountX = chunkCountX * HexMetrics.chunkSizeX;
            cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;
            CreateChunks();
            CreateCells();
        }
        
        private void CreateChunks()
        {
            chunks = new HexGridChunk[chunkCountX * chunkCountZ];

            for(int z = 0, i = 0; z < chunkCountZ; z++) 
            {
                for (int x = 0; x < chunkCountX; x++) 
                {
                    HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                    chunk.transform.SetParent(transform);
                }
            }
        }

        private void CreateCells() 
        {
            cells = new HexCell[cellCountZ * cellCountX];

            for (int z = 0, i = 0; z < cellCountZ; z++) {
                for (int x = 0; x < cellCountX; x++) {
                    CreateCell(x, z, i++);
                }
            }
        }
        
        private void CreateCell(int x, int z, int index) 
        {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.OuterRadius * 1.5f);

            HexCell cell = cells[index] = Instantiate(cellPrefab);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.Color = defaultColor;
            
            if (x > 0) 
            {
                cell.SetNeighbor(HexDirection.W, cells[index - 1]);
            }
            if (z > 0)
            {
                if ((z & 1) == 0) 
                {
                    cell.SetNeighbor(HexDirection.SE, cells[index - cellCountX]);
                    if (x > 0) 
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[index - cellCountX - 1]);
                    }
                }
                else 
                {
                    cell.SetNeighbor(HexDirection.SW, cells[index - cellCountX]);
                    if (x < cellCountX - 1) 
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[index - cellCountX + 1]);
                    }
                }
            }
            
            TMP_Text label = Instantiate(cellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.coordinates.ToStringOnSeparateLines();
            cell.uiRect = label.rectTransform;
            
            cell.Elevation = 0;
            
            AddCellToChunk(x, z, cell);
        }
        
        private void AddCellToChunk(int x, int z, HexCell cell)
        {
            int chunkX = x / HexMetrics.chunkSizeX;
            int chunkZ = z / HexMetrics.chunkSizeZ;
            HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];
            int localX = x - chunkX * HexMetrics.chunkSizeX;
            int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
            chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
        }

        public HexCell GetCell(Vector3 position) 
        {
            position = transform.InverseTransformPoint(position);
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);
            int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
            return cells[index];
        }
        
        public HexCell GetCell (HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            if (z < 0 || z >= cellCountZ)
            {
                return null;
            }
            int x = coordinates.X + z / 2;
            if (x < 0 || x >= cellCountX)
            {
                return null;
            }
            return cells[x + z * cellCountX];
        }
        
        public void ShowUI (bool visible)
        {
            for (int i = 0; i < chunks.Length; i++) 
            {
                chunks[i].ShowUI(visible);
            }
        }
    }
}