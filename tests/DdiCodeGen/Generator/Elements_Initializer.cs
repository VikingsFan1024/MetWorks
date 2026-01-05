using DdiCodeGen.Generator.Models.Elements.Initializer;

public class Elements_Initializer
{
    readonly Loader Loader = new();
    const string FixturesPath = @"../../../fixtures/Elements_Initializer";
    [Fact]
    public void Primitive_Array_Instance()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }

    [Fact]
    public void Named_Instance_Array()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
}
