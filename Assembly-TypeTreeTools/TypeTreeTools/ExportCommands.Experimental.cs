using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Core;
using UnityEditor;

namespace TypeTreeTools
{
    class ExportCommands_Experimental
    {
        const string OutputDirectory = "Output";
        [MenuItem("Tools/Type Tree/Debug/Debug Struct")]
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
            foreach (var type in structTypes)
            {
                tw.WriteLine("{0} Size {1} (0x{1:X})", type.Name, GetSize(type));
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    tw.WriteLine("    {0} {1}\toffset {2} (0x{2:X}) size {3} (0x{3:X})", field.FieldType.Name, field.Name, Marshal.OffsetOf(type, field.Name).ToInt32(), GetSize(field.FieldType));
                }
                tw.WriteLine();
            }
        }
        [MenuItem("Tools/Type Tree/Debug/Dump Meta")]
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

                if (obj == null)
                {
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
        [MenuItem("Tools/Type Tree/Debug/Export Struct Dump Old")]
        static unsafe void ExportStructDump()
        {
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "structs_old.dump"));

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
                if (obj == null)
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
                    TypeTreeUtility.TextDumpNodes(tree, tw);
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
    }
}
