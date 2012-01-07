using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// The Product
	/// </summary>
	internal class Product : ILicenseItem
	{
		#region Enumerations
		internal enum Products : int
		{
			None = 0,
			XEditNetCtrl = 1
		}
		#endregion

		#region Private state members
		private Products product;
		#endregion

		#region Constructors
		internal Product(Products Product)
		{
			ProductCode = Product;
		}

		internal Product(int ProductValue)
		{
			Value = ProductValue;
		}
		#endregion

		#region Properties
		internal Products ProductCode
		{
			get { return product; }
			set
			{
				if (!Enum.IsDefined(typeof(Products), value))
					throw new Exception(String.Format("Illegal License operation: Products object threw an Out Of Bounds exception attempting to set a unrecognized Product code of {0}",
						value));

				product = value;
			}
		}
		#endregion

		#region LicenseItem interface properties
		public int Value
		{
			get { return (int)ProductCode; }
			set { ProductCode = (Products)value; }
		}

		public int DataSize
		{
			get { return 4; }
		}
		#endregion
	}
}
