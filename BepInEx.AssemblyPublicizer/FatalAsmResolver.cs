using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Serialized;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Builder;

namespace BepInEx.AssemblyPublicizer;

internal static class FatalAsmResolver
{
    /// Same as <see cref="AssemblyDefinition.FromFile(string)"/> but throws only on fatal errors
    public static AssemblyDefinition FromFile(string filePath)
    {
        return AssemblyDefinition.FromImage(PEImage.FromFile(filePath), new ModuleReaderParameters(FatalThrowErrorListener.Instance));
    }

    private sealed class FatalThrowErrorListener : IErrorListener
    {
        public static FatalThrowErrorListener Instance { get; } = new();

        public IList<Exception> Exceptions { get; } = new List<Exception>();

        /// <inheritdoc />
        public void MarkAsFatal()
        {
            throw new AggregateException(Exceptions);
        }

        /// <inheritdoc />
        public void RegisterException(Exception exception) => Exceptions.Add(exception);
    }

    /// Same as <see cref="ModuleDefinition.Write(string)"/> but throws only on fatal errors
    public static void FatalWrite(this ModuleDefinition module, string filePath)
    {
        var result = new ManagedPEImageBuilder().CreateImage(module);
        if (result.HasFailed)
        {
            var errorListener = (FatalThrowErrorListener) result.ErrorListener;
            throw new AggregateException("Construction of the PE image failed with one or more errors.", errorListener.Exceptions);
        }

        using var fileStream = File.Create(filePath);
        new ManagedPEFileBuilder().CreateFile(result.ConstructedImage).Write(new BinaryStreamWriter(fileStream));
    }
}
