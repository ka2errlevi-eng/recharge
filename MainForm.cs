using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace AlgeriaRechargeDesktop
{
    public partial class MainForm : Form
    {
        private SerialPort _port = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Algeria Mobile Recharge POS";
            this.Size = new System.Drawing.Size(480, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panels
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new System.Windows.Forms.Padding(10)
            };

            // COM Port
            var lblPort = new Label
            {
                Text = "COM Port:",
                Location = new System.Drawing.Point(10, 10),
                Width = 80
            };
            var cbPort = new ComboBox
            {
                Location = new System.Drawing.Point(140, 10),
                Width = 180,
                Name = "CbPort"
            };
            var btnLoadPorts = new Button
            {
                Text = "Load Ports",
                Location = new System.Drawing.Point(330, 10),
                Name = "BtnLoadPorts"
            };

            // Operator
            var lblOperator = new Label
            {
                Text = "Operator:",
                Location = new System.Drawing.Point(10, 50),
                Width = 80
            };
            var cbOperator = new ComboBox
            {
                Location = new System.Drawing.Point(140, 50),
                Width = 180,
                Name = "CbOperator"
            };

            // Phone or voucher
            var lblPhone = new Label
            {
                Text = "Phone / Voucher:",
                Location = new System.Drawing.Point(10, 90),
                Width = 120
            };
            var tbPhone = new TextBox
            {
                Location = new System.Drawing.Point(140, 90),
                Width = 180,
                Name = "TbPhone"
            };

            // Action buttons
            var btnRecharge = new Button
            {
                Text = "Recharge (Voucher)",
                Location = new System.Drawing.Point(10, 140),
                Width = 150,
                Name = "BtnRecharge"
            };
            var btnBalance = new Button
            {
                Text = "Check Balance",
                Location = new System.Drawing.Point(180, 140),
                Width = 150,
                Name = "BtnBalance"
            };

            // Result box
            var lblResult = new Label
            {
                Text = "Result:",
                Location = new System.Drawing.Point(10, 190),
                Width = 80
            };
            var tbResult = new TextBox
            {
                Location = new System.Drawing.Point(10, 210),
                Size = new System.Drawing.Size(440, 100),
                Multiline = true,
                ReadOnly = true,
                Name = "TbResult"
            };

            // Add controls
            mainPanel.Controls.AddRange(new Control[]
            {
                lblPort, cbPort, btnLoadPorts,
                lblOperator, cbOperator,
                lblPhone, tbPhone,
                btnRecharge, btnBalance,
                lblResult, tbResult
            });
            this.Controls.Add(mainPanel);

            // Events (minimal code–only version)
            btnLoadPorts.Click += (s, e) =>
            {
                var ports = SerialPort.GetPortNames();
                cbPort.Items.Clear();
                foreach (var p in ports) cbPort.Items.Add(p);
                if (ports.Length > 0 && cbPort.Items.Count > 0)
                    cbPort.SelectedIndex = 0;
                Log("COM ports loaded.");
            };

            cbOperator.Items.AddRange(new object[] { "Djezzy", "Ooredoo", "Mobilis" });
            if (cbOperator.Items.Count > 0)
                cbOperator.SelectedIndex = 0;

            btnRecharge.Click += (s, e) =>
            {
                var portName = cbPort.Text;
                var operatorName = cbOperator.Text;
                var voucherCode = tbPhone.Text.Trim();

                if (string.IsNullOrEmpty(portName) || string.IsNullOrEmpty(voucherCode))
                {
                    MessageBox.Show("COM Port and Voucher code are required.");
                    return;
                }

                // Djezzy / Ooredoo / Mobilis USSD
                string ussdCode = operatorName switch
                {
                    "Djezzy"  => $"*138*{voucherCode}#",
                    "Ooredoo" => $"*115*{voucherCode}#",
                    "Mobilis" => $"*111*{voucherCode}#",
                    _         => null
                };

                if (string.IsNullOrEmpty(ussdCode))
                {
                    MessageBox.Show("Invalid operator.");
                    return;
                }

                if (SendUssd(portName, ussdCode))
                {
                    Log($"Recharge sent: {ussdCode}");
                    MessageBox.Show("Recharge USSD sent successfully.");
                }
                else
                {
                    Log("Recharge failed.");
                    MessageBox.Show("Recharge failed. Check COM port or code.");
                }
            };

            btnBalance.Click += (s, e) =>
            {
                var portName = cbPort.Text;
                var operatorName = cbOperator.Text;

                if (string.IsNullOrEmpty(portName) || string.IsNullOrEmpty(operatorName))
                {
                    MessageBox.Show("COM Port and Operator are required.");
                    return;
                }

                string balanceCode = operatorName switch
                {
                    "Djezzy"  => "*710#",
                    "Ooredoo" => "*200#",
                    "Mobilis" => "*222#",
                    _         => null
                };

                if (string.IsNullOrEmpty(balanceCode))
                {
                    MessageBox.Show("Invalid operator.");
                    return;
                }

                if (SendUssd(portName, balanceCode))
                {
                    Log($"Balance check sent: {balanceCode}");
                    MessageBox.Show("Balance check USSD sent successfully.");
                }
                else
                {
                    Log("Balance check failed.");
                    MessageBox.Show("Balance check failed. Check COM port.");
                }
            };
        }

        private void Log(string msg)
        {
            var tb = GetControl<TextBox>("TbResult");
            tb.Text += $"{DateTime.Now:HH:mm:ss} - {msg}{Environment.NewLine}";
            tb.SelectionStart = tb.Text.Length;
            tb.ScrollToCaret();
        }

        private Control GetControl(Control root, string name)
        {
            if (root.Name == name) return root;
            foreach (Control c in root.Controls)
            {
                var found = GetControl(c, name);
                if (found != null) return found;
            }
            return null;
        }

        private T GetControl<T>(string name) where T : Control
        {
            return (T)GetControl(this.Controls[0], name);
        }

        private bool SendUssd(string portName, string code)
        {
            SerialPort port = null;
            try
            {
                port = new SerialPort
                {
                    PortName = portName,
                    BaudRate = 115200,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    Handshake = Handshake.None,
                    ReadTimeout = 5000,
                    WriteTimeout = 5000,
                    DtrEnable = true,
                    RtsEnable = true
                };

                port.Open();
                System.Threading.Thread.Sleep(500);

                // AT init
                if (!SendCommand(port, "AT", "OK")) goto ERROR;
                if (!SendCommand(port, "ATE0", "OK")) goto ERROR;

                // Send USSD
                if (!SendCommand(port, $"ATD{code};", "OK")) goto ERROR;

                port.Close();
                port.Dispose();
                return true;

            ERROR:
                Log($"Error sending USSD: {code}");
                port?.Close();
                port?.Dispose();
                return false;
            }
        }

        private bool SendCommand(SerialPort port, string cmd, string expected)
        {
            try
            {
                port.WriteLine(cmd);
                var response = ReadResponse(port);
                Log($"{cmd} -> {response}");
                return response.Contains(expected);
            }
            catch
            {
                return false;
            }
        }

        private string ReadResponse(SerialPort port)
        {
            var sb = new System.Text.StringBuilder();
            var buffer = new char[256];
            int timeout = 10;

            while (timeout > 0)
            {
                try
                {
                    int count = port.Read(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        sb.Append(new string(buffer, 0, count));
                        if (sb.ToString().Contains("OK") || sb.ToString().Contains("ERROR"))
                            break;
                    }
                }
                catch (System.IO.IOException) { break; }
                System.Threading.Thread.Sleep(200);
                timeout--;
            }

            return sb.ToString().Trim();
        }
    }
}
