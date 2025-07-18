using System.Globalization;

namespace Flee.PublicTypes
{
    public readonly struct Date
    {
        private const string DATE_ONLY_FORMAT = "yyyy-MM-dd";

        private const string DATE_ONLY_FORMAT_FLEXIBLE = "yyyy-M-d";

        public static bool TryParse(string input, out Date result)
        {
            if (System.DateTime.TryParseExact(input, new[] { DATE_ONLY_FORMAT, DATE_ONLY_FORMAT_FLEXIBLE },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                result = new Date(dt);
                return true;
            }
            result = new(1900, 1, 1);
            return false;
        }

        private readonly System.DateTime _value;

        private Date(System.DateTime dateTime)
        {
            _value = dateTime != default ? dateTime.Date : new(1900, 1, 1);
        }

        public int Year => _value.Year;

        public int Month => _value.Month;

        public int Day => _value.Day;

        public long Ticks => _value.Ticks;

        public Date(int year, int month, int day)
        {
            _value = new System.DateTime(year, month, day);
        }

        public Date(long ticks)
        {
            _value = ticks > 0 ? new System.DateTime(ticks).Date : new(1900, 1, 1);
        }

        public override bool Equals(object obj)
        {
            if (obj is System.DateTime sdt)
            {
                return _value.Equals(sdt);
            }
            if (obj is Date d)
            {
                return _value.Equals(d._value);
            }
            if (obj is DateTime dt)
            {
                if (dt.Hour > 0 || dt.Minute > 0 || dt.Second > 0)
                {
                    return false;
                }
                return dt.Year == _value.Year && dt.Month == _value.Month && dt.Day == _value.Day;
            }
            return false;
        }

        public static bool operator ==(Date left, Date right) => left.Equals(right);

        public static bool operator !=(Date left, Date right) => !(left == right);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString(DATE_ONLY_FORMAT);

        public static implicit operator System.DateTime(Date d) => d._value;

        public static implicit operator Date(System.DateTime dt) => new(dt);

        public static implicit operator Date(DateTime dt) => new(dt.Year, dt.Month, dt.Day);
    }
}