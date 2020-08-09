#pragma once
#include <string>
template <class T = char>
class stl_allocator : public std::allocator<T>
{
public:
	template<class _Other>
	struct rebind
	{
		typedef stl_allocator<_Other> other;
	};

	stl_allocator()
	{
	}
	stl_allocator(const std::allocator<T>&)
	{
	}
private:
	int rootref;
};
typedef std::basic_string<char, std::char_traits<char>, stl_allocator<char>> TypeTreeString;
struct Object__RTTI
{
	Object__RTTI* base;
	void* factory;
	int classId;
	TypeTreeString className;
	int size;
	bool isAbstract;
	bool unk0;
	bool unk1;
};
struct TypeTreeNode
{
	int16_t m_Version;
	int8_t m_Depth;
	int8_t m_IsArray;
	int32_t m_Type; // offset; &(1<<31) => uses common buffer; otherwise local buffer
	int32_t m_Name; // same as Type
	int32_t m_ByteSize;
	int32_t m_Index;
	int32_t m_MetaFlag;
};
struct MemLabelId
{
	int id;
	// int *rootref; <-- only in debug builds
};

template <typename T>
struct dynamic_array
{
	T* data;
	MemLabelId label;
	size_t size;
	size_t cap; // < 0 => data is in union{T*, T[N]}
}; // 0x14

struct RTTIClass {
	RTTIClass* base;
	void* unk1; // probably constructor
	const char* name;
	const char* unk3;
	const char* unk4;
	int classID;
	int objectSize;
	int typeIndex;
	int unk5;
	bool isAbstract;
	bool unk6;
	bool unk7;
};
struct RuntimeTypeArray
{
	size_t count;
	RTTIClass* Types[1];
};
struct RuntimeTypeArray2
{
	size_t count;
	RTTIClass** Types;
};