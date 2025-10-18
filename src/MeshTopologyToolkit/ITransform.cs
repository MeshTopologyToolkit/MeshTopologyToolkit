﻿using System.Numerics;

namespace MeshTopologyToolkit
{
    public interface ITransform
    {
        ITransform Combine(ITransform childTransform);

        Matrix4x4 ToMatrix();

        /// <summary>
        /// Transforms a position vector (point) from local space defined by this Transform
        /// into world space. A position is affected by scale, rotation, and translation.
        /// </summary>
        /// <param name="localPoint">The position vector in local space.</param>
        /// <returns>The position vector in world space.</returns>
        Vector3 TransformPosition(Vector3 localPoint);

        /// <summary>
        /// Transforms a direction vector from local space defined by this Transform
        /// into world space. A direction is affected by scale and rotation, but NOT translation.
        /// </summary>
        /// <param name="localDirection">The direction vector in local space (does not need to be unit length).</param>
        /// <returns>The direction vector in world space (retains length from scale).</returns>
        Vector3 TransformDirection(Vector3 localDirection);
    }
}
