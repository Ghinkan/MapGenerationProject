using MapGenerationProject.Base;
using UnityEngine;

namespace MapGenerationProject
{
    public class HexMapCamera : MonoBehaviour
    {
        public HexGrid grid;
        public float moveSpeedMinZoom, moveSpeedMaxZoom;
        public float stickMinZoom, stickMaxZoom;
        public float swivelMinZoom, swivelMaxZoom;
        public float rotationSpeed;

        private Transform swivel; 
        private Transform stick;
        private float zoom;
        private float rotationAngle;

        private void Awake()
        {
            swivel = transform.GetChild(0);
            stick = swivel.GetChild(0);
            
            float initialZ = stick.localPosition.z;
            zoom = Mathf.InverseLerp(stickMinZoom, stickMaxZoom, initialZ);
        }
        
        private void Update()
        {
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (zoomDelta != 0f) 
            {
                AdjustZoom(zoomDelta);
            }
            
            float rotationDelta = Input.GetAxis("Rotation");
            if (rotationDelta != 0f)
            {
                AdjustRotation(rotationDelta);
            }
            
            float xDelta = Input.GetAxis("Horizontal");
            float zDelta = Input.GetAxis("Vertical");
            if (xDelta != 0f || zDelta != 0f)
            {
                AdjustPosition(xDelta, zDelta);
            }
        }
	
        private void AdjustZoom(float delta)
        {
            zoom = Mathf.Clamp01(zoom + delta);
            float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, distance);
            
            float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
            swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
        
        private void AdjustRotation(float delta)
        {
            rotationAngle += delta * rotationSpeed * Time.deltaTime;
            
            if (rotationAngle < 0f)
                rotationAngle += 360f;
            else if (rotationAngle >= 360f)
                rotationAngle -= 360f;
            
            transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
        }
        
        private void AdjustPosition(float xDelta, float zDelta)
        {
            Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
            float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
            float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;
		
            Vector3 position = transform.localPosition;
            position += direction * distance;
            transform.localPosition = ClampPosition(position);
        }
        
        private Vector3 ClampPosition(Vector3 position)
        {
            float xMax = (grid.chunkCountX * HexMetrics.chunkSizeX - 0.5f) * (2f * HexMetrics.InnerRadius);
            position.x = Mathf.Clamp(position.x, 0f, xMax);
            
            float zMax = (grid.chunkCountZ * HexMetrics.chunkSizeZ - 1) * (1.5f * HexMetrics.OuterRadius);
            position.z = Mathf.Clamp(position.z, 0f, zMax);

            return position;
        }
    }
}
