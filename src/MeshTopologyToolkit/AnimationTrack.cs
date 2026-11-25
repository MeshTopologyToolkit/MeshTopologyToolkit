using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Animation of a single node. Any of the curves can be null (the property is not animated).
    /// </summary>
    public class AnimationTrack
    {
        public AnimationTrack(Node? target = null, string? targetName = null)
        {
            Target = target;
            TargetName = targetName;
        }

        /// <summary>
        /// The animated node. Null for animations that are not bound to a scene yet (for example, loaded from a standalone file).
        /// </summary>
        public Node? Target { get; set; }

        /// <summary>
        /// Name of the animated node, used to bind the track when <see cref="Target"/> is not set.
        /// </summary>
        public string? TargetName { get; set; }

        public string? NodeName => Target?.Name ?? TargetName;

        public AnimationCurve<Vector3>? Translation { get; set; }

        public AnimationCurve<Quaternion>? Rotation { get; set; }

        public AnimationCurve<Vector3>? Scale { get; set; }

        public float StartTime
        {
            get
            {
                float result = float.MaxValue;
                if (Translation?.Keys.Count > 0) result = Math.Min(result, Translation.StartTime);
                if (Rotation?.Keys.Count > 0) result = Math.Min(result, Rotation.StartTime);
                if (Scale?.Keys.Count > 0) result = Math.Min(result, Scale.StartTime);
                return result == float.MaxValue ? 0.0f : result;
            }
        }

        public float EndTime
        {
            get
            {
                float result = 0.0f;
                if (Translation?.Keys.Count > 0) result = Math.Max(result, Translation.EndTime);
                if (Rotation?.Keys.Count > 0) result = Math.Max(result, Rotation.EndTime);
                if (Scale?.Keys.Count > 0) result = Math.Max(result, Scale.EndTime);
                return result;
            }
        }

        public override string ToString() => $"Track \"{NodeName}\"";
    }
}
