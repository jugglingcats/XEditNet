using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// Base definition of licenses' constinuent data types
	/// </summary>
	internal interface ILicenseItem
	{
		int DataSize	// Number of bit in data type
		{
			get;
		}

		int Value		// Value of data
		{
			get;
			set;
		}
	}
}
