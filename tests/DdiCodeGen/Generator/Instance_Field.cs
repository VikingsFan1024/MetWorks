using DdiCodeGen.Generator.Models.Instance.Field;

public class Instance_Field
{
    readonly Loader Loader = new();
    const string FixturesPath = @"../../../fixtures/Instance_Field";
    [Fact]
    public void Instance_with_Mixed_Assignments()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
}
