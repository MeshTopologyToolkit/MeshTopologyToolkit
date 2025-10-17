namespace MeshTopologyToolkit
{
    public interface IMeshVertexAttributeConverterProvider
    {
        bool TryGetConverter<TFrom, TTo>(out IMeshVertexAttributeConverter<TFrom, TTo>? converter);
    }
}
