using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace MapGenerationProject.DOTS
{
    [CreateAssetMenu(menuName = "Input Reader")]
    public class InputReader : ScriptableObject, InputSystem.IMapActions
    {
        public event UnityAction<Vector2> OnMoveEvent;
        public event UnityAction<float> OnRotateEvent;
        public event UnityAction<float> OnZoomEvent;
        
        private InputSystem _inputSystem;

        public void EnableMapActions()
        {
            if (_inputSystem == null)
            {
                _inputSystem = new InputSystem();
                _inputSystem.Map.SetCallbacks(this);
            }

            _inputSystem.Enable();
        }
        
        private void OnDisable()
        {
            _inputSystem?.Disable();
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
        }
        
        public void OnRotation(InputAction.CallbackContext context)
        {
            OnRotateEvent?.Invoke(context.ReadValue<float>());
        }
        
        public void OnZoom(InputAction.CallbackContext context)
        {
            OnZoomEvent?.Invoke(context.ReadValue<float>());
        }
    }
}