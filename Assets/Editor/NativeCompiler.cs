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

        public static bool Is64BitProcess { get { return IntPtr.Size == 8; } }

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
                Debug.LogErrorFormat("Error running command {0} {1}",
                    exe, arguments);
            }
            if (!Directory.Exists("Logs")) Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/BuildResult.txt", strOutput);
        }
        [MenuItem("Tools/Build NativeTypeTreeTools.dll")]
        static void BuildNativeTypeTreeTools()
        {
            var msbuild = GetFullPath("msbuild.exe");
            msbuild = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe";
            if (!File.Exists(msbuild))
            {
                throw new Exception("Could not find executable msbuild.exe in path");
            }
            var outDir = Is64BitProcess ?
                Path.GetFullPath(@"CustomPlugins\x86_64") :
                Path.GetFullPath(@"CustomPlugins\x86");
            var platform = Is64BitProcess ? "x64" : "x86";
            var arguments = string.Format("NativeTypeTreeTools.sln /t:Build /p:OutDir={0} /p:Platform={1}",
                outDir, platform);
            Start(msbuild, arguments);
        }
    }
}
