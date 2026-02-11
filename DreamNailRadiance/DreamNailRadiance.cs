using System.Collections.Generic;
using System.Linq;
using DanielSteginkUtils.Utilities;
using GlobalEnums;
using Modding;
using Modding.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamNailRadiance
{
    public class DreamNailRadiance : Mod
    {
        internal static DreamNailRadiance Instance;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
            On.HutongGames.PlayMaker.FsmState.OnEnter += OnEnterState;

            Log("Initialized");
        }

        /// <summary>
        /// Stores the Radiance's legs for ease of reference
        /// </summary>
        private GameObject legsObject = null;

        /// <summary>
        /// When a new scene loads, check for the Radiance and add the components to her legs;
        /// this way they will show up in DebugMod
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            GameObject[] gameObjects = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject[] radianceObjects = gameObjects.Where(x => x.name.Contains("Radiance")).ToArray();
            foreach (GameObject radiance in radianceObjects)
            {
                EnemyDreamnailReaction reaction = radiance.GetComponent<EnemyDreamnailReaction>();
                if (reaction == default)
                {
                    continue;
                }
                //Log($"Radiance detected: {GetFullPath(radiance)}");

                GameObject[] legsObjects = gameObjects.Where(x => x.name.Equals("Legs")).ToArray();
                foreach (GameObject legs in legsObjects)
                {
                    if (legs.transform.parent != radiance.transform)
                    {
                        continue;
                    }
                    //Log($"Radiance Legs detected: {GetFullPath(legs)}");

                    legsObject = legs;
                    legs.layer = (int)PhysLayers.ENEMIES;

                    // Set up the Dream Nail component to match the Radiance's
                    EnemyDreamnailReaction legsReaction = legs.GetOrAddComponent<EnemyDreamnailReaction>();
                    legsReaction.SetConvoTitle("RADIANCE");
                    ClassIntegrations.SetField<EnemyDreamnailReaction>(legsReaction, "convoAmount", 6);
                    GameObject prefab = ClassIntegrations.GetField<EnemyDreamnailReaction, GameObject>(reaction, "dreamImpactPrefab");
                    ClassIntegrations.SetField<EnemyDreamnailReaction>(legsReaction, "dreamImpactPrefab", prefab);
                    //Log("EnemyDreamnailReaction added to Legs");

                    // Add a 2D Collider to trigger the Dream Nail
                    BoxCollider2D legsCollider = legs.GetOrAddComponent<BoxCollider2D>();
                    legsCollider.isTrigger = true;
                    legsCollider.size = new Vector2(1, 5);
                    legsCollider.offset = new Vector2(0, 0);
                    legsCollider.enabled = false; // We only want to enable the collider while dream-nailing
                                                  // Otherwise you can attack Radiance via her legs
                    //Log("BoxCollider2D added to Legs");
                }
            }
        }

        /// <summary>
        /// Dream Nail status is most easily monitored via the Dream Nail FSM
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void OnEnterState(On.HutongGames.PlayMaker.FsmState.orig_OnEnter orig, HutongGames.PlayMaker.FsmState self)
        {
            orig(self);

            // We want to toggle the Legs' collider at specific points in the Dream Nail FSM
            // We can also skip if the Legs don't exist yet
            if (!self.Fsm.Name.Equals("Dream Nail") ||
                legsObject == null)
            {
                return;
            }

            // We can also, of course, skip if the collider doesn't exist for some reason
            BoxCollider2D legsCollider = legsObject.GetComponent<BoxCollider2D>();
            if (legsCollider == null)
            {
                return;
            }

            // When we start the Dream Nail, enable the collider so we can dream nail the legs
            if (self.Name.Equals("Charge"))
            {
                legsCollider.enabled = true;
            }

            // When we stop, or forcibly cancel, disable the collider
            List<string> stopStateNames = new List<string>()
            {
                "Regain Control",
                "Cancel",
                "Take Control"
            };
            if (stopStateNames.Any(x => self.Name.Equals(x)))
            {
                legsCollider.enabled = false;
            }
        }
    }
}