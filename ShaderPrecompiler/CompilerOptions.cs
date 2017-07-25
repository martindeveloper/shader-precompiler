using CommandLineParser.Arguments;

namespace ShaderPrecompiler
{
    class CompilerOptions
    {
        [SwitchArgument('f', "force-build", false, Optional = true)]
        public bool ForceBuild = false;

        [SwitchArgument('c', "clean-build", false, Optional = true)]
        public bool CleanBuild = false;

        [ValueArgument(typeof(string), 'p', "compiler", DefaultValue = "", Optional = true)]
        public string CompilerPath;

        [SwitchArgument('d', "debug", false, Optional = true)]
        public bool Debug = false;

        [SwitchArgument('s', "generate-pdb", true, Optional = true)]
        public bool CanGeneratePDBs;

        [ValueArgument(typeof(string), 'i', "input", DefaultValue = "", Optional = false)]
        public string InputDirectory;

        [ValueArgument(typeof(string), 'v', "shader-version", DefaultValue = "5_0", Optional = false)]
        public string ShaderModelVersion = "";

        [SwitchArgument('l', "interactive", false, Optional = true)]
        public bool IsInteractive = false;
    }
}
