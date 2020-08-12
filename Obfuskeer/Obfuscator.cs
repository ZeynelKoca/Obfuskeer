using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Obfuscating_with_mono_cecil
{
    class Obfuscator
    {
        private List<string> fieldWords;
        private List<string> methodWords;

        public Obfuscator()
        {
            fieldWords = new List<string>() { "__", "incontrol", "enum", "optional" };
            methodWords = new List<string>() { "__", "incontrol", "start", "stop", "update", "awake", "option" };
        }

        public void ObfuscateField(FieldDefinition f)
        {
            string name = f.FullName.ToLower();

            string res = fieldWords.Where(x => name.Contains(x)).FirstOrDefault();
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
            string name = m.FullName.ToLower();

            string res = methodWords.Where(x => name.Contains(x)).FirstOrDefault();
            if (res != null || m.Name.ToLower().StartsWith("on"))
                return;

            Console.Write($"Obfuscated Method '{m.Name}' >> ");
            m.Name = "M" + Encryptor.Instance.GetHashString(m.Name);
            Console.WriteLine($"'{m.Name}'");
        }

        public void ObfuscateStrings(MethodDefinition m)
        {
            if (m.FullName.ToLower().Contains("gamemanager::ongui") && m.HasBody)
            {
                Console.WriteLine($"\nObfuscating All Strings For The Method: {m.FullName}");
                ILProcessor ilp = m.Body.GetILProcessor();

                for (int i = 0; i < m.Body.Instructions.Count; i++)
                {
                    Instruction InstructionDef = m.Body.Instructions[i];
                    if (InstructionDef.OpCode == OpCodes.Ldstr)
                    {
                        Console.Write($"Obfuscated '{InstructionDef.Operand}' >> ");
                        InstructionDef.Operand = Convert.ToBase64String(Encoding.UTF8.GetBytes(InstructionDef.Operand.ToString()));
                        ilp.InsertAfter(InstructionDef, Instruction.Create(OpCodes.Call));
                        Console.WriteLine($"'{InstructionDef.Operand}'");
                    }
                }
            }
        }
    }
}
