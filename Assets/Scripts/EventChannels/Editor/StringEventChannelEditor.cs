using UnityEditor;
namespace UnityEngine.EventChannels.Editor
{
    [CustomEditor(typeof(StringEventChannel))]
    public class StringEventChannelEditor : GenericEventChannelEditor<string> { }
}
