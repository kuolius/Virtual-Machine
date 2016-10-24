using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VM
{
    public partial class VMScreen : UserControl
    {
        private ushort mScreenMemoryLocation;
        private byte[] mScreenMemory;
        public ushort ScreenMemoryLocation { get; set; }
        public ushort cursor;
        public VMScreen()
        {
            InitializeComponent();
            mScreenMemoryLocation = 0xa000;
            mScreenMemory = new byte[4000];
            cursor = 0;
            for(int i=0;i<4000;i+=2)
            {
                mScreenMemory[i] = 32;
                mScreenMemory[i + 1] = 7;
            }
            SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint, true);
        }

        public void Poke(byte Value,ushort Address= 0xa000)
        {
            ushort MemLoc;
            try
            {
                MemLoc = (ushort)(Address - mScreenMemoryLocation + cursor);

            }
            catch(Exception)
            {
                return;
            }
            if (MemLoc < 0 || MemLoc > 3999)
                return;
            mScreenMemory[MemLoc] = Value;
            cursor++;
            Refresh();
        }

        public byte Peek(ushort Address)
        {
            ushort MemLoc;
            try
            {
                MemLoc = (ushort)(Address - mScreenMemoryLocation);
            }
            catch(Exception)
            {
                return (byte)0;
            }
            if (MemLoc < 0 || MemLoc > 3999)
                return (byte)0;
            return mScreenMemory[MemLoc];
        }

        

        private void VMScreen_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bmp = new Bitmap(this.Width, this.Height);
            Graphics bmpGraphics = Graphics.FromImage(bmp);
            Font f = new Font("Courier New", 10f, FontStyle.Bold);
            int xLoc = 0;
            int yLoc = 0;

            for (int i = 0; i < 4000; i += 2)
            {
                SolidBrush bgBrush = null;
                SolidBrush fgBrush = null;
                if ((mScreenMemory[i + 1] & 112) == 112)
                {
                    bgBrush = new SolidBrush(Color.Gray);
                }
                if ((mScreenMemory[i + 1] & 112) == 96)
                {
                    bgBrush = new SolidBrush(Color.Brown);
                }
                if ((mScreenMemory[i + 1] & 112) == 80)
                {
                    bgBrush = new SolidBrush(Color.Magenta);
                }
                if ((mScreenMemory[i + 1] & 112) == 64)
                {
                    bgBrush = new SolidBrush(Color.Red);
                }
                if ((mScreenMemory[i + 1] & 112) == 48)
                {
                    bgBrush = new SolidBrush(Color.Cyan);
                }
                if ((mScreenMemory[i + 1] & 112) == 32)
                {
                    bgBrush = new SolidBrush(Color.Green);
                }
                if ((mScreenMemory[i + 1] & 112) == 16)
                {
                    bgBrush = new SolidBrush(Color.Blue);
                }
                if ((mScreenMemory[i + 1] & 112) == 0)
                {
                    bgBrush = new SolidBrush(Color.Black);
                }

                if ((mScreenMemory[i + 1] & 7) == 7)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.White);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Gray);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 6)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.Yellow);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Brown);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 5)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.Fuchsia);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Magenta);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 4)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.Pink);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Red);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 0)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.Gray);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Black);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 1)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.LightBlue);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Blue);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 2)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.LightGreen);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Green);
                    }
                }
                if ((mScreenMemory[i + 1] & 7) == 3)
                {
                    if ((mScreenMemory[i + 1] & 8) == 8)
                    {
                        fgBrush = new SolidBrush(Color.LightCyan);
                    }
                    else
                    {
                        fgBrush = new SolidBrush(Color.Cyan);
                    }
                }
                if (bgBrush == null)
                    bgBrush = new SolidBrush(Color.Black);
                if (fgBrush == null)
                    fgBrush = new SolidBrush(Color.Gray);
                if ((xLoc % 800 == 0) && (xLoc != 0))
                {
                    yLoc += 11;
                    xLoc = 0;
                }
                string s = System.Text.Encoding.ASCII.GetString(mScreenMemory, i, 1);
                PointF pf = new PointF(xLoc, yLoc);
                bmpGraphics.FillRectangle(bgBrush, xLoc + 2, yLoc + 2, 8f, 11f);
                bmpGraphics.DrawString(s, f, fgBrush, pf);
                xLoc += 8;
            }
            e.Graphics.DrawImage(bmp, new Point(0, 0));
            bmpGraphics.Dispose();
            bmp.Dispose();
        }
    }
}
