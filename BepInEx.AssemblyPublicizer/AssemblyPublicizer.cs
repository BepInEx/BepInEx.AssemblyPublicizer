using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace BepInEx.AssemblyPublicizer;

public static class AssemblyPublicizer
{
    public static void Publicize(string assemblyPath, string outputPath, AssemblyPublicizerOptions? options = null)
    {
        var assembly = FatalAsmResolver.FromFile(assemblyPath);
        var module = assembly.ManifestModule ?? throw new NullReferenceException();
        module.MetadataResolver = new DefaultMetadataResolver(NoopAssemblyResolver.Instance);

        Publicize(assembly, options);
        module.FatalWrite(outputPath);
    }

    public static AssemblyDefinition Publicize(AssemblyDefinition assembly, AssemblyPublicizerOptions? options = null)
    {
        options ??= new AssemblyPublicizerOptions();

        var module = assembly.ManifestModule!;

        var attribute = options.IncludeOriginalAttributesAttribute ? new OriginalAttributesAttribute(module) : null;

        foreach (var typeDefinition in module.GetAllTypes())
        {
            if (attribute != null && typeDefinition == attribute.Type)
                continue;

            Publicize(typeDefinition, attribute, options);
        }

        return assembly;
    }

    private static void Publicize(TypeDefinition typeDefinition, OriginalAttributesAttribute? attribute, AssemblyPublicizerOptions options)
    {
        if (options.Strip && !typeDefinition.IsEnum && !typeDefinition.IsInterface)
        {
            foreach (var methodDefinition in typeDefinition.Methods)
            {
                if (!methodDefinition.HasMethodBody)
                    continue;

                var newBody = methodDefinition.CilMethodBody = new CilMethodBody(methodDefinition);
                newBody.Instructions.Add(CilOpCodes.Ldnull);
                newBody.Instructions.Add(CilOpCodes.Throw);
                methodDefinition.NoInlining = true;
            }
        }

        if (!options.PublicizeCompilerGenerated && typeDefinition.IsCompilerGenerated())
            return;

        if (options.HasTarget(PublicizeTarget.Types) && (!typeDefinition.IsNested && !typeDefinition.IsPublic || typeDefinition.IsNested && !typeDefinition.IsNestedPublic))
        {
            if (attribute != null)
                typeDefinition.CustomAttributes.Add(attribute.ToCustomAttribute(typeDefinition.Attributes & TypeAttributes.VisibilityMask));

            typeDefinition.Attributes &= ~TypeAttributes.VisibilityMask;
            typeDefinition.Attributes |= typeDefinition.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;
        }

        if (options.HasTarget(PublicizeTarget.Methods))
        {
            foreach (var methodDefinition in typeDefinition.Methods)
            {
                if (!methodDefinition.IsVirtual || methodDefinition is { IsVirtual: true, IsReuseSlot: true })
                    Publicize(methodDefinition, attribute, options);
            }

            // Special case for accessors generated from auto properties, publicize them regardless of PublicizeCompilerGenerated
            if (!options.PublicizeCompilerGenerated)
            {
                foreach (var propertyDefinition in typeDefinition.Properties)
                {
                    if (propertyDefinition.GetMethod is { } getMethod &&
                        (!getMethod.IsVirtual || getMethod is { IsVirtual: true, IsReuseSlot: true }))
                        Publicize(getMethod, attribute, options, true);
                    if (propertyDefinition.SetMethod is { } setMethod &&
                        (!setMethod.IsVirtual || setMethod is { IsVirtual: true, IsReuseSlot: true }))
                        Publicize(setMethod, attribute, options, true);
                }
            }
        }

        if (options.HasTarget(PublicizeTarget.Fields))
        {
            var eventNames = new HashSet<Utf8String?>(typeDefinition.Events.Select(e => e.Name));
            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.IsPrivateScope)
                    continue;

                if (!fieldDefinition.IsPublic)
                {
                    // Skip event backing fields
                    if (eventNames.Contains(fieldDefinition.Name))
                        continue;

                    if (!options.PublicizeCompilerGenerated && fieldDefinition.IsCompilerGenerated())
                        continue;

                    if (attribute != null)
                        fieldDefinition.CustomAttributes.Add(attribute.ToCustomAttribute(fieldDefinition.Attributes & FieldAttributes.FieldAccessMask));

                    fieldDefinition.Attributes &= ~FieldAttributes.FieldAccessMask;
                    fieldDefinition.Attributes |= FieldAttributes.Public;
                }
            }
        }
    }

    private static void Publicize(MethodDefinition methodDefinition, OriginalAttributesAttribute? attribute, AssemblyPublicizerOptions options, bool ignoreCompilerGeneratedCheck = false)
    {
        if (methodDefinition.IsCompilerControlled)
            return;

        if (!methodDefinition.IsPublic)
        {
            if (!ignoreCompilerGeneratedCheck && !options.PublicizeCompilerGenerated && methodDefinition.IsCompilerGenerated())
                return;

            if (attribute != null)
                methodDefinition.CustomAttributes.Add(attribute.ToCustomAttribute(methodDefinition.Attributes & MethodAttributes.MemberAccessMask));

            methodDefinition.Attributes &= ~MethodAttributes.MemberAccessMask;
            methodDefinition.Attributes |= MethodAttributes.Public;
        }
    }
}
