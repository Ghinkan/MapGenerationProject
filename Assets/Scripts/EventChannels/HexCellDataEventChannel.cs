using MapGenerationProject.DOTS;
using Unity.Collections;
namespace UnityEngine.EventChannels
{
    [CreateAssetMenu(menuName = "HexCellDataChannel", fileName = "Events/HexCellData Channel")]
    public class HexCellDataEventChannel : GenericEventChannel<NativeArray<HexCellData>> { }
}