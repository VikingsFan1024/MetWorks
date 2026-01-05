namespace DdiCodeGen.TemplateStore;
/// <summary>
/// Represents a store of templates that can be retrieved by name.
/// </summary>
public interface ITemplateStore
{
    /// <summary>
    /// Gets the template content for the specified template name
    /// </summary>
    /// <param name="name">The name of the template to retrieve.</param>
    /// <returns>The template content as a string.</returns>
    string GetTemplate(string name);
    string GetTemplate(TemplateEnum templateEnum);
    /// <summary>
    /// Lists partial template names
    /// </summary>
    IEnumerable<string> GetPartialTemplateNames();
}