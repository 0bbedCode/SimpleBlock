using System;
using System.Windows.Forms;

namespace SimpleBlock {
    public partial class RepoModifyForm : Form {
        private Repo _repo;
        public RepoModifyForm() {
            InitializeComponent();
        }

        public RepoModifyForm(Repo repo) {
            InitializeComponent();
            if (repo is null)
                throw new Exception("Null");

            _repo = repo;
            saveButton.Click -= SaveButton_Click;
            saveButton.Click += SaveButtonEx_Click;

            repoNameText.Text = _repo.Name;
            repoUrlTextBox.Text = _repo.URI;
        }

        private void ParseButton_Click(object sender, EventArgs e) {
            Parse();
            if (_repo is null)
                return;

            hostList.Items.Clear();
            var entrs = _repo.GetEnteries();
            foreach(var itm in _repo.GetEnteries()) 
                hostList.Items.Add(new ListViewItem { Text = itm });
            
            //for(var i = 0; i < entrs.Count; i++) {
            //    var lvi = new ListViewItem();
            //    lvi.Text = entrs[i];
            //    hostList.Items.Add(lvi);
            //}
        }

        private void Parse() {
            if (!Utils.StringIsValid(repoUrlTextBox.Text)) {
                MessageBox.Show("Invalid URL", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Utils.StringIsValid(repoNameText.Text)) {
                MessageBox.Show("Invalid Repo Name", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _repo = new Repo(repoUrlTextBox.Text, repoNameText.Text, false);
        }

        private void SaveButtonEx_Click(object sender, EventArgs e) { 
            if(repoUrlTextBox.Text != _repo.URI) 
                _repo.URI = repoUrlTextBox.Text;
            
            if(repoNameText.Text != _repo.Name)
                _repo.Name = repoNameText.Text;

            RepoCore.RemoveRepo(_repo);
            RepoCore.AddNewRepo(_repo);
            Close();
        }

        private void SaveButton_Click(object sender, EventArgs e) {
            if(_repo is null)
                Parse();

            if (RepoCore.HasRepo(_repo)) {
                MessageBox.Show("Repo Already Exists", "Existing Repo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_repo is null) {
                MessageBox.Show("Invalid Repo", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else {
                RepoCore.AddNewRepo(_repo);
                Close();
            }
        }
    }
}
