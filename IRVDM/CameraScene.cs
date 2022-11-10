using CitizenFX.Core;
using CitizenFX.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IRVDM
{
    /// <summary>
    /// Time based camera switching 
    /// </summary>
    class CameraScene
    {
        public enum SceneState
        {
            None,
            IsInProgresse,
            HasFinished
        }

        public string Name { get; set; }
        public SceneState State { get; private set; } = SceneState.None;

        /// <summary>
        /// if this scene is one and there is no other scenes
        /// </summary>
        public bool IsFixed { get; private set; } = true;
        public CameraSceneFormat Current { get; private set; }

        public readonly List<CameraSceneFormat> ScenesList;

        /// <summary>
        /// </summary>
        /// <param name="scenes"> list of scene you want to switch must be in order</param>
        public CameraScene(List<CameraSceneFormat> scenes, string scenesName)
        {
            if (scenes.Count() > 1)
            {
                IsFixed = false;
            }

            ScenesList = scenes;
            Name = scenesName;
        }

        public CameraScene(CameraSceneFormat scene, string scenesName)
        {
            ScenesList = new List<CameraSceneFormat>();
            ScenesList.Add(scene);
            Name = scenesName;
        }

        /// <summary>
        /// starts switching between first scene to the last one (you must render camera first)
        /// </summary>
        /// <returns></returns>
        public async Task StartCameraScenes()
        {
            foreach (CameraSceneFormat scene in ScenesList)
            {
                Debug.WriteLine($"Starting to Change of the scene for {scene.Name}");
                await SwitchTo(scene);

                State = SceneState.IsInProgresse;

                if (scene.IsTemp)
                    return;

                await BaseScript.Delay(scene.Time);
            }

            State = SceneState.HasFinished;
        }

        private async Task SwitchTo(CameraSceneFormat scene)
        {
            if (scene.EaseTime != -1)
            {
                int gameTimer = Game.GameTime;
                //int lastCam = ScenesList.IndexOf(scene);
                float Camx = scene.Cam.Position.X;
                float Camy = scene.Cam.Position.Y;
                float Camz = scene.Cam.Position.Z;

                float nexCamX = scene.Position.X;
                float nexCamY = scene.Position.Y;
                float nexCamZ = scene.Position.Z;

                float div = scene.EaseTime / 1000;
                float difx = (nexCamX - Camx) / div;
                float dify = (nexCamY - Camy) / div;
                float difz = (nexCamZ - Camz) / div;

                float sq = scene.Cam.Position.LengthSquared();
                float sq2 = scene.Position.LengthSquared();

                float rotCamx = scene.Cam.Position.X;
                float rotCamy = scene.Cam.Position.Y;
                float rotCamz = scene.Cam.Position.Z;

                float rotNexCamX = scene.Position.X;
                float rotNexCamY = scene.Position.Y;
                float rotNexCamZ = scene.Position.Z;

                float rotDifx = (rotNexCamX - rotCamx) / (div / 6);
                float rotDify = (rotNexCamY - rotCamy) / (div / 6);
                float rotDifz = (rotNexCamZ - rotCamz) / (div / 6);

                while (Math.Sqrt(scene.Cam.Position.DistanceToSquared(scene.Position)) >= 0.05d)
                {
                    //Debug.WriteLine($"sqrt {Math.Sqrt(scene.Cam.Position.DistanceToSquared(scene.Position))}, squared {scene.Cam.Position.DistanceToSquared(scene.Position)}");
                    scene.Cam.Position += new Vector3(difx * Game.LastFrameTime, dify * Game.LastFrameTime, difz * Game.LastFrameTime);
                    if (Math.Sqrt(scene.Cam.Rotation.DistanceToSquared(scene.Rotation)) >= 0.3d)
                        scene.Cam.Rotation += new Vector3(rotDifx * Game.LastFrameTime, rotDify * Game.LastFrameTime, rotDifz * Game.LastFrameTime);
                    if (Game.GameTime >= gameTimer + div * 1000 + 30) break;
                    await BaseScript.Delay(0);
                }

                Debug.WriteLine($"REACHED IN {(float)(Game.GameTime - gameTimer) / 1000}");
            }

            scene.Cam.Position = scene.Position;
            scene.Cam.Rotation = scene.Rotation;

            if (scene.HaveEffect != false)
                Screen.Effects.Start(scene.Effect, 1000);

            if (scene.IsPointing)
                scene.Cam.PointAt(scene.EntityToPoint);
            else
                scene.Cam.StopPointing();

            if (scene.IsShaking)
                scene.Cam.Shake(CameraShake.SmallExplosion, 1.0f);
            else
                scene.Cam.StopShaking();

            if (scene.HaveSound)
                Game.PlaySound(scene.SounDic, scene.SoundName);

            Current = scene;
        }

        public async void Goto(string name) => await SwitchTo(ScenesList.Find(p => p.Name == name));
    }
}
