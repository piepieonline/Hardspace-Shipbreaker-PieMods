using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class ModdedShipLoaderPreloadPatcher
{
    public static ManualLogSource logSource;

    public static void Initialize()
    {
        logSource = Logger.CreateLogSource("Modded Ship Loader Preloader");
    }

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    // Patches the assemblies
    public static void Patch(AssemblyDefinition assembly)
    {
        ModuleDefinition module = assembly.MainModule;

        TypeDefinition entityData = module.Types.First(t => t.FullName == "CutterScript");

        /*
        entityData.Fields.Add(new FieldDefinition("customId", FieldAttributes.Public | FieldAttributes.Static, module.ImportReference(typeof(int))));
        entityData.Fields.Add(new FieldDefinition("customType", FieldAttributes.Public, module.ImportReference(typeof(int))));

        entityData.Fields.Add(new FieldDefinition("customData", FieldAttributes.Public,
            module.ImportReference(typeof(Dictionary<,>)).MakeGenericInstanceType(module.TypeSystem.String, module.TypeSystem.Object)));
        */

        var newType = new TypeDefinition("BBI.Unity.Game", "AddressableLoader", TypeAttributes.Class | TypeAttributes.Public, entityData.BaseType);

        newType.Fields.Add(new FieldDefinition("refs", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        newType.Fields.Add(new FieldDefinition("assetGUID", FieldAttributes.Public, module.ImportReference(typeof(string))));
        newType.Fields.Add(new FieldDefinition("childPath", FieldAttributes.Public, module.ImportReference(typeof(string))));

        newType.Fields.Add(new FieldDefinition("disabledChildren", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        assembly.MainModule.Types.Add(newType);


        var newSOType = new TypeDefinition("BBI.Unity.Game", "AddressableSOLoader", TypeAttributes.Class | TypeAttributes.Public, entityData.BaseType);

        newSOType.Fields.Add(new FieldDefinition("onChild", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("comp", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("field", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("refs", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        assembly.MainModule.Types.Add(newSOType);

        logSource.LogInfo("Preloader patching (AssemblyCSharp) is successful!");
    }
}


public static class ModdedShipLoaderPreloadPatcher2
{
    public static ManualLogSource logSource;

    public static void Initialize()
    {
        logSource = Logger.CreateLogSource("Modded Ship Loader Preloader2");

        /*
        var assemblyResolver = new DefaultAssemblyResolver();
        var assemblyLocation = System.IO.Path.GetDirectoryName("D:\\Games\\Xbox\\Hardspace- Shipbreaker\\Content\\Shipbreaker_Data\\Managed");
        assemblyResolver.AddSearchDirectory(assemblyLocation);
        */
    }

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "BBI.Unity.Game.dll" };

    public static string[] ResolveDirectories { get; set; } =
        {
            "D:\\Games\\Xbox\\Hardspace- Shipbreaker\\Content\\Shipbreaker_Data\\Managed\\"
        };

    // Patches the assemblies
    public static void Patch(AssemblyDefinition assembly)
    {
        ModuleDefinition module = assembly.MainModule;

        // var resolver = (BaseAssemblyResolver)assembly.AssemblyResolver;
        var moduleResolver = (BaseAssemblyResolver)module.AssemblyResolver;

        foreach (var dir in ResolveDirectories)
        {
            // resolver.AddSearchDirectory(dir);
            moduleResolver.AddSearchDirectory(dir);
        }

        // resolver.ResolveFailure += ResolverOnResolveFailure;
        // Add our dependency resolver to the assembly resolver of the module we are patching
        moduleResolver.ResolveFailure += ResolverOnResolveFailure;

        // monoModder.PerformPatches(monoModPath);

        // Then remove our resolver after we are done patching to not interfere with other patchers
        // moduleResolver.ResolveFailure -= ResolverOnResolveFailure;

        var typesToModify = System.IO.File.ReadAllLines(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "TypesToModify.txt"));

        foreach(var typeName in typesToModify)
        {
            assembly.MainModule.Types.Where(t => t.Name == typeName).First().Fields.Add(new FieldDefinition("AssetBasis", FieldAttributes.Public, module.ImportReference(typeof(string))));
        }

        logSource.LogInfo("Preloader patching (BBIUnityGame) is successful!");
    }

    private static AssemblyDefinition ResolverOnResolveFailure(object sender, AssemblyNameReference reference)
    {
        logSource.LogInfo($"Failure to find {reference.Name}");

        foreach (var directory in ResolveDirectories)
        {
            var potentialDirectories = new List<string> { directory };

            potentialDirectories.AddRange(Directory.GetDirectories(directory, "*", SearchOption.AllDirectories));

            var potentialFiles = potentialDirectories.Select(x => Path.Combine(x, $"{reference.Name}.dll"))
                                                     .Concat(potentialDirectories.Select(
                                                                 x => Path.Combine(x, $"{reference.Name}.exe")));

            foreach (string path in potentialFiles)
            {
                if (!File.Exists(path))
                    continue;

                var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters(ReadingMode.Deferred));

                if (assembly.Name.Name == reference.Name)
                    return assembly;

                assembly.Dispose();
            }
        }

        return null;
    }
}