using System;
using System.Text;
using System.Windows.Forms;

namespace XEditNet.Licensing.LicenseProvider
{
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label lblMinDate;
		private System.Windows.Forms.Label lblMaxDate;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ComboBox cbRelease;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox cbProduct;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox cbLicenseType;
		private System.Windows.Forms.Button btnGenerateKey;
		private System.Windows.Forms.Button btnParseKey;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox tbKey;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox tbKeyNumber;
		private System.Windows.Forms.MonthCalendar calDate;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.lblMinDate = new System.Windows.Forms.Label();
			this.lblMaxDate = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.calDate = new System.Windows.Forms.MonthCalendar();
			this.label6 = new System.Windows.Forms.Label();
			this.cbRelease = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.cbProduct = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.cbLicenseType = new System.Windows.Forms.ComboBox();
			this.btnGenerateKey = new System.Windows.Forms.Button();
			this.btnParseKey = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.tbKey = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.tbKeyNumber = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblMinDate
			// 
			this.lblMinDate.Location = new System.Drawing.Point(248, 32);
			this.lblMinDate.Name = "lblMinDate";
			this.lblMinDate.Size = new System.Drawing.Size(64, 16);
			this.lblMinDate.TabIndex = 0;
			this.lblMinDate.Text = "lblMinDate";
			// 
			// lblMaxDate
			// 
			this.lblMaxDate.Location = new System.Drawing.Point(312, 32);
			this.lblMaxDate.Name = "lblMaxDate";
			this.lblMaxDate.Size = new System.Drawing.Size(64, 16);
			this.lblMaxDate.TabIndex = 1;
			this.lblMaxDate.Text = "lblMaxDate";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(312, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "License key expires at end of day of specified expiry date.";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(232, 16);
			this.label2.TabIndex = 3;
			this.label2.Text = "Expiry / Registration Dates must be between:";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.calDate);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.cbRelease);
			this.groupBox1.Controls.Add(this.label5);
			this.groupBox1.Controls.Add(this.cbProduct);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.cbLicenseType);
			this.groupBox1.Location = new System.Drawing.Point(16, 64);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(448, 208);
			this.groupBox1.TabIndex = 13;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Key Properties";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 16);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(176, 16);
			this.label4.TabIndex = 27;
			this.label4.Text = "Expiry Date / Registration Date:";
			// 
			// calDate
			// 
			this.calDate.Location = new System.Drawing.Point(8, 32);
			this.calDate.Name = "calDate";
			this.calDate.TabIndex = 26;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(232, 120);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(80, 24);
			this.label6.TabIndex = 25;
			this.label6.Text = "Release:";
			// 
			// cbRelease
			// 
			this.cbRelease.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbRelease.Location = new System.Drawing.Point(320, 120);
			this.cbRelease.Name = "cbRelease";
			this.cbRelease.Size = new System.Drawing.Size(120, 21);
			this.cbRelease.TabIndex = 24;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(232, 88);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(80, 16);
			this.label5.TabIndex = 23;
			this.label5.Text = "Product:";
			// 
			// cbProduct
			// 
			this.cbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbProduct.Location = new System.Drawing.Point(320, 88);
			this.cbProduct.Name = "cbProduct";
			this.cbProduct.Size = new System.Drawing.Size(120, 21);
			this.cbProduct.TabIndex = 22;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(232, 56);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(88, 16);
			this.label3.TabIndex = 21;
			this.label3.Text = "License Type:";
			// 
			// cbLicenseType
			// 
			this.cbLicenseType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbLicenseType.Location = new System.Drawing.Point(320, 56);
			this.cbLicenseType.Name = "cbLicenseType";
			this.cbLicenseType.Size = new System.Drawing.Size(120, 21);
			this.cbLicenseType.TabIndex = 20;
			this.cbLicenseType.SelectedIndexChanged += new System.EventHandler(this.cbLicenseType_SelectedIndexChanged);
			// 
			// btnGenerateKey
			// 
			this.btnGenerateKey.Location = new System.Drawing.Point(472, 72);
			this.btnGenerateKey.Name = "btnGenerateKey";
			this.btnGenerateKey.Size = new System.Drawing.Size(96, 24);
			this.btnGenerateKey.TabIndex = 14;
			this.btnGenerateKey.Text = "Generate Key";
			this.btnGenerateKey.Click += new System.EventHandler(this.btnGenerateKey_Click);
			// 
			// btnParseKey
			// 
			this.btnParseKey.Location = new System.Drawing.Point(480, 296);
			this.btnParseKey.Name = "btnParseKey";
			this.btnParseKey.Size = new System.Drawing.Size(96, 24);
			this.btnParseKey.TabIndex = 15;
			this.btnParseKey.Text = "Parse Key";
			this.btnParseKey.Click += new System.EventHandler(this.btnParseKey_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.tbKey);
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Location = new System.Drawing.Point(16, 280);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(448, 56);
			this.groupBox2.TabIndex = 16;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Key";
			// 
			// tbKey
			// 
			this.tbKey.Location = new System.Drawing.Point(72, 24);
			this.tbKey.Name = "tbKey";
			this.tbKey.Size = new System.Drawing.Size(360, 20);
			this.tbKey.TabIndex = 1;
			this.tbKey.Text = "";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(8, 24);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(56, 16);
			this.label7.TabIndex = 0;
			this.label7.Text = "Key:";
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(16, 344);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(72, 16);
			this.label8.TabIndex = 17;
			this.label8.Text = "Key Number:";
			// 
			// tbKeyNumber
			// 
			this.tbKeyNumber.Location = new System.Drawing.Point(88, 344);
			this.tbKeyNumber.Name = "tbKeyNumber";
			this.tbKeyNumber.ReadOnly = true;
			this.tbKeyNumber.Size = new System.Drawing.Size(360, 20);
			this.tbKeyNumber.TabIndex = 18;
			this.tbKeyNumber.Text = "";
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(592, 374);
			this.Controls.Add(this.tbKeyNumber);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.btnParseKey);
			this.Controls.Add(this.btnGenerateKey);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.lblMaxDate);
			this.Controls.Add(this.lblMinDate);
			this.Name = "Form1";
			this.Text = "ioko Product License Generator";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}
		private void Form1_Load(object sender, System.EventArgs e)
		{
			foreach ( string s in Enum.GetNames(typeof(XEditNet.Licensing.LicenseType.LicenseTypes)) )
				cbLicenseType.Items.Add(s);

			cbLicenseType.SelectedIndex = 0;

			foreach ( string s in Enum.GetNames(typeof(XEditNet.Licensing.Product.Products)) )
				cbProduct.Items.Add(s);
			cbProduct.SelectedIndex = 0;

			foreach ( string s in Enum.GetNames(typeof(XEditNet.Licensing.Release.Releases)) )
				cbRelease.Items.Add(s);
			cbRelease.SelectedIndex = 0;

			calDate.MinDate = LicenseDate.MinimumValue;
			calDate.MaxDate = LicenseDate.MaximumValue;
			calDate.MaxSelectionCount = 1;

			lblMinDate.Text = LicenseDate.MinimumValue.ToShortDateString();
			lblMaxDate.Text = LicenseDate.MaximumValue.ToShortDateString();
		}

		private void btnGenerateKey_Click(object sender, System.EventArgs e)
		{
			LicenseBase licenseBase = new LicenseBase();

			switch (cbLicenseType.SelectedIndex)
			{
				case 0:
					licenseBase.LicenseObject.Type = LicenseType.LicenseTypes.None;
					break;

				case 1:
					licenseBase.LicenseObject.Type = LicenseType.LicenseTypes.Trial;
					licenseBase.ExpiryDate.LicenseDateTime = calDate.SelectionStart;
					break;

				case 2:
					licenseBase.LicenseObject.Type = LicenseType.LicenseTypes.Full;
					licenseBase.RegistrationDate.LicenseDateTime = calDate.SelectionStart;
					break;

				case 3:
					licenseBase.LicenseObject.Type = LicenseType.LicenseTypes.Invalid;
					break;

				default:
					throw new Exception("Key generation error");
			}

			switch (cbProduct.SelectedIndex)
			{
				case 0:
					licenseBase.ProductObject.ProductCode = Product.Products.None;
					break;
				case 1:
					licenseBase.ProductObject.ProductCode = Product.Products.XEditNetCtrl;
					break;
				default:
					throw new Exception("Key generation error");
			}
			
			licenseBase.ReleaseObject.Value = (int) Enum.Parse(typeof(Release.Releases), cbRelease.SelectedItem.ToString());

			tbKey.Text = licenseBase.LicenseKey;
			SetKeyNumber(licenseBase);
		}

		private void btnParseKey_Click(object sender, System.EventArgs e)
		{
			LicenseBase licenseBase = new LicenseBase();
			try
			{
				licenseBase.LicenseKey = tbKey.Text;
				SetKeyNumber(licenseBase);
				switch (licenseBase.LicenseObject.Type)
				{
					case LicenseType.LicenseTypes.Trial:
						cbLicenseType.SelectedIndex = 0;
						calDate.SelectionStart = licenseBase.ExpiryDate.LicenseDateTime;
						break;
					case LicenseType.LicenseTypes.Full:
						cbLicenseType.SelectedIndex = 1;
						calDate.SelectionStart = licenseBase.RegistrationDate.LicenseDateTime;
						break;
					case LicenseType.LicenseTypes.Invalid:
						cbLicenseType.SelectedIndex = 2;
						break;
					case LicenseType.LicenseTypes.None:
						cbLicenseType.SelectedIndex = 3;
						break;
					default:
						throw new Exception("Key parsing error");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			switch (licenseBase.ProductObject.ProductCode)
			{
				case Product.Products.None:
					cbProduct.SelectedIndex = 0;
					break;
				case Product.Products.XEditNetCtrl:
					cbProduct.SelectedIndex = 1;
					break;
				default:
					throw new Exception("Key parsing error");
			}
			cbRelease.SelectedIndex = licenseBase.ReleaseObject.Value;
		}

		private void SetKeyNumber(LicenseBase licenseBase)
		{
			LicenseRandomNumber[] numbers =  licenseBase.LicenceNumberObject;
			StringBuilder sb = new StringBuilder();
			foreach(LicenseRandomNumber number in numbers)
			{
				sb.AppendFormat("{0:000}-", number.Value);
			}
			string keyNumber = sb.ToString();
			tbKeyNumber.Text = keyNumber.Substring(0, keyNumber.Length - 1);
		}

		private void cbLicenseType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			calDate.Enabled = (cbLicenseType.SelectedIndex < 2);
			switch (cbLicenseType.SelectedIndex)
			{
				case 0:
					calDate.SelectionStart = DateTime.Now.AddDays(30);
					break;
				case 1:
					calDate.SelectionStart = DateTime.Now;
					break;
			}
		}
	}
}
