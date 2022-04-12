using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vasi;

namespace GatlingAspid
{
    internal class Aspid : MonoBehaviour
    {
        private AudioSource _audio;
        private PlayMakerFSM _spitter;
        private tk2dSpriteAnimator _anim;
        private tk2dSprite _sprite;

        private void Awake()
        {
            _spitter = gameObject.LocateMyFSM("spitter");
            _anim = GetComponent<tk2dSpriteAnimator>();
            _audio = GetComponent<AudioSource>();
            _sprite = GetComponent<tk2dSprite>();
        }

        private void Start()
        {
            if (GatlingAspid.Instance.GlobalSettings.AspidHP > 0)
            {
                GetComponent<HealthManager>().hp = GatlingAspid.Instance.GlobalSettings.AspidHP;
            }

            tk2dSpriteCollectionData aspidCollectionData = _sprite.Collection;
            List<tk2dSpriteDefinition> aspidSpriteDefs = aspidCollectionData.spriteDefinitions.ToList();

            GameObject collectionPrefab = GatlingAspid.GameObjects["Cln"];
            var collection = collectionPrefab.GetComponent<tk2dSpriteCollection>();

            foreach (tk2dSpriteDefinition def in collection.spriteCollection.spriteDefinitions)
            {
                def.material.shader = aspidSpriteDefs[0].material.shader;
                aspidSpriteDefs.Add(def);
            }

            aspidCollectionData.spriteDefinitions = aspidSpriteDefs.ToArray();

            List<tk2dSpriteAnimationClip> clips = _anim.Library.clips.ToList();

            var attackAnticClip = _anim.Library.GetClipByName("Attack Antic");
            var attackRecoverClip = new tk2dSpriteAnimationClip
            {
                frames = attackAnticClip.frames.Reverse().ToArray(),
                fps = attackAnticClip.fps,
                loopStart = 0,
                name = "Recover",
                wrapMode = tk2dSpriteAnimationClip.WrapMode.Once,
            };

            clips.Add(attackRecoverClip);

            GameObject animationPrefab = GatlingAspid.GameObjects["Anim"];
            var animation = animationPrefab.GetComponent<tk2dSpriteAnimation>();

            foreach (tk2dSpriteAnimationClip clip in animation.clips)
            {
                clips.Add(clip);
            }

            _anim.Library.clips = clips.ToArray();

            var shootPoint = new GameObject("Shoot Point");
            shootPoint.transform.SetParent(transform);
            shootPoint.transform.localPosition = new Vector2(1, -0.25f);

            var fireSpawn = _spitter.GetAction<SpawnObjectFromGlobalPool>("Fire", 1);
            fireSpawn.position = shootPoint.transform.localPosition;
            if (GatlingAspid.Instance.GlobalSettings.Crystals)
            {
                fireSpawn.gameObject = GatlingAspid.GameObjects["Crystal"];
                shootPoint.transform.localPosition = new Vector2(0, -0.25f);
            }
            GameObject venom = fireSpawn.gameObject.Value;
            venom.GetComponent<Renderer>().sortingOrder++;
            // Only 24 prefabs in the object pool, so increase it
            venom.CreatePool(128);

            var states = _spitter.FsmStates.ToList();

            var continueFiringState = new FsmState(_spitter.Fsm);
            continueFiringState.Name = "Continue Firing?";
            states.Add(continueFiringState);

            var fireRecoverState = new FsmState(_spitter.Fsm);
            fireRecoverState.Name = "Fire Recover";
            states.Add(fireRecoverState);

            var firePauseState = new FsmState(_spitter.Fsm);
            firePauseState.Name = "Fire Pause";
            states.Add(firePauseState);

            _spitter.Fsm.States = states.ToArray();

            List<FsmInt> intVariables = _spitter.FsmVariables.IntVariables.ToList();

            var shots = new FsmInt("Shots");
            shots.Value = 0;
            intVariables.Add(shots);

            var shotsMax = new FsmInt("Shots Max");
            shotsMax.Value = GatlingAspid.Instance.GlobalSettings.ShotsPerBarrage;
            intVariables.Add(shotsMax);

            _spitter.FsmVariables.IntVariables = intVariables.ToArray();

            List<FsmGameObject> gameObjectVariables = _spitter.FsmVariables.GameObjectVariables.ToList();
            var grenade = new FsmGameObject("Grenade");
            gameObjectVariables.Add(grenade);

            FsmBool startedFiringBool = _spitter.CreateBool("Started Firing");
            startedFiringBool.Value = false;

            fireRecoverState.InsertAction(0, new SetBoolValue
            {
                boolVariable = startedFiringBool,
                boolValue = false,
            });

            _spitter.GetState("Distance Fly").InsertAction(0, new SetIntValue
            {
                intVariable = shots,
                intValue = 0,
            });

            var fireState = _spitter.GetState("Fire");

            firePauseState.AddTransition(FsmEvent.Finished, "Fire");
            fireRecoverState.AddTransition(FsmEvent.Finished, "Fire Dribble");
            _spitter.ChangeTransition("Fire", "WAIT", "Continue Firing?");

            _spitter.GetAction<FireAtTarget>("Fire", 2).speed.Value = 60;
            _spitter.GetAction<FireAtTarget>("Fire", 2).spread.Value = 15;

            _spitter.GetAction<Tk2dPlayAnimationWithEvents>("Fire Anticipate").clipName.Value = "Wield";
            _spitter.GetAction<Tk2dPlayAnimation>("Fire").clipName.Value = "Shoot";
            
            fireState.RemoveAction<AudioPlay>();
            for (int i = 0; i <= 6; i++)
            {
                fireState.RemoveAction(fireState.Actions.Length - 1);
            }
            
            fireState.AddAction(new IntAdd
            {
                intVariable = shots,
                add = 1,
                everyFrame = false,
            });

            fireState.AddAction(new BoolTest
            {
                boolVariable = startedFiringBool,
                isTrue = FsmEvent.FindEvent("WAIT"),
            });

            fireState.AddAction(new SetAudioLoop
            {
                gameObject = new FsmOwnerDefault
                {
                    GameObject = null,
                    OwnerOption = OwnerDefaultOption.UseOwner,
                },
                loop = true,
            });

            fireState.AddAction(new AudioPlay
            {
                gameObject = new FsmOwnerDefault
                {
                    GameObject = null,
                    OwnerOption = OwnerDefaultOption.UseOwner,
                },
                oneShotClip = GatlingAspid.AudioClips["Firing"],
            });

            fireState.AddAction(new SetBoolValue
            {
                boolVariable = startedFiringBool,
                boolValue = true,
            });

            firePauseState.AddAction(new Wait
            {
                time = GatlingAspid.Instance.GlobalSettings.FireRate > 0 ? (1.0f / GatlingAspid.Instance.GlobalSettings.FireRate) : 0.025f,
                finishEvent = FsmEvent.Finished,
                realTime = false,
            });

            fireRecoverState.AddAction(new AudioStop
            {
                gameObject = new FsmOwnerDefault
                {
                    GameObject = null,
                    OwnerOption = OwnerDefaultOption.UseOwner,
                },
            });

            fireRecoverState.AddAction(new Tk2dPlayAnimationWithEvents
            {
                animationCompleteEvent = FsmEvent.Finished,
                clipName = "Recover",
                gameObject = new FsmOwnerDefault
                {
                    GameObject = null,
                    OwnerOption = OwnerDefaultOption.UseOwner,
                },
            });

            if (GatlingAspid.Instance.GlobalSettings.Grenades)
            {
                fireRecoverState.AddAction(new CreateObject
                {
                    gameObject = GatlingAspid.GameObjects["Jelly"],
                    position = new FsmVector3
                    {
                        Name = "",
                        RawValue = Vector3.zero,
                        Value = Vector3.zero,
                    },
                    rotation = new FsmVector3
                    {
                        Name = "",
                        RawValue = Vector3.zero,
                        Value = Vector3.zero,
                    },
                    spawnPoint = shootPoint,
                    storeObject = grenade,
                });

                fireRecoverState.AddAction(new IgnoreCollision
                {
                    gameObject1 = gameObject,
                    gameObject2 = grenade,
                });

                fireRecoverState.AddAction(new SetFsmState
                {
                    gameObject = grenade,
                    fsmName = "Lil Jelly",
                    stateName = "Startle",
                });

                fireRecoverState.AddAction(new SetFsmState
                {
                    gameObject = grenade,
                    fsmName = "Lil Jelly",
                    stateName = "Chase",
                });

                fireRecoverState.AddAction(new AudioPlayerOneShotSingle
                {
                    audioClip = GatlingAspid.AudioClips["Grenade"],
                    audioPlayer = FindObjectsOfType<GameObject>(true).First(go => go.name.Contains("Audio Player Actor")),
                    delay = 0,
                    pitchMax = 1.15f,
                    pitchMin = 0.85f,
                    spawnPoint = shootPoint,
                    storePlayer = new FsmGameObject
                    {
                        Name = "",
                        RawValue = null,
                        Value = null,
                    },
                });
            }

            continueFiringState.InsertAction(0, new IntCompare
            {
                equal = FsmEvent.FindEvent("TRUE"),
                greaterThan = FsmEvent.FindEvent("TRUE"),
                integer1 = shots,
                integer2 = shotsMax,
                lessThan = FsmEvent.FindEvent("FALSE"),
            });

            continueFiringState.AddTransition("TRUE", "Fire Recover");
            continueFiringState.AddTransition("FALSE", "Fire Pause");
        }
    }
}
