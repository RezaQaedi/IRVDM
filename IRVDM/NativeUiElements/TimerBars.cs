using CitizenFX.Core.UI;
using IRVDM;
using System.Collections.Generic;
using System.Drawing;
using Font = CitizenFX.Core.UI.Font;


namespace NativeUI
{
    public abstract class TimerBarBase
    {
        public string Label { get; set; }

        public TimerBarBase(string label)
        {
            Label = label;
        }

        public virtual void Draw(int interval)
        {
            SizeF res = BaseUI.GetScreenResolutionMaintainRatio();
            PointF safe = BaseUI.GetSafezoneBounds();
            new UIResText(Label, new PointF((int)res.Width - safe.X + 410, (int)res.Height - safe.Y - (-290 + (4 * interval))), 0.3f, UnknownColors.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();

            new NativeUI.Sprite("timerbars", "all_black_bg", new PointF((int)res.Width - safe.X + 300, (int)res.Height - safe.Y - (-280 + (4 * interval))), new SizeF(300, 37), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();
            Screen.Hud.HideComponentThisFrame(HudComponent.AreaName);
            Screen.Hud.HideComponentThisFrame(HudComponent.StreetName);
            Screen.Hud.HideComponentThisFrame(HudComponent.VehicleName);
        }
    }

    public class TextTimerBar : TimerBarBase
    {
        public string Text { get; set; }

        public TextTimerBar(string label, string text) : base(label)
        {
            Text = text;
        }

        public override void Draw(int interval)
        {
            SizeF res = BaseUI.GetScreenResolutionMaintainRatio();
            PointF safe = BaseUI.GetSafezoneBounds();

            base.Draw(interval);
            new UIResText(Text, new PointF((int)res.Width - safe.X + 520, (int)res.Height - safe.Y - (-279 + (4 * interval))), 0.5f, UnknownColors.White, Font.ChaletLondon, UIResText.Alignment.Right).Draw();
        }
    }

    public class BarTimerBar : TimerBarBase
    {
        public float _Percentage = 0;
        /// <summary>
        /// Bar percentage. Goes from 0 to 1.
        /// </summary>
        public float Percentage
        {
            get { return _Percentage; }
            set
            {
                if (value > 0 && value < 1f)
                {
                    _Percentage = value;
                }
                else if (value < 0)
                    _Percentage = 0;
                else if (value > 1f)
                    _Percentage = 1f;
            }
        }

        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }

        public BarTimerBar(string label) : base(label)
        {
            BackgroundColor = UnknownColors.DarkRed;
            ForegroundColor = UnknownColors.Red;
        }

        public override void Draw(int interval)
        {
            SizeF res = BaseUI.GetScreenResolutionMaintainRatio();
            PointF safe = BaseUI.GetSafezoneBounds();

            base.Draw(interval);

            var start = new PointF((int)res.Width - safe.X + 420, (int)res.Height - safe.Y - (-293 + (4 * interval)));

            new UIResRectangle(start, new SizeF(150, 15), BackgroundColor).Draw();
            new UIResRectangle(start, new SizeF((int)(150 * _Percentage), 15), ForegroundColor).Draw();
        }
    }

    public class TimerBarPool
    {
        private static List<TimerBarBase> _bars = new List<TimerBarBase>();

        public TimerBarPool()
        {
            _bars = new List<TimerBarBase>();
        }

        public List<TimerBarBase> ToList()
        {
            return _bars;
        }

        public void Add(TimerBarBase timer)
        {
            _bars.Add(timer);
        }

        public void Remove(TimerBarBase timer)
        {
            _bars.Remove(timer);
        }

        public void Draw()
        {
            for (int i = 0; i < _bars.Count; i++)
            {
                _bars[i].Draw(i * 10);
            }
        }
    }
}
