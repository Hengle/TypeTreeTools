using System;
using System.Runtime.InteropServices;

namespace Unity.Core
{
    public unsafe partial struct TypeTree
    {
#if UNITY_2019_1 || UNITY_2019_2
        public TypeTreeShareableData* Data;
        public TypeTreeShareableData m_PrivateData;
#elif UNITY_2019_3_OR_NEWER
        public TypeTreeShareableData* Data;
        public IntPtr ReferencedTypes;
        [MarshalAs(UnmanagedType.U1)]
        public bool PoolOwned;
#else
        public DynamicArray<TypeTreeNode> m_Nodes;
        public DynamicArray<byte> m_StringBuffer;
        public DynamicArray<uint> m_ByteOffsets;
#endif

        public void Init()
        {
            s_TypeTree(ref this, ref *kMemTypeTree);
        }

        public DynamicArray<TypeTreeNode> Nodes =>
#if UNITY_2019_1_OR_NEWER
                Data->Nodes;
#else
                m_Nodes;
#endif

        public DynamicArray<byte> StringBuffer =>
#if UNITY_2019_1_OR_NEWER
                Data->StringBuffer;
#else
                m_StringBuffer;
#endif

        public DynamicArray<uint> ByteOffsets =>
#if UNITY_2019_1_OR_NEWER
                Data->Nodes;
#else
                m_ByteOffsets;
#endif

#if UNITY_2019_1 || UNITY_2019_2
        [PdbSymbol("??0TypeTree@@QEAA@AEBUMemLabelId@@_N@Z")]
        static readonly TypeTreeDelegate s_TypeTree;
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr* TypeTreeDelegate(ref TypeTree typeTree, ref MemLabelId memLabel, bool allocatePrivateData = false);
#else
        [PdbSymbol("??0TypeTree@@QEAA@AEBUMemLabelId@@@Z")]
        static readonly TypeTreeDelegate s_TypeTree;
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr* TypeTreeDelegate(ref TypeTree typeTree, ref MemLabelId memLabel);
#endif
        [PdbSymbol("?kMemTypeTree@@3UMemLabelId@@A")]
        public static MemLabelId* kMemTypeTree;
    }
}
