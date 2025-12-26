// using Microsoft.CodeAnalysis;
//
// namespace Snatch.SourceGenerators.Generators.StaticViewLocator;
//
// [Generator]
// internal sealed class Generator : IIncrementalGenerator
// {
//     private static readonly string AssemblyName = typeof(Generator).Assembly.GetName().Name;
//     private static readonly string AttributeName = "StaticViewLocatorAttribute";
//     private static readonly string AttributeFullName = $"{AssemblyName}.{AttributeName}";
//     private static readonly string Attribute = $$"""
//         namespace {{AssemblyName}}.Attributes;
//
//         [AttributeUsage(AttributeTargets.Class)]
//         public sealed class StaticViewLocatorAttribute : Attribute { }
//         """;
//
//     public const string Name = "StaticViewLocator";
//     public const string Id = "SVLG";
//
//     public void Initialize(IncrementalGeneratorInitializationContext context)
//     {
//         context.RegisterPostInitializationOutput(initializationContext =>
//         {
//             initializationContext.AddSource($"{AssemblyName}.{AttributeName}.g.cs", Attribute);
//         });
//
//         context.SyntaxProvider.ForAttributeWithMetadataName(
//             AttributeFullName,
//             (node, token) =>
//             {
//
//             }
//         );
//     }
// }
