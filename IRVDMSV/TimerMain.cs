using CitizenFX.Core;
using IRVDMShared;
using System;
using static CitizenFX.Core.Native.API;

namespace IRVDMSV
{
    internal class TimerMain
    {
        #region Fields and props

        public long MaxTimeToSpendInMilliSecond { get; private set; }
        public long GameTimerTimeToReach { get; private set; }
        public bool HasFinished { get; private set; }
        private bool _IsRunning;
        public bool IsRunning
        {
            get
            {
                if (HasFinished)
                    return false;
                else
                    return _IsRunning;
            }
            private set { _IsRunning = value; }
        }
        private bool HasDisposed = false;

        public TimeFormat CurrentTimer = new TimeFormat();

        public event Action OnTimerEnd;
        public event Action OnTimerDisposed;

        #endregion

        #region Methods

        /// <summary>
        /// dispose The timer
        /// </summary>
        public void StopTimerNow()
        {
            HasDisposed = true;
        }

        public void StartTimerNow(int ms)
        {
            MaxTimeToSpendInMilliSecond = ms;
            CalculateTime(MaxTimeToSpendInMilliSecond);
            StartTimer();
        }

        /// <summary>
        /// Calcaulate the Time We have to reach
        /// </summary>
        /// <param name="gametimertime"></param>
        private void CalculateTime(long gametimertime)
        {
            GameTimerTimeToReach = GetGameTimer() + MaxTimeToSpendInMilliSecond;
        }

        private async void StartTimer()
        {
            long CurrectGameTimer = GetGameTimer();
            int diffrence = Convert.ToInt32(GameTimerTimeToReach) - Convert.ToInt32(CurrectGameTimer);
            HasFinished = false;
            IsRunning = true;

            while (true)
            {
                await BaseScript.Delay(1000);
                CurrectGameTimer = GetGameTimer();
                diffrence = Convert.ToInt32(GameTimerTimeToReach) - Convert.ToInt32(CurrectGameTimer);

                CurrentTimer = ConvertTime(diffrence);

                if (CurrectGameTimer >= GameTimerTimeToReach)
                {
                    CurrentTimer.MilliSecond = 0;
                    OnTimerEnd?.Invoke();
                    IsRunning = false;
                    HasFinished = true;
                    break;
                }

                if (HasDisposed)
                {
                    OnTimerDisposed?.Invoke();
                    HasDisposed = false;
                    IsRunning = false;
                    break;
                }
            }
        }

        private TimeFormat ConvertTime(int ms)
        {
            TimeFormat Temp = new TimeFormat();
            Temp.MilliSecond = ms;

            if (ms / 1000 >= 60)
            {
                Temp.Second = (ms / 1000) % 60;
            }
            else
            {
                Temp.Second = ms / 1000;
            }

            Temp.Minutes = ms / 60000;

            return Temp;
        }
        #endregion
    }
}
