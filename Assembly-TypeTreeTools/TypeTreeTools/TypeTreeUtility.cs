﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Core;

namespace TypeTreeTools
{
    public static unsafe class TypeTreeUtility
    {
        public static void CreateBinaryDump(TypeTree tree, BinaryWriter writer)
        {
            writer.Write(tree.Nodes.Size);
            writer.Write(tree.StringBuffer.Size);

            var iterator = new TypeTreeIterator(&tree);
            BinaryDumpNodes(iterator, writer);

            for (ulong i = 0, n = tree.StringBuffer.Size; i < n; i++)
                writer.Write(tree.StringBuffer.Ptr[i]);
        }

        static void BinaryDumpNodes(TypeTreeIterator it, BinaryWriter writer)
        {
            while (!it.IsNull)
            {
                var node = it.GetNode();
                writer.Write(node->Version);
                writer.Write(node->Level);
                writer.Write((byte)node->TypeFlags);
                writer.Write(node->TypeStrOffset);
                writer.Write(node->NameStrOffset);
                writer.Write(node->ByteSize);
                writer.Write(node->Index);
                writer.Write((int)node->MetaFlag);
                BinaryDumpNodes(it.GetChildren(), writer);
                it = it.GetNext();
            }
        }

        public static void CreateTextDump(TypeTree tree, TextWriter writer)
        {
#if UNITY_2019_1_0_OR_NEWER
            var iterator = new TypeTreeIterator(&tree);
            TextDumpNodes(iterator, writer);
#else
            TextDumpNodes(tree, writer);
#endif
        }
        #if UNITY_2019_1_0_OR_NEWER
        static void TextDumpNodes(TypeTreeIterator it, TextWriter writer)
        {
            while (!it.IsNull)
            {
                var node = it.GetNode();

                if (node->Index < 0)
                    goto Next;

                for (int j = 0; j < node->Level; j++)
                    writer.Write("  ");

                writer.WriteLine(string.Format("{0} {1} // ByteSize{{{2}}}, Index{{{3}}}, IsArray{{{4}}}, MetaFlag{{{5}}}",
                    it.Type,
                    it.Name,
                    node->ByteSize.ToString("x"),
                    node->Index.ToString("x"),
                    (byte)node->TypeFlags,
                    ((int)node->MetaFlag).ToString("x")
                ));

            Next:
                TextDumpNodes(it.GetChildren(), writer);
                it = it.GetNext();
            }
        }
#else
        static void TextDumpNodes(TypeTree tree, TextWriter writer)
        {
            for (int i = 0; i < (int)tree.Nodes.Size; i++)
            {
                var node = tree.Nodes.Ptr[i];
                for (int j = 0; j < node.Level; j++)
                    writer.Write("  ");
                string type = null;
                string name = null;
                if(node.TypeStrOffset < 0)
                {
                    var addr = new IntPtr((*CommonString.BufferBegin).ToInt64() + (0x7fffffff & node.TypeStrOffset));
                    type = Marshal.PtrToStringAnsi(addr);
                } else
                {
                    var addr = &tree.StringBuffer.Ptr[node.TypeStrOffset];
                    type = Marshal.PtrToStringAnsi(new IntPtr(addr));
                }
                if (node.NameStrOffset < 0)
                {
                    var addr = new IntPtr((*CommonString.BufferBegin).ToInt64() + (0x7fffffff & node.NameStrOffset));
                    type = Marshal.PtrToStringAnsi(addr);
                }
                else
                {
                    var addr = &tree.StringBuffer.Ptr[node.NameStrOffset];
                    type = Marshal.PtrToStringAnsi(new IntPtr(addr));
                }
                writer.WriteLine(string.Format("{0} {1} // ByteSize{{{2}}}, Index{{{3}}}, IsArray{{{4}}}, MetaFlag{{{5}}}",
                    type,
                    name,
                    node.ByteSize.ToString("x"),
                    node.Index.ToString("x"),
                    (byte)node.TypeFlags,
                    ((int)node.MetaFlag).ToString("x")
                ));
            }
        }
#endif
    }
}
