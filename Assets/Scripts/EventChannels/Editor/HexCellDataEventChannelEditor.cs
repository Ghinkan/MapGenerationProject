using MapGenerationProject.DOTS;
using Unity.Collections;
using UnityEditor;
namespace UnityEngine.EventChannels.Editor
{
    [CustomEditor(typeof(HexCellDataEventChannel))]
    public class HexCellDataEventChannelEditor : GenericEventChannelEditor<NativeArray<HexCellData>> { }
}