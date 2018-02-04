using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Timers;

namespace SBUSDecoderCFG
{
    public partial class frmMain : Form
    {
        Dictionary<int, ComboBox> cbBankA = new Dictionary<int,ComboBox>();
        Dictionary<int, ComboBox> cbBankB = new Dictionary<int, ComboBox>();

        public int version;
        public static System.Timers.Timer aTimer;

        private void cbBanksInit()
        {
            cbBankA.Clear();
            cbBankA.Add(1, cbChA1);
            cbBankA.Add(2, cbChA2);
            cbBankA.Add(3, cbChA3);
            cbBankA.Add(4, cbChA4);
            cbBankA.Add(5, cbChA5);
            cbBankA.Add(6, cbChA6);
            cbBankA.Add(7, cbChA7);
            cbBankA.Add(8, cbChA8);
            cbBankA.Add(9, cbChA9);
            cbBankA.Add(10, cbChA10);
            cbBankA.Add(11, cbChA11);
            cbBankA.Add(12, cbChA12);
            cbBankA.Add(13, cbChA13);
            cbBankA.Add(14, cbChA14);
            cbBankA.Add(15, cbChA15);
            cbBankA.Add(16, cbChA16);

            cbBankB.Clear();
            cbBankB.Add(1, cbChB1);
            cbBankB.Add(2, cbChB2);
            cbBankB.Add(3, cbChB3);
            cbBankB.Add(4, cbChB4);
            cbBankB.Add(5, cbChB5);
            cbBankB.Add(6, cbChB6);
            cbBankB.Add(7, cbChB7);
            cbBankB.Add(8, cbChB8);
            cbBankB.Add(9, cbChB9);
            cbBankB.Add(10, cbChB10);
            cbBankB.Add(11, cbChB11);
            cbBankB.Add(12, cbChB12);
            cbBankB.Add(13, cbChB13);
            cbBankB.Add(14, cbChB14);
            cbBankB.Add(15, cbChB15);
            cbBankB.Add(16, cbChB16);

            SetBankDefaultCh(cbBankA);
            SetBankDefaultCh(cbBankB);
        }

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            cbBanksInit();
            cbPorts.Items.Clear();
            cbPorts.Items.AddRange(SerialPort.GetPortNames());
            if (cbPorts.Items.Count > 0) cbPorts.SelectedIndex = 0;

            if (!chkSW.Checked) ChkSwitches_Set();

            chkSW.Enabled = false;
            radioButton1.Checked = true;
            version = 2;
        }

        private int GetChannelsCount(int FrameIndex)
        {
            int ms = (FrameIndex * 4) + 16;
            double us = ms * 1000;
            // minus sync
            us -= 2700;
            // 2200us per channel
            int chc = Convert.ToInt32(Math.Floor(us / 2200));
            return chc;
        }

        void update_cbChannelsPPM(ComboBox cb, int FrameIndex, int maxCh)
        {
            int maxChannelsCount = GetChannelsCount(FrameIndex);
            if (maxChannelsCount > maxCh) maxChannelsCount = maxCh;
            cb.Items.Clear();
            for (int i = 1; i <= maxChannelsCount; i++)
                cb.Items.Add(i);
            // Default = maxChannelsCount
            cb.SelectedIndex = maxChannelsCount - 1;
        }

        void update_cbChannelsPWM(ComboBox cb, int FrameIndex, int maxCh)
        {
            int maxChannelsCount = GetChannelsCount(FrameIndex)+1;
            if (maxChannelsCount > maxCh) maxChannelsCount = maxCh;
            cb.Items.Clear();
            for (int i = 1; i <= maxChannelsCount; i++)
                cb.Items.Add(i);
            // Default = maxChannelsCount
            cb.SelectedIndex = maxChannelsCount - 1;
        }

        private void UpdateBankEnabled(Dictionary<int, ComboBox> Bank, int chCount)
        {
            foreach (KeyValuePair<int, ComboBox> itm in Bank)
                itm.Value.Enabled = itm.Key <= chCount;
        }

        private void SetBankDefaultCh(Dictionary<int, ComboBox> Bank)
        {
            foreach (KeyValuePair<int, ComboBox> itm in Bank)
                itm.Value.SelectedIndex = itm.Key-1;
        }

        private void chkPPMB_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkPPMB.Checked)
            {
                // Frame = 20ms
                cbFrameB.SelectedIndex = 1;
                // Channels <8
                update_cbChannelsPWM(cbChannelsB, cbFrameB.SelectedIndex, 8);

                if (radioButton2.Checked)
                    chkSW.Enabled = true;
                if (chkSW.Checked && !radioButton1.Checked)
                    groupBox4.Enabled = true;
            }
            else
            {
                // Frame = 24ms
                cbFrameB.SelectedIndex = 2;
                // Channels up to 16
                update_cbChannelsPPM(cbChannelsB, cbFrameB.SelectedIndex, 16);

                if (chkPPMA.Checked && radioButton2.Checked)
                    chkSW.Enabled = false;
                groupBox4.Enabled = false;
            }
            UpdateBankEnabled(cbBankB, cbChannelsB.Items.Count);
        }

        private void chkPPMA_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkPPMA.Checked)
            {
                // Frame = 20ms
                cbFrameA.SelectedIndex = 1;
                // Channels <8
                update_cbChannelsPWM(cbChannelsA, cbFrameA.SelectedIndex, 8);

                if (radioButton2.Checked)
                    chkSW.Enabled = true;
                if (chkSW.Checked && !radioButton1.Checked)
                    groupBox3.Enabled = true;
            }
            else
            {
                // Frame = 24ms
                cbFrameA.SelectedIndex = 2;
                // Channels up to 16
                update_cbChannelsPPM(cbChannelsA, cbFrameA.SelectedIndex, 16);

                if (chkPPMB.Checked && radioButton2.Checked)
                    chkSW.Enabled = false;
                groupBox3.Enabled = false;
            }
            UpdateBankEnabled(cbBankA, cbChannelsA.Items.Count);
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            /* Taken from Arduino file
                Packet:
                0 - start byte 0xaa
                1 - command (R - read / W - write)
                2 - PPM Bank A
                3 - Timer2 loops to Bank A
                4 - Channels bank A
                5:20 - channel map bank A
                21 - ppm bank B
                22 - Timer2 loops to Bank B
                23 - Channels bank B
                24:39 - channel map bank B
                40 - Switches enabled - new V3
                41 - Bank A switches - new V3
                42 - Bank B switches - new V3
                43 - check byte 0x5d
                */
            int buffsize;
            if (version == 2)
                buffsize = 41;
            else
                buffsize = 44;

            byte[] buff = new byte[buffsize];
            buff[0] = 0xaa;
            buff[1] = 0x52; // R

            for (int i = 2; i < buffsize-1; i++)
                buff[i] = 0;

            buff[buffsize-1] = 0x5d; // Check byte

            COMPort.Open();
            COMPort.Write(buff, 0, buffsize);

            // Tried to catch up the endless loops
            // Seems to be necessary due to slow COM
            MessageBox.Show("Start to read data", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
            int cnt = 0;
            while (cnt < buffsize-1)
            {
                if (COMPort.BytesToRead > buffsize - 1)
                    cnt = COMPort.Read(buff, 0, buffsize);
                if (cnt == 0)
                {
                    MessageBox.Show("Read Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    COMPort.Close();
                    return;
                }
            }
            COMPort.Close();

            chkPPMA.Checked = (buff[2] == 1);
            if (buff[3] - 4 < 7) cbFrameA.SelectedIndex = buff[3] - 4;
            if (buff[4] - 1 < 16) cbChannelsA.SelectedIndex = buff[4] - 1;

            if (buff[5] < 17) cbChA1.SelectedIndex = buff[5];
            if (buff[6] < 17) cbChA2.SelectedIndex = buff[6];
            if (buff[7] < 17) cbChA3.SelectedIndex = buff[7];
            if (buff[8] < 17) cbChA4.SelectedIndex = buff[8];
            if (buff[9] < 17) cbChA5.SelectedIndex = buff[9];
            if (buff[10] < 17) cbChA6.SelectedIndex = buff[10];
            if (buff[11] < 17) cbChA7.SelectedIndex = buff[11];
            if (buff[12] < 17) cbChA8.SelectedIndex = buff[12];
            if (buff[13] < 17) cbChA9.SelectedIndex = buff[13];
            if (buff[14] < 17) cbChA10.SelectedIndex = buff[14];
            if (buff[15] < 17) cbChA11.SelectedIndex = buff[15];
            if (buff[16] < 17) cbChA12.SelectedIndex = buff[16];
            if (buff[17] < 17) cbChA13.SelectedIndex = buff[17];
            if (buff[18] < 17) cbChA14.SelectedIndex = buff[18];
            if (buff[19] < 17) cbChA15.SelectedIndex = buff[19];
            if (buff[20] < 17) cbChA16.SelectedIndex = buff[20];

            chkPPMB.Checked = (buff[21] == 1);
            if (buff[22] - 4 < 7) cbFrameB.SelectedIndex = buff[22] - 4;
            if (buff[23] - 1 < 16) cbChannelsB.SelectedIndex = buff[23] - 1;

            if (buff[24] < 17) cbChB1.SelectedIndex = buff[24];
            if (buff[25] < 17) cbChB2.SelectedIndex = buff[25];
            if (buff[26] < 17) cbChB3.SelectedIndex = buff[26];
            if (buff[27] < 17) cbChB4.SelectedIndex = buff[27];
            if (buff[28] < 17) cbChB5.SelectedIndex = buff[28];
            if (buff[29] < 17) cbChB6.SelectedIndex = buff[29];
            if (buff[30] < 17) cbChB7.SelectedIndex = buff[30];
            if (buff[31] < 17) cbChB8.SelectedIndex = buff[31];
            if (buff[32] < 17) cbChB9.SelectedIndex = buff[32];
            if (buff[33] < 17) cbChB10.SelectedIndex = buff[33];
            if (buff[34] < 17) cbChB11.SelectedIndex = buff[34];
            if (buff[35] < 17) cbChB12.SelectedIndex = buff[35];
            if (buff[36] < 17) cbChB13.SelectedIndex = buff[36];
            if (buff[37] < 17) cbChB14.SelectedIndex = buff[37];
            if (buff[38] < 17) cbChB15.SelectedIndex = buff[38];
            if (buff[39] < 17) cbChB16.SelectedIndex = buff[39];

            if (version == 3)
            {
                chkSW.Checked = (buff[40] == 1);
                chkCH8.Checked = Convert.ToBoolean((buff[41] & 1 << 7) >> 7);
                chkCH7.Checked = Convert.ToBoolean((buff[41] & 1 << 6) >> 6);
                chkCH6.Checked = Convert.ToBoolean((buff[41] & 1 << 5) >> 5);
                chkCH5.Checked = Convert.ToBoolean((buff[41] & 1 << 4) >> 4);
                chkCH4.Checked = Convert.ToBoolean((buff[41] & 1 << 3) >> 3);
                chkCH3.Checked = Convert.ToBoolean((buff[41] & 1 << 2) >> 2);
                chkCH2.Checked = Convert.ToBoolean((buff[41] & 1 << 1) >> 1);
                chkCH1.Checked = Convert.ToBoolean((buff[41] & 1 << 0) >> 0);

                chkCH16.Checked = Convert.ToBoolean((buff[42] & 1 << 7) >> 7);
                chkCH15.Checked = Convert.ToBoolean((buff[42] & 1 << 6) >> 6);
                chkCH14.Checked = Convert.ToBoolean((buff[42] & 1 << 5) >> 5);
                chkCH13.Checked = Convert.ToBoolean((buff[42] & 1 << 4) >> 4);
                chkCH12.Checked = Convert.ToBoolean((buff[42] & 1 << 3) >> 3);
                chkCH11.Checked = Convert.ToBoolean((buff[42] & 1 << 2) >> 2);
                chkCH10.Checked = Convert.ToBoolean((buff[42] & 1 << 1) >> 1);
                chkCH9.Checked = Convert.ToBoolean((buff[42] & 1 << 0) >> 0);
            }
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            int buffsize;
            if (version == 2)
                buffsize = 41;
            else
                buffsize = 44;

            byte[] buff = new byte[buffsize];
            buff[0] = 0xaa;
            buff[1] = 0x57; // W
            buff[2] = Convert.ToByte(chkPPMA.Checked);
            buff[3] = (byte)(cbFrameA.SelectedIndex + 4);
            buff[4] = (byte)(cbChannelsA.SelectedIndex + 1);

            buff[5] = (byte)(cbChA1.SelectedIndex);
            buff[6] = (byte)(cbChA2.SelectedIndex);
            buff[7] = (byte)(cbChA3.SelectedIndex);
            buff[8] = (byte)(cbChA4.SelectedIndex);
            buff[9] = (byte)(cbChA5.SelectedIndex);
            buff[10] = (byte)(cbChA6.SelectedIndex);
            buff[11] = (byte)(cbChA7.SelectedIndex);
            buff[12] = (byte)(cbChA8.SelectedIndex);
            buff[13] = (byte)(cbChA9.SelectedIndex);
            buff[14] = (byte)(cbChA10.SelectedIndex);
            buff[15] = (byte)(cbChA11.SelectedIndex);
            buff[16] = (byte)(cbChA12.SelectedIndex);
            buff[17] = (byte)(cbChA13.SelectedIndex);
            buff[18] = (byte)(cbChA14.SelectedIndex);
            buff[19] = (byte)(cbChA15.SelectedIndex);
            buff[20] = (byte)(cbChA16.SelectedIndex);

            buff[21] = Convert.ToByte(chkPPMB.Checked);
            buff[22] = (byte)(cbFrameB.SelectedIndex + 4);
            buff[23] = (byte)(cbChannelsB.SelectedIndex + 1);

            buff[24] = (byte)(cbChB1.SelectedIndex);
            buff[25] = (byte)(cbChB2.SelectedIndex);
            buff[26] = (byte)(cbChB3.SelectedIndex);
            buff[27] = (byte)(cbChB4.SelectedIndex);
            buff[28] = (byte)(cbChB5.SelectedIndex);
            buff[29] = (byte)(cbChB6.SelectedIndex);
            buff[30] = (byte)(cbChB7.SelectedIndex);
            buff[31] = (byte)(cbChB8.SelectedIndex);
            buff[32] = (byte)(cbChB9.SelectedIndex);
            buff[33] = (byte)(cbChB10.SelectedIndex);
            buff[34] = (byte)(cbChB11.SelectedIndex);
            buff[35] = (byte)(cbChB12.SelectedIndex);
            buff[36] = (byte)(cbChB13.SelectedIndex);
            buff[37] = (byte)(cbChB14.SelectedIndex);
            buff[38] = (byte)(cbChB15.SelectedIndex);
            buff[39] = (byte)(cbChB16.SelectedIndex);

            if (version == 3)
            {
                buff[40] = Convert.ToByte(chkSW.Checked);
                int conversion = 0;
                conversion = Convert.ToInt16(chkCH1.Checked) << 0;
                conversion |= Convert.ToInt16(chkCH2.Checked) << 1;
                conversion |= Convert.ToInt16(chkCH3.Checked) << 2;
                conversion |= Convert.ToInt16(chkCH4.Checked) << 3;
                conversion |= Convert.ToInt16(chkCH5.Checked) << 4;
                conversion |= Convert.ToInt16(chkCH6.Checked) << 5;
                conversion |= Convert.ToInt16(chkCH7.Checked) << 6;
                conversion |= Convert.ToInt16(chkCH8.Checked = false) << 7; // On pin D9 seems to be OCR1A
                buff[41] = Convert.ToByte(conversion);                      // servo operation is ok
                conversion = 0;                                             // but no switching possible
                conversion |= Convert.ToInt16(chkCH9.Checked) << 0;         // CH9 (D10) with OCR1B seems be to ok?!
                conversion |= Convert.ToInt16(chkCH10.Checked) << 1;
                conversion |= Convert.ToInt16(chkCH11.Checked) << 2;
                conversion |= Convert.ToInt16(chkCH12.Checked) << 3;
                conversion |= Convert.ToInt16(chkCH13.Checked) << 4;
                conversion |= Convert.ToInt16(chkCH14.Checked) << 5;
                conversion |= Convert.ToInt16(chkCH15.Checked) << 6;
                conversion |= Convert.ToInt16(chkCH16.Checked) << 7;
                buff[42] = Convert.ToByte(conversion);
            }

            buff[buffsize-1] = 0x5d;

            COMPort.Open();
            COMPort.Write(buff, 0, buffsize);
            COMPort.Close();

//            MessageBox.Show("Data written", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void cbFrameA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!chkPPMA.Checked)
            {
                // Channels <8
                update_cbChannelsPWM(cbChannelsA, cbFrameA.SelectedIndex, 8);
            }
            else
            {
                // Channels up to 16
                update_cbChannelsPPM(cbChannelsA, cbFrameA.SelectedIndex, 16);
            }
            UpdateBankEnabled(cbBankA, cbChannelsA.Items.Count);
        }

        private void cbFrameB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!chkPPMB.Checked)
            {
                // Channels <8
                update_cbChannelsPWM(cbChannelsB, cbFrameB.SelectedIndex, 8);
            }
            else
            {
                // Channels up to 16
                update_cbChannelsPPM(cbChannelsB, cbFrameB.SelectedIndex, 16);
            }
            UpdateBankEnabled(cbBankB, cbChannelsB.Items.Count);
        }

        private void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            COMPort.PortName = cbPorts.SelectedItem.ToString();
        }

        private void chkSW_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkPPMA.Checked || !chkPPMB.Checked)
                ChkSwitches_Set();
        }

        private void ChkSwitches_Set()
        {
            if (!chkSW.Checked)
            {
                groupBox3.Enabled = false;
                groupBox4.Enabled = false;
            }
            else
            {
                if (!chkPPMA.Checked)
                    groupBox3.Enabled = true;
                if (!chkPPMB.Checked)
                    groupBox4.Enabled = true;
            }
        }

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            radioButton1.Checked = true;
            chkPPMA.Checked = true;
            chkPPMB.Checked = true;
            chkPPMA.Checked = false;
            chkPPMB.Checked = false;
            cbChA1.SelectedIndex = 0;
            cbChA2.SelectedIndex = 1;
            cbChA3.SelectedIndex = 2;
            cbChA4.SelectedIndex = 3;
            cbChA5.SelectedIndex = 4;
            cbChA6.SelectedIndex = 5;
            cbChA7.SelectedIndex = 6;
            cbChA8.SelectedIndex = 7;
            cbChB1.SelectedIndex = 8;
            cbChB2.SelectedIndex = 9;
            cbChB3.SelectedIndex = 10;
            cbChB4.SelectedIndex = 11;
            cbChB5.SelectedIndex = 12;
            cbChB6.SelectedIndex = 13;
            cbChB7.SelectedIndex = 14;
            cbChB8.SelectedIndex = 15;
            chkSW.Enabled = false;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            version = 2;
            chkSW.Enabled = false;
            groupBox3.Enabled = false;
            groupBox4.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            version = 3;
            if ((chkPPMA.Checked ^ chkPPMB.Checked) || (!chkPPMA.Checked && !chkPPMB.Checked))
                chkSW.Enabled = true;
            if (chkSW.Checked)
            {
                if (!chkPPMA.Checked)
                    groupBox3.Enabled = true;
                if (!chkPPMB.Checked)
                    groupBox4.Enabled = true;
            }
        }
    }
}
