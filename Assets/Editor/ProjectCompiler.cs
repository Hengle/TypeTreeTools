using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Linq;

namespace TypeTreeTools
{
    static class ProjectCompiler
    {
        const string ProjectDirectory = "Assembly-TypeTreeTools";

        const string ProjectName = "TypeTreeTools";

        static readonly string[] VersionSplitChars =
        {
            ".", "a", "b", "rc", "f"
        };

        static string[] GetDependencies(Assembly assembly)
        {
            // Older versions of unity don't include Mono/MonoBleedingEdge libraries 
            // (mscorlib.dll, System.dll, System.Core.dll and System.Xml.dll)
            // in assembly.allReferences
#if UNITY_2017
            var result = assembly.allReferences.ToList();
            var currentAssembly = System.Reflection.Assembly.Load(assembly.name);
            foreach (var name in currentAssembly.GetReferencedAssemblies())
            {
                var reference = System.Reflection.Assembly.Load(name);
                result.Add(reference.Location);
            }
            return result.Distinct().ToArray();
#elif UNITY_2018_1
            // Unity 2018.1.0 has a bug where assemblyReferences contains null elements
            // allReferences is a product of compiledAssemblyReferences and assemblyReferences
            // so calling assemblyReferences throws a null reference exception
            return assembly.compiledAssemblyReferences;
#else
            return assembly.allReferences;
#endif
        }
        [MenuItem("Tools/Build TypeTreeTools.dll")]
        static void BuildTypeTreeTools()
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent             = true,
                IndentChars        = "  ",
            };

            using (var xw = XmlWriter.Create(Path.Combine(ProjectDirectory, "Generated.props"), settings))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("Project");
                xw.WriteStartElement("PropertyGroup");

                var version = Application.unityVersion;
                var split = Application.unityVersion.Split(VersionSplitChars, StringSplitOptions.RemoveEmptyEntries);
                xw.WriteStartElement("EditorVersion");
                xw.WriteString(version);
                xw.WriteEndElement();
                xw.WriteStartElement("EditorVersionMajor");
                xw.WriteString(split[0]);
                xw.WriteEndElement();
                xw.WriteStartElement("EditorVersionMinor");
                xw.WriteString(split[1]);
                xw.WriteEndElement();

                xw.WriteStartElement("EditorContentsDir");
                xw.WriteString(EditorApplication.applicationContentsPath);
                xw.WriteEndElement();

                foreach (var assembly in CompilationPipeline.GetAssemblies())
                {
                    if (assembly.name != "Assembly-CSharp-Editor")
                        continue;

                    var defines = string.Join(";", assembly.defines);
                    xw.WriteStartElement("DefineConstants");
                    xw.WriteString(defines);
                    xw.WriteEndElement();
                    xw.WriteEndElement();

                    xw.WriteStartElement("ItemGroup");
                    var assemblies = GetDependencies(assembly);
                    UnityEngine.Debug.Log(string.Format("Found {0} assemblies", assemblies.Length));
                    foreach (var reference in assemblies)
                    {
                        var name = Path.GetFileNameWithoutExtension(reference);
                        var path = Path.GetFullPath(reference);

                        if (name == ProjectName)
                            continue;

                        xw.WriteStartElement("Reference");
                        xw.WriteAttributeString("Private", bool.FalseString);
                        xw.WriteAttributeString("Include", name);
                        xw.WriteAttributeString("HintPath", path);
                        xw.WriteEndElement();
                    }
                }

                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName         = "dotnet",
                    Arguments        = "build",
                    WorkingDirectory = ProjectDirectory
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}
