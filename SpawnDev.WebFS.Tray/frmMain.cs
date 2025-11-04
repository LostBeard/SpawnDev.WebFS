using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SpawnDev.BlazorJS;
using SpawnDev.WebFS.Host;
using System.ComponentModel;
using System.Diagnostics;

namespace SpawnDev.WebFS.Tray
{
    public partial class frmMain : Form
    {
        NotifyIcon? _sysTray = null;
        ToolStripMenuItem? _recentMI = null;
        WinFormsApp WinFormsApp { get; }
        DokanService DokanService { get; }
        WebFSServer WebFSServer { get; }
        public frmMain(WinFormsApp winFormsApp)
        {
            WinFormsApp = winFormsApp;
            DokanService = WinFormsApp.Services.GetRequiredService<DokanService>();
            WebFSServer = WinFormsApp.Services.GetRequiredService<WebFSServer>();
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;
            base.OnLoad(e);
            Visible = false;
            ShowInTaskbar = false;
            InitTray();

            _ = Task.Run(async () =>
            {
                await WinFormsApp.Services.StartBackgroundServices();
            });
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
        async Task Shutdown()
        {
            WinFormsApp.Dispose();
            await Task.Delay(2000);
            this.Close();
        }
        void InitTray()
        {
            _sysTray = new NotifyIcon();
            _sysTray.Icon = this.Icon;
            _sysTray.Visible = true;
            _sysTray.DoubleClick += (s, e) =>
            {
                if (Visible && ShowInTaskbar)
                {
                    Hide();
                    ShowInTaskbar = false;
                }
                else
                {
                    this.TopMost = true;
                    Show();
                    this.TopMost = true;
                    ShowInTaskbar = true;
                    this.TopMost = false;
                }
            };

            _sysTray.ContextMenuStrip = new ContextMenuStrip();

            _sysTray.ContextMenuStrip.Items.Add(_recentMI = new ToolStripMenuItem("Domains"));

            _sysTray.ContextMenuStrip.Opening += (s, e) =>
            {
                UpdateMenu();
            };

            using var p = Process.GetCurrentProcess();
            var appExe = p.MainModule!.FileName;
            var appExePath = Path.GetDirectoryName(appExe);

            // start with windows
            ToolStripMenuItem autoStartGMMI = null;
            string autoStartValue = "\"" + appExe + "\" --background";
            string appExeFileName = Path.GetFileName(appExe).ToLower().Replace(".vshost", "");

            autoStartGMMI = new ToolStripMenuItem("Autostart", null, (s, e) =>
            {
                autoStartGMMI!.Checked = !autoStartGMMI.Checked;
                try
                {
                    using (var rkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    {
                        if (autoStartGMMI.Checked)
                        {
                            rkey.SetValue(Application.ProductName, autoStartValue);
                        }
                        else
                        {
                            rkey.DeleteValue(Application.ProductName!);
                        }
                    }
                }
                catch { }
            });

            try
            {
                using (var rkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    var startup_path = rkey?.GetValue(Application.ProductName, "").ToString();
                    autoStartGMMI.Checked = startup_path?.ToLower().Contains(appExeFileName) ?? false;
                }
            }
            catch { }
            _sysTray.ContextMenuStrip.Items.Add(autoStartGMMI);

            // exit
            _sysTray.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, async (s, e) =>
            {
                await Shutdown();
            }));
        }
        void UpdateMenu()
        {
            if (_recentMI == null) return;
            _recentMI.DropDownItems.Clear();
            var connectedHosts = WebFSServer.ConnectedDomains;
            foreach (var provider in WebFSServer.DomainProviders.Values)
            {
                var mm = new ToolStripMenuItem(provider.Host);
                _recentMI.DropDownItems.Add(mm);
                var isConnected = connectedHosts.Contains(provider.Host);
                if (isConnected) mm.ForeColor = Color.BlueViolet;
                //Goto [provider.Host]
                mm.DropDownItems.Add(new ToolStripMenuItem($"Goto {provider.Host}", null, (s, e) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = provider.Url,
                        UseShellExecute = true
                    });
                }));
                // Enable checked/unchecked
                mm.DropDownItems.Add(new ToolStripMenuItem("Enable", null, (s, e) =>
                {
                    if (s is ToolStripMenuItem ts)
                    {
                        var isChecked = ts.CheckState == CheckState.Checked;
                        WebFSServer.SetDomainAllowed(provider.Host, !isChecked);
                    }
                })
                {
                    CheckState = provider.Enabled == null ? CheckState.Indeterminate : (provider.Enabled == true ? CheckState.Checked : CheckState.Unchecked),
                });
            }
            if (_recentMI.DropDownItems.Count == 0)
            {
                _recentMI.DropDownItems.Add("None");
            }
        }
        void UpdateMenuOld()
        {
            if (_recentMI == null) return;
            _recentMI.DropDownItems.Clear();
            foreach (var mi in WebFSServer.DomainProviders)
            {
                ToolStripMenuItem? m = null;
                var isConnected = WebFSServer.ConnectedDomains.Contains(mi.Key);
                m = new ToolStripMenuItem(mi.Key, null, (s, e) =>
                {
                    WebFSServer.SetDomainAllowed(mi.Key, !m!.Checked);
                });
                if (isConnected) m.ForeColor = Color.BlueViolet;
                m.Checked = mi.Value.Enabled == true;
                _recentMI.DropDownItems.Add(m);
            }
            if (_recentMI.DropDownItems.Count == 0)
            {
                _recentMI.DropDownItems.Add("None");
            }
        }
    }
}
