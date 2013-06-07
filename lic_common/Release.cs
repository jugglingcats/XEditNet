using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// Product Release number
	/// </summary>
	internal class Release : ILicenseItem
	{
		#region Enumerations
		internal enum Releases : int
		{
			ALPHA = 0
		}
		#endregion

		#region Private state members
		private Releases release;
		#endregion

		#region Constructors
		internal Release(int Release)
		{
			Value = Release;
		}
		#endregion

		#region LicenseItem interface properties
		public int Value
		{
			get { return (int) release; }
			set { release = (Releases) value; }
		}

		public int DataSize
		{
			get { return 4; }
		}
		#endregion
	}
}
