using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.Items.Rat.SCP018
{
    internal class SCP018Manager : MonoBehaviour
    {
        //need to find out to play audio as the player

        //public static SCP018Manager Instance;
        public float bounceMultipler;
        //public float maxSpeed;
        public bool slStyle;
        public float audioVolume;
        public AudioClip jumpSound;
        private AudioSource ballSound;
        public Rigidbody ball;
        public float bounceMultiplier;
        public float maxBounceSpeed;
        Vector3 reflectedVelocity;
        public bool hasCollided;
        public int playerDamage = 0;
        public int enemyDamage = 0;

        private Vector3 velocity_;
        /*public static void Init()
        {
            if (Instance == null)
            {
                Instance = new GameObject("SCP018Manager").AddComponent<SCP018Manager>();
            }
        }
        */
        public void Start()
        {
            ball = GetComponent<Rigidbody>();
            ballSound = GetComponent<AudioSource>();
            maxBounceSpeed = ConfigManager.config018maxSpeed.Value;
            slStyle = ConfigManager.config018slStyle.Value;
            LayerMask mask = LayerMask.GetMask("Player");
            //Physics.IgnoreLayerCollision(0, mask, false);

        }

        private IEnumerator BlowUpxD()
        {

            yield return null;
            Landmine.SpawnExplosion(ball.transform.position + Vector3.up, true, 6f, 7f, 20, 5f);
            Destroy(gameObject);


        }
        public void throwDaBall(float x, float y, float z) 
        {
            ball.AddForce(x, y, z);
        }
        public void OnCollisionEnter(Collision collision)
        {
            /*
            logger.LogDebug("Wazzzzup, ball speed is:" + ball.velocity); //a negative value
            Vector3 normal = collision.contacts[0].normal;
            reflectedVelocity = Vector3.Reflect(ball.velocity, normal);
            reflectedVelocity *= bounceMultiplier;
            reflectedVelocity = Vector3.ClampMagnitude(reflectedVelocity, maxBounceSpeed);
            ball.velocity = reflectedVelocity;
            //ball.velocity = reflectedVelocity;
            audioVolume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 25);
            ballSound.PlayOneShot(jumpSound, audioVolume);
            logger.LogDebug("ball speed is at " + reflectedVelocity); //no value is inputted
            */
            logger.LogDebug("contact has been made");
            if (collision.collider != null)
            {
                audioVolume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 25);
                ballSound.PlayOneShot(jumpSound, audioVolume);
                ReflectProjectile(ball, collision.contacts[0].normal);
                //hasCollided = true;
            }
        }
        //testing out method one
        //Need to try potentially reflecting only on the axis that is contacted and everything gets multiplied except for the hit contact
        private void ReflectProjectile(Rigidbody rb, Vector3 reflectVector)
        {
            velocity_ = Vector3.Reflect(velocity_, reflectVector);
            ball.velocity = velocity_;
        }
        /*
        public  void OnHitGround()
        {
            //base.OnHitGround();
            if (!hasCollided)
            {
                hasCollided = true;
                IncreaseSpeed();

            }
        }
        
        private void IncreaseSpeed()
        {
            if (ball != null)
            {
                if (ball.velocity.magnitude < maxBounceSpeed)
                {
                    ball.velocity *= bounceMultiplier;

                }
                hasCollided = false;
            }
        }
        */
        private void OnTriggerEnter(Collider other)
        {
            if (ball.velocity.magnitude > 20f)
            {
                PlayerControllerB component1 = other.gameObject.GetComponent<PlayerControllerB>();
                if ((UnityEngine.Object)component1 != (UnityEngine.Object)null && (UnityEngine.Object)component1 == (UnityEngine.Object)GameNetworkManager.Instance.localPlayerController && !component1.isPlayerDead)
                {
                    playerDamage = (int)(ball.velocity.magnitude / 5f);
                    GameNetworkManager.Instance.localPlayerController.DamagePlayer(playerDamage);
                }
                else
                {
                    EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                    if (!((UnityEngine.Object)component3 != (UnityEngine.Object)null) || !((UnityEngine.Object)component3.mainScript != (UnityEngine.Object)null) || !component3.mainScript.IsOwner || !component3.mainScript.IsOwner || !component3.mainScript.enemyType.canDie || component3.mainScript.isEnemyDead)
                    {
                        enemyDamage = (int)(ball.velocity.magnitude / 20f);
                        //component3.mainScript.KillEnemyOnOwnerClient();
                        component3.mainScript.HitEnemy(enemyDamage);
                    }
                }
            }
        }
    }
}
