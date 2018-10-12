using UnityEngine;

namespace ToolBox
{
    public struct MathTools
    {
        public static Vector3 RoundVector3(Vector3 _inVec, int _decimalPlaces)
        {
            float x = (float)System.Math.Round(_inVec.x, _decimalPlaces);
            float y = (float)System.Math.Round(_inVec.y, _decimalPlaces);
            float z = (float)System.Math.Round(_inVec.z, _decimalPlaces);
            return new Vector3(x, y, z);
        }
    }

    public static class ExtensionMethods
    {
        
    }
}
