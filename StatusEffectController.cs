using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static ItemSCPs.Plugin;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace ItemSCPs
{
    public class StatusEffectController : NetworkBehaviour
    {
#pragma warning disable CS8618
        public VignetteOverlay vignetteOverlay;
        public StatusEffectAudioLibrary audioLibrary;

        public static StatusEffectController Instance;

        public PlayerControllerB playerAttachedTo;
#pragma warning restore CS8618

        private readonly List<StatusEffect> effects = new();

        public static void Init(PlayerControllerB player)
        {
            StatusEffectController _instance = GameObject.Instantiate(ItemSCPsContentHandler.Instance.StatusEffectController!.StatusEffectControllerPrefab, player.transform).GetComponent<StatusEffectController>();

            if (IsServerOrHost)
                _instance.NetworkObject.Spawn();

            _instance.playerAttachedTo = player;

            if (player == localPlayer)
                StatusEffectController.Instance = _instance;
        }

        private void Update()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                effect.Tick(Time.deltaTime);

                if (effect.IsFinished || (playerAttachedTo.isPlayerDead && effect.removeOnDeath))
                {
                    effect.OnRemove();
                    effects.RemoveAt(i);
                }
            }
        }

        public void ApplyEffect(StatusEffect effect)
        {
            if (effect.id != "")
            {
                var existing = effects.FirstOrDefault(e => e.id == effect.id);

                if (existing != null)
                {
                    existing.OnReapply(effect);
                    return;
                }
            }

            effect.OnApply();
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

        public void RemoveEffect(StatusEffect effect)
        {
            effect.OnRemove();
            effects.Remove(effect);
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

        [ServerRpc(RequireOwnership = false)]
        public void PlayRandomClipServerRpc(string id, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            if (!IsServer) { return; }
            PlayRandomClipClientRpc(id, bodyPartIndex, volume, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [ClientRpc]
        public void PlayRandomClipClientRpc(string id, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            AudioGroup group = audioLibrary.groups.Where(x => x.id == id).FirstOrDefault();
            if (group == null) return;
            int index = Utils.randomGlobal.Next(0, group.clips.Length);
            AudioClip clip = group.clips[index];
            logger.LogDebug($"Playing sound effect {id}, index {index}, volume {volume}, minMaxDistance {min3DDistance}-{max3DDistance}, cutoffFrequency {cutoffFrequency}");
            Utils.PlaySoundAtPosition(playerAttachedTo.bodyParts[bodyPartIndex], clip, volume, min3DDistance: min3DDistance, max3DDistance: max3DDistance, cutoffFrequency: cutoffFrequency, audibleNoiseID: audibleNoiseID);
        }

        public void TestAudio()
        {
            PlayRandomClipServerRpc("cough", 0, 0.6f, 2, 10, 1500);
        }
    }

    public abstract class StatusEffect(string id, bool removeOnDeath, float duration)
    {
        protected StatusEffectController controller => StatusEffectController.Instance;

        public string id = id;
        public bool removeOnDeath = removeOnDeath;
        public float duration = duration;

        protected float elapsedTime;

        public bool IsFinished => duration > 0 && elapsedTime >= duration;

        public enum ReapplyResult
        {
            Reapplied,
            Replace,
            Reject
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);

            if (duration > 0)
                elapsedTime += deltaTime;
        }

        public virtual void OnApply() { }
        public abstract ReapplyResult OnReapply(StatusEffect effect);
        public virtual void OnTick(float deltaTime) { }
        public void Remove()
        {
            controller?.RemoveEffect(this);
        }
        public virtual void OnRemove() { }
    }

    public class VignetteOverlay : MonoBehaviour // TODO: Test this
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

    [CreateAssetMenu]
    public class StatusEffectAudioLibrary : ScriptableObject
    {
#pragma warning disable CS8618
        public AudioGroup[] groups;
#pragma warning restore CS8618
    }

    [System.Serializable]
    public class AudioGroup
    {
#pragma warning disable CS8618
        public string id;
        public AudioClip[] clips;
#pragma warning restore CS8618
    }

    [HarmonyPatch]
    public class StatusEffectControllerPatches
    {

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        public static void ConnectClientToPlayerObjectPostfix(PlayerControllerB __instance)
        {
            try
            {
                StatusEffectController.Init(__instance);
            }
            catch
            {
                return;
            }
        }
    }
}
