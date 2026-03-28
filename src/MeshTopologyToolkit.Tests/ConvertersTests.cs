namespace MeshTopologyToolkit.Tests;

public class ConvertersTests
{
    [Fact]
    public void AllConvertesUsedInDefault()
    {
        var provider = MeshVertexAttributeConverterProvider.Default;

        typeof(IMeshVertexAttributeConverter).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMeshVertexAttributeConverter).IsAssignableFrom(t))
            .SelectMany(t => t.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMeshVertexAttributeConverter<,>)))
            .Select(i => (From: i.GetGenericArguments()[0], To: i.GetGenericArguments()[1]))
            .ToList()
            .ForEach(k =>
            {
                Assert.True(provider.TryGetConverter(k.From, k.To, out var converter), $"No converter found for {k.From} to {k.To}");
                Assert.True(typeof(IMeshVertexAttributeConverter<,>).MakeGenericType(k.From, k.To).IsInstanceOfType(converter), $"Invalid converter, expected IMeshVertexAttributeConverter<{k.From},{k.To}>");
            });
    }
}
