using DdiCodeGen.Generator.Models;
using System.Runtime.CompilerServices;
public static class YamlTestHelper
{
    public static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine("fixtures", name));
    public static void RunAccessorsTest<T>(
        string fixturesPath,
        [CallerMemberName] string callerName = ""
    ) where T : ModelBase
    {
        string testPath = Path.Combine(fixturesPath, callerName);
        var modelInputPath = Path.Combine(testPath, "Model-input.json");
        var model = LoadJsonFixture<T>(modelInputPath);
        if (model is null)
            throw new InvalidOperationException($"Failed to load model from {modelInputPath}");

        var TemplateInfo = NameToInfo[model.TemplateRequested];
        var _templateStore = new TemplateStore();
        var _templateRenderer = new TemplateRenderer(_templateStore);
        var result = _templateRenderer.Render(TemplateInfo.TemplateEnum, model);

        var expectedResultsPath = Path.Combine(testPath, "expected-result.txt");
        var actualResultsPath = Path.Combine(testPath, "actual-result.txt");

        File.WriteAllText(actualResultsPath, result);

        var same = File.ReadAllText(expectedResultsPath)
            .Equals(File.ReadAllText(actualResultsPath), StringComparison.Ordinal);

        Assert.True(same, $"Generated code does not match expected results. See {expectedResultsPath} and {actualResultsPath}.");        
    }
    public static T LoadJsonFixture<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Fixture deserialized to null");
    }
}