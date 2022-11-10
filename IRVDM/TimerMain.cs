using System;
using static CitizenFX.Core.Native.API;
using IRVDMShared;

namespace IRVDM
{
    class TimerMain
    {
        #region Fields and props

        public int MaxTimeToSpendInMilliSecond { get; private set; }
        public int GameTimerTimeToReach { get; private set; }
        public bool IsFinsihed { get; private set; }
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
            CalculateTime();
            StartTimer();
        }

        /// <summary>
        /// Calcaulate the Time We have to reach
        /// </summary>
        private void CalculateTime()
        {
            GameTimerTimeToReach = GetNetworkTime() + MaxTimeToSpendInMilliSecond;
        }

        private async void StartTimer()
        {
            var CurrectGameTimer = GetNetworkTime();
            var diffrence = GameTimerTimeToReach - CurrectGameTimer;
            IsFinsihed = false;

            while (true)
            {
                await CommonFunctions.Delay(0);
                CurrectGameTimer = GetNetworkTime();
                diffrence = GameTimerTimeToReach - CurrectGameTimer;

                CurrentTimer = ConvertTime(diffrence);

                if (CurrectGameTimer >= GameTimerTimeToReach)
                {
                    CurrentTimer.MilliSecond = 0;
                    OnTimerEnd?.Invoke();

                    IsFinsihed = true;
                    break;
                }

                if (HasDisposed)
                {
                    OnTimerDisposed?.Invoke();
                    HasDisposed = false;
                    break;
                }
            }
        }

        private TimeFormat ConvertTime(int ms)
        {
            TimeFormat Temp = new TimeFormat
            {
                MilliSecond = ms
            };

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
