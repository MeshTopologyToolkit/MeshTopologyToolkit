using System.Numerics;

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
            public int ParentIndex { get; set; }
            public Vector3 InitialPosition { get; set; }
            public Quaternion InitialRotation { get; set; }
            public Vector3 InitialScale { get; set; }
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
                var boneCollisionFlags = (BoneCollisionShape)reader.ReadByte();

                var bone = new Bone(name, parentIndex_, initialPosition_, initialRotation_, initialScale_);

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
    }
}
