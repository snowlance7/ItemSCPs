using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public class StatusEffectController : MonoBehaviour
    {
        public static StatusEffectController? Instance { get; private set; }

        private Dictionary<Type, StatusEffect> singletonEffects = new();
        private List<StatusEffect> multiInstanceEffects = new();

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = Instantiate(new GameObject("StatusEffectController"), localPlayer.transform).AddComponent<StatusEffectController>();
            }
        }
        public void Update()
        {
            foreach (var effect in singletonEffects.Values)
                effect.Tick();
            foreach (var effect in multiInstanceEffects.ToList())
                effect.Tick();
        }

        public void ApplyEffect<T>(T effect) where T : StatusEffect
        {
            var type = typeof(T);

            if (effect.AllowMultipleInstances)
            {
                multiInstanceEffects.Add(effect);
                effect.Initialize(this);
                return;
            }

            if (singletonEffects.TryGetValue(type, out var existing))
            {
                if (existing.CanStack)
                {
                    existing.OnStackAdded(effect);
                    return;
                }

                if (!existing.Overridable) { return; }

                existing.OnRemove();
                singletonEffects.Remove(type);
            }

            singletonEffects[type] = effect;
            effect.Initialize(this);
        }

        public void RemoveEffect(StatusEffect effect)
        {
            if (effect.AllowMultipleInstances)
            {
                if (!multiInstanceEffects.Remove(effect)) return;
                effect.OnRemove();
                return;
            }

            var type = effect.GetType();

            if (!singletonEffects.TryGetValue(type, out var existing)) return;
            if (existing != effect) return;

            existing.OnRemove();
            singletonEffects.Remove(type);
        }

        public void RemoveEffect<T>() where T : StatusEffect
        {
            multiInstanceEffects.RemoveAll(e =>
            {
                if (e is T typed)
                {
                    typed.OnRemove();
                    return true;
                }
                return false;
            });

            if (singletonEffects.TryGetValue(typeof(T), out var singleton))
            {
                singleton.OnRemove();
                singletonEffects.Remove(typeof(T));
            }
        }

        public void RemoveEffects(params Type[] effectTypes)
        {
            foreach (var type in effectTypes)
            {
                multiInstanceEffects.RemoveAll(e =>
                {
                    if (type.IsAssignableFrom(e.GetType()))
                    {
                        e.OnRemove();
                        return true;
                    }
                    return false;
                });

                if (singletonEffects.TryGetValue(type, out var singleton))
                {
                    singleton.OnRemove();
                    singletonEffects.Remove(type);
                }
            }
        }

        public bool HasEffect<T>() where T : StatusEffect
        {
            return singletonEffects.ContainsKey(typeof(T)) ||
                   multiInstanceEffects.Any(e => e is T);
        }

        public void Reset(bool overridePermanent = false)
        {
            foreach (var effect in singletonEffects.ToList())
            {
                if (effect.Value.IsPermanent && !overridePermanent) { continue; }
                effect.Value.OnRemove();
                singletonEffects.Remove(effect.Key);
            }
            foreach (var effect in multiInstanceEffects.ToList())
            {
                if (effect.IsPermanent && !overridePermanent) continue;
                effect.OnRemove();
                multiInstanceEffects.Remove(effect);
            }
        }
    }

    public abstract class StatusEffect
    {
#pragma warning disable CS8618
        protected StatusEffectController controller;
#pragma warning restore CS8618

        public virtual bool AllowMultipleInstances => false;
        public virtual bool Overridable => false;
        public virtual bool IsPermanent => false;
        public virtual bool CanStack => false;

        public Coroutine? effectRoutine;
        public float effectTime;

        protected int stacks = 1;

        public void Initialize(StatusEffectController controller)
        {
            this.controller = controller;
            OnApply();
        }

        public virtual void Update()
        {
            if (effectTime > 0)
            {
                effectTime -= Time.deltaTime;
                if (effectTime <= 0)
                {
                    OnRemove();
                }
            }
        }

        public virtual void OnApply() { }
        public virtual void OnRemove()
        {
            if (effectRoutine != null)
                controller.StopCoroutine(effectRoutine);
            controller.RemoveEffect(this);
        }
        public virtual void OnStackAdded(StatusEffect effect) { }
        public virtual void Tick() { }
    }
}
