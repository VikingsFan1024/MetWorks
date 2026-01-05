namespace DdiCodeGen.Generator;

using DdiCodeGen.SyntaxLoader.Models;
using DdiCodeGen.Generator.Models;
using System.Collections.Generic;
using System.Linq;

// Convenience alias for source model
using SrcModel = DdiCodeGen.SyntaxLoader.Models.Model;

/// <summary>
/// Performs single-pass transformation of source Model to all template-specific record types.
/// Traverses the source model once and extracts data for all templates simultaneously.
/// </summary>
public sealed class ModelTransformer
{
    private readonly SrcModel _sourceModel;
    private readonly CodeGen _codeGen;

    public ModelTransformer(SrcModel sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _codeGen = sourceModel.CodeGen ?? throw new ArgumentNullException(nameof(sourceModel.CodeGen));
    }

    /// <summary>
    /// Performs single-pass transformation of the source model.
    /// Returns all template-specific models ready for rendering.
    /// </summary>
    public TransformationResult TransformAll()
    {
        var result = new TransformationResult(_codeGen);

        // Single pass through instances - extract data for all templates simultaneously
        foreach (var instance in _sourceModel.Instances)
        {
            TransformInstance(instance, result);
        }

        // Finalize all models with accumulated instance data
        result.FinalizeAll();

        return result;
    }

    /// <summary>
    /// Extracts data from a single instance for all applicable templates in one pass.
    /// </summary>
    private void TransformInstance(
        Instance instance,
        TransformationResult result
    )
    {
        result.RegisterInstance(instance.InstanceName!);

        // Accessors template (instance list aggregation)
        result.AddAccessorInstance(
            new AccessorsModels.Instance {
                Name = instance.InstanceName!,
                ClassQualified = instance.ClassQualified!,
                IsArray = instance.InstanceIsArray!,
                InterfaceQualified = instance.InterfaceQualified
            }
        );

        // Registry template (instance list aggregation)
        result.AddRegistryInstance(new RegistryModels.Instance
        {
            Name = instance.InstanceName!,
            HasAssignments = instance.HasAssignments,
            HasDisposable = false  // TODO: Determine from source model
        });

        // Instance.Factory template (per-instance)
        result.SetInstanceFactoryData(instance.InstanceName!, new InstanceFactoryModels.Instance
        {
            Name = instance.InstanceName!,
            ClassQualified = instance.ClassQualified!,
            IsArray = instance.InstanceIsArray!,
            HasElements = instance.HasElements
        });

        // Instance.Field template (per-instance)
        result.SetInstanceFieldData(instance.InstanceName!, new InstanceFieldModels.Instance
        {
            Name = instance.InstanceName!,
            ClassQualified = instance.ClassQualified!,
            IsArray = instance.InstanceIsArray!
        });

        // Elements.Initializer template (per-instance)
        if (instance.HasElements)
        {
            result.SetElementsInitializerData(
                instance.InstanceName!, new ElementsInitializerModels.Instance {
                    Name = instance.InstanceName!,
                    ClassQualified = instance.ClassQualified!,
                    IsArray = instance.InstanceIsArray!,
                    ElementsConstructionExpression = instance.ElementsConstructionExpression
                }
            );
        }

        // Assignments.Initializer template (per-instance)
        if (instance.HasAssignments)
        {
            result.SetAssignmentsInitializerData(
                instance.InstanceName!, new AssignmentInitializerModels.Instance {
                    Name = instance.InstanceName!,
                    HasAssignments = instance.HasAssignments,
                    Assignments = ExtractAssignments(instance)
                }
            );
        }
    }

    /// <summary>
    /// Extracts assignment data from an instance.
    /// </summary>
    private List<AssignmentInitializerModels.Assignment> ExtractAssignments(Instance instance)
    {
        return instance.Assignments
            .Select(a => new AssignmentInitializerModels.Assignment
            {
                ParameterName = a.Name
                    ?? throw new InvalidOperationException($"Assignment missing name on instance {instance.InstanceName}"),
                InitializerArgumentExpression = a.InitializerParameterAssignmentClause
                    ?? string.Empty
            })
            .ToList();
    }
}

/// <summary>
/// Contains transformed template-specific models built from a single pass through the source model.
/// Separates accumulation phase from finalization phase.
/// </summary>
public sealed class TransformationResult
{
    private readonly CodeGen _codeGen;
    private readonly string _generationTimeUtc = DateTime.UtcNow.ToString("o");
    private readonly List<string> _instanceOrder = new();
    private readonly HashSet<string> _instanceNameSet = new();
    private bool _finalized;

    // Accumulated instance data for list-based templates
    private readonly List<AccessorsModels.Instance> _accessorInstances = new();
    private readonly List<RegistryModels.Instance> _registryInstances = new();

    // Per-instance template data (keyed by instance name)
    private readonly Dictionary<string, InstanceFactoryModels.Instance> _instanceFactoryData = new();
    private readonly Dictionary<string, InstanceFieldModels.Instance> _instanceFieldData = new();
    private readonly Dictionary<string, ElementsInitializerModels.Instance> _elementsInitializerData = new();
    private readonly Dictionary<string, AssignmentInitializerModels.Instance> _assignmentsInitializerData = new();

    // Finalized models
    private AccessorsModels.Model? _accessorsModel;
    private RegistryModels.Model? _registryModel;
    private Dictionary<string, InstanceFactoryModels.Model>? _instanceFactoryModels;
    private Dictionary<string, InstanceFieldModels.Model>? _instanceFieldModels;
    private Dictionary<string, ElementsInitializerModels.Model>? _elementsInitializerModels;
    private Dictionary<string, AssignmentInitializerModels.Model>? _assignmentsInitializerModels;

    public TransformationResult(CodeGen codeGen)
    {
        _codeGen = codeGen ?? throw new ArgumentNullException(nameof(codeGen));
    }

    #region Accumulation Phase (populated during single pass)

    public void AddAccessorInstance(AccessorsModels.Instance instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        _accessorInstances.Add(instance);
    }

    public void AddRegistryInstance(RegistryModels.Instance instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        _registryInstances.Add(instance);
    }

    public void RegisterInstance(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new ArgumentException("Instance name required", nameof(instanceName));

        if (!_instanceNameSet.Add(instanceName))
            throw new InvalidOperationException($"Duplicate instance name '{instanceName}' detected during transformation.");

        _instanceOrder.Add(instanceName);
    }

    public void SetInstanceFactoryData(string instanceName, InstanceFactoryModels.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _instanceFactoryData[instanceName] = data;
    }

    public void SetInstanceFieldData(string instanceName, InstanceFieldModels.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _instanceFieldData[instanceName] = data;
    }

    public void SetElementsInitializerData(string instanceName, ElementsInitializerModels.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _elementsInitializerData[instanceName] = data;
    }

    public void SetAssignmentsInitializerData(string instanceName, AssignmentInitializerModels.Instance data)
    {
        if (string.IsNullOrEmpty(instanceName)) throw new ArgumentException("Instance name required", nameof(instanceName));
        if (data == null) throw new ArgumentNullException(nameof(data));
        _assignmentsInitializerData[instanceName] = data;
    }

    #endregion

    #region Finalization Phase (called after accumulation)

    /// <summary>
    /// Finalizes all accumulated data into complete ModelBase records.
    /// Must be called once after all accumulation is complete.
    /// </summary>
    public void FinalizeAll()
    {
        _accessorsModel = new AccessorsModels.Model
        {
            GenerationTimeRoundTripUtc = _generationTimeUtc,
            TemplateRequested = EnumToInfo[TemplateEnum.Accessors].Name,
            Namespace = _codeGen.Namespace!,
            ContainerClass = _codeGen.RegistryClass!,
            Instances = _accessorInstances
        };

        _registryModel = new RegistryModels.Model
        {
            GenerationTimeRoundTripUtc = _generationTimeUtc,
            TemplateRequested = EnumToInfo[TemplateEnum.Registry].Name,
            Namespace = _codeGen.Namespace!,
            ContainerClass = _codeGen.RegistryClass!,
            Instances = _registryInstances
        };

        _instanceFactoryModels = _instanceFactoryData.ToDictionary(
            kvp => kvp.Key,
            kvp => new InstanceFactoryModels.Model
            {
                GenerationTimeRoundTripUtc = _generationTimeUtc,
                TemplateRequested = EnumToInfo[TemplateEnum.InstanceFactory].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                Instance = kvp.Value
            });

        _instanceFieldModels = _instanceFieldData.ToDictionary(
            kvp => kvp.Key,
            kvp => new InstanceFieldModels.Model
            {
                GenerationTimeRoundTripUtc = _generationTimeUtc,
                TemplateRequested = EnumToInfo[TemplateEnum.InstanceField].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                Instance = kvp.Value
            });

        _elementsInitializerModels = _elementsInitializerData.ToDictionary(
            kvp => kvp.Key,
            kvp => new ElementsInitializerModels.Model
            {
                GenerationTimeRoundTripUtc = _generationTimeUtc,
                TemplateRequested = EnumToInfo[TemplateEnum.ElementsInitializer].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                Instance = kvp.Value
            });

        _assignmentsInitializerModels = _assignmentsInitializerData.ToDictionary(
            kvp => kvp.Key,
            kvp => new AssignmentInitializerModels.Model
            {
                GenerationTimeRoundTripUtc = _generationTimeUtc,
                TemplateRequested = EnumToInfo[TemplateEnum.AssignmentsInitializer].Name,
                Namespace = _codeGen.Namespace!,
                ContainerClass = _codeGen.RegistryClass!,
                InitializerName = _codeGen.Initializer
                    ?? throw new InvalidOperationException("Initializer is required for assignments initializer template."),
                Instance = kvp.Value
            });

            _finalized = true;
    }

    #endregion

    #region Result Access (after FinalizeAll)

    /// <summary>
    /// Gets the finalized Accessors model. Must call FinalizeAll() first.
    /// </summary>
    public AccessorsModels.Model AccessorsModel
    {
        get => _accessorsModel ?? throw new InvalidOperationException("FinalizeAll() must be called first");
    }

    /// <summary>
    /// Gets the finalized Registry model. Must call FinalizeAll() first.
    /// </summary>
    public RegistryModels.Model RegistryModel
    {
        get => _registryModel ?? throw new InvalidOperationException("FinalizeAll() must be called first");
    }

    /// <summary>
    /// Gets per-instance factory data by instance name.
    /// </summary>
    public InstanceFactoryModels.Model GetInstanceFactoryData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_instanceFactoryModels is null || !_instanceFactoryModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No factory data for instance '{instanceName}'");
        return model;
    }

    /// <summary>
    /// Gets per-instance field data by instance name.
    /// </summary>
    public InstanceFieldModels.Model GetInstanceFieldData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_instanceFieldModels is null || !_instanceFieldModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No field data for instance '{instanceName}'");
        return model;
    }

    /// <summary>
    /// Gets per-instance elements initializer data by instance name.
    /// </summary>
    public ElementsInitializerModels.Model GetElementsInitializerData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_elementsInitializerModels is null || !_elementsInitializerModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No elements initializer data for instance '{instanceName}'");
        return model;
    }

    /// <summary>
    /// Gets per-instance assignments initializer data by instance name.
    /// </summary>
    public AssignmentInitializerModels.Model GetAssignmentsInitializerData(string instanceName)
    {
        if (!_finalized)
            throw new InvalidOperationException("FinalizeAll() must be called first");

        if (_assignmentsInitializerModels is null || !_assignmentsInitializerModels.TryGetValue(instanceName, out var model))
            throw new KeyNotFoundException($"No assignments initializer data for instance '{instanceName}'");

        return model;
    }

    /// <summary>
    /// Gets all accumulated instance names for iteration.
    /// </summary>
    public IEnumerable<string> AllInstanceNames => _finalized
        ? _instanceOrder
        : throw new InvalidOperationException("FinalizeAll() must be called first");

    #endregion
}