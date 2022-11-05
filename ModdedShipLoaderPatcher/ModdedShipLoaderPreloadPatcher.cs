using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class AssemblyResolver
{
    public static string[] ResolveDirectories()
    {
        // "D:\\Games\\Xbox\\Hardspace- Shipbreaker\\Content\\Shipbreaker_Data\\Managed\\"
        var dllLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

        return new string[] {
            Path.GetDirectoryName(
                Path.Combine(
                    dllLocation.Remove(dllLocation.LastIndexOf('\\')),
                    "../../../Shipbreaker_Data/Managed/"
                )
            ) + "\\"
        };
    }

    public static AssemblyDefinition ResolverOnResolveFailure(object sender, AssemblyNameReference reference)
    {
        foreach (var directory in ResolveDirectories())
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

public static class ModdedShipLoaderPreloadPatcher_AssemblyCSharp
{
    public static ManualLogSource logSource;

    public static void Initialize()
    {
        logSource = Logger.CreateLogSource("Modded Ship Loader Preloader - Assembly-CSharp");
    }

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    // Patches the assemblies
    public static void Patch(AssemblyDefinition assembly)
    {
        ModuleDefinition module = assembly.MainModule;

        TypeDefinition entityData = module.Types.First(t => t.FullName == "CutterScript");

        var moduleResolver = (BaseAssemblyResolver)module.AssemblyResolver;
        foreach (var dir in AssemblyResolver.ResolveDirectories())
        {
            moduleResolver.AddSearchDirectory(dir);
        }

        // Add our dependency resolver to the assembly resolver of the module we are patching
        moduleResolver.ResolveFailure += AssemblyResolver.ResolverOnResolveFailure;

        // Bring in Component type for componentsOnChildren
        var asm = AssemblyDefinition.ReadAssembly(AssemblyResolver.ResolveDirectories()[0] + "UnityEngine.CoreModule.dll");
        TypeReference componentTypeReference = module.ImportReference(asm.MainModule.Types.First(t => t.FullName == "UnityEngine.Component"));

        var newType = new TypeDefinition("BBI.Unity.Game", "AddressableLoader", TypeAttributes.Class | TypeAttributes.Public, entityData.BaseType);

        newType.Fields.Add(new FieldDefinition("refs", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        newType.Fields.Add(new FieldDefinition("assetGUID", FieldAttributes.Public, module.ImportReference(typeof(string))));
        newType.Fields.Add(new FieldDefinition("childPath", FieldAttributes.Public, module.ImportReference(typeof(string))));

        newType.Fields.Add(new FieldDefinition("disabledChildren", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        newType.Fields.Add(new FieldDefinition("componentsOnChildren", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(componentTypeReference)));
        newType.Fields.Add(new FieldDefinition("componentsOnChildrenPaths", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        assembly.MainModule.Types.Add(newType);

        var newSOType = new TypeDefinition("BBI.Unity.Game", "AddressableSOLoader", TypeAttributes.Class | TypeAttributes.Public, entityData.BaseType);

        newSOType.Fields.Add(new FieldDefinition("onChild", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("comp", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("field", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("refs", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        assembly.MainModule.Types.Add(newSOType);

        // New Component Loader
        /*
        var AddressableComponentValueType = new TypeDefinition("BBI.Unity.Game", "AddressableComponentValue", TypeAttributes.Class | TypeAttributes.Public, entityData.BaseType);
        AddressableComponentValueType.Fields.Add(new FieldDefinition("component", FieldAttributes.Public, componentTypeReference));
        //AddressableComponentValueType.Fields.Add(new FieldDefinition("field", FieldAttributes.Public, module.ImportReference(typeof(System.Reflection.FieldInfo))));
        AddressableComponentValueType.Fields.Add(new FieldDefinition("field", FieldAttributes.Public, module.TypeSystem.String));
        AddressableComponentValueType.Fields.Add(new FieldDefinition("address", FieldAttributes.Public, module.TypeSystem.String));
        MethodReference attributeConstructor = module.ImportReference(typeof(System.SerializableAttribute).GetConstructor(Type.EmptyTypes));
        AddressableComponentValueType.CustomAttributes.Add(new CustomAttribute(attributeConstructor));

        assembly.MainModule.Types.Add(AddressableComponentValueType);
        */

        var AddressableComponentLoaderType = new TypeDefinition("BBI.Unity.Game", "AddressableComponentLoader", TypeAttributes.Class | TypeAttributes.Public, entityData.BaseType);
        //AddressableComponentLoaderType.Fields.Add(new FieldDefinition("componentValues", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(AddressableComponentValueType)));

        AddressableComponentLoaderType.Fields.Add(new FieldDefinition("components", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(componentTypeReference)));
        AddressableComponentLoaderType.Fields.Add(new FieldDefinition("fields", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        AddressableComponentLoaderType.Fields.Add(new FieldDefinition("addresses", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        assembly.MainModule.Types.Add(AddressableComponentLoaderType);

        logSource.LogInfo("Preloader patching (AssemblyCSharp) is successful!");
    }
}


public static class ModdedShipLoaderPreloadPatcher_BBIUnityGame
{
    public static ManualLogSource logSource;

    public static void Initialize()
    {
        logSource = Logger.CreateLogSource("Modded Ship Loader Preloader - BBI.Unity.Game");
    }

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "BBI.Unity.Game.dll" };

    // Patches the assemblies
    public static void Patch(AssemblyDefinition assembly)
    {
        ModuleDefinition module = assembly.MainModule;

        var moduleResolver = (BaseAssemblyResolver)module.AssemblyResolver;
        foreach (var dir in AssemblyResolver.ResolveDirectories())
        {
            moduleResolver.AddSearchDirectory(dir);
        }
        moduleResolver.ResolveFailure += AssemblyResolver.ResolverOnResolveFailure;


        var typesToModify = System.IO.File.ReadAllLines(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "TypesToModify.txt"));

        foreach(var typeName in typesToModify)
        {
            assembly.MainModule.Types.Where(t => t.Name == typeName).First().Fields.Add(new FieldDefinition("AssetBasis", FieldAttributes.Public, module.ImportReference(typeof(string))));
            assembly.MainModule.Types.Where(t => t.Name == typeName).First().Fields.Add(new FieldDefinition("AssetCloneRef", FieldAttributes.Public, module.ImportReference(typeof(string))));
        }


        logSource.LogInfo("Preloader patching (BBIUnityGame) is successful!");
    }
}