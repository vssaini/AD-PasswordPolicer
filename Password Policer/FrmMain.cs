using System;
using System.Windows.Forms;
using PasswordPolicer.Code;

namespace PasswordPolicer
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();

            // By default values set
            txtDomainName.Text = "domain.com";
            txtUserId.Text = "jflorida";

            // Show date after specific days
            //const int days = 70;
            //var newDate = DateTime.Now.AddDays(days);
            //var message = string.Format("Date after {0} days from today:{1}", days, String.Format("{0:MMMM d, yyyy}", newDate));
            //MessageBox.Show(message);
        }

        private void btnGetPassExpiryDate_Click(object sender, EventArgs e)
        {
            try
            {
                var domain = txtDomainName.Text.Trim();
                var userName = txtUserId.Text.Trim();
                var expiryDate = Utility.GetPasswordExpiryDate(domain, userName);

                if (expiryDate != null)
                {
                    lblMessage.Visible = true;
                    lblMessage.Text = expiryDate;
                }
                else
                {
                    lblMessage.Visible = true;
                    lblMessage.Text = "Not Available";
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error occured - " + exc, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
