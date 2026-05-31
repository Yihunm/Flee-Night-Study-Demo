using UnityEngine;

namespace FleeNightStudy
{
    public class GameplayAudioManager : MonoBehaviour
    {
        public static GameplayAudioManager Instance { get; private set; }

        AudioSource _source;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
        }

        public void PlayPickupTextbook() => PlayTone(880f, 0.08f, 0.35f);
        public void PlayDoorOpen() => PlayTone(220f, 0.12f, 0.4f);
        public void PlayFootstep() => PlayTone(120f, 0.04f, 0.15f);
        public void PlayTeacherCough() => PlayTone(180f, 0.25f, 0.45f);
        public void PlayClassStartBell() => PlayTone(660f, 0.35f, 0.5f);
        public void PlayClassDismissalBell() => PlayTone(523f, 0.4f, 0.55f);

        void PlayTone(float freq, float duration, float volume)
        {
            if (_source == null) return;
            int rate = 44100;
            int samples = Mathf.RoundToInt(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float env = 1f - i / (float)samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * env * volume;
            }
            var clip = AudioClip.Create("tone", samples, 1, rate, false);
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }
    }
}
