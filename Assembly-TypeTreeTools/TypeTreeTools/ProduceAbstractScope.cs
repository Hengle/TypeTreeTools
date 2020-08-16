using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dia2Lib;
using EasyHook;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;

namespace TypeTreeTools
{
    public unsafe delegate NativeObject* ObjectProducerDelegate(MemLabelId label, ObjectCreationMode mode);
    public unsafe delegate void ObjectConstructorDelegate(NativeObject* self, MemLabelId label, ObjectCreationMode mode);
    // I don't know the actual size of the vtable, but 60 pointers is big enough
    // to prevent a hard crash when allocating a custom one for use with internal
    // type tree APIs (at least on Unity 2020) so it's good enough.
    [StructLayout(LayoutKind.Explicit, Size = sizeof(System.Int64) * 60)]
    public unsafe struct VirtualMethodTable
    {
        [FieldOffset(sizeof(System.Int64) * 8)]
        public IntPtr getTypeVirtualInternal;
        public delegate RuntimeTypeInfo* GetTypeVirtualInternalDelegate(in NativeObject obj);
    }
    public unsafe struct ProduceAbstractScope : IDisposable
    {
        GCHandle constructorHandle;
        GCHandle produceHandle;
        GCHandle getTypeHandle;
        readonly IntPtr originalProduce;
        readonly LocalHook transferHook;

        public ProduceAbstractScope(RuntimeTypeInfo* type)
            : this()
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type->IsAbstract)
                throw new ArgumentException("Type must be abstract.", nameof(type));

            // In order to generate type trees for abstract types, we need to hook
            // VirtualRedirectTransfer to call the appropriate Transfer method on the type.
            for (var iter = type; iter != null; iter = iter->Base)
            {
                if (TryHookTransfer(Marshal.PtrToStringAnsi(iter->ClassName), out transferHook))
                    break;
            }

            if (transferHook == null)
                throw new ArgumentException("Couldn't locate a ::Transfer method for type.", nameof(type));

            // We also need to locate a constructor that we can call, as not all of the
            // abstract types actually have one defined.
            ObjectConstructorDelegate ctor = null;
            for (var iter = type; iter != null; iter = iter->Base)
            {
                if (TryFindConstructor(Marshal.PtrToStringAnsi(iter->ClassName), out ctor))
                    break;
            }

            if (ctor == null)
                throw new ArgumentException("Couldn't locate a constructor for type.", nameof(type));

            constructorHandle = GCHandle.Alloc(ctor, GCHandleType.Pinned);

            // We need to override the 'produce helper' so that attempting to produce
            // the type will no longer result in an error. Additionally, we override the
            // GetTypeVirtualInternal method to avoid errors on types without their own
            // constructors.
            var getType = CreateGetTypeDelegate(type->DerivedFromInfo.TypeIndex);
            getTypeHandle = GCHandle.Alloc(getType, GCHandleType.Pinned);
            var produce = CreateProduceHelper(
                type->Size,
                type->DerivedFromInfo.TypeIndex,
                Marshal.GetFunctionPointerForDelegate(getType),
                ctor
            );

            // Pin the produce method to avoid GC and assign it to the type.
            // Also set IsAbstract to false to allow the type to be produced.
            produceHandle = GCHandle.Alloc(produce, GCHandleType.Pinned);
            originalProduce = type->Factory;
            type->Factory = Marshal.GetFunctionPointerForDelegate(produce);
            type->IsAbstract = false;
        }

        bool TryHookTransfer(string typeName, out LocalHook hook)
        {
            string virtualTransferFunc = $"?VirtualRedirectTransfer@{typeName}@@UEAAXAEAVGenerateTypeTreeTransfer@@@Z";
            string transferFunc = $"??$Transfer@VGenerateTypeTreeTransfer@@@{typeName}@@IEAAXAEAVGenerateTypeTreeTransfer@@@Z";
            if (TryGetAddressForSymbol(virtualTransferFunc, out var original) &&
                TryGetAddressForSymbol(transferFunc, out var transfer))
            {
                Log.WriteLine("  TransferType: {0}", typeName);
                Log.WriteLine("  VirtualRedirectTransfer: {0}", virtualTransferFunc);
                Log.WriteLine("  $Transfer: {0}", transferFunc);
                LocalHook.Release();
                hook = LocalHook.CreateUnmanaged(original, transfer, IntPtr.Zero);
                hook.ThreadACL.SetInclusiveACL(new[] { 0 });
                return true;
            }

            hook = null;
            return false;
        }

        bool TryFindConstructor(string typeName, out ObjectConstructorDelegate ctor)
        {
            string constructorFunc = $"??0{typeName}@@QEAA@UMemLabelId@@W4ObjectCreationMode@@@Z";
            if (TryGetDelegateForSymbol(
                constructorFunc,
                out ctor
            ))
            {
                Log.WriteLine("  ConstructorType: {0}", typeName);
                Log.WriteLine("  Constructor: {0}", constructorFunc);
                return true;
            } else
            {
                return false;
            }
        }

        VirtualMethodTable.GetTypeVirtualInternalDelegate CreateGetTypeDelegate(uint typeIndex)
        {
            return (in NativeObject obj) => RuntimeTypes.Types[typeIndex];
        }

        ObjectProducerDelegate CreateProduceHelper(int objectSize, uint typeIndex, IntPtr getType, ObjectConstructorDelegate ctor)
        {
            return (label, mode) =>
            {
                var vtable = (VirtualMethodTable*)UnsafeUtility.Malloc(sizeof(VirtualMethodTable), 8, Allocator.Persistent);
                var alloc = (NativeObject*)UnsafeUtility.Malloc(objectSize, 8, Allocator.Persistent);

                // Call the C++ constructor
                ctor.Invoke(alloc, label, mode);

                // Copy the original virtual method table and override GetTypeVirtualInternal
                // to call a custom method that returns the correct runtime type.
                *vtable = *((VirtualMethodTable*)alloc->VirtualFunctionTable);
                vtable->getTypeVirtualInternal = getType;

                // Assign the new method table and type index to the object.
                alloc->VirtualFunctionTable = (IntPtr*)vtable;
                alloc->CachedTypeIndex = typeIndex;
                return alloc;
            };
        }

        public void Dispose()
        {
            // Dispose of the EasyHook handle.
            transferHook.Dispose();

            // Dispose of our GC handles, this will allow the delegates to be collected.
            produceHandle.Free();
            getTypeHandle.Free();
            constructorHandle.Free();
        }

        static bool TryGetDelegateForSymbol(string symbolName, Type delegateType, out Delegate method)
        {
            if (TryGetAddressForSymbol(symbolName, out IntPtr address))
            {
                method = Marshal.GetDelegateForFunctionPointer(address, delegateType);
                return true;
            }

            method = null;
            return false;
        }
        static bool TryGetDelegateForSymbol<T>(string symbolName, out T method)
                where T : Delegate
        {
            if (TryGetAddressForSymbol(symbolName, out IntPtr address))
            {
                method = Marshal.GetDelegateForFunctionPointer<T>(address);
                return true;
            }

            method = null;
            return false;
        }
        static bool TryGetAddressForSymbol(string symbolName, out IntPtr address)
        {
            if (TryGetRelativeVirtualAddress(symbolName, out uint rva))
            {
                address = new IntPtr(Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64() + rva);
                return true;
            }

            address = IntPtr.Zero;
            return false;
        }
        static bool TryGetRelativeVirtualAddress(string symbolName, out uint rva)
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