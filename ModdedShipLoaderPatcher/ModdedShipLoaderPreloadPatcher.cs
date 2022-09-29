using System;
using System.Collections.Generic;
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

        newSOType.Fields.Add(new FieldDefinition("comp", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("field", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));
        newSOType.Fields.Add(new FieldDefinition("refs", FieldAttributes.Public, module.ImportReference(typeof(List<>)).MakeGenericInstanceType(module.TypeSystem.String)));

        assembly.MainModule.Types.Add(newSOType);


        logSource.LogInfo("Preloader patching is successful!");
    }
}