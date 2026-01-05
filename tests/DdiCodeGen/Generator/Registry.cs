using DdiCodeGen.Generator.Models.Registry;

public class Registry
{
    const string FixturesPath = @"../../../fixtures/Registry";
    [Fact]
    public void All()
    {
        RunAccessorsTest<Model>(
            fixturesPath: FixturesPath
        );
    }
}
