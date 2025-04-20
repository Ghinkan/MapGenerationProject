using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public class HexMapCamera : MonoBehaviour
    {
        private float _inverseStickZoomRange;
        private float _rotationSpeedRad;
        
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private HexGrid _hexGrid;
        [SerializeField] private float _moveSpeedMinZoom;
        [SerializeField] private float _moveSpeedMaxZoom;
        [SerializeField] private float _stickMinZoom;
        [SerializeField] private float _stickMaxZoom;
        [SerializeField] private float _swivelMinZoom;
        [SerializeField] private float _swivelMaxZoom;
        [SerializeField] private float _rotationSpeed;

        private Transform _transform;
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
            _transform = transform;
            _swivel = _transform.GetChild(0);
            _stick = _swivel.GetChild(0);
            
            _inverseStickZoomRange = 1f / (_stickMaxZoom - _stickMinZoom);
            _rotationSpeedRad = _rotationSpeed * Mathf.Deg2Rad;
            
            float initialZ = _stick.localPosition.z;
            _actualZoom = (initialZ - _stickMinZoom) * _inverseStickZoomRange;
            
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
            float distance = _stickMinZoom + _actualZoom * (_stickMaxZoom - _stickMinZoom);
            _stick.localPosition = new Vector3(0f, 0f, distance);
            
            float angle = _swivelMinZoom + _actualZoom * (_swivelMaxZoom - _swivelMinZoom);
            _swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
        
        private void AdjustRotation(float delta)
        {
            if (Mathf.Abs(delta) < 0.01f) return;
            _actualRotationAngle = Mathf.Repeat(_actualRotationAngle + delta * _rotationSpeedRad * Time.deltaTime, Mathf.PI * 2f);
            _transform.localRotation = Quaternion.Euler(0f, _actualRotationAngle * Mathf.Rad2Deg, 0f);
        }
        
        private void AdjustPosition(Vector2 movementInput)
        {
            if (movementInput == Vector2.zero) return;
            
            Vector3 direction = _transform.localRotation * new Vector3(movementInput.x, 0f, movementInput.y).normalized;
            float damping = Mathf.Max(Mathf.Abs(movementInput.x), Mathf.Abs(movementInput.y));
            float speed = _moveSpeedMinZoom + _actualZoom * (_moveSpeedMaxZoom - _moveSpeedMinZoom);
            float distance = speed * damping * Time.deltaTime;

            Vector3 position = _transform.localPosition;
            position += direction * distance;
            _transform.localPosition = ClampPosition(position);
        }
        
        private Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, 0f, XMaxPosition);
            position.z = Mathf.Clamp(position.z, 0f, ZMaxPosition);

            return position;
        }
    }
}