using Dragonfly.Engine.Core;
using DragonflyUtils;
using System.Collections.Generic;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    internal enum AudioCommand { None = 0, Play, PlayLoop, Stop }

    public struct AudioEffect
    {
        public float DeltaDecibels;
        public float DeltaPitch;
    }

    public class CompAudio : Component, ICompUpdatable, ICompPausable
    {
        private DF_SoundInstance audio;
        private AudioCommand requestedCmd;
        private bool playingLoop;

        public CompAudio(Component parent, string audioName) : base(parent)
        {
            Effects = new List<Component<AudioEffect>>();
            audio = GetComponent<CompAudioEngine>().CreateAudioInstance(Context.GetResourcePath(audioName));
            VolumeDecibels = 0;
            SoundPosition = new CompValue<Float3>(this, Float3.Zero);
        }

        public float VolumeDecibels { get; set; }

        public UpdateType NeededUpdates { get { return (requestedCmd != AudioCommand.None || IsPlaying) ? UpdateType.FrameStart1 : UpdateType.None; } }

        public CompValue<Float3> SoundPosition { get; private set; }

        public void Play()
        {
            requestedCmd = AudioCommand.Play;
        }

        public void PlayLoop()
        {
            requestedCmd = AudioCommand.PlayLoop;
        }

        public void Stop()
        {
            requestedCmd = AudioCommand.Stop;
        }

        public bool IsPlaying { get; private set; }

        public bool Is3DLocated { get; set; }

        public float SourceSize { get; set; }

        public void Update(UpdateType updateType)
        {
            switch (requestedCmd)
            {
                case AudioCommand.Play:
                    audio.Play(false);
                    IsPlaying = true;
                    break;

                case AudioCommand.PlayLoop:
                    audio.Play(true);
                    IsPlaying = true;
                    playingLoop = true;
                    break;

                case AudioCommand.Stop:
                    audio.Stop();
                    IsPlaying = false;
                    playingLoop = false;
                    break;
            }

            requestedCmd = AudioCommand.None;

            if(IsPlaying)
            {
                // accumulate this instance volume with other audio effects
                float volume = VolumeDecibels, pitch = 0;
                foreach(Component<AudioEffect> e in Effects)
                {
                    AudioEffect evalue = e.GetValue();
                    volume += evalue.DeltaDecibels;
                    pitch += evalue.DeltaPitch;
                }

                audio.SetVolume(volume);
                audio.SetPitch(pitch);

                if (Is3DLocated)
                {
                    // apply 3d stereo effect
                    CompCamera mainCamera = Context.GetModule<BaseMod>().MainPass.Camera;
                    Float3 listenerDir = mainCamera.Direction, listenerPos = mainCamera.LocalPosition;
                    Float3 soundPos = SoundPosition.GetValue() * GetTransform().ToFloat4x4(mainCamera.GetTransform().Tile);
                    audio.SetPan3D(
                        listenerDir.X, listenerDir.Y, listenerDir.Z,
                        listenerPos.X, listenerPos.Y, listenerPos.Z,
                        soundPos.X, soundPos.Y, soundPos.Z);
                }
            }
        }

        public void Pause()
        {
            if (IsPlaying) audio.Stop();
            requestedCmd = AudioCommand.None;
            IsPlaying = false;
        }

        public void Resume()
        {
            if (playingLoop)
                requestedCmd = AudioCommand.PlayLoop;
        }

        public List<Component<AudioEffect>> Effects { get; private set; }
    }
}
