namespace MeshTopologyToolkit.MathHelpers
{
    public class ScalarMathHelper: IMathHelper<float>
    {
        public float Lerp(float from, float to, float amount)
        {
            return from + (to - from) * amount;
        }
    }
}
