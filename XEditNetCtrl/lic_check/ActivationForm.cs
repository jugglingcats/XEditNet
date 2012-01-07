using System;
using System.ComponentModel;
using System.Windows.Forms;
using XEditNet.Licensing;

namespace XEditNet.Licensing
{
	internal class ActivationDialog : Form
	{
		#region Setup Form, Variables and Controls

		private Label label5;
		private Button CancelBtn;
		private Button RegisterBtn;
		string strKey;
		private GroupBox groupBox1;
		private Label infoLabel;
		private Container components = null;
		private System.Windows.Forms.TextBox licText;
		private DateTime trialExpiryDate=DateTime.MinValue;

		public ActivationDialog()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			string msg;
			if ( trialExpiryDate.Equals(DateTime.MinValue) )
				msg="Licence key is missing/invalid.";
			else
				msg="Existing trial licence key has expired. Please enter a new key.";

			infoLabel.Text=msg;

			base.OnLoad(e);
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
			this.licText = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.RegisterBtn = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.infoLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// licText
			// 
			this.licText.Location = new System.Drawing.Point(8, 56);
			this.licText.Name = "licText";
			this.licText.Size = new System.Drawing.Size(312, 20);
			this.licText.TabIndex = 12;
			this.licText.Text = "";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(7, 39);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(256, 13);
			this.label5.TabIndex = 9;
			this.label5.Text = "Please enter a valid licence key:";
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(241, 88);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.TabIndex = 7;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// RegisterBtn
			// 
			this.RegisterBtn.Location = new System.Drawing.Point(153, 89);
			this.RegisterBtn.Name = "RegisterBtn";
			this.RegisterBtn.TabIndex = 6;
			this.RegisterBtn.Text = "OK";
			this.RegisterBtn.Click += new System.EventHandler(this.RegisterBtn_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Location = new System.Drawing.Point(10, 28);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(306, 3);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "groupBox1";
			// 
			// infoLabel
			// 
			this.infoLabel.Location = new System.Drawing.Point(7, 8);
			this.infoLabel.Name = "infoLabel";
			this.infoLabel.Size = new System.Drawing.Size(312, 16);
			this.infoLabel.TabIndex = 11;
			// 
			// ActivationDialog
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(328, 119);
			this.Controls.Add(this.infoLabel);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.RegisterBtn);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.licText);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ActivationDialog";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Enter Licence Key";
			this.ResumeLayout(false);

		}
		#endregion

		#endregion

		#region MainEventHandlers
		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult=DialogResult.Cancel;
			this.Close();
		}

		private void RegisterBtn_Click(object sender, EventArgs e)
		{
			strKey=this.licText.Text;
			switch(XEditNetLicenseValidator.ValidateLicense(strKey))
			{
				case LicenseState.Full:
					StoreKey();
					MessageBox.Show("Thank you for purchasing XEditNet", "Thank you");
					DialogResult=DialogResult.OK;
					this.Close();
					break;

				case LicenseState.Invalid:
					MessageBox.Show("Sorry, this is an invalid key", "Error");
					break;

				case LicenseState.None:
					MessageBox.Show("None");
					break;

				case LicenseState.Trial_Active:
					StoreKey();
					MessageBox.Show("This key is an active trial key and has been stored", "Thank you");
					DialogResult=DialogResult.OK;
					this.Close();
					break;

				case LicenseState.Trial_Expired:
					MessageBox.Show("Sorry, this is a trial key that has already expired", "Error");
					break;
			}


		}
		#endregion

		#region key Storage
		private void StoreKey()
		{
			XEditNetLicenseContext context1 = new XEditNetLicenseContext();
			context1.SetSavedLicenseKey(null,strKey);
		}

		public DateTime TrialExpiryDate
		{
			set { trialExpiryDate=value; }
		}

		#endregion

	}
}
