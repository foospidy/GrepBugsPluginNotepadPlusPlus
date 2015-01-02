using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Collections.Generic;
using System.Web;
using System.ServiceModel.Web;
using NppPluginNET;
using GrepBugsCore;

namespace GrepBugsPluginNpp
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "GrepBugs";
        static string iniFilePath        = null;
        static frmMyDlg frmMyDlg         = null;
        static int idMyDlg               = -1;
        static Bitmap tbBmp              = Properties.Resources.grepbugs;
        static Bitmap tbBmp_tbTab        = Properties.Resources.grepbugs;
        static Icon tbIcon               = null;
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            PluginBase.SetCommand(0, "Run GrepBugs", myMenuFunction, new ShortcutKey(false, false, false, Keys.None));
            //PluginBase.SetCommand(1, "GrepBugs Panel", myDockableDialog);
            idMyDlg = 0;
        }

        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp  = tbBmp.GetHbitmap();
            IntPtr pTbIcons      = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }
        
        internal static void PluginCleanUp()
        {
            GrepBugsCore.GrepBugsCore.scanResults.Clear();
        }
        #endregion

        #region " Menu functions "
        internal static void myMenuFunction()
        {
            myDockableDialog();

            frmMyDlg.setStatusLabel("Scanning...");

            DownloadRules();
            RunScan();

            string lastScan = DateTime.Now.ToString();

            Win32.WritePrivateProfileString("Scan", "LastScan", lastScan, iniFilePath);
            frmMyDlg.setStatusLabel("Scan results as of " + lastScan);
        }

        internal static void myDockableDialog()
        {
            if (frmMyDlg == null)
            {
                frmMyDlg = new frmMyDlg();

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "GrepBugs";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);
                
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }
        }
        #endregion

        #region " Core Functions "
        internal static void RunScan()
        {
            // Get unique list of extensions from open files
            var extensions         = new List<string>(GrepBugsCore.GrepBugsCore.GetOpenFileExtensions());
            var uniq_extensions    = new List<string>();
            string tmp_extension   =  "";
            StringBuilder debugOut = new StringBuilder();

            extensions.Sort();

            foreach (var extention in extensions)
            {
                if (extention.Length > 0)
                {
                    if (extention.ToString() != tmp_extension)
                    {
                        uniq_extensions.Add(extention.ToString());
                        tmp_extension = extention.ToString();
                    }
                }
            }
            
            int matchCount = 0;
            string tags = "";
            List<resultsListBoxData> tagsList = new List<resultsListBoxData>();

            // clear previous results
            GrepBugsCore.GrepBugsCore.scanResults.Clear();

            foreach (var extension in uniq_extensions)
            {
                GrepBugsCore.GrepBugsCore.ScanFilesByExtension(extension);
            }
            
            if (0 == GrepBugsCore.GrepBugsCore.scanResults.Count)
            {
                List<resultsListBoxData> noMatch = new List<resultsListBoxData>();
                noMatch.Add(new resultsListBoxData() { Value="0", Text="No Match!" });
                frmMyDlg.setResultsListBoxData(noMatch);
            }
            else
            {
                matchCount = 0;

                foreach (GrepBugsCore.GrepBugsCore.Match match in GrepBugsCore.GrepBugsCore.scanResults)
                {
                    if (match.tags != tags)
                    {
                        matchCount = GrepBugsCore.GrepBugsCore.GetMatchesCountByRuleID(match.id);
                        tagsList.Add(new resultsListBoxData() { Value = match.id.ToString(), Text = match.tags + " (" + matchCount + ")" });
                        tags = match.tags;
                    }
                }

                frmMyDlg.setResultsListBoxData(tagsList);
            }
        }
  
        internal static void DownloadRules()
        {
            string rules_url         = "https://grepbugs.com/rules";
            StringBuilder config_dir = new StringBuilder(Win32.MAX_PATH);
            StringBuilder tmp_file   = new StringBuilder(Win32.MAX_PATH);
            StringBuilder rules_file = new StringBuilder(Win32.MAX_PATH);

            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, config_dir);
            tmp_file.Append(config_dir.ToString()   + "\\GrepBugs.tmp");
            rules_file.Append(config_dir.ToString() + "\\GrepBugs.json");

            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.Headers.Add("user-agent", "GrepBugs for Notepad++ (1.0.0)");
                    myWebClient.DownloadFile(rules_url, tmp_file.ToString());
                    File.Copy(tmp_file.ToString(), rules_file.ToString(), true);
                    File.Delete(tmp_file.ToString());
                }

            }
            catch (Exception e)
            {
                if (File.Exists(rules_file.ToString()))
                {
                    frmMyDlg.setStatusLabel("Unable to retrieve rules file, using cached copy...");
                }
                else
                {
                    frmMyDlg.setStatusLabel("Scan fail!");
                    MessageBox.Show("Unable to retrieve rules file, and no cached copy availible. Scan fail!");
                    MessageBox.Show(e.Message);
                }
            }
        }
        #endregion
    }

    #region " ListBox Data Classes "
    public class resultsListBoxData
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    public class matchesListBoxData
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
    #endregion
}