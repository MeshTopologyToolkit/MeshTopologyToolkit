namespace MeshTopologyToolkit.Urho3D
{
    public class Urho3DModelVersionFlags
    {
        public string Magic { get; }
        public bool HasVertexDeclarations { get; }
        public bool HasMorphWeights { get; }
        public bool HasVersion { get; }
        public int Version { get; }

        public Urho3DModelVersionFlags(Urho3DModelVersion version)
        {
            Magic = "UMDL";
            HasVertexDeclarations = false;
            HasMorphWeights = false;
            Version = 1;

            if (version >= Urho3DModelVersion.VertexDeclarations)
            {
                Magic = "UMD2";
                HasVertexDeclarations = true;
            }
            if (version >= Urho3DModelVersion.Rbfx)
            {
                HasVersion = true;
                Version = (version - Urho3DModelVersion.Rbfx) + 1;
                Magic = "UMD3";
            }
            if (version >= Urho3DModelVersion.MorphWeightVersion)
            {
                HasMorphWeights = true;
            }
        }
    }

}
