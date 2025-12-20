using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class Vector4EqualityComparer : IEqualityComparer<Vector4>
    {
        private readonly float _toleranceSq;

        public Vector4EqualityComparer(float tolerance)
        {
            this._toleranceSq = tolerance * tolerance;
        }

        public bool Equals(Vector4 x, Vector4 y)
        {
            return (x - y).LengthSquared() <= _toleranceSq;
        }

        public int GetHashCode(Vector4 obj)
        {
            return obj.GetHashCode();
        }
    }
}
