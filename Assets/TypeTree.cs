﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public unsafe struct TypeTree
{
    readonly IntPtr dataPointer;

    ref readonly ShareableData Data => ref Unsafe.AsRef<ShareableData>(dataPointer.ToPointer()); 

    public ref readonly TypeTreeNodeArray Nodes => ref Data.Nodes;

    public ref readonly DynamicArray<byte> Strings => ref Data.Strings;

    public ref readonly DynamicArray<uint> ByteOffsets => ref Data.ByteOffsets;

    public TypeTreeIterator GetIterator()
    {
        return new TypeTreeIterator(dataPointer);
    }

    public struct ShareableData
    {
        public TypeTreeNodeArray Nodes;
        public DynamicArray<byte> Strings;
        public DynamicArray<uint> ByteOffsets;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Nodes.Length.ToInt32());
        writer.Write(Strings.Length.ToInt32());

        WriteNodes(GetIterator(), writer);

        for (int i = 0, n = Strings.Length.ToInt32(); i < n; i++)
            writer.Write(Strings.Data[i]);
    }

    static void WriteNodes(TypeTreeIterator it, BinaryWriter writer)
    {
        while (!it.IsNull)
        {
            it.Node.Write(writer);
            WriteNodes(it.GetChildren(), writer);
            it = it.GetNext();
        }
    }

    public void Dump(TextWriter writer)
    {
        DumpNodes(GetIterator(), writer);
    }

    static void DumpNodes(TypeTreeIterator it, TextWriter writer)
    {
        while (!it.IsNull)
        {
            var node = it.Node;

            for (int j = 0; j < node.Depth; j++)
                writer.Write("  ");

            writer.WriteLine(string.Format("{0} {1} // ByteSize{{{2}}}, Index{{{3}}}, IsArray{{{4}}}, MetaFlag{{{5}}}",
                it.Type,
                it.Name,
                node.ByteSize.ToString("x"),
                node.Index.ToString("x"),
                (byte)node.TypeFlags,
                ((int)node.MetaFlags).ToString("x")
            ));

            DumpNodes(it.GetChildren(), writer);
            it = it.GetNext();
        }
    }

    string GetNodeString(int offset)
    {
        var buffer = offset < 0 ? CommonString.BufferBegin : (IntPtr)Strings.Data;
        return Marshal.PtrToStringAnsi(IntPtr.Add(buffer, offset & 0x7fffffff));
    }
}