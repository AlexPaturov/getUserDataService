using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace GetUserDataServiceGUI
{
    public class MainForm : Form
    {
        private static CrossThreadComm.TraceCb _conInfoTrace;
        private static CrossThreadComm.UpdateState _updateState;
        private static CrossThreadComm.UpdateRXTX _updRxTx;
        private int _DiagSerportRx;
        private int _DiagSerportTx;
        private static object lck = new object();
        protected object _traceListBoxLock = new object();
        private Dictionary<string, string> dn = new Dictionary<string, string>();
        private List<string> _items = new List<string>();
        ServerMode sm = null;
        private bool _running = false;
        private bool _shuttingdown = false;
        protected Thread thServiceThread = null;
        protected bool _connected = false;
        protected bool _is_shown = false;
        private bool drag;
        private Point start_point = new Point(0, 0);
        private bool draggable = true;
        private IContainer components;
        private Panel panel5;
        private Button buttonMinimize;
        private Label label7;
        private Label label6;
        private Label labelRxSerial;
        private Label labelSerialTX;
        private Panel panel4;
        private Panel panel1;
        private Label label5;
        private Button btnCloseForm;
        private Button buttonClearLog;
        private Button buttonRefresh;
        private ListBox listBoxInfoTrace;
        private Button buttonStop;
        private Button buttonStart;
        private Label labelRemoteHost;
        private Label label2;
        private TextBox tbClientPort;
        private TextBox tbClientHost;
        private ContextMenuStrip contextMenuStrip1;
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private void ServiceThread()
        {
            _running = true;
            ConnInfoTrace((object)("ServiceThread start " + this.dn["clientHost"] + " " + int.Parse(this.dn["clientPort"].Trim()).ToString()));
            
            try
            {
                sm = new ServerMode();
                sm.Run(dn, MainForm._conInfoTrace, MainForm._updateState, MainForm._updRxTx);
                 
            }
            catch (Exception ex)
            {
                _running = false;
                ConnInfoTrace("ServiceThread start failed..");
                ConnInfoTrace(ex.Message);
                ConnInfoTrace(ex.StackTrace);
                logger.Error(ex);
            }

            ConnInfoTrace("ServiceThread stopped");
            _running = false;
            sm = null;
            thServiceThread = null;
        }

        private void ButtonStartClick(object sender, EventArgs e)
        {
            #region enable/disable input element
            this.tbClientHost.Enabled   = false;
            this.tbClientPort.Enabled   = false;
            this.buttonStop.Enabled     = true;
            this.buttonStart.Enabled    = false;
            this.buttonRefresh.Enabled  = false;
            this.dn["clientHost"]       = this.tbClientHost.Text.Trim();
            this.dn["clientPort"]       = this.tbClientPort.Text.Trim();
            #endregion

            thServiceThread = new Thread(ServiceThread);
            _running = true;
            thServiceThread.Start();
            Thread.Sleep(100);
            
            if (!this._running)
            {
                this.buttonStop.Enabled = false;
                this.buttonStart.Enabled = true;
                this.buttonRefresh.Enabled = true;
                this.panel5.BackColor = Color.Gray;
            }
            else
            {
                this.panel5.BackColor = Color.Orange;
            }

        }

        private void ButtonStopClick(object sender, EventArgs e) 
        {
            HandleStop();
        }

        private void HandleStop()
        {
            if (sm != null)
            {
                ConnInfoTrace((object)"Stop request ServiceThread");
                sm.StopRequest();
            }
            sm = null;
            this.panel5.BackColor = Color.Gray;
            this.buttonStop.Enabled = false;
            this.tbClientHost.Enabled = true;
            this.tbClientPort.Enabled = true;
            this.buttonStart.Enabled = true;
            this.buttonRefresh.Enabled = true;
        }

        public void UpdateState(object obj, CrossThreadComm.State state)
        {
            if (this.InvokeRequired)
            {
                if (this._shuttingdown)
                    return;
                this.Invoke((Delegate)new CrossThreadComm.UpdateState(this.UpdateState), obj, (object)state);
            }
            else
            {
                switch (state)
                {
                    case CrossThreadComm.State.connect:
                        this.panel5.BackColor = Color.Green;
                        this._connected = true;
                        break;
                    case CrossThreadComm.State.disconnect:
                        this.panel5.BackColor = Color.Orange;
                        this._connected = false;
                        break;
                    case CrossThreadComm.State.terminate:
                        this._connected = false;
                        this.ConnInfoTrace((object)"UpdateState() -- ServiceThread has finished");
                        break;
                }
            }
        }

        public void UpdateRxTx(object obj, int bytesFromSerial, int bytesToSerial)
        {
            if (this.InvokeRequired)
            {
                if (this._shuttingdown)
                    return;
                this.Invoke((Delegate)new CrossThreadComm.UpdateRXTX(this.UpdateRxTx), obj, (object)bytesFromSerial, (object)bytesToSerial);
            }
            else
            {
                this._DiagSerportRx += bytesFromSerial;
                this._DiagSerportTx += bytesToSerial;
            }
        }

        public void ConnInfoTrace(object obj)
        {
            if (this.InvokeRequired)
            {
                if (this._shuttingdown)
                    return;
                this.Invoke((Delegate)new CrossThreadComm.TraceCb(this.ConnInfoTrace), obj);
            }
            else
            {
                string str = (string)obj;
                lock (this._traceListBoxLock)
                {
                    this.labelRxSerial.Text = this._DiagSerportRx.ToString();
                    this.labelSerialTX.Text = this._DiagSerportTx.ToString();
                    if (str != null)
                    {
                        this.listBoxInfoTrace.Items.Add((object)(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + str));
                        //logger.Debug((object)(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + str));
                        if (this.listBoxInfoTrace.Items.Count > 256)
                            this.listBoxInfoTrace.Items.RemoveAt(0);
                        this.listBoxInfoTrace.SelectedIndex = this.listBoxInfoTrace.Items.Count - 1;
                        //logger.Debug(this.listBoxInfoTrace.Items.Count - 1);
                    }
                    else
                    {
                        this.listBoxInfoTrace.Items.Clear();
                        this._DiagSerportRx = 0;
                        this._DiagSerportTx = 0;
                        this.labelRxSerial.Text = this._DiagSerportRx.ToString();
                        this.labelSerialTX.Text = this._DiagSerportTx.ToString();
                    }
                }
            }
        }

        private void InitializeSerialToIPGui()
        {
            SetDefaultCulture();
            this.ConnInfoTrace((object)"GUI init");
            
            this.tbClientHost.Text = this.dn["clientHost"];
            this.tbClientPort.Text = this.dn["clientPort"];
            this.buttonStop.Enabled = false;
        }

        public MainForm()
        {
            this.InitializeComponent();
            MainForm._conInfoTrace = new CrossThreadComm.TraceCb(this.ConnInfoTrace);
            MainForm._updateState = new CrossThreadComm.UpdateState(this.UpdateState);
            MainForm._updRxTx = new CrossThreadComm.UpdateRXTX(this.UpdateRxTx);
            dn.Add("clientHost", "127.0.0.1");
            dn.Add("clientPort", "8887");

            ToolTip toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
            toolTip.SetToolTip((Control)this.buttonRefresh, "Refresh the ... ");
            toolTip.SetToolTip((Control)this.buttonClearLog, "Clear the trace log");
            this.panel5.BackColor = Color.Gray;
            this.InitializeSerialToIPGui();
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            this._shuttingdown = true;
            this._is_shown = false;
            this.HandleStop();
            logger.Info("Close " + System.Diagnostics.Process.GetCurrentProcess().ProcessName);  // спецификация - дата, время запуска драйвера
        }

        private void ButtonRefreshClick(object sender, EventArgs e) => this.InitializeSerialToIPGui();

        private void ButtonClearLogClick(object sender, EventArgs e) => this.ConnInfoTrace((object)null);

        private void MainFormMouseDown(object sender, MouseEventArgs e)
        {
            Point point = new Point(e.X, e.Y);
            this.drag = true;
            this.start_point = new Point(e.X, e.Y);
            if (e.Button != MouseButtons.Right)
                return;
            ++point.X;
            ++point.Y;
        }

        private void Form_MouseUp(object sender, MouseEventArgs e) => this.drag = false;

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.drag)
                return;
            Point screen = this.PointToScreen(new Point(e.X, e.Y));
            this.Location = new Point(screen.X - this.start_point.X, screen.Y - this.start_point.Y);
        }

        public bool Draggable
        {
            set => this.draggable = value;
            get => this.draggable;
        }

        private void Label5MouseDown(object sender, MouseEventArgs e)
        {
            MouseEventArgs e1 = e;
            this.MainFormMouseDown(sender, e1);
        }

        private void Label5MouseMove(object sender, MouseEventArgs e)
        {
            MouseEventArgs e1 = e;
            this.Form_MouseMove(sender, e1);
        }

        private void Label5MouseUp(object sender, MouseEventArgs e) => this.Form_MouseUp(sender, e);

        private void MainFormLoad(object sender, EventArgs e)
        { 
            this._is_shown = true;
            logger.Info("Start " + System.Diagnostics.Process.GetCurrentProcess().ProcessName); // спецификация - дата, время запуска драйвера
        }

        private void ButtonMinimizeClick(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;

        private void Button1MouseHover(object sender, EventArgs e)
        {
        }

        private void Button1MouseEnter(object sender, EventArgs e)
        {
            this.btnCloseForm.ForeColor = SystemColors.ControlLightLight;
            this.btnCloseForm.BackColor = Color.Red;
            this.btnCloseForm.Height = 26;
        }

        private void Button1MouseLeave(object sender, EventArgs e)
        {
            this.btnCloseForm.ForeColor = SystemColors.ControlLightLight;
            this.btnCloseForm.BackColor = Color.DodgerBlue;
            this.btnCloseForm.Height = 26;
        }

        private void MainFormActivated(object sender, EventArgs e)
        {
            if (!this._is_shown)
                return;
            this.Opacity = 1.0;
        }

        private void MainFormDeactivate(object sender, EventArgs e)
        {
            if (!this._is_shown)
                return;
            this.Opacity = 0.8;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label2 = new System.Windows.Forms.Label();
            this.labelRemoteHost = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.listBoxInfoTrace = new System.Windows.Forms.ListBox();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.buttonClearLog = new System.Windows.Forms.Button();
            this.btnCloseForm = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.labelSerialTX = new System.Windows.Forms.Label();
            this.labelRxSerial = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.buttonMinimize = new System.Windows.Forms.Button();
            this.panel5 = new System.Windows.Forms.Panel();
            this.tbClientPort = new System.Windows.Forms.TextBox();
            this.tbClientHost = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 91);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 26);
            this.label2.TabIndex = 2;
            this.label2.Text = "client port";
            // 
            // labelRemoteHost
            // 
            this.labelRemoteHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.labelRemoteHost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRemoteHost.Location = new System.Drawing.Point(16, 49);
            this.labelRemoteHost.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelRemoteHost.Name = "labelRemoteHost";
            this.labelRemoteHost.Size = new System.Drawing.Size(120, 26);
            this.labelRemoteHost.TabIndex = 8;
            this.labelRemoteHost.Text = "client host";
            // 
            // buttonStart
            // 
            this.buttonStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStart.BackColor = System.Drawing.SystemColors.Control;
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.Location = new System.Drawing.Point(111, 452);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(171, 37);
            this.buttonStart.TabIndex = 7;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStartClick);
            // 
            // buttonStop
            // 
            this.buttonStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStop.Enabled = false;
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.Location = new System.Drawing.Point(332, 452);
            this.buttonStop.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(127, 37);
            this.buttonStop.TabIndex = 9;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.ButtonStopClick);
            // 
            // listBoxInfoTrace
            // 
            this.listBoxInfoTrace.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxInfoTrace.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxInfoTrace.FormattingEnabled = true;
            this.listBoxInfoTrace.HorizontalScrollbar = true;
            this.listBoxInfoTrace.ItemHeight = 16;
            this.listBoxInfoTrace.Location = new System.Drawing.Point(16, 175);
            this.listBoxInfoTrace.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.listBoxInfoTrace.Name = "listBoxInfoTrace";
            this.listBoxInfoTrace.ScrollAlwaysVisible = true;
            this.listBoxInfoTrace.Size = new System.Drawing.Size(979, 260);
            this.listBoxInfoTrace.TabIndex = 13;
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRefresh.Location = new System.Drawing.Point(908, 452);
            this.buttonRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(85, 37);
            this.buttonRefresh.TabIndex = 14;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.ButtonRefreshClick);
            // 
            // buttonClearLog
            // 
            this.buttonClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonClearLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonClearLog.Location = new System.Drawing.Point(22, 452);
            this.buttonClearLog.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonClearLog.Name = "buttonClearLog";
            this.buttonClearLog.Size = new System.Drawing.Size(83, 37);
            this.buttonClearLog.TabIndex = 8;
            this.buttonClearLog.Text = "Clear";
            this.buttonClearLog.UseVisualStyleBackColor = true;
            this.buttonClearLog.Click += new System.EventHandler(this.ButtonClearLogClick);
            // 
            // btnCloseForm
            // 
            this.btnCloseForm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCloseForm.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnCloseForm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCloseForm.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.btnCloseForm.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnCloseForm.Location = new System.Drawing.Point(947, 1);
            this.btnCloseForm.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCloseForm.Name = "btnCloseForm";
            this.btnCloseForm.Size = new System.Drawing.Size(64, 32);
            this.btnCloseForm.TabIndex = 19;
            this.btnCloseForm.Text = "X";
            this.btnCloseForm.UseVisualStyleBackColor = false;
            this.btnCloseForm.Click += new System.EventHandler(this.btnCloseFormClick);
            this.btnCloseForm.MouseEnter += new System.EventHandler(this.Button1MouseEnter);
            this.btnCloseForm.MouseLeave += new System.EventHandler(this.Button1MouseLeave);
            this.btnCloseForm.MouseHover += new System.EventHandler(this.Button1MouseHover);
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.BackColor = System.Drawing.Color.DodgerBlue;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label5.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label5.Location = new System.Drawing.Point(1, 1);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(883, 32);
            this.label5.TabIndex = 20;
            this.label5.Text = "Get User Data";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label5.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Label5MouseDown);
            this.label5.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Label5MouseMove);
            this.label5.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Label5MouseUp);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Silver;
            this.panel1.Location = new System.Drawing.Point(-1, 34);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1, 383);
            this.panel1.TabIndex = 21;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.Silver;
            this.panel4.Location = new System.Drawing.Point(0, 37);
            this.panel4.Margin = new System.Windows.Forms.Padding(0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1, 423);
            this.panel4.TabIndex = 23;
            // 
            // labelSerialTX
            // 
            this.labelSerialTX.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelSerialTX.Location = new System.Drawing.Point(117, 137);
            this.labelSerialTX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSerialTX.Name = "labelSerialTX";
            this.labelSerialTX.Size = new System.Drawing.Size(120, 28);
            this.labelSerialTX.TabIndex = 24;
            this.labelSerialTX.Text = "0";
            this.labelSerialTX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelRxSerial
            // 
            this.labelRxSerial.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelRxSerial.Location = new System.Drawing.Point(553, 136);
            this.labelRxSerial.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelRxSerial.Name = "labelRxSerial";
            this.labelRxSerial.Size = new System.Drawing.Size(121, 28);
            this.labelRxSerial.TabIndex = 25;
            this.labelRxSerial.Text = "0";
            this.labelRxSerial.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label6.Location = new System.Drawing.Point(16, 137);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(88, 28);
            this.label6.TabIndex = 26;
            this.label6.Text = "SerTX:";
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label7.Location = new System.Drawing.Point(463, 137);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(83, 28);
            this.label7.TabIndex = 27;
            this.label7.Text = "SerRX:";
            // 
            // buttonMinimize
            // 
            this.buttonMinimize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMinimize.BackColor = System.Drawing.Color.DodgerBlue;
            this.buttonMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMinimize.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.buttonMinimize.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.buttonMinimize.Location = new System.Drawing.Point(885, 1);
            this.buttonMinimize.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonMinimize.Name = "buttonMinimize";
            this.buttonMinimize.Size = new System.Drawing.Size(64, 32);
            this.buttonMinimize.TabIndex = 28;
            this.buttonMinimize.Text = "_";
            this.buttonMinimize.UseVisualStyleBackColor = false;
            this.buttonMinimize.Click += new System.EventHandler(this.ButtonMinimizeClick);
            // 
            // panel5
            // 
            this.panel5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panel5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel5.Location = new System.Drawing.Point(287, 451);
            this.panel5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(41, 37);
            this.panel5.TabIndex = 29;
            // 
            // tbClientPort
            // 
            this.tbClientPort.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbClientPort.Location = new System.Drawing.Point(144, 87);
            this.tbClientPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbClientPort.Name = "tbClientPort";
            this.tbClientPort.Size = new System.Drawing.Size(213, 30);
            this.tbClientPort.TabIndex = 3;
            // 
            // tbClientHost
            // 
            this.tbClientHost.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbClientHost.Location = new System.Drawing.Point(144, 49);
            this.tbClientHost.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbClientHost.Name = "tbClientHost";
            this.tbClientHost.Size = new System.Drawing.Size(213, 30);
            this.tbClientHost.TabIndex = 32;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(1013, 497);
            this.Controls.Add(this.tbClientHost);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.buttonMinimize);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.labelRxSerial);
            this.Controls.Add(this.labelSerialTX);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnCloseForm);
            this.Controls.Add(this.buttonClearLog);
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.listBoxInfoTrace);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.tbClientPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.labelRemoteHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "MainForm";
            this.Text = "ARM to moxa bidi";
            this.Activated += new System.EventHandler(this.MainFormActivated);
            this.Deactivate += new System.EventHandler(this.MainFormDeactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
            this.Load += new System.EventHandler(this.MainFormLoad);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainFormMouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void RadioButtonServerCheckedChanged(object sender, EventArgs e)
        {
            this.labelRemoteHost.Enabled = false;
            this.tbClientHost.Enabled = false;
        }

        private void RadioButtonClientCheckedChanged(object sender, EventArgs e)
        {
            this.labelRemoteHost.Enabled = true;
            this.tbClientHost.Enabled = true;
        }

        public static void SetDefaultCulture()
        {
            CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            Type type = typeof(CultureInfo);
            type.InvokeMember("s_userDefaultCulture",
                                BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                cultureInfo,
                                new object[] { cultureInfo });

            type.InvokeMember("s_userDefaultUICulture",
                                BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                cultureInfo,
                                new object[] { cultureInfo });
        }

        private void btnCloseFormClick(object sender, EventArgs e)
        {
            this.HandleStop();
            this.Close();
        }
    }
}
