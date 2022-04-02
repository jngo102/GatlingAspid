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
        private const int ShotsMax = 60;

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
            }
            GameObject venom = fireSpawn.gameObject.Value;
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

            var intVariables = _spitter.FsmVariables.IntVariables.ToList();

            var shots = new FsmInt("Shots");
            shots.Value = 0;
            intVariables.Add(shots);

            var shotsMax = new FsmInt("Shots Max");
            shotsMax.Value = ShotsMax;
            intVariables.Add(shotsMax);

            _spitter.FsmVariables.IntVariables = intVariables.ToArray();

            _spitter.CreateBool("Started Firing").Value = false;

            var setFiringBoolValueFalse = new SetBoolValue
            {
                boolVariable = _spitter.Fsm.GetFsmBool("Started Firing"),
                boolValue = false,
            };

            var setIntValue = new SetIntValue
            {
                intVariable = _spitter.Fsm.GetFsmInt("Shots"),
                intValue = 0,
            };
            _spitter.GetState("Fire Recover").InsertAction(0, setFiringBoolValueFalse);
            _spitter.GetState("Distance Fly").InsertAction(0, setIntValue);
            _spitter.GetState("Fire Recover").InsertMethod(1, () => _audio.Stop());

            var fireState = _spitter.GetState("Fire");

            _spitter.GetState("Fire Pause").AddTransition(FsmEvent.Finished, "Fire");
            _spitter.GetState("Fire Recover").AddTransition(FsmEvent.Finished, "Fire Dribble");
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

            fireState.InsertMethod(0, () =>
            {
                if (!_spitter.Fsm.GetFsmBool("Started Firing").Value)
                {
                    _audio.clip = GatlingAspid.AudioClips["Firing"];
                    _audio.volume = 0.5f;
                    _audio.loop = true;
                    _audio.Play();
                }
            });

            var setFiringBoolValueTrue = new SetBoolValue
            {
                boolVariable = _spitter.Fsm.GetFsmBool("Started Firing"),
                boolValue = true,
            };

            fireState.InsertAction(1, setFiringBoolValueTrue);

            var intAdd = new IntAdd
            {
                intVariable = _spitter.Fsm.GetFsmInt("Shots"),
                add = 1,
                everyFrame = false,
            };
            fireState.AddAction(intAdd);

            var firePauseWait = new Wait
            {
                time = 0.025f,
                finishEvent = FsmEvent.Finished,
                realTime = false,
            };
            _spitter.GetState("Fire Pause").InsertAction(0, firePauseWait);

            string attackRecoverAnim = "Recover";
            _spitter.GetState("Fire Recover").InsertMethod(0, () =>
            {
                _anim.Play(attackRecoverAnim);
                _audio.volume = 1.0f;
                GameObject jelly = Instantiate(GatlingAspid.GameObjects["Jelly"], transform.position, Quaternion.identity);
                Physics2D.IgnoreCollision(GetComponent<BoxCollider2D>(), jelly.GetComponent<CircleCollider2D>());
            });

            var fireRecoverWait = new Wait
            {
                time = 1.0f / _anim.GetClipByName(attackRecoverAnim).fps * _anim.GetClipByName(attackRecoverAnim).frames.Length,
                finishEvent = FsmEvent.Finished,
                realTime = false,
            };
            _spitter.GetState("Fire Recover").InsertAction(1, fireRecoverWait);

            _spitter.GetState("Continue Firing?").InsertMethod(0, () =>
            {
                if (_spitter.Fsm.GetFsmInt("Shots").Value >= _spitter.Fsm.GetFsmInt("Shots Max").Value)
                {
                    _spitter.SetState("Fire Recover");
                }
                else
                {
                    _spitter.SetState("Fire Pause");
                }
            });
        }
    }
}
