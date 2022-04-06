using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Vasi;

namespace GatlingAspid
{
    internal class Jelly : MonoBehaviour
    {
        private PlayMakerFSM _lilJelly;

        private void Awake()
        {
            _lilJelly = gameObject.LocateMyFSM("Lil Jelly");
        }

        private void Start()
        {
            _lilJelly.SetState("Chase");

            _lilJelly.GetState("Die").InsertMethod(0, Explode);
            //_lilJelly.GetAction<ChaseObjectV2>("Chase").accelerationForce.Value *= 2;
        }

        private void Explode()
        {
            GameObject explosionObj = _lilJelly.GetAction<SpawnObjectFromGlobalPool>("Uumuu Explosion").gameObject.Value;
            GameObject explosion = Instantiate(explosionObj, transform.position, Quaternion.identity);
            explosion.SetActive(true);
            Destroy(explosion.LocateMyFSM("damages_enemy"));
            Destroy(gameObject);
        }
    }
}
