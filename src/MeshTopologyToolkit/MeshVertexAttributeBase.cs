namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeBase<T>
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
                attribute = new MeshVertexAttributeAdapter<T, TTo>((IMeshVertexAttribute<T>)this, converter);
                return true;
            }

            attribute = null;
            return false;
        }
    }
}
