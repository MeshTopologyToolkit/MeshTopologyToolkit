using MeshTopologyToolkit.MathHelpers;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class MathHelper<T>
    {
        public static IMathHelper<T> Default { get; set; }

        static MathHelper()
        {
            switch (typeof(T))
            {
                case var t when t == typeof(float):
                    Default = (IMathHelper<T>)new ScalarMathHelper();
                    break;
                case var t when t == typeof(Vector2):
                    Default = (IMathHelper<T>)new Vector2MathHelper();
                    break;
                case var t when t == typeof(Vector3):
                    Default = (IMathHelper<T>)new Vector3MathHelper();
                    break;
                case var t when t == typeof(Vector4):
                    Default = (IMathHelper<T>)new Vector4MathHelper();
                    break;
                default:
                    Default = (IMathHelper<T>)new DefaultMathHelper<T>();
                    break;
            }
        }
    }
}
