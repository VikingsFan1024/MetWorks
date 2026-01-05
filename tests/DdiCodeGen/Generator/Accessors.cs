using DdiCodeGen.Generator.Models.Accessors;

public class Accessors
{
    readonly Loader Loader = new();
    const string FixturesPath = @"../../../fixtures/Accessors";
    [Fact]
    public void Instance_with_Mixed_Assignments()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
    [Fact]
    public void Instance_with_Named_Instance_References()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
    [Fact]
    public void Instance_with_Primitive_Assignments()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
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
    [Fact]
    public void Simple_Instance_No_Initialization()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
}
