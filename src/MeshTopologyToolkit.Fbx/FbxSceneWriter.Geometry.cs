using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    internal sealed partial class FbxSceneWriter
    {
        /// <summary>Control point weights of a skinned mesh, collected from the joint and weight attributes.</summary>
        private sealed class ControlPointWeights
        {
            public readonly List<(int joint, float weight)>?[] PerPoint;

            public ControlPointWeights(int count)
            {
                PerPoint = new List<(int, float)>?[count];
            }
        }

        private readonly Dictionary<IMesh, ControlPointWeights?> _weights = new Dictionary<IMesh, ControlPointWeights?>();

        private long WriteGeometry(IMesh mesh)
        {
            if (!mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions)
                || !mesh.TryGetAttributeIndices(MeshAttributeKey.Position, out var positionIndices))
                return 0;

            // Triangles as corner indices (an index in the index lists of the attributes) with the material slot.
            var corners = new List<int>();
            var triangleMaterial = new List<int>();
            foreach (var drawCall in mesh.DrawCalls)
            {
                if (drawCall.LodLevel != 0)
                    continue;
                foreach (var face in drawCall.GetFaces())
                {
                    corners.Add(face.A);
                    corners.Add(face.B);
                    corners.Add(face.C);
                    triangleMaterial.Add(drawCall.MaterialIndex);
                }
            }

            long id = NewId();
            var geometry = AddObject("Geometry", id, mesh.Name ?? "Mesh", "Mesh");
            var props = Properties(geometry);
            AddProperty(props, "Color", "ColorRGB", "Color", "", D(0.8), D(0.8), D(0.8));

            var vertices = new double[positions.Count * 3];
            for (int i = 0; i < positions.Count; ++i)
            {
                vertices[i * 3] = positions[i].X;
                vertices[i * 3 + 1] = positions[i].Y;
                vertices[i * 3 + 2] = positions[i].Z;
            }
            geometry.AddChild("Vertices", FbxProperty.FromArray(vertices));

            var polygonVertexIndex = new int[corners.Count];
            for (int i = 0; i < corners.Count; ++i)
            {
                int p = positionIndices[corners[i]];
                polygonVertexIndex[i] = (i % 3) == 2 ? ~p : p;
            }
            geometry.AddChild("PolygonVertexIndex", FbxProperty.FromArray(polygonVertexIndex));
            geometry.AddChild("GeometryVersion", I(124));

            var layer = new FbxNode("Layer");
            layer.Properties.Add(I(0));
            layer.AddChild("Version", I(100));
            var layerElements = new List<string>();

            // Normals.
            IMeshVertexAttribute<Vector3>? normals = null;
            IReadOnlyList<int>? normalIndices = null;
            if (mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var foundNormals) && mesh.TryGetAttributeIndices(MeshAttributeKey.Normal, out var foundNormalIndices))
            {
                normals = foundNormals;
                normalIndices = foundNormalIndices;
                var element = AddLayerElement(geometry, "LayerElementNormal", 0, "", "ByPolygonVertex", "IndexToDirect");
                element.AddChild("Normals", FbxProperty.FromArray(Flatten(foundNormals)));
                element.AddChild("NormalsIndex", FbxProperty.FromArray(Remap(corners, foundNormalIndices)));
                layerElements.Add("LayerElementNormal");
            }

            // Tangents are written per polygon vertex together with the binormals computed from the sign.
            if (mesh.TryGetAttributeIndices(MeshAttributeKey.Tangent, out var tangentIndices) && TryGetTangents(mesh, out var tangents))
            {
                var tangentElement = AddLayerElement(geometry, "LayerElementTangent", 0, "", "ByPolygonVertex", "Direct");
                var binormalElement = AddLayerElement(geometry, "LayerElementBinormal", 0, "", "ByPolygonVertex", "Direct");
                var tangentData = new double[corners.Count * 3];
                var tangentW = new double[corners.Count];
                var binormalData = new double[corners.Count * 3];
                var binormalW = new double[corners.Count];
                bool hasNormals = normals != null && normalIndices != null;
                for (int i = 0; i < corners.Count; ++i)
                {
                    var t = tangents[tangentIndices[corners[i]]];
                    var t3 = new Vector3(t.X, t.Y, t.Z);
                    tangentData[i * 3] = t3.X;
                    tangentData[i * 3 + 1] = t3.Y;
                    tangentData[i * 3 + 2] = t3.Z;
                    tangentW[i] = t.W;
                    var n = hasNormals ? normals![normalIndices![corners[i]]] : Vector3.UnitZ;
                    var b = Vector3.Cross(n, t3) * (t.W < 0.0f ? -1.0f : 1.0f);
                    binormalData[i * 3] = b.X;
                    binormalData[i * 3 + 1] = b.Y;
                    binormalData[i * 3 + 2] = b.Z;
                    binormalW[i] = t.W;
                }
                tangentElement.AddChild("Tangents", FbxProperty.FromArray(tangentData));
                tangentElement.AddChild("TangentsW", FbxProperty.FromArray(tangentW));
                binormalElement.AddChild("Binormals", FbxProperty.FromArray(binormalData));
                binormalElement.AddChild("BinormalsW", FbxProperty.FromArray(binormalW));
                layerElements.Add("LayerElementBinormal");
                layerElements.Add("LayerElementTangent");
            }

            // UV sets and colors.
            foreach (var key in mesh.GetAttributeKeys().Where(k => k.Name == MeshAttributeNames.TexCoord).OrderBy(k => k.Channel))
            {
                if (!mesh.TryGetAttribute<Vector2>(key, out var uvs) || !mesh.TryGetAttributeIndices(key, out var uvIndices))
                    continue;
                var element = AddLayerElement(geometry, "LayerElementUV", key.Channel, "UVChannel_" + (key.Channel + 1), "ByPolygonVertex", "IndexToDirect");
                var data = new double[uvs.Count * 2];
                for (int i = 0; i < uvs.Count; ++i)
                {
                    data[i * 2] = uvs[i].X;
                    data[i * 2 + 1] = uvs[i].Y;
                }
                element.AddChild("UV", FbxProperty.FromArray(data));
                element.AddChild("UVIndex", FbxProperty.FromArray(Remap(corners, uvIndices)));
                layerElements.Add("LayerElementUV");
            }
            foreach (var key in mesh.GetAttributeKeys().Where(k => k.Name == MeshAttributeNames.Color).OrderBy(k => k.Channel))
            {
                if (!mesh.TryGetAttribute<Vector4>(key, out var colors) || !mesh.TryGetAttributeIndices(key, out var colorIndices))
                    continue;
                var element = AddLayerElement(geometry, "LayerElementColor", key.Channel, "", "ByPolygonVertex", "IndexToDirect");
                var data = new double[colors.Count * 4];
                for (int i = 0; i < colors.Count; ++i)
                {
                    data[i * 4] = colors[i].X;
                    data[i * 4 + 1] = colors[i].Y;
                    data[i * 4 + 2] = colors[i].Z;
                    data[i * 4 + 3] = colors[i].W;
                }
                element.AddChild("Colors", FbxProperty.FromArray(data));
                element.AddChild("ColorIndex", FbxProperty.FromArray(Remap(corners, colorIndices)));
                layerElements.Add("LayerElementColor");
            }

            // Materials.
            {
                bool allSame = triangleMaterial.Count == 0 || triangleMaterial.All(m => m == triangleMaterial[0]);
                var element = AddLayerElement(geometry, "LayerElementMaterial", 0, "", allSame ? "AllSame" : "ByPolygon", "IndexToDirect");
                element.AddChild("Materials", FbxProperty.FromArray(allSame ? new[] { triangleMaterial.Count == 0 ? 0 : triangleMaterial[0] } : triangleMaterial.ToArray()));
                layerElements.Add("LayerElementMaterial");
            }

            foreach (var name in layerElements)
            {
                var le = layer.AddChild("LayerElement");
                le.AddChild("Type", S(name));
                le.AddChild("TypedIndex", I(0));
            }
            geometry.Children.Add(layer);

            // Additional UV sets and color sets need their own layers, otherwise only the first one is visible to some readers.
            int extraLayer = 1;
            foreach (var key in mesh.GetAttributeKeys().Where(k => k.Name == MeshAttributeNames.TexCoord && k.Channel > 0).OrderBy(k => k.Channel))
            {
                var extra = new FbxNode("Layer");
                extra.Properties.Add(I(extraLayer++));
                extra.AddChild("Version", I(100));
                var le = extra.AddChild("LayerElement");
                le.AddChild("Type", S("LayerElementUV"));
                le.AddChild("TypedIndex", I(key.Channel));
                geometry.Children.Add(extra);
            }

            return id;
        }

        private FbxNode AddLayerElement(FbxNode geometry, string type, int index, string name, string mapping, string reference)
        {
            var element = geometry.AddChild(type, I(index));
            element.AddChild("Version", I(type == "LayerElementUV" || type == "LayerElementMaterial" ? 101 : 102));
            element.AddChild("Name", S(name));
            element.AddChild("MappingInformationType", S(mapping));
            element.AddChild("ReferenceInformationType", S(reference));
            return element;
        }

        private static bool TryGetTangents(IMesh mesh, out IMeshVertexAttribute<Vector4> tangents)
        {
            if (mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Tangent, out tangents))
                return true;
            if (mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Tangent, out var tangents3))
            {
                var list = new ListMeshVertexAttribute<Vector4>(tangents3.Count);
                for (int i = 0; i < tangents3.Count; ++i)
                    list.Add(new Vector4(tangents3[i], 1.0f));
                tangents = list;
                return true;
            }
            return false;
        }

        private static double[] Flatten(IMeshVertexAttribute<Vector3> values)
        {
            var data = new double[values.Count * 3];
            for (int i = 0; i < values.Count; ++i)
            {
                data[i * 3] = values[i].X;
                data[i * 3 + 1] = values[i].Y;
                data[i * 3 + 2] = values[i].Z;
            }
            return data;
        }

        private static int[] Remap(List<int> corners, IReadOnlyList<int> indices)
        {
            var result = new int[corners.Count];
            for (int i = 0; i < result.Length; ++i)
                result[i] = indices[corners[i]];
            return result;
        }

        // ---------------------------------------------------------------- Skins

        private ControlPointWeights? GetWeights(IMesh mesh)
        {
            if (_weights.TryGetValue(mesh, out var cached))
                return cached;

            ControlPointWeights? result = null;
            if (mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions)
                && mesh.TryGetAttributeIndices(MeshAttributeKey.Position, out var positionIndices)
                && mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Joints, out var joints)
                && mesh.TryGetAttributeIndices(MeshAttributeKey.Joints, out var jointIndices)
                && mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Weights, out var weights)
                && mesh.TryGetAttributeIndices(MeshAttributeKey.Weights, out var weightIndices))
            {
                result = new ControlPointWeights(positions.Count);
                int cornerCount = positionIndices.Count;
                for (int c = 0; c < cornerCount; ++c)
                {
                    int p = positionIndices[c];
                    if (result.PerPoint[p] != null)
                        continue;
                    var list = new List<(int, float)>(4);
                    var j = joints[jointIndices[c]];
                    var w = weights[weightIndices[c]];
                    if (w.X > 0.0f) list.Add(((int)(j.X + 0.5f), w.X));
                    if (w.Y > 0.0f) list.Add(((int)(j.Y + 0.5f), w.Y));
                    if (w.Z > 0.0f) list.Add(((int)(j.Z + 0.5f), w.Z));
                    if (w.W > 0.0f) list.Add(((int)(j.W + 0.5f), w.W));
                    result.PerPoint[p] = list;
                }
            }
            _weights[mesh] = result;
            return result;
        }

        private static double[] ToArray(Matrix4x4 m)
        {
            return new double[]
            {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44,
            };
        }

        private static Matrix4x4 GetWorldMatrix(Node node)
        {
            var world = Matrix4x4.Identity;
            for (var n = node; n != null; n = n.Parent)
                world *= n.Transform.ToMatrix();
            return world;
        }

        private void WriteSkins(IEnumerable<Node> rootNodes)
        {
            var written = new HashSet<(Skin, IMesh)>();
            var poseNodes = new List<(long id, Matrix4x4 world)>();
            var poseSet = new HashSet<long>();

            foreach (var node in EnumerateNodes(rootNodes))
            {
                var reference = node.Mesh;
                if (reference?.Skin == null || reference.Mesh == null || !_geometryIds.TryGetValue(reference.Mesh, out var geometryId) || geometryId == 0)
                    continue;
                var skin = reference.Skin;
                if (!written.Add((skin, reference.Mesh)))
                    continue;
                var weights = GetWeights(reference.Mesh);
                if (weights == null || skin.Joints.Count == 0)
                    continue;

                long skinId = NewId();
                var skinNode = AddObject("Deformer", skinId, skin.Name ?? "Skin", "Skin");
                skinNode.AddChild("Version", I(101));
                skinNode.AddChild("Link_DeformAcuracy", D(50));
                Connect(skinId, geometryId);

                var meshWorld = GetWorldMatrix(node);
                for (int j = 0; j < skin.Joints.Count; ++j)
                {
                    var indexes = new List<int>();
                    var values = new List<double>();
                    for (int p = 0; p < weights.PerPoint.Length; ++p)
                    {
                        var list = weights.PerPoint[p];
                        if (list == null)
                            continue;
                        foreach (var (joint, weight) in list)
                        {
                            if (joint == j)
                            {
                                indexes.Add(p);
                                values.Add(weight);
                            }
                        }
                    }
                    if (indexes.Count == 0)
                        continue;

                    var joint0 = skin.Joints[j];
                    long clusterId = NewId();
                    var cluster = AddObject("Deformer", clusterId, joint0.Name ?? "Joint", "Cluster", "SubDeformer");
                    cluster.AddChild("Version", I(100));
                    cluster.AddChild("UserData", S(""), S(""));
                    cluster.AddChild("Indexes", FbxProperty.FromArray(indexes.ToArray()));
                    cluster.AddChild("Weights", FbxProperty.FromArray(values.ToArray()));

                    // The inverse bind matrix is the matrix from the mesh to the bone, the link matrix is the bone in the world at the bind pose.
                    var meshToBone = j < skin.InverseBindMatrices.Count ? skin.InverseBindMatrices[j] : Matrix4x4.Identity;
                    Matrix4x4.Invert(meshToBone, out var boneToMesh);
                    cluster.AddChild("Transform", FbxProperty.FromArray(ToArray(meshToBone)));
                    cluster.AddChild("TransformLink", FbxProperty.FromArray(ToArray(boneToMesh * meshWorld)));

                    Connect(clusterId, skinId);
                    Connect(_nodeIds[joint0], clusterId);

                    if (poseSet.Add(_nodeIds[joint0]))
                        poseNodes.Add((_nodeIds[joint0], GetWorldMatrix(joint0)));
                }
                if (poseSet.Add(_nodeIds[node]))
                    poseNodes.Add((_nodeIds[node], meshWorld));
            }

            if (poseNodes.Count > 0)
            {
                var pose = AddObject("Pose", NewId(), "BIND_POSES", "BindPose");
                pose.AddChild("Type", S("BindPose"));
                pose.AddChild("Version", I(100));
                pose.AddChild("NbPoseNodes", I(poseNodes.Count));
                foreach (var (id, world) in poseNodes)
                {
                    var poseNode = pose.AddChild("PoseNode");
                    poseNode.AddChild("Node", L(id));
                    poseNode.AddChild("Matrix", FbxProperty.FromArray(ToArray(world)));
                }
            }
        }

        // ---------------------------------------------------------------- Animations

        private static Vector3 UnwrapEuler(Vector3 current, Vector3 previous)
        {
            float Unwrap(float c, float p)
            {
                while (c - p > 180.0f) c -= 360.0f;
                while (c - p < -180.0f) c += 360.0f;
                return c;
            }
            return new Vector3(Unwrap(current.X, previous.X), Unwrap(current.Y, previous.Y), Unwrap(current.Z, previous.Z));
        }

        private void WriteAnimations(IEnumerable<Node> rootNodes)
        {
            var nodesByName = new Dictionary<string, Node>();
            foreach (var node in EnumerateNodes(rootNodes))
                if (node.Name != null && !nodesByName.ContainsKey(node.Name))
                    nodesByName.Add(node.Name, node);

            foreach (var animation in _content.Animations)
            {
                long stackId = NewId();
                var stack = AddObject("AnimationStack", stackId, animation.Name ?? "Take 001", "");
                var stackProps = Properties(stack);
                AddProperty(stackProps, "LocalStart", "KTime", "Time", "", L(ToTicks(animation.StartTime)));
                AddProperty(stackProps, "LocalStop", "KTime", "Time", "", L(ToTicks(animation.EndTime)));
                AddProperty(stackProps, "ReferenceStart", "KTime", "Time", "", L(ToTicks(animation.StartTime)));
                AddProperty(stackProps, "ReferenceStop", "KTime", "Time", "", L(ToTicks(animation.EndTime)));

                long layerId = NewId();
                AddObject("AnimationLayer", layerId, "BaseLayer", "");
                Connect(layerId, stackId);

                foreach (var track in animation.Tracks)
                {
                    var target = track.Target;
                    if (target == null && track.TargetName != null)
                        nodesByName.TryGetValue(track.TargetName, out target);
                    if (target == null || !_nodeIds.TryGetValue(target, out var modelId))
                        continue;

                    var times = Animation.GetSampleTimes(track);
                    if (times.Count == 0)
                        continue;

                    if (track.Translation != null && track.Translation.Keys.Count > 0)
                        WriteVectorCurves(layerId, modelId, "Lcl Translation", "T", times, times.Select(t => track.Translation.Evaluate(t)).ToList());
                    if (track.Rotation != null && track.Rotation.Keys.Count > 0)
                    {
                        var euler = new List<Vector3>(times.Count);
                        foreach (var t in times)
                        {
                            var e = QuaternionToEuler(track.Rotation.Evaluate(t));
                            if (euler.Count > 0)
                                e = UnwrapEuler(e, euler[euler.Count - 1]);
                            euler.Add(e);
                        }
                        WriteVectorCurves(layerId, modelId, "Lcl Rotation", "R", times, euler);
                    }
                    if (track.Scale != null && track.Scale.Keys.Count > 0)
                        WriteVectorCurves(layerId, modelId, "Lcl Scaling", "S", times, times.Select(t => track.Scale.Evaluate(t)).ToList());
                }
            }
        }

        private void WriteVectorCurves(long layerId, long modelId, string property, string name, List<float> times, List<Vector3> values)
        {
            long curveNodeId = NewId();
            var curveNode = AddObject("AnimationCurveNode", curveNodeId, name, "");
            var props = Properties(curveNode);
            AddProperty(props, "d|X", "Number", "", "A", D(values[0].X));
            AddProperty(props, "d|Y", "Number", "", "A", D(values[0].Y));
            AddProperty(props, "d|Z", "Number", "", "A", D(values[0].Z));
            Connect(curveNodeId, layerId);
            ConnectProperty(curveNodeId, modelId, property);

            var ticks = times.Select(t => ToTicks(t)).ToArray();
            for (int component = 0; component < 3; ++component)
            {
                long curveId = NewId();
                var curve = AddObject("AnimationCurve", curveId, "", "");
                var data = values.Select(v => component == 0 ? v.X : component == 1 ? v.Y : v.Z).ToArray();
                curve.AddChild("Default", D(data[0]));
                curve.AddChild("KeyVer", I(4009));
                curve.AddChild("KeyTime", FbxProperty.FromArray(ticks));
                curve.AddChild("KeyValueFloat", FbxProperty.FromArray(data));
                // Linear interpolation: one attribute group used by all keys.
                curve.AddChild("KeyAttrFlags", FbxProperty.FromArray(new[] { 260 }));
                curve.AddChild("KeyAttrDataFloat", FbxProperty.FromArray(new[] { 0.0f, 0.0f, BitConverter.Int32BitsToSingle(218434820), 0.0f }));
                curve.AddChild("KeyAttrRefCount", FbxProperty.FromArray(new[] { data.Length }));
                ConnectProperty(curveId, curveNodeId, component == 0 ? "d|X" : component == 1 ? "d|Y" : "d|Z");
            }
        }
    }
}
