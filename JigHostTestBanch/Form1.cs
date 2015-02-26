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
        
        public Form1()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                cmbPorts.Items.Add(port);
            }

            if (ports.Count() > 0) cmbPorts.SelectedIndex = 0;
            BuildDataView();
        }

        private void BuildDataView()
        {
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "Field Name";
            dgv.Columns[1].Name = "Filed Value";


            string[] row = new string[] { "Cycles 0.01-2[Hz]", "1" };
            dgv.Rows.Add(row);
            row = new string[] { "Duty Cycle 0-100[%]", "50" };
            dgv.Rows.Add(row);
            row = new string[] { "Positive Degree {45° max }", "45" };
            dgv.Rows.Add(row);
            row = new string[] { "Negative Degree{45° max }","45" };
            dgv.Rows.Add(row);
            row = new string[] { "Speed [rpm]", "76" };
            dgv.Rows.Add(row);
            row = new string[] { "Homing Position [Deg]", "20" };
            dgv.Rows.Add(row);


            this.dgv.Columns[0].Width = 150;
            this.dgv.Columns[1].Width = 70;
        }

        private void cmbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnOpenCom_Click(object sender, EventArgs e)
        {
            if (comPort.IsOpen) comPort.Close();
            comPort.PortName = cmbPorts.SelectedItem.ToString();

            // try to open the selected port:
            try
            {
                comPort.Open();
            }
            // give a message, if the port is not available:
            catch
            {
                MessageBox.Show("Serial port " + comPort.PortName +
                   " cannot be opened!", "RS232 tester",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[17];
            data[0] = START_OPCODE;
            Buffer.BlockCopy(BitConverter.GetBytes(float.Parse(dgv[1, 0].Value.ToString())), 0, data, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(float.Parse(dgv[1, 1].Value.ToString())), 0, data, 5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 2].Value.ToString())), 0, data, 9, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 3].Value.ToString())), 0, data, 11, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 4].Value.ToString())), 0, data, 13, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 5].Value.ToString())), 0, data, 15, 2);

            if (comPort.IsOpen)
            {
                comPort.Write(data, 0, data.Count());
            }
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[17];
            data[0] = HOME_OPCODE;
            Buffer.BlockCopy(BitConverter.GetBytes(float.Parse(dgv[1, 0].Value.ToString())), 0, data, 1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(float.Parse(dgv[1, 1].Value.ToString())), 0, data, 5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 2].Value.ToString())), 0, data, 9, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 3].Value.ToString())), 0, data, 11, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 4].Value.ToString())), 0, data, 13, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(short.Parse(dgv[1, 5].Value.ToString())), 0, data, 15, 2);

            if (comPort.IsOpen)
            {
                comPort.Write(data, 0, data.Count());
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
            }
        }
    }
}
