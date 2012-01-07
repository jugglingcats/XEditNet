using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// Base license object
	/// </summary>
	internal class LicenseBase
	{
		#region Constants
		private const int licenseRandomNumberCount = 9;
		#endregion

		#region Protected state members
		protected LicenseType licenseType;
		protected Release release;
		protected Product product;
		protected LicenseDate expiryDate;
		protected LicenseDate registrationDate;
		protected LicenseRandomNumber[] licenseRandomNumbers;
		#endregion

		#region Constructors
		internal LicenseBase()
		{
			InitializeDefaults();
		}

		internal LicenseBase(string Key)
		{
			InitializeDefaults();
			LicenseKey = Key;
		}

		private void InitializeDefaults()
		{
			licenseType = new LicenseType(LicenseType.LicenseTypes.None);
			product = new Product(Product.Products.None);
			expiryDate = new LicenseDate(0);
			registrationDate = new LicenseDate(0);
			release = new Release(1);

			// now for the random license number:
			System.Random randomGenerator = new System.Random();
			licenseRandomNumbers = new LicenseRandomNumber[licenseRandomNumberCount];
			for (int index = 0; index < licenseRandomNumberCount; index++)
				licenseRandomNumbers[index] = new LicenseRandomNumber(randomGenerator);
		}
		#endregion

		#region Properties
		internal LicenseType LicenseObject
		{
			get { return licenseType; }
		}

		internal Release ReleaseObject
		{
			get { return release; }
		}
	
		internal Product ProductObject
		{
			get { return product; }
		}

		internal LicenseRandomNumber[] LicenceNumberObject
		{
			get { return licenseRandomNumbers; }
		}

		internal LicenseDate ExpiryDate
		{
			get
			{
				if (licenseType.Type == LicenseType.LicenseTypes.Trial)
					return expiryDate;
				throw new Exception("Illegal License operation: LicenseBase object threw a general exception, the ExpiryDate property cannot be accessed for a non-trial license");
			}
		}

		internal LicenseDate RegistrationDate
		{
			get
			{
				if (licenseType.Type == LicenseType.LicenseTypes.Full)
					return registrationDate;
				throw new Exception("Illegal License operation: LicenseBase object threw a general exception, the RegistrationDate property cannot be accessed for license that is not full");
			}
		}

		/// <summary>
		/// Get generates new Seed
		/// Set sets Product, Release, LicenseType, Seed and Expiry or Purchase Date from key
		/// </summary>
		internal string LicenseKey
		{
			get
			{
				LicenseBaseByteSerializer lbbs = new LicenseBaseByteSerializer();

				lbbs.AppendData(ProductObject);
				lbbs.AppendData(LicenseObject);
				lbbs.AppendData(ReleaseObject);
				for (int index = 0; index < licenseRandomNumberCount; index++)
					lbbs.AppendData(licenseRandomNumbers[index]);

				switch(LicenseObject.Type)
				{
					case LicenseType.LicenseTypes.Trial:
						lbbs.AppendData(ExpiryDate);
						break;
					case LicenseType.LicenseTypes.Full:
						lbbs.AppendData(RegistrationDate);
						break;
				}

				byte[] ChecksumedData = LicenseChecksum.AppendChecksum(lbbs.ByteArray);
				byte[] ObfuscatedData = LicenseChecksumObfuscator.Obfuscate(ChecksumedData);

				return LicenseKeyConvertor.KeyFromByteArray(ObfuscatedData);
			}
			set
			{
				expiryDate.Value = 0;
				registrationDate.Value = 0;

				byte[] ObfuscatedData = LicenseKeyConvertor.ByteArrayFromKey(value.ToUpper());
				byte[] ChecksumedData = LicenseChecksumObfuscator.DeObfuscate(ObfuscatedData);
				byte[] RawData = LicenseChecksum.ValidateAndStripChecksum(ChecksumedData);

				LicenseBaseByteDeserializer lbbd = new LicenseBaseByteDeserializer(RawData);

				lbbd.ReadData(ProductObject);
				lbbd.ReadData(LicenseObject);
				lbbd.ReadData(ReleaseObject);
				for (int index = 0; index < licenseRandomNumberCount; index++)
					lbbd.ReadData(licenseRandomNumbers[index]);

				switch(LicenseObject.Type)
				{
					case LicenseType.LicenseTypes.Trial:
						lbbd.ReadData(ExpiryDate);
						break;
					case LicenseType.LicenseTypes.Full:
						lbbd.ReadData(RegistrationDate);
						break;
				}
			}
		}
		#endregion

	}
}
