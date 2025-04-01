using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public class HexMapCamera : MonoBehaviour
    {
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private HexGrid _hexGrid;
        [SerializeField] private float _moveSpeedMinZoom;
        [SerializeField] private float _moveSpeedMaxZoom;
        [SerializeField] private float _stickMinZoom;
        [SerializeField] private float _stickMaxZoom;
        [SerializeField] private float _swivelMinZoom;
        [SerializeField] private float _swivelMaxZoom;
        [SerializeField] private float _rotationSpeed;

        private Transform _swivel; 
        private Transform _stick;
        
        private Vector2 _movementInput;
        private float _rotationInput;
        private float _actualZoom;
        private float _actualRotationAngle;
        
        private const float XMaxPosition = (HexMetrics.ChunkCountX * HexMetrics.ChunkCellSizeX - 0.5f) * (2f * HexMetrics.InnerRadius);
        private const float ZMaxPosition = (HexMetrics.ChunkCountZ * HexMetrics.ChunkCellSizeZ - 1) * (1.5f * HexMetrics.OuterRadius);

        private void Awake()
        {
            _swivel = transform.GetChild(0);
            _stick = _swivel.GetChild(0);
            
            float initialZ = _stick.localPosition.z;
            _actualZoom = Mathf.InverseLerp(_stickMinZoom, _stickMaxZoom, initialZ);
            
            _inputReader.OnZoomEvent += AdjustZoom;
            _inputReader.OnRotateEvent += HandleRotationInput;
            _inputReader.OnMoveEvent += HandleMovementInput;
            _inputReader.EnableMapActions();
        }
        
        private void OnDestroy()
        {
            _inputReader.OnZoomEvent -= AdjustZoom;
            _inputReader.OnRotateEvent -= HandleRotationInput;
            _inputReader.OnMoveEvent -= HandleMovementInput;
        }
        
        private void HandleRotationInput(float rotationDirection) => _rotationInput = rotationDirection;
        private void HandleMovementInput(Vector2 moveDirection)   => _movementInput = moveDirection;
        
        private void Update()
        {
            AdjustRotation(_rotationInput);
            AdjustPosition(_movementInput);
        }
        
        private void AdjustZoom(float delta)
        {
            _actualZoom = Mathf.Clamp01(_actualZoom + delta);
            float distance = Mathf.Lerp(_stickMinZoom, _stickMaxZoom, _actualZoom);
            _stick.localPosition = new Vector3(0f, 0f, distance);
            
            float angle = Mathf.Lerp(_swivelMinZoom, _swivelMaxZoom, _actualZoom);
            _swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
        
        private void AdjustRotation(float delta)
        {
            if (Mathf.Abs(delta) < 0.01f) return;
    
            _actualRotationAngle = Mathf.Repeat(_actualRotationAngle + delta * _rotationSpeed * Time.deltaTime, 360f);
            transform.localRotation = Quaternion.Euler(0f, _actualRotationAngle, 0f);
        }
        
        private void AdjustPosition(Vector2 movementInput)
        {
            if (movementInput == Vector2.zero) return;

            Vector3 direction = transform.localRotation * new Vector3(movementInput.x, 0f, movementInput.y).normalized;
            float damping = Mathf.Max(Mathf.Abs(movementInput.x), Mathf.Abs(movementInput.y));
            float distance = Mathf.Lerp(_moveSpeedMinZoom, _moveSpeedMaxZoom, _actualZoom) * damping * Time.deltaTime;

            Vector3 position = transform.localPosition;
            position += direction * distance;
            transform.localPosition = ClampPosition(position);
        }
        
        private Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, 0f, XMaxPosition);
            position.z = Mathf.Clamp(position.z, 0f, ZMaxPosition);

            return position;
        }
    }
}