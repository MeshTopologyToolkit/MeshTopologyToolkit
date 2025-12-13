namespace MeshTopologyToolkit
{
    public interface IMeshVertexAttributeConverter
    {
    }

    public interface IMeshVertexAttributeConverter<TFrom, TTo> : IMeshVertexAttributeConverter
    {
        TTo Convert(TFrom value);
    }
}
