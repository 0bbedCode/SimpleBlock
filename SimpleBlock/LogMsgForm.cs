using System;
using System.Windows.Forms;

namespace SimpleBlock {
    public partial class LogMsgForm : Form {
        public LogMsgForm(LogEx log) {
            InitializeComponent();
            logBox.AppendText(log.ToString());
        }

        private void LogMsgForm_Load(object sender, EventArgs e) { }
    }
}
