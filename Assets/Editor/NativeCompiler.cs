using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TypeTreeTools
{
    static class NativeCompiler
    {
        const string ProjectDirectory = "NativeTypeTreeTools";

        const string ProjectName = "TypeTreeTools";

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
        static void Start(string exe, string arguments)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    WorkingDirectory = ProjectDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };

            process.Start();
            string strOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if(process.ExitCode != 0)
            {
                Debug.LogError($"Error running command {exe} {arguments}");
            }
            File.WriteAllText("Assets/BuildResult.txt", strOutput);
        }
        [MenuItem("Tools/Build NativeTypeTreeTools.dll")]
        static void BuildNativeTypeTreeTools()
        {
            var msbuild = GetFullPath("msbuild.exe");
            msbuild = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe";
            if (!File.Exists(msbuild))
            {
                throw new Exception($"Could not find executable msbuild.exe in path");
            }
            var outDir = Environment.Is64BitProcess ?
                Path.GetFullPath(@"CustomPlugins\x86_64") :
                Path.GetFullPath(@"CustomPlugins\x86");
            var platform = Environment.Is64BitProcess ? "x64" : "x86";
            Start(msbuild, $"NativeTypeTreeTools.sln /t:Build /p:OutDir={outDir} /p:Platform={platform}");
        }
    }
}
