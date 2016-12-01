using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HammingCode
{
    public partial class View : Form
    {
        public View()
        {
            InitializeComponent();
            this.button1.Enabled = false;
            this.button2.Enabled = false;
        }



        private void pictureBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            if (openDialog.ShowDialog() == DialogResult.OK)
                this.textBox_Arhive_From.Text = openDialog.FileName;

            if (String.IsNullOrEmpty(this.textBox_Arhive_From.Text.Trim()) || String.IsNullOrEmpty(this.textBox_Arhive_To.Text.Trim()))
                this.button1.Enabled = false;
            else
                this.button1.Enabled = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            if (saveDialog.ShowDialog() == DialogResult.OK)
                this.textBox_Arhive_To.Text = saveDialog.FileName;

            if (String.IsNullOrEmpty(this.textBox_Arhive_From.Text.Trim()) || String.IsNullOrEmpty(this.textBox_Arhive_To.Text.Trim()))
                this.button1.Enabled = false;
            else
                this.button1.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            FileStream stream = File.OpenRead(this.textBox_Arhive_From.Text.Trim());
            if (File.Exists(this.textBox_Arhive_To.Text.Trim()))
                File.Delete(this.textBox_Arhive_To.Text.Trim());
            FileStream streamOutput = File.OpenWrite(this.textBox_Arhive_To.Text.Trim());
            streamOutput.Seek(1, SeekOrigin.Begin);

            byte byteForWrite = 0;
            int counter = -1;
            while (true)
            {
                int readed = stream.ReadByte();
                if (readed == -1)
                    break;
                byte temp = (byte)readed;
                int forWrite = 0;


                InsertControlBits(ref forWrite, temp);
                CountControlBits(ref forWrite);

                for (int i = 0; i < 12; i++)
                {
                    counter++;
                    int index = forWrite & 1 << 11 - i;
                    if (index > 0)
                        byteForWrite = (byte)(byteForWrite | 1 << 7 - counter);
                    if (counter == 7)
                    {
                        streamOutput.WriteByte(byteForWrite);
                        byteForWrite = 0;
                        counter = -1;
                    }
                }
            }
            if (counter > 0)
                streamOutput.WriteByte(byteForWrite);

            byte numberOfZerous = GetNumberOfZerous(byteForWrite);
            streamOutput.Seek(0, SeekOrigin.Begin);
            streamOutput.WriteByte(numberOfZerous);

            stream.Close();
            streamOutput.Close();
            MessageBox.Show("Finished\nTime: " + (DateTime.Now - now).ToString());
        }


        private void InsertControlBits(ref int forWrite, byte temp)
        {
            byte countOfPow = 0;
            int countForByte = 7;

            for (int i = 0; i < 12; i++)
            {
                double index = ((double)(i + 1)) / Math.Pow(2, countOfPow);
                if (index == 1)
                {
                    countOfPow++;
                    continue;
                }
                else
                {
                    int bitOfByte = temp & 1 << countForByte;
                    countForByte--;
                    if (bitOfByte > 0)
                        forWrite = forWrite | 1 << 11 - i;
                    else
                        forWrite = forWrite | 0 << 11 - i;
                }
            }
        }
        private void CountControlBits(ref int forWrite)
        {
            int step = 1;
            for (int k = 1; k <= 12; k *= 2)
            {
                int sum = 0;
                for (int i = step - 1; i < 12; i += step * 2)
                {
                    for (int j = 0; j < step; j++)
                    {
                        if (i + j > 11) break;
                        int value = forWrite & 1 << 11 - (i + j);
                        if (value > 0)
                            sum++;
                    }
                }
                if (sum % 2 == 1)
                    forWrite = forWrite | 1 << 12 - k;
                step *= 2;
            }
        }

        private byte GetNumberOfZerous(byte byteForWrite)
        {
            byte count = 0;

            for (int i = 0; i < 8; i++)
            {
                int index = byteForWrite & 1 << i;
                if (index == 0)
                    count++;
                else
                    break;
            }
            return count;
        }



        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

        private void button2_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            FileStream stream = File.OpenRead(this.textBox1.Text.Trim());
            if (File.Exists(this.textBox2.Text.Trim()))
                File.Delete(this.textBox2.Text.Trim());
            if (File.Exists(this.textBox3.Text.Trim()))
                File.Delete(this.textBox3.Text.Trim());

            FileStream streamOutput = File.OpenWrite(this.textBox3.Text.Trim());
            FileStream streamWithError = File.OpenWrite(this.textBox2.Text.Trim());

            int posMineError = Convert.ToInt32(this.comboBox1.Text.ToString());
            int numberOfZerous = stream.ReadByte();
            long sizeOfFile = stream.Length;

            int counterForSize = 1;
            int codedByte = 0;
            int k = 0;
            while (true)
            {
                int index = stream.ReadByte();
                if (index == -1)
                    break;
                byte temp = (byte)index;
                counterForSize++;

                int countTo;
                if (sizeOfFile - counterForSize == 0)
                {
                    if (numberOfZerous == 8)
                        countTo = 8;
                    else
                        countTo = 8 - numberOfZerous;
                }
                else
                    countTo = 8;

                for (int i = 0; i < countTo; i++)
                {
                    int code = temp & 1 << 7 - i;
                    if (code > 0)
                        codedByte = codedByte | 1 << 11 - k;

                    k++;
                    if (k == 12)
                    {
                        DoError(ref codedByte, posMineError);
                        byte errorByte = GetInitialByte(codedByte);

                        List<int> controlBits = GetControlBits(codedByte);
                        int positionOfError = HasError(controlBits, codedByte);
                        if (positionOfError >= 0)
                            codedByte = codedByte ^ 1 << 11 - positionOfError;

                        byte tempB = GetInitialByte(codedByte);
                        

                        streamOutput.WriteByte(tempB);
                        streamWithError.WriteByte(errorByte);
                        k = 0;
                        codedByte = 0;
                    }
                }
            }
            stream.Close();
            streamOutput.Close();
            streamWithError.Close();
            MessageBox.Show("Finished\nTime: " + (DateTime.Now - now).ToString());
        }

        private void DoError(ref int codedByte, int posMineError)
        {
            int count = 0;
            int i = 2;
            for (; i< 12;i++)
            {
                if (i == 3 || i == 7)
                    continue;
                count++;
                if (count == posMineError)
                    break;
            }
            codedByte = codedByte ^ 1 << 11 - i;
            
        }

        

        private List<int> GetControlBits(int codedByte)
        {
            List<int> controlBits = new List<int>();


            for (int step = 1; step < 5; step++)
            {
                int counterForPow = step - 1;
                int sum = 0;

                for (int i = (int)(Math.Pow(2, counterForPow) - 1); i < 12; i += (int)Math.Pow(2, step))
                {
                    for (int j = 0; j < Math.Pow(2, step - 1); j++)
                    {
                        if (i + j > 11)
                            break;
                        double index = ((double)(i + 1 + j)) / Math.Pow(2, counterForPow);
                        if (index == 1)
                        {
                            counterForPow++;
                            continue;
                        }

                        index = codedByte & 1 << 11 - i - j;
                        if (index > 0)
                            sum++;
                    }
                }
                if (sum % 2 == 1)
                    controlBits.Add(1);
                else
                    controlBits.Add(0);

            }
            return controlBits;
        }

        private int HasError(List<int> controlBits, int codedByte)
        {
            int position = 0;
            int counter = 0;
            for (int i = 1; i <= 12; i *= 2)
            {
                int index = codedByte & 1 << 12 - i;
                if ((index > 0 && controlBits[counter] > 0) || index == controlBits[counter])
                    ;
                else
                    position += i;
                counter++;
            }
            return position - 1;
        }        
        
        private byte GetInitialByte(int codedByte)
        {
            int result = 0;
            int k = 0;
            for (int i = 2; i < 12; i++)
            {
                if (i == 3 || i == 7)
                    continue;
                int index = codedByte & 1 << 11 - i;
                if (index > 0)
                    result = result | 1 << 7 - k;
                k++;
            }
            return (byte)result;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            OpenFileDialog opefFile = new OpenFileDialog();
            if (opefFile.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = opefFile.FileName;
                this.textBox2.Text = opefFile.FileName;
                this.textBox3.Text = opefFile.FileName;
            }

            if (String.IsNullOrEmpty(this.textBox1.Text.Trim()) || String.IsNullOrEmpty(this.textBox2.Text.Trim()) || String.IsNullOrEmpty(this.textBox3.Text.Trim()))
                this.button2.Enabled = false;
            else
                this.button2.Enabled = true;
        }

        

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.textBox1.Text.Trim()) || String.IsNullOrEmpty(this.textBox2.Text.Trim()) || String.IsNullOrEmpty(this.textBox3.Text.Trim()))
                this.button2.Enabled = false;
            else
                this.button2.Enabled = true;
        }
    }
}
