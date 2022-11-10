using System;
using System.Collections.Generic;
using CitizenFX.Core;

namespace IRVDM
{
    class MainMenuLocation
    {
        public string Name { get; set; }
        public Vector3 PlayerPedPos { get; set; }
        public Vector3 CameraPos { get; set; }
        public Vector3 PlayerPedLookPos { get; set; }
        public float PlayerHeading { get; set; }
        public Vector3 CameraRot { get; set; }

        public Vector3 ChangeLoadOutPos { get; set; }
        public Vector3 PrimaryWeaponObjPos { get; set; }
        public Vector3 SeconderyWeaponObjPos { get; set; }
        public Vector3 EquipmentWeaponObjPos { get; set; }
        public Vector3 LoadotShowPlayPos  { get; set; }
        public Vector3 ChangeSkinCameraPos01  { get; set; }
        public Vector3 ChangeSkinCameraPos02 { get; set; }
        public Vector3 ChangeSkinCameraPos03 { get; set; }
        public Vector3 ChangeSkinCameraRot01 { get; set; }
        public Vector3 ChangeSkinCameraRot02 { get; set; }
        public Vector3 ChangeSkinCameraRot03 { get; set; }
        public int MaxTimeWaitForPlayerToReachChangeSkinPos { get; set; }
        public Vector3 ChangeSkinPlayerPos { get; set; }
        public float ChangeSkinPlayerHeding { get; set; }
        public float PropsHeading { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
