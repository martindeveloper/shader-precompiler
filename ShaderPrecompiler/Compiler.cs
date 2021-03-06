﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShaderPrecompiler
{
    class Compiler
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static Regex IncludeRegex = new Regex("#include\\s*\"(.*)\"", RegexOptions.Compiled);
        private CompilerOptions _options;

        public Compiler(CompilerOptions options)
        {
            _options = options;
        }

        public bool CompileShaders()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_options.InputDirectory);

            return (RecursiveCompile(directoryInfo, ".hlsl") == 0);
        }


        private int RecursiveCompile(DirectoryInfo info, string pattern)
        {
            int failures = 0;

            if (_options.CleanBuild)
            {
                foreach (var child in info.GetFiles("*.cso"))
                {
                    string associatedPdb = child.FullName.Substring(0,child.FullName.Length - 4) + ".pdb";

                    if (_options.CanGeneratePDBs && File.Exists(associatedPdb))
                    {
                        File.Delete(associatedPdb);
                        Logger.Info("Cleaning '{0}'", associatedPdb);
                    }

                    Logger.Info("Cleaning '{0}'", child.FullName);

                    child.Delete();
                }
            }

            foreach (FileInfo child in info.GetFiles("*" + pattern))
            {
                //determine what type of shader we're compiling
                string compileTarget;

                if (child.Name.EndsWith(".vs" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "vs";
                }
                else if (child.Name.EndsWith(".ps" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "ps";
                }
                else if (child.Name.EndsWith(".gs" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "gs";
                }
                else if (child.Name.EndsWith(".ds" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "ds";
                }
                else if (child.Name.EndsWith(".hs" + pattern, StringComparison.OrdinalIgnoreCase))
                {
                    compileTarget = "hs";
                }
                else
                {
                    //not a known shader target, don't compile
                    Logger.Warn("{0} (1,1): Unsure of shader type - skipping", child.FullName);
                    continue;
                }
                 
                string flags;

                if (_options.Debug)
                {
                    flags = "/Od ";
                    if (_options.CanGeneratePDBs)
                    {
                        flags += "/Zi /Fd \"" + child.FullName.Substring(0,child.FullName.Length-5) + ".pdb\"";
                    }
                }
                else
                {
                    flags = string.Empty;
                }

                string compiledTarget = child.FullName.Substring(0,child.FullName.Length - 5) + ".cso";

                ProcessStartInfo start = new ProcessStartInfo(
                    _options.CompilerPath,
                    "/nologo /T " + compileTarget + "_"+ _options.ShaderModelVersion +" /E main /Fo \"" + compiledTarget + "\" " + 
                    flags + " \"" + child.FullName + "\"") 
                    { 
                        CreateNoWindow = true, 
                        UseShellExecute = false, 
                        RedirectStandardOutput = true 
                    };

                bool precompile = true;

                //precompile shaders unless explicitly stated
                using (FileStream fs = new FileStream(child.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new StreamReader(fs))
                    {
                        if (reader.EndOfStream ||
                            reader.ReadLine().StartsWith("#pragma message \"noprecompile\"", true, CultureInfo.InvariantCulture))
                        {
                            precompile = false;
                        }
                    }
                }

                //if this shader has no precompiled pair, or its precompiled data is older than the source
                //we should compile an updated version.
                FileInfo precompiled = new FileInfo(compiledTarget);

                //if we aren't supposed to precompile this, ensure we remove any pre existing precompiled version
                if (!precompile)
                {
                    if (precompiled.Exists)
                    {
                        Logger.Info("Deleting '{0}'", precompiled.FullName);

                        File.Delete(precompiled.FullName);
                    }
                }
                //otherwise if we are supposed to precompile, compile if there is no precompiled versino, or if its out
                //of date.
                else if (!precompiled.Exists || precompiled.LastWriteTimeUtc <= GetLastModified(child.FullName) || _options.ForceBuild)
                {
                    if (precompiled.Exists)
                    {
                        File.Delete(precompiled.FullName);
                    }

                    Logger.Info("Compiling '{0}'", child.FullName);

                    using (Process compilerProcess = Process.Start(start))
                    {
                        if (!compilerProcess.WaitForExit(30000))
                        {
                            Logger.Error("{0} (1,1): error - compiler timed out", child.FullName);

                            ++failures;
                        }
                        else
                        {
                            try
                            {
                                using (StreamReader reader = compilerProcess.StandardOutput)
                                {
                                    Logger.Info(reader.ReadToEnd().TrimEnd());
                                }
                            }
                            catch (Exception)
                            {
                            }

                            if (compilerProcess.ExitCode != 0)
                            {
                                ++failures;
                            }
                        }
                    }
                }
            }

            foreach (DirectoryInfo child in info.GetDirectories())
            {
                failures += RecursiveCompile(child, pattern);
            }

            return failures;
        }

        /**
         * find the last modified date of a shader file (recursivly checks last modified dates of any included files)
         */
        private DateTime GetLastModified(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            DateTime lastWriteModified = fileInfo.LastWriteTimeUtc;

            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    MatchCollection matches = IncludeRegex.Matches(reader.ReadToEnd());

                    foreach (Object match in matches)
                    {
                        DateTime lastModified = GetLastModified(Path.Combine(fileInfo.DirectoryName, ((Match)match).Groups[1].Value));

                        if (lastModified > lastWriteModified)
                        {
                            lastWriteModified = lastModified;
                        }
                    }
                }
            }

            return lastWriteModified;
        }
    }
}
