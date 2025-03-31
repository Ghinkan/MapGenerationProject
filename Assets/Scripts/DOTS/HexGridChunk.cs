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
                _refreshChunkMesh.RaiseEvent(ChunkData.ChunkIndex);
        }

        private void RefreshChunk(int chunkIndex)
        {
            enabled = true;
        }
    }
}