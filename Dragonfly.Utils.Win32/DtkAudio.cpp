#include <directxtk/Audio.h>
#include <msclr/marshal.h>


using namespace DirectX;
using namespace msclr::interop;

namespace DragonflyUtils {

	ref class DF_AudioEngine;
	ref class DF_SoundEffect;
	ref class DF_SoundInstance;

	public ref class DF_SoundInstance
	{
	private:
		SoundEffectInstance * audioInstance;
		WORD channelCount;
		float sourceSize;

	internal:
		DF_SoundInstance(SoundEffectInstance * soundInstance, SoundEffect * audioEffect)
		{
			audioInstance = soundInstance;
			channelCount = audioEffect->GetFormat()->nChannels;
			sourceSize = 0;
		}

	public:
		void SetVolume(float decibels)
		{
			float volume = XAudio2DecibelsToAmplitudeRatio(decibels);
			audioInstance->SetVolume(volume);
		}

		void SetPan(float pan)
		{
			audioInstance->SetPan(pan);
		}

		void SetPitch(float pitch)
		{
			audioInstance->SetPitch(pitch);
		}

		void SetSourceSize(float size)
		{
			this->sourceSize = size;
		}

		void SetPan3D(
			float listenerFacingDir_x, float listenerFacingDir_y, float listenerFacingDir_z,
			float listenerPos_x, float listenerPos_y, float listenerPos_z,
			float soundSourcePos_x, float soundSourcePos_y, float soundSourcePos_z)
		{
			AudioListener listener;
			XMFLOAT3 listenerDir(listenerFacingDir_x, listenerFacingDir_y, listenerFacingDir_z);
			XMFLOAT3 up(0, 1.f, 0);
			listener.SetOrientation(listenerDir, up);
			XMFLOAT3 listenerPos(listenerPos_x, listenerPos_y, listenerPos_z);
			listener.SetPosition(listenerPos);
			AudioEmitter emitter;
			emitter.ChannelCount = channelCount;
			emitter.InnerRadius = sourceSize * 0.5f;
			XMFLOAT3 soundPos(soundSourcePos_x, soundSourcePos_y, soundSourcePos_z);
			emitter.SetPosition(soundPos);
			audioInstance->Apply3D(listener, emitter, false);
		}

		void Play(bool loop)
		{
			audioInstance->Play(loop);
		}

		void Stop()
		{
			audioInstance->Stop();
		}

		~DF_SoundInstance()
		{
			delete audioInstance;
		}

	};


	public ref class DF_SoundEffect
	{
	private:
		SoundEffect * soundEffect;

	internal:
		DF_SoundEffect(SoundEffect * soundEffect)
		{
			this->soundEffect = soundEffect;
		}

	public:

		void PlayAsync(float decibels, float pan)
		{
			soundEffect->Play(XAudio2DecibelsToAmplitudeRatio(decibels), 0, pan);
		}

		DF_SoundInstance ^ CreateInstance()
		{
			return gcnew DF_SoundInstance(soundEffect->CreateInstance(SoundEffectInstance_Use3D).release(), soundEffect);
		}

		bool IsInUse()
		{
			return soundEffect->IsInUse();
		}

		~DF_SoundEffect()
		{
			delete soundEffect;
		}

	};

	public ref class DF_AudioEngine
	{
	private:
		AudioEngine * audioEngine;
		marshal_context marshalContext;

	public:
		DF_AudioEngine()
		{
			AUDIO_ENGINE_FLAGS eflags = AudioEngine_Default;
#ifdef _DEBUG
			eflags = eflags | AudioEngine_Debug;
#endif
			audioEngine = new AudioEngine(eflags);
		}

		bool Update()
		{
			return audioEngine->Update();
		}

		DF_SoundEffect ^ CreateSoundEffect(System::String ^ audioFilePath)
		{			
			SoundEffect * soundEffect = new SoundEffect(this->audioEngine, marshalContext.marshal_as<const WCHAR*>(audioFilePath));
			return gcnew DF_SoundEffect(soundEffect);
		}

		void Suspend()
		{
			audioEngine->Suspend();
		}

		void Resume()
		{
			audioEngine->Resume();
		}


		~DF_AudioEngine()
		{
			audioEngine->Suspend();
			delete audioEngine;
		}

	};


	

}