using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    [System.Serializable]
    public struct HexCoordinates
    {
        public readonly int X;
        public readonly int Z;
        public readonly int Y { get { return -X - Z; } }
        
        [ShowInInspector, ReadOnly] public Vector3 CoordinateVector { get { return new Vector3(X, Y, Z); } }
        
        public HexCoordinates(int x, int z) 
        {
            X = x;
            Z = z;
        }
        
        public static HexCoordinates FromPosition(float3 position) 
        {
            // Dividir la posicion por el tamaño del hexagono
            float x = position.x / (HexMetrics.InnerRadius * 2f);
            float y = -x;
            float offset = position.z / (HexMetrics.OuterRadius * 3f);
            x -= offset;
            y -= offset;
        
            // Redondear las coordenadas
            int iX = Mathf.RoundToInt(x);
            int iY = Mathf.RoundToInt(y);
            int iZ = Mathf.RoundToInt(-x -y);
            
            // Ajustar si las coordenadas redondeadas no son validas
            if (iX + iY + iZ != 0) 
            {
                float dX = math.abs(x - iX);
                float dY = math.abs(y - iY);
                float dZ = math.abs(-x -y - iZ);
        
                // Correccion de la coordenada mas afectada
                if (dX > dY && dX > dZ) 
                {
                    iX = -iY - iZ;
                }
                else if (dZ > dY)
                {
                    iZ = -iX - iY;
                }
            }
        
            return new HexCoordinates(iX, iZ);
        }
        
        public static HexCoordinates FromOffsetCoordinates(int x, int z) 
        {
            return new HexCoordinates(x - z / 2, z);
        }
        
        public readonly HexCoordinates Step(HexDirection direction)
        {
            return direction switch 
            {
                HexDirection.NE => new HexCoordinates(X, Z + 1),
                HexDirection.E => new HexCoordinates(X + 1, Z),
                HexDirection.SE => new HexCoordinates(X + 1, Z - 1),
                HexDirection.SW => new HexCoordinates(X, Z - 1),
                HexDirection.W => new HexCoordinates(X - 1, Z),
                _ => new HexCoordinates(X - 1, Z + 1),
            };
        }
        
        public override string ToString() 
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        public readonly string ToStringOnSeparateLines() 
        {
            return X + "\n" + Y + "\n" + Z;
        }
    }
}