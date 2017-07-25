using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShaderPrecompiler
{
    class CommandLineArguments
    {
        public class ArgumentsParsingFailedException : Exception { }

        private CommandLineParser.CommandLineParser Parser;
        private CompilerOptions Options;

        public CommandLineArguments()
        {
            Parser = new CommandLineParser.CommandLineParser();
            Options = new CompilerOptions();
        }

        public void ShowHelp()
        {
            Parser.ExtractArgumentAttributes(Options);
            Parser.ShowUsage();
        }

        public CompilerOptions Parse(string[] arguments)
        {
            Parser.ExtractArgumentAttributes(Options);
            Parser.ParseCommandLine(arguments);
            
            if(!Parser.ParsingSucceeded)
            {
                throw new ArgumentsParsingFailedException();
            }

            return Options;
        }
    }
}
