using System;
using System.Linq;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Terminal;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Text;
using System.Net.Mail;
using Guna.UI2.WinForms;
using EasyModbus;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Diagnostics;
using ModbusRTU;
using System.Globalization;
using System.Web.UI.WebControls.WebParts;
using EasyModbus.Exceptions;
using static EasyModbus.ModbusServer;
using System.Xml.Linq;
using System.Management;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.WinForms;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using System.Windows.Markup;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;



namespace WATER_QUALITY_MONITORING
{
    public partial class SWM : Form
    {
        public SWM()
        {
            InitializeComponent();

            WindowState = FormWindowState.Maximized;

            rtuSlave = new ModbusServer();

            configItem();

            readTXT();

            readAlarm();

            configMB();

            configSlave();

            configLog();

            txtPassword.UseSystemPasswordChar = true;
            txtPassword2.UseSystemPasswordChar = true;

            btnCOM1.Enabled = false;
            btnCOM2.Enabled = false;
            btnID.Enabled = false;
            setCSV.Enabled = false;
            btnConfigDTU.Enabled = false;
            btnReboot.Enabled = false;

            Load += YourForm_Load;

            FormClosing += YourForm_FormClosing;

            BackAll();
        }

        string userAdmin = "admin";
        string passAdmin = "12345";

        int loc = 65139;

        private string filePath = "config.dat";
        string[] defaultValues = {
                    "Port1 = COM9",
                    "Port2 = COM7",
                    "Baudrate1 = 9600",
                    "Baudrate2 = 9600",
                    "Stopbits1 = 1",
                    "Stopbits2 = 1",
                    "Parity1 = None",
                    "Parity2 = None",
                    "Interval1 = 2000",

                    "interval = 60",
                    "Units = Seconds",

                    "ID = 1",
                    "idph = 10",
                    "idtemp = 10",
                    "idfcl = 13",
                    "idtur = 14",

                    "cph = 6.88",
                    "cb1ph = 7.6",
                    "cb2ph = 4.01",
                    "rv1 = 6,88",
                    "cv1 = 6,88", //hellow

                    "ctemp = 28.40",
                    "cb1temp = 28.40",
                    "cb2temp = 50",
                    "rv2 = 28,40",
                    "cv2 = 28,40", //hellow

                    "cfcl = 0.25",
                    "cb1fcl = 0.017",
                    "cb2fcl = 2",
                    "rv3 = 0,15",
                    "cv3 = 0,15", //hellow

                    "ctur = 0.28",
                    "cb1tur = 0.28",
                    "cb2tur = 10",
                    "rv4 = 0,18",
                    "cv4 = 0,18", //hellow

                    "first = true",
                    
                    "coe1 = 1.0",
                    "max1 = 14",
                    "min1 = 0",
                    "off1 = 0.0",

                    "coe2 = 1.0",
                    "max2 = 100",
                    "min2 = 0",
                    "off2 = 0.0",

                    "coe3 = 1.0",
                    "max3 = 20",
                    "min3 = 0",
                    "off3 = 0.0",

                    "coe4 = 1.0",
                    "max4 = 20",
                    "min4 = 0",
                    "off4 = 0.0",
                };

        private async void YourForm_Load(object sender, EventArgs e)
        {   

            pgLoading.Show();
            pgLoading.BringToFront();

            timer.Interval = 1000;
            timer.Tick += getTime;
            timer.Start();

            DTUconnection();

            connectionTime.Interval = 20000;
            connectionTime.Start();

            req = new Thread(new ThreadStart(requestSensor));

            req.Start();

            //updateTabel();

            await DelayAsync(1000);
            Console.WriteLine("done loading");

            pgLoading.SendToBack();
            pgLoading.Hide();

            pgMain.BringToFront();
            pgMain.Show();

            delay_connect();

            

        }

        private void YourForm_FormClosing(object sender, EventArgs e)
        {
            request = false;

            rtuMaster.Disconnect();

            displayTime.Stop();

        }


        private Task DelayAsync(int millisecondsDelay)
        {
            return Task.Delay(millisecondsDelay);
        }


        #region ==================================|  INTERFACE ACTION  |===============================

        bool DTUconnected = false;

        private void connectionTime_Tick(object sender, EventArgs e)
        {
            DTUconnection();
        }

        private void configItem()
        {
            slcPort1.MaxDropDownItems = 5;
            slcPort2.MaxDropDownItems = 5;            
        }


        private async void delay_connect()
        {
            Image disc = Properties.Resources.icons8_disconnected_50;
            Image conn = Properties.Resources.icons8_connected_50;

            Image discw = Properties.Resources.icons8_thin_client_100;
            Image connw = Properties.Resources.icons8_thin_client_100__1_;

            await DelayAsync(1000);
            /*
            infoConnect.Image = disc;
            infoWifi.Image = discw;*/

            

            //infoWifi.Image = connw;
        }

        bool server_connnected()
        {
            Image discw = Properties.Resources.icons8_thin_client_100;
            Image connw = Properties.Resources.icons8_thin_client_100__1_;

            infoWifi.Image = connw;

            dtu_connnected();

            return true;
        }

        bool server_disconnnected()
        {
            Image discw = Properties.Resources.icons8_thin_client_100;
            Image connw = Properties.Resources.icons8_thin_client_100__1_;

            infoWifi.Image = discw;

            return false;
        }

        bool dtu_connnected()
        {
            Image disc = Properties.Resources.icons8_delete_link_100__1_;
            Image conn = Properties.Resources.icons8_link_100__1_;

            infoConnect.Image = conn;

            return true;
        }

        bool dtu_disconnnected()
        {
            Image disc = Properties.Resources.icons8_delete_link_100__1_;
            Image conn = Properties.Resources.icons8_link_100__1_;

            infoConnect.Image = disc;

            server_disconnnected();

            return false;
        }

        private void DTUconnection()
        {
            Image disc = Properties.Resources.icons8_delete_link_100__1_;
            Image conn = Properties.Resources.icons8_link_100__1_;

            term = new terminal(slcPort2.Text, int.Parse(slcBaudrate2.Text));

            term.Port = slcPort2.Text;
            term.Baudrate = int.Parse(slcBaudrate2.Text);
            term.Parity = Parity.None;
            term.StopBits = StopBits.One;

            if (!DTUused)
            {
               /* term.Open();
                term.Write("+++", 1000);
                txtWriteBottom(term.dataSender);

                if (!term.receivedData.Contains("No data") && !term.receivedData.Contains("Error"))
                {
                    DTUconnected = true;
                    infoConnect.Image = conn;
                }
                else
                {
                    DTUconnected = false;
                    infoConnect.Image = disc;
                }

                txtWriteBottom("DTU : " + term.receivedData);

                term.Close();*/
            }
            
            
        }




        private void txtWriteBottom(string newText, Guna2TextBox txt = null)
        {
            // If txt is not provided, use txtInfo as the default TextBox
            if (txt == null)
            {
                // Assuming txtInfo is the name of your default TextBox control
                txt = txtInfo;
            }

            if (txt != null)
            {
                if (txt.InvokeRequired)
                {
                    // Invoke the method on the UI thread
                    txt.Invoke(new Action(() =>
                    {
                        // Check if the control is disposed or disposing
                        if (!txt.IsDisposed && !txt.Disposing)
                        {
                            txt.AppendText(newText + Environment.NewLine);
                            txt.SelectionStart = txt.Text.Length;
                            txt.ScrollToCaret();
                        }
                    }));
                }
                else
                {
                    // Update the UI directly
                    if (!txt.IsDisposed && !txt.Disposing)
                    {
                        txt.AppendText(newText + Environment.NewLine);
                        txt.SelectionStart = txt.Text.Length;
                        txt.ScrollToCaret();
                    }
                }
            }
        }



        private void btnLoginSensor_Click(object sender, EventArgs e)
        {
            if (txtUsername2.Text == userAdmin && txtPassword2.Text == passAdmin)
            {
                txtWrongPass2.Text = "     ";

                pgLoginSensor.Hide();

                txtUsername2.Text = "";
                txtPassword2.Text = "";

                if (pageSensor == 1) { pgSpH.Show();  loginIn = true; }
                if (pageSensor == 2) { pgSTemp.Show(); loginIn = true; }
                if (pageSensor == 3) { pgSFcl.Show(); loginIn = true; }
                if (pageSensor == 4) { pgStur.Show(); loginIn = true; }

                LOCK();
            }
            else
            {
                txtWrongPass.Text = "User not registered!";
            }
        }


        private void BSEN_Click_1(object sender, EventArgs e)
        {
            if (pageSensor == 1) { pgLoginSensor.Hide(); pgSpH.Show(); loginIn = false; }
            if (pageSensor == 2) { pgLoginSensor.Hide(); pgSTemp.Show(); loginIn = false; }
            if (pageSensor == 3) { pgLoginSensor.Hide(); pgSFcl.Show(); loginIn = false; }
            if (pageSensor == 4) { pgLoginSensor.Hide(); pgStur.Show(); loginIn = false; }
        }


        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtUsername.Text == userAdmin  && txtPassword.Text == passAdmin)
            {
                txtWrongPass.Text = "     ";

                pgLogin.Hide();

                txtUsername.Text = "";
                txtPassword.Text = "";

                pgSetting.Show();
            }
            else
            {
                txtWrongPass.Text = "User not registered!";
            }
        }

        private void OnlyNumeric_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the pressed key is a digit or a control key (e.g., Backspace)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                // If the pressed key is not a digit, set the handled property to true
                e.Handled = true;
            }
        }

        private void OnlyNumericandPoint_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, backspace, and a single decimal point
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

        }

        private void btnHidePass_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
        }


        private void btnHidePass2_Click(object sender, EventArgs e)
        {
            txtPassword2.UseSystemPasswordChar = !txtPassword2.UseSystemPasswordChar;
        }

        private void HOME_Click(object sender, EventArgs e)
        {
            BackAll();
            pgMain.Show();
            pgMain.BringToFront();

            loginIn = false;

            IDph.Text = phid;
            IDtemp.Text = tempid;
            IDfcl.Text = fclid;
            IDtur.Text = turid;

            coeph.Text = Coe1.ToString(); 
            maxph.Text = Max1.ToString();
            minph.Text = Min1.ToString();
            offph.Text = Off1.ToString();

            coetemp.Text = Coe2.ToString();
            maxtemp.Text = Max2.ToString();
            mintemp.Text = Min2.ToString();
            offtemp.Text = Off2.ToString();

            coefcl.Text = Coe3.ToString();
            maxfcl.Text = Max3.ToString();
            minfcl.Text = Min3.ToString();
            offfcl.Text = Off3.ToString();

            coetur.Text = Coe4.ToString();
            maxtur.Text = Max4.ToString();
            mintur.Text = Min4.ToString();
            offtur.Text = Off4.ToString();


        }

        private void BC_Click(object sender, EventArgs e)
        {
            BackAll();
            pgCurve.Show();
            pgCurve.BringToFront();
        }
        private void BS_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSetup.Show();
            pgSetup.BringToFront();

            loginIn = false;

            IDph.Text = phid;
            IDtemp.Text = tempid;
            IDfcl.Text = fclid;
            IDtur.Text = turid;

            coeph.Text = Coe1.ToString();
            maxph.Text = Max1.ToString();
            minph.Text = Min1.ToString();
            offph.Text = Off1.ToString();

            coetemp.Text = Coe2.ToString();
            maxtemp.Text = Max2.ToString();
            mintemp.Text = Min2.ToString();
            offtemp.Text = Off2.ToString();

            coefcl.Text = Coe3.ToString();
            maxfcl.Text = Max3.ToString();
            minfcl.Text = Min3.ToString();
            offfcl.Text = Off3.ToString();

            coetur.Text = Coe4.ToString();
            maxtur.Text = Max4.ToString();
            mintur.Text = Min4.ToString();
            offtur.Text = Off4.ToString();
        }

        private void BackAll()
        {
            pgLoading.Hide();
            pgLogin.Hide();
            pgMain.Hide();
            pgSetup.Hide();
            pgCurve.Hide();
            pgSTemp.Hide();
            pgSpH.Hide();
            pgStur.Hide();
            pgSFcl.Hide();
            pgCTemp.Hide();
            pgCpH.Hide();
            pgCtur.Hide();
            pgCFcl.Hide();
            pgRecord.Hide();
            pgWarning.Hide();
            pgSetting.Hide();
            pgSetting3.Hide();
            pgLoginSensor.Hide();
            pgSetting2.Hide();
        }

        private void btnSetup_Click(object sender, EventArgs e)
        {
            pgMain.Hide();
            pgSetup.Show();
        }

        private void btnCurve_Click(object sender, EventArgs e)
        {
            BackAll();
            pgCurve.Show();
        }

        private void spH_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSpH.Show();

            pageSensor = 1;
        }

        private void sFcl_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSFcl.Show();

            pageSensor = 3;
        }

        private void sTemp_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSTemp.Show();

            pageSensor = 2;
        }

        private void sTur_Click(object sender, EventArgs e)
        {
            BackAll();
            pgStur.Show();

            pageSensor = 4;
        }

        private void cpH_Click(object sender, EventArgs e)
        {
            BackAll();
            pgCpH.Show();
        }

        private void cTemp_Click(object sender, EventArgs e)
        {
            BackAll();
            pgCTemp.Show();
        }

        private void cFcl_Click(object sender, EventArgs e)
        {
            BackAll();
            pgCFcl.Show();
        }

        private void cTur_Click(object sender, EventArgs e)
        {
            BackAll();
            pgCtur.Show();
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            BackAll();
            pgRecord.Show();
        }

        private void btnWarning_Click(object sender, EventArgs e)
        {
            BackAll();
            pgWarning.Show();
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSetting.Show();
        }

        private void set1_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSetting.Show();
        }

        private void set2_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSetting2.Show();
        }

        private void set3_Click(object sender, EventArgs e)
        {
            BackAll();
            pgSetting3.Show();
        }

        #endregion

        #region ==================================|  Datetime  |===============================


        private void getTime(object sender, EventArgs e)
        {
            UpdateTime();
        }


        private void UpdateTime()
        {
            DateTime sett = DateTime.Now;
            DateTime now = DateTime.Now;

            timeday.Text = now.ToString("dddd");
            time2.Text = now.ToString("HH:mm:ss\ndd/MM/yyyy");
        }

        #endregion

        #region ==================================|  TXT  |===============================

        //string cpath = "D:\\";
        string cpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private void readTXT()
        {
            /*
            
            Port1 = COM8  - 3
            Port2 = COM24  - 4
            Baudrate1 = 9600
            Baudrate2 = 9600
            Stopbits1 = 1
            Stopbits2 = 1
            Parity1 = None
            Parity2 = None
            Interval1 = 1500

             
            */

            try
            {
                string fullpath = Path.Combine(cpath, filePath); //config.dat

                string[] lines = File.ReadAllLines(fullpath);

                Console.WriteLine("txt ada, reading!!");

                foreach (string line in lines)
                {
                    // Split each line into key and value
                    string[] parts = line.Split('=');

                    if (parts.Length == 2)
                    {
                        // Trim to remove extra spaces, and assign to variables
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Assign values to corresponding controls
                        switch (key)
                        {
                            case "Port1":
                                slcPort1.Items.Add(value);
                                slcPort1.Text = value;
                                break;

                            case "Port2":
                                slcPort2.Items.Add(value);
                                slcPort2.Text = value;
                                break;

                            case "Baudrate1":
                                slcBaudrate1.Text = value;
                                break;

                            case "Baudrate2":
                                slcBaudrate2.Text = value;
                                break;

                            case "Stopbits1":
                                slcStopbits1.Text = value;
                                break;

                            case "Stopbits2":
                                slcStopbits2.Text = value;
                                break;

                            case "Parity1":
                                slcParity1.Text = value;
                                break;

                            case "Parity2":
                                slcParity2.Text = value;
                                break;

                            case "Interval1":
                                slcInterval1.Text = value;
                                intervalReq = int.Parse(value);
                                break;

                            case "interval":
                                //csvInterval.Text = value;
                                intervalcsv = int.Parse(value);
                                rtuSlave.holdingRegisters[101] = (short)intervalcsv;

                                break;

                            case "Units":
                                slcUnits.Text = value;
                                break;

                            case "ID": txtID.Text = value; break;
                            case "idph": phid = value; break;
                            case "idtemp": tempid = value; break; 
                            case "idfcl": fclid = value; break; 
                            case "idtur": turid = value; break;

                            case "cph": cph.Text = value; break; 
                            case "rv1": RV1 = double.Parse(value); setRegister(RV1, 104); break; 
                            case "cv1": CV1 = double.Parse(value); setRegister(CV1, 102); break; 
                            
                            case "ctemp": ctemp.Text = value; break; 
                            case "rv2": RV2 = double.Parse(value); setRegister(RV2, 124); break; 
                            case "cv2": CV2 = double.Parse(value); setRegister(CV2, 122); break; 

                            case "cfcl": cfcl.Text = value; break;   
                            case "rv3": RV3 = double.Parse(value); setRegister(RV3, 144); break; 
                            case "cv3": CV3 = double.Parse(value); setRegister(CV3, 142); break;
                                
                            case "ctur": ctur.Text = value; break; 
                            case "rv4": RV4 = double.Parse(value); setRegister(RV4, 164); break; 
                            case "cv4": CV4 = double.Parse(value); setRegister(CV4, 162); break; 

                            case "first": first = bool.Parse(value); break;

                            case "coe1": Coe1 = double.Parse(value); coeph.Text = value; setRegister(Coe1, 106); break;
                            case "max1": Max1 = double.Parse(value); maxph.Text = value; setRegister(Max1, 108); break;
                            case "min1": Min1 = double.Parse(value); minph.Text = value; setRegister(Min1, 110); break;
                            case "off1": Off1 = double.Parse(value); offph.Text = value; setRegister(Off1, 112); break;

                            case "coe2": Coe2 = double.Parse(value); coetemp.Text = value;  setRegister(Coe2, 126); break;
                            case "max2": Max2 = double.Parse(value); maxtemp.Text = value;  setRegister(Max2, 128); break;
                            case "min2": Min2 = double.Parse(value); mintemp.Text = value;  setRegister(Min2, 130); break;
                            case "off2": Off2 = double.Parse(value); offtemp.Text = value;  setRegister(Off2, 132); break;

                            case "coe3": Coe3 = double.Parse(value); coefcl.Text = value; setRegister(Coe3, 146); break;
                            case "max3": Max3 = double.Parse(value); maxfcl.Text = value; setRegister(Max3, 148); break;
                            case "min3": Min3 = double.Parse(value); minfcl.Text = value; setRegister(Min3, 150); break;
                            case "off3": Off3 = double.Parse(value); offfcl.Text = value; setRegister(Off3, 152); break;

                            case "coe4": Coe4 = double.Parse(value); coetur.Text = value; setRegister(Coe4, 166); break;
                            case "max4": Max4 = double.Parse(value); maxtur.Text = value; setRegister(Max4, 168); break;
                            case "min4": Min4 = double.Parse(value); mintur.Text = value; setRegister(Min4, 170); break;
                            case "off4": Off4 = double.Parse(value); offtur.Text = value; setRegister(Off4, 172); break;

                            case "cb1ph": cal1ph.Text = value; break;
                            case "cb2ph": cal2ph.Text = value; break;

                            case "cb1temp": cal1temp.Text = value; break;
                            case "cb2temp": cal2temp.Text = value; break;

                            case "cb1fcl": cal1fcl.Text = value; break;
                            case "cb2fcl": cal2fcl.Text = value; break;

                            case "cb1tur": cal1tur.Text = value; break;
                            case "cb2tur": cal2tur.Text = value; break;

                            // Add cases for other keys as needed
                            default:
                                // Handle unknown key or ignore
                                Console.WriteLine("not appended with value : "+ value);
                                break;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                //C:\Users\bayue\OneDrive - Universitas Airlangga\Document\AWG.Corp\Project\Water Quality\UI\v0.92 - revision com txt only\WATER QUALITY MONITORING\WATER QUALITY MONITORING\
                
                createTXT();
                readTXT();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading the file: {ex.Message}", "File Reading", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void createTXT()
        {
            try
            {
                string fullpath = Path.Combine(cpath, filePath);

                File.WriteAllLines(fullpath, defaultValues);

                Console.WriteLine("txt gaada, generating!!");

            }
            catch { }
        }

        private void UpdateConfigValue(string searchKey, string newValue, string filePath = null)
        {
            try
            {
                if(filePath == null)
                {
                    filePath = "config.dat";
                    filePath = Path.Combine(cpath, filePath);
                }

                string[] lines = File.ReadAllLines(filePath);

                // Find the index of the line that starts with the searchKey
                int index = Array.FindIndex(lines, line => line.StartsWith(searchKey));

                // If the searchKey is found, update its value with the new value
                if (index != -1)
                {
                    lines[index] = $"{searchKey} = {newValue}";
                    // Write the updated defaultValues array to the file
                    File.WriteAllLines(filePath, lines);
                    
                }
                else
                {
                    // If the searchKey is not found, you might want to handle this case accordingly
                    Console.WriteLine($"Error: '{searchKey}' not found in defaultValues.");
                }
                
                
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during the file write operation
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
/*
        private string getTXT(string name, string filePath = "pathCOM.txt")
        {
            string data = "";

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                //Console.WriteLine("txt ada, reading!!");

                foreach (string line in lines)
                {
                    // Split each line into key and value
                    string[] parts = line.Split('=');

                    if (parts.Length == 2)
                    {
                        // Trim to remove extra spaces, and assign to variables
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Assign values to corresponding controls
                        switch (key)
                        {
                            case "Port1":
                                if (name == "Port1")
                                    data = value;
                                break;

                            case "Port2":
                                if (name == "Port2")
                                    data = value;
                                break;

                            case "il1":
                                if (name == "il1")
                                    data = value;
                                break;

                            default:
                                // Handle unknown key or ignore
                                Console.WriteLine("not appended with value : " + value);
                                break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            if (data != "") { string info = $"get {name} with value {data}";  Console.WriteLine(info); txtWriteBottom(info + "\n", txtInfo); }

            return data;
        }*/

        #endregion

        #region ==================================|  Logger  |===============================

        /////////////////////////////     log     ///////////////////////////////////

        string pathlog = "log-";

        string[] addline = new string[8];

        string logdata = "";

        bool first = false;

        private void changeNameLog()
        {
            pathlog = "log-";

            string currentDate = DateTime.Now.ToString("dd_MM_yyyy");
            pathlog += currentDate + ".csv";
            
            Console.WriteLine(pathlog);
        }


        private void configLog()
        {

            dataRecord.ColumnCount = 9;
            dataRecord.Columns[0].Name = "Timestamp";
            dataRecord.Columns[1].Name = "pH";
            dataRecord.Columns[2].Name = "Temp";
            dataRecord.Columns[3].Name = "Fcl";
            dataRecord.Columns[4].Name = "Turbidity";
            dataRecord.Columns[5].Name = "Raw pH";
            dataRecord.Columns[6].Name = "Raw Temp";
            dataRecord.Columns[7].Name = "Raw Fcl";
            dataRecord.Columns[8].Name = "Raw Turbidity";

            //changeNameLog();


            // Disable user adding rows
            dataRecord.AllowUserToAddRows = false;

            // Disable row and column resizing
            dataRecord.AllowUserToResizeRows = false;
            dataRecord.AllowUserToResizeColumns = false;

            dataRecord.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataRecord.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataRecord.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            dataRecord.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            /*
                        int pengkali = 1;

                        if      (unit == "Seconds") { pengkali = 1000;          units.Text = "s";  }
                        else if (unit == "Minutes") { pengkali = (1000 * 60);   units.Text = "m"; }
            */
            /*
            string pathCOM1 = Path.Combine(destinationDirectory, pathCOM);
            slcPort1.Text = getTXT("Port1", pathCOM1);*/
                       
            csvInterval.Text = intervalcsv.ToString();

            csvtimer.Interval = intervalcsv * 1000; //* pengkali;
            csvtimer.Start();


        }


        int intervalcsv = 60;

        


        private void csvtimer_Tick(object sender, EventArgs e)
        {
            /*if(rtuSlave.holdingRegisters[100] != (short)intervalcsv)
            {
                intervalcsv = (int)rtuSlave.holdingRegisters[100];
                Console.WriteLine("interval tidak sama");
                UpdateConfigValue("interval", intervalcsv.ToString(),"il.txt");
            }*/
            //Console.WriteLine("IKIIIIIIII NYIMPEEEEEEEEEEEEEEEEEEEEEEEEENNNNNNNNNNNNNNNNNNNNNNNNNNNNN");
            WriteLog();   

            //Console.WriteLine("ngeprint csv");
        }



        private void setCSV_Click(object sender, EventArgs e)
        {
            csvtimer.Stop();

            configLog();
        }


        int count = 0;

        void WriteLog()
        {
            try
            {
                changeNameLog();

                string filePath = Path.Combine(cpath, pathlog); //log.csv

                bool check = false;


                for (int i = 0; i < addline.Length; i++)
                {
                    if (!string.IsNullOrEmpty(addline[i]))
                    {
                        check = true;
                    }
                    else
                    {
                        check = false;
                    }
                }

                if (check)
                {
                    // Export DataGridView data to CSV
                    StringBuilder sb = new StringBuilder();

                    logdata = $"{DateTime.Now};{addline[0]:0.00};{addline[1]:0.00};{addline[2]:0.00};{addline[3]:0.00};{addline[4]};{addline[5]};{addline[6]};{addline[7]};";
                    Console.WriteLine(logdata);

                    // Write the data to the file (Append mode)
                    using (StreamWriter writer = new StreamWriter(filePath, append: true))
                    {                        
                        writer.WriteLine(logdata);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                //MessageBox.Show("File not found. Creating a new file with default values.", "File Reading", MessageBoxButtons.OK, MessageBoxIcon.Information);

                createlog();
                WriteLog();
            }

        }

        private void slcTanggal_Click(object sender, EventArgs e)
        {
            slcTanggal.Items.Clear();
            updateTanggal();
        }

        private void updateTanggal()
        {

            string filePath = cpath;
            string[] csvFiles = Directory.GetFiles(filePath, "log-*.csv");

            Array.Sort(csvFiles);
            Array.Reverse(csvFiles);

            if (csvFiles.Length > 0)
            {
                // Extract and parse the date from each file name
                List<(DateTime date, string fileName)> fileDates = new List<(DateTime date, string fileName)>();
                foreach (string csvFile in csvFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(csvFile); // Get only the file name without extension
                    string dateString = fileName.Substring(4); // Extract the date part from the file name
                    if (DateTime.TryParseExact(dateString, "dd_MM_yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        fileDates.Add((date, csvFile));
                    }
                }

                // Sort the file names based on the extracted dates (from newest to oldest)
                fileDates.Sort((x, y) => y.date.CompareTo(x.date));

                
                // Add sorted file names to the ComboBox
                foreach ((DateTime date, string fileName) in fileDates)
                {
                    string fileName2 = Path.GetFileName(fileName);
                    fileName2 = Path.GetFileNameWithoutExtension(fileName2);
                    slcTanggal.Items.Add(fileName2);
                }
            } 

        }


        private void updateTabel()
        {
            try
            {
                if(slcTanggal.Text != string.Empty)
                {
                    dataRecord.Rows.Clear();

                    string filePath = Path.Combine(cpath, slcTanggal.Text + ".csv");
                    string[] lines = File.ReadAllLines(filePath);

                    // Store the latest timestamp and its corresponding values
                    string lastTimestamp = "";
                    string[] lastValues = null;

                    foreach (string line in lines)
                    {
                        string[] values = line.Split(';');
                        string csvTimestamp = values[0];

                        // Add the current row from the CSV file
                        dataRecord.Rows.Add(values);

                        // Update the lastTimestamp and lastValues for the next iteration
                        lastTimestamp = csvTimestamp;
                        lastValues = values;
                    }

                    // MessageBox.Show("Data loaded successfully.");
                }
                else
                {
                    MessageBox.Show("Please select a date to display data!\nIn top left corner ");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        private void updateRecord_Click(object sender, EventArgs e)
        {
            updateTabel();
        }

        private void exportRecord_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save CSV File";
            saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Export DataGridView data to CSV
                StringBuilder sb = new StringBuilder();

                // Add column headers
                for (int i = 0; i < dataRecord.Columns.Count; i++)
                {
                    sb.Append(dataRecord.Columns[i].HeaderText);
                    if (i < dataRecord.Columns.Count - 1)
                    {
                        sb.Append(";");
                    }
                }
                sb.AppendLine();

                // Add row data
                foreach (DataGridViewRow row in dataRecord.Rows)
                {
                    for (int i = 0; i < dataRecord.Columns.Count; i++)
                    {
                        sb.Append(row.Cells[i].Value);
                        if (i < dataRecord.Columns.Count - 1)
                        {
                            sb.Append(";");
                        }
                    }
                    sb.AppendLine();
                }

                // Write CSV data to file
                File.WriteAllText(saveFileDialog.FileName, sb.ToString());
            }

        }

        

        private void createlog()
        {
            try
            {
                string filePath = Path.Combine(cpath, pathlog);
                File.Create(filePath);

                StringBuilder sb = new StringBuilder();

                // Add column headers
                for (int i = 0; i < dataRecord.Columns.Count; i++)
                {
                    sb.Append(dataRecord.Columns[i].HeaderText);
                    if (i < dataRecord.Columns.Count - 1)
                    {
                        sb.Append(";");
                    }
                }
                sb.AppendLine();

                using (StreamWriter writer = new StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(sb.ToString());
                }
            }
            catch { }
        }

        static string[] ReadLog(string filePath)
        {
            string[] lines;

            // Read the data from the file
            using (StreamReader reader = new StreamReader(filePath))
            {
                // ReadToEnd returns the entire contents of the file as a single string
                string content = reader.ReadToEnd();
                // Split the content by line breaks
                lines = content.Split('\n');
            }

            return lines;
        }


        #endregion

        #region ==================================|  Charting Grafik  |===============================

        private void updateChart()
        {
            phchart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;
            tempchart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;
            fclchart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;
            turchart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X;

            
            if (select == 11)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-1);

                /*DateTime h24 = DateTime.Now.AddDays(-1).Date + DateTime.Now.TimeOfDay;

                string current = now.ToString("yyyy-MM-dd HH:00:00");
                string yesterday = h24.ToString("yyyy-MM-dd HH:00:00");*/

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        if (now.Day == rowTimestamp.Day)
                        {
                            times.Add(rowTimestamp.ToString("HH:mm"));
                            Console.WriteLine(rowTimestamp.ToString("dd/MM/yyyy - HH:mm"));

                            // Add the value from the DataGridView to the list of values
                            if (double.TryParse(dataRecord.Rows[i].Cells[1].Value.ToString(), out double value))
                            {
                                values.Add(value);
                            }
                        }

                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = now.ToString("dd/MM/yyyy"),
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 12)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-7);

                int step = 20; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[1].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        ScalesYAt = 0,
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 13)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-30);

                int step = 60; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[1].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 21)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-1);

                /*DateTime h24 = DateTime.Now.AddDays(-1).Date + DateTime.Now.TimeOfDay;

                string current = now.ToString("yyyy-MM-dd HH:00:00");
                string yesterday = h24.ToString("yyyy-MM-dd HH:00:00");*/

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        if (now.Day == rowTimestamp.Day)
                        {
                            times.Add(rowTimestamp.ToString("HH:mm"));
                            Console.WriteLine(rowTimestamp.ToString("dd/MM/yyyy - HH:mm"));

                            // Add the value from the DataGridView to the list of values
                            if (double.TryParse(dataRecord.Rows[i].Cells[2].Value.ToString(), out double value))
                            {
                                values.Add(value);
                            }
                        }

                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = now.ToString("dd/MM/yyyy"),
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 22)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-7);

                int step = 20; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[2].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        ScalesYAt = 0,
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 23)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-30);

                int step = 60; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[2].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }


            if (select == 31)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-1);

                /*DateTime h24 = DateTime.Now.AddDays(-1).Date + DateTime.Now.TimeOfDay;

                string current = now.ToString("yyyy-MM-dd HH:00:00");
                string yesterday = h24.ToString("yyyy-MM-dd HH:00:00");*/

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        if (now.Day == rowTimestamp.Day)
                        {
                            times.Add(rowTimestamp.ToString("HH:mm"));
                            Console.WriteLine(rowTimestamp.ToString("dd/MM/yyyy - HH:mm"));

                            // Add the value from the DataGridView to the list of values
                            if (double.TryParse(dataRecord.Rows[i].Cells[3].Value.ToString(), out double value))
                            {
                                values.Add(value);
                            }
                        }

                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = now.ToString("dd/MM/yyyy"),
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 32)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-7);

                int step = 20; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[3].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        ScalesYAt = 0,
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 33)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-30);

                int step = 60; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[3].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }


            if (select == 41)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-1);

                /*DateTime h24 = DateTime.Now.AddDays(-1).Date + DateTime.Now.TimeOfDay;

                string current = now.ToString("yyyy-MM-dd HH:00:00");
                string yesterday = h24.ToString("yyyy-MM-dd HH:00:00");*/

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        if (now.Day == rowTimestamp.Day)
                        {
                            times.Add(rowTimestamp.ToString("HH:mm"));
                            Console.WriteLine(rowTimestamp.ToString("dd/MM/yyyy - HH:mm"));

                            // Add the value from the DataGridView to the list of values
                            if (double.TryParse(dataRecord.Rows[i].Cells[4].Value.ToString(), out double value))
                            {
                                values.Add(value);
                            }
                        }

                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = now.ToString("dd/MM/yyyy"),
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 42)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-7);

                int step = 20; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[4].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        ScalesYAt = 0,
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 43)
            {
                DateTime now = DateTime.Now;
                DateTime yesterday = now.Date.AddDays(-30);

                int step = 60; // 20 min

                List<double> values = new List<double>();
                List<string> times = new List<string>();

                //DateTime curr = yesterday;

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {
                        // Check if the timestamp is within the range of yesterday with the same hour until now
                        if (rowTimestamp >= yesterday && rowTimestamp <= now)
                        {
                            // Calculate the difference in minutes between the current row timestamp and yesterdaySameHour
                            double minutesDifference = (rowTimestamp - yesterday).TotalMinutes;

                            // Check if the difference is divisible by 20, indicating a 20-minute interval
                            if (minutesDifference % step == 0)
                            {
                                times.Add(rowTimestamp.ToString("HH:mm"));
                                if (double.TryParse(dataRecord.Rows[i].Cells[4].Value.ToString(), out double value))
                                {
                                    values.Add(value);
                                }

                            }
                        }
                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = $"{yesterday.ToString("dd/MM/yyyy")} - {now.ToString("dd/MM/yyyy")}",
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 101)
            {
                DateTime currentTimestamp = DateTime.Now;
                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {

                        times.Add(rowTimestamp.ToString("HH:mm"));

                        // Add the value from the DataGridView to the list of values
                        if (double.TryParse(dataRecord.Rows[i].Cells[1].Value.ToString(), out double value))
                        {
                            values.Add(value);
                        }

                    }
                }

                phchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null
                    }
                };

                phchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 102)
            {
                DateTime currentTimestamp = DateTime.Now;
                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {

                        times.Add(rowTimestamp.ToString("HH:mm"));

                        // Add the value from the DataGridView to the list of values
                        if (double.TryParse(dataRecord.Rows[i].Cells[2].Value.ToString(), out double value))
                        {
                            values.Add(value);
                        }

                    }
                }

                tempchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                tempchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 103)
            {
                DateTime currentTimestamp = DateTime.Now;
                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {

                        times.Add(rowTimestamp.ToString("HH:mm"));

                        // Add the value from the DataGridView to the list of values
                        if (double.TryParse(dataRecord.Rows[i].Cells[3].Value.ToString(), out double value))
                        {
                            values.Add(value);
                        }

                    }
                }

                fclchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null
                    }
                };

                fclchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Labels = times.ToArray(),
                    }
                };
            }

            if (select == 104)
            {
                DateTime currentTimestamp = DateTime.Now;
                List<double> values = new List<double>();
                List<string> times = new List<string>();

                for (int i = 0; i < dataRecord.Rows.Count; i++)
                {
                    // Get the timestamp from the first column of each row
                    if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                    {

                        times.Add(rowTimestamp.ToString("HH:mm"));

                        // Add the value from the DataGridView to the list of values
                        if (double.TryParse(dataRecord.Rows[i].Cells[4].Value.ToString(), out double value))
                        {
                            values.Add(value);
                        }

                    }
                }

                turchart.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values.ToArray(),
                        Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 4 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                        GeometryFill = null,
                    }
                };

                turchart.XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Labels = times.ToArray(),
                    }
                };
            }



            /*
                        if (select == 21)
                        {
                            DateTime currentTimestamp = DateTime.Now;
                            List<double> values = new List<double>();
                            List<string> times = new List<string>();

                            for (int i = 0; i < dataRecord.Rows.Count; i++)
                            {
                                // Get the timestamp from the first column of each row
                                if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                                {

                                    times.Add(rowTimestamp.ToString("HH:mm"));

                                    // Add the value from the DataGridView to the list of values
                                    if (double.TryParse(dataRecord.Rows[i].Cells[2].Value.ToString(), out double value))
                                    {
                                        values.Add(value);
                                    }

                                }
                            }

                            tempchart.Series = new ISeries[]
                            {
                                new LineSeries<double>
                                {
                                    Values = values.ToArray(),
                                    Fill = null,
                                    GeometrySize = 0,
                                    GeometryFill = null,
                                }
                            };

                            tempchart.XAxes = new List<Axis>
                            {
                                new Axis
                                {
                                    Labels = times.ToArray(),
                                }
                            };
                        }

                        if (select == 31)
                        {
                            DateTime currentTimestamp = DateTime.Now;
                            List<double> values = new List<double>();
                            List<string> times = new List<string>();

                            for (int i = 0; i < dataRecord.Rows.Count; i++)
                            {
                                // Get the timestamp from the first column of each row
                                if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                                {

                                    times.Add(rowTimestamp.ToString("HH:mm"));

                                    // Add the value from the DataGridView to the list of values
                                    if (double.TryParse(dataRecord.Rows[i].Cells[3].Value.ToString(), out double value))
                                    {
                                        values.Add(value);
                                    }

                                }
                            }

                            fclchart.Series = new ISeries[]
                            {
                                new LineSeries<double>
                                {
                                    Values = values.ToArray(),
                                    Fill = null,
                                    GeometrySize = 0,
                                    GeometryFill = null,
                                }
                            };

                            fclchart.XAxes = new List<Axis>
                            {
                                new Axis
                                {
                                    Labels = times.ToArray(),
                                }
                            };
                        }

                        if (select == 41)
                        {
                            DateTime currentTimestamp = DateTime.Now;
                            List<double> values = new List<double>();
                            List<string> times = new List<string>();

                            for (int i = 0; i < dataRecord.Rows.Count; i++)
                            {
                                // Get the timestamp from the first column of each row
                                if (DateTime.TryParse(dataRecord.Rows[i].Cells[0].Value.ToString(), out DateTime rowTimestamp))
                                {

                                    times.Add(rowTimestamp.ToString("HH:mm"));

                                    // Add the value from the DataGridView to the list of values
                                    if (double.TryParse(dataRecord.Rows[i].Cells[4].Value.ToString(), out double value))
                                    {
                                        values.Add(value);
                                    }

                                }
                            }

                            turchart.Series = new ISeries[]
                            {
                                new LineSeries<double>
                                {
                                    Values = values.ToArray(),
                                    Fill = null,
                                    GeometrySize = 0,
                                    GeometryFill = null,
                                }
                            };

                            turchart.XAxes = new List<Axis>
                            {
                                new Axis
                                {
                                    Labels = times.ToArray(),
                                }
                            };
                        }*/
        }

        // ph
        int select = 0;

        private void CPH_Click(object sender, EventArgs e)
        {
            select = 101;
            updateChart();
        }

        private void CTEMP_Click(object sender, EventArgs e)
        {
            select = 102;
            updateChart();
        }

        private void CFCL_Click(object sender, EventArgs e)
        {
            select = 103;
            updateChart();
        }

        private void CTUR_Click(object sender, EventArgs e)
        {
            select = 104;
            updateChart();
        }

        ///12324343423423423423244323424324343243342342342342432342342432243342342432



        private void h11_Click(object sender, EventArgs e)
        {
            select = 11;
            updateChart();
        }

        private void h12_Click(object sender, EventArgs e)
        {
            select = 12;
            updateChart();
        }

        private void h13_Click(object sender, EventArgs e)
        {
            select = 13;
            updateChart();
        }


        private void h21_Click(object sender, EventArgs e)
        {
            select = 21;
            updateChart();
        }

        private void h22_Click(object sender, EventArgs e)
        {
            select = 22;
            updateChart();
        }

        private void h23_Click(object sender, EventArgs e)
        {
            select = 23;
            updateChart();
        }

        private void h31_Click(object sender, EventArgs e)
        {
            select = 31;
            updateChart();
        }

        private void h32_Click(object sender, EventArgs e)
        {
            select = 32;
            updateChart();
        }

        private void h33_Click(object sender, EventArgs e)
        {
            select = 33;
            updateChart();
        }

        private void h41_Click(object sender, EventArgs e)
        {
            select = 41;
            updateChart();
        }

        private void h42_Click(object sender, EventArgs e)
        {
            select = 42;
            updateChart();
        }

        private void h43_Click(object sender, EventArgs e)
        {
            select = 43;
            updateChart();
        }


        #endregion

        #region ==================================|  Calibrate  |===============================

        //bool first = true;

        private int[] ConvertShortArrayToIntArray(short[] shortArray)
        {
            int[] intArray = new int[shortArray.Length];
            for (int i = 0; i < shortArray.Length; i++)
            {
                intArray[i] = shortArray[i];
            }
            return intArray;
        }

        private void setRegister(double CV, int register)
        {
            int[] data = ConvertFloatToTwoIntegers((float)CV);

            rtuSlave.holdingRegisters[register] = (short)data[0];    //reg 101, 64
            rtuSlave.holdingRegisters[register + 1] = (short)data[1];    //reg 102, 65
        }

        void textboxChange(Guna2TextBox txt, string data)
        {
            if (txt.InvokeRequired)
            {
                // Invoke the method on the UI thread
                txt.Invoke(new Action(() =>
                {
                    txt.Text = data;
                }));

            }
            else
            {
                txt.Text = data;
            }
        }

        private double getRegister(int register)
        {

            short[] data2 = new short[2] { rtuSlave.holdingRegisters[register], rtuSlave.holdingRegisters[register+1] };

            double result = Math.Round((double)ConvertData(ConvertShortArrayToIntArray(data2)), 5);

            return result;
        }


        private double CalibrateValue(double originalValue, double calibrationValue, double referenceValue)
        {
            // Calibrate the original value
            double calibratedValue = originalValue * (calibrationValue / referenceValue);
            // Return the calibrated value
            return calibratedValue;
        }

        double RV1 = 0;
        double RV2 = 0;
        double RV3 = 0;
        double RV4 = 0;

        double OV1 = 50;
        double OV2 = 50;
        double OV3 = 50;
        double OV4 = 50;

        double Coe1, Max1, Min1, Off1;
        double Coe2, Max2, Min2, Off2;
        double Coe3, Max3, Min3, Off3;
        double Coe4, Max4, Min4, Off4;

        double CV1 = 0;
        double CV2 = 0;
        double CV3 = 0;
        double CV4 = 0;

        private double Calibration1()
        {
            CV1 = double.Parse(cph.Text, CultureInfo.InvariantCulture);

            double DV = CalibrateValue(OV1, CV1, RV1);

            double result = 0;

            /*Console.WriteLine(result);
            Console.WriteLine($"ph : {CV1} {Coe1} {OV1} {DV} {RV1} {Max1}  {Min1}");*/


            if (OV1 == 0) { return result; }

            result = (DV * Coe1) + Off1;

            double tolerance = 0.01; // Adjust tolerance as needed

            if (result - Max1 > tolerance)
            {
                result = Max1;
            }
            else if (Min1 - result > tolerance)
            {
                result = Min1;
            }

            

            return result;
        }

        private double Calibration2()
        {
            CV2 = double.Parse(ctemp.Text, CultureInfo.InvariantCulture);

            double DV = CalibrateValue(OV2, CV2, RV2);

            double result = 0;
            if(OV2 == 0) { return result; }

            result = (DV * Coe2) + Off2;

            /*Console.WriteLine(result);
            Console.WriteLine($"{CV2} {Coe2} {OV2} {DV} {RV2} {Max2}  {Min2}");*/

            double tolerance = 0.01; // Adjust tolerance as needed

            if (result - Max2 > tolerance)
            {
                result = Max2;
            }
            else if (Min2 - result > tolerance)
            {
                result = Min2;
            }

            return result;
        }

        private double Calibration3()
        {
            CV3 = double.Parse(cfcl.Text, CultureInfo.InvariantCulture);

            double DV = CalibrateValue(OV3, CV3, RV3);

            double result = 0;
            if(OV3 == 0) { return result; }

            /*Console.WriteLine(result);
            Console.WriteLine($"fcl : {CV3} {Coe3} {OV3} {DV} {RV3} {Max3}  {Min3}");*/

            result = (DV * Coe3) + Off3;

            double tolerance = 0.01; // Adjust tolerance as needed

            if (result - Max3 > tolerance)
            {
                result = Max3;
            }
            else if (Min3 - result > tolerance)
            {
                result = Min3;
            }

            return result;
        }

        private double Calibration4()
        {
            CV4 = double.Parse(ctur.Text, CultureInfo.InvariantCulture);

            double DV = CalibrateValue(OV4, CV4, RV4);

            double result = 0;
            if (OV4 == 0) { return result; }

            result = (DV * Coe4) + Off4;

            /*Console.WriteLine(result);
            Console.WriteLine($"{CV4} {Coe4} {OV4} {DV} {RV4} {Max4}  {Min4}");*/

            double tolerance = 0.01; // Adjust tolerance as needed

            if (result - Max4 > tolerance)
            {
                result = Max4;
            }
            else if (Min4 - result > tolerance)
            {
                result = Min4;
            }

            return result;
        }



        #endregion

        #region ==================================|  Modbus Sensor  |===============================

        private ModbusClient rtuMaster;

        private int intervalReq = 1000;

        private Thread req;


        private void configMB()
        {            
            rtuMaster = new ModbusClient();

            request = false;

            rtuMaster.SerialPort = slcPort1.Text;
            rtuMaster.Baudrate = int.Parse(slcBaudrate1.Text);

            Parity par;
            if (slcParity1.Text == "Odd") { par = Parity.Odd; }
            else if (slcParity1.Text == "Even") { par = Parity.Even; }
            else { par = Parity.None; }

            StopBits sb;
            if (slcStopbits1.Text == "2") { sb = StopBits.Two; }
            else { sb = StopBits.One; }

            rtuMaster.Parity = par;
            rtuMaster.StopBits = sb;
            rtuMaster.ConnectionTimeout = 250;
            rtuMaster.NumberOfRetries = 2;

            intervalReq = int.Parse(slcInterval1.Text);

            defineparam1();
            defineparam2();
            defineparam3();
            defineparam4();

            try
            {
                request = true;

                rtuMaster.Connect();

                displayTime.Interval = 2000;
                displayTime.Start();
            }
            catch { }
        }

        static string ByteArrayToString(byte[] byteArray)
        {
            string result = "";
            if (byteArray != null)
            {
                foreach (byte b in byteArray)
                {
                    result += b.ToString("X2") + " ";
                }

            }
            return result; // Remove the trailing space
        }

        int[] data1;
        int[] data2;
        int[] data3;
        int[] data4;

        string phid, tempid, fclid, turid;


        void send()
        {
            string sendDat = rtuMaster.UnitIdentifier.ToString("X2") + " 03 00 01 00 02";  //bentuk frame
            sendDat = sendDat + " " + stringCRC(sendDat); // append dan generate crc
            txtWriteBottom("TX : " + sendDat, txtInfo2);// masukkan ke txtInfo2
        }

        void reciv()
        {
            string getDat = "";
            getDat = ByteArrayToString(rtuMaster.receiveData);
            txtWriteBottom("RX : " + getDat + "\n", txtInfo2);
        }

        void getError(Exception ex, string name)
        {        
            string err = $"{name} Error: {ex.GetType().Name} - {ex.Message}";
            txtWriteBottom(err + "\n", txtInfo2);
            Console.WriteLine(err);
            if (name == "pH") { w1.BackColor = Color.Red; data1 = null; }
            if (name == "Temp") { w2.BackColor = Color.Red; data2 = null; }
            if (name == "Fcl") { w4.BackColor = Color.Red; data3 = null; }
            if (name == "Tur") { w3.BackColor = Color.Red; data4 = null; }
            if (name == "Connection")
            {
                w1.BackColor = Color.Red;
                w2.BackColor = Color.Red;
                w3.BackColor = Color.Red;
                w4.BackColor = Color.Red;
            }
            
        }

        bool request = false;

        private async void requestSensor()
        {
            string name = "";

            while (request)
            {
                

                try
                {
                    name = "Connection";
                    rtuMaster.Connect();

                    name = "pH";
                    rtuMaster.UnitIdentifier = byte.Parse(phid);
                    data1 = rtuMaster.ReadHoldingRegisters(1, 2); 
                    w1.BackColor = SystemColors.Highlight;

                    send();
                    reciv();
                }
                catch (Exception ex)
                {
                    getError(ex, name);
                }

                await DelayAsync(150);
                //Thread.Sleep(150);

                try 
                { 
                    name = "Connection";
                    rtuMaster.Connect();

                    name = "Temp";
                    rtuMaster.UnitIdentifier = byte.Parse(tempid); ;
                    data2 = rtuMaster.ReadHoldingRegisters(3, 2); 
                    w2.BackColor = SystemColors.Highlight;

                    send();
                    reciv();
                }
                catch (Exception ex)
                {
                    getError(ex, name);
                }

                await DelayAsync(150);
                //Thread.Sleep(150);

                try 
                {

                    name = "Connection";
                    rtuMaster.Connect();

                    name = "Fcl";
                    rtuMaster.UnitIdentifier = byte.Parse(fclid);
                    data3 = rtuMaster.ReadHoldingRegisters(1, 2); 
                    w4.BackColor = SystemColors.Highlight;

                    send();
                    reciv();
                }
                catch (Exception ex)
                {
                    getError(ex, name);
                }

                await DelayAsync(150);
                //Thread.Sleep(150);


                try 
                {
                    name = "Connection";
                    rtuMaster.Connect();

                    name = "Tur";
                    rtuMaster.UnitIdentifier = byte.Parse(turid);
                    data4 = rtuMaster.ReadHoldingRegisters(1, 2); 
                    w3.BackColor = SystemColors.Highlight;

                    send();
                    reciv();
                }
                catch (Exception ex)
                {
                    getError(ex, name);
                }

                //Thread.Sleep(150);
                await DelayAsync(2000);

                Console.WriteLine("this is the request"); 
                //Thread.Sleep(intervalReq);
                //rtuMaster.Disconnect();
            }
        }


        private string displayRaw(int[] data, Guna2TextBox disraw)
        {
            string result = ".........";

            if(data != null)
            {
                string hex = $"{data[0].ToString("X2")} {data[1].ToString("X2")}";
                double draw = getFloat(hex);
                string raw = $"{(long)(draw * Math.Pow(10, 8))}";

                result = raw; 

                if (disraw.InvokeRequired) { disraw.Invoke((MethodInvoker)(() => { disraw.Text = raw; })); } else { disraw.Text = raw; }
            }
            

            return result;
        }

        static int[] ConvertFloatToTwoIntegers(float input)
        {
            byte[] bytes = BitConverter.GetBytes(input);
            int hex = BitConverter.ToInt32(bytes, 0);
            string hexString = hex.ToString("X8");

            string firstPart = hexString.Substring(0, 4);
            string secondPart = hexString.Substring(4, 4);

            int firstValue = Convert.ToInt32(firstPart, 16);
            int secondValue = Convert.ToInt32(secondPart, 16);
            
            //reversed
            return new int[] { secondValue, firstValue };
        }


        float ph, temp, fcl, tur;

        private void displayData()
        {
            string f = "F2";

            //Console.WriteLine(addline);

            if (rtuMaster.Connected)
            {

                short v = (short)(loc - 32767);
                rtuSlave.holdingRegisters[21] = v; // logger id : kode pos

                if (data1 != null)
                {

                    rawph.Text = displayRaw(data1, rph);

                    ph = ConvertData(data1);
                    //Console.WriteLine(ph);

                    OV1 = (double)ph;

                    double dis = Calibration1();                    

                    if (data1 != null)
                    {
                        int[] data = ConvertFloatToTwoIntegers((float)dis);
                        rtuSlave.holdingRegisters[1] = (short)data[0]; // reg 0
                        rtuSlave.holdingRegisters[2] = (short)data[1];

                        rtuSlave.holdingRegisters[9]  = (short)data1[0];
                        rtuSlave.holdingRegisters[10] = (short)data1[1];

                    }

                    addline[4] = ph.ToString();
                    addline[0] = dis.ToString(f);

                    //if (rawph.InvokeRequired) { rawph.Invoke((MethodInvoker)(() => { rawph.Text = ph.ToString(); })); } else { rawph.Text = ph.ToString(); }
                    if (ddph.InvokeRequired) { ddph.Invoke((MethodInvoker)(() => { ddph.Text = dis.ToString(f); })); } else { ddph.Text = dis.ToString(f); }
                    if (dph.InvokeRequired) { dph.Invoke((MethodInvoker)(() => { dph.Text = ddph.Text; })); } else { dph.Text = ddph.Text; }

                }

                if (data2 != null)
                {

                    rawtemp.Text = displayRaw(data2, rtemp);

                    temp = ConvertData(data2);

                    OV2 = (double)temp;

                    double dis = Calibration2();

                    if (data2 != null)
                    {
                        int[] data = ConvertFloatToTwoIntegers((float)dis);
                        rtuSlave.holdingRegisters[3] = (short)data[0];
                        rtuSlave.holdingRegisters[4] = (short)data[1];

                        rtuSlave.holdingRegisters[11] = (short)data2[0];
                        rtuSlave.holdingRegisters[12] = (short)data2[1];

                    }

                    addline[5] = temp.ToString();
                    addline[1] = dis.ToString(f);


                    //if (rawtemp.InvokeRequired) { rawtemp.Invoke((MethodInvoker)(() => { rawtemp.Text = temp.ToString(); })); } else { rawtemp.Text = temp.ToString(); }
                    if (ddtemp.InvokeRequired) { ddtemp.Invoke((MethodInvoker)(() => { ddtemp.Text = dis.ToString(f); })); } else { ddtemp.Text = dis.ToString(f); }
                    if (dtemp.InvokeRequired) { dtemp.Invoke((MethodInvoker)(() => { dtemp.Text = ddtemp.Text; })); } else { dtemp.Text = ddtemp.Text; }

                }

                if (data3 != null)
                {

                    rawfcl.Text = displayRaw(data3, rfcl);

                    fcl = ConvertData(data3);

                    //Console.WriteLine(fcl);

                    OV3 = (double)fcl;

                    double dis = Calibration3();

                    if (data3 != null)
                    {
                        int[] data = ConvertFloatToTwoIntegers((float)dis);
                        rtuSlave.holdingRegisters[5] = (short)data[0];
                        rtuSlave.holdingRegisters[6] = (short)data[1];
                        rtuSlave.holdingRegisters[13] = (short)data3[0];
                        rtuSlave.holdingRegisters[14] = (short)data3[1];

                    }

                    addline[6] = fcl.ToString();
                    addline[2] = dis.ToString(f);

                    //if (rawph.InvokeRequired) { rawph.Invoke((MethodInvoker)(() => { rawph.Text = ph.ToString(); })); } else { rawph.Text = ph.ToString(); }
                    if (ddfcl.InvokeRequired) { ddfcl.Invoke((MethodInvoker)(() => { ddfcl.Text = dis.ToString(f); })); } else { ddfcl.Text = dis.ToString(f); }
                    if (dfcl.InvokeRequired) { dfcl.Invoke((MethodInvoker)(() => { dfcl.Text = ddfcl.Text; })); } else { dfcl.Text = ddfcl.Text; }

                }

                if (data4 != null)
                {

                    rawtur.Text = displayRaw(data4, rtur);

                    tur = ConvertData(data4);

                    OV4 = (double)tur;

                    double dis = Calibration4();

                    if (data4 != null)
                    {
                        int[] data = ConvertFloatToTwoIntegers((float)dis);
                        rtuSlave.holdingRegisters[7] = (short)data[0];
                        rtuSlave.holdingRegisters[8] = (short)data[1];
                        rtuSlave.holdingRegisters[15] = (short)data4[0];
                        rtuSlave.holdingRegisters[16] = (short)data4[1];

                    }

                    addline[7] = tur.ToString();

                    addline[3] = dis.ToString(f);

                    if (ddtur.InvokeRequired) { ddtur.Invoke((MethodInvoker)(() => {ddtur.Text = dis.ToString(f);  })); } else { ddtur.Text = dis.ToString(f); }
                    if (dtur.InvokeRequired) { dtur.Invoke((MethodInvoker)(() => { dtur.Text = ddtur.Text; })); } else { dtur.Text = ddtur.Text; }

                }
            }

        }

        private void displayTime_Tick(object sender, EventArgs e)
        {
            displayData();
        }


        static float ConvertData(int[] decimalNumbers)
        {
            string hexString = "";
            float floatResult = 0;
            if (decimalNumbers != null)
            {
                hexString = string.Join(" ", decimalNumbers.Select(number => number.ToString("X2")));
                
                hexString = hexString.Replace("FFFF", "");

                string[] pairs = hexString.Split(' ');
                Array.Reverse(pairs);

                for (int i = 0; i < pairs.Length; i++)
                {
                    while (pairs[i].Length < 4)
                    {
                        pairs[i] = "0" + pairs[i];
                    }
                }

                string reversedHexString = string.Join("", pairs);

                //Console.WriteLine(reversedHexString);

                // Convert hex string to integer
                long longResult = Convert.ToInt64(reversedHexString, 16);

                // Convert long to float
                floatResult = BitConverter.ToSingle(BitConverter.GetBytes(longResult), 0);
            }



            return floatResult;


        }


        private double getFloat(string hexString)
        {
            float result = 0;

            if(hexString != "")
            {

                hexString = hexString.Replace("FFFF", "");

                string[] pairs = hexString.Split(' ');
                Array.Reverse(pairs);

                for (int i = 0; i < pairs.Length; i++)
                {
                    while (pairs[i].Length < 4)
                    {
                        pairs[i] = "0" + pairs[i];
                    }
                }

                string reversedHexString = string.Join("", pairs);

                long longResult = Convert.ToInt64(reversedHexString, 16);

                // Convert long to float
                result = BitConverter.ToSingle(BitConverter.GetBytes(longResult), 0);
            }


            return (double)result;

            
        }

        static string stringCRC(string input)
        {
            string[] hexValues = input.Split(' ');
            byte[] dataBytes = new byte[hexValues.Length];

            for (int i = 0; i < hexValues.Length; i++)
            {
                dataBytes[i] = Convert.ToByte(hexValues[i], 16);
            }

            ushort crc = 0xFFFF; // Initial CRC value

            foreach (byte dataByte in dataBytes)
            {
                crc ^= dataByte;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001; // XOR with the CRC-16 polynomial
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return $"{crc & 0xFF:X2} {crc >> 8:X2}"; // Format as two separate bytes with space
        }


        #endregion

        #region ==================================|  Modbus Slave  |===============================

        private ModbusServer rtuSlave;

        private byte ID = 1;




        private void configSlave()
        {
            /*
                rtuMaster = new ModbusClient();

                string pathCOM1 = Path.Combine(destinationDirectory, pathCOM);
                slcPort1.Text = getTXT("Port1", pathCOM1);
                request = false;

                rtuMaster.SerialPort = getTXT("Port1", pathCOM1);
                rtuMaster.Baudrate = int.Parse(slcBaudrate1.Text);
            */



            try
            {
                
                rtuSlave.UnitIdentifier = ID;

                rtuSlave.HoldingRegistersChanged += SlaveDataChanged;

                /*string pathCOM2 = Path.Combine(destinationDirectory, pathCOM);
                slcPort2.Text = getTXT("Port2", pathCOM2);*/

                if (slcPort1.Text != slcPort2.Text)
                {                    

                    //rtuSlave.Port = slcPort2.Text;

                    rtuSlave.SerialPort = slcPort2.Text;
                    rtuSlave.Baudrate = int.Parse(slcBaudrate2.Text);

                    Parity par;
                    if (slcParity2.Text == "Odd") { par = Parity.Odd; }
                    else if (slcParity2.Text == "Even") { par = Parity.Even; }
                    else { par = Parity.None; }

                    StopBits sb;
                    if (slcStopbits2.Text == "2") { sb = StopBits.Two; }
                    else { sb = StopBits.One; }

                    rtuSlave.Parity = par;
                    rtuSlave.StopBits = sb;
                                        

                    listenTime.Interval = 2000;
                    listenTime.Start();
                }

                
            }
            catch (Exception ex)
            {
                string err = $"Slave Error: {ex.GetType().Name} - {ex.Message}";
                txtWriteBottom(err + "\n", txtInfo);
                Console.WriteLine(err);
            }


        }

        string Port2;

        private void SlaveDataChanged(int register, int numberOfRegisters)
        {

            if (register == 101)
            {
                intervalcsv = (int)rtuSlave.holdingRegisters[101];  

                configLog();

                UpdateConfigValue("interval", intervalcsv.ToString());
            }

            if (register == 102 || register == 103) { CV1  = getRegister(102); textboxChange(cal1ph, CV1.ToString(CultureInfo.InvariantCulture)); cal_ph1(); }
            if (register == 104 || register == 105) { RV1  = getRegister(104); textboxChange(cal1ph, RV1.ToString(CultureInfo.InvariantCulture)); cal_ph2(); }
            if (register == 106 || register == 107) { Coe1 = getRegister(106); textboxChange(coeph, Coe1.ToString(CultureInfo.InvariantCulture)); upparam1(); }
            if (register == 108 || register == 109) { Max1 = getRegister(108); textboxChange(maxph, Max1.ToString(CultureInfo.InvariantCulture)); upparam1(); }
            if (register == 110 || register == 111) { Min1 = getRegister(110); textboxChange(minph, Min1.ToString(CultureInfo.InvariantCulture)); upparam1(); }
            if (register == 112 || register == 113) { Off1 = getRegister(112); textboxChange(offph, Off1.ToString(CultureInfo.InvariantCulture)); upparam1(); }

            if (register == 122 || register == 123) { CV2  = getRegister(122); textboxChange(cal1temp, CV2.ToString(CultureInfo.InvariantCulture)); cal_temp1(); }
            if (register == 124 || register == 125) { RV2  = getRegister(124); textboxChange(cal1temp, RV2.ToString(CultureInfo.InvariantCulture)); cal_temp2(); }
            if (register == 126 || register == 127) { Coe2 = getRegister(126); textboxChange(coetemp, Coe2.ToString(CultureInfo.InvariantCulture)); upparam2(); }
            if (register == 128 || register == 129) { Max2 = getRegister(128); textboxChange(maxtemp, Max2.ToString(CultureInfo.InvariantCulture)); upparam2(); }
            if (register == 130 || register == 131) { Min2 = getRegister(130); textboxChange(mintemp, Min2.ToString(CultureInfo.InvariantCulture)); upparam2(); }
            if (register == 132 || register == 133) { Off2 = getRegister(132); textboxChange(offtemp, Off2.ToString(CultureInfo.InvariantCulture)); upparam2(); }

            if (register == 142 || register == 143) { CV3  = getRegister(142); textboxChange(cal1fcl, CV3.ToString(CultureInfo.InvariantCulture));  cal_fcl1(); }
            if (register == 144 || register == 145) { RV3  = getRegister(144); textboxChange(cal2fcl, RV3.ToString(CultureInfo.InvariantCulture));  cal_fcl2(); }
            if (register == 146 || register == 147) { Coe3 = getRegister(146); textboxChange(coefcl, Coe3.ToString(CultureInfo.InvariantCulture)); upparam3(); }
            if (register == 148 || register == 149) { Max3 = getRegister(148); textboxChange(maxfcl, Max3.ToString(CultureInfo.InvariantCulture)); upparam3(); }
            if (register == 150 || register == 151) { Min3 = getRegister(150); textboxChange(minfcl, Min3.ToString(CultureInfo.InvariantCulture)); upparam3(); }
            if (register == 152 || register == 153) { Off3 = getRegister(152); textboxChange(offfcl, Off3.ToString(CultureInfo.InvariantCulture)); upparam3(); }

            if (register == 162 || register == 163) { CV4  = getRegister(162); textboxChange(cal1tur, CV4.ToString(CultureInfo.InvariantCulture)); cal_tur1(); }
            if (register == 164 || register == 165) { RV4  = getRegister(164); textboxChange(cal1tur, RV4.ToString(CultureInfo.InvariantCulture)); cal_tur2(); }
            if (register == 166 || register == 167) { Coe4 = getRegister(166); textboxChange(coetur, Coe4.ToString(CultureInfo.InvariantCulture)); upparam4(); }
            if (register == 168 || register == 169) { Max4 = getRegister(168); textboxChange(maxtur, Max4.ToString(CultureInfo.InvariantCulture)); upparam4(); }
            if (register == 170 || register == 171) { Min4 = getRegister(170); textboxChange(mintur, Min4.ToString(CultureInfo.InvariantCulture)); upparam4(); }
            if (register == 172 || register == 173) { Off4 = getRegister(172); textboxChange(offtur, Off4.ToString(CultureInfo.InvariantCulture)); upparam4(); }

        }


        private void listenTime_Tick(object sender, EventArgs e)
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();

                if ((slcPort1.Text != slcPort2.Text) && ports.Contains(slcPort2.Text))
                {
                    rtuSlave.Listen();
                    dtu_connnected();

                    //Console.WriteLine(" intervalnya : " + intervalcsv.ToString());
                }
                else
                {
                    string err = "Check configuure COM and ID device : " + DTUused;
                    Console.WriteLine(err);
                    txtWriteBottom(err + "\n", txtInfo);
                    dtu_disconnnected();
                }

                
            }
            catch (InvalidOperationException ex)
            {
                // Handle InvalidOperationException (e.g., port is closed)
                string err = $"Slave Error: {ex.GetType().Name} - {ex.Message}";
                txtWriteBottom(err + "\n", txtInfo2);
                Console.WriteLine(err);
            }
            catch (IOException ex)
            {
                // Handle IOException (e.g., port does not exist)
                string err = $"Slave Error: {ex.GetType().Name} - {ex.Message}";
                txtWriteBottom(err + "\n", txtInfo2);
                Console.WriteLine(err);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                string err = $"Slave Error: {ex.GetType().Name} - {ex.Message}";
                txtWriteBottom(err + "\n", txtInfo2);
                Console.WriteLine(err);
            }
            finally
            {
                // Additional cleanup or error handling if needed
            }
        }



        #endregion

        #region ==================================|  Settings  |===============================

        terminal term = new terminal();

        bool DTUused = false;
        private void ScanPorts1()
        {
            string[] ports = SerialPort.GetPortNames();

            //slcPort1.Items.Clear();

            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    // Check if the port is not already in the ComboBox items
                    if (!slcPort1.Items.Contains(port))
                    {
                        slcPort1.Items.Add(port);
                    }
                }
            }
            else
            {
                MessageBox.Show("No connected ports found.", "Port Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Optionally handle the case when no ports are found
            }
        }

        private void slcPort1_Click(object sender, EventArgs e)
        {
            ScanPorts1();
        }

        private void ScanPorts2()
        {
            string[] ports = SerialPort.GetPortNames();

            //slcPort1.Items.Clear();

            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    // Check if the port is not already in the ComboBox items
                    if (!slcPort2.Items.Contains(port))
                    {
                        slcPort2.Items.Add(port);
                    }
                }
            }
            else
            {
                MessageBox.Show("No connected ports found.", "Port Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Optionally handle the case when no ports are found
            }
        }
        private void slcPort2_Click(object sender, EventArgs e)
        {
            ScanPorts2();
        }

        private void btnCOM1_Click(object sender, EventArgs e)
        {
            rtuMaster.Disconnect();

            configMB();

            UpdateConfigValue("Port1", slcPort1.Text);
            UpdateConfigValue("Baudrate1", slcBaudrate1.Text);
            UpdateConfigValue("Parity1", slcParity1.Text);
            UpdateConfigValue("Stopbits1", slcStopbits1.Text);
            UpdateConfigValue("Interval1", slcInterval1.Text);

            
        }

        private void btnCOM2_Click(object sender, EventArgs e)
        {
            //rtuSlave.StopListening();

            configSlave();

            UpdateConfigValue("Port2", slcPort2.Text);
            UpdateConfigValue("Baudrate2", slcBaudrate2.Text);
            UpdateConfigValue("Parity2", slcParity2.Text);
            UpdateConfigValue("Stopbits2", slcStopbits2.Text);

            
        }

        private void btnID_Click(object sender, EventArgs e)
        {
            ID = byte.Parse(txtID.Text);

            configSlave();

            UpdateConfigValue("ID", ID.ToString());

        }

        private void DTUconfig()
        {
            term = new terminal(slcPort2.Text, int.Parse(slcBaudrate2.Text));

            term.Port = slcPort2.Text;
            term.Baudrate = int.Parse(slcBaudrate2.Text);
            term.Parity = Parity.None;
            term.StopBits = StopBits.One;

            rtuSlave.StopListening();

            DTUused = true;

            string[] commands = new string[]
            {
                "+++",
                "AT+DTUMODE=2,1",
                "AT+IPPORT=\"sg-1-mqtt.iot-api.com\",1883,1",
                "AT+CLIENTID=\"Client-ID\",1",
                "AT+USERPWD=\"y50u8fwuusci3p29\",\"39VE7nK7cg\",1",
                "AT+SECSERVER=0,0,0,0",
                "AT+WILL=\"topic\",\"willmessage\",0,11",
                "AT+BLOCKINFO=1,0,0,0",
                "AT+CLEANSESSION=1,1",
                "AT+MQTTKEEP=120,1",
                "AT+MQTTSUB=1,\"data/stream/set\",0,1,1",
                "AT+MQTTSUB=0,\"\",0,2,1",
                "AT+MQTTSUB=0,\"\",0,3,1",
                "AT+MQTTSUB=0,\"\",0,4,1",
                "AT+MQTTSUB=0,\"\",0,5,1",
                "AT+MQTTPUB=1,\"data/stream\",0,1,1,1",
                "AT+MQTTPUBID=1,1,0,\"\"",
                "AT+MQTTPUB=0,\"\",0,1,2,1",
                "AT+MQTTPUBID=1,2,0,\"\"",
                "AT+MQTTSSL=0,1",
                "AT+KEEPALIVE=0,0,\"ping\",1",
                "AT+DTUID=1,2,0,\"regis002\",1",
                "AT+TCPMODBUS=0,1",
                "AT+TCPHEX=0,1",
                "AT&W"
            };

            foreach (string command in commands)
            {
                term.Write(command);
                txtWriteBottom(term.dataSender);
                txtWriteBottom(term.receivedData);
            }

            term.Close();

            DTUused = false;
        }

        

        private void softreboot()
        {
            term = new terminal(slcPort2.Text, int.Parse(slcBaudrate2.Text));

            term.Port = slcPort2.Text;
            term.Baudrate = int.Parse(slcBaudrate2.Text);
            term.Parity = Parity.None;
            term.StopBits = StopBits.One;

            rtuSlave.StopListening();

            DTUused = true;

            term.Write("AT+CFUN=1,1");
            txtWriteBottom(term.dataSender);

            if (term.receivedData != "")
            {
                txtWriteBottom("SUCCESS SOFT-REBOOT");
            }
            else
            {
                txtWriteBottom("UNSUCCESS SOFT-REBOOT");
            }

            
            txtWriteBottom(term.receivedData);

            term.Close();

            DTUused = false;
        }
        private void btnReboot_Click(object sender, EventArgs e)
        {
            softreboot();
        }

        private void btnConfigDTU_Click(object sender, EventArgs e)
        {
            DTUconfig();
        }

        private void writeRequest()
        {
            string datawrite = txtWrite.Text;
            txtWriteBottom("TX : " + datawrite, txtInfo2);

            //pisah berdasar space
            string[] Arr = datawrite.Split(' ');

            //pindah var
            string[] var = new string[Arr.Length];

            for (int i = 0; i < Arr.Length; i++)
            { // append data
                var[i] = Arr[i];
            }

            //rubah semua ke int
            int Id_req = Convert.ToInt32(var[0], 16);
            int Fc_req = Convert.ToInt32(var[1], 16);
            int Adr = Convert.ToInt32(var[2] + var[3], 16);
            int Qua = Convert.ToInt32(var[4] + var[5], 16);

            string crc = var[6] + "-" + var[7];

            byte ID = (byte)Id_req;
            byte FC = (byte)Fc_req;
            byte[] start = BitConverter.GetBytes((ushort)Adr);
            byte[] quantity = BitConverter.GetBytes((ushort)Qua);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(start);
                Array.Reverse(quantity);
            }

            byte[] datasend = new byte[] { ID, FC }.Concat(start).Concat(quantity).ToArray();

            byte[] getcrc = CRC(datasend);

            Console.WriteLine($"TX : {datawrite}");
            //Console.WriteLine("CRC: " + BitConverter.ToString(getcrc));
            //Console.WriteLine("CRC same?  " + (crc == BitConverter.ToString(getcrc)));

            if(crc == BitConverter.ToString(getcrc)) //crc saama?
            {
                rtuMaster.UnitIdentifier = (byte)Id_req;
                
                if (Fc_req == 3)
                {
                    try 
                    { 
                        rtuMaster.ReadHoldingRegisters(Adr, Qua);

                        byte[] data = rtuMaster.receiveData;

                        // Convert byte array to string with space-separated values
                        string getDat = string.Join(" ", data);
                        txtWriteBottom("RX : " + getDat + "\n", txtInfo2);

                    } 
                    catch { }
                }
                else if (Fc_req == 6)
                {
                    try 
                    {
                        rtuMaster.WriteSingleRegister(Adr, Qua);

                        byte[] data = rtuMaster.receiveData;

                        // Convert byte array to string with space-separated values
                        string getDat = string.Join(" ", data);
                        txtWriteBottom("RX : " + getDat + "\n", txtInfo2);

                    } 
                    catch { }
                }
                else 
                {
                    txtWriteBottom("\nFailed to write, Check the format\nUsing Function code 03|06 only!", txtInfo2);
                }
            }

        }


        private static byte[] CRC(byte[] data)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    }
                    else
                    {
                        crc = (ushort)(crc >> 1);
                    }
                }
            }

            return new byte[] { (byte)(crc & 0xFF), (byte)((crc >> 8) & 0xFF) };
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            writeRequest();
        }

        ///  per sensor

        private void defineparam1()
        {
            if (!string.IsNullOrEmpty(coeph.Text) &&
                !string.IsNullOrEmpty(maxph.Text) &&
                !string.IsNullOrEmpty(minph.Text) &&
                !string.IsNullOrEmpty(offph.Text))
            {
                saveparam1();

                Coe1 = double.Parse(coeph.Text, CultureInfo.InvariantCulture);
                Max1 = double.Parse(maxph.Text, CultureInfo.InvariantCulture);
                Min1 = double.Parse(minph.Text, CultureInfo.InvariantCulture);
                Off1 = double.Parse(offph.Text, CultureInfo.InvariantCulture);

                if (calb1ph) { textboxChange(cph, cal1ph.Text); UpdateConfigValue("cph", cal1ph.Text); calb1ph = false; /*RV1 = double.Parse(cph.Text, CultureInfo.InvariantCulture);*/ UpdateConfigValue("cv1", CV1.ToString()); UpdateConfigValue("cb1ph", cal1ph.Text); }
                if (calb2ph) { textboxChange(cph, cal2ph.Text); UpdateConfigValue("cph", cal2ph.Text); calb2ph = false; RV1 = double.Parse(cph.Text, CultureInfo.InvariantCulture); UpdateConfigValue("rv1", RV1.ToString()); UpdateConfigValue("cb2ph", cal2ph.Text); }
            }
        }


        private void defineparam2()
        {
            if (!string.IsNullOrEmpty(coetemp.Text) &&
                !string.IsNullOrEmpty(maxtemp.Text) &&
                !string.IsNullOrEmpty(mintemp.Text) &&
                !string.IsNullOrEmpty(offtemp.Text))
            {
                saveparam2();

                Coe2 = double.Parse(coetemp.Text, CultureInfo.InvariantCulture);
                Max2 = double.Parse(maxtemp.Text, CultureInfo.InvariantCulture);
                Min2 = double.Parse(mintemp.Text, CultureInfo.InvariantCulture);
                Off2 = double.Parse(offtemp.Text, CultureInfo.InvariantCulture);

                if (calb1temp) { textboxChange(ctemp, cal1temp.Text); UpdateConfigValue("ctemp", cal1temp.Text); calb1temp = false; /*RV1 = double.Parse(cph.Text, CultureInfo.InvariantCulture);*/ UpdateConfigValue("cv2", CV2.ToString()); UpdateConfigValue("cb1temp", cal1temp.Text); }
                if (calb2temp) { textboxChange(ctemp, cal2temp.Text); UpdateConfigValue("ctemp", cal2temp.Text); calb2temp = false; RV2 = double.Parse(ctemp.Text, CultureInfo.InvariantCulture); UpdateConfigValue("rv2", RV2.ToString()); UpdateConfigValue("cb2temp", cal2temp.Text); }
            }
        }
        private void defineparam3()
        {
            if (!string.IsNullOrEmpty(coefcl.Text) &&
                !string.IsNullOrEmpty(maxfcl.Text) &&
                !string.IsNullOrEmpty(minfcl.Text) &&
                !string.IsNullOrEmpty(offfcl.Text))
            {
                saveparam3();

                Coe3 = double.Parse(coefcl.Text, CultureInfo.InvariantCulture);
                Max3 = double.Parse(maxfcl.Text, CultureInfo.InvariantCulture);
                Min3 = double.Parse(minfcl.Text, CultureInfo.InvariantCulture);
                Off3 = double.Parse(offfcl.Text, CultureInfo.InvariantCulture);

                if (calb1fcl) { textboxChange(cfcl, cal1fcl.Text); UpdateConfigValue("cfcl", cal1fcl.Text); calb1fcl = false; /*RV1 = double.Parse(cph.Text, CultureInfo.InvariantCulture);*/ UpdateConfigValue("cv3", CV3.ToString()); UpdateConfigValue("cb1fcl", cal1fcl.Text); }
                if (calb2fcl) { textboxChange(cfcl, cal2fcl.Text); UpdateConfigValue("cfcl", cal1fcl.Text); calb2fcl = false; RV3 = double.Parse(cfcl.Text, CultureInfo.InvariantCulture); UpdateConfigValue("rv3", RV3.ToString()); UpdateConfigValue("cb2fcl", cal2fcl.Text); }
            }
        }

        private void defineparam4()
        {
            if (!string.IsNullOrEmpty(coetur.Text) &&
                !string.IsNullOrEmpty(maxtur.Text) &&
                !string.IsNullOrEmpty(mintur.Text) &&
                !string.IsNullOrEmpty(offtur.Text))
            {
                saveparam4();

                Coe4 = double.Parse(coetur.Text, CultureInfo.InvariantCulture);
                Max4 = double.Parse(maxtur.Text, CultureInfo.InvariantCulture);
                Min4 = double.Parse(mintur.Text, CultureInfo.InvariantCulture);
                Off4 = double.Parse(offtur.Text, CultureInfo.InvariantCulture);

                if (calb1tur) { textboxChange(ctur, cal1tur.Text); UpdateConfigValue("ctur", cal1tur.Text); calb1tur = false; /*RV1 = double.Parse(cph.Text, CultureInfo.InvariantCulture);*/ UpdateConfigValue("cv4", CV4.ToString()); UpdateConfigValue("cb1tur", cal1tur.Text); }
                if (calb2tur) { textboxChange(ctur, cal2tur.Text); UpdateConfigValue("ctur", cal2tur.Text); calb2tur = false; RV4 = double.Parse(ctur.Text, CultureInfo.InvariantCulture); UpdateConfigValue("rv4", RV4.ToString()); UpdateConfigValue("cb2tur", cal2tur.Text); }
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------
        
        private void upparam1()
        {
            saveparam1();

            Coe1 = double.Parse(coeph.Text, CultureInfo.InvariantCulture);
            Max1 = double.Parse(maxph.Text, CultureInfo.InvariantCulture);
            Min1 = double.Parse(minph.Text, CultureInfo.InvariantCulture);
            Off1 = double.Parse(offph.Text, CultureInfo.InvariantCulture);

            
        }


        private void upparam2()
        {
            saveparam2();

            Coe2 = double.Parse(coetemp.Text, CultureInfo.InvariantCulture);
            Max2 = double.Parse(maxtemp.Text, CultureInfo.InvariantCulture);
            Min2 = double.Parse(mintemp.Text, CultureInfo.InvariantCulture);
            Off2 = double.Parse(offtemp.Text, CultureInfo.InvariantCulture);
            
        }
        private void upparam3()
        {
            saveparam3();

            Coe3 = double.Parse(coefcl.Text, CultureInfo.InvariantCulture);
            Max3 = double.Parse(maxfcl.Text, CultureInfo.InvariantCulture);
            Min3 = double.Parse(minfcl.Text, CultureInfo.InvariantCulture);
            Off3 = double.Parse(offfcl.Text, CultureInfo.InvariantCulture);
            
        }

        private void upparam4()
        {
            saveparam4();

            Coe4 = double.Parse(coetur.Text, CultureInfo.InvariantCulture);
            Max4 = double.Parse(maxtur.Text, CultureInfo.InvariantCulture);
            Min4 = double.Parse(mintur.Text, CultureInfo.InvariantCulture);
            Off4 = double.Parse(offtur.Text, CultureInfo.InvariantCulture);

            
        }






        int pageSensor = 0;
        int cmd = 0;
        bool loginIn = true;

        private void phID_Click(object sender, EventArgs e)
        {
            cmd = 1; 
            pageSensor = 1;
            LOCK();
        }

        private void tempID_Click(object sender, EventArgs e)
        {
            cmd = 1;
            pageSensor = 2;
            LOCK();
        }

        private void turID_Click(object sender, EventArgs e)
        {
            cmd = 1;
            pageSensor = 3;
            LOCK();
        }

        private void fclID_Click(object sender, EventArgs e)
        {
            cmd = 1;
            pageSensor = 4;
            LOCK();
        }

        private void LOCK() 
        {
            // need safe login admin
            /*if (loginIn) 
            {*/
            if (cmd == 1 && pageSensor == 1) { UpdateConfigValue("idph", IDph.Text); phid = IDph.Text; }
            if (cmd == 1 && pageSensor == 2) { UpdateConfigValue("idtemp", IDtemp.Text); tempid = IDtemp.Text; }
            if (cmd == 1 && pageSensor == 3) { UpdateConfigValue("idfcl", IDfcl.Text); fclid = IDfcl.Text; }
            if (cmd == 1 && pageSensor == 4) { UpdateConfigValue("idtur", IDtur.Text); turid = IDtur.Text; }
            if (cmd == 2 && pageSensor == 1) { defineparam1(); }
            if (cmd == 2 && pageSensor == 2) { defineparam2(); }
            if (cmd == 2 && pageSensor == 3) { defineparam3(); }
            if (cmd == 2 && pageSensor == 4) { defineparam4(); }
            if (cmd == 3 && pageSensor == 1) { defineparam1(); }
            if (cmd == 3 && pageSensor == 2) { defineparam2(); }
            if (cmd == 3 && pageSensor == 3) { defineparam3(); }
            if (cmd == 3 && pageSensor == 4) { defineparam4(); }
            /*}
            else
            {
                //BackAll();
                //pgLoginSensor.Show();
                LOCK();
            }*/
            
        }

        private void saveparam1()
        {
            UpdateConfigValue("coe1", coeph.Text);
            UpdateConfigValue("max1", maxph.Text); 
            UpdateConfigValue("min1", minph.Text);
            UpdateConfigValue("off1", offph.Text);
        }

        private void saveparam2()
        {
            UpdateConfigValue("coe2", coetemp.Text);
            UpdateConfigValue("max2", maxtemp.Text);
            UpdateConfigValue("min2", mintemp.Text);
            UpdateConfigValue("off2", offtemp.Text);
        }

        private void saveparam3()
        {
            UpdateConfigValue("coe3", coefcl.Text);
            UpdateConfigValue("max3", maxfcl.Text);
            UpdateConfigValue("min3", minfcl.Text);
            UpdateConfigValue("off3", offfcl.Text);
        }

        private void saveparam4()
        {
            UpdateConfigValue("coe4", coetur.Text);
            UpdateConfigValue("max4", maxtur.Text);
            UpdateConfigValue("min4", mintur.Text);
            UpdateConfigValue("off4", offtur.Text);
        }

        private void setMinmax1_Click(object sender, EventArgs e)
        {
            defineparam1();
        }

        private void setMinMax2_Click(object sender, EventArgs e)
        {
            defineparam2();
        }


        private void setMinMax3_Click(object sender, EventArgs e)
        {
            defineparam3();
        }

        private void setMinMax4_Click(object sender, EventArgs e)
        {
            defineparam4();
        }

        bool calb1ph = false;
        bool calb2ph = false;
        bool calb1temp = false;
        bool calb2temp = false;
        bool calb1fcl = false;
        bool calb2fcl = false;
        bool calb1tur = false;
        bool calb2tur = false;

        private void calph1_Click(object sender, EventArgs e) { cal_ph1(); }
        private void calph2_Click(object sender, EventArgs e) { cal_ph2(); }

        //1315, 840

        void cal_ph1()
        {
            cmd = 2;
            pageSensor = 1;
            calb1ph = true;
            LOCK();
        }

        void cal_ph2()
        {
            cmd = 3;
            pageSensor = 1;
            calb2ph = true;
            LOCK();
        }

        private void caltemp1_Click(object sender, EventArgs e) { cal_temp1(); }
        private void caltemp2_Click(object sender, EventArgs e) { cal_temp2(); }

        void cal_temp1()
        {
            cmd = 2;
            pageSensor = 2;
            calb1temp = true;
            LOCK();
        }

        void cal_temp2()
        {
            cmd = 3;
            pageSensor = 2;
            calb2temp = true;
            LOCK();
        }


        private void calfcl1_Click(object sender, EventArgs e) { cal_fcl1(); }
        private void calfcl2_Click(object sender, EventArgs e) { cal_fcl2(); }

        void cal_fcl1()
        {
            cmd = 2;
            pageSensor = 3;
            calb1fcl = true;
            LOCK();
        }

        void cal_fcl2()
        {
            cmd = 3;
            pageSensor = 3;
            calb2fcl = true;
            LOCK();
        }

        private void caltur1_Click(object sender, EventArgs e) { cal_tur1(); }  
        private void caltur2_Click(object sender, EventArgs e) { cal_tur2(); }

        void cal_tur1()
        {
            cmd = 2;
            pageSensor = 4;
            calb1tur = true;
            LOCK();
        }

        void cal_tur2()
        {
            cmd = 3;
            pageSensor = 4;
            calb2tur = true;
            LOCK();
        }

        private void CLOSE_Click(object sender, EventArgs e)
        {
            // Display a confirmation dialog before closing the form
            DialogResult result = MessageBox.Show("Are you sure you want to close the form?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Close the form if the user confirms
                this.Close();
            }
        }

        #endregion

        #region ==================================|  Warning/Alarm  |===============================

        // |||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||| Alarm |||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||

        double phmerah1;
        double phkuning1a, phkuning1b;
        double phnormala, phnormalb;
        double phkuning2a, phkuning2b;
        double phmerah2;

        double tempmerah1;
        double tempkuning1a, tempkuning1b;
        double tempnormala, tempnormalb;
        double tempkuning2a, tempkuning2b;
        double tempmerah2;

        double fclmerah1;
        double fclkuning1a, fclkuning1b;
        double fclnormala, fclnormalb;
        double fclkuning2a, fclkuning2b;
        double fclmerah2;

        double turmerah1;
        double turkuning1a, turkuning1b;
        double turnormala, turnormalb;
        double turkuning2a, turkuning2b;
        double turmerah2;
                

        private void readAlarm()
        {
            string filename = "alarm.txt";
            string fullpath = Path.Combine(cpath,filename);

            
            string[] lines = File.ReadAllLines(fullpath);

            foreach (string line in lines)
            {
                // Split each line into key and value
                string[] parts = line.Split(':');

                if (parts.Length == 2)
                {
                    // Trim to remove extra spaces, and assign to variables
                    string name = parts[0].Trim();
                    string value = parts[1].Trim();

                    // Assign values to corresponding controls
                    switch (name)
                    {
                        case "ph":
                            string[] def1 = value.Split(';');
                            Console.WriteLine("ph ada : " + def1.Length);
                             
                            //Console.WriteLine($"{def.First()}  {def.Last()}");

                            if (def1.Length == 5)
                            {
                                PM1.Text = def1[0].Trim().ToString();
                                PK1.Text = def1[1].Trim().ToString();
                                PN.Text  = def1[2].Trim().ToString();
                                PK2.Text = def1[3].Trim().ToString();
                                PM2.Text = def1[4].Trim().ToString();
                            }   

                            break;

                        case "temp":
                            string[] def2 = value.Split(';');
                            Console.WriteLine("temp ada : " + def2.Length);

                            //Console.WriteLine($"{def.First()}  {def.Last()}");

                            if (def2.Length == 5)
                            {
                                TM1.Text = def2[0].Trim().ToString();
                                TK1.Text = def2[1].Trim().ToString();
                                TN.Text  = def2[2].Trim().ToString();
                                TK2.Text = def2[3].Trim().ToString();
                                TM2.Text = def2[4].Trim().ToString();
                            }

                            break;

                        case "fcl":
                            string[] def3 = value.Split(';');
                            Console.WriteLine("fcl ada : " + def3.Length);

                            //Console.WriteLine($"{def.First()}  {def.Last()}");

                            if (def3.Length == 5)
                            {
                                FM1.Text = def3[0].Trim().ToString();
                                FK1.Text = def3[1].Trim().ToString();
                                FN.Text  = def3[2].Trim().ToString();
                                FK2.Text = def3[3].Trim().ToString();
                                FM2.Text = def3[4].Trim().ToString();
                            }

                            break;

                        case "tur":
                            string[] def4 = value.Split(';');
                            Console.WriteLine("tur ada : " + def4.Length);

                            //Console.WriteLine($"{def.First()}  {def.Last()}");

                            if (def4.Length == 5)
                            {
                                UM1.Text = def4[0].Trim().ToString();
                                UK1.Text = def4[1].Trim().ToString();
                                UN.Text  = def4[2].Trim().ToString();
                                UK2.Text = def4[3].Trim().ToString();
                                UM2.Text = def4[4].Trim().ToString();
                            }

                            break;

                    }

                }
            }

            set2Param();

            
        }

        private void set2Param()
        {
            if (!string.IsNullOrEmpty(PM1.Text) &&
                !string.IsNullOrEmpty(PK1.Text) &&
                !string.IsNullOrEmpty(PN.Text)  &&
                !string.IsNullOrEmpty(PK2.Text) &&
                !string.IsNullOrEmpty(PM2.Text) &&
                !string.IsNullOrEmpty(TM1.Text) &&
                !string.IsNullOrEmpty(TK1.Text) &&
                !string.IsNullOrEmpty(TN.Text)  &&
                !string.IsNullOrEmpty(TK2.Text) &&
                !string.IsNullOrEmpty(TM2.Text) &&
                !string.IsNullOrEmpty(FM1.Text) &&
                !string.IsNullOrEmpty(FK1.Text) &&
                !string.IsNullOrEmpty(FN.Text)  &&
                !string.IsNullOrEmpty(FK2.Text) &&
                !string.IsNullOrEmpty(FM2.Text) &&
                !string.IsNullOrEmpty(UM1.Text) &&
                !string.IsNullOrEmpty(UK1.Text) &&
                !string.IsNullOrEmpty(UN.Text)  &&
                !string.IsNullOrEmpty(UK2.Text) &&
                !string.IsNullOrEmpty(UM2.Text))
            {

                getAlarm("ph" ,  PM1, PK1, PN, PK2, PM2, ref phmerah1, ref phkuning1a, ref phkuning1b, ref phnormala, ref phnormalb, ref phkuning2a, ref phkuning2b, ref phmerah2);
                getAlarm("temp", TM1, TK1, TN, TK2, TM2, ref tempmerah1, ref tempkuning1a, ref tempkuning1b, ref tempnormala, ref tempnormalb, ref tempkuning2a, ref tempkuning2b, ref tempmerah2);
                getAlarm("fcl", FM1, FK1, FN, FK2, FM2, ref fclmerah1, ref fclkuning1a, ref fclkuning1b, ref fclnormala, ref fclnormalb, ref fclkuning2a, ref fclkuning2b, ref fclmerah2);
                getAlarm("tur", UM1, UK1, UN, UK2, UM2, ref turmerah1, ref turkuning1a, ref turkuning1b, ref turnormala, ref turnormalb, ref turkuning2a, ref turkuning2b, ref turmerah2);

                /*string[] tvk1 = TK1.Text.Split('-');
                string[] tvk2 = TK2.Text.Split('-');
                string[] tvn = TN.Text.Split('-');

                if (!TM1.Text.Contains("-") && TM1.Text.Contains("<")) { tempmerah1 = double.Parse(TM1.Text.Replace("<", ""), CultureInfo.InvariantCulture); }
                if (!TM2.Text.Contains("-") && TM2.Text.Contains(">")) { tempmerah2 = double.Parse(TM2.Text.Replace(">", ""), CultureInfo.InvariantCulture); }

                if (tvk1.Length == 2 && TK1.Text.Contains("-")) { tempkuning1a = double.Parse(tvk1[0], CultureInfo.InvariantCulture); tempkuning1b = double.Parse(tvk1[1], CultureInfo.InvariantCulture); }
                if (tvk2.Length == 2 && TK2.Text.Contains("-")) { tempkuning2a = double.Parse(tvk2[0], CultureInfo.InvariantCulture); tempkuning2b = double.Parse(tvk2[1], CultureInfo.InvariantCulture); }
                if (tvn.Length == 2 && TN.Text.Contains("-")) { tempnormala = double.Parse(tvn[0], CultureInfo.InvariantCulture); tempnormalb = double.Parse(tvn[1], CultureInfo.InvariantCulture); }

                string[] fvk1 = FK1.Text.Split('-');
                string[] fvk2 = FK2.Text.Split('-');
                string[] fvn = FN.Text.Split('-');

                if (!FM1.Text.Contains("-") && FM1.Text.Contains("<")) { fclmerah1 = double.Parse(FM1.Text.Replace("<", ""), CultureInfo.InvariantCulture); }
                if (!FM2.Text.Contains("-") && FM2.Text.Contains(">")) { fclmerah2 = double.Parse(FM2.Text.Replace(">", ""), CultureInfo.InvariantCulture); }

                fclkuning1a = double.Parse(fvk1[0], CultureInfo.InvariantCulture); fclkuning1b = double.Parse(fvk1[1], CultureInfo.InvariantCulture);
                fclkuning2a = double.Parse(fvk2[0], CultureInfo.InvariantCulture); fclkuning2b = double.Parse(fvk2[1], CultureInfo.InvariantCulture);
                fclnormala = double.Parse(fvn[0], CultureInfo.InvariantCulture); fclnormalb = double.Parse(fvn[1], CultureInfo.InvariantCulture);

                string[] uvk1 = UK1.Text.Split('-');
                string[] uvk2 = UK2.Text.Split('-');
                string[] uvn = UN.Text.Split('-');


                if (!UM1.Text.Contains("-") && UM1.Text.Contains("<")) { turmerah1 = double.Parse(UM1.Text.Replace("<", ""), CultureInfo.InvariantCulture); }
                if (!UM2.Text.Contains("-") && UM2.Text.Contains(">")) { turmerah2 = double.Parse(UM2.Text.Replace(">", ""), CultureInfo.InvariantCulture); }

                if (uvk1.Length == 2 && UK1.Text.Contains("-")) { turkuning1a = double.Parse(uvk1[0], CultureInfo.InvariantCulture); turkuning1b = double.Parse(uvk1[1], CultureInfo.InvariantCulture); }
                else if (UK1.Text.Contains("-")) { turkuning1a = 0.0; turkuning1b = 0.0; }

                if (uvk2.Length == 2 && UK2.Text.Contains("-")) { turkuning2a = double.Parse(uvk2[0], CultureInfo.InvariantCulture); turkuning2b = double.Parse(uvk2[1], CultureInfo.InvariantCulture); }
                if (uvn.Length == 2 && UN.Text.Contains("-")) { turnormala = double.Parse(uvn[0], CultureInfo.InvariantCulture); turnormalb = double.Parse(uvn[1], CultureInfo.InvariantCulture); }*/
            }
        }

        private double doubleAdd(Guna2TextBox a, string replace = "")
        {
            string s = a.Text.Replace(replace, "");
            return double.Parse(s.Trim(), CultureInfo.InvariantCulture);
        }

        private double doubleAdd(string a)
        {
            return double.Parse(a.Trim(), CultureInfo.InvariantCulture);
        }

        private void success(string a)
        {
            Console.WriteLine($"{a} success append");
        }

        private void err(string a)
        {
            Console.WriteLine($"{a} ga masuk");
        }

        private void getAlarm(string name, Guna2TextBox merah1, Guna2TextBox kuning1, Guna2TextBox normal, Guna2TextBox kuning2, Guna2TextBox merah2, ref double vmerah1, ref double vkuning1a, ref double vkuning1b, ref double vnormala, ref double vnormalb, ref double vkuning2a, ref double vkuning2b, ref double vmerah2 )
        {
            if (merah1.Text.Contains("<") && !merah1.Text.Contains("-")) 
            { 
                vmerah1 = doubleAdd(merah1, "<");

                success($"{name}merah1");
            }
            else
            {
                err($"{name}merah1");
            }

            if (!kuning1.Text.Contains("<") && kuning1.Text.Contains("-"))
            {
                string[] pvk1 = kuning1.Text.Split('-');
                if(pvk1.Length == 2 && !string.IsNullOrEmpty(pvk1[0]) && !string.IsNullOrEmpty(pvk1[1]))
                {
                    vkuning1a = doubleAdd(pvk1[0]);
                    vkuning1b = doubleAdd(pvk1[1]);

                    success($"{name}kuning1");
                }
                else
                {
                    err($"{name}kuning1");
                }
                
            }
            else
            {
                err("phkuning1");
            }

            if (!normal.Text.Contains("<") && normal.Text.Contains("-"))
            {
                string[] pvk1 = normal.Text.Split('-');
                if (pvk1.Length == 2 && !string.IsNullOrEmpty(pvk1[0]) && !string.IsNullOrEmpty(pvk1[1]))
                {
                    vnormala = doubleAdd(pvk1[0]);
                    vnormalb = doubleAdd(pvk1[1]);

                    success($"{name}normal");
                }
                else
                {
                    err($"{name}normal");
                }

            }
            else
            {
                err($"{name}normal");
            }

            if (!kuning2.Text.Contains("<") && kuning2.Text.Contains("-"))
            {
                string[] pvk1 = kuning2.Text.Split('-');
                if (pvk1.Length == 2 && !string.IsNullOrEmpty(pvk1[0]) && !string.IsNullOrEmpty(pvk1[1]))
                {
                    vkuning2a = doubleAdd(pvk1[0]);
                    vkuning2b = doubleAdd(pvk1[1]);

                    success($"{name}kuning2");
                }
                else
                {
                    err($"{name}kuning2");
                }

            }
            else
            {
                err($"{name}kuning2");
            }


            if (merah2.Text.Contains(">") && !merah2.Text.Contains("-"))
            {
                vmerah2 = doubleAdd(merah2, ">");

                success($"{name}merah2");
            }
            else
            {
                err($"{name}merah2");
            }
        }

        private void updateAlarm_Click(object sender, EventArgs e)
        {
            readAlarm();
        }

        Image warning = Properties.Resources.warning_icon;
        Image danger = Properties.Resources.danger_icon;

        void ph_warning() { wph.Show(); wph.Image = warning; }
        void temp_warning() { wtemp.Show(); wtemp.Image = warning; }
        void fcl_warning() { wfcl.Show(); wfcl.Image = warning; }
        void tur_warning() { wtur.Show(); wtur.Image = warning; }
        void ph_danger() { wph.Show(); wph.Image = danger; }
        void temp_danger() { wtemp.Show(); wtemp.Image = danger; }
        void fcl_danger() { wfcl.Show(); wfcl.Image = danger; }
        void tur_danger() { wtur.Show(); wtur.Image = danger; }
        void ph_normal() { wph.Hide(); }
        void temp_normal() { wtemp.Hide(); }
        void fcl_normal() { wfcl.Hide(); }
        void tur_normal() { wtur.Hide(); }

        void alarm_ph()
        {
            double value = double.Parse(dph.Text);

            if (value < phmerah1)
            {
                ph_danger(); 
            }
            else if (value >= phkuning1a && value < phkuning1b)
            {
                ph_warning();
            }
            else if (value >= phnormala && value < phnormalb)
            {
                ph_normal(); 
            }
            else if (value >= phkuning2a && value < phkuning2b)
            {
                ph_warning();
            }
            else
            {
                ph_danger(); 
            }

            //Console.WriteLine($"ph {value} {phmerah1}  {phkuning1a}  {phkuning1b}  {phnormala}  {phnormalb} {phkuning2a}  {phkuning2b}");
        }

        void alarm_temp()
        {
            double value = double.Parse(dtemp.Text);

            if (value >= tempnormala && value < tempnormalb)
            {
                temp_normal();
            }
            else if (value >= tempkuning2a && value < tempkuning2b)
            {
                temp_warning();
            }
            else
            {
                temp_danger();
            }
            //Console.WriteLine($"temp {value}");
        }

        void  alarm_fcl()
        {
            double value = double.Parse(dfcl.Text);

            if (value < fclmerah1)
            {
                fcl_danger();
            }
            else if (value >= fclkuning1a && value < fclkuning1b)
            {
                fcl_warning();
            }
            else if (value >= fclnormala && value < fclnormalb)
            {
                fcl_normal();
            }
            else if (value >= fclkuning2a && value < fclkuning2b)
            {
                fcl_warning();
            }
            else
            {
                fcl_danger();
            }
            //Console.WriteLine($"fcl {value}");
        }

        void alarm_tur()
        {
            double value = double.Parse(dtur.Text);

            if (value >= turnormala && value < turnormalb)
            {
                tur_normal();
            }
            else if (value >= turkuning2a && value < turkuning2b)
            {
                tur_warning();
            }
            else
            {
                tur_danger();
            }
            //Console.WriteLine($"tur {value}");
        }

        private void dph_TextChanged(object sender, EventArgs e)
        {
            alarm_ph();
        }

        private void dtemp_TextChanged(object sender, EventArgs e)
        {
            alarm_temp();
        }

        private void dfcl_TextChanged(object sender, EventArgs e)
        {
            alarm_fcl();
        }

        private void dtur_TextChanged(object sender, EventArgs e)
        {
            alarm_tur();
        }

        

        #endregion

    }


    
    
}





#region ==================================|  Terminal.Net  |===============================
namespace Terminal
{
    public class terminal
    {
        private SerialPort sp;
        private ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

        public string dataSender = "";

        public terminal()
        {
            if (Port != string.Empty)
            {
                sp = new SerialPort();
                sp.PortName = Port;
                sp.BaudRate = Baudrate;
                sp.Parity = Parity;
                sp.StopBits = StopBits;
                sp.DataBits = 8;

                sp.DataReceived += SerialPort_DataReceived;
            }
        }

        public terminal(string portName, int baudRate = 9600)
        {
            sp = new SerialPort(portName, baudRate);
            sp.Parity = Parity;
            sp.StopBits = StopBits;
            sp.DataBits = 8;
            sp.DataReceived += SerialPort_DataReceived;
        }

        public void Open()
        {
            try
            {
                sp.PortName = Port;
                sp.BaudRate = Baudrate;
                sp.Parity = Parity;
                sp.StopBits = StopBits;
                sp.DataBits = 8;

                sp.ReadTimeout = 200;
                sp.WriteTimeout = 200;

                sp.Open();

                sp.DiscardOutBuffer();
                sp.DiscardInBuffer(); //cleaning
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            
        }

        public void Close()
        {
            if (sp.IsOpen)
            {
                sp.Close();
            }
        }

        public void Write(string data = "", int timeoutMilliseconds = 1500)
        {
            
            try
            {
                if (!sp.IsOpen)
                {
                    try { sp.Open(); } catch { }
                }

                dataReceivedEvent.Reset();
                receivedData = "";
                dataSender = "";

                sp.Write(data);
                dataSender = data + "\n";

                Console.WriteLine("Send : " + data);

                Console.WriteLine("Recieve : ");

                // Wait for the dataReceivedEvent or timeout
                if (!dataReceivedEvent.WaitOne(timeoutMilliseconds))
                {
                    receivedData = "Timeout: No data received within the specified time.";
                    Console.WriteLine(receivedData);
                }
            }
            catch (Exception ex)
            {
                receivedData = $"Error: {ex.GetType().Name} - {ex.Message}";
                Console.WriteLine(receivedData);
            }

        }

        public string WriteRead(string data, int timeoutMilliseconds = 5000)
        {
            if (sp.IsOpen)
            {
                sp.DiscardInBuffer();

                sp.Write(data);

                // Wait for data to be received or timeout
                DateTime startTime = DateTime.Now;
                receivedData = "";

                while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMilliseconds)
                {
                    if (sp.BytesToRead > 0)
                    {
                        receivedData += sp.ReadExisting();
                        // Optionally, you can add a condition to break out of the loop
                        // if a specific termination condition is met in the received data.
                    }
                    else
                    {
                        // Optional: Add a small delay to avoid high CPU usage in the loop
                        System.Threading.Thread.Sleep(10);
                    }
                }

                Console.WriteLine("Received data: " + receivedData);

                return receivedData;
            }
            else
            {
                Console.WriteLine("Serial port is not open. Call Open() before writing.");
                return null;
            }
        }

        public int Interval = 1000;
        public string Port = "";
        public int Baudrate = 9600;
        public Parity Parity = Parity.None;
        public StopBits StopBits = StopBits.One;
        public string receivedData = "";

        public void Clear()
        {
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Handle incoming data here
            try
            {
                receivedData = sp.ReadExisting();
                Console.WriteLine(receivedData);

                // Set the event to signal that data has been received
                dataReceivedEvent.Set();
            }
            catch { }
        }

    }
}

#endregion


#region ==================================|  Modbus.Net  |===============================

namespace ModbusRTU
{

    public class Modbus
    {
        private SerialPort sp;
        private ManualResetEvent dataReceivedEvent = new ManualResetEvent(false);

        public string dataSender = "";

        public Modbus()
        {
            if (SerialPort != string.Empty)
            {
                sp = new SerialPort();
                sp.PortName = SerialPort;
                sp.BaudRate = Baudrate;
                sp.Parity = Parity;
                sp.StopBits = StopBits;
                sp.DataBits = 8;

                sp.DataReceived += SerialPort_DataReceived;
            }
        }

        public Modbus(string portName, int baudRate = 9600)
        {
            sp = new SerialPort(portName, baudRate);
            sp.Parity = Parity;
            sp.StopBits = StopBits;
            sp.DataBits = 8;
            //sp.DataReceived += SerialPort_DataReceived;
        }

        public bool Connected = false;

        public void Connect()
        {
            try
            {
                sp.PortName = SerialPort;
                sp.BaudRate = Baudrate;
                sp.Parity = Parity;
                sp.StopBits = StopBits;
                sp.DataBits = 8;

                sp.ReadTimeout = 200;
                sp.WriteTimeout = 200;

                sp.Open();

                Connected = true;

                sp.DiscardOutBuffer();
                sp.DiscardInBuffer(); //cleaning
            }
            catch (Exception ex)
            {
                string err = $"Error: {ex.GetType().Name} - {ex.Message}";
                Console.WriteLine(err);
            }

        }

        public void Disconnect()
        {
            if (sp.IsOpen)
            {
                sp.Close();

                Connected = false;
            }
        }

        public byte UnitIdentifier = 1;
        public int Timeout = 1500;

        public int FunctionCode = 3;


        public void Write(int start_register, int quantity_register)
        {
            byte fc = (byte)FunctionCode;
            string data = "";
            int timeoutMilliseconds = Timeout;
            try
            {
                if (!sp.IsOpen)
                {
                    try { sp.Open(); } catch { }
                }

                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                dataReceivedEvent.Reset();
                receivedData = "";
                dataSender = "";

                byte[] start = BitConverter.GetBytes((ushort)start_register);
                byte[] quantity = BitConverter.GetBytes((ushort)quantity_register);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(start);
                    Array.Reverse(quantity);
                }

                data += UnitIdentifier.ToString("X2") + " ";
                data += fc.ToString("X2") + " ";
                data += start[0].ToString("X2") + " " + start[1].ToString("X2") + " ";
                data += quantity[0].ToString("X2") + " " + quantity[1].ToString("X2");

                string senddata = data + " " + stringCRC(data);

                //Console.WriteLine("senddata : " + senddata);
                byte[] tosend = ConvertToByteArray(senddata);

                //foreach (byte b in tosend) { Console.Write(b.ToString("X2")); }

                StringBuilder receivedDataBuilder = new StringBuilder();

                //Console.WriteLine("Send : " + senddata);

                sp.Write(tosend, 0, tosend.Length);
                dataSender = senddata + "\n";

                byte[] response = new byte[5 + (2 * quantity_register)];
                int bytesRead = 0;

                // Set the ReadTimeout property of the SerialPort
                sp.ReadTimeout = Timeout;

                // Create a Stopwatch to measure elapsed time
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (bytesRead < response.Length)
                {
                    try
                    {
                        response[bytesRead] = (byte)(sp.ReadByte());
                        //Console.Write(response[bytesRead]); Console.Write(" ");

                        if (bytesRead > 2 && bytesRead < response.Length - 2)
                        {
                            receivedDataBuilder.Append(response[bytesRead].ToString("X2")); // Assuming ASCII encoding
                            //receivedDataBuilder.Append(' ');
                        }

                        bytesRead++;
                    }
                    catch (TimeoutException ex)
                    {
                        // Handle timeout exception
                        receivedData = $"Error: {ex.GetType().Name} - {ex.Message}";
                        Console.WriteLine(receivedData);
                        break; // Exit the loop
                    }

                    // Check if the elapsed time exceeds the timeout
                    if (stopwatch.ElapsedMilliseconds >= Timeout)
                    {
                        Console.WriteLine("Timeout occurred while reading from serial port.");
                        break; // Exit the loop
                    }
                }

                // Stop the Stopwatch
                stopwatch.Stop();

                receivedData = BitConverter.ToString(response, 0, bytesRead);
                if (response != null && receivedData != string.Empty)
                {
                    receiveData = response;
                    OnlyResult = receivedDataBuilder.ToString();
                    receivedData = receivedData.Replace("-", " ");
                    //Console.WriteLine("Recieve : " + receivedData);
                }


            }
            catch (Exception ex)
            {
                receivedData = $"Error: {ex.GetType().Name} - {ex.Message}";
                Console.WriteLine(receivedData);
            }

        }

        static byte[] ConvertToByteArray(string dataString)
        {
            // Split the string by spaces and filter out empty entries
            string[] parts = dataString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] byteArray = new byte[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out byte parsedByte))
                {
                    byteArray[i] = parsedByte; // Parse each part into a byte
                }
                else
                {
                    Console.WriteLine($"Unable to parse '{parts[i]}' as byte.");
                    // You might want to handle this case differently based on your requirements
                }
            }

            return byteArray;
        }

        public string OnlyResult;

        static string stringCRC(string input)
        {
            //Console.WriteLine(input);
            string[] hexValues = input.Split(' ');
            byte[] dataBytes = new byte[hexValues.Length];

            for (int i = 0; i < hexValues.Length; i++)
            {
                dataBytes[i] = Convert.ToByte(hexValues[i], 16);
            }

            ushort crc = 0xFFFF; // Initial CRC value

            foreach (byte dataByte in dataBytes)
            {
                crc ^= dataByte;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001; // XOR with the CRC-16 polynomial
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return $"{crc & 0xFF:X2} {crc >> 8:X2}"; // Format as two separate bytes with space
        }

        public int Interval = 1000;
        public string SerialPort = "";
        public int Baudrate = 9600;
        public Parity Parity = Parity.None;
        public StopBits StopBits = StopBits.One;
        public string receivedData = "";

        public byte[] receiveData;

        public void Clear()
        {
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Handle incoming data here
            try
            {
                receivedData = sp.ReadExisting();
                Console.WriteLine(receivedData);

                // Set the event to signal that data has been received
                dataReceivedEvent.Set();
            }
            catch { }
        }

    }
}

#endregion










// closing app, pc ikut mati, masuk ke form closing (?)
// atau hanya pas close secara biasa saja?


// set app ke ontop, jadi anggap tidak ada windows dan langsung ke aplikasi saja