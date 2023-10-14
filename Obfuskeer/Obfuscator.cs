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

        public void ObfuscateClassName(TypeDefinition c)
        {
            Console.Write($"\nObfuscated Class '{c.Name}' >> ");
            c.Name = "C" + Encryptor.Instance.GetHashString(c.Name);
            Console.WriteLine($"'{c.Name}'");
        }

        public void ObfuscateFieldName(FieldDefinition f)
        {
            string name = f.FullName.ToLower();

            if (_ignoredFieldWords.Any(x => name.Contains(x))) return;

            Console.Write($"Obfuscated Field '{f.Name}' >> ");
            f.Name = "F" + Encryptor.Instance.GetHashString(f.Name);
            Console.WriteLine($"'{f.Name}'");

        }

        public void ObfuscatePropertyName(PropertyDefinition p)
        {
            Console.Write($"Obfuscated Property '{p.Name}' >> ");
            p.Name = "P" + Encryptor.Instance.GetHashString(p.Name);
            Console.WriteLine($"'{p.Name}'");
        }

        public void ObfuscateMethodName(MethodDefinition m)
        {
            var name = m.FullName.ToLower();

            if (_ignoredMethodWords.Any(x => name.Contains(x)) || m.Name.ToLower().StartsWith("on"))
                return;

            Console.Write($"Obfuscated Method '{m.Name}' >> ");
            m.Name = "M" + Encryptor.Instance.GetHashString(m.Name);
            Console.WriteLine($"'{m.Name}'");

            if (m.HasParameters)
            {
                ObfuscateParameterNames(m);
            }
        }

        private void ObfuscateParameterNames(MethodDefinition m)
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
            if (ContainsBranchInstructions(m)) return; // Skip for now because this will break offset instructions and branch targets

            Console.WriteLine($"Obfuscating all Strings for the method {m.Name}");
            ILProcessor ilp = m.Body.GetILProcessor();

            for (int i = 0; i < m.Body.Instructions.Count;)
            {
                var instructionDef = m.Body.Instructions[i];

                if (instructionDef.OpCode != OpCodes.Ldstr ||
                    instructionDef.Operand.ToString().Contains("{") || instructionDef.Operand.ToString().Contains("}")) // Don't obfuscate interpolated strings
                {
                    i++;
                    continue;
                }

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
                i += modifiedInstructions.Count;  // Adjust index because of the new instructions
            }
        }

        private bool ContainsBranchInstructions(MethodDefinition method)
        {
            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Br || instruction.OpCode == OpCodes.Br_S)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
