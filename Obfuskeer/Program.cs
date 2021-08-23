using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Obfuskeer
{
    class Program
    {
        public static string FileName;
        public static string FilePath;
        public static string FileDirectory;

        private static AssemblyDefinition _assemblyDef;
        private static Obfuscator _obfuscator;
        private static Stopwatch _timer;

        private static void InitConsole()
        {
            Console.Title = "OBFUSKEER";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"
                         ██████╗ ██████╗ ███████╗██╗   ██╗███████╗██╗  ██╗███████╗███████╗██████╗ 
                        ██╔═══██╗██╔══██╗██╔════╝██║   ██║██╔════╝██║ ██╔╝██╔════╝██╔════╝██╔══██╗
                        ██║   ██║██████╔╝█████╗  ██║   ██║███████╗█████╔╝ █████╗  █████╗  ██████╔╝
                        ██║   ██║██╔══██╗██╔══╝  ██║   ██║╚════██║██╔═██╗ ██╔══╝  ██╔══╝  ██╔══██╗
                        ╚██████╔╝██████╔╝██║     ╚██████╔╝███████║██║  ██╗███████╗███████╗██║  ██║
                         ╚═════╝ ╚═════╝ ╚═╝      ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝╚═╝  ╚═╝"); // Ansi shadow
            Console.WriteLine(@"
                         Created by Zeynel Koca A.K.A. Sisko
-----------------------------------------------------------------------------------------------------------------------
                         ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void PrintDoneMessage()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            string elapsed = "Total Time Taken: " + _timer.Elapsed.ToString(@"s\.ff") + "s";
            Console.WriteLine(@$"
 ___________________________________________________________________________________________________________________
|                                                                                                                   |
|                                                                                                                   |   
|                                            ____    ___   _   _  _____                                             |
|                                           |  _ \  / _ \ | \ | || ____|                                            |
|                                           | | | || | | ||  \| ||  _|                                              |
|                                           | |_| || |_| || |\  || |___                                             |
|                                           |____/  \___/ |_| \_||_____|                                            |
|                                                                                                                   |
|                                             {elapsed}                                               |
|___________________________________________________________________________________________________________________|");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void ObfuscateAllMethods(TypeDefinition t)
        {
            if (t.HasMethods)
            {
                Console.WriteLine($"\nObfuscating All Methods For {t.Name}..");
                foreach (var m in t.Methods)
                {
                    if (m.IsConstructor) continue;
                    if (m.IsRuntime) continue;
                    if (m.IsRuntimeSpecialName) continue;
                    if (m.IsVirtual) continue;
                    if (m.IsAbstract) continue;
                    if (m.GenericParameters != null && m.GenericParameters.Count > 0) continue;
                    if (m.Overrides.Count > 0) continue;
                    if (m.Name.StartsWith("<") || m.Name.ToLower().StartsWith("do")) continue;

                    _obfuscator.ObfuscateMethod(m);
                }
            }
        }

        private static void ObfuscateAllFields(TypeDefinition t)
        {
            if (t.HasFields)
            {
                Console.WriteLine($"\nObfuscating All Fields For {t.Name}..");

                foreach (var f in t.Fields)
                {
                    if (f.IsRuntimeSpecialName) continue;
                    if (f.IsSpecialName) continue;
                    if (f.Name.StartsWith("<")) continue;

                    _obfuscator.ObfuscateField(f);
                }
            }
        }

        private static void ObfuscateAllProperties(TypeDefinition t)
        {
            foreach (var p in t.Properties)
            {
                if (p.IsSpecialName) continue;
                if (p.IsRuntimeSpecialName) continue;

                _obfuscator.ObfuscateProperty(p);
            }
        }

        private static void ObfuscateAllStrings(MethodDefinition m)
        {
            Console.WriteLine($"Obfuscating all Strings for the method {m.Name}");
            ILProcessor ilp = m.Body.GetILProcessor();

            foreach (var instructionDef in m.Body.Instructions)
            {
                if (instructionDef.OpCode == OpCodes.Ldstr)
                {
                    Console.Write($"Obfuscated '{instructionDef.Operand}' >> ");
                    instructionDef.Operand =
                        Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(instructionDef.Operand.ToString() ?? string.Empty));
                    Console.WriteLine($"'{instructionDef.Operand}'");
                }
                else if (instructionDef.OpCode == OpCodes.Ldc_I4_0)
                {
                    instructionDef.OpCode = OpCodes.Nop;
                    ilp.InsertAfter(instructionDef, Instruction.Create(OpCodes.Ldc_I4_1));
                }
            }
        }

        private static void ObfuscateFile()
        {
            foreach (TypeDefinition t in _assemblyDef.MainModule.Types)
            {
                if (t.Name.StartsWith("<")) continue;

                ObfuscateAllFields(t);
                if (t.HasMethods)
                {
                    foreach (var m in t.Methods)
                    {
                        if (m.FullName.ToLower().Contains("initguistrings") && m.HasBody)
                        {
                            ObfuscateAllStrings(m);
                        }
                    }

                    ObfuscateAllMethods(t);
                }

                ObfuscateAllProperties(t);

                _obfuscator.ObfuscateClass(t);
            }
        }

        private static int Init()
        {
            _obfuscator = new Obfuscator();
            _timer = new Stopwatch();

            FilePath = Console.ReadLine();
            if (FilePath == null) return 0;

            FilePath = FilePath.Replace("\"", "");

            if (File.Exists(FilePath))
            {
                FileName = Path.GetFileName(FilePath);
            }
            else
            {
                Console.WriteLine("Inserted File path could not be found. Try dragging the file into the console.");
                return 0;
            }

            if (Path.GetExtension(FilePath) != ".dll")
            {
                Console.WriteLine("Inserted file isn't of a .dll or .exe file type.");
                return 0;
            }

            FileDirectory = FilePath.Replace(FileName ?? string.Empty, "");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(FileDirectory);

            _assemblyDef =
                AssemblyDefinition.ReadAssembly(FilePath, new ReaderParameters {AssemblyResolver = resolver});

            return 1;
        }

        static void Main()
        {
            InitConsole();
            Console.WriteLine("How to use: simply drag a file or paste a filepath in the console and press enter.");
            while (true)
            {
                Console.Write("\nObfuskeer> ");
                if (Init() == 1)
                    break;
            }

            _timer.Start();
            ObfuscateFile();
            _timer.Stop();

            PrintDoneMessage();

            string newFilePath = $"{FileDirectory}OBFUSKEER_{FileName}";
            _assemblyDef.Write(newFilePath);
            Console.WriteLine($"The obfuscated version of '{FileName}' can be found in the same directory and it's called 'OBFUSKEER_{FileName}'");
            Console.WriteLine("\nPress enter to close the program\n");
            Console.Write("Obfuskeer> ");
            Console.ReadLine();
        }
    }
}
