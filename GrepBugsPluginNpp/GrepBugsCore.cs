using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.ServiceModel.Web;
using NppPluginNET;

namespace GrepBugsCore
{
    public class GrepBugsCore
    {
        static public List<Match> scanResults = new List<Match>();

        static public IntPtr GetActiveDocument()
        {
            return PluginBase.GetCurrentScintilla();
        }

        static public void SetActiveDocument(string fileAndLine)
        {
            string file = fileAndLine.Split('|')[0];
            int line    = Int32.Parse(fileAndLine.Split('|')[1]);
            
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, file.ToString());
            Win32.SendMessage(GetActiveDocument(), SciMsg.SCI_GOTOLINE, line - 1, 0);
        }

        static public int GetActiveDocumentLength()
        {
            return (int)Win32.SendMessage(GetActiveDocument(), SciMsg.SCI_GETLENGTH, 0, 0);
        }

        static public string GetActiveDocumentText()
        {
            int length         = GetActiveDocumentLength();
            StringBuilder text = new StringBuilder(length + 1);

            if (length > 0)
            {
                Win32.SendMessage(GetActiveDocument(), SciMsg.SCI_GETTEXT, length + 1, text);
            }

            return text.ToString();
        }

        static public int GetNumberOfOpenFiles()
        {
            return (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, 0);
        }

        static public List<string> GetOpenFileExtensions()
        {
            List<string> extensions       = new List<string>();
            int filecount                 = GetNumberOfOpenFiles();
            ClikeStringArray cStringArray = new ClikeStringArray(filecount, Win32.MAX_PATH);

            if (filecount > 0)
            {
                if (Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, cStringArray.NativePointer, filecount) != IntPtr.Zero)
                {
                    foreach (var file in cStringArray.ManagedStringsUnicode)
                    {
                        extensions.Add(Path.GetExtension(file.ToString()));
                    }
                }

            }

            return extensions;
        }

        static public List<string> GetOpenFilesByExtension(string extension)
        {
            List<string> files            = new List<string>();
            int filecount                 = GetNumberOfOpenFiles();
            ClikeStringArray cStringArray = new ClikeStringArray(filecount, Win32.MAX_PATH);

            if (filecount > 0)
            {
                if (Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, cStringArray.NativePointer, filecount) != IntPtr.Zero)
                {
                    foreach (var file in cStringArray.ManagedStringsUnicode)
                    {
                        if (Path.GetExtension(file.ToString()) == extension)
                        {
                            files.Add(file.ToString());
                        }
                    }
                }
            }
            
            return files;
        }

        static public List<Rules> GetRulesByExtension(string extension)
        {
            List<Rules> rulesToScanWith = new List<Rules>();

            try
            {
                // absolute path to rules file
                StringBuilder rules_file = new StringBuilder(Win32.MAX_PATH);
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, rules_file);
                rules_file.Append("\\GrepBugs.json");

                // read json from rules file
                StringBuilder json = new StringBuilder();
                json.Append(System.IO.File.ReadAllText(rules_file.ToString()));
                
                // convert json to object
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Rules>));
                MemoryStream ms                       = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(json.ToString()));
                var rulesObject                       = (List<Rules>)serializer.ReadObject(ms);
                ms.Close();

                foreach (var rule in rulesObject)
                {
                    string[] split          = rule.extension.ToString().Split(',');
                    List<string> extensions = new List<string>(split);

                    if (extensions.Contains(extension.Remove(extension.IndexOf('.'), 1)))
                    {
                        rulesToScanWith.Add(rule);
                    }
                }

                return rulesToScanWith;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error!: " + e.Message);
                //frmMyDlg.setText1(e.StackTrace.ToString());
                return rulesToScanWith;
            }
        }

        static public Rules GetRuleByRuleId(string ruleId)
        {
            Rules emptyRule = new Rules();

            // absolute path to rules file
            StringBuilder rules_file = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, rules_file);
            rules_file.Append("\\GrepBugs.json");

            // read json from rules file
            StringBuilder json = new StringBuilder();
            json.Append(System.IO.File.ReadAllText(rules_file.ToString()));

            // convert json to object
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Rules>));
            MemoryStream ms = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(json.ToString()));
            var rulesObject = (List<Rules>)serializer.ReadObject(ms);
            ms.Close();

            foreach (var rule in rulesObject)
            {
                if (ruleId == rule.id)
                {
                   return rule;
                }
            }

            return emptyRule;
        }

        static public List<Match> GetMatchesByRuleId(string ruleId)
        {
            List<Match> matches = new List<Match>();

            // absolute path to rules file
            StringBuilder rules_file = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, rules_file);
            rules_file.Append("\\GrepBugs.json");

            // read json from rules file
            StringBuilder json = new StringBuilder();
            json.Append(System.IO.File.ReadAllText(rules_file.ToString()));

            // convert json to object
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Rules>));
            MemoryStream ms = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(json.ToString()));
            var rulesObject = (List<Rules>)serializer.ReadObject(ms);
            ms.Close();

            foreach (var match in scanResults)
            {
                if (ruleId == match.id)
                {
                    matches.Add(match);
                }
            }

            return matches;
        }

        static public int GetMatchesCountByRuleID(string ruleId)
        {
            int count = 0;

            foreach (var match in scanResults)
            {
                if (ruleId == match.id)
                {
                    count++;
                }
            }

            return count;
        }

        [DataContract]
        public class Rules
        {
            [DataMember]
            public string id { get; set; }

            [DataMember]
            public string tags { get; set; }

            [DataMember]
            public string language { get; set; }

            [DataMember]
            public string extension { get; set; }

            [DataMember]
            public string regex { get; set; }

            [DataMember]
            public string description { get; set; }
        }

        public class Results
        {
            public List<Match> matches { get; set; }
        }

        public class Match {
            public string id { get; set; }
            public string tags { get; set; }
            public string file { get; set; }
            public string line { get; set; }
        }

        static public void ScanFilesByExtension(string extension)
        {
            List<Rules> scanRules = new List<Rules>(GetRulesByExtension(extension));
            List<string> files    = new List<string>(GetOpenFilesByExtension(extension));
           
            int ruleCount = 0;
            int fileCount = 0;
            int lineCount = 0;
            int matchCount = 0;

            // for each rule...
            foreach (var rule in scanRules)
            {
                ruleCount++;
                fileCount = 0;
                // ...scan each file
                foreach (var file in files)
                {
                    fileCount++;
                    lineCount = 0;
                    foreach (string line in File.ReadLines(file.ToString()))
                    {
                        lineCount++;

                        if(Regex.IsMatch(line, rule.regex)) {
                            Match newMatch = new Match();
                            matchCount++;
                            newMatch.id   = rule.id;
                            newMatch.tags = rule.tags;
                            newMatch.file = file.ToString();
                            newMatch.line = lineCount.ToString();
                            scanResults.Add(newMatch);
                        }
                    }
                }
            }
        }
    }
}
