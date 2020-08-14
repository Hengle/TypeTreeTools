using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Unity.Core;
using Newtonsoft.Json;

namespace TypeTreeTools
{
    unsafe static partial class ExportCommands
    {
        const string OutputDirectory = "Output";

        [MenuItem("Tools/Type Tree/Legacy/Export String Data")]
        static unsafe void ExportStringData()
        {
            var source = *(byte**)CommonString.BufferBegin;
            var length = *(byte**)CommonString.BufferEnd - source - 1;
            var buffer = new byte[length];

            fixed (byte* destination = buffer)
                Buffer.MemoryCopy(source, destination, length, length);

            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllBytes(Path.Combine(OutputDirectory, "strings.dat"), buffer);
        }

        [MenuItem("Tools/Type Tree/Legacy/Export Classes JSON")]
        static void ExportClassesJson()
        {
            var dictionary = new Dictionary<int, string>();

            for (int i = 0; i < RuntimeTypes.Count; i++)
            {
                var type = RuntimeTypes.Types[i];
                var name = Marshal.PtrToStringAnsi(type->ClassName);
                dictionary.Add((int)type->PersistentTypeID, name);
            }

            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(Path.Combine(OutputDirectory, "classes.json"), JsonConvert.SerializeObject(dictionary));
        }
        [MenuItem("Tools/Type Tree/Legacy/Export Struct Data")]
        static unsafe void ExportStructData()
        {
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var bw = new BinaryWriter(File.OpenWrite(Path.Combine(OutputDirectory, "structs.dat")));

            foreach (char c in Application.unityVersion)
                bw.Write((byte)c);
            bw.Write((byte)0);

            bw.Write((int)Application.platform);
            bw.Write((byte)1); // hasTypeTrees
            var countPosition = (int)bw.BaseStream.Position;
            var typeCount = 0;
            Debug.LogFormat("Writing RunTimeTypes");
            for (int i = 0; i < RuntimeTypes.Count; i++)
            {
                var type = RuntimeTypes.Types[i];
                var iter = type;
                Debug.LogFormat("Type {0} Child Class {1}, {2}, {3}, {4}, {5}",
                    i,
                    Marshal.PtrToStringAnsi(type->ClassNamespace),
                    Marshal.PtrToStringAnsi(type->ClassName),
                    Marshal.PtrToStringAnsi(type->Module),
                    type->PersistentTypeID,
                    type->Size
                    );
                Debug.LogFormat("Type {0} getting base type", i);
                while (iter->IsAbstract)
                {
                    if (iter->Base == null)
                        goto NextType;

                    iter = iter->Base;
                }
                Debug.LogFormat("Type {0} BaseType is {1}, {2}, {3}, {4}, {5}",
                    i,
                    Marshal.PtrToStringAnsi(type->ClassNamespace),
                    Marshal.PtrToStringAnsi(type->ClassName),
                    Marshal.PtrToStringAnsi(type->Module),
                    type->PersistentTypeID,
                    type->Size
                    );
                Debug.LogFormat("Type {0} Getting native object", i);
                var obj = NativeObject.GetOrProduce(*iter);

                if (obj == null)
                    continue;
                Debug.LogFormat("Type {0} Produced object {1}", i, obj->InstanceID);

                Debug.LogFormat("Type {0} Getting Type Tree", i);
                var tree = new TypeTree();
                tree.Init();
                if (obj->GetTypeTree(flags, ref tree))
                {
                    Debug.LogFormat("Type {0} Getting PersistentTypeID", i);
                    // Shouldn't this write type.PersistentTypeID instead?
                    // I'm leaving it as iter.PersistentTypeID for consistency
                    // with existing C++ code that generates structs.dat
                    bw.Write((int)iter->PersistentTypeID);

                    Debug.LogFormat("Type {0} Getting GUID", i);
                    // GUID
                    for (int j = 0, n = (int)iter->PersistentTypeID < 0 ? 0x20 : 0x10; j < n; ++j)
                        bw.Write((byte)0);
                    Debug.LogFormat("Type {0} Creating Binary Dump", i);
                    TypeTreeUtility.CreateBinaryDump(tree, bw);
                    typeCount++;
                }
                Debug.LogFormat("Type {0} Destroy if Not Singleton or Persistent", i);
                NativeObject.DestroyIfNotSingletonOrPersistent(obj);

            NextType:
                continue;
            }

            bw.Seek(countPosition, SeekOrigin.Begin);
            bw.Write(typeCount);
        }

        [MenuItem("Tools/Type Tree/Legacy/Export Struct Dump")]
        static unsafe void ExportStructDump()
        {
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "structs.dump"));

            for (int i = 0; i < RuntimeTypes.Count; i++)
            {
                var type = RuntimeTypes.Types[i];
                var iter = type;
                var inheritance = string.Empty;

                while (true)
                {
                    inheritance += Marshal.PtrToStringAnsi(iter->ClassName);

                    if (iter->Base == null)
                        break;

                    inheritance += " <- ";
                    iter = iter->Base;
                }

                tw.WriteLine("\n// classID{{{0}}}: {1}", (int)type->PersistentTypeID, inheritance);

                iter = type;
                while (iter->IsAbstract)
                {
                    tw.WriteLine("// {0} is abstract", Marshal.PtrToStringAnsi(iter->ClassName));

                    if (iter->Base == null)
                        break;

                    iter = iter->Base;
                }

                var obj = NativeObject.GetOrProduce(*iter);
                if(obj == null)
                {
                    Log.WriteLine("Type {0} {1}: Produced null object", i, Marshal.PtrToStringAnsi(type->ClassName));
                }
                else
                {
                    Log.WriteLine("Type {0} {1}: Generating type. PersistentId {2}, Persistent {3}", 
                        i,
                        Marshal.PtrToStringAnsi(type->ClassName), (int)type->PersistentTypeID, obj->IsPersistent);
                }
                if (obj == null)
                    continue;
                var tree = new TypeTree();
                tree.Init();
                if (obj->GetTypeTree(flags, ref tree))
                {
                    TypeTreeUtility.CreateTextDump(tree, tw);
                }
                else
                {
                    Log.WriteLine("Type {0} {1}: Error generating type tree",
                        i,
                        Marshal.PtrToStringAnsi(type->ClassName));
                }

                if (!obj->IsPersistent &&
                    type->PersistentTypeID != PersistentTypeID.SpriteAtlasDatabase &&
                    type->PersistentTypeID != PersistentTypeID.SceneVisibilityState &&
                    type->PersistentTypeID != PersistentTypeID.InspectorExpandedState &&
                    type->PersistentTypeID != PersistentTypeID.AnnotationManager &&
                    type->PersistentTypeID != PersistentTypeID.MonoManager)
                {
                    Log.WriteLine("Type {0} {1}: Destroying object. InstanceID {2}. CachedType {3}",
                        i,
                        Marshal.PtrToStringAnsi(type->ClassName),
                        obj->InstanceID,
                        obj->CachedTypeIndex);
                }
                NativeObject.DestroyIfNotSingletonOrPersistent(obj);
            }
        }
        static int GetSize(Type type)
        {
            if (type.IsEnum)
            {
                return Marshal.SizeOf(typeof(int));
            }
            else
            {
                return Marshal.SizeOf(type);
            }
        }
        [MenuItem("Tools/Type Tree/Legacy/Debug Struct")]
        static unsafe void DebugStruct()
        {
            Directory.CreateDirectory(OutputDirectory);
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "structs_debug.txt"));
            var structTypes = new Type[]
            {
                typeof(TypeTree),
                typeof(NativeObject),
                typeof(RuntimeTypeInfo),
                typeof(RuntimeTypes.RuntimeTypeArray),
                typeof(TypeTreeIterator),
                typeof(TypeTreeNode),
                typeof(TypeTreeShareableData),
                typeof(MemLabelId),
                typeof(AllocationRootWithSalt),
                typeof(DerivedFromInfo),
                typeof(DynamicArray<TypeTree>)
            };
            foreach(var type in structTypes)
            {
                tw.WriteLine("{0} Size {1} (0x{1:X})", type.Name, GetSize(type));
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    tw.WriteLine("    {0} {1}\toffset {2} (0x{2:X}) size {3} (0x{3:X})", field.FieldType.Name, field.Name, Marshal.OffsetOf(type, field.Name).ToInt32(), GetSize(field.FieldType));
                }
                tw.WriteLine();
            }
        }
        [MenuItem("Tools/Type Tree/Legacy/Dump Meta")]
        static unsafe void DebugMeta()
        {
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "dump_meta.txt"));

            for (int i = 0; i < RuntimeTypes.Count; i++)
            {
                var type = RuntimeTypes.Types[i];
                var iter = type;
                while (iter->IsAbstract)
                {
                    if (iter->Base == null)
                        break;
                    iter = iter->Base;
                }

                var obj = NativeObject.GetOrProduce(*type);

                if (obj == null) {
                    tw.WriteLine("{0} {1} NULL",
                        i,
                        iter->ClassName);
                    continue;
                }

                tw.WriteLine("{0} {1} InstanceId {2} CachedTypeIndex {3} IsPersistant {4}",
                    i,
                    Marshal.PtrToStringAnsi(iter->ClassName),
                    obj->InstanceID,
                    obj->CachedTypeIndex,
                    obj->IsPersistent);

                NativeObject.DestroyIfNotSingletonOrPersistent(obj);
            }
        }
    }
}
