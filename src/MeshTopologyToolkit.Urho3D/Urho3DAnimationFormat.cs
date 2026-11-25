using MeshTopologyToolkit.Operators;
using System.Numerics;
using System.Text;

namespace MeshTopologyToolkit.Urho3D
{
    /// <summary>
    /// Urho3D animation file (.ani).
    /// </summary>
    /// <remarks>
    /// Urho3D animations only have tracks of bone (node) names with keyframes of position, rotation and scale.
    /// A loaded <see cref="Animation"/> has tracks with <see cref="AnimationTrack.TargetName"/> set and no <see cref="AnimationTrack.Target"/>:
    /// call <see cref="Animation.Bind(Node)"/> to bind them to nodes of a scene.
    /// To save an animation that was loaded from another format use <see cref="TryWrite(IFileSystemEntry, Animation, bool)"/>.
    /// </remarks>
    public class Urho3DAnimationFormat : IFileFormat
    {
        private const string FileId = "UANI";

        private const byte ChannelPosition = 1;
        private const byte ChannelRotation = 2;
        private const byte ChannelScale = 4;

        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Urho3D animation", ".ani"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public SpaceTransform FormatToGltfTransform => new SpaceTransform(SpaceTransform._XYZ, flipFaceIndices: true);

        /// <summary>
        /// Samples per second for curves that cannot be stored as is (cubic spline). Urho3D animations only have linear keyframes.
        /// </summary>
        public float CubicSampleRate { get; set; } = 30.0f;

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer() { FileToGltfTransform = this.FormatToGltfTransform };
            if (!entry.Exists)
                return false;

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;

                using (var reader = new BinaryReader(stream))
                {
                    var magic = reader.ReadBytes(4);
                    if (magic.Length != 4 || Encoding.ASCII.GetString(magic) != FileId)
                        return false;

                    var name = reader.ReadStringZ(Encoding.UTF8);
                    var length = reader.ReadSingle();
                    var animation = new Animation(name);

                    var numTracks = reader.ReadUInt32();
                    for (uint trackIndex = 0; trackIndex < numTracks; ++trackIndex)
                    {
                        var track = new AnimationTrack(null, reader.ReadStringZ(Encoding.UTF8));
                        var mask = reader.ReadByte();
                        var numKeys = reader.ReadUInt32();

                        var translation = (mask & ChannelPosition) != 0 ? AnimationCurve.CreateVector3() : null;
                        var rotation = (mask & ChannelRotation) != 0 ? AnimationCurve.CreateQuaternion() : null;
                        var scale = (mask & ChannelScale) != 0 ? AnimationCurve.CreateVector3() : null;

                        for (uint keyIndex = 0; keyIndex < numKeys; ++keyIndex)
                        {
                            var time = reader.ReadSingle();
                            if (translation != null)
                                translation.Add(time, reader.ReadVector3());
                            if (rotation != null)
                            {
                                var q = reader.ReadVector4();
                                rotation.Add(time, new Quaternion(q.Y, q.Z, q.W, q.X));
                            }
                            if (scale != null)
                                scale.Add(time, reader.ReadVector3());
                        }

                        track.Translation = translation;
                        track.Rotation = rotation;
                        track.Scale = scale;
                        animation.Tracks.Add(track);
                    }

                    content.Add(animation);
                    return true;
                }
            }
        }

        /// <summary>
        /// Writes an animation of the container (expected to be in Urho3D space).
        /// If the container has several animations, the one named as the file is written, or the first one.
        /// </summary>
        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            if (content.Animations.Count == 0)
                return false;

            var fileName = Path.GetFileNameWithoutExtension(entry.Name);
            var animation = content.Animations.FirstOrDefault(_ => _.Name == fileName) ?? content.Animations[0];
            return Write(entry, animation);
        }

        /// <summary>
        /// Writes an animation that can be loaded from any format.
        /// </summary>
        /// <param name="entry">File to write.</param>
        /// <param name="animation">Animation to write.</param>
        /// <param name="fromGltfSpace">True if the animation is in glTF space (right handed, Y up, meters), as animations are when read by
        /// a <see cref="FileFormatCollection"/> that forces glTF space or by a glTF file format. The animation is converted to Urho3D space.</param>
        public bool TryWrite(IFileSystemEntry entry, Animation animation, bool fromGltfSpace = true)
        {
            if (fromGltfSpace)
            {
                var transform = this.FormatToGltfTransform.Invert();
                animation = new SpaceTransformOperator(transform).Transform(animation);
            }
            return Write(entry, animation);
        }

        private bool Write(IFileSystemEntry entry, Animation animation)
        {
            using (var stream = entry.OpenWrite())
            {
                if (stream == null)
                    return false;

                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Encoding.ASCII.GetBytes(FileId));
                    WriteStringZ(writer, animation.Name ?? string.Empty);
                    writer.Write(animation.Duration);

                    var tracks = animation.Tracks.Where(_ => _.NodeName != null).ToList();
                    writer.Write((uint)tracks.Count);
                    float start = animation.StartTime;
                    foreach (var track in tracks)
                    {
                        WriteStringZ(writer, track.NodeName!);

                        bool hasTranslation = track.Translation != null && track.Translation.Keys.Count > 0;
                        bool hasRotation = track.Rotation != null && track.Rotation.Keys.Count > 0;
                        bool hasScale = track.Scale != null && track.Scale.Keys.Count > 0;
                        byte mask = (byte)((hasTranslation ? ChannelPosition : 0) | (hasRotation ? ChannelRotation : 0) | (hasScale ? ChannelScale : 0));
                        writer.Write(mask);

                        var times = Animation.GetSampleTimes(track, CubicSampleRate);
                        writer.Write((uint)times.Count);

                        Quaternion previous = Quaternion.Identity;
                        bool hasPrevious = false;
                        foreach (var time in times)
                        {
                            writer.Write(time - start);
                            if (hasTranslation)
                                writer.Write(track.Translation!.Evaluate(time));
                            if (hasRotation)
                            {
                                var q = Quaternion.Normalize(track.Rotation!.Evaluate(time));
                                // Keep rotations on the short path, as Urho3D blends keyframes linearly.
                                if (hasPrevious && Quaternion.Dot(previous, q) < 0.0f)
                                    q = Quaternion.Negate(q);
                                previous = q;
                                hasPrevious = true;
                                writer.Write(q.W);
                                writer.Write(q.X);
                                writer.Write(q.Y);
                                writer.Write(q.Z);
                            }
                            if (hasScale)
                                writer.Write(track.Scale!.Evaluate(time));
                        }
                    }
                }
            }
            return true;
        }

        private static void WriteStringZ(BinaryWriter writer, string value)
        {
            writer.Write(Encoding.UTF8.GetBytes(value));
            writer.Write((byte)0);
        }
    }
}
