using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP.RatUnfinished
{
    internal class SCP018Behavior : PhysicsProp
    {
        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public float speed;
        public float bounceMultiplier = 1.1f;
        public float maxBounceSpeed = 100f;
        //public Rigidbody ball;
        public float audioVolume;
        public AudioClip jumpSound;
        private AudioSource ballSound;
        public int playerDamage = 0;
        public int enemyDamage = 0;
        public BoxCollider throwPos;

       // private bool hasCollided;
        //public bool DontRequirePulling = false;
        //public Ray ballThrowRay;



        //public Ray grenadeThrowRay;
        //public RaycastHit grenadeHit;
        //private int stunGrenadeMask = 268437761;

        public GameObject daBall;
        //public bool activated = true;
        public bool slStyle;
        //public Transform startPosition;
        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 018 behavior");
            speed = 1f;
            //ball = GetComponent<Rigidbody>();
            ballSound = GetComponent<AudioSource>();
            //if (ball == null)
            //{
                //ball = gameObject.AddComponent<Rigidbody>();
            //}

        }

        public override void Update()
        {
            base.Update();
            

        }
        public override void DiscardItem()
        {
            if (IsOwner)
            {
                if (playerHeldBy != null)
                {
                    summonThrowBall(true);
                }
            }
        }
        public override void DiscardItemFromEnemy()
        {
            summonThrowBall(false);
        }
        
        public void summonThrowBall(bool player)
        {
            if (player == true)
            {
                Vector3 positionOfBall = transform.position;
                Quaternion rotationOfBall = transform.rotation;
                GameObject balll = Instantiate(daBall, positionOfBall, rotationOfBall);
                Rigidbody rbb = balll.GetComponent<Rigidbody>();
                rbb.AddForce(20, -2, 20, ForceMode.Impulse);
                DestroyObjectInHand(playerHeldBy);
            }
            else
            {
                Vector3 positionOfBall = transform.position;
                Quaternion rotationOfBall = transform.rotation;
                GameObject balll = Instantiate(daBall, positionOfBall, rotationOfBall);
                Rigidbody rbb = balll.GetComponent<Rigidbody>();
                rbb.AddForce(20, -2, 20, ForceMode.Impulse);
                
            }
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (IsOwner)
            {
                Vector3 positionOfBall = transform.position;
                Quaternion rotationOfBall = transform.rotation;
                logger.LogDebug(rotationOfBall + "thats rotation and also" + positionOfBall);
                //method 2

                Vector3 playerLook = throwPos.center;


                float checkx = throwPos.transform.position.x;
                float checky = throwPos.transform.position.y;
                float checkz = throwPos.transform.position.z;
                GameObject balll = Instantiate(daBall, positionOfBall, rotationOfBall);

                float resultx = checkx - balll.transform.position.x;
                float resulty = checky - balll.transform.position.y;
                float resultz = checkz - balll.transform.position.z;
                float xPush;
                float yPush;
                float zPush;
                if (resultx > 1)
                {
                    xPush = 40f;
                }
                else if (-1 < resultx && resultx < 1)
                {
                    xPush = 0;
                }
                else
                {
                    xPush = -40f;
                }
                if (resulty > 1)
                {
                    yPush = 10f;
                }
                else if (-1 < resulty && resulty < 1)
                {
                    yPush = 0;
                }
                else
                {
                    yPush = -10f;
                }
                if (resultz > 1)
                {
                    zPush = 40f;
                }
                else if (-1 < resultz && resultz < 1)
                {
                    zPush = 0;
                }
                else
                {
                    zPush = -40f;
                }

                //GameObject balll = Instantiate(daBall, positionOfBall, rotationOfBall);
                //Debug.DrawRay(playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
                Rigidbody rbb = balll.GetComponent<Rigidbody>();
                //balll.throwDaBall(xPush, yPush, zPush);

                rbb.AddForce(xPush, yPush, zPush, ForceMode.Impulse);
                //rbb.AddForce(playerLook * 2f, ForceMode.Impulse); //either disappears or spawns in front of ppl

                //method 2
                //balll.transform.position = Vector3.MoveTowards(balll.transform.position, playerLook, 3f);
                DestroyObjectInHand(playerHeldBy);
                //grab item name then, call a vector 3 of where ever the player is looking then destroy
                //UnityEngine.Object.Destroy(gameObject);
            }
        }
        
        /*
        public override void OnHitGround()
        {
            base.OnHitGround();
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
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider != null)
            {
                audioVolume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 25);
                ballSound.PlayOneShot(jumpSound, audioVolume);
                hasCollided = true;
            }
        }
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
        */








        /*
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            // Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            //ball.AddForce(screenPoint * speed, ForceMode.Impulse);
            //StunGrenadeItem scp018 = GetComponent<StunGrenadeItem>();
            if (IsOwner)
            {
                Vector3 positionOfBall = transform.position;
                Quaternion rotationOfBall = transform.rotation;
                logger.LogDebug(rotationOfBall + "thats rotation and also" + positionOfBall);
                Instantiate(daBall, positionOfBall, rotationOfBall);
                Rigidbody ballRigid = daBall.GetComponent<Rigidbody>();
                //ballRigid.AddForce(playerHeldBy.gameplayCamera.WorldToScreenPoint(transform.position) * 1);
                if (ballRigid != null)
                {
                    // Calculate the throw direction (where the player is looking)
                    Vector3 throwDirection = playerHeldBy.gameplayCamera.transform.forward;

                    // Apply force to the ball in the direction the camera is facing
                    ballRigid.AddForce(throwDirection * 5);
                }
                UnityEngine.Object.Destroy(gameObject);
                    //playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination());
                    //what if I have it so it just discards the object/delete and spawn 018 Manager
                    activated = true;

            }
           
        }
        public Vector3 GetGrenadeThrowDestination()
        {
            Vector3 position = transform.position;
            
            Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
            grenadeThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            position = !Physics.Raycast(grenadeThrowRay, out grenadeHit, 12f, stunGrenadeMask, QueryTriggerInteraction.Ignore) ? grenadeThrowRay.GetPoint(10f) : grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f);
            Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
            grenadeThrowRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 30f, stunGrenadeMask, QueryTriggerInteraction.Ignore))
            {
                return grenadeHit.point + Vector3.up * 0.05f;
            }
            return grenadeThrowRay.GetPoint(30f);
        }


        public void OnCollisionEnter(Collision collision)
        {
            //Vector3 positionOfBall = transform.position;
            //transform.position = positionOfBall;
            
            if (activated == true)
            {
                Instantiate(daBall);
            }
            /*
            speed *= 1.5f;
            Vector3 ballPosition = transform.position;
            ball.AddForce(ballPosition * speed, ForceMode.Acceleration); //should bounce in opposite direction, might change
            //it to be a random offset
            //Also scp-018 wont maintain material
            logger.LogDebug("YO"); //wont even do this, so weird,
            */
        /*if (safetyHold == false)
        {
            Vector3 normal = collision.contacts[0].normal;
            Vector3 reflectedVelocity = Vector3.Reflect(ball.velocity, normal);
            reflectedVelocity *= bounceMultiplier;
            reflectedVelocity = Vector3.ClampMagnitude(reflectedVelocity, maxBounceSpeed);
            //ball.velocity = reflectedVelocity;
            audioVolume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 25);
            ballSound.PlayOneShot(jumpSound, audioVolume);
            logger.LogDebug("ball speed is at " + reflectedVelocity);
        }
        */

        //} 


    }//Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);

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
                if (component1 != null && component1 == GameNetworkManager.Instance.localPlayerController && !component1.isPlayerDead)
                {
                    playerDamage = (int)(ball.velocity.magnitude / 5f);
                    GameNetworkManager.Instance.localPlayerController.DamagePlayer(playerDamage);
                }
                else
                {
                    EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                    if (!(component3 != null) || !(component3.mainScript != null) || !component3.mainScript.IsOwner || !component3.mainScript.IsOwner || !component3.mainScript.enemyType.canDie || component3.mainScript.isEnemyDead)
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
