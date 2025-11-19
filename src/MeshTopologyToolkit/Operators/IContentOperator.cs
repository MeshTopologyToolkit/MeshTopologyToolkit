namespace MeshTopologyToolkit.Operators
{
    public interface IContentOperator
    {
        FileContainer Transform(FileContainer container);

        IMesh? Tramsform(IMesh? mesh);
    }
}
