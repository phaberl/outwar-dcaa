using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using DCT.Parsing;
using DCT.Pathfinding;
using DCT.Protocols.Http;
using DCT.Settings;
using DCT.Threading;
using Version=DCT.Security.Version;

namespace DCT.UI
{
    internal partial class StartDialog : Form
    {
        private string local = "DCT-Release.exe"; // Fallback declaration for compile safety

        internal StartDialog()
        {
            InitializeComponent();
            lnkGo.Enabled = false;
        }

        private void frmStart_Load(object sender, EventArgs e)
        {
            this.Text = string.Format("You are using v[{0}{1}] of Typpo's DC Tool - www.typpo.us", Version.Full, Version.Beta);
            Run();
            txtMain.SelectionStart = 0;
            txtMain.SelectionLength = 0;
            lnkGo.Enabled = true;
        }

        private void Run()
        {
            SetStatus("Loading open message...");
            string src = HttpSocket.DefaultInstance.Get("http://www.typpo.us/dctopen.txt").Replace("\n", "\r\n");
            txtMain.Text = src;

            Parser p = new Parser(src);
            CoreUI.Instance.ChatPanel.Channel = p.Parse("<chan>", "</chan>");
            CoreUI.Instance.ChatPanel.Server = p.Parse("<svr>", "</svr>");
            
            int tmp;
            if (int.TryParse(p.Parse("<port>", "</port>"), out tmp))
                CoreUI.Instance.ChatPanel.Port = tmp;
            else
            {
                CoreUI.Instance.ChatPanel.Port = 6667;
            }

            if (src.Contains("<msg>"))
            {
                MessageBox.Show(p.Parse("<msg>", "</msg>"), "Message", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Focus();
                txtMain.SelectionLength = 0;
            }

            try
            {
                if (Version.Full != p.Parse("<ver>", "</ver>"))
                {
                    string url = p.Parse("<url>", "</url>");

                    if (url == "ERROR")
                    {
                        this.Invoke((MethodInvoker)delegate { 
                            this.DialogResult = DialogResult.OK;
                            this.Close(); 
                        });
                        return;
                    }

                    if (File.Exists(local))
                    {
                        SetStatus("You've already downloaded the new version, use it instead: " + local);
                    }
                    else
                    {
                        new WebClient().DownloadFile(new Uri(url), local);
                    }
                    Process.Start(local);
                    Globals.Terminate = true;
                    Application.Exit();
                    return;
                }

                CoreUI.Instance.Changes = p.Parse("Change History:", "End Changes").Replace("\r", "").Trim();
                SetStatus("Building latest DC maps from host site...");
                ThreadEngine.DefaultInstance.DoParameterized(Pathfinder.BuildMap, true);
                SetStatus("Ready with latest maps...");
            }
            catch (FormatException)
            {
                ThreadEngine.DefaultInstance.DoParameterized(Pathfinder.BuildMap, false);
                SetStatus("Could not read new map status, update maps manually");
            }
            catch
            {
                txtMain.SelectionLength = 0;
                this.Focus();
                MessageBox.Show("Automatic updating failed.\n\n" +
                    "Could not read startup instructions from server. If map data has already been saved to your computer, the program should work.\n\n" +
                    "If this error persists (and you can get to www.typpo.us), please close or adjust any firewall/router/antivirus/antispyware that is blocking this program's connection to the internet.", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Hand);
                
                this.Invoke((MethodInvoker)delegate { 
                    this.DialogResult = DialogResult.OK;
                    this.Close(); 
                });
                return;
            }
        }

        private void SetStatus(string txt)
        {
            lblMain.Text = txt;
        }

        private void lnkGo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Close();
        }
    }
}