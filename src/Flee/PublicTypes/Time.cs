using System.Text.RegularExpressions;

namespace Flee.PublicTypes
{
    public readonly struct Time
    {
        private static readonly Regex _regex = new(@"^(\d?\d):(\d?\d):(\d?\d)$");

        public static bool TryParse(string input, out Time result)
        {
            Match match = _regex.Match(input);
            if (!match.Success || match.Groups.Count != 4)
            {
                result = default;
                return false;
            }

            int hour = int.Parse(match.Groups[1].Value);
            int minute = int.Parse(match.Groups[2].Value);
            int second = int.Parse(match.Groups[3].Value);

            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                result = default;
                return false;
            }

            result = new Time(hour, minute, second);
            return true;
        }
        
        public readonly int Hour;

        public readonly int Minute;

        public readonly int Second;

        public int TotalSeconds => Hour * 3600 + Minute * 60 + Second;

        public Time(int hour, int minute, int second)
        {
            if (hour < 0 || hour > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(hour), "Hour must be between 0 and 23.");
            }
            if (minute < 0 || minute > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(minute), "Minute must be between 0 and 59.");
            }
            if (second < 0 || second > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(second), "Second must be between 0 and 59.");
            }
            Hour = hour;
            Minute = minute;
            Second = second;
        }

        public Time(int totalSeconds)
        {
            Hour = totalSeconds / 3600;
            Minute = totalSeconds % 3600 / 60;
            Second = totalSeconds % 60;
        }

        public override bool Equals(object obj)
        {
            if (obj is Time t)
            {
                return t.Hour == Hour && t.Minute == Minute && t.Second == Second;
            }
            return false;
        }
        
        public static bool operator ==(Time left, Time right) => left.Equals(right);

        public static bool operator !=(Time left, Time right) => !(left == right);

        public override int GetHashCode() => unchecked(Hour.GetHashCode() * 93);

        public override string ToString() => $"{Hour:D2}:{Minute:D2}:{Second:D2}";
        
        public static implicit operator System.DateTime(Time dt) => new(1900, 1, 1, dt.Hour, dt.Minute, dt.Second);

        public static implicit operator Time(System.DateTime dt) => new(dt.Hour, dt.Minute, dt.Second);
    }
}