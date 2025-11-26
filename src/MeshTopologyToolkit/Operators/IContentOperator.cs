namespace MeshTopologyToolkit.Operators
{
    public interface IContentOperator
    {
        FileContainer Transform(FileContainer container);

        IMesh Transform(IMesh mesh);

        Material Transform(Material mesh);

        Texture Transform(Texture mesh);
    }
}
