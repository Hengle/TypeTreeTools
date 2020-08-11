#pragma once
#include <string>
struct TypeTreeNode
{
	int16_t m_Version;
	int8_t m_Level;
	int8_t m_IsArray;
	int32_t m_TypeStrOffset; // offset; &(1<<31) => uses common buffer; otherwise local buffer
	int32_t m_NameStrOffset; // same as Type
	int32_t m_ByteSize;
	int32_t m_Index;
	int32_t m_MetaFlag;
};
struct AllocationRootWithSalt {
	uint32_t m_Salt;
	uint32_t m_RootReferenceIndex;
};
enum MemLabelIdentifier {
	// Not worth filling out
};
struct MemLabelId
{
	AllocationRootWithSalt m_RootReferenceWithSalt;
	MemLabelIdentifier identifier;
};

template <typename T>
struct dynamic_array
{
	T* data;
	MemLabelId label;
	size_t size;
	size_t cap; // < 0 => data is in union{T*, T[N]}
}; // 0x14

struct DerivedFromInfo
{
	uint32_t typeIndex;
	uint32_t descendantCount;
};

struct RTTIClass {
	RTTIClass* base;
	void* factory; // probably constructor
	const char* className;
	const char* classNamespace;
	const char* module;
	int32_t persistentTypeID;
	int32_t size;
	DerivedFromInfo derivedFromInfo;
	bool isAbstract;
	bool isSealed;
	bool isEditorOnly;
	void* attributes;
	uint64_t attributeCount;

};
struct RuntimeTypeArray
{
	size_t count;
	RTTIClass* Types[1];
};

struct TypeTree
{
	// +0
	dynamic_array<TypeTreeNode> m_Nodes;
	// +20
	dynamic_array<char> m_StringData;
	// +40
	dynamic_array<void*> m_ByteOffsets;
	// +60
	std::string Dump(char* globalBuf) const;
	void Write(FILE* file) const;
};
class ProxyTransfer;
class Object;
typedef int ObjectCreationMode;