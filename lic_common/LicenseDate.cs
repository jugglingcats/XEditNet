using System;

namespace XEditNet.Licensing
{
	/// <summary>
	/// Converts expiry date into numeric value
	/// </summary>
	internal class LicenseDate : ILicenseItem
	{
		#region Constants
		private const int startYear = 2004;
		private const int dataSize = 12;	// number of bits in numeric result
		#endregion

		#region Private state members
		private int numericDateValue;
		#endregion

		#region Constructors
		internal LicenseDate(DateTime LicenseDate)
		{
			LicenseDateTime = LicenseDate;
		}

		internal LicenseDate(int NumericDate)
		{
			Value = NumericDate;
		}
		#endregion

		#region Properties
		internal DateTime LicenseDateTime
		{
			get { return EndOfDay(MinimumValue.AddDays(Value)); }
			set
			{
				if (value.Year < startYear)
					throw new Exception(String.Format("Illegal License operation: LicenseDate object threw an Out Of Bounds exception setting LicenseDateTime year to {0} - minimum permitted value is {1}",
						value.Year, startYear));

				int NumericDate = -1;	// 0-based day count: set cumulative counter to -1 so 1st day = 0
				while (value.Year >= startYear)
				{
					NumericDate += value.DayOfYear;
					value = value.Subtract(new System.TimeSpan(value.DayOfYear, value.Hour, value.Minute, value.Millisecond));
				}
				Value = NumericDate;
			}
		}
		#endregion

		#region LicenseItem interface properties
		public int DataSize
		{
			get { return dataSize; }
		}

		public int Value
		{
			get { return numericDateValue; }
			set
			{
				if (value > MaximumDateValue)
					throw new Exception(String.Format("Illegal License operation: LicenseDate object threw an Out Of Bounds exception setting NumericDateValue to {0} - maximum permitted value is {1}",
						value, MaximumDateValue));

				numericDateValue = value;
			}
		}
		#endregion

		#region Date manipulation private static members
		private static DateTime EndOfDay(DateTime Date)
		{
			return Date.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
		}
		#endregion

		#region Value range static members
		internal static DateTime MinimumValue
		{
			get
			{
				return new DateTime(startYear, 1, 1);
			}
		}
	
		internal static DateTime MaximumValue
		{
			get { return  EndOfDay(MinimumValue.AddDays(MaximumDateValue)); }
		}

		private static int MaximumDateValue
		{
			get { return (1 << dataSize) - 1; }
		}
		#endregion
	}
}
