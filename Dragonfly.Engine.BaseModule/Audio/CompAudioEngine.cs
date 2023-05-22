using Dragonfly.Engine.Core;
using DragonflyUtils;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class CompAudioEngine : Component, ICompUpdatable, ICompPausable
    {
        private class AudioCache
        {
            public DF_SoundEffect Audio;
            public int RefCount;
            public float LastUsed;
        }

        private bool suspended;
        private Dictionary<string, AudioCache> loadedSounds;

        internal CompAudioEngine(Component parent) : base(parent)
        {
            Engine = new DF_AudioEngine();
            loadedSounds = new Dictionary<string, AudioCache>();
        }

        internal DF_AudioEngine Engine { get; private set; }

        public UpdateType NeededUpdates { get { return !suspended ? UpdateType.FrameStart1 : UpdateType.None; } }

        public bool Paused 
        { 
            get 
            { 
                return suspended; 
            }
            set 
            {
                if (value)
                    Pause();
                else
                    Resume();
            }
        }

        public void Pause()
        {
            Engine.Suspend();
            suspended = true;
        }

        public void Resume()
        {
            Engine.Resume();
            suspended = false;
        }

        public void Update(UpdateType updateType)
        {
            Engine.Update();
        }

        private DF_SoundEffect LoadAudio(string audioName, bool incrRefCount)
        {
            if (!loadedSounds.ContainsKey(audioName))
            {
                AudioCache newCache = new AudioCache();
                newCache.Audio = Engine.CreateSoundEffect(audioName);
                newCache.LastUsed = Context.Time.RealSecondsFromStart.FloatValue;
                loadedSounds[audioName] = newCache;
            }

            AudioCache cache = loadedSounds[audioName];
            if (incrRefCount) cache.RefCount++;
            return cache.Audio;
        }

        internal DF_SoundInstance CreateAudioInstance(string audioName)
        {
            return LoadAudio(audioName, true).CreateInstance();
                 
        }

        public void PlaySound(string audioName, float volume)
        {
            DF_SoundEffect sound = LoadAudio(audioName, false);
            sound.PlayAsync(volume, 0);
        }

        
    }


    

}
