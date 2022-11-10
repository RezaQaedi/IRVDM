using CitizenFX.Core;

namespace IRVDM
{
    class CameraSceneFormat
    {
        public string Name { get; set; }
        public Camera Cam { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public bool IsShaking { get; set; } = false;
        public bool IsPointing { get; set; } = false;
        public bool IsTemp { get; set; } = false;
        public bool HaveEffect { get; set; } = false;
        public int Time { get; set; } = 0;
        public bool HaveSound { get; set; } = false;
        public string SounDic { get; set; }
        public string SoundName { get; set; }
        public int EaseTime { get; set; } = -1;
        public CitizenFX.Core.UI.ScreenEffect Effect { get; set; }
        public Entity EntityToPoint { get; set; }

        public CameraSceneFormat(string name, Vector3 pos, Vector3 rot)
        {
            Name = name;
            Position = pos;
            Rotation = rot;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
