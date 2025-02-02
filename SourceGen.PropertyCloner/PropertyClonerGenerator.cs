﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PropertyCloner;

[Generator]
public sealed class PropertyClonerGenerator : IIncrementalGenerator
{
    private const string AttributesFile = "PropertyClonerAttributes.g.cs";
    private const string Attribute = @"// <auto-generated/>

namespace PropertyCloner
{
    [global::System.CodeDom.Compiler.GeneratedCode(""PropertyClonerGenerator"", ""1.0.0"")]
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PropertyClonerAttribute : global::System.Attribute
    {
        public PropertyClonerAttribute() { }
    }

    [global::System.CodeDom.Compiler.GeneratedCode(""PropertyClonerGenerator"", ""1.0.0"")]
    [global::System.AttributeUsage(global::System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ClonableAttribute : global::System.Attribute
    {
        public ClonableAttribute() { }
    }
}";

    private sealed class ClassToGenerate
    {
        public string Name { get; }
        public string Namespace { get; }
        public List<PropertyInfo> Properties { get; }
        public string? BaseTypeName { get; }

        public ClassToGenerate(string name, string @namespace, List<PropertyInfo> properties, string? baseTypeName)
        {
            Name = name;
            Namespace = @namespace;
            Properties = properties;
            BaseTypeName = baseTypeName;
        }
    }

    private sealed class PropertyInfo
    {
        public string Name { get; }
        public string Type { get; }
        public bool HasClonableAttribute { get; }
        public bool IsFromBaseClass { get; }

        public PropertyInfo(string name, string type, bool hasClonableAttribute, bool isFromBaseClass = false)
        {
            Name = name;
            Type = type;
            HasClonableAttribute = hasClonableAttribute;
            IsFromBaseClass = isFromBaseClass;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attributes to the compilation
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource(AttributesFile, SourceText.From(Attribute, Encoding.UTF8)));

        // Get all class declarations with our attribute
        IncrementalValuesProvider<ClassToGenerate> classDeclarations =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(m => m is not null)!;

        // Register the source output
        context.RegisterSourceOutput(classDeclarations,
            (spc, source) => Execute(source, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

    private static ClassToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return null;

        // Check if class has our attribute
        bool hasPropertyClonerAttribute = classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("PropertyCloner"));

        if (!hasPropertyClonerAttribute)
            return null;

        // Get the semantic model
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
        if (classSymbol == null) return null;

        var properties = new List<PropertyInfo>();

        // Get properties from the current class
        properties.AddRange(classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => new PropertyInfo(
                p.Identifier.Text,
                p.Type.ToString(),
                p.AttributeLists.SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString().Contains("Clonable"))
            )));

        // Get base type properties
        var baseType = classSymbol.BaseType;
        string? baseTypeName = null;

        if (baseType != null && !baseType.ToString().Equals("object", StringComparison.OrdinalIgnoreCase))
        {
            baseTypeName = baseType.Name;

            // Get all properties from base type that have the Clonable attribute
            var baseProperties = baseType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.GetAttributes()
                    .Any(a => a.AttributeClass?.Name.Contains("Clonable") == true))
                .Select(p => new PropertyInfo(
                    p.Name,
                    p.Type.ToString(),
                    true,
                    true
                ));

            properties.AddRange(baseProperties);
        }

        return new ClassToGenerate(
            classDeclaration.Identifier.Text,
            GetNamespace(classDeclaration),
            properties,
            baseTypeName
        );
    }

    private static void Execute(ClassToGenerate classToGenerate, SourceProductionContext context)
    {
        var propertyAssignments = classToGenerate.Properties
            .Where(p => p.HasClonableAttribute)
            .Select(p => $"            clone.{p.Name} = instance.{p.Name};")
            .ToList();

        string generatedCode = $@"
using System;

namespace {classToGenerate.Namespace}
{{
    public static class {classToGenerate.Name}Extensions
    {{
        public static {classToGenerate.Name} CloneProperties(this {classToGenerate.Name} instance)
        {{
            var clone = new {classToGenerate.Name}();
{string.Join(Environment.NewLine, propertyAssignments)}
            return clone;
        }}
    }}
}}";

        context.AddSource($"{classToGenerate.Name}.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
    }

    private static string GetNamespace(SyntaxNode syntax)
    {
        // Get the namespace declaration
        var namespaceDeclaration = syntax.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDeclaration?.Name.ToString() ?? "GlobalNamespace";
    }
}