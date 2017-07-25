using CommandLineParser.Arguments;

namespace ShaderPrecompiler
{
    class CompilerOptions
    {
        [SwitchArgument('f', "force-build", false, Optional = true, Description = "Forces to compile all .hlsl shaders even if they were not changed")]
        public bool ForceBuild = false;

        [SwitchArgument('c', "clean-build", false, Optional = true, Description = "Sets if old .cso and .pdb files should be deleted before compilation")]
        public bool CleanBuild = false;

        [ValueArgument(typeof(string), 'p', "compiler", DefaultValue = "", Optional = true, Description = "Path to HLSL compiler executable")]
        public string CompilerPath;

        [SwitchArgument('d', "debug", false, Optional = true, Description = "Adds debug flag to compiler")]
        public bool Debug = false;

        [SwitchArgument('s', "generate-pdb", true, Optional = true, Description = "Allows PDB generation")]
        public bool CanGeneratePDBs;

        [ValueArgument(typeof(string), 'i', "input", DefaultValue = "", Optional = false, Description = "Input folder which contains .hlsl files")]
        public string InputDirectory;

        [EnumeratedValueArgument(typeof(string), 'v', "shader-version", DefaultValue = "5_0", Optional = true, Description = "Specify shader model version", AllowedValues = "5_1;5_0;4_1;4_0")]
        public string ShaderModelVersion = "";

        [SwitchArgument('l', "interactive", false, Optional = true, Description = "Sets if program will wait for user input")]
        public bool IsInteractive = false;
    }
}
