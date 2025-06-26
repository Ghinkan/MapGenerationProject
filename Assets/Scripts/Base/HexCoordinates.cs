using UnityEngine;
namespace MapGenerationProject.Base
{
    [System.Serializable]
    public struct HexCoordinates 
    { 
        public int X { get; private set; }
        public int Y { get { return -X - Z; } }
        public int Z { get; private set; }
        
        public Vector3 CoordinateVector { get { return new Vector3(X, Y, Z); } }
        
        public HexCoordinates(int x, int z) 
        {
            X = x;
            Z = z;
        }
        
        public static HexCoordinates FromPosition(Vector3 position) 
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
                float dX = Mathf.Abs(x - iX);
                float dY = Mathf.Abs(y - iY);
                float dZ = Mathf.Abs(-x -y - iZ);
        
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
        
        public override string ToString() 
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        public string ToStringOnSeparateLines() 
        {
            return X + "\n" + Y + "\n" + Z;
        }
    }
}