# DDI Future Features & Roadmap

This document tracks planned enhancements to the Declarative Dependency Injection (DDI) code generator that have been intentionally postponed but should be implemented in future iterations.

## Status: Planned / Not Yet Implemented

All features listed here are **deferred** to future work. This document serves as the canonical backlog for DDI enhancements.

---

## 1. Constructor Parameter Support

**Current State:**  
All classes referenced in YAML must have parameterless constructors. Initialization is handled via a separate `InitializeAsync` method with parameters defined in `initializerParameters`.

**Planned Enhancement:**  
Support classes with parameterized constructors using the same parameter definition pattern as initializers.

### Design Approach
- Add optional `constructorParameters` to class definitions in YAML (identical structure to `initializerParameters`)
- Allow parameter values to be assigned via:
  - Literal values (`assignedValue`)
  - References to other named instances (`assignedNamedInstance`)
  - References to interfaces (`qualifiedInterfaceName`)
- Generate constructor invocations in `*_InstanceFactory.Create()` methods instead of `new()`

### Example YAML (Future)
```yaml
namespaces:
  - namespaceName: "DatabaseAccess"
    classes:
      - className: "PostgresConnection"
        qualifiedInterfaceName: "IDbConnection"
        constructorParameters:
          - parameterName: "connectionString"
            qualifiedClassName: "System.String"
          - parameterName: "timeout"
            qualifiedClassName: "System.Int32"

namedInstances:
  - namedInstanceName: "MainDbConnection"
    qualifiedClassName: "DatabaseAccess.PostgresConnection"
    assignments:
      - parameterName: "connectionString"
        assignedValue: "Host=localhost;Database=mydb"
      - parameterName: "timeout"
        assignedValue: "30"
```

### Generated Code (Future)
```csharp
public static PostgresConnection Create(Registry registry)
{
    // Constructor-driven instance: construct with parameters
    var instance = new PostgresConnection(
        connectionString: "Host=localhost;Database=mydb",
        timeout: 30
    );
    registry.RegisterMainDbConnection(instance);
    return instance;
}
```

### Benefits
- More idiomatic C# (many libraries use constructor injection)
- Allows use of `readonly` fields initialized via constructor
- Immutable object support

### Implementation Tasks
- [ ] Extend YAML schema with `constructorParameters`
- [ ] Update validator to handle constructor parameter definitions
- [ ] Modify `InstanceFactory` template to generate constructor calls
- [ ] Add tests for constructor-based instantiation
- [ ] Update documentation

---

## 2. Lifetime Scopes (Singleton, Scoped, Transient)

**Current State:**  
All instances are effectively singletons - created once during `Registry.CreateAll()` and stored in backing fields. There is no Microsoft.Extensions.DependencyInjection integration at present.

**Planned Enhancement:**  
Support standard DI lifetime scopes: Singleton, Scoped, and Transient, with integration into Microsoft.Extensions.DependencyInjection container.

**Note:** This feature was previously explored but deferred during simplification. Registration templates (`Registration.hbs`, `Registration.Fragment.hbs`) were removed as orphaned code.

### Design Approach
- Add `lifetime` property to named instance definitions (default: `Singleton`)
- Modify code generation based on lifetime:
  - **Singleton**: Current behavior (instance stored in backing field)
  - **Scoped**: Register factory with DI container, create per scope
  - **Transient**: Register factory with DI container, create on every resolution

### Example YAML (Future)
```yaml
namedInstances:
  - namedInstanceName: "TheFileLogger"
    qualifiedClassName: "Logging.FileLogger"
    lifetime: "Singleton"  # default
    assignments: [...]

  - namedInstanceName: "RequestContext"
    qualifiedClassName: "Web.RequestContext"
    lifetime: "Scoped"
    assignments: [...]

  - namedInstanceName: "CommandHandler"
    qualifiedClassName: "Commands.Handler"
    lifetime: "Transient"
    assignments: [...]
```

### Generated Code Patterns (Future)

**Singleton** (current behavior):
```csharp
private Logging.FileLogger _TheFileLoggerInstance;
public void RegisterTheFileLogger(Logging.FileLogger instance) =>
    _TheFileLoggerInstance = instance;
```

**Scoped**:
```csharp
services.AddScoped<Web.RequestContext>(sp => 
{
    var instance = new Web.RequestContext();
    await instance.InitializeAsync(...);
    return instance;
});
```

**Transient**:
```csharp
services.AddTransient<Commands.Handler>(sp => 
{
    var instance = new Commands.Handler();
    await instance.InitializeAsync(...);
    return instance;
});
```

### Considerations
- Singleton instances can reference Scoped/Transient instances via factories
- Scoped/Transient instances cannot be "named" in the same way (no backing field)
- May need to generate separate DI registration code path for non-singleton lifetimes
- Async initialization for Scoped/Transient is complex (cannot `await` in factory lambda)

### Implementation Tasks
- [ ] Add `lifetime` enum to YAML schema
- [ ] Update validator to check lifetime validity
- [ ] Create separate code generation paths for each lifetime
- [ ] Handle async initialization for non-singleton lifetimes
- [ ] Add tests for all three lifetime scopes
- [ ] Document lifetime scope behavior and limitations

---

## 3. Lazy Creation

**Current State:**  
All instances are created eagerly during `Registry.CreateAll()` - invoked synchronously at startup.

**Planned Enhancement:**  
Support lazy creation where instances are only created when first accessed.

### Design Approach
- Add `eagerLoad: false` flag to named instance definitions (default: `true`)
- For lazy instances:
  - Generate `Lazy<T>` backing fields instead of direct instance fields
  - Wrap factory logic in lazy initializer
  - First access triggers creation

### Example YAML (Future)
```yaml
namedInstances:
  - namedInstanceName: "TheFileLogger"
    qualifiedClassName: "Logging.FileLogger"
    eagerLoad: true  # created during CreateAll()
    assignments: [...]

  - namedInstanceName: "ExpensiveResource"
    qualifiedClassName: "Resources.HeavyProcessor"
    eagerLoad: false  # created on first access
    assignments: [...]
```

### Generated Code (Future)

**Eager** (current behavior):
```csharp
private Logging.FileLogger _TheFileLoggerInstance;

public void CreateAll()
{
    TheFileLogger_InstanceFactory.Create(this);
    // ...
}
```

**Lazy**:
```csharp
private readonly Lazy<Resources.HeavyProcessor> _ExpensiveResourceInstance = 
    new Lazy<Resources.HeavyProcessor>(() => 
    {
        var instance = new Resources.HeavyProcessor();
        return instance;
    });

public Resources.HeavyProcessor GetExpensiveResource() =>
    _ExpensiveResourceInstance.Value;
```

### Considerations
- Lazy creation + async initialization is tricky (`Lazy<T>` doesn't support async factories)
- May need `AsyncLazy<T>` pattern for lazy + async initialization
- Thread-safety: `Lazy<T>` handles this by default
- Circular dependencies become runtime errors instead of generation-time errors

### Implementation Tasks
- [ ] Add `eagerLoad` boolean flag to YAML schema
- [ ] Generate `Lazy<T>` backing fields for lazy instances
- [ ] Handle async initialization with lazy creation (research AsyncLazy pattern)
- [ ] Update `CreateAll()` to skip lazy instances
- [ ] Add tests for lazy creation scenarios
- [ ] Document lazy creation behavior and limitations

---

## 4. Lazy Initialization

**Current State:**  
All instances with assignments are initialized during `Registry.InitializeAllAsync()` - invoked asynchronously after creation but before use.

**Planned Enhancement:**  
Support lazy initialization where `InitializeAsync` is only called when the instance is first accessed, not during startup.

### Design Approach
- Add `lazyInitialization: true` flag to named instance definitions (default: `false`)
- For lazy-initialized instances:
  - Create instance during `CreateAll()` (or lazily if `eagerLoad: false`)
  - Skip initialization during `InitializeAllAsync()`
  - Generate accessor wrapper that checks initialization state and calls `InitializeAsync` on first access
  - Use `SemaphoreSlim` or similar for thread-safe initialization

### Example YAML (Future)
```yaml
namedInstances:
  - namedInstanceName: "DatabaseConnection"
    qualifiedClassName: "Database.Connection"
    eagerLoad: true
    lazyInitialization: true  # created immediately, initialized on first use
    assignments:
      - parameterName: "connectionString"
        assignedValue: "..."
```

### Generated Code Pattern (Future)
```csharp
private Database.Connection _DatabaseConnectionInstance;
private bool _DatabaseConnectionInitialized = false;
private readonly SemaphoreSlim _DatabaseConnectionInitLock = new(1, 1);

public async Task<IDbConnection> GetDatabaseConnectionAsync()
{
    if (!_DatabaseConnectionInitialized)
    {
        await _DatabaseConnectionInitLock.WaitAsync();
        try
        {
            if (!_DatabaseConnectionInitialized)
            {
                await DatabaseConnection_Initializer.Initialize_DatabaseConnectionAsync(this);
                _DatabaseConnectionInitialized = true;
            }
        }
        finally
        {
            _DatabaseConnectionInitLock.Release();
        }
    }
    return _DatabaseConnectionInstance;
}
```

### Considerations
- Lazy initialization means accessors must be `async Task<T>` instead of synchronous
- This is a breaking change to accessor signatures
- May need both sync and async accessor variants
- Introduces runtime initialization errors (not caught at startup)
- Combines poorly with eager initialization of dependent instances

### Implementation Tasks
- [ ] Add `lazyInitialization` boolean flag to YAML schema
- [ ] Generate async accessor methods for lazy-initialized instances
- [ ] Add thread-safe initialization guards (double-check locking pattern)
- [ ] Handle initialization errors gracefully
- [ ] Update dependent instance resolution to handle async accessors
- [ ] Add tests for lazy initialization
- [ ] Document initialization timing and async accessor patterns

---

## 5. Combined Lazy Creation + Lazy Initialization

**Current State:**  
Creation and initialization are two separate eager phases.

**Planned Enhancement:**  
Support instances that are both lazily created AND lazily initialized - true "on-demand" instances.

### Design Approach
Combine patterns from sections 3 and 4:
- `eagerLoad: false` + `lazyInitialization: true`
- First access triggers both creation and initialization
- Use `AsyncLazy<T>` pattern to handle async initialization in lazy factory

### Example YAML (Future)
```yaml
namedInstances:
  - namedInstanceName: "HeavyResource"
    qualifiedClassName: "Resources.ExpensiveService"
    eagerLoad: false        # don't create at startup
    lazyInitialization: true # don't initialize at startup
    assignments:
      - parameterName: "config"
        assignedValue: "..."
```

### Generated Code Pattern (Future)
```csharp
private readonly AsyncLazy<Resources.ExpensiveService> _HeavyResourceInstance = 
    new AsyncLazy<Resources.ExpensiveService>(async () => 
    {
        var instance = new Resources.ExpensiveService();
        await instance.InitializeAsync(config: "...");
        return instance;
    });

public Task<IExpensiveService> GetHeavyResourceAsync() =>
    _HeavyResourceInstance.Value;
```

### Considerations
- Requires `AsyncLazy<T>` helper class (not in BCL, must be implemented or referenced)
- All accessors become `Task<T>` - no synchronous access possible
- Initialization errors occur at first access, not at startup
- Testing becomes harder (need to trigger initialization in tests)

### Implementation Tasks
- [ ] Implement or reference `AsyncLazy<T>` helper
- [ ] Generate combined lazy creation + initialization code
- [ ] Add validation to prevent invalid combinations of flags
- [ ] Add comprehensive tests for combined lazy behavior
- [ ] Document on-demand initialization patterns

---

## Priority and Sequencing

Suggested implementation order:

1. **Constructor Parameter Support** - Relatively isolated change, high value
2. **Lazy Creation** - Foundation for other lazy features
3. **Lazy Initialization** - Builds on lazy creation
4. **Lifetime Scopes** - More complex, requires DI container integration
5. **Combined Lazy Creation + Initialization** - Requires 2 & 3 first

---

## Related Documentation

- [docs/declarative-di.md](./declarative-di.md) - Current implementation reference
- [docs/YAMLAuthoring/README-yaml-authoring.md](./YAMLAuthoring/README-yaml-authoring.md) - YAML schema guide (to be updated with new features)

---

## Contributing

When implementing any of these features:

1. Update this document to mark features as "In Progress" or "Completed"
2. Update YAML schema documentation
3. Add validator rules for new properties
4. Generate new templates or modify existing ones
5. Add comprehensive tests (unit + integration)
6. Update `docs/declarative-di.md` with new capabilities
7. Update `maximal-valid.yaml` fixture with examples

---

_Last updated: 2026-01-01_  
_Document created to track postponed DDI features_
