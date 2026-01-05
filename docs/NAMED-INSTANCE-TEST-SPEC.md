# Named Instance Test Specification

## Named Instance Rules

### Core Constraints
1. **Construction:** All instances use either primitive type OR parameterless constructor only
2. **Required:** `qualifiedClassName` (must be specified)
3. **Optional:** `qualifiedInterfaceName` (if class implements interface)
4. **Mutually Exclusive:** Cannot have both `elements` and `assignments`

### Type Categories
- **Non-Array:** Regular class instances
- **Array:** Array types (e.g., `String[]`, `MyClass[]`)

---

## Named Instance Variations

### Variation Matrix

```
┌─────────────────┬──────────┬────────────┬─────────────────┬──────────────┐
│ Variation       │ Elements │ Assignments│ Interface       │ Scenario ID  │
├─────────────────┼──────────┼────────────┼─────────────────┼──────────────┤
│ Simple          │ ❌        │ ❌         │ Optional        │ 1            │
│ With Init       │ ❌        │ ✅         │ Optional        │ 2a/2b/2c     │
│ Array           │ ✅      │ ❌         │ Optional*       │ 3a/3b        │
└─────────────────┴──────────┴────────────┴─────────────────┴──────────────┘

* Array interfaces like ISettingConfiguration[] are supported
```

---

## Scenario 1: Simple Instance (No Initialization)

**Characteristics:**
- Non-array class
- No elements
- No assignments
- May have interface

**YAML Example:**
```yaml
namedInstances:
  - namedInstanceName: "RootCancellationTokenSource"
    qualifiedClassName: "System.Threading.CancellationTokenSource"
    qualifiedInterfaceName: null
    assignments: []
    elements: []
```

**Generated Files:**
- ✅ `{Name}_InstanceField.g.cs` - Private backing field
- ✅ `{Name}_InstanceFactory.g.cs` - Creates with `new()`
- ✅ `Registry.Accessors.g.cs` - Register/Get/Get_Internal methods
- ❌ No `_Initializer.g.cs`
- ❌ No `_ElementsInitializer.g.cs`

**Test Assertions:**
- Factory creates instance with parameterless constructor
- Field declared as concrete type
- Accessor returns concrete type (no interface)
- Registry.CreateAll() invokes factory
- Registry.InitializeAllAsync() does NOT call initializer

---

## Scenario 2a: Instance with Primitive Assignments

**Characteristics:**
- Non-array class
- No elements
- Assignments with literal values only
- May have interface

**YAML Example:**
```yaml
namedInstances:
  - namedInstanceName: "TheFileLogger"
    qualifiedClassName: "Logging.FileLogger"
    qualifiedInterfaceName: "InterfaceDefinition.IFileLogger"
    assignments:
      - parameterName: "fileSizeLimitBytes"
        assignedValue: "10485760"
      - parameterName: "minimumLevel"
        assignedValue: "Information"
      - parameterName: "rollOnFileSizeLimit"
        assignedValue: "true"
    elements: []
```

**Generated Files:**
- ✅ `{Name}_InstanceField.g.cs`
- ✅ `{Name}_InstanceFactory.g.cs`
- ✅ `{Name}_Initializer.g.cs` - Calls `InitializeAsync(...)` with literal values
- ✅ `Registry.Accessors.g.cs` - Dual accessor (external=interface, internal=concrete)
- ❌ No `_ElementsInitializer.g.cs`

**Test Assertions:**
- Initializer contains correct parameter assignments
- Literal values correctly typed (int, string, bool)
- String values are quoted
- Boolean values are lowercase (true/false)
- Registry.InitializeAllAsync() calls initializer
- External accessor returns interface type
- Internal accessor returns concrete type

---

## Scenario 2b: Instance with Named Instance References

**Characteristics:**
- Non-array class
- No elements
- Assignments referencing other named instances
- May have interface

**YAML Example:**
```yaml
namedInstances:
  - namedInstanceName: "TheUDPSettingsRepository"
    qualifiedClassName: "Settings.SettingsRepository"
    qualifiedInterfaceName: "InterfaceDefinition.ISettingsRepository"
    assignments:
      - parameterName: "iFileLogger"
        assignedNamedInstance: "TheFileLogger"
      - parameterName: "settingConfigurations"
        assignedNamedInstance: "TheUdpPortSettings"
    elements: []
```

**Generated Files:**
- ✅ `{Name}_Initializer.g.cs` - Calls `registry.Get{Name}_Internal()` for each reference

**Test Assertions:**
- Initializer uses `registry.Get{ReferencedInstance}_Internal()` calls
- Correct parameter names
- References resolved in dependency order
- Registry accessor methods exist for referenced instances

---

## Scenario 2c: Instance with Mixed Assignments

**Characteristics:**
- Non-array class
- No elements
- Assignments with mix of literals, named instance references, and null values
- May have interface

**YAML Example:**
```yaml
namedInstances:
  - namedInstanceName: "UdpTemperatureSetting"
    qualifiedClassName: "Settings.SettingConfiguration"
    qualifiedInterfaceName: "InterfaceDefinition.ISettingConfiguration"
    assignments:
      - parameterName: "defaultValue"
        assignedValue: "degree fahrenheit"
      - parameterName: "isEditable"
        assignedValue: "true"
      - parameterName: "enumValues"
        assignedNamedInstance: "TemperatureOptions"
      - parameterName: "isSecret"
        assignedValue: null
    elements: []
```

**Generated Files:**
- ✅ `{Name}_Initializer.g.cs` - Mixes literal values, registry calls, and null

**Test Assertions:**
- Literal values correctly formatted
- Named instance references use `registry.Get{Name}_Internal()`
- Null values passed as `null` (not string "null")
- Parameters in correct order
- Handles nullable parameters (e.g., `int?`, `string?`)

---

## Scenario 3a: Primitive Array

**Characteristics:**
- Array type (`String[]`, `Int32[]`, etc.)
- Non-empty elements
- Elements have `assignedValue` (literals)
- No assignments
- No interface (arrays don't typically have interfaces)

**YAML Example:**
```yaml
namedInstances:
  - namedInstanceName: "TemperatureOptions"
    qualifiedClassName: "System.String[]"
    qualifiedInterfaceName: null
    assignments: []
    elements:
      - assignedValue: "degree celsius"
        assignedNamedInstance: null
      - assignedValue: "degree fahrenheit"
        assignedNamedInstance: null
```

**Generated Files:**
- ✅ `{Name}_InstanceField.g.cs`
- ✅ `{Name}_InstanceFactory.g.cs` - Calls `_ElementsInitializer.CreateElements(registry)`
- ✅ `{Name}_ElementsInitializer.g.cs` - Returns `new[] { "value1", "value2" }`
- ❌ No `_Initializer.g.cs`

**Test Assertions:**
- ElementsInitializer creates array with correct type
- Array initialization syntax: `new[] { ... }`
- Element values correctly typed and formatted
- InstanceFactory delegates to ElementsInitializer
- Registry.CreateAll() invokes factory
- No initialization in Registry.InitializeAllAsync()

---

## Scenario 3b: Named Instance Array

**Characteristics:**
- Array type (`MyClass[]`, `IInterface[]`)
- Non-empty elements
- Elements have `assignedNamedInstance` (references)
- No assignments
- May have array interface (`IInterface[]`)

**YAML Example:**
```yaml
namedInstances:
  - namedInstanceName: "TheUdpListenerSettings"
    qualifiedClassName: "Settings.SettingConfiguration[]"
    qualifiedInterfaceName: "InterfaceDefinition.ISettingConfiguration[]"
    assignments: []
    elements:
      - assignedValue: null
        assignedNamedInstance: "UdpTemperatureSetting"
      - assignedValue: null
        assignedNamedInstance: "UdpWindspeedSetting"
      - assignedValue: null
        assignedNamedInstance: "UdpPressureSetting"
```

**Generated Files:**
- ✅ `{Name}_ElementsInitializer.g.cs` - Returns `new[] { registry.Get{Ref1}_Internal(), registry.Get{Ref2}_Internal() }`
- ✅ Dual accessor if interface present

**Test Assertions:**
- ElementsInitializer calls `registry.Get{Name}_Internal()` for each element
- Array created with correct concrete type
- Elements resolved in correct order
- External accessor returns array interface if specified
- Internal accessor returns concrete array type

---

## Validation Rules (Must Fail)

### Rule 1: Arrays Cannot Have Assignments
```yaml
# ❌ INVALID
namedInstances:
  - namedInstanceName: "Invalid"
    qualifiedClassName: "System.String[]"
    assignments:
      - parameterName: "something"
        assignedValue: "value"
    elements:
      - assignedValue: "test"
```

**Expected Error:** "Arrays cannot have assignments"

---

### Rule 2: Non-Arrays Cannot Have Elements
```yaml
# ❌ INVALID
namedInstances:
  - namedInstanceName: "Invalid"
    qualifiedClassName: "MyClass"
    assignments: []
    elements:
      - assignedValue: "test"
```

**Expected Error:** "Non-arrays cannot have elements"

---

### Rule 3: Arrays Must Have Non-Empty Elements
```yaml
# ❌ INVALID
namedInstances:
  - namedInstanceName: "Invalid"
    qualifiedClassName: "System.String[]"
    assignments: []
    elements: []
```

**Expected Error:** "Arrays must have non-empty elements"

---

### Rule 4: Assignment Cannot Have Both Value and Reference
```yaml
# ❌ INVALID
namedInstances:
  - namedInstanceName: "Invalid"
    qualifiedClassName: "MyClass"
    assignments:
      - parameterName: "param"
        assignedValue: "value"
        assignedNamedInstance: "SomeInstance"
    elements: []
```

**Expected Error:** "Assignment cannot specify both assignedValue and assignedNamedInstance"

---

### Rule 5: Element Cannot Have Both Value and Reference
```yaml
# ❌ INVALID
namedInstances:
  - namedInstanceName: "Invalid"
    qualifiedClassName: "System.String[]"
    assignments: []
    elements:
      - assignedValue: "value"
        assignedNamedInstance: "SomeInstance"
```

**Expected Error:** "Element cannot specify both assignedValue and assignedNamedInstance"

---

## Test Implementation Template

```csharp
public class NamedInstanceGenerationTests
{
    private readonly ITestOutputHelper _output;
    private readonly TemplateStore _templateStore;
    
    public NamedInstanceGenerationTests(ITestOutputHelper output)
    {
        _output = output;
        _templateStore = new TemplateStore();
    }
    
    private IDictionary<string, string> GenerateFromYaml(string yaml)
    {
        var loader = new Loader();
        var model = loader.Load(yaml);
        var generator = new CodeGenerator(_templateStore);
        return generator.GenerateFiles(model);
    }
    
    // Scenario 1
    [Fact]
    public void Scenario1_SimpleInstance_GeneratesFactoryAndField()
    {
        var yaml = @"
namedInstances:
  - namedInstanceName: RootCancellationTokenSource
    qualifiedClassName: System.Threading.CancellationTokenSource
    assignments: []
    elements: []";
        
        var files = GenerateFromYaml(yaml);
        
        // Assert files exist
        Assert.Contains("RootCancellationTokenSource_InstanceFactory.g.cs", files.Keys);
        Assert.Contains("RootCancellationTokenSource_InstanceField.g.cs", files.Keys);
        
        // Assert files don't exist
        Assert.DoesNotContain("RootCancellationTokenSource_Initializer.g.cs", files.Keys);
        Assert.DoesNotContain("RootCancellationTokenSource_ElementsInitializer.g.cs", files.Keys);
        
        // Assert factory content
        var factory = files["RootCancellationTokenSource_InstanceFactory.g.cs"];
        Assert.Contains("new System.Threading.CancellationTokenSource()", factory);
        
        _output.WriteLine(factory);
    }
    
    // Scenario 2a
    [Fact]
    public void Scenario2a_PrimitiveAssignments_GeneratesInitializer()
    {
        // ... implementation
    }
    
    // Scenario 2b
    [Fact]
    public void Scenario2b_InstanceReferences_UsesInternalAccessors()
    {
        // ... implementation
    }
    
    // Scenario 2c
    [Fact]
    public void Scenario2c_MixedAssignments_HandlesAllTypes()
    {
        // ... implementation
    }
    
    // Scenario 3a
    [Fact]
    public void Scenario3a_PrimitiveArray_GeneratesElementsInitializer()
    {
        // ... implementation
    }
    
    // Scenario 3b
    [Fact]
    public void Scenario3b_InstanceArray_UsesRegistryReferences()
    {
        // ... implementation
    }
    
    // Validation Tests
    [Fact]
    public void Validate_ArrayWithAssignments_Fails()
    {
        // ... implementation
    }
    
    [Fact]
    public void Validate_NonArrayWithElements_Fails()
    {
        // ... implementation
    }
    
    [Fact]
    public void Validate_ArrayWithEmptyElements_Fails()
    {
        // ... implementation
    }
    
    [Fact]
    public void Validate_AssignmentWithBothValueAndReference_Fails()
    {
        // ... implementation
    }
}
```

---

## Summary: Generated Files by Scenario

| Scenario | _InstanceField | _InstanceFactory | _Initializer | _ElementsInitializer | Registry.Accessors |
|----------|----------------|------------------|--------------|----------------------|-------------------|
| 1        | ✅             | ✅               | ❌           | ❌                   | ✅ (single)       |
| 2a       | ✅             | ✅               | ✅           | ❌                   | ✅ (dual if interface) |
| 2b       | ✅             | ✅               | ✅           | ❌                   | ✅ (dual if interface) |
| 2c       | ✅             | ✅               | ✅           | ❌                   | ✅ (dual if interface) |
| 3a       | ✅             | ✅               | ❌           | ✅                   | ✅ (single)       |
| 3b       | ✅             | ✅               | ❌           | ✅                   | ✅ (dual if interface) |

---
Scenario 1:  Simple Instance (No Initialization)
Scenario 2a: Instance with Primitive Assignments
Scenario 2b: Instance with Named Instance References
Scenario 2c: Instance with Mixed Assignments
Scenario 3a: Primitive Array
Scenario 3b: Named Instance Array

_Last Updated: 2026-01-03_
_This specification defines all valid Named Instance variations and their expected code generation output._
