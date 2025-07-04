﻿using UnityEngine;
namespace MapGenerationProject.Base
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        [SerializeField] private HexCell[] neighbors;
        public HexGridChunk chunk;
        public RectTransform uiRect;
        
        public Vector3 Position 
        {
            get { return transform.localPosition; }
        }
        
        int elevation = int.MinValue;
        public int Elevation 
        {
            get { return elevation; }
            set
            {
                if (elevation == value) return;
                
                elevation = value;
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                transform.localPosition = position;
                
                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;
                
                Refresh();
            }
        }
        
        Color color;
        public Color Color 
        {
            get { return color; }
            set {
                if (color == value) return;
                color = value;
                Refresh();
            }
        }
        
        public HexCell GetNeighbor(HexDirection direction) 
        {
            return neighbors[(int)direction];
        }
        
        public void SetNeighbor(HexDirection direction, HexCell cell) 
        {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }
        
        public HexEdgeType GetEdgeType(HexDirection direction) 
        {
            return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
        }
        
        public HexEdgeType GetEdgeType(HexCell otherCell) 
        {
            return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
        }
        
        private void Refresh() 
        {
            if(!chunk) return;
            chunk.Refresh();
            for(int i = 0; i < neighbors.Length; i++) 
            {
                HexCell neighbor = neighbors[i];
                if (neighbor && neighbor.chunk != chunk) 
                {
                    neighbor.chunk.Refresh();
                }
            }
                
        }
    }
}