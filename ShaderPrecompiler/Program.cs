using System;
using System.Diagnostics;
using System.IO;

namespace ShaderPrecompiler
{
    /// <summary>
    /// The compiler automatically includes all .hlsl files included in the input directory. .fx effect files are not supported
    /// 
    /// Put the following at the start of a hlsl if you want it to be exlcuded from compilation
    /// #pragma message "noprecompile"
    /// 
    /// The compiler also uses the following naming convention to determine what type of shader to compile an hlsl file as
    /// 
    /// pixel shader: *_ps.hlsl
    /// vertex shader: *_vs.hlsl
    /// geometry shader: *_gs.hlsl
    /// hull shader: *_hs.hlsl
    /// domain shader: *_ds.hlsl
    /// 
    /// command line arguments
    /// -input:"path_to_shaders_directory" [optional] which directory contains the shaders to be compiled (the compiler searches recursively into this directory)
    ///                                     If not specified, the current directory is used.
    /// -force [optional] all shaders be rebuilt even if they seem up to date
    /// -clean [optional] all compiled .cso and .pdb objects in the input directory be removed before compiling
    /// -debug [optional] optimizations will be disabled in the compiled shaders (if omitted, optimizations will be enabled)
    /// -version [optional] specify the shader model version to use when compiling (defaults to 5_0 if not specified)
    /// -compiler:"path_to_fxc" [optional] specify the location on disk where the FXC compiler is located. If not specified, the following paths will be tried
    ///                                     The default install path for the windows 8 SDK.
    ///                                     The default install path for the June 2010 DirectX SDK
    ///                                     If its still not found, it is assumed that fxc can be found on the PATH
    /// </summary>
    class Program
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Logger.Info("ShaderCompiler {0}", version);

            CommandLineArguments argumentsParser = new CommandLineArguments();

            if(args.Length == 0)
            {
                argumentsParser.ShowHelp();

                return 0;
            }

            CompilerOptions options;

            try
            {
                options = argumentsParser.Parse(args);
            }
            catch (CommandLineArguments.ArgumentsParsingFailedException exception)
            {
                Logger.Error("Failed to parse command line arguments {0}", exception.Message);

                return 1;
            }
            
            LocateCompiler(options);

            if (string.IsNullOrEmpty(options.InputDirectory))
            {
                Logger.Warn("No input directory specified, assuming current directory");

                options.InputDirectory = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(options.InputDirectory))
            {
                Logger.Error("Input directory {0} doesn't exist", options.InputDirectory);

                return 1;
            }


            Compiler compiler = new Compiler(options);
            bool areShadersCompiled = compiler.CompileShaders();

            if (areShadersCompiled)
            {
                Logger.Info("All shaders compiled successfully!");

                return 0;
            }
            else
            {
                Logger.Error("Some shaders failed to compile!");

                return 1;
            }
        }

        private static void LocateCompiler(CompilerOptions options)
        {
            string windows10SDKPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Windows Kits\\10\\bin\\x64\\fxc.exe";
            string DXSDKPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Microsoft DirectX SDK (June 2010)\\Utilities\\bin\\x86\\fxc.exe";

            //we will give the windows 10 SDK compiler preference 
            if (!string.IsNullOrEmpty(options.CompilerPath))
            {
                if (!File.Exists(options.CompilerPath))
                {
                    Logger.Warn("Specified compiler not found at {0} attempting to locate from default paths", options.CompilerPath);
                }
            }

            if (string.IsNullOrEmpty(options.CompilerPath))
            {
                if (File.Exists(windows10SDKPath))
                {
                    Logger.Info("Using fxc from Windows 10 SDK");
                    options.CompilerPath = windows10SDKPath;
                }
                //then we'll try to use the compiler in the DX June 2012 SDK
                else if (File.Exists(DXSDKPath))
                {
                    Logger.Info("Using fxc from June 2010 DX SDK");
                    options.CompilerPath = DXSDKPath;
                }
                //couldn't find the compiler, so lets hope its on the path
                else
                {
                    Logger.Warn("Couldn't find fxc, assuming its on the PATH");
                    options.CompilerPath = "fxc.exe";
                }
            }

            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(options.CompilerPath);

                if (versionInfo.FileMajorPart >= 9 && versionInfo.FileMinorPart >= 30)
                {
                    options.CanGeneratePDBs = true;
                }
            }
            catch (Exception exception)
            {
                Logger.Warn("Couldn't determine if fxc supports pdb file generation. {0}", exception.Message);

                options.CanGeneratePDBs = false;
            }
        }
    }
}
