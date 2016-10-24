using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace VM
{
    public partial class Form1 : Form
    {
        #region Variables
        private byte[] VMMemory;
        private byte[] RealMemory1;
        private byte[] RealMemory2;
        private byte[] RealMemory3;
        private ushort StartAddr;
        private ushort ExecAddr;
        private ushort FileSize;
        private ushort ip;
        private ushort ax;
        private ushort bx;
        private ushort cx;
        private ushort dx;
        private ushort bp;
        private ushort sp;
        private byte ah;
        private byte al;
        private byte bh;
        private byte bl;
        private byte ch;
        private byte cl;
        private byte dh;
        private byte dl;
        private byte FLAGS;
        private int speedMS;
        delegate void SetTextCallBack(string text);
        delegate void PokeCallBack(byte value);
        Thread prog;
        #endregion
        
        public Form1(string[] args)
        {
            InitializeComponent();
            VMMemory = new byte[65536];
            RealMemory1 = new byte[2000000000];
            RealMemory2= new byte[2000000000];
            RealMemory3= new byte[294967296];
            StartAddr = 0;
            ExecAddr = 0;
            FileSize = 0;
            ax = 0;
            bx = 0;
            cx = 0;
            dx = 0;
            bp = 4096;
            sp = 4096;
            al = 0;
            ah = 0;
            bh = 0;
            bl = 0;
            ch = 0;
            cl = 0;
            dh = 0;
            dl = 0;
            prog = null;
            UpdateRegistersStatus();
            if (args.Length>0)
                runProgram(args[0]);
            
        }

        private void UpdateRegistersStatus()
        {
            string strRegisters = "";

            strRegisters += "ax = 0x" + ax.ToString("X").PadLeft(4, '0');
            strRegisters += " bx = 0x" + bx.ToString("X").PadLeft(4, '0');
            strRegisters += " cx = 0x" + cx.ToString("X").PadLeft(4, '0');
            strRegisters += " dx = 0x" + dx.ToString("X").PadLeft(4, '0');
            strRegisters += " ip = 0x" + ip.ToString("X").PadLeft(4, '0');
            strRegisters += " FLAGS=0x" + FLAGS.ToString("X").PadLeft(2, '0');
            strRegisters += " sp=0x" + sp.ToString("X").PadLeft(4, '0');
            strRegisters += " bp=0x" + bp.ToString("X").PadLeft(4, '0');

            if(lblRegister.InvokeRequired)
            {
                SetTextCallBack z = new SetTextCallBack(SetRegisterText);
                Invoke(z, new object[] { strRegisters });

            }
            else
            {
                SetRegisterText(strRegisters);
            }
        }

        private void ThreadPoke(byte value)
        {
            if(vmScreen1.InvokeRequired)
            {
                PokeCallBack pcb = new PokeCallBack(Poke);
                Invoke(pcb, new object[] { value });
            }
            else
            {
                Poke(value);
            }
        }
        private void Poke(byte value)
        {
            lock(vmScreen1)
            {
                vmScreen1.Poke(value);
            }
        }
        private void SetRegisterText(string text)
        {
            lblRegister.Text = text;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte Magic1;
            byte Magic2;
            byte Magic3;
            byte Magic4;

            openFileDialog1.ShowDialog();

            BinaryReader br;
            if (openFileDialog1.FileName == "") return;
            FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open);
            br = new BinaryReader(fs);

            Magic1 = br.ReadByte();
            Magic2 = br.ReadByte();
            Magic3 = br.ReadByte();
            Magic4 = br.ReadByte();
          
            if(Magic1!='5' || Magic2!='5' || Magic3!='a' || Magic4!='a')
            {
                MessageBox.Show("This is not a valid VM file!", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            StartAddr = br.ReadUInt16();
            ExecAddr = br.ReadUInt16();
            FileSize =(ushort)( br.ReadUInt16()-StartAddr);
            ushort Counter = 0;
            while(FileSize>0)
            {
                VMMemory[(StartAddr + Counter)] = br.ReadByte();
                Counter++;
                FileSize--;
            }
            br.Close();
            fs.Close();
            ip = StartAddr;

            prog = new Thread(delegate () { ExecuteProgram(ExecAddr, Counter); });
            prog.Start();
        }

        private void runProgram(string name)
        {
            byte Magic1;
            byte Magic2;
            byte Magic3;
            byte Magic4;

            

            BinaryReader br;
            if (name == "") return;
            FileStream fs = new FileStream(name, FileMode.Open);
            br = new BinaryReader(fs);

            Magic1 = br.ReadByte();
            Magic2 = br.ReadByte();
            Magic3 = br.ReadByte();
            Magic4 = br.ReadByte();

            if (Magic1 != '5' || Magic2 != '5' || Magic3 != 'a' || Magic4 != 'a')
            {
                MessageBox.Show("This is not a valid VM file!", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            StartAddr = br.ReadUInt16();
            ExecAddr = br.ReadUInt16();
            FileSize = (ushort)(br.ReadUInt16() - StartAddr);
            ushort Counter = 0;
            while (FileSize > 0)
            {
                VMMemory[(StartAddr + Counter)] = br.ReadByte();
                Counter++;
                FileSize--;
            }
            br.Close();
            fs.Close();
            ip = StartAddr;

            prog = new Thread(delegate () { ExecuteProgram(ExecAddr, Counter); });
            prog.Start();
        }

        private void UpdateReg(byte reg)
        {
            if(reg==0x01)//ax
            {
                ah = (byte)(ax >> 8);
                al = (byte)(ax & 0x00ff);
            }
            if (reg == 0x02)//bx
            {
                bh = (byte)(bx >> 8);
                bl = (byte)(bx & 0x00ff);
            }
            if (reg == 0x03)//cx
            {
                ch = (byte)(cx >> 8);
                cl = (byte)(cx & 0x00ff);
            }
            if (reg == 0x04)//dx
            {
                dh = (byte)(dx >> 8);
                dl = (byte)(dx & 0x00ff);
            }
            if(reg == 0x05)//ah
            {
                ax = (ushort)((ah << 8) + al);

            }
            if(reg == 0x06)//al
            {
                ax= (ushort)((ah << 8) + al);
            }
            if (reg == 0x07)//bh
            {
                bx = (ushort)((bh << 8) + bl);

            }
            if (reg == 0x08)//bl
            {
                bx = (ushort)((bh << 8 )+ bl);
            }
            if (reg == 0x09)//ch
            {
                cx = (ushort)((ch << 8) + cl);

            }
            if (reg == 0x0a)//cl
            {
                cx = (ushort)((ch << 8 )+ cl);
            }
            if (reg == 0x0b)//dh
            {
                dx = (ushort)((dh << 8) + dl);

            }
            if (reg == 0x0c)//al
            {
                dx = (ushort)((dh << 8 )+ dl);
            }
        }

        private void ExecuteProgram(ushort ExecAddr,ushort ProgLength)
        {
            while (true)
            {
             
                byte Instruction = VMMemory[ip];
                Thread.Sleep(speedMS);
                #region END
                if (Instruction == 0x02) break;
                #endregion
                #region MOV
                if (Instruction == 0x01) //mov
                {
                    byte Register = VMMemory[ip + 1];
                    ProgLength -= 1;

                    if (Register == 0x01)// ax
                    {
                        //MessageBox.Show(Convert.ToString(VMMemory[ip+3]));
                        ax = (ushort)(VMMemory[ip + 3] << 8);
                        ax += VMMemory[ip + 2];
                        ah = VMMemory[ip + 3];
                        al = VMMemory[ip + 2];
                        ProgLength -= 2;
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx = (ushort)(VMMemory[ip + 3] << 8);
                        bx += VMMemory[ip + 2];
                        bh = VMMemory[ip + 3];
                        bl = VMMemory[ip + 2];
                        ProgLength -= 2;
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx = (ushort)(VMMemory[ip + 3] << 8);
                        cx += VMMemory[ip + 2];
                        ch = VMMemory[ip + 3];
                        cl = VMMemory[ip + 2];
                        ProgLength -= 2;
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx = (ushort)(VMMemory[ip + 3] << 8);
                        dx += VMMemory[ip + 2];
                        dh = VMMemory[ip + 3];
                        dl = VMMemory[ip + 2];
                        ProgLength -= 2;
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah = VMMemory[ip + 2];
                        ax = (ushort)((ax & 0x00ff) + (ushort)(ah << 8));
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al = VMMemory[ip + 2];
                        ax = (ushort)((ax & 0xff00) + al);
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh = VMMemory[ip + 2];
                        bx = (ushort)((bx & 0x00ff) + (ushort)(bh << 8));
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl = VMMemory[ip + 2];
                        bx = (ushort)((bx & 0xff00) + bl);
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch = VMMemory[ip + 2];
                        cx = (ushort)((cx & 0x00ff) + (ushort)(ch << 8));
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl = VMMemory[ip + 2];
                        cx = (ushort)((cx & 0xff00) + cl);
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh = VMMemory[ip + 2];
                        dx = (ushort)((dx & 0x00ff) + (ushort)(dh << 8));
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl = VMMemory[ip + 2];
                        dx = (ushort)((dx & 0xff00) + dl);
                        ProgLength -= 1;
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp = (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        ProgLength -= 1;
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp = (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        ProgLength -= 1;
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }


                }
                #endregion MOV
                #region JMP
                if(Instruction==0x03)
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                }
                #endregion
                #region INT
                if (Instruction == 0x04)
                {
                    byte address = VMMemory[ip + 1];
                    ProgLength--;
                    if (address == 0x10) // Print
                    {
                        ThreadPoke(al);
                        ThreadPoke(ah);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;

                    }
                }
                #endregion INT
                #region MOVPOINTER
                if (Instruction == 0x05)
                {
                    byte Register = VMMemory[ip + 1];
                    ProgLength--;
                    #region ax
                    if (Register == 0x01)//ax
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            al = VMMemory[ax + Plus];
                            ah = VMMemory[ax + Plus + 1];
                            ax = (ushort)((ah<<8) + al);

                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            al = VMMemory[bx + Plus];
                            ah = VMMemory[bx + Plus + 1];
                            ax = (ushort)((ah << 8) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            al = VMMemory[cx + Plus];
                            ah = VMMemory[cx + Plus + 1];
                            ax = (ushort)((ah << 8) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            al = VMMemory[dx + Plus];
                            ah = VMMemory[dx + Plus + 1];
                            ax = (ushort)((ah << 8) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            al = VMMemory[bp + Plus];
                            ah = VMMemory[bp + Plus + 1];
                            ax = (ushort)((VMMemory[bp + Plus+1]<<8) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            al = VMMemory[sp + Plus];
                            ah = VMMemory[sp + Plus + 1];
                            ax = (ushort)((VMMemory[sp + Plus + 1] << 8) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region bx
                    if (Register == 0x02)//bx
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {

                            bl = VMMemory[ax + Plus];
                            bh = VMMemory[ax + Plus + 1];
                            bx = (ushort)((bh << 8) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            bl = VMMemory[bx + Plus];
                            bh = VMMemory[bx + Plus + 1];
                            bx = (ushort)((bh << 8) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            bl = VMMemory[cx + Plus];
                            bh = VMMemory[cx + Plus + 1];
                            bx = (ushort)((bh << 8) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            bl = VMMemory[dx + Plus];
                            bh = VMMemory[dx + Plus + 1];
                            bx = (ushort)((bh << 8) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            bl = VMMemory[bp + Plus];
                            bh = VMMemory[bp + Plus + 1];
                            bx = (ushort)((VMMemory[bp + Plus + 1] << 8) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            bl = VMMemory[sp + Plus];
                            bh = VMMemory[bp + Plus + 1];
                            bx = (ushort)((VMMemory[sp + Plus + 1] << 8) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region cx
                    if (Register == 0x03)//cx
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {

                            cl = VMMemory[ax + Plus];
                            ch = VMMemory[ax + Plus + 1];
                            cx = (ushort)((ch << 8) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            cl = VMMemory[bx + Plus];
                            ch = VMMemory[bx + Plus + 1];
                            cx = (ushort)((ch << 8) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            cl = VMMemory[cx + Plus];
                            ch = VMMemory[cx + Plus + 1];
                            cx = (ushort)((ch << 8) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            cl = VMMemory[dx + Plus];
                            ch = VMMemory[dx + Plus + 1];
                            cx = (ushort)((ch << 8) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            cl = VMMemory[bp + Plus];
                            ch = VMMemory[bp + Plus + 1];
                            cx = (ushort)((VMMemory[bp + Plus + 1] << 8) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            cl = VMMemory[sp + Plus];
                            ch = VMMemory[bp + Plus + 1];
                            cx = (ushort)((VMMemory[sp + Plus + 1] << 8) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region dx
                    if (Register == 0x04)//dx
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {

                            dl = VMMemory[ax + Plus];
                            dh = VMMemory[ax + Plus + 1];
                            dx = (ushort)((dh << 8) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            dl = VMMemory[bx + Plus];
                            dh = VMMemory[bx + Plus + 1];
                            dx = (ushort)((dh << 8) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            dl = VMMemory[cx + Plus];
                            dh = VMMemory[cx + Plus + 1];
                            dx = (ushort)((dh << 8) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            dl = VMMemory[dx + Plus];
                            dh = VMMemory[dx + Plus + 1];
                            dx = (ushort)((dh << 8) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            dl = VMMemory[bp + Plus];
                            dh = VMMemory[bp + Plus + 1];
                            dx = (ushort)((VMMemory[bp + Plus + 1] << 8) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            dl = VMMemory[sp + Plus];
                            dh = VMMemory[bp + Plus + 1];
                            dx = (ushort)((VMMemory[sp + Plus + 1] << 8) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region ah
                    if (Register == 0x05)//ah
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            ah = VMMemory[ax + Plus];
                            ax = (ushort)((ax & 0x00ff) + ah << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            ah = VMMemory[bx + Plus];
                            ax = (ushort)((ax & 0x00ff) + ah << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            ah = VMMemory[cx + Plus];
                            ax = (ushort)((ax & 0x00ff) + ah << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            ah = VMMemory[dx + Plus];
                            ax = (ushort)((ax & 0x00ff) + ah << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            
                            ah = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            ah = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region al
                    if (Register == 0x06)//al
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            al = VMMemory[ax + Plus];
                            ax = (ushort)((ax & 0xff00) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            al = VMMemory[bx + Plus];
                            ax = (ushort)((ax & 0xff00) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            al = VMMemory[cx + Plus];
                            ax = (ushort)((ax & 0xff00) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            al = VMMemory[dx + Plus];
                            ax = (ushort)((ax & 0xff00) + al);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            al = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            al = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region bh
                    if (Register == 0x07)//bh
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            bh = VMMemory[ax + Plus];
                            bx = (ushort)((bx & 0x00ff) + bh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            bh = VMMemory[bx + Plus];
                            bx = (ushort)((bx & 0x00ff) + bh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            bh = VMMemory[cx + Plus];
                            bx = (ushort)((bx & 0x00ff) + bh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            bh = VMMemory[dx + Plus];
                            bx = (ushort)((bx & 0x00ff) + bh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            bh = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            bh = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (Register == 0x08)//bl
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            bl = VMMemory[ax + Plus];
                            bx = (ushort)((bx & 0xff00) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            bl = VMMemory[bx + Plus];
                            bx = (ushort)((bx & 0xff00) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            bl = VMMemory[cx + Plus];
                            bx = (ushort)((bx & 0xff00) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            bl = VMMemory[dx + Plus];
                            bx = (ushort)((bx & 0xff00) + bl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            bl = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            bl = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }
                    #endregion
                    #region ch
                    if (Register == 0x09)//ch
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            ch = VMMemory[ax + Plus];
                            cx = (ushort)((cx & 0x00ff) + ch << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            ch = VMMemory[bx + Plus];
                            cx = (ushort)((cx & 0x00ff) + ch << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            ch = VMMemory[cx + Plus];
                            cx = (ushort)((cx & 0x00ff) + ch << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            ch = VMMemory[dx + Plus];
                            cx = (ushort)((cx & 0x00ff) + ch << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            ch = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            ch = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (Register == 0x0a)//cl
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            cl = VMMemory[ax + Plus];
                            cx = (ushort)((cx & 0xff00) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            cl = VMMemory[bx + Plus];
                            cx = (ushort)((cx & 0xff00) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            cl = VMMemory[cx + Plus];
                            cx = (ushort)((cx & 0xff00) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            cl = VMMemory[dx + Plus];
                            cx = (ushort)((cx & 0xff00) + cl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            cl = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            cl = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (Register == 0x0b)//dh
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            dh = VMMemory[ax + Plus];
                            dx = (ushort)((dx & 0x00ff) + dh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            dh = VMMemory[bx + Plus];
                            dx = (ushort)((dx & 0x00ff) + dh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            dh = VMMemory[cx + Plus];
                            dx = (ushort)((dx & 0x00ff) + dh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            dh = VMMemory[dx + Plus];
                            dx = (ushort)((dx & 0x00ff) + dh << 8);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            dh = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            dh = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (Register == 0x0c)//dl
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            dl = VMMemory[ax + Plus];
                            dx = (ushort)((dx & 0xff00) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            dl = VMMemory[bx + Plus];
                            dx = (ushort)((dx & 0xff00) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            dl = VMMemory[cx + Plus];
                            dx = (ushort)((dx & 0xff00) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            dl = VMMemory[dx + Plus];
                            dx = (ushort)((dx & 0xff00) + dl);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {

                            dl = VMMemory[bp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            dl = VMMemory[sp + Plus];
                            UpdateReg(Register);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    
                    if (Register == 0x0d)//bp
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {
                            
                            bp = (ushort)((VMMemory[ax + Plus + 1] << 8) + VMMemory[ax + Plus]);

                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            bp = (ushort)((VMMemory[bx + Plus + 1] << 8) + VMMemory[bx + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            bp = (ushort)((VMMemory[cx + Plus + 1] << 8) + VMMemory[cx + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            bp = (ushort)((VMMemory[dx + Plus + 1] << 8) + VMMemory[dx + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            bp = (ushort)((VMMemory[bp + Plus + 1] << 8) + VMMemory[bp + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            bp = (ushort)((VMMemory[sp + Plus + 1] << 8) + VMMemory[sp + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }

                    #endregion
                    #region sp

                    if (Register == 0x0e)//sp
                    {
                        byte RegisterPointer = VMMemory[ip + 2];
                        ProgLength--;
                        byte Plus = VMMemory[ip + 3];
                        ProgLength--;
                        if (RegisterPointer == 0x01)//ax
                        {

                            sp = (ushort)((VMMemory[ax + Plus + 1] << 8) + VMMemory[ax + Plus]);

                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x02)//bx
                        {
                            sp = (ushort)((VMMemory[bx + Plus + 1] << 8) + VMMemory[bx + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x03)//cx
                        {
                            sp = (ushort)((VMMemory[cx + Plus + 1] << 8) + VMMemory[cx + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x04)//dx
                        {
                            sp = (ushort)((VMMemory[dx + Plus + 1] << 8) + VMMemory[dx + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0d)//bp
                        {
                            sp = (ushort)((VMMemory[bp + Plus + 1] << 8) + VMMemory[bp + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (RegisterPointer == 0x0e)//sp
                        {
                            sp = (ushort)((VMMemory[sp + Plus + 1] << 8) + VMMemory[sp + Plus]);
                            ip += 4;
                            UpdateRegistersStatus();
                            continue;
                        }

                    }

                    #endregion
                }
                #endregion MOVPOINTER
                #region MOVREG
                if (Instruction == 0x0d)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];
                    ProgLength -= 2;
                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if(reg2==0x01)//ax
                        {
                            //ax = ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax = bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax = cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax = dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax = bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax = sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx = ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            //bx = bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx = cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx = dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx = bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx = sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx = ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx = bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            //cx = cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx = dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx = bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx = sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx = ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx = bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx = cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            //dx = dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx = bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx = sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if(reg1==0x05)//ah
                    {
                        if(reg2==0x05)//ah
                        {
                            //ah = ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah = cl;
                            UpdateReg(reg1);
                            ip += 3; 
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            //al = al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al = cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                           // bh = bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh = cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                           // bl = bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl = cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            //ch = ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch = cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                           // cl = cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh = cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                           // dh = dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh = dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl = ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl = al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl = bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl = bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl = ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl = cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl = dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                           // dl = dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {
                            //ax = ax;
                            bp = ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp = bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp = cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp = dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                          // bp = bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp = sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {
                            //ax = ax;
                            sp = ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp = bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp = cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp = dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp = bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            //sp = sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion

                }
                #endregion
                #region CMP
                if (Instruction==0x06)
                {
                    byte regcod1 = VMMemory[ip + 1];
                    byte regcod2 = VMMemory[ip + 2];
                    ushort reg1 = 0;
                    ushort reg2 = 0;
                    if(regcod1==0x01)//ax
                    {
                        reg1 = ax;
                    }
                    else if(regcod1==0x02)//bx
                    {
                        reg1 = bx;
                    }
                    else if (regcod1 == 0x03)//cx
                    {
                        reg1 = cx;
                    }
                    else if (regcod1 == 0x04)//dx
                    {
                        reg1 = dx;
                    }
                    else if (regcod1 == 0x05)//ah
                    {
                        reg1 = ah;
                    }
                    else if (regcod1 == 0x06)//al
                    {
                        reg1 = al;
                    }
                    else if (regcod1 == 0x07)//bh
                    {
                        reg1 = bh;
                    }
                    else if (regcod1 == 0x08)//bl
                    {
                        reg1 = bl;
                    }
                    else if (regcod1 == 0x09)//ch
                    {
                        reg1 = ch;
                    }
                    else if (regcod1 == 0x0a)//cl
                    {
                        reg1 = cl;
                    }
                    else if (regcod1 == 0x0b)//dh
                    {
                        reg1 = dh;
                    }
                    else if (regcod1 == 0x0c)//dl
                    {
                        reg1 = dl;
                    }

                    if (regcod2 == 0x01)//ax
                    {
                        reg2 = ax;
                    }
                    else if (regcod2 == 0x02)//bx
                    {
                        reg2 = bx;
                    }
                    else if (regcod2 == 0x03)//cx
                    {
                        reg2 = cx;
                    }
                    else if (regcod2 == 0x04)//dx
                    {
                        reg2 = dx;
                    }
                    else if (regcod2 == 0x05)//ah
                    {
                        reg2 = ah;
                    }
                    else if (regcod2 == 0x06)//al
                    {
                        reg2 = al;
                    }
                    else if (regcod2 == 0x07)//bh
                    {
                        reg2 = bh;
                    }
                    else if (regcod2 == 0x08)//bl
                    {
                        reg2 = bl;
                    }
                    else if (regcod2 == 0x09)//ch
                    {
                        reg2 = ch;
                    }
                    else if (regcod2 == 0x0a)//cl
                    {
                        reg2 = cl;
                    }
                    else if (regcod2 == 0x0b)//dh
                    {
                        reg2 = dh;
                    }
                    else if (regcod2 == 0x0c)//dl
                    {
                        reg2 = dl;
                    }


                    if (reg1 > reg2)
                    {
                        FLAGS =(byte)( FLAGS | 2);
                    }
                    else if(reg1<reg2)
                    {
                        FLAGS =(byte)( FLAGS & 253);
                    }
                    if(reg1==reg2)
                    {
                        FLAGS = (byte)(FLAGS | 1);

                    }
                    else if(reg1!=reg2)
                    {
                        FLAGS = (byte)(FLAGS & 254);
                    }
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }

                #endregion
                #region JE
                if(Instruction==0x07 && (FLAGS & 1)==1)
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                    UpdateRegistersStatus();
                    continue;
                }
                else if(Instruction == 0x07)
                {
                    ProgLength -= 2;
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region JNE
                if(Instruction==0x08 && (FLAGS & 1)==0)
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                    UpdateRegistersStatus();
                    continue;
                }
                else if (Instruction == 0x08)
                {
                    ProgLength -= 2;
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region JG
                if (Instruction==0x09 && (FLAGS & 2)==2)
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                    UpdateRegistersStatus();
                    continue;
                }
                else if (Instruction == 0x09)
                {
                    ProgLength -= 2;
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region JL
                if (Instruction == 0x0a && (FLAGS & 2) == 0)
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                    UpdateRegistersStatus();
                    continue;
                }
                else if (Instruction == 0x0a)
                {
                    ProgLength -= 2;
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region JGE
                if (Instruction == 0x0b && ((FLAGS & 2) == 2 || (FLAGS & 1)==1))
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                    UpdateRegistersStatus();
                    continue;
                }
                else if (Instruction == 0x0b)
                {
                    ProgLength -= 2;
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region JLE
                if (Instruction == 0x0c && ((FLAGS & 2) == 0 || (FLAGS & 1) == 1))
                {
                    ip = (ushort)((VMMemory[ip + 2] << 8) + VMMemory[ip + 1]);
                    UpdateRegistersStatus();
                    continue;
                }
                else if (Instruction == 0x0c)
                {
                    ProgLength -= 2;
                    ip += 3;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region INC
                if(Instruction==0x0e)
                {
                    byte reg = VMMemory[ip + 1];
                    if(reg==0x01)//ax
                    {
                        ax += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x02)//bx
                    {
                        bx += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x03)//cx
                    {
                        cx += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x04)//dx
                    {
                        dx += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x05)//ah
                    {
                        ah += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x06)//al
                    {
                        al += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x07)//bh
                    {
                        bh += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x08)//bl
                    {
                        bl += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x09)//ch
                    {
                        ch += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0a)//cl
                    {
                        cl += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0b)//dh
                    {
                        dh += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0c)//dl
                    {
                        dl += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0d)//bp
                    {
                        bp += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0e)//sp
                    {
                        sp += 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }

                }
                #endregion
                #region PUSH
                if(Instruction==0x0f)
                {
                    byte reg = VMMemory[ip + 1];

                    if (reg == 0x01)//ax
                    {
                        sp -= 2;
                        VMMemory[sp + 1] = ah;
                        VMMemory[sp] = al;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x02)//bx
                    {
                        sp -= 2;
                        VMMemory[sp + 1] = bh;
                        VMMemory[sp] = bl;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x03)//cx
                    {
                        sp -= 2;
                        VMMemory[sp + 1] = ch;
                        VMMemory[sp] = cl;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x04)//dx
                    {
                        sp -= 2;
                        VMMemory[sp + 1] = dh;
                        VMMemory[sp] = dl;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0d)//bp
                    {
                        sp -= 2;
                        VMMemory[sp + 1] =(byte)( bp >> 8);
                        VMMemory[sp] = (byte)(bp & 0x00ff);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    


                }
                #endregion
                #region POP
                if(Instruction==0x10)
                {
                    byte reg = VMMemory[ip + 1];
                    if(reg==0x01)//ax
                    {
                        ax = (ushort)((VMMemory[sp + 1] << 8) + VMMemory[sp]);
                        UpdateReg(reg);
                        sp += 2;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x02)//bx
                    {
                        bx = (ushort)((VMMemory[sp + 1] << 8) + VMMemory[sp]);
                        UpdateReg(reg);
                        sp += 2;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x03)//cx
                    {
                        cx = (ushort)((VMMemory[sp + 1] << 8) + VMMemory[sp]);
                        UpdateReg(reg);
                        sp += 2;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x04)//dx
                    {
                        dx = (ushort)((VMMemory[sp + 1] << 8) + VMMemory[sp]);
                        UpdateReg(reg);
                        sp += 2;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0d)//bp
                    {
                        bp = (ushort)((VMMemory[sp + 1] << 8) + VMMemory[sp]);
                        sp += 2;
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    
                }
                #endregion
                #region CALL
                if(Instruction==0x11)
                {
                    sp -= 2;
                    ip+=3;
                    VMMemory[sp+1] = (byte)(ip >> 8);
                    VMMemory[sp] = (byte)(ip & 0x00ff);
                    bp = sp;
                    ip -= 3;
                    ip = (ushort)((VMMemory[ip + 2]<<8)+VMMemory[ip+1]);
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region RET
                if(Instruction==0x12)
                {
                    ip = (ushort)((VMMemory[bp + 1] << 8) + VMMemory[bp]);
                    sp += 2;
                    bp = sp;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region DEC
                if(Instruction==0x13)
                {
                    byte reg = VMMemory[ip + 1];
                    if (reg == 0x01)//ax
                    {
                        ax -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x02)//bx
                    {
                        bx -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x03)//cx
                    {
                        cx -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x04)//dx
                    {
                        dx -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x05)//ah
                    {
                        ah -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x06)//al
                    {
                        al -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x07)//bh
                    {
                        bh -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x08)//bl
                    {
                        bl -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x09)//ch
                    {
                        ch -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0a)//cl
                    {
                        cl -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg== 0x0b)//dh
                    {
                        dh -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0c)//dl
                    {
                        dl -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0d)//bp
                    {
                        bp -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (reg == 0x0e)//sp
                    {
                        sp -= 1;
                        UpdateReg(reg);
                        ip += 2;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region ADD
                if(Instruction==0x14)
                {
                    byte Register = VMMemory[ip + 1];
                  

                    if (Register == 0x01)// ax
                    {

                        ax += (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);
                        
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx += (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx += (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx += (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah += VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl += VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp += (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp += (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                       
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region ADDREG
                if(Instruction==0x15)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];
                    ProgLength -= 2;
                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax += ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax += bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax += cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax += dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax += bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax += sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx += ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx += bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx += cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx += dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx += bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx += sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx += ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx += bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx += cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx += dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx += bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx += sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx += ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx += bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx += cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx += dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx += bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx += sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah += ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al += al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                             bh += bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl += bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch += ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl += cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh += dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh += dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl += ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl += al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl += bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl += bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl += ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl += cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl += dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl += dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {
                            
                            bp += ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp += bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp += cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp += dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp += bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp += sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {
                           
                            sp += ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp += bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp += cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp += dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp += bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp += sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region SUB
                if(Instruction==0x16)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax -= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx -= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx -= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx -= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah -= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl -= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp -= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp -= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region SUBREG
                if (Instruction == 0x17)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];
                    ProgLength -= 2;
                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax -= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax -= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax -= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax -= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax -= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax -= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx -= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx -= bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx -= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx -= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx -= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx -= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx -= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx -= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx -= cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx -= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx -= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx -= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx -= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx -= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx -= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx -= dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx -= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx -= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah -= ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al -= al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bh -= bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl -= bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch -= ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl -= cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh -= dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh -= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl -= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl -= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl -= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl -= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl -= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl -= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl -= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl -= dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            bp -= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp -= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp -= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp -= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp -= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp -= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            sp -= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp -= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp -= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp -= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp -= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp -= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region MUL
                if(Instruction==0x18)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax *= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx *= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx *= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx *= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah *= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl *= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp *= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp *= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region MULREG
                if(Instruction==0x19)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];
                    
                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax *= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax *= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax *= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax *= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax *= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax *= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx *= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx *= bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx *= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx *= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx *= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx *= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx *= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx *= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx *= cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx *= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx *= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx *= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx *= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx *= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx *= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx *= dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx *= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx *= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah *= ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al *= al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bh *= bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl *= bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch *= ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl *= cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh *= dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh *= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl *= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl *= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl *= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl *= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl *= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl *= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl *= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl *= dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            bp *= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp *= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp *= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp *= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp *= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp *= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            sp *= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp *= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp *= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp *= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp *= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp *= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region DIV
                if (Instruction == 0x1a)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax /= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx /= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx /= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx /= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah /= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl /= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp /= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp /= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region DIVREG
                if (Instruction == 0x1b)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];

                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax /= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax /= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax /= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax /= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax /= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax /= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx /= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx /= bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx /= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx /= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx /= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx /= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx /= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx /= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx /= cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx /= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx /= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx /= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx /= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx /= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx /= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx /= dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx /= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx /= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah /= ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al /= al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bh /= bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl /= bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch /= ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl /= cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh /= dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh /= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl /= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl /= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl /= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl /= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl /= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl /= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl /= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl /= dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            bp /= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp /= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp /= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp /= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp /= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp /= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            sp /= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp /= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp /= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp /= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp /= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp /= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region SHR
                if (Instruction == 0x1c)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax >>= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx >>= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx >>= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx >>= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah >>= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl >>= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp >>= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp >>= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region SHL
                if (Instruction == 0x1d)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax <<= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx <<= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx <<= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx <<= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah <<= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl <<= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp <<= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp <<= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region OR
                if (Instruction == 0x1e)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax |= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx |= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx |= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx |= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah |= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl |= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp |= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp |= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region ORREG
                if (Instruction == 0x1f)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];

                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax |= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax |= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax |= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax |= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax |= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax |= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx |= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx |= bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx |= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx |= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx |= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx |= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx |= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx |= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx |= cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx |= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx |= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx |= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx |= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx |= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx |= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx |= dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx |= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx |= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah |= ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al |= al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bh |= bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl |= bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch |= ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl |= cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh |= dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh |= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl |= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl |= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl |= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl |= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl |= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl |= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl |= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl |= dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            bp |= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp |= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp |= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp |= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp |= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp |= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            sp |= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp |= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp |= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp |= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp |= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp |= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region AND
                if (Instruction == 0x20)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax &= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx &= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx &= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx &= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah &= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl &= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp &= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp &= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region ANDREG
                if (Instruction == 0x21)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];

                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax &= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax &= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax &= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax &= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax &= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax &= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx &= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx &= bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx &= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx &= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx &= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx &= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx &= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx &= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx &= cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx &= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx &= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx &= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx &= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx &= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx &= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx &= dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx &= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx &= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah &= ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al &= al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bh &= bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl &= bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch &= ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl &= cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh &= dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh &= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl &= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl &= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl &= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl &= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl &= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl &= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl &= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl &= dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            bp &= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp &= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp &= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp &= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp &= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp &= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            sp &= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp &= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp &= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp &= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp &= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp &= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region NOT
                if (Instruction == 0x22)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax = (ushort)(~ax);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx = (ushort)(~bx);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx = (ushort)(~cx);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx = (ushort)(~dx);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah = (byte)(~ah);
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al = (byte)(~al);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh = (byte)(~bh);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl = (byte)(~bl);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch = (byte)(~ch);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl = (byte)(~cl);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh = (byte)(~dh);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl = (byte)(~dl);
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp = (ushort)(~bp);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp = (ushort)(~sp);
                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region XOR
                if (Instruction == 0x24)
                {
                    byte Register = VMMemory[ip + 1];


                    if (Register == 0x01)// ax
                    {

                        ax ^= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x02)// bx
                    {
                        bx ^= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;

                    }
                    if (Register == 0x03)// cx
                    {
                        cx ^= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x04)// dx
                    {
                        dx ^= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);
                        UpdateReg(Register);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x05)// ah
                    {
                        ah ^= VMMemory[ip + 2];
                        UpdateReg(Register);

                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x06)// al
                    {
                        al ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x07)// bh
                    {
                        bh ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x08)// bl
                    {
                        bl ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x09)// ch
                    {
                        ch ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0a)// cl
                    {
                        cl ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0b)// dh
                    {
                        dh ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0c)// dl
                    {
                        dl ^= VMMemory[ip + 2];
                        UpdateReg(Register);
                        ip += 3;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0d)// bp
                    {

                        bp ^= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                    if (Register == 0x0e)// sp
                    {

                        sp ^= (ushort)((VMMemory[ip + 3] << 8) + VMMemory[ip + 2]);

                        ip += 4;
                        UpdateRegistersStatus();
                        continue;
                    }
                }
                #endregion
                #region XORREG
                if (Instruction == 0x25)
                {
                    byte reg1 = VMMemory[ip + 1];
                    byte reg2 = VMMemory[ip + 2];

                    #region ax
                    if (reg1 == 0x01)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            ax ^= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            ax ^= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            ax ^= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            ax ^= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            ax ^= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            ax ^= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region bx
                    if (reg1 == 0x02)//bx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            bx ^= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bx ^= bx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bx ^= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bx ^= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bx ^= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bx ^= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region cx
                    if (reg1 == 0x03)//ax
                    {
                        if (reg2 == 0x01)//ax
                        {
                            cx ^= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            cx ^= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            cx ^= cx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            cx ^= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            cx ^= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            cx ^= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region dx
                    if (reg1 == 0x04)//dx
                    {
                        if (reg2 == 0x01)//ax
                        {
                            dx ^= ax;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            dx ^= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            dx ^= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            dx ^= dx;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            dx ^= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            dx ^= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region ah
                    if (reg1 == 0x05)//ah
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ah ^= ah;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ah ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ah ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ah ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ah ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ah ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ah ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ah ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region al
                    if (reg1 == 0x06)//al
                    {
                        if (reg2 == 0x05)//ah
                        {
                            al ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            al ^= al;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            al ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            al ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            al ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            al ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            al ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            al ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bh
                    if (reg1 == 0x07)//bh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bh ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bh ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bh ^= bh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bh ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bh ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bh ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bh ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bh ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bl
                    if (reg1 == 0x08)//bl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            bl ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            bl ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            bl ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            bl ^= bl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            bl ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            bl ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            bl ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            bl ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region ch
                    if (reg1 == 0x09)//ch
                    {
                        if (reg2 == 0x05)//ah
                        {
                            ch ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            ch ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            ch ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            ch ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            ch ^= ch;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            ch ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            ch ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            ch ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region cl
                    if (reg1 == 0x0a)//cl
                    {
                        if (reg2 == 0x05)//ah
                        {

                            cl ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            cl ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            cl ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            cl ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            cl ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            cl ^= cl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            cl ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            cl ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dh
                    if (reg1 == 0x0b)//dh
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dh ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dh ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dh ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dh ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dh ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dh ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dh ^= dh;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dh ^= dl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region dl
                    if (reg1 == 0x0c)//dl
                    {
                        if (reg2 == 0x05)//ah
                        {
                            dl ^= ah;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x06)//al
                        {
                            dl ^= al;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x07)//bh
                        {
                            dl ^= bh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x08)//bl
                        {
                            dl ^= bl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x09)//ch
                        {
                            dl ^= ch;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0a)//cl
                        {
                            dl ^= cl;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0b)//dh
                        {
                            dl ^= dh;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                        if (reg2 == 0x0c)//dl
                        {
                            dl ^= dl;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;
                        }
                    }
                    #endregion
                    #region bp
                    if (reg1 == 0x0d)//bp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            bp ^= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            bp ^= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            bp ^= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            bp ^= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            bp ^= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            bp ^= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                    #region sp
                    if (reg1 == 0x0e)//sp
                    {
                        if (reg2 == 0x01)//ax
                        {

                            sp ^= ax;
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x02)//bx
                        {
                            sp ^= bx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x03)//cx
                        {
                            sp ^= cx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x04)//dx
                        {
                            sp ^= dx;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0d)//bp
                        {
                            sp ^= bp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                        if (reg2 == 0x0e)//sp
                        {
                            sp ^= sp;
                            UpdateReg(reg1);
                            ip += 3;
                            UpdateRegistersStatus();
                            continue;

                        }
                    }
                    #endregion
                }
                #endregion
                #region PUSHA
                if(Instruction==0x26)
                {
                    sp -= 8;
                    VMMemory[sp] = dl;
                    VMMemory[sp + 1] = dh;
                    VMMemory[sp + 2] = cl;
                    VMMemory[sp + 3] = ch;
                    VMMemory[sp + 4] = bl;
                    VMMemory[sp + 5] = bh;
                    VMMemory[sp + 6] = al;
                    VMMemory[sp + 7] = ah;
                    sp -= 2;
                    VMMemory[sp] = (byte)((sp + 2) & 0x00ff);
                    VMMemory[sp + 1] = (byte)((sp + 2) >> 8);
                    sp -= 2;
                    VMMemory[sp] = (byte)(bp & 0x00ff);
                    VMMemory[sp + 1] = (byte)(bp >> 8);
                    ip += 1;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
                #region POPA
                if(Instruction==0x27)
                {
                    bp = (ushort)(VMMemory[sp] + (VMMemory[sp + 1] << 8));
                    sp += 4;
                    dx = (ushort)(VMMemory[sp] + (VMMemory[sp + 1] << 8));
                    UpdateReg(0x04);
                    sp += 2;
                    cx = (ushort)(VMMemory[sp] + (VMMemory[sp + 1] << 8));
                    UpdateReg(0x03);
                    sp += 2;
                    bx = (ushort)(VMMemory[sp] + (VMMemory[sp + 1] << 8));
                    UpdateReg(0x02);
                    sp += 2;
                    ax = (ushort)(VMMemory[sp] + (VMMemory[sp + 1] << 8));
                    UpdateReg(0x01);
                    sp += 2;
                    ip++;
                    UpdateRegistersStatus();
                    continue;
                }
                #endregion
            }
        }

        private void mSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            speedMS = 500;
        }

        private void realTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            speedMS = 0;
        }

        private void mSToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            speedMS = 250;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (prog!=null && prog.IsAlive)
                prog.Abort();
            
                
        }

        private void mSToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            speedMS = 1000;
        }

        private void mSToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            speedMS = 2000;
        }

        private void mSToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            speedMS = 3000;
        }
    }
}
