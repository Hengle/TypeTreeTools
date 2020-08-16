using Dia2Lib;
using EasyHook;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using UnityEditor;

namespace TypeTreeTools
{
    unsafe class TestAbstract
    {
        [PdbSymbol("?Produce@Object@@CAPEAV1@PEBVType@Unity@@0HUMemLabelId@@W4ObjectCreationMode@@@Z")]
        static readonly ProduceDelegate s_Produce;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate NativeObject* ProduceDelegate(in RuntimeTypeInfo a, in RuntimeTypeInfo b, int instanceID, MemLabelId label, ObjectCreationMode creationMode);
        [PdbSymbol("?DestroySingleObject@@YAXPEAVObject@@@Z")]
        static readonly DestroySingleObjectDelegate s_DestroySingleObject;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DestroySingleObjectDelegate(ref NativeObject obj);
        const string OutputDirectory = "Output";
        // TODO: Support producing abstract types. To do this, the following steps are necessary:
        //       1. Replace T::VirtualRedirectTransfer with T::Transfer. This can be done by either
        //          hooking the method via EasyHook, or modifying the virtual function table.
        //          This works because both methods have compatible signatures.
        //       2. Create a new Factory method for the type, by locating its constructor function
        //          and using that to create a new delegate.
        //       3. Create a new RuntimeTypeInfo based on the original, with the new Factory method.
        //          It also needs to have the IsAbstract field set to false.
        //       4. Hook T::GetTypeVirtualInternal to return the appropriate RuntimeTypeInfo.
        //
        // ?VirtualRedirectTransfer@AudioListener@@UEAAXAEAVGenerateTypeTreeTransfer@@@Z
        // ~~?VirtualRedirectTransfer@AudioBehaviour@@UEAAXAEAVGenerateTypeTreeTransfer@@@Z~~
        // ??0AudioBehaviour@@QEAA@UMemLabelId@@W4ObjectCreationMode@@@Z
        // ?GetTypeVirtualInternal@AudioBehaviour@@EEBAQEBVType@Unity@@XZ
        [MenuItem("Tools/Type Tree/Debug/Dump Abstract")]
        static unsafe void DumbAbstractTest()
        {
            Log.Reset();
            Directory.CreateDirectory(OutputDirectory);
            var flags = TransferInstructionFlags.SerializeGameRelease;
            using var tw = new StreamWriter(Path.Combine(OutputDirectory, "dump_abstract.txt"));

            for (int i = 0; i < RuntimeTypes.Count; i++)
            {
                var type = RuntimeTypes.Types[i];
                //if (type->PersistentTypeID != PersistentTypeID.AudioBehaviour) continue;
                if (!type->IsAbstract) continue;
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

                Log.WriteLine("Type {0}: {1} {2} {3}",
                    i,
                    Marshal.PtrToStringAnsi(type->ClassNamespace),
                    Marshal.PtrToStringAnsi(type->ClassName),
                    Marshal.PtrToStringAnsi(type->Module));
                NativeObject* obj = null;
                using (var scope = new ProduceAbstractScope(type))
                {
                    obj = s_Produce(*type, *type, 0, new MemLabelId(), ObjectCreationMode.Default);
                    if (obj == null)
                    {
                        Log.WriteLine("  Type {0}: Produced null object", i);
                        tw.WriteLine("// Unable to produce {0}", Marshal.PtrToStringAnsi(type->ClassName));
                        continue;
                    }
                    else
                    {
                        Log.WriteLine("  Type {0}: Produced object {0}", i, obj->InstanceID);
                    }
                    var tree = new TypeTree();
                    tree.Init();
                    if (obj->GetTypeTree(flags, ref tree))
                    {
                        TypeTreeUtility.TextDumpNodes(tree, tw);
                    }
                    else
                    {
                        Log.WriteLine("  Type {0}: Error generating type tree", i);
                    }
                    if (!obj->IsPersistent &&
                        type->PersistentTypeID != PersistentTypeID.SpriteAtlasDatabase &&
                        type->PersistentTypeID != PersistentTypeID.SceneVisibilityState &&
                        type->PersistentTypeID != PersistentTypeID.InspectorExpandedState &&
                        type->PersistentTypeID != PersistentTypeID.AnnotationManager &&
                        type->PersistentTypeID != PersistentTypeID.MonoManager)
                    {
                        Log.WriteLine("  Type {0}: Destroying object. InstanceID {1}. CachedType {2}",
                            i,
                            obj->InstanceID,
                            obj->CachedTypeIndex);
                    }
                    //NativeObject.DestroyIfNotSingletonOrPersistent(obj);
                }
                var methodTable = obj->VirtualFunctionTable;
                s_DestroySingleObject(ref *obj);
                UnsafeUtility.Free(methodTable, Allocator.Persistent);
            }
        }
        public static bool TryHookTransfer(string typeName, out LocalHook hook)
        {
            if (TryGetAddressForSymbol($"?VirtualRedirectTransfer@{typeName}@@UEAAXAEAVGenerateTypeTreeTransfer@@@Z", out var original) &&
                TryGetAddressForSymbol($"??$Transfer@VGenerateTypeTreeTransfer@@@{typeName}@@IEAAXAEAVGenerateTypeTreeTransfer@@@Z", out var transfer))
            {
                LocalHook.Release();
                hook = LocalHook.CreateUnmanaged(original, transfer, IntPtr.Zero);
                hook.ThreadACL.SetInclusiveACL(new[] { 0 });
                return true;
            }

            hook = null;
            return false;
        }
        public static bool TryGetAddressForSymbol(string symbolName, out IntPtr address)
        {
            if (TryGetRelativeVirtualAddress(symbolName, out uint rva))
            {
                address = new IntPtr(Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64() + rva);
                return true;
            }

            address = IntPtr.Zero;
            return false;
        }
        public static bool TryGetRelativeVirtualAddress(string symbolName, out uint rva)
        {
            PdbSession.Current.globalScope.findChildren(
                SymTagEnum.SymTagPublicSymbol,
                symbolName,
                compareFlags: 0u,
                out IDiaEnumSymbols symbols
            );

            foreach (IDiaSymbol symbol in symbols)
            {
                rva = symbol.relativeVirtualAddress;
                return true;
            }

            rva = 0;
            return false;
        }
    }
}
