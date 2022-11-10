namespace IRVDMShared
{
    public class TimeFormat
    {
        public int Hour { get; set; }
        public int Second { get; set; }
        public int Minutes { get; set; }
        public int MilliSecond { get; set; }

        public override string ToString()
        {
            string min;
            string sec;

            if (Minutes < 10 && Minutes >= 0)
                min = $"0{Minutes.ToString()}";
            else
                min = Minutes.ToString();

            if (Second < 10 && Second >= 0)
                sec = $"0{Second.ToString()}";
            else
                sec = Second.ToString();

            return $"{min}:{sec}";
        }

        public string ToString(bool MiliSecond)
        {
            return Minutes + ":" + Second + ":" + MilliSecond;
        }
    }
}