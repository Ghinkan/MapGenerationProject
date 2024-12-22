using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace UnityEngine.EventChannels.Editor
{
    /// <summary>
    /// This Editor class creates a custom Inspector for event channels that carry a payload
    /// (e.g., BoolEventChannelSO, FloatEventChannelSO, etc.). A ListView shows the subscribed
    /// listeners in the scene. Click each item to ping it in the Hierarchy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [CustomEditor(typeof(GenericEventChannel<>), true)]
    public abstract class GenericEventChannelEditor<T> : UnityEditor.Editor
    {
        private GenericEventChannel<T> _eventChannel;

        // Label and counter for items in the list
        private Label _listenersLabel;
        private ListView _listenersListView;
        private Button _raiseEventButton;

        private void OnEnable()
        {
            if (_eventChannel == null)
                _eventChannel = target as GenericEventChannel<T>;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Draw default elements in the inspector
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var spaceElement = new VisualElement();
            spaceElement.style.marginBottom = 20;
            root.Add(spaceElement);

            // ChangeEquipmentInSlot a label
            _listenersLabel = new Label();
            _listenersLabel.text = "Listeners:";
            _listenersLabel.style.borderBottomWidth = 1;
            _listenersLabel.style.borderBottomColor = Color.grey;
            _listenersLabel.style.marginBottom = 2;
            root.Add(_listenersLabel);

            // ChangeEquipmentInSlot a ListView to show Listeners
            _listenersListView = new ListView(GetListeners(), 20, MakeItem, BindItem);
            root.Add(_listenersListView);

            // Button to test event
            _raiseEventButton = new Button();
            _raiseEventButton.text = "Raise Event";
            _raiseEventButton.RegisterCallback<ClickEvent>(evt => _eventChannel.RaiseEvent(default(T)));
            _raiseEventButton.style.marginBottom = 20;
            _raiseEventButton.style.marginTop = 20;
            root.Add(_raiseEventButton);

            return root;
        }

        private VisualElement MakeItem()
        {
            var element = new VisualElement();
            var label = new Label();
            element.Add(label);
            return element;
        }

        private void BindItem(VisualElement element, int index)
        {
            //if (m_RuntimeSet.Items.Count == 0)
            //    return;
            List<MonoBehaviour> listeners = GetListeners();

            var item = listeners[index];

            Label label = (Label)element.ElementAt(0);
            label.text = GetListenerName(item);

            // Attach a ClickEvent to the label
            label.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Ping the item in the Hierarchy
                EditorGUIUtility.PingObject(item.gameObject);
            });

        }

        private string GetListenerName(MonoBehaviour listener)
        {
            if (listener == null)
                return "<null>";

            string combinedName = listener.gameObject.name + " (" + listener.GetType().Name + ")";
            return combinedName;

        }

        // Gets a list of MonoBehaviours that are listening to the event channel
        private List<MonoBehaviour> GetListeners()
        {
            List<MonoBehaviour> listeners = new List<MonoBehaviour>();

            if (_eventChannel == null || _eventChannel.GameEvent == null)
                return listeners;

            // Get all delegates subscribed to the OnEventRaised action
            var delegateSubscribers = _eventChannel.GameEvent.GetInvocationList();

            foreach (var subscriber in delegateSubscribers)
            {
                // Get the MonoBehaviour associated with each delegate
                var componentListener = subscriber.Target as MonoBehaviour;

                // Append to the list and return
                if (!listeners.Contains(componentListener))
                {
                    listeners.Add(componentListener);
                }
            }

            return listeners;
        }

    }
}