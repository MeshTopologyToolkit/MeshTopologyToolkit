using System.Numerics;
using System.Text;

namespace MeshTopologyToolkit.Urho3D
{
    public class Skeleton
    {
        [Flags]
        enum BoneCollisionShape : byte
        {
            BONECOLLISION_NONE = 0x0,
            BONECOLLISION_SPHERE = 0x1,
            BONECOLLISION_BOX = 0x2,
        };

        public class Bone
        {
            public Bone(string name, int parentIndex_, Vector3 initialPosition_, Quaternion initialRotation_, Vector3 initialScale_)
            {
                Name = name;
                ParentIndex = parentIndex_;
                InitialPosition = initialPosition_;
                InitialRotation = initialRotation_;
                InitialScale = initialScale_;
            }

            public string Name { get; set; }

            /// <summary>
            /// Index of the parent bone. The root bone is its own parent.
            /// </summary>
            public int ParentIndex { get; set; }

            /// <summary>
            /// Initial position relative to the parent bone.
            /// </summary>
            public Vector3 InitialPosition { get; set; }

            /// <summary>
            /// Initial rotation relative to the parent bone.
            /// </summary>
            public Quaternion InitialRotation { get; set; }

            /// <summary>
            /// Initial scale relative to the parent bone.
            /// </summary>
            public Vector3 InitialScale { get; set; }

            /// <summary>
            /// Model space to bone space at the bind pose (System.Numerics row-vector convention).
            /// </summary>
            public Matrix4x4 OffsetMatrix { get; set; } = Matrix4x4.Identity;

            public BoundingBox3? BoundingBox { get; set; }
            public float? BoundingSphereRadius { get; set; }
        }

        public IList<Bone> Bones { get; } = new List<Bone>();

        public Skeleton()
        {
        }

        public void Read(BinaryReader reader)
        {
            var numBones = reader.ReadInt32();
            for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
            {
                var name = reader.ReadStringZ();
                var parentIndex_ = reader.ReadInt32();
                var initialPosition_ = reader.ReadVector3();
                var rot = reader.ReadVector4();
                var initialRotation_ = new Quaternion(rot.Y, rot.Z, rot.W, rot.X);
                var initialScale_ = reader.ReadVector3();
                var offsetMatrix = ReadMatrix3x4(reader);
                var boneCollisionFlags = (BoneCollisionShape)reader.ReadByte();

                var bone = new Bone(name, parentIndex_, initialPosition_, initialRotation_, initialScale_);
                bone.OffsetMatrix = offsetMatrix;

                if (0 != (boneCollisionFlags & BoneCollisionShape.BONECOLLISION_SPHERE))
                {
                    var r = reader.ReadSingle();
                    bone.BoundingSphereRadius = r;
                }
                if (0 != (boneCollisionFlags & BoneCollisionShape.BONECOLLISION_BOX))
                {
                    var min = reader.ReadVector3();
                    var max = reader.ReadVector3();
                    bone.BoundingBox = new BoundingBox3(min, max);
                }
                Bones.Add(bone);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((uint)Bones.Count);
            foreach (var bone in Bones)
            {
                writer.Write(Encoding.UTF8.GetBytes(bone.Name));
                writer.Write((byte)0);
                writer.Write((uint)bone.ParentIndex);
                writer.Write(bone.InitialPosition);
                writer.Write(bone.InitialRotation.W);
                writer.Write(bone.InitialRotation.X);
                writer.Write(bone.InitialRotation.Y);
                writer.Write(bone.InitialRotation.Z);
                writer.Write(bone.InitialScale);
                WriteMatrix3x4(writer, bone.OffsetMatrix);

                var flags = BoneCollisionShape.BONECOLLISION_NONE;
                if (bone.BoundingSphereRadius.HasValue)
                    flags |= BoneCollisionShape.BONECOLLISION_SPHERE;
                if (bone.BoundingBox.HasValue)
                    flags |= BoneCollisionShape.BONECOLLISION_BOX;
                writer.Write((byte)flags);
                if (bone.BoundingSphereRadius.HasValue)
                    writer.Write(bone.BoundingSphereRadius.Value);
                if (bone.BoundingBox.HasValue)
                {
                    writer.Write(bone.BoundingBox.Value.Min);
                    writer.Write(bone.BoundingBox.Value.Max);
                }
            }
        }

        // Urho3D stores Matrix3x4 as 3 rows of 4 floats (column vector convention, translation in the last column).
        // System.Numerics uses the transposed (row vector) convention.
        private static Matrix4x4 ReadMatrix3x4(BinaryReader reader)
        {
            var f = new float[12];
            for (int i = 0; i < f.Length; ++i)
                f[i] = reader.ReadSingle();
            return new Matrix4x4(
                f[0], f[4], f[8], 0.0f,
                f[1], f[5], f[9], 0.0f,
                f[2], f[6], f[10], 0.0f,
                f[3], f[7], f[11], 1.0f);
        }

        private static void WriteMatrix3x4(BinaryWriter writer, Matrix4x4 m)
        {
            writer.Write(m.M11); writer.Write(m.M21); writer.Write(m.M31); writer.Write(m.M41);
            writer.Write(m.M12); writer.Write(m.M22); writer.Write(m.M32); writer.Write(m.M42);
            writer.Write(m.M13); writer.Write(m.M23); writer.Write(m.M33); writer.Write(m.M43);
        }

        /// <summary>
        /// Creates a node hierarchy and a skin from the bones. Nodes of root bones (bones that are their own parents) are added to <paramref name="rootNodes"/>.
        /// </summary>
        public Skin ToSkin(IList<Node> rootNodes, string? name = null)
        {
            var nodes = new List<Node>(Bones.Count);
            foreach (var bone in Bones)
            {
                nodes.Add(new Node(bone.Name) { Transform = new TRSTransform(bone.InitialPosition, bone.InitialRotation, bone.InitialScale) });
            }

            var skin = new Skin(name);
            for (int i = 0; i < Bones.Count; ++i)
            {
                var parentIndex = Bones[i].ParentIndex;
                if (parentIndex == i || parentIndex < 0 || parentIndex >= nodes.Count)
                    rootNodes.Add(nodes[i]);
                else
                    nodes[parentIndex].AddChild(nodes[i]);

                skin.Joints.Add(nodes[i]);
                skin.InverseBindMatrices.Add(Bones[i].OffsetMatrix);
            }
            if (rootNodes.Count > 0)
                skin.Skeleton = rootNodes[0];
            return skin;
        }

        /// <summary>
        /// Creates bones from a skin. Urho3D needs the root bone first and parents before their children,
        /// so the order of the bones can differ from the order of the joints.
        /// </summary>
        /// <param name="skin">Source skin.</param>
        /// <param name="mesh">Optional mesh with joint indices and weights. It is used to calculate bone bounding boxes.</param>
        /// <param name="jointToBone">For every joint of the skin, the index of its bone.</param>
        public static Skeleton FromSkin(Skin skin, UnifiedIndexedMesh? mesh, out int[] jointToBone)
        {
            int count = skin.Joints.Count;
            var jointIndices = new Dictionary<Node, int>();
            for (int i = 0; i < count; ++i)
            {
                if (!jointIndices.ContainsKey(skin.Joints[i]))
                    jointIndices.Add(skin.Joints[i], i);
            }

            var parents = new int[count];
            var roots = new List<int>();
            for (int i = 0; i < count; ++i)
            {
                var parent = skin.Joints[i].Parent;
                // Parents that are not joints are not bones: such a joint is a root.
                if (parent != null && jointIndices.TryGetValue(parent, out var parentIndex))
                    parents[i] = parentIndex;
                else
                {
                    parents[i] = -1;
                    roots.Add(i);
                }
            }

            if (count > 0 && roots.Count != 1)
                throw new NotSupportedException($"Urho3D skeleton must have exactly one root bone, but the skin has {roots.Count} root joints.");

            // Parents first (depth first from the root keeps children of a bone together).
            var order = new List<int>(count);
            var children = new List<int>[count];
            for (int i = 0; i < count; ++i)
                children[i] = new List<int>();
            for (int i = 0; i < count; ++i)
            {
                if (parents[i] >= 0)
                    children[parents[i]].Add(i);
            }
            var stack = new Stack<int>();
            foreach (var root in roots)
            {
                stack.Push(root);
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    order.Add(current);
                    for (int c = children[current].Count - 1; c >= 0; --c)
                        stack.Push(children[current][c]);
                }
            }
            if (order.Count != count)
                throw new NotSupportedException("Skin joints contain a cycle or duplicates.");

            jointToBone = new int[count];
            for (int bone = 0; bone < count; ++bone)
                jointToBone[order[bone]] = bone;

            var skeleton = new Skeleton();
            for (int bone = 0; bone < count; ++bone)
            {
                int joint = order[bone];
                var node = skin.Joints[joint];
                int parentBone = parents[joint] >= 0 ? jointToBone[parents[joint]] : bone;

                // The root bone also gets whatever hangs above the skeleton in the hierarchy.
                var local = parents[joint] >= 0 ? node.Transform.ToTRS() : GetWorldTransform(node);
                var result = new Bone(node.Name ?? $"Bone{bone}", parentBone, local.Translation, local.Rotation, local.Scale);
                result.OffsetMatrix = joint < skin.InverseBindMatrices.Count ? skin.InverseBindMatrices[joint] : Matrix4x4.Identity;
                skeleton.Bones.Add(result);
            }

            if (mesh != null)
                skeleton.UpdateBoundingBoxes(mesh, jointToBone);

            return skeleton;
        }

        private static TRSTransform GetWorldTransform(Node node)
        {
            var matrix = node.Transform.ToMatrix();
            for (var parent = node.Parent; parent != null && !(parent is Scene); parent = parent.Parent)
            {
                matrix = matrix * parent.Transform.ToMatrix();
            }
            if (!Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation))
                throw new InvalidOperationException($"Failed to decompose world matrix of node {node.Name}.");
            return new TRSTransform(translation, rotation, scale);
        }

        /// <summary>
        /// Calculates bone bounding boxes in bone space from the vertices that the bone moves significantly.
        /// </summary>
        private void UpdateBoundingBoxes(UnifiedIndexedMesh mesh, int[] jointToBone)
        {
            if (!mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions) ||
                !mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Joints, out var joints) ||
                !mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Weights, out var weights))
                return;

            var boxes = new BoundingBox3[Bones.Count];
            for (int i = 0; i < boxes.Length; ++i)
                boxes[i] = BoundingBox3.Empty;

            for (int v = 0; v < positions.Count; ++v)
            {
                var j = joints[v];
                var w = weights[v];
                for (int k = 0; k < 4; ++k)
                {
                    float weight = k == 0 ? w.X : k == 1 ? w.Y : k == 2 ? w.Z : w.W;
                    if (weight < 0.33f)
                        continue;
                    int joint = (int)((k == 0 ? j.X : k == 1 ? j.Y : k == 2 ? j.Z : j.W) + 0.5f);
                    if (joint < 0 || joint >= jointToBone.Length)
                        continue;
                    int bone = jointToBone[joint];
                    boxes[bone] = boxes[bone].Merge(Vector3.Transform(positions[v], Bones[bone].OffsetMatrix));
                }
            }

            for (int i = 0; i < boxes.Length; ++i)
            {
                if (!boxes[i].IsEmpty)
                    Bones[i].BoundingBox = boxes[i];
            }
        }
    }
}
