using DdiCodeGen.Generator.Models.Instance.Assignments.Initializer;

public class Assignments_Initializer
{
    readonly Loader Loader = new();
    const string FixturesPath = @"../../../fixtures/Assignments_Initializer";
    [Fact]
    public void Instance_with_Primitive_Assignments()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
}
