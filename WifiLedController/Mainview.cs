using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WifiLedController {
    public partial class Mainview : Form
    {
        #region functions
        static int multicastPort = 48899;
        UdpClient receive = new UdpClient(multicastPort);
        static string requestString = "HF-A11ASSISTHREAD";//String required to get led controllers to answer.
        List<WifiLed> foundLeds = new List<WifiLed>();
        List<WifiLed> activeLeds = new List<WifiLed>();
        WifiLed selectedLed = null;
        int DummyCount = 0;
        ScreenProcessor sp = new ScreenProcessor();
        XmlSettings xmlSettings = new XmlSettings();

        public Mainview()
        {
            InitializeComponent();
            xmlSettings.Load();
            //WifiLedController.Properties.Settings.Default.Initialize(new System.Configuration.SettingsContext(), new System.Configuration.SettingsPropertyCollection(),new System.Configuration.SettingsProviderCollection());
            findLeds();
            FillFunctionList();
            updateCalculations();
            SetupAmbianceSettings();
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.WorkerReportsProgress = true;
        }


        private void SetupAmbianceSettings() {
            (int X, int Xwidth, int Xstride, int Y, int Yheight, int Ystride) = xmlSettings.ReadAmbianceSettings();
            numericUpDownAdvancedX.Value = X;
            numericUpDownAdvancedXwidth.Value = Xwidth;
            numericUpDownAdvancedXstride.Value = Xstride;
            numericUpDownAdvancedY.Value = Y;
            numericUpDownAdvancedYheight.Value = Yheight;
            numericUpDownAdvancedYstride.Value = Ystride;
        }
        /* Fills the function list with known functions. Uses custom DisplayValuePair to display nicely and store the relevant byte value.
         */
        private void FillFunctionList() {
            listBoxFunctions.BeginUpdate();
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Seven Color Cross Fade", 0x25));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Red Gradual Change", 0x26));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Green Gradual Change", 0x27));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Blue Gradual Change", 0x28));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Yellow Gradual Change", 0x29));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Cyan Gradual Change", 0x2A));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Purple Gradual Change", 0x2B));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("White Gradual Change", 0x2C));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Red Green Cross Fade", 0x2D));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Red Blue Cross Fade", 0x2E));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Green Blue Cross Fade", 0x2F));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Seven Color Strobe Flash", 0x30));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Red Strobe Flash", 0x31));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Green Strobe Flash", 0x32));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Blue Strobe Flash", 0x33));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Yellow Strobe Flash", 0x34));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Cyan Strobe Flash", 0x35));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Purple Strobe Flash", 0x36));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("White Strobe Flash", 0x37));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("Seven Color Jumping Change", 0x38));
            listBoxFunctions.Items.Add(new DisplayValuePair<string, byte>("No Function", 0x61));
            listBoxFunctions.EndUpdate();
        }

        public void findLeds() {
            Debug.WriteLine("Requesting devices to register themselves.");
            byte[] bytestring = Encoding.ASCII.GetBytes(requestString);
            receive.EnableBroadcast = true; //important! enables multicast

            receive.Send(bytestring, bytestring.Length, new IPEndPoint(IPAddress.Broadcast, 48899));
            
            receive.BeginReceive(new AsyncCallback(response), null);
        }

        private async void response(IAsyncResult res) {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, multicastPort);
            byte[] received = receive.EndReceive(res, ref RemoteIpEndPoint);
            receive.BeginReceive(new AsyncCallback(response), null);

            //Process codes
            string device = Encoding.ASCII.GetString(received);
            Debug.WriteLine(device); //Example format: 192.168.1.234,ACCF239EF102,HF-LPB100-ZJ200
            string[] data = device.Split(',');

            if(data.Length == 3) {//A string we are interested in contains three parts
                IPAddress iP = IPAddress.Parse(data[0]);//We only care about the first part, which is an ip
                //iPAddresses.Add(iP);//Yay sets! 
                
                WifiLed led = new WifiLed(iP,data[1]);
                //New Led found
                if (!foundLeds.Contains(led)) {
                    Debug.WriteLine("Found new a device at: " + data[0] + "| contained in foundLeds: " + foundLeds.Contains(led));
                    foundLeds.Add(led);
                    //checkedListBoxDevices.Items.Add(led);
                    //this.Invoke((MethodInvoker)(() => OutputBox.Items.Add(engineOutput)));

                    //check if we have a custom name for this led
                    string custom = xmlSettings.FindCustomName(led.macAddress);
                    if (custom != null && custom != "") {
                        Debug.WriteLine("[Mainview.respons] custom = {0}", custom);
                        led.name = custom;
                    }

                    this.Invoke((MethodInvoker)(() => checkedListBoxDevices.Items.Add(led)));
                    if (checkedListBoxDevices.Items.Count == 1) {//first item is selected. so that we have something to work with, supress event 
                        this.checkedListBoxDevices.SelectedValueChanged -= new EventHandler(checkedListBoxDevices_SelectedIndexChanged);
                        this.Invoke((MethodInvoker)(() => checkedListBoxDevices.SetSelected(0, true)));
                        this.checkedListBoxDevices.SelectedValueChanged += new EventHandler(checkedListBoxDevices_SelectedIndexChanged);
                    }
                    //add event listener for this led
                    led.WifiLedUpdated += WifiLedUpdatedListener;

                    //led.getStatus();
                    //Task task = new Task(led.getStatus);
                    //await Task.Run(() => led.getStatus());
                    //Thread thread = new Thread(led.getStatus);
                    //thread.Start();
                    //Gather information asynchrounously
                    Task.Factory.StartNew(() => led.GetStatus());
                }

            }
            
        }

        private void updateViewColor(byte red, byte green, byte blue, byte warmWhite) {
            this.numericUpDownRed.ValueChanged -= new EventHandler(this.numericUpDownRed_ValueChanged);
            this.numericUpDownGreen.ValueChanged -= new EventHandler(this.numericUpDownGreen_ValueChanged);
            this.numericUpDownBlue.ValueChanged -= new EventHandler(this.numericUpDownBlue_ValueChanged);
            this.redBar.Scroll -= new EventHandler(this.redBar_Scroll);
            this.greenBar.Scroll -= new EventHandler(this.greenBar_Scroll);
            this.blueBar.Scroll -= new EventHandler(this.blueBar_Scroll);

            this.numericUpDownWarmWhite.ValueChanged -= new EventHandler(this.numericUpDownWarmWhite_ValueChanged);
            this.trackBarWarmWhite.Scroll -= new EventHandler(this.trackBarWarmWhite_Scroll);

            this.trackBarFunctionSpeed.Scroll -= new EventHandler(this.trackBarFunctionSpeed_Scroll);
            this.listBoxFunctions.SelectedIndexChanged -= new EventHandler(this.listBoxFunctions_SelectedIndexChanged);

            //Set all 
            //groupBoxCurrentDevice.Text = selectedLed.name;
            pictureBox1.BackColor = Color.FromArgb(red, green, blue);
            numericUpDownRed.Value = red;
            redBar.Value = red;
            numericUpDownGreen.Value = green;
            greenBar.Value = green;
            numericUpDownBlue.Value = blue;
            blueBar.Value = blue;

            numericUpDownWarmWhite.Value = warmWhite;
            trackBarWarmWhite.Value = warmWhite;

            //set handlers back up
            this.numericUpDownRed.ValueChanged += new EventHandler(this.numericUpDownRed_ValueChanged);
            this.numericUpDownGreen.ValueChanged += new EventHandler(this.numericUpDownGreen_ValueChanged);
            this.numericUpDownBlue.ValueChanged += new EventHandler(this.numericUpDownBlue_ValueChanged);
            this.redBar.Scroll += new EventHandler(this.redBar_Scroll);
            this.greenBar.Scroll += new EventHandler(this.greenBar_Scroll);
            this.blueBar.Scroll += new EventHandler(this.blueBar_Scroll);

            this.numericUpDownWarmWhite.ValueChanged += new EventHandler(this.numericUpDownWarmWhite_ValueChanged);
            this.trackBarWarmWhite.Scroll += new EventHandler(this.trackBarWarmWhite_Scroll);

            this.trackBarFunctionSpeed.Scroll += new EventHandler(this.trackBarFunctionSpeed_Scroll);
            this.listBoxFunctions.SelectedIndexChanged += new EventHandler(this.listBoxFunctions_SelectedIndexChanged);
        }

        private void updateActiveWifiLeds() {
            if (activeLeds.Count < 1 && selectedLed != null) {
                selectedLed.UpdateRGBWW((byte)numericUpDownRed.Value, (byte)numericUpDownGreen.Value, (byte)numericUpDownBlue.Value, (byte)numericUpDownWarmWhite.Value);
            } else {
                foreach (WifiLed led in activeLeds) {
                    led.UpdateRGBWW((byte)numericUpDownRed.Value, (byte)numericUpDownGreen.Value, (byte)numericUpDownBlue.Value, (byte)numericUpDownWarmWhite.Value);
                }
            }
        }

        private void updateView() {//Passively update all element to match selected element
            //Temporary removal of eventhandlers to supress accidental changes or colour changes
            this.numericUpDownRed.ValueChanged -= new EventHandler(this.numericUpDownRed_ValueChanged);
            this.numericUpDownGreen.ValueChanged -= new EventHandler(this.numericUpDownGreen_ValueChanged);
            this.numericUpDownBlue.ValueChanged -= new EventHandler(this.numericUpDownBlue_ValueChanged);
            this.redBar.Scroll -= new EventHandler(this.redBar_Scroll);
            this.greenBar.Scroll -= new EventHandler(this.greenBar_Scroll);
            this.blueBar.Scroll -= new EventHandler(this.blueBar_Scroll);

            this.numericUpDownWarmWhite.ValueChanged -= new EventHandler(this.numericUpDownWarmWhite_ValueChanged);
            this.trackBarWarmWhite.Scroll -= new EventHandler(this.trackBarWarmWhite_Scroll);

            this.trackBarFunctionSpeed.Scroll -= new EventHandler(this.trackBarFunctionSpeed_Scroll);
            this.listBoxFunctions.SelectedIndexChanged -= new EventHandler(this.listBoxFunctions_SelectedIndexChanged);

            //Set all 
            //groupBoxCurrentDevice.Text = selectedLed.name;
            pictureBox1.BackColor = Color.FromArgb(selectedLed.Red, selectedLed.Green, selectedLed.Blue);
            numericUpDownRed.Value = selectedLed.Red;
            redBar.Value = selectedLed.Red;
            numericUpDownGreen.Value = selectedLed.Green;
            greenBar.Value = selectedLed.Green;
            numericUpDownBlue.Value = selectedLed.Blue;
            blueBar.Value = selectedLed.Blue;

            numericUpDownWarmWhite.Value = selectedLed.WarmWhite;
            trackBarWarmWhite.Value = selectedLed.WarmWhite;

            textBoxSettingsName.Text = selectedLed.name;

            controlsToggler();
            //check if we should have control
            if (activeLeds.Count < 1 || activeLeds.Contains(selectedLed)) {
                if (selectedLed.Active) {//set on off buttons correctly
                    buttonOn.Enabled = false;
                    buttonOff.Enabled = true;
                } else {
                    buttonOn.Enabled = true;
                    buttonOff.Enabled = false;
                }
            }

            //set handlers back up
            this.numericUpDownRed.ValueChanged += new EventHandler(this.numericUpDownRed_ValueChanged);
            this.numericUpDownGreen.ValueChanged += new EventHandler(this.numericUpDownGreen_ValueChanged);
            this.numericUpDownBlue.ValueChanged += new EventHandler(this.numericUpDownBlue_ValueChanged);
            this.redBar.Scroll += new EventHandler(this.redBar_Scroll);
            this.greenBar.Scroll += new EventHandler(this.greenBar_Scroll);
            this.blueBar.Scroll += new EventHandler(this.blueBar_Scroll);

            this.numericUpDownWarmWhite.ValueChanged += new EventHandler(this.numericUpDownWarmWhite_ValueChanged);
            this.trackBarWarmWhite.Scroll += new EventHandler(this.trackBarWarmWhite_Scroll);

            this.trackBarFunctionSpeed.Scroll += new EventHandler(this.trackBarFunctionSpeed_Scroll);
            this.listBoxFunctions.SelectedIndexChanged += new EventHandler(this.listBoxFunctions_SelectedIndexChanged);
        }

        private void controlsToggler() {//If working in in single mode or device is in the active list
            if ((checkedListBoxDevices.CheckedItems.Count < 1) || checkedListBoxDevices.CheckedItems.Contains(selectedLed)) {
                pictureBox1.Enabled = true;
                numericUpDownRed.Enabled = true;
                redBar.Enabled = true;
                numericUpDownGreen.Enabled = true;
                greenBar.Enabled = true;
                numericUpDownBlue.Enabled = true;
                blueBar.Enabled = true;
                numericUpDownWarmWhite.Enabled = true;
                trackBarWarmWhite.Enabled = true;
                buttonOn.Enabled = true;
                buttonOff.Enabled = true;
                buttonRefreshList.Enabled = true;
                listBoxFunctions.Enabled = true;
                trackBarFunctionSpeed.Enabled = true;
            } else {
                pictureBox1.Enabled = false;
                numericUpDownRed.Enabled = false;
                redBar.Enabled = false;
                numericUpDownGreen.Enabled = false;
                greenBar.Enabled = false;
                numericUpDownBlue.Enabled = false;
                blueBar.Enabled = false;
                numericUpDownWarmWhite.Enabled = false;
                trackBarWarmWhite.Enabled = false;
                buttonOn.Enabled = false;
                buttonOff.Enabled = false;
                buttonRefreshList.Enabled = false;
                listBoxFunctions.Enabled = false;
                trackBarFunctionSpeed.Enabled = false;
            }
        }

        private void updateRed(int red) {
            this.numericUpDownRed.ValueChanged -= new EventHandler(this.numericUpDownRed_ValueChanged);
            this.redBar.Scroll -= new EventHandler(this.redBar_Scroll);

            //Set red
            pictureBox1.BackColor = Color.FromArgb(red, (int)numericUpDownGreen.Value, (int)numericUpDownBlue.Value);
            numericUpDownRed.Value = red;
            redBar.Value = red;
            //set handlers back up
            this.numericUpDownRed.ValueChanged += new EventHandler(this.numericUpDownRed_ValueChanged);
            this.redBar.Scroll += new EventHandler(this.redBar_Scroll);
        }


        private void updateGreen(int green) {
            this.numericUpDownGreen.ValueChanged -= new EventHandler(this.numericUpDownGreen_ValueChanged);
            this.greenBar.Scroll -= new EventHandler(this.greenBar_Scroll);

            pictureBox1.BackColor = Color.FromArgb((int)numericUpDownRed.Value, green, (int)numericUpDownBlue.Value);
            numericUpDownGreen.Value = green;
            greenBar.Value = green;
            //set handlers back up

            this.numericUpDownGreen.ValueChanged += new EventHandler(this.numericUpDownGreen_ValueChanged);
            this.greenBar.Scroll += new EventHandler(this.greenBar_Scroll);
        }
        private void updateBlue(int blue) {
            this.numericUpDownBlue.ValueChanged -= new EventHandler(this.numericUpDownBlue_ValueChanged);
            this.blueBar.Scroll -= new EventHandler(this.blueBar_Scroll);

            pictureBox1.BackColor = Color.FromArgb((int)numericUpDownRed.Value, (int)numericUpDownGreen.Value, blue);
            numericUpDownBlue.Value = blue;
            blueBar.Value = blue;

            //set handlers back up
            this.numericUpDownBlue.ValueChanged += new EventHandler(this.numericUpDownBlue_ValueChanged);
            this.blueBar.Scroll += new EventHandler(this.blueBar_Scroll);
        }

        private void updateWarmWhite(int warmwhite) {
            this.numericUpDownWarmWhite.ValueChanged -= new EventHandler(this.numericUpDownWarmWhite_ValueChanged);
            this.trackBarWarmWhite.Scroll -= new EventHandler(this.trackBarWarmWhite_Scroll);

            numericUpDownWarmWhite.Value = warmwhite;
            trackBarWarmWhite.Value = warmwhite;

            //set handlers back up
            this.numericUpDownWarmWhite.ValueChanged += new EventHandler(this.numericUpDownWarmWhite_ValueChanged);
            this.trackBarWarmWhite.Scroll += new EventHandler(this.trackBarWarmWhite_Scroll);
        }

        //Calculate the number of points that we actually check.
        private void updateCalculations() {
            int xLines = (int)Math.Truncate(numericUpDownAdvancedXwidth.Value / numericUpDownAdvancedXstride.Value);
            int ycolumns = (int)Math.Truncate(numericUpDownAdvancedYheight.Value / numericUpDownAdvancedYstride.Value);
            int calculations = xLines * ycolumns;
            labelAdvancedCalcAmount.Text = calculations + " calculations required.";
            xmlSettings.AddAmbianceSettings((int)numericUpDownAdvancedX.Value, (int)numericUpDownAdvancedXwidth.Value, (int)numericUpDownAdvancedXstride.Value,
                (int)numericUpDownAdvancedY.Value, (int)numericUpDownAdvancedYheight.Value, (int)numericUpDownAdvancedYstride.Value);
        }

        #endregion
        #region eventlisteners

        private void WifiLedUpdatedListener(object sender, EventArgs e) {
            if((WifiLed)sender == selectedLed) {
                updateView();
            }
        }

        private void numericUpDownRed_ValueChanged(object sender, EventArgs e){
            updateRed((int)numericUpDownRed.Value);
            updateActiveWifiLeds();
            //Debug.WriteLine("[Red_ValueChanged] Sender is " + sender.ToString());
        }

        private void redBar_Scroll(object sender, EventArgs e){
            updateRed(redBar.Value);
            updateActiveWifiLeds();
        }

        private void greenBar_Scroll(object sender, EventArgs e){
            updateGreen(greenBar.Value);
            updateActiveWifiLeds();
        }

        private void numericUpDownGreen_ValueChanged(object sender, EventArgs e){
            updateGreen((int)numericUpDownGreen.Value);
            updateActiveWifiLeds();
        }

        private void blueBar_Scroll(object sender, EventArgs e){
            updateBlue(blueBar.Value);
            updateActiveWifiLeds();
        }

        private void numericUpDownBlue_ValueChanged(object sender, EventArgs e){
            updateBlue((int)numericUpDownBlue.Value);
            updateActiveWifiLeds();
        }

        private void trackBarWarmWhite_Scroll(object sender, EventArgs e) {
            updateWarmWhite(trackBarWarmWhite.Value);
            updateActiveWifiLeds();

        }

        private void numericUpDownWarmWhite_ValueChanged(object sender, EventArgs e) {
            updateWarmWhite((int)numericUpDownWarmWhite.Value);
            updateActiveWifiLeds();
        }

        private void buttonOn_Click(object sender, EventArgs e){
            //send turn on command to appopriate leds
            if(activeLeds.Count < 1 && selectedLed != null) {
                selectedLed.TurnOn();
            } else { 
                foreach(WifiLed led in activeLeds) {
                    //Debug.WriteLine("[ButtonOn] Turning on Led: {0}",led);
                    Task.Factory.StartNew(() => led.TurnOn());
                }
            }
            //switch which button is active
            buttonOff.Enabled = true;
            buttonOn.Enabled = false;
        }

        private void buttonOff_Click(object sender, EventArgs e) {
            if (activeLeds.Count < 1 && selectedLed != null) {
                selectedLed.TurnOff();
            } else {
                foreach (WifiLed led in activeLeds) {
                    Task.Factory.StartNew(() => led.TurnOff());
                }
            }
            buttonOff.Enabled = false;
            buttonOn.Enabled = true;
        }

        private void pictureBox1_Click(object sender, EventArgs e) {
            if (colorDialog1.ShowDialog() == DialogResult.OK) {
                pictureBox1.BackColor = colorDialog1.Color;
                updateViewColor(pictureBox1.BackColor.R, pictureBox1.BackColor.G, pictureBox1.BackColor.B, (byte)numericUpDownWarmWhite.Value);
                updateActiveWifiLeds();
            }
        }
        private void buttonRefresh_Click(object sender, EventArgs e) {
            findLeds();
        }



        private void RefreshButton_Click(object sender, EventArgs e) {               
            selectedLed.GetStatus();
        }

        private void checkedListBoxDevices_SelectedIndexChanged(object sender, EventArgs e) {

            selectedLed = (WifiLed)checkedListBoxDevices.SelectedItem;
            updateView();
            /*
            Debug.WriteLine("Index===============================");
            Debug.WriteLine("[{0}] ON = {1}",selectedLed.name, selectedLed.Active);
            Debug.WriteLine("[{0}] Red = {1}", selectedLed.name, selectedLed.Red);
            Debug.WriteLine("[{0}] Green = {1}", selectedLed.name, selectedLed.Green);
            Debug.WriteLine("[{0}] Blue = {1}", selectedLed.name, selectedLed.Blue);
            Debug.WriteLine("[{0}] IP = {1}", selectedLed.name, selectedLed.iPAddress);
            Debug.WriteLine("[{0}] Name = {1}", selectedLed.name, selectedLed.name);
            Debug.WriteLine("===============================");
            */
        }
        private void checkedListBoxDevices_ItemCheck(object sender, ItemCheckEventArgs e) {
            //activeLeds.Clear();
            Debug.WriteLine(e.NewValue);
            if(e.NewValue == CheckState.Checked) {//new check
                activeLeds.Add((WifiLed)checkedListBoxDevices.Items[e.Index]);
            } else {// uncheck
                activeLeds.Remove((WifiLed)checkedListBoxDevices.Items[e.Index]);
            }
         
            foreach(WifiLed led in activeLeds) {
                Debug.WriteLine("[activeLeds] : " + led);
            }

        }

        private void button1_Click(object sender, EventArgs e) {
            foreach (WifiLed led in activeLeds) {
                Debug.WriteLine("[DEBUG activeLeds] : " + led);
            }
        }

        private void buttonAddDummy_Click(object sender, EventArgs e) {
            DummyWifiLed newDummy = new DummyWifiLed(IPAddress.Parse("255.255.255.255"), "Dummy" + DummyCount);
            string custom = xmlSettings.FindCustomName(newDummy.macAddress);
            if (custom != null && custom != "") {
                Debug.WriteLine("[buttonAddDummy_Click] custom = {0}", custom);
                newDummy.name = custom;
            }
            DummyCount++;
            foundLeds.Add(newDummy);
            checkedListBoxDevices.Items.Add(newDummy, false);
        }

        private void listBoxFunctions_SelectedIndexChanged(object sender, EventArgs e) {
            DisplayValuePair<string, byte> function = (DisplayValuePair<string, byte>)listBoxFunctions.SelectedItem;
            byte speed = (byte)trackBarFunctionSpeed.Value;
            if (activeLeds.Count < 1) {
                selectedLed.UpdateFunction(function.Value, speed);
            } else {
                foreach (WifiLed led in activeLeds) {
                    led.UpdateFunction(function.Value, speed);
                }
            }
        }

        private void trackBarFunctionSpeed_Scroll(object sender, EventArgs e) {
            DisplayValuePair<string, byte> function = (DisplayValuePair<string, byte>)listBoxFunctions.SelectedItem;
            byte speed = (byte)trackBarFunctionSpeed.Value;
            if (activeLeds.Count < 1) {
                selectedLed.UpdateFunction(function.Value,speed);
            } else {
                foreach (WifiLed led in activeLeds) {
                    led.UpdateFunction(function.Value, speed);
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e) {
            pictureBoxAdvanced.BackColor = sp.MouseLocColor();
        }


        public bool drawRectangle = false;
        public bool ambiantMode = false;
        public bool mouseTracking = false;
        private void checkBoxAdvancedShowRegion_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxAdvancedShowRegion.Checked) {
                drawRectangle = true;
                //start background if not already running
                if (!backgroundWorker1.IsBusy) {
                    backgroundWorker1.RunWorkerAsync();
                }
            } else {
                drawRectangle = false;
            }
        }

        private void checkBoxAmbianceMode_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxAmbianceMode.Checked) {
                ambiantMode = true;
                if (!backgroundWorker1.IsBusy) {
                    backgroundWorker1.RunWorkerAsync();
                }
            } else {
                ambiantMode = false;
            }
        }

        private void checkBoxMouseTracking_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxMouseTracking.Checked) {
                mouseTracking = true;
                if (!backgroundWorker1.IsBusy) {
                    backgroundWorker1.RunWorkerAsync();
                }
            } else {
                mouseTracking = false;
            }
        }
        


        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;
            Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero);
            Brush brush = new SolidBrush(Color.Magenta);
            Color averageColor;
            Stopwatch functionTime = new Stopwatch();
            //crun functions in the back ground
            while (ambiantMode || drawRectangle || mouseTracking) {
                if (drawRectangle) {
                    gsrc.FillRectangle(brush, (float)numericUpDownAdvancedX.Value, (float)numericUpDownAdvancedY.Value, 
                        (float)numericUpDownAdvancedXwidth.Value, (float)numericUpDownAdvancedYheight.Value);
                }
                if (ambiantMode) {
                    functionTime.Start();
                    //need some validation of values, especially for the settings values.
                    averageColor = sp.GetAverageColorSection((int)numericUpDownAdvancedX.Value, (int)numericUpDownAdvancedY.Value, 
                        (int)numericUpDownAdvancedXwidth.Value, (int)numericUpDownAdvancedYheight.Value,
                        (int)numericUpDownAdvancedXstride.Value,(int)numericUpDownAdvancedYstride.Value,
                        (int)numericUpDownSettingsRed.Value, (int)numericUpDownSettingsGreen.Value, (int)numericUpDownSettingsBlue.Value);
                   
                    
                    this.Invoke((MethodInvoker)(() => updateViewColor(averageColor.R, averageColor.G, averageColor.B, 0)));
                    this.Invoke((MethodInvoker)(() => updateActiveWifiLeds()));
                    functionTime.Stop();
                    //labelAdvancedCalcTime.Text = "Calculation time is " + functionTime.ElapsedMilliseconds + " ms.";
                    this.Invoke((MethodInvoker)(() => labelAdvancedCalcTime.Text = "Calculation time is " + functionTime.ElapsedMilliseconds + " ms."));
                    functionTime.Reset();
                }
                if (mouseTracking) {
                    var pointer = sp.MouseLocPosColor();
                    pictureBoxAdvanced.BackColor = pointer.Item1;
                    this.Invoke((MethodInvoker)(() => labelAdvancedCoordinates.Text = "Mouse at: X= " + pointer.Item2.X + " Y= " + pointer.Item2.Y));
                }
                //maybe add a sleep here.
            }
            //properly clean up graphics.
            gsrc.Dispose();
            Debug.WriteLine("Ending background worker for now.");

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

        }

        private void numericUpDownAdvancedX_ValueChanged(object sender, EventArgs e) {
            updateCalculations();
        }

        private void numericUpDownAdvancedY_ValueChanged(object sender, EventArgs e) {
            updateCalculations();
        }

        private void numericUpDownAdvancedXwidth_ValueChanged(object sender, EventArgs e) {
            updateCalculations();
        }

        private void numericUpDownAdvancedXstride_ValueChanged(object sender, EventArgs e) {
            updateCalculations();
        }

        private void numericUpDownAdvancedYheight_ValueChanged(object sender, EventArgs e) {
            updateCalculations();
        }

        private void numericUpDownAdvancedYstride_ValueChanged(object sender, EventArgs e) {
            updateCalculations();
        }
        #endregion

        private void buttonSettingsNameSave_Click(object sender, EventArgs e) {
            //Change name in memory
            selectedLed.name = textBoxSettingsName.Text;
            checkedListBoxDevices.Refresh();
            
            xmlSettings.AddCustomName(selectedLed.macAddress, selectedLed.name);

        }

        private void Mainview_FormClosing(object sender, FormClosingEventArgs e) {
            Debug.WriteLine("Shutting down...");
            xmlSettings.Save();
        }

        private void buttonAddDummy_Click_1(object sender, EventArgs e) {
            DummyWifiLed newDummy = new DummyWifiLed(IPAddress.Parse("255.255.255.255"), "Dummy" + DummyCount);
            string custom = xmlSettings.FindCustomName(newDummy.macAddress);
            if (custom != null && custom != "") {
                Debug.WriteLine("[buttonAddDummy_Click] custom = {0}", custom);
                newDummy.name = custom;
            }
            DummyCount++;
            foundLeds.Add(newDummy);
            checkedListBoxDevices.Items.Add(newDummy, false);
        }
    }
}
