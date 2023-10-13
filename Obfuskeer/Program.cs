using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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
            Console.WriteLine($@"
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

            for (int i = 0; i < m.Body.Instructions.Count; i++)
            {
                var instructionDef = m.Body.Instructions[i];

                if (instructionDef.OpCode != OpCodes.Ldstr) continue;
                if (instructionDef.Operand.ToString().Contains("{") || instructionDef.Operand.ToString().Contains("}")) continue; // Don't obfuscate interpolated strings

                Console.Write($"Obfuscated '{instructionDef.Operand}' >> ");
                string originalString = instructionDef.Operand.ToString() ?? string.Empty;
                string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalString));
                Console.WriteLine($"'{base64Encoded}'");

                // Get the methods we need to call
                var getUtf8Method = m.Module.ImportReference(
                    typeof(Encoding).GetProperty("UTF8").GetGetMethod());
                var fromBase64Method = m.Module.ImportReference(
                    typeof(Convert).GetMethod("FromBase64String", new[] { typeof(string) }));
                var getStringMethod = m.Module.ImportReference(
                    typeof(Encoding).GetMethod("GetString", new[] { typeof(byte[]) }));

                // Inject new instructions to call Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("base64Encoded"))
                var modifiedInstructions = new List<Instruction>
                {
                    Instruction.Create(OpCodes.Call, getUtf8Method),
                    Instruction.Create(OpCodes.Ldstr, base64Encoded),
                    Instruction.Create(OpCodes.Call, fromBase64Method),
                    Instruction.Create(OpCodes.Callvirt, getStringMethod)
                };

                // Replace the original Ldstr instruction with the new instructions
                foreach (var newInstr in modifiedInstructions)
                {
                    ilp.InsertBefore(instructionDef, newInstr);
                }
                ilp.Remove(instructionDef);
                i += modifiedInstructions.Count - 1;  // Adjust index because of the new instructions
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
                        if (m.HasBody)
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
                AssemblyDefinition.ReadAssembly(FilePath, new ReaderParameters { AssemblyResolver = resolver });

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
