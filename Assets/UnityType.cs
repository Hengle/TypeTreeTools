﻿using System;
using System.Runtime.InteropServices;

public unsafe struct UnityType
{
    public UnityType* BaseClass;
    public IntPtr ProduceHelper;
    public IntPtr Name;
    public IntPtr NativeNamespace;
#if UNITY_2017_1_OR_NEWER
    public IntPtr Module;
#endif
    public int PersistentTypeID;
    public int ByteSize;
    public uint RuntimeTypeIndex;
    public uint DescendantCount;
    public bool IsAbstract;
    public bool IsSealed;
#if UNITY_2017_1_OR_NEWER
    public bool IsEditorOnly;
#endif

    [PdbImport("?ms_runtimeTypes@RTTI@@0URuntimeTypeArray@1@A")]
    static readonly IntPtr runtimeTypes;

    [PdbImport("?TypeToScriptingType@Scripting@@YA?AVScriptingClassPtr@@PEBVType@Unity@@@Z")]
    static readonly TypeToScriptingTypeDelegate TypeToScriptingType;
    delegate IntPtr TypeToScriptingTypeDelegate(out IntPtr scripting, in UnityType type);

    [PdbImport("?GetTypeTree@TypeTreeCache@@YA_NVScriptingClassPtr@@W4TransferInstructionFlags@@AEAVTypeTree@@@Z")]
    static readonly GetTypeTreeDelegate GetTypeTree;
    delegate bool GetTypeTreeDelegate(IntPtr classPtr, TransferInstructionFlags flags, out TypeTree tree);

    public IntPtr GetScriptingClassPtr()
    {
        var ptr = TypeToScriptingType(out _, in this);

        if (ptr != IntPtr.Zero)
            return *(IntPtr*)ptr;

        return ptr;
    }

    public bool TryGetTypeTree(TransferInstructionFlags flags, out TypeTree tree)
    {
        if (GetTypeTree != null)
        {
            GetTypeTree(GetScriptingClassPtr(), flags, out tree);
            return true;
        }

        tree = default;
        return false;
    }

    public static ref readonly RuntimeTypeArray RuntimeTypes
    {
        get => ref *(RuntimeTypeArray*)runtimeTypes;
    }

    public static ref readonly UnityType GetByClassID(ClassID id)
    {
        foreach (ref var type in RuntimeTypes)
        {
            if (type.PersistentTypeID == (int)id)
                return ref type;
        }

        throw new ArgumentException(null, nameof(id));
    }

    public string GetName()
    {
        return Marshal.PtrToStringAnsi(Name);
    }

    public bool HasTypeTree
    {
        get
        {
            if (IsAbstract)
                return false;

            // Investigate
            switch ((ClassID)PersistentTypeID)
            {
            case ClassID.Vector3f:
            case ClassID.AudioMixerLiveUpdateBool:
            case ClassID.@bool:
            case ClassID.@void:
            case ClassID.RootMotionData:
            case ClassID.AudioMixerLiveUpdateFloat:
            case ClassID.MonoObject:
            case ClassID.Collision2D:
            case ClassID.Polygon2D:
            case ClassID.Collision:
            case ClassID.@float:
            case ClassID.@int:
                return false;
            }

            return true;
        }
    }
}
