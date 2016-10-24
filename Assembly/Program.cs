using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Assembly
{
    class Program
    {
        static string sourceProgram="";
        static Dictionary<string, ushort> labelTable=new Dictionary<string, ushort>();
        static Dictionary<string, byte> registersCodes = new Dictionary<string, byte>() { { "ax", 0x01 }, { "bx", 0x02 }, { "cx", 0x03 }, { "dx", 0x04 }, { "ah", 0x05 }, { "al", 0x06 }, { "bh", 0x07 }, { "bl", 0x08 }, { "ch", 0x09 }, { "cl", 0x0a }, { "dh", 0x0b }, { "dl", 0x0c }, { "bp",0x0d}, {"sp",0x0e } };

        static int fileIndexPointer=0;
        static ushort asLength=0;
        static bool isEnd=false;
        static ushort executionAddress=0;
       // public enum registers{ax,bx,cx,dx,bp,sp,ip,si,di };
        static string sourceFileName;
        static string outputFileName;
        static string origin;

        static void Main(string[] args)
        {
            
            Console.Write("Write the exact path of your asm file: ");
            string input = Console.ReadLine();
            string[] elements= input.Split(' '); 
            for(int i=0; i<elements.Length; i++)
            {
                if(elements[i]=="-i")
                {
                    sourceFileName=elements[i + 1].Substring(1,elements[i+1].Length-2);
                }

                if(elements[i]=="-o")
                {
                    outputFileName = elements[i + 1].Substring(1, elements[i + 1].Length - 2);
                }

                if(elements[i]=="-origin")
                {

                    origin = elements[i + 1];
                }
                
            }
            if(input=="main")
            {
                origin = "1000";
                outputFileName = "code.VM";
                sourceFileName = "test.asm";
            }
            assemble();
           // Console.WriteLine(sourceFileName + " " + outputFileName + " " + origin);
            Console.ReadLine();
        }

        static void assemble()
        {
            asLength = Convert.ToUInt16(origin, 16);
            BinaryWriter output;
            TextReader input;
            FileStream file = new FileStream(outputFileName, FileMode.Create);
            output = new BinaryWriter(file);
            input = File.OpenText(sourceFileName);
            sourceProgram = input.ReadToEnd();
            string sourceTemp = "";
            for (int i = 0; i < sourceProgram.Length; i++)
            {
                if (sourceProgram[i] == '%' && sourceProgram[i + 1] == 'i' && sourceProgram[i + 2] == 'n' && sourceProgram[i + 3] == 'c' && sourceProgram[i + 4] == 'l' && sourceProgram[i + 5] == 'u' && sourceProgram[i + 6] == 'd' && sourceProgram[i + 7] == 'e')
                {
                    string includeName = "";
                    i += 10;
                    while (sourceProgram[i] != '"')
                    {
                        includeName += sourceProgram[i];
                        i++;
                    }
                    TextReader includeInput = File.OpenText(includeName);
                    sourceTemp += includeInput.ReadToEnd();
                    sourceTemp += "\n";
                    if(i+1<=sourceProgram.Length-1)
                        i++;
                }
                sourceTemp += sourceProgram[i];
            }
            sourceProgram = sourceTemp;
            /*foreach(char i in sourceProgram)
            {
                Console.Write(i);
            }*/
            
            input.Close();
            output.Write('5');
            output.Write('5');
            output.Write('a');
            output.Write('a');
            output.Write(Convert.ToUInt16(origin, 16));
            output.Write((ushort)0);
            output.Write((ushort)0);
            parse(output);
            
            output.Seek(6, SeekOrigin.Begin);
            output.Write(executionAddress);
            output.Write(asLength);
            output.Close();
            file.Close();
            /*foreach (KeyValuePair<string, ushort> pairs in labelTable)
                Console.WriteLine(pairs.Key+" "+pairs.Value);*/
            Console.WriteLine("Done!");
        }

        static void parse(BinaryWriter output)
        {
            fileIndexPointer = 0;
            while (!isEnd)
                LabelScan(output,true);
            isEnd = false;
            fileIndexPointer = 0;
            asLength = Convert.ToUInt16(origin, 16);
            while (!isEnd)
                LabelScan(output, false);

        }

        static void LabelScan(BinaryWriter output,bool isLabelScan)
        {
            if (fileIndexPointer > sourceProgram.Length - 1) { isEnd = true; return; }
            char b = sourceProgram[fileIndexPointer];
            if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                if (isLabelScan) labelTable.Add(GetLabelName(), asLength);
                while (sourceProgram[fileIndexPointer] != '\n')
                {
                    fileIndexPointer++;
                    if (fileIndexPointer > sourceProgram.Length - 1) { isEnd = true; break; }
                }
                fileIndexPointer++;
                return;

            }
            else if (char.IsWhiteSpace(sourceProgram[fileIndexPointer]) && fileIndexPointer + 1 < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer + 1]))
            {
                eatWhiteSpaces();
                ReadMnemonic(output, isLabelScan);
            }
            else
            {
                while (sourceProgram[fileIndexPointer] != '\n')
                {
                    fileIndexPointer++;
                    if (fileIndexPointer > sourceProgram.Length - 1) { isEnd = true; break; }
                }
                fileIndexPointer++;
            }
        }

        static void ReadMnemonic(BinaryWriter output,bool isLabelScan)
        {
            string mnemonic = "";
            if (fileIndexPointer >= sourceProgram.Length - 1)
            {
                isEnd = true;
                return;
            }
            while (!char.IsWhiteSpace(sourceProgram[fileIndexPointer]))
            {
                mnemonic += sourceProgram[fileIndexPointer];
                if (mnemonic == "end") break;
                fileIndexPointer++;

            }
            if (mnemonic == "mov") interpretMov(output,isLabelScan);
            if (mnemonic == "end")
                {
                //isEnd = true;
                doEnd(output, isLabelScan);
               // eatWhiteSpaces();
                //return;
                    };
            if (mnemonic == "jmp") interpretJmp(output, isLabelScan);
            if (mnemonic == "int") interpretInt(output, isLabelScan);
            if (mnemonic == "db") interpretDb(output, isLabelScan);
            if (mnemonic == "times") interpretTimes(output, isLabelScan);
            if (mnemonic == "cmp") interpretCmp(output, isLabelScan);
            if (mnemonic == "je") interpretJe(output, isLabelScan);
            if (mnemonic == "jne") interpretJne(output, isLabelScan);
            if (mnemonic == "jg") interpretJg(output, isLabelScan);
            if (mnemonic == "jl") interpretJl(output, isLabelScan);
            if (mnemonic == "jge") interpretJge(output, isLabelScan);
            if (mnemonic == "jle") interpretJle(output, isLabelScan);
            if (mnemonic == "inc") interpretInc(output, isLabelScan);
            if (mnemonic == "push") interpretPush(output, isLabelScan);
            if (mnemonic == "pop") interpretPop(output, isLabelScan);
            if (mnemonic == "call") interpretCall(output, isLabelScan);
            if (mnemonic == "ret") interpretRet(output, isLabelScan);
            if (mnemonic == "dec") interpretDec(output, isLabelScan);
            if (mnemonic == "add") interpretAdd(output, isLabelScan);
            if (mnemonic == "sub") interpretSub(output, isLabelScan);
            if (mnemonic == "mul") interpretMul(output, isLabelScan);
            if (mnemonic == "div") interpretDiv(output, isLabelScan);
            if (mnemonic == "shr") interpretShr(output, isLabelScan);
            if (mnemonic == "shl") interpretShl(output, isLabelScan);
            if (mnemonic == "or") interpretOr(output, isLabelScan);
            if (mnemonic == "and") interpretAnd(output, isLabelScan);
            if (mnemonic == "not") interpretNot(output, isLabelScan);
            if (mnemonic == "xor") interpretXor(output, isLabelScan);
            if (mnemonic == "pusha") interpretPusha(output, isLabelScan);
            if (mnemonic == "popa") interpretPopa(output, isLabelScan);

            if (fileIndexPointer >= sourceProgram.Length - 1)
            {
                isEnd = true;
                return;
            }

            while (sourceProgram[fileIndexPointer] != '\n')
                fileIndexPointer++;
            fileIndexPointer++;
            

        }

        static void interpretMov(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            if(char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                string operand = ReadOperand();
                int bits = 16;
                if (operand[1] == 'h' || operand[1] == 'l')
                    bits = 8;
                byte reg;
                registersCodes.TryGetValue(operand, out reg);
                fileIndexPointer++;
                
                if (bits == 16)
                {
                    ushort val = 0;
                    if (char.IsDigit(sourceProgram[fileIndexPointer]))
                        val = ReadWord();
                    else if(char.IsLetter(sourceProgram[fileIndexPointer]))
                    {
                        string sval = "";
                        while (fileIndexPointer<=sourceProgram.Length-1 && char.IsLetter(sourceProgram[fileIndexPointer]))
                        {
                            sval += sourceProgram[fileIndexPointer];
                            fileIndexPointer++;
                        }
                        byte register = 0;
                        registersCodes.TryGetValue(sval, out register);
                        if(register!=0)
                        {
                            interpretMovReg(output, isLabelScan,reg,register);
                            return;
                        }
                        labelTable.TryGetValue(sval, out val);
                        //Console.WriteLine(sval);
                    }
                    else if (sourceProgram[fileIndexPointer] == '[')
                    {
                        interpretMovPointer(output, isLabelScan, reg);
                        return;
                    }
                    asLength += 4;
                    if (!isLabelScan)
                    {
                        output.Write((byte)0x01);
                        output.Write(reg);
                        output.Write(val);
                       
                       // Console.WriteLine(val >>8);
                       // Console.WriteLine(val & Convert.ToUInt16("00ff", 16));

                    }
                    
                   
                }
                else if(bits==8)
                {
                    byte val = 0;
                    if (char.IsDigit(sourceProgram[fileIndexPointer]))
                        val = ReadByte();
                    else if (sourceProgram[fileIndexPointer] == '[')
                    {
                        interpretMovPointer(output, isLabelScan,reg);
                        return;
                    }
                    else if(char.IsLetter(sourceProgram[fileIndexPointer]))
                    {
                        string sval = "";
                        while (fileIndexPointer <= sourceProgram.Length - 1 && char.IsLetter(sourceProgram[fileIndexPointer]))
                        {
                            sval += sourceProgram[fileIndexPointer];
                            fileIndexPointer++;
                        }
                        byte register = 0;
                        registersCodes.TryGetValue(sval, out register);
                        if (register != 0)
                        {
                            interpretMovReg(output, isLabelScan, reg, register);
                            return;
                        }
                    }
                    asLength += 3;
                    if (!isLabelScan)
                    {
                        output.Write((byte)0x01);
                        output.Write(reg);
                        output.Write(val);

                    }
                }
                
            }
        }

        static void interpretJmp(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string label="";
            while (char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                label += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
                if (fileIndexPointer >= sourceProgram.Length) break;
            }
            ushort address;
            labelTable.TryGetValue(label, out address);
            if (sourceProgram[fileIndexPointer] == '$') address = asLength;
            asLength += 3;
            if(!isLabelScan)
            {
                output.Write((byte)0x03);
                output.Write(address);
            }
      
        }

        static void interpretInt(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while(char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
                if (fileIndexPointer >= sourceProgram.Length) break;
            }
            byte val=Convert.ToByte(sval,16);
            asLength += 2;
            if(!isLabelScan)
            {
                output.Write((byte)0x04);
                output.Write(val);
            }
        }

        static void interpretMovPointer(BinaryWriter output,bool isLabelScan,byte register)
        {
            fileIndexPointer++;
            string reg = "";
            byte plus = 0;
            while(sourceProgram[fileIndexPointer]!=']')
            {
                if (sourceProgram[fileIndexPointer] == '+')
                {
                    string sval = "";
                    while (sourceProgram[fileIndexPointer] != ']')
                    {
                        sval += sourceProgram[fileIndexPointer];
                        fileIndexPointer++;
                    }
                    plus = Convert.ToByte(sval);
                    fileIndexPointer++;
                    break;
                }
                
                reg += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            fileIndexPointer++;
            byte val;
            registersCodes.TryGetValue(reg, out val);
            asLength += 4;
            if(!isLabelScan)
            {
                output.Write((byte)0x05);
                output.Write(register);
                output.Write(val);
                output.Write(plus);
            }
        }

        static void interpretCmp(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            byte reg1 = 0; byte reg2 = 0;
            string sval = "";
            while(char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            registersCodes.TryGetValue(sval, out reg1);
            fileIndexPointer++;
            sval = "";
            while(fileIndexPointer<=sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if(!isLabelScan)
            {
                output.Write((byte)0x06);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretJe(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while(fileIndexPointer<=sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            ushort labelAddr;
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if(!isLabelScan)
            {
                output.Write((byte)0x07);
                output.Write(labelAddr);
            }
        }

        static void interpretJne(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer <= sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            ushort labelAddr;
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x08);
                output.Write(labelAddr);
            }
        }

        static void interpretJg(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer <= sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            ushort labelAddr;
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x09);
                output.Write(labelAddr);
            }
        }

        static void interpretJl(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer <= sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            ushort labelAddr;
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x0a);
                output.Write(labelAddr);
            }
        }

        static void interpretJge(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer <= sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            ushort labelAddr;
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x0b);
                output.Write(labelAddr);
            }
        }

        static void interpretJle(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer <= sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            ushort labelAddr;
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x0c);
                output.Write(labelAddr);
            }
        }

        static void interpretMovReg(BinaryWriter output,bool isLabelScan,byte reg1,byte reg2)
        {
            asLength += 3;
            if(!isLabelScan)
            {
                output.Write((byte)0x0d);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretInc(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval="";
            byte reg;
            while(fileIndexPointer<=sourceProgram.Length-1 && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            registersCodes.TryGetValue(sval, out reg);
            asLength += 2;
            if(!isLabelScan)
            {
                output.Write((byte)0x0e);
                output.Write(reg);
            }
            
        }

        static void interpretPush(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval="";
            while(fileIndexPointer<=sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg=0;
            registersCodes.TryGetValue(sval, out reg);
            asLength += 2;
            if(!isLabelScan)
            {
                output.Write((byte)0x0f);
                output.Write(reg);
            }
        }

        static void interpretPop(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer <= sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            asLength += 2;
            if (!isLabelScan)
            {
                output.Write((byte)0x10);
                output.Write(reg);
            }
        }

        static void interpretCall(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            ushort labelAddr = 0;
            while(fileIndexPointer<=sourceProgram.Length-1 && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;

            }
            labelTable.TryGetValue(sval, out labelAddr);
            asLength += 3;
            if(!isLabelScan)
            {
                output.Write((byte)0x11);
                output.Write(labelAddr);
            }
        }

        static void interpretRet(BinaryWriter output,bool isLabelScan)
        {
            asLength++;
            if(!isLabelScan)
            {
                output.Write((byte)0x12);
            }
        }

        static void interpretDec(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval="";
            while(fileIndexPointer<=sourceProgram.Length-1 && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg=0;
            registersCodes.TryGetValue(sval,out reg);
            asLength += 2;
            if(!isLabelScan)
            {
                output.Write((byte)0x13);
                output.Write(reg);
            }
        }

        static void interpretAdd(BinaryWriter output,bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while(sourceProgram[fileIndexPointer]!=',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1]=='l')
            {
                bits = 8;
            }
            byte reg=0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex=false;
            if(sourceProgram[fileIndexPointer+1]=='x')
            {
                isHex = true;
            }
            if(char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while(fileIndexPointer<sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if(char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretAddReg(output, isLabelScan, reg);
                return;
            }
            
            if(bits==16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if(!isLabelScan)
                {
                    output.Write((byte)0x14);
                    output.Write(reg);
                    output.Write(val);
                }                     

            }
            else if(bits==8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if(!isLabelScan)
                {
                    output.Write((byte)0x14);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretAddReg(BinaryWriter output,bool isLabelScan,byte reg1)
        {
            string sval = "";
            while(fileIndexPointer<sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2=0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if(!isLabelScan)
            {
                output.Write((byte)0x15);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretSub(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretSubReg(output, isLabelScan, reg);
                return;
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x16);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x16);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretSubReg(BinaryWriter output, bool isLabelScan, byte reg1)
        {
            string sval = "";
            while (fileIndexPointer < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2 = 0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x17);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretMul(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretMulReg(output, isLabelScan, reg);
                return;
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x18);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x18);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretMulReg(BinaryWriter output, bool isLabelScan, byte reg1)
        {
            string sval = "";
            while (fileIndexPointer < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2 = 0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x19);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretDiv(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretDivReg(output, isLabelScan, reg);
                return;
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1a);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1a);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretDivReg(BinaryWriter output, bool isLabelScan, byte reg1)
        {
            string sval = "";
            while (fileIndexPointer < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2 = 0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x1b);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretShr(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1c);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1c);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretShl(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1d);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1d);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretOr(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretOrReg(output, isLabelScan, reg);
                return;
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1e);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x1e);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretOrReg(BinaryWriter output, bool isLabelScan, byte reg1)
        {
            string sval = "";
            while (fileIndexPointer < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2 = 0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x1f);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretAnd(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretAndReg(output, isLabelScan, reg);
                return;
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x20);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x20);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretAndReg(BinaryWriter output, bool isLabelScan, byte reg1)
        {
            string sval = "";
            while (fileIndexPointer < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2 = 0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x21);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretNot(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (fileIndexPointer<sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            asLength += 2;
            if(!isLabelScan)
            {
                output.Write((byte)0x22);
                output.Write(reg);
            }
            
        }

        static void interpretXor(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            while (sourceProgram[fileIndexPointer] != ',')
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            int bits = 16;
            if (sval[1] == 'h' || sval[1] == 'l')
            {
                bits = 8;
            }
            byte reg = 0;
            registersCodes.TryGetValue(sval, out reg);
            fileIndexPointer++;
            sval = "";
            bool isHex = false;
            if (sourceProgram[fileIndexPointer + 1] == 'x')
            {
                isHex = true;
            }
            if (char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                while (fileIndexPointer < sourceProgram.Length && char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                }
            }
            else if (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                interpretXorReg(output, isLabelScan, reg);
                return;
            }

            if (bits == 16)
            {

                ushort val;
                if (isHex) val = Convert.ToUInt16(sval, 16);
                else val = ushort.Parse(sval);
                asLength += 4;
                if (!isLabelScan)
                {
                    output.Write((byte)0x24);
                    output.Write(reg);
                    output.Write(val);
                }

            }
            else if (bits == 8)
            {
                byte val;
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 3;
                if (!isLabelScan)
                {
                    output.Write((byte)0x24);
                    output.Write(reg);
                    output.Write(val);
                }
            }
        }

        static void interpretXorReg(BinaryWriter output, bool isLabelScan, byte reg1)
        {
            string sval = "";
            while (fileIndexPointer < sourceProgram.Length && char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            byte reg2 = 0;
            registersCodes.TryGetValue(sval, out reg2);
            asLength += 3;
            if (!isLabelScan)
            {
                output.Write((byte)0x25);
                output.Write(reg1);
                output.Write(reg2);
            }
        }

        static void interpretPusha(BinaryWriter output,bool isLabelScan)
        {
            
            asLength++;
            if (!isLabelScan) output.Write((byte)0x26);
        }

        static void interpretPopa(BinaryWriter output, bool isLabelScan)
        {
           
            asLength++;
            if (!isLabelScan) output.Write((byte)0x27);
        }

        static void interpretDb(BinaryWriter output, bool isLabelScan)
        {

            eatWhiteSpaces();
            string sval = "";
            byte val = 0;
            bool isHex = false;
            if (fileIndexPointer<sourceProgram.Length-1 && sourceProgram[fileIndexPointer + 1] == 'x')
                isHex = true;
            if (sourceProgram[fileIndexPointer] != '"')
            {
                while (char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
                {
                    sval += sourceProgram[fileIndexPointer];
                    fileIndexPointer++;
                    if (fileIndexPointer >= sourceProgram.Length) break;
                }
                if (isHex) val = Convert.ToByte(sval, 16);
                else val = byte.Parse(sval);
                asLength += 1;
                if (!isLabelScan)
                {
                    //output.Write((byte)0x05);
                    output.Write(val);
                }
            }
            else if(sourceProgram[fileIndexPointer]=='"')
            {
                fileIndexPointer++;
                while(sourceProgram[fileIndexPointer]!='"')
                {
                    asLength++;
                    if(!isLabelScan)
                    {
                        output.Write(sourceProgram[fileIndexPointer]);
                    }
                    fileIndexPointer++;
                }
                fileIndexPointer++;
                
            }
        }

        static void interpretTimes(BinaryWriter output, bool isLabelScan)
        {
            eatWhiteSpaces();
            string sval = "";
            string type = "";
            ushort times = 0;
            byte val;
            while(char.IsDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
                
            }
            times = ushort.Parse(sval);
            eatWhiteSpaces();
            sval = "";
            while (char.IsLetter(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;

            }
            if (sval == "db") type = "bytes";
            eatWhiteSpaces();
            bool isHex = false;
            sval = "";
            if (sourceProgram[fileIndexPointer + 1] == 'x') isHex = true;
            while(char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
                if (fileIndexPointer >= sourceProgram.Length) break;
            }
            if (isHex) val = Convert.ToByte(sval, 16);
            else val = Byte.Parse(sval);
            asLength += times;
            if (!isLabelScan)
            {
                if (type == "bytes")
                {
                    for (int i = 1; i <= times; i++)
                        output.Write(val);
                }
            }
        }

        static void doEnd(BinaryWriter output, bool isLabelScan)
        {
            asLength++;
            if(!isLabelScan)
            {
                output.Write((byte)0x02);
            }

        }

        static ushort ReadWord()
        {
            ushort val=0;
            bool isHex = false;
            string sval = "";
            if (sourceProgram[fileIndexPointer + 1] == 'x')
                isHex = true;
            while(char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
                if (fileIndexPointer >= sourceProgram.Length) break;
            }
            if(isHex)
            {
                val = Convert.ToUInt16(sval, 16);
                //Console.WriteLine(val);
            }
            else
            {
                val = ushort.Parse(sval);
            }
            return val;
        }

        static byte ReadByte()
        {
            byte val = 0;
            bool isHex = false;
            string sval = "";
            if (sourceProgram[fileIndexPointer+1] == 'x')
                isHex = true;
            
            while (char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                sval += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
                if (fileIndexPointer >= sourceProgram.Length) break;
            }
            if (isHex)
            {
                val = Convert.ToByte(sval, 16);
            }
            else
            {
                val = byte.Parse(sval);
            }
            return val;
        }

        static string ReadOperand()
        {
            string val = "";
            while(sourceProgram[fileIndexPointer]!=',')
            {
                val += sourceProgram[fileIndexPointer];
                fileIndexPointer++;
            }
            return val;
        }

        static void eatWhiteSpaces()
        {
            while (char.IsWhiteSpace(sourceProgram[fileIndexPointer]) || sourceProgram[fileIndexPointer] == '\n')
            {
                fileIndexPointer++;
                if (fileIndexPointer >= sourceProgram.Length) break;
            }  
        }

        static string GetLabelName()
        {
            string val="";
            while(char.IsLetterOrDigit(sourceProgram[fileIndexPointer]))
            {
                if(sourceProgram[fileIndexPointer]==':')
                {
                    fileIndexPointer++;
                    break;
                }
                
                val += sourceProgram[fileIndexPointer];
                fileIndexPointer++;

            }
            //Console.Write(val);
            return val;
        }
    }
}
