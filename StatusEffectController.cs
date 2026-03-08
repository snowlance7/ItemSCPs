using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public class StatusEffectController : NetworkBehaviour
    {
#pragma warning disable CS8618
        public VignetteOverlay vignetteOverlay;
        public NetworkAudioSource networkAudioSource;
        public AudioSource audioSource;
        public AudioClip[] audioClips;

        public static StatusEffectController Instance;
#pragma warning restore CS8618

        private readonly List<StatusEffect> effects = new();

        private void Update()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                effect.Tick(Time.deltaTime);

                if (effect.IsFinished)
                {
                    effect.OnRemove();
                    effects.RemoveAt(i);
                }
            }
        }

        public void ApplyEffect(StatusEffect effect)
        {
            var existing = effects.FirstOrDefault(e => e.GetType() == effect.GetType());

            if (existing != null)
            {
                existing.OnReapply(effect);
                return;
            }

            effect.Initialize();
            effects.Add(effect);
        }

        public void RemoveEffect<T>() where T : StatusEffect
        {
            effects.RemoveAll(e =>
            {
                if (e is T effect)
                {
                    effect.OnRemove();
                    return true;
                }
                return false;
            });
        }

        public bool HasEffect<T>() where T : StatusEffect
        {
            return effects.Any(e => e is T);
        }

        public void ClearAll()
        {
            foreach (var effect in effects)
                effect.OnRemove();

            effects.Clear();
        }

        public void PlayOneShot(AudioClip audioClip, float volume = 1f, float pitch = 1f) // TODO: Figure out why this isnt working
        {
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioClip);
            networkAudioSource.PlayOneShot(audioClip);
        }

        public void TestAudio()
        {
            int index = Utils.randomLocal.Next(0, audioClips.Length);
            logger.LogDebug("Playing audio at index: " + index);
            PlayOneShot(audioClips[index]);
        }
    }

    public abstract class StatusEffect(float duration = 0f)
    {
        protected StatusEffectController controller => StatusEffectController.Instance;

        public float duration = duration;
        protected float timeRemaining;

        public bool IsFinished => duration > 0 && timeRemaining <= 0;

        public void Initialize()
        {
            timeRemaining = duration;
            OnApply();
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);

            if (duration > 0)
                timeRemaining -= deltaTime;
        }

        public virtual void OnApply() { }
        public virtual void OnTick(float deltaTime) { }
        public virtual void OnRemove() { }

        public virtual void OnReapply(StatusEffect newEffect)
        {
            timeRemaining = newEffect.duration;
        }
    }

    public class VignetteOverlay : MonoBehaviour
    {
#pragma warning disable CS8618
        public Image visual;
        Material material;
#pragma warning restore CS8618

        static readonly int InsetId = Shader.PropertyToID("_Inset");

        public float intensityDecreasePerSecond = 0.05f;

        float currentIntensity;

        void Awake()
        {
            material = visual.material;
        }

        void Update()
        {
            if (currentIntensity <= 0f) return;

            currentIntensity = Mathf.Max(0f,
                currentIntensity - intensityDecreasePerSecond * Time.deltaTime);

            material.SetFloat(InsetId, currentIntensity);
        }

        public void SetIntensity(float intensity)
        {
            currentIntensity = Mathf.Clamp01(intensity);
            material.SetFloat(InsetId, currentIntensity);
        }
    }

    [HarmonyPatch]
    public class StatusEffectControllerPatches
    {

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        public static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
        {
            try
            {
                StatusEffectController _instance = GameObject.Instantiate(ItemSCPsContentHandler.Instance.StatusEffectController!.StatusEffectControllerPrefab, __instance.transform).GetComponent<StatusEffectController>();

                if (IsServerOrHost)
                    _instance.NetworkObject.Spawn();

                if (__instance == localPlayer)
                    StatusEffectController.Instance = _instance;
            }
            catch
            {
                return;
            }
        }
    }
}
