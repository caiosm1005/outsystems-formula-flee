using System.Globalization;

namespace Flee.PublicTypes
{
    public readonly struct DateTime
    {
        private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        
        private const string DATE_TIME_FORMAT_FLEXIBLE = "yyyy-M-d H:m:s";

        public static bool TryParse(string input, out DateTime result)
        {
            if (System.DateTime.TryParseExact(input, new[] { DATE_TIME_FORMAT, DATE_TIME_FORMAT_FLEXIBLE },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                result = new DateTime(dt);
                return true;
            }
            result = new(1900, 1, 1, 0, 0, 0);
            return false;
        }

        private readonly System.DateTime _value;

        private DateTime(System.DateTime dateTime)
        {
            _value = dateTime != default ? dateTime : new(1900, 1, 1);
        }

        public int Year => _value.Year;

        public int Month => _value.Month;

        public int Day => _value.Day;

        public int Hour => _value.Hour;

        public int Minute => _value.Minute;

        public int Second => _value.Second;

        public long Ticks => _value.Ticks;

        public DateTime(int year, int month, int day, int hour, int minute, int second)
        {
            _value = new System.DateTime(year, month, day, hour, minute, second);
        }

        public DateTime(long ticks)
        {
            if (ticks > 0)
            {
                var dt = new System.DateTime(ticks);
                _value = new System.DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
            }
            else
            {
                _value = new(1900, 1, 1, 0, 0, 0);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is System.DateTime sdt)
            {
                return _value.Equals(sdt);
            }
            if (obj is DateTime dt)
            {
                return _value.Equals(dt._value);
            }
            if (obj is Date d)
            {
                if (_value.TimeOfDay.Seconds > 0)
                {
                    return false;
                }
                return d.Year == _value.Year && d.Month == _value.Month && d.Day == _value.Day;
            }
            return false;
        }

        public static bool operator ==(DateTime left, DateTime right) => left.Equals(right);

        public static bool operator !=(DateTime left, DateTime right) => !(left == right);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString(DATE_TIME_FORMAT);

        public static implicit operator System.DateTime(DateTime dt) => dt._value;

        public static implicit operator DateTime(System.DateTime dt) => new(dt);

        public static implicit operator DateTime(Date d) => new(d.Year, d.Month, d.Day, 0, 0, 0);
    }
}