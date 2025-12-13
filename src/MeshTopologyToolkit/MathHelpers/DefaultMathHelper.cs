using System.Diagnostics;

namespace MeshTopologyToolkit.MathHelpers
{
    public class DefaultMathHelper<T> : IMathHelper<T>
    {
        public DefaultMathHelper()
        {
            Trace.WriteLine($"Type {typeof(T).FullName} is not fully supported. Modify MathHelper<T> static constructor to support the type.");
        }
        public T Lerp(T from, T to, float amount)
        {
            return from;
        }
    }

}
