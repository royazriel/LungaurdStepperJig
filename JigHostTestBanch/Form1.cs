using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;

namespace JigHostTestBanch
{

 
    public partial class Form1 : Form
    {
        SerialPort comPort = new SerialPort();
        const int HOME_OPCODE = 0xF8;
        const int START_OPCODE = 0xF9;
        const int STOP_OPCODE = 0xFA;
        const int NOP_OPCODE = 0xFB;
        const string version_num = "1.1.0";
        
        public Form1()
        {
            InitializeComponent();

            pic.Image = Bitmap.FromHicon(new Icon( Properties.Resources.red_light , new Size(32, 32)).Handle);

            string[] ports = SerialPort.GetPortNames();

            this.Text += " Ver:" + version_num;
            foreach (string port in ports)
            {
                cmbPorts.Items.Add(port);
            }

            cmbPorts.SelectedIndex = cmbPorts.FindString(Properties.Settings.Default.ComPort);

            BuildDataView();

        }

        private void DisplayDataTransfer( byte opcode )
        {
            byte[] data = new byte[1];
            try
            {
                comPort.Read(data, 0, 1);
                if (data[0] == opcode)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        pic.Image = Bitmap.FromHicon(new Icon(Properties.Resources.red_light, new Size(32, 32)).Handle);
                        pic.Refresh();
                        System.Threading.Thread.Sleep(30);
                        pic.Image = Bitmap.FromHicon(new Icon(Properties.Resources.green_light, new Size(32, 32)).Handle);
                        pic.Refresh();
                    }
                }
                else
                {
                    pic.Image = Bitmap.FromHicon(new Icon(Properties.Resources.red_light, new Size(32, 32)).Handle);
                    MessageBox.Show("Serial port " + comPort.PortName +
                      " port is opened, cannot connect with device", "JigHostTestBanch",
                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Serial port " + comPort.PortName +
                     " port is opened, device not repondeing", "JigHostTestBanch",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BuildDataView()
        {
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "Field Name";
            dgv.Columns[1].Name = "Filed Value";


            string[] row = new string[] { "Cycles 0.01-2[Hz]", Properties.Settings.Default.cycleHz.ToString() };
            dgv.Rows.Add(row);
            row = new string[] { "Duty Cycle 0-100[%]", Properties.Settings.Default.dutyCycle.ToString() };
            dgv.Rows.Add(row);
            row = new string[] { "Positive Degree {45° max }", Properties.Settings.Default.positiveDegree.ToString() };
            dgv.Rows.Add(row);
            row = new string[] { "Negative Degree{45° max }", Properties.Settings.Default.negativeDegree.ToString() };
            dgv.Rows.Add(row);
            row = new string[] { "Speed [rpm]", Properties.Settings.Default.speed.ToString() };
            dgv.Rows.Add(row);
            row = new string[] { "Homing Position [Deg]", Properties.Settings.Default.homeDegree.ToString() };
            dgv.Rows.Add(row);


            this.dgv.Columns[0].Width = 150;
            this.dgv.Columns[1].Width = 60;


            foreach (DataGridViewColumn column in dgv.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void cmbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comPort.IsOpen)
            {
                comPort.Close();
                pic.Image = Bitmap.FromHicon(new Icon(Properties.Resources.red_light, new Size(32, 32)).Handle);
            }
        }

        private void btnOpenCom_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[] {NOP_OPCODE };
            if (comPort.IsOpen) comPort.Close();
            comPort.PortName = cmbPorts.SelectedItem.ToString();

            // try to open the selected port:
            try
            {
                comPort.ReadTimeout = 100;
                comPort.Open();
                comPort.Write(data, 0, 1);
                data[0] = 0;
                comPort.Read( data,0, 1 );
                if (data[0] != NOP_OPCODE)
                {
                    MessageBox.Show("Serial port " + comPort.PortName +
                   " port is opened, cannot connect with device", "JigHostTestBanch",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    pic.Image = Bitmap.FromHicon(new Icon(Properties.Resources.green_light, new Size(32, 32)).Handle);
                }
                
            }
            // give a message, if the port is not available:
            catch
            {
                MessageBox.Show("Serial port " + comPort.PortName +
                   " cannot be opened!", "JigHostTestBanch",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool ValidateValues( float freq , float dc, int pos, int neg, int speed, int home)
        {
            if (freq < 0.01f || freq > 2.0f)
            {
                MessageBox.Show("frequency value is out of range ", "JigHostTestBanch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (dc < 1.0 || freq > 100.0f)
            {
                MessageBox.Show("duty cycle value is out of range ", "JigHostTestBanch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (pos < 1 || pos > 45)
            {
                MessageBox.Show("positive degree value is out of range ", "JigHostTestBanch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (neg < 1 || neg > 45)
            {
                MessageBox.Show("negative degree value is out of range ", "JigHostTestBanch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (speed < 1 || speed > 76)
            {
                MessageBox.Show("speed value is out of range ", "JigHostTestBanch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (home < 1 || home > 45)
            {
                MessageBox.Show("home degree value is out of range ", "JigHostTestBanch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[17];
            data[0] = START_OPCODE;

            float freq = float.Parse(dgv[1, 0].Value.ToString());
            float dc = float.Parse(dgv[1, 1].Value.ToString());
            int posDeg = short.Parse(dgv[1, 2].Value.ToString());
            int negDeg = short.Parse(dgv[1, 3].Value.ToString());
            int speed = short.Parse(dgv[1, 4].Value.ToString());
            int homingDeg = short.Parse(dgv[1, 5].Value.ToString());

            if (!ValidateValues(freq, dc, posDeg, negDeg, speed, homingDeg)) return;

            Buffer.BlockCopy(BitConverter.GetBytes(freq), 0, data, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(dc), 0, data, 5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(posDeg), 0, data, 9, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(negDeg), 0, data, 11, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(speed), 0, data, 13, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(homingDeg), 0, data, 15, 2);

            if (comPort.IsOpen)
            {
                comPort.Write(data, 0, data.Count());
                DisplayDataTransfer(data[0]);
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[17];
            data[0] = HOME_OPCODE;
            float freq = float.Parse(dgv[1, 0].Value.ToString());
            float dc = float.Parse(dgv[1, 1].Value.ToString());
            int posDeg = short.Parse(dgv[1, 2].Value.ToString());
            int negDeg = short.Parse(dgv[1, 3].Value.ToString());
            int speed = short.Parse(dgv[1, 4].Value.ToString());
            int homingDeg = short.Parse(dgv[1, 5].Value.ToString());

            if (!ValidateValues(freq, dc, posDeg, negDeg, speed, homingDeg)) return;

            Buffer.BlockCopy(BitConverter.GetBytes(freq), 0, data, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(dc), 0, data, 5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(posDeg), 0, data, 9, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(negDeg), 0, data, 11, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(speed), 0, data, 13, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(homingDeg), 0, data, 15, 2);

            if (comPort.IsOpen)
            {
                comPort.Write(data, 0, data.Count());
                DisplayDataTransfer(data[0]);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[17];
            data[0] = STOP_OPCODE;
            Buffer.BlockCopy(BitConverter.GetBytes(float.Parse(dgv[1, 0].Value.ToString())), 0, data, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(float.Parse(dgv[1, 1].Value.ToString())), 0, data, 5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 2].Value.ToString())), 0, data, 9, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 3].Value.ToString())), 0, data, 11, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 4].Value.ToString())), 0, data, 13, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 5].Value.ToString())), 0, data, 15, 2);

            if (comPort.IsOpen)
            {
                comPort.Write(data, 0, data.Count());
                DisplayDataTransfer(data[0]);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (comPort.IsOpen)
                comPort.Close();

            Properties.Settings.Default.ComPort = cmbPorts.SelectedText;
            Properties.Settings.Default.cycleHz = float.Parse(dgv[1, 0].Value.ToString());
            Properties.Settings.Default.dutyCycle = float.Parse(dgv[1, 1].Value.ToString());
            Properties.Settings.Default.positiveDegree = int.Parse(dgv[1, 2].Value.ToString());
            Properties.Settings.Default.negativeDegree = int.Parse(dgv[1, 3].Value.ToString());
            Properties.Settings.Default.speed = int.Parse(dgv[1, 4].Value.ToString());
            Properties.Settings.Default.homeDegree = int.Parse(dgv[1, 5].Value.ToString());
            Properties.Settings.Default.Save();
        }
    }
}
