#include "Structs.h"


std::string TypeTree::Dump(char* globalBuf) const {
	std::string result{};
	char debug[512];
	memset(debug, 0, 512);
	for (int i = 0; i < m_Nodes.size; i++) {
		auto& node = m_Nodes.data[i];
		char* type;
		char* name;
		if (node.m_TypeStrOffset < 0) {
			type = globalBuf + (0x7fffffff & node.m_TypeStrOffset);
		}
		else {
			type = m_StringData.data + node.m_TypeStrOffset;
		}
		if (node.m_NameStrOffset < 0) {
			name = globalBuf + (0x7fffffff & node.m_NameStrOffset);
		}
		else {
			name = m_StringData.data + node.m_NameStrOffset;
		}
		sprintf_s(debug, "%s %s // ByteSize{%x}, Index{%x}, IsArray{%d}, MetaFlag{%x}",
			type, name, node.m_ByteSize, node.m_Index, node.m_IsArray, node.m_MetaFlag);
		for (int j = 0; j < node.m_Level; j++) {
			result += "  ";
		}
		result += std::string(debug);
		result += "\n";
	}
	return result;
}
void TypeTree::Write(FILE* file) const
{
	fwrite(&m_Nodes.size, 4, 1, file);
	fwrite(&m_StringData.size, 4, 1, file);
	fwrite(m_Nodes.data, m_Nodes.size * sizeof(TypeTreeNode), 1, file);
	fwrite(m_StringData.data, m_StringData.size, 1, file);
}