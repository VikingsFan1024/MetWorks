global using DdiCodeGen.SyntaxLoader.Models;
global using System.Collections.Generic;
global using System;
global using DdiCodeGen.TemplateStore;
global using System.Linq;
global using HandlebarsDotNet;
global using System.Dynamic;
global using DdiCodeGen.Shared;
global using static DdiCodeGen.TemplateStore.TemplateDictionary;
// Template-specific model aliases to avoid ambiguity
global using AccessorsModels = DdiCodeGen.Generator.Models.Accessors;
global using RegistryModels = DdiCodeGen.Generator.Models.Registry;
global using AssignmentInitializerModels = DdiCodeGen.Generator.Models.Instance.Assignments.Initializer;
global using ElementsInitializerModels = DdiCodeGen.Generator.Models.Elements.Initializer;
global using InstanceFactoryModels = DdiCodeGen.Generator.Models.Instance.Factory;
global using InstanceFieldModels = DdiCodeGen.Generator.Models.Instance.Field;
