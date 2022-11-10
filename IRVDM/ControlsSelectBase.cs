using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace IRVDM
{
    class ControlsSelectBase
    {
        public string Name { get; set; } = "";
        public int SelectedIndex { get; private set; } = 0;
        public State VHState { get; set; } = State.verticalhorizantal;

        public int MaxItem { get; set; } = 6;
        public int MaxRows { get; set; } = 2;
        public int MaxItemsInRows { get; set; } = 3;

        public Dictionary<PosbileControl, Control> Controls = new Dictionary<PosbileControl, Control>()
        {
            [PosbileControl.up] = Control.PhoneUp,
            [PosbileControl.down] = Control.PhoneDown,
            [PosbileControl.back] = Control.PhoneLeft,
            [PosbileControl.Next] = Control.PhoneRight,
            [PosbileControl.select] = Control.SkipCutscene
        };

        public event Action<ControlsSelectBase, int, int> OnIndexChanged;
        public event Action<ControlsSelectBase, int> OnItemSelected;

        public enum PosbileControl
        {
            up,
            down,
            Next,
            back,
            select
        }

        public enum State
        {
            vertical,
            horizantal,
            verticalhorizantal,
        }

        public async void Process()
        {
            switch (VHState)
            {
                case State.vertical:
                    break;
                case State.horizantal:
                    break;
                case State.verticalhorizantal:
                    List<int> upest = new List<int>();
                    List<int> downest = new List<int>();

                    for (int i = 1; i <= MaxItemsInRows; i++)
                    {
                        upest.Add(MaxItemsInRows - i);
                        downest.Add((MaxItemsInRows * MaxRows) - i);
                    }

                    //not fully working (MAX ITEM MUST BE SAME VALE AS ROW * MAX ITEM IN ROW)
                    if ((MaxRows * MaxItemsInRows) > MaxItem)
                    {
                        int max = (MaxRows * MaxItemsInRows);
                        int diff = max - MaxItem;
                        for (int i = max; i > MaxItem; i--)
                        {
                            downest.Remove(i - 1);
                            downest.Add(i - MaxItemsInRows - 1);
                        }
                    }


                    if (Game.IsControlJustPressed(0, Controls[PosbileControl.back]))
                    {
                        if (SelectedIndex == 0)
                        {
                            int newIndex = MaxItem - 1;
                            OnIndexChanged?.Invoke(this, newIndex, SelectedIndex);
                            SelectedIndex = newIndex;
                        }
                        else
                        {
                            OnIndexChanged?.Invoke(this, SelectedIndex - 1, SelectedIndex);
                            SelectedIndex--;
                        }

                        await BaseScript.Delay(50);
                    }
                    if (Game.IsControlJustPressed(0, Controls[PosbileControl.Next]))
                    {
                        if (SelectedIndex == MaxItem - 1)
                        {
                            int newIndex = 0;
                            OnIndexChanged?.Invoke(this, newIndex, SelectedIndex);
                            SelectedIndex = newIndex;
                        }
                        else
                        {
                            OnIndexChanged?.Invoke(this, SelectedIndex + 1, SelectedIndex);
                            SelectedIndex++;
                        }

                        await BaseScript.Delay(50);
                    }
                    if (Game.IsControlJustPressed(0, Controls[PosbileControl.up]))
                    {
                        //if (upest.Contains(SelectedIndex))
                        //{
                        //    int newIndex = SelectedIndex + ((MaxRows * MaxItemsInRows) - MaxItemsInRows);
                        //    OnIndexChanged?.Invoke(this, newIndex, SelectedIndex);
                        //    SelectedIndex = newIndex;
                        //}
                        if (!upest.Contains(SelectedIndex))
                        {
                            int newIndex = SelectedIndex - MaxItemsInRows;
                            OnIndexChanged?.Invoke(this, newIndex, SelectedIndex);
                            SelectedIndex = newIndex;
                        }

                        await BaseScript.Delay(50);
                    }
                    if (Game.IsControlJustPressed(0, Controls[PosbileControl.down]))
                    {
                        //if (downest.Contains(SelectedIndex))
                        //{
                        //    int newIndex = SelectedIndex - ((MaxRows * MaxItemsInRows) - MaxItemsInRows);
                        //    OnIndexChanged?.Invoke(this, newIndex, SelectedIndex);
                        //    SelectedIndex = newIndex;
                        //}
                        if (!downest.Contains(SelectedIndex))
                        {
                            int newIndex = SelectedIndex + MaxItemsInRows;
                            OnIndexChanged?.Invoke(this, newIndex, SelectedIndex);
                            SelectedIndex = newIndex;
                        }

                        await BaseScript.Delay(50);
                    }
                    if (Game.IsControlJustPressed(0, Controls[PosbileControl.select]))
                    {
                        OnItemSelected?.Invoke(this, SelectedIndex);
                    }

                    break;
            }
        }
    }
}
