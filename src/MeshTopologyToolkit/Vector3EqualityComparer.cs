using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        internal static readonly IEqualityComparer<Vector3> Default = new Vector3EqualityComparer(1e-6f);

        private readonly float _toleranceSq;

        public Vector3EqualityComparer(float tolerance)
        {
            this._toleranceSq = tolerance * tolerance;
        }

        public bool Equals(Vector3 x, Vector3 y)
        {
            return (x - y).LengthSquared() <= _toleranceSq;
        }

        public int GetHashCode(Vector3 obj)
        {
            return obj.GetHashCode();
        }
    }
}
