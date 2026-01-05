# Partial Templates - Implementation Summary

## What We've Done

✅ **Created partials infrastructure:**
- `Templates/_partials/` directory
- `GeneratedFileHeader.hbs` - File header pattern
- `AccessorTriple.hbs` - Register + dual accessor pattern
- `README.md` - Partials documentation
- `Registry.Accessors.REFACTORED.hbs` - Example refactored template

## Current State

**Template Loading:** `TemplateRenderer.Render()` creates a new Handlebars instance per render call
```csharp
public static string Render(string template, object ctx)
{
    var compiled = Handlebars.Compile(template);  // New instance each time
    return compiled(ctx);
}
```

**Problem:** Partials need to be registered on the Handlebars instance *before* compiling templates.

## Required Changes

### Option A: Shared Handlebars Instance (Recommended)

**Change `TemplateRenderer.cs`:**

```csharp
namespace DdiCodeGen.Generator;

public static class TemplateRenderer
{
    private static readonly Lazy<IHandlebars> _handlebarsInstance = new(() => CreateHandlebarsWithPartials());
    
    private static IHandlebars CreateHandlebarsWithPartials()
    {
        var handlebars = Handlebars.Create();
        
        // Load partials from embedded resources
        var assembly = typeof(TemplateRenderer).Assembly;
        var partialResources = assembly.GetManifestResourceNames()
            .Where(r => r.Contains(".Templates._partials.") && r.EndsWith(".hbs"));
        
        foreach (var resourceName in partialResources)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            
            using var reader = new StreamReader(stream);
            var partialContent = reader.ReadToEnd();
            
            // Extract partial name: "DdiCodeGen.TemplateStore.Templates._partials.GeneratedFileHeader.hbs"
            // -> "GeneratedFileHeader"
            var parts = resourceName.Split('.');
            var partialName = parts[^2]; // Second to last element before ".hbs"
            
            handlebars.RegisterTemplate(partialName, partialContent);
        }
        
        return handlebars;
    }
    
    public static string Render(string template, object ctx)
    {
        var compiled = _handlebarsInstance.Value.Compile(template);
        return compiled(ctx);
    }
}
```

### Option B: Pass Handlebars Instance (Alternative)

If you want more control, pass the Handlebars instance as a parameter:

```csharp
public static string Render(IHandlebars handlebars, string template, object ctx)
{
    var compiled = handlebars.Compile(template);
    return compiled(ctx);
}
```

Then initialize once at generator startup:
```csharp
var handlebars = CreateHandlebarsWithPartials();
// Pass to all Render() calls
```

## Testing the Changes

### 1. Verify Partial Loading

Add this test to ensure partials are registered:

```csharp
[Fact]
public void Partials_AreRegistered()
{
    var template = "{{> GeneratedFileHeader TemplateName='Test' GeneratedHeader='2026-01-01'}}";
    var result = TemplateRenderer.Render(template, new { });
    
    Assert.Contains("// Test", result);
    Assert.Contains("// 2026-01-01", result);
    Assert.Contains("#nullable enable", result);
}
```

### 2. Compare Output

Generate code with refactored template and compare:

```bash
# Generate with current Registry.Accessors.hbs
# Save output as Registry.Accessors.CURRENT.g.cs

# Rename Registry.Accessors.REFACTORED.hbs to Registry.Accessors.hbs
# Generate again
# Save output as Registry.Accessors.REFACTORED.g.cs

# Compare
diff Registry.Accessors.CURRENT.g.cs Registry.Accessors.REFACTORED.g.cs
```

Should be **identical** (except possibly whitespace).

### 3. Check Embedded Resources

Verify partials are embedded:

```bash
# After build, check assembly
dotnet build src/DdiCodeGen/TemplateStore/TemplateStore.csproj

# List embedded resources
ilspy src/DdiCodeGen/TemplateStore/bin/Debug/net10.0/TemplateStore.dll --list-resources | grep -i partial
```

Should see:
```
DdiCodeGen.TemplateStore.Templates._partials.GeneratedFileHeader.hbs
DdiCodeGen.TemplateStore.Templates._partials.AccessorTriple.hbs
```

## Migration Checklist

- [ ] Update `TemplateRenderer.cs` to register partials
- [ ] Build and verify partials are embedded resources
- [ ] Add unit test for partial loading
- [ ] Test refactored template produces identical output
- [ ] Rename `.REFACTORED.hbs` → `.hbs` (replace original)
- [ ] Refactor remaining templates to use `GeneratedFileHeader` partial
- [ ] Delete old template versions
- [ ] Update documentation

## Benefits Once Complete

1. ✅ **DRY** - File header defined once, used in 6+ templates
2. ✅ **Consistency** - One place to change header format
3. ✅ **Modularity** - Reusable template components
4. ✅ **Testability** - Test partials independently
5. ✅ **Maintainability** - Clear component library

## Next Steps

1. Implement Option A changes in `TemplateRenderer.cs`
2. Run tests to verify partials load
3. Generate code and compare output
4. If identical, proceed with full template migration

---

**Questions?** See `docs/TEMPLATE-REFACTORING-GUIDE.md` for detailed explanation.
