namespace MeshTopologyToolkit
{
    public class ListMeshVertexAttribute<T> : List<T>, IMeshVertexAttribute<T>
    {
        public bool TryCast<TTo>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<TTo>? attribute)
        {
            if (typeof(T) == typeof(TTo))
            {
                attribute = (IMeshVertexAttribute<TTo>)this;
                return true;
            }

            if (converterProvider.TryGetConverter<T, TTo>(out var converter))
            {
                attribute = new MeshVertexAttributeAdapter<T, TTo>(this, converter);
                return true;
            }

            attribute = null;
            return false;
        }
    }
}
