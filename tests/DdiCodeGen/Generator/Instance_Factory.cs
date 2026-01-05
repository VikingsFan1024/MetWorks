using DdiCodeGen.Generator.Models.Instance.Factory;

public class Instance_Factory
{
    readonly Loader Loader = new();
    const string FixturesPath = @"../../../fixtures/Instance_Factory";
    [Fact]
    public void Primitive_Array_Instance()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
}
