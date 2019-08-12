using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pi_Custom_Assembler
{
    //This literally only compiles the file I need it to. More instructions supported coming.
    //My custom assembly format is - InstructionConditional (Params) (Bitflags)
    public class TheMostDisgustingCodeIHaveEverWritten
    {
        public enum Instruction
        {
            BroWhyCantYouJustBeNullable,
            ADD,
            SUB,
            MOVW,
            MOVT,
            STR,
            LDR,
            ORR,
            B,
            BL,
            BX
        }

        public enum Conditional
        {
            AndAgainWithTheNullThing,
            EQ = 0000, //equal
            NE = 0001, //not equal
            CS = 0010, //unsigned higher or same
            CC = 0011, //unsigned lower
            MI = 0100, //negative
            PL = 0101, //positive or zero
            VS = 0110, //overflow
            VC = 0111, //no overflow
            HI = 1000, //unsigned higher
            LS = 1001, //unsigned lower or same
            GE = 1010, //greater or equal
            LT = 1011, //less than
            GT = 1100, //greater than
            LE = 1101, //less than or equal
            AL = 1110  //always
        }


        public static Dictionary<Instruction, string> DPrs = new Dictionary<Instruction, string>()
        {
            { Instruction.ADD,"0100" },
            { Instruction.SUB,"0010" },
            { Instruction.ORR,"1100" }
        };

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Drag a binary file over this executible to compile it");
            }
            else
            {
                BruhMoment(args[0]);
            }
            Console.ReadKey();

        }

        private static void BruhMoment(string file)
        {
            string[] lines = File.ReadAllLines(file);
            StringBuilder words = new StringBuilder();
            lines.ToList().ForEach((s) => words.Append(LineToWord(s)));
            byte[] writethis = StringsToBytes(words.ToString());

            string outputName = "";
            string[] justUseALibrary = file.Split('.');
            for (int i = 0; i < justUseALibrary.Length - 1; i++)
            {
                //TODO fix this not working with folders with . in their names
                outputName += justUseALibrary[i];
            }
            outputName += ".img";

            File.WriteAllBytes(outputName, writethis);
            Console.WriteLine("File written, press any key to continue...");
        }

        private static string LineToWord(string s)
        {
            if (!char.IsLetter(s[0]))
            {
                Console.WriteLine("skipped comment " + s);
                return "";
            }
            List<string> tokens = new List<string>();
            foreach (Match m in Regex.Matches(s, "\\w+")) tokens.Add(m.Value);
            var (Instruc, Cond) = AppropriateInstruction(tokens[0]);
            StringBuilder ISuckAtThis = new StringBuilder();
            ISuckAtThis.Append(((int)Cond).ToString().PadLeft(4, '0'));

            #region PleaseDontLookAtThis
            string rn;
            string rd;
            string imm;
            string rot;
            string opr;
            string flags = tokens.Last().Flags();
            try
            {
                switch (Instruc)
                {
                    case Instruction.ORR:
                    case Instruction.SUB:
                    case Instruction.ADD: //COND 00 I OPCD S Rn Rd 12bitOperand  # (Immediate, no rot)ADD R2 R4 0x08 0 I | (Register, no rot, set flag)ADD R2 R4 R3 0 S
                        rd = GetRegister(tokens[1]);
                        rn = GetRegister(tokens[2]);
                        ISuckAtThis.Append("00");
                        ISuckAtThis.Flag(flags, "I");
                        ISuckAtThis.Append(DPrs[Instruc]);
                        ISuckAtThis.Flag(flags, "S");
                        ISuckAtThis.Append(rn);
                        ISuckAtThis.Append(rd);
                        if (flags.Contains("I"))
                        {
                            imm = GetImmediate(tokens[3], 8);
                            rot = GetImmediate(tokens[4], 4);
                            ISuckAtThis.Append(rot);
                            ISuckAtThis.Append(imm);
                        }
                        else
                        {
                            opr = GetRegister(tokens[3]);
                            rot = GetImmediate(tokens[4], 8);
                            ISuckAtThis.Append(rot);
                            ISuckAtThis.Append(opr);
                        }
                        break;
                    case Instruction.MOVW:
                        rd = GetRegister(tokens[1]);
                        imm = GetImmediate(tokens[2], 16);
                        ISuckAtThis.Append("00110000");
                        ISuckAtThis.Append(imm.Substring(0, 4) + rd + imm.Substring(4));
                        break;
                    case Instruction.MOVT:
                        ISuckAtThis.Append("00110100");
                        rd = GetRegister(tokens[1]);
                        imm = GetImmediate(tokens[2], 16);
                        ISuckAtThis.Append(imm.Substring(0, 4) + rd + imm.Substring(4));
                        break;
                    case Instruction.LDR: //COND 01 I P U B W L Rn Rd Offset # (load R2 into R3, no offset)LDR R3 R2 0
                        flags += "L";
                        goto case Instruction.STR;
                    case Instruction.STR:
                        rd = GetRegister(tokens[1]);
                        rn = GetRegister(tokens[2]);
                        ISuckAtThis.Append("01");
                        ISuckAtThis.Flag(flags, "I");
                        ISuckAtThis.Flag(flags, "P");
                        ISuckAtThis.Flag(flags, "U");
                        ISuckAtThis.Flag(flags, "B");
                        ISuckAtThis.Flag(flags, "W");
                        ISuckAtThis.Flag(flags, "L");
                        ISuckAtThis.Append(rn);
                        ISuckAtThis.Append(rd);
                        ISuckAtThis.Append(GetImmediate(tokens[3], 12));
                        break;
                    case Instruction.BL:
                        flags += "L";
                        goto case Instruction.B;
                    case Instruction.B:
                        ISuckAtThis.Append("101");
                        ISuckAtThis.Flag(flags, "L");
                        ISuckAtThis.Append(GetImmediate(tokens[1], 24));
                        break;
                    case Instruction.BX:
                        ISuckAtThis.Append("000100101111111111110001");
                        ISuckAtThis.Append(GetRegister(tokens[1]));
                        break;
                }
            }
            catch
            {
                throw new Exception("Somethings wrong with line: " + s);
            }
            #endregion PleaseDontLookAtThis

            if (ISuckAtThis.Length != 32) throw new Exception("Somethings wrong with line: " + s);
            //Console.WriteLine(ISuckAtThis.ToString());

            return SwapEndian(ISuckAtThis.ToString());
        }

        private static string SwapEndian(string v)
        {
            Console.WriteLine(v);
            StringBuilder s = new StringBuilder();
            for (int i = 3; i >= 0; i--)
            {
                s.Append(v.Substring(8 * i, 8));
            }
            return s.ToString();
        }

        private static byte[] StringsToBytes(string v)
        {
            byte[] w = new byte[v.Length / 8];
            for (int i = 0; i < v.Length / 8; i++)
            {
                w[i] = Convert.ToByte(v.Substring(8 * i, 8), 2);
            }
            return w;
        }

        private static string GetImmediate(string v, int bits)
        {
            return Convert.ToString(v.GetValue(), 2).PadLeft(bits, '0');
        }

        private static string GetRegister(string v) => Convert.ToString(int.Parse(v.Substring(1)), 2).PadLeft(4, '0');

        private static (Instruction Instruc, Conditional Cond) AppropriateInstruction(string v)
        {
            Instruction ret = Instruction.BroWhyCantYouJustBeNullable;
            Conditional con = Conditional.AndAgainWithTheNullThing;
            if (Enum.TryParse(v, out ret)) return (ret, Conditional.AL);

            string[] uh = new string[] { v.Substring(0, v.Length - 2), v.Substring(v.Length - 2) };
            if (!Enum.TryParse(uh[0], out ret) || !Enum.TryParse(uh[1], out con))
            {
                throw new ArgumentException("Instruction not supported: " + v);
            }
            return (ret, con);
        }
    }
    public static class Extenstions
    {
        public static int GetValue(this string s)
        {
            if (s.ToLower().StartsWith("0x"))
            {
                string hex = s.Substring(2);
                return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            }
            return int.Parse(s);
        }

        public static string Flags(this string input)
        {
            try
            {
                input.GetValue();
                return "";
            }
            catch
            {
                return input.ToUpper();
            }
        }

        public static void Flag(this StringBuilder sb, string flags, string look)
        {
            sb.Append(flags.ToUpper().Contains(look.ToUpper()) ? "1" : "0");
        }
    }
}
