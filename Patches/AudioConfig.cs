using GameNetcodeStuff;
using UnityEngine;

namespace DeadAndBored.Patches
{
    internal class AudioConfig
    {
        private PlayerControllerB playerControllerB;

        private Transform enemyTransform;

        private bool lowPassFilter = false;
        private bool highPassFilter = false;

        private float panStereo = 0f;

        private float playerVoicePitchTargets = 0f;
        private float playerPitch = 0f;

        private float spatialBlend = 0f;

        private bool set2D = false;

        private float volume = 0f;

        public AudioConfig(PlayerControllerB playerControllerB, bool lowPassFilter, bool highPassFilter, float panStereo, float playerVoicePitchTargets, float playerPitch, float spatialBlend, bool set2D, float volume)
        {
            this.playerControllerB = playerControllerB;
            this.enemyTransform = null;
            this.lowPassFilter = lowPassFilter;
            this.highPassFilter = highPassFilter;
            this.panStereo = panStereo;
            this.playerVoicePitchTargets = playerVoicePitchTargets;
            this.playerPitch = playerPitch;
            this.spatialBlend = spatialBlend;
            this.set2D = set2D;
            this.volume = volume;
        }

        public bool LowPassFilter { get => lowPassFilter; }
        public bool HighPassFilter { get => highPassFilter; }
        public float PanStereo { get => panStereo; }
        public float PlayerVoicePitchTargets { get => playerVoicePitchTargets; }
        public float PlayerPitch { get => playerPitch; }
        public float SpatialBlend { get => spatialBlend; }
        public bool Set2D { get => set2D; }
        public float Volume { get => volume; }
        public Transform EnemyT { get => enemyTransform == null ? null : enemyTransform; set => enemyTransform = value; }
        public Transform AudioSourceT { get => playerControllerB.currentVoiceChatAudioSource.transform; }
    }
}
