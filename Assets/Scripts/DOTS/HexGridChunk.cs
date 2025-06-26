using Unity.Collections;
using UnityEngine;
using UnityEngine.EventChannels;
namespace MapGenerationProject.DOTS
{
    public class HexGridChunk : MonoBehaviour
    {
        [SerializeField] private HexMesh _hexMesh;
        [SerializeField] private IntEventChannel _hexSelected;
        [SerializeField] private IntEventChannel _refreshChunkMesh;
        
        public ChunkData ChunkData;
        
        private void Start()
        {
            _hexSelected.GameEvent += Refresh;
            _refreshChunkMesh.GameEvent += RefreshChunk;
        }
        
        private void OnDestroy()
        {
            _hexSelected.GameEvent -= Refresh;
            _refreshChunkMesh.GameEvent -= RefreshChunk;
        }
        
        private void LateUpdate() 
        {
            _hexMesh.TriangulateChunk(ChunkData);
            enabled = false;
        }
        
        private void Refresh(int cellIndex)
        {
            if (HexGrid.Cells[cellIndex].ChunkIndex == ChunkData.ChunkIndex)
            {
                _refreshChunkMesh.RaiseEvent(ChunkData.ChunkIndex);
                
                NativeArray<HexCellData> neighbors = HexMetrics.GetNeighbors(HexGrid.Cells, HexGrid.Cells[cellIndex]);
                foreach (HexCellData neighbor in neighbors)
                {
                    if(neighbor.ChunkIndex != ChunkData.ChunkIndex)
                        _refreshChunkMesh.RaiseEvent(neighbor.ChunkIndex);
                }
                neighbors.Dispose();
            }
        }

        private void RefreshChunk(int chunkIndex)
        {
            if (ChunkData.ChunkIndex == chunkIndex)
                enabled = true;
        }
    }
}