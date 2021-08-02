using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Obfuskeer
{
    class Obfuscator
    {
        private readonly List<string> _ignoredFieldWords;
        private readonly List<string> _ignoredMethodWords;

        public Obfuscator()
        {
            _ignoredFieldWords = new List<string>() { "__", "incontrol", "enum", "optional" };
            _ignoredMethodWords = new List<string>() { "__", "incontrol", "start", "stop", "update", "fixedupdate", "lateupdate", "awake", "option", "pausemanager", "fallingrock", "postfix", "prefix", "transpiler" };
        }

        public void ObfuscateClass(TypeDefinition c)
        {
            Console.Write($"\nObfuscated Class '{c.Name}' >> ");
            c.Name = "C" + Encryptor.Instance.GetHashString(c.Name);
            Console.WriteLine($"'{c.Name}'");
        }

        public void ObfuscateField(FieldDefinition f)
        {
            string name = f.FullName.ToLower();

            string res = _ignoredFieldWords.FirstOrDefault(x => name.Contains(x));
            if (res != null)
                return;

            Console.Write($"Obfuscated Field '{f.Name}' >> ");
            f.Name = "F" + Encryptor.Instance.GetHashString(f.Name);
            Console.WriteLine($"'{f.Name}'");

        }

        public void ObfuscateProperty(PropertyDefinition p)
        {
            Console.Write($"Obfuscated Property '{p.Name}' >> ");
            p.Name = "P" + Encryptor.Instance.GetHashString(p.Name);
            Console.WriteLine($"'{p.Name}'");
        }

        public void ObfuscateMethod(MethodDefinition m)
        {
            var name = m.FullName.ToLower();

            var res = _ignoredMethodWords.FirstOrDefault(x => name.Contains(x));
            if (res != null || m.Name.ToLower().StartsWith("on"))
                return;

            Console.Write($"Obfuscated Method '{m.Name}' >> ");
            m.Name = "M" + Encryptor.Instance.GetHashString(m.Name);
            Console.WriteLine($"'{m.Name}'");

            if (m.HasParameters)
            {
                ObfuscateParameters(m);
            }
        }

        private void ObfuscateParameters(MethodDefinition m)
        {
            foreach (var p in m.Parameters)
            {
                Console.Write($"Obfuscated parameter '{p.Name}' >> ");
                p.Name = Encryptor.Instance.GetHashString(p.Name);
                Console.WriteLine($"'{p.Name}'");
            }
        }

        public void ObfuscateStrings(MethodDefinition m)
        {
            if (m.FullName.ToLower().Contains("gamemanager::ongui") && m.HasBody)
            {
                Console.WriteLine($"\nObfuscating All Strings For The Method: {m.FullName}");
                ILProcessor ilp = m.Body.GetILProcessor();

                foreach (var instructionDef in m.Body.Instructions)
                {
                    if (instructionDef.OpCode != OpCodes.Ldstr) continue;

                    Console.Write($"Obfuscated '{instructionDef.Operand}' >> ");
                    instructionDef.Operand =
                        Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(instructionDef.Operand.ToString() ?? string.Empty));
                    ilp.InsertAfter(instructionDef, Instruction.Create(OpCodes.Call));
                    Console.WriteLine($"'{instructionDef.Operand}'");
                }
            }
        }
    }
}
