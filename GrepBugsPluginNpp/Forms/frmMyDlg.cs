using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GrepBugsPluginNpp
{
    public partial class frmMyDlg : Form
    {
        public frmMyDlg()
        {
            InitializeComponent();
        }

        public void setStatusLabel(string txt)
        {
            this.lblStatus.Text = txt;
        }

        private void frmMyDlg_Load(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        public void setMatchesListBoxData(List<matchesListBoxData> list)
        {
            this.matchesListBox.ValueMember = "Value";
            this.matchesListBox.DisplayMember = "Text";
            this.matchesListBox.DataSource = list;
        }

        public void setResultsListBoxData(List<resultsListBoxData> list)
        {
            this.resultsListBox.ValueMember = "Value";
            this.resultsListBox.DisplayMember = "Text";
            this.resultsListBox.DataSource = list;
        }

        private void resultsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // load matches, description, and regex into tabs
            GrepBugsCore.GrepBugsCore.Rules rule = new GrepBugsCore.GrepBugsCore.Rules();
            List<matchesListBoxData> matches     = new List<matchesListBoxData>();

            foreach (var result in GrepBugsCore.GrepBugsCore.scanResults)
            {
                if (result.id == this.resultsListBox.SelectedValue.ToString())
                {
                    matches.Add(new matchesListBoxData() { Value = rule.id, Text = result.file + " | " + result.line });
                }
            }

            setMatchesListBoxData(matches);

            rule = GrepBugsCore.GrepBugsCore.GetRuleByRuleId(this.resultsListBox.SelectedValue.ToString());
            this.descriptionTextBox.Text = rule.description;
            this.regexTextBox.Text = rule.regex;
        }

        private void matchesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // open file and go to line
            GrepBugsCore.GrepBugsCore.SetActiveDocument(this.matchesListBox.Text);
        }
    }
}
