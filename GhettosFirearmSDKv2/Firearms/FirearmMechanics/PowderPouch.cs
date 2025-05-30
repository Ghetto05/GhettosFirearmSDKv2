using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class PowderPouch : MonoBehaviour
{
    private readonly float _delay = 0.15f;

    public Item item;
    public bool opened;
    public Transform source;
    public GameObject grain;
    public float maxAngle;

    public Transform lid;
    public Transform lidClosedPosition;
    public Transform lidOpenedPosition;

    public AudioSource[] tapSounds;
    public AudioSource[] grainSpawnSounds;
    public AudioSource grainFlowSound;

    private float _lastEject;

    private void Start()
    {
        item.OnHeldActionEvent += ItemOnOnHeldActionEvent;
    }

    private void ItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
    {
        if (action == Interactable.Action.UseStart)
        {
            Tap();
        }

        if (action == Interactable.Action.AlternateUseStart)
        {
            if (opened)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
    }

    public void Open()
    {
        if (opened)
        {
            return;
        }
        opened = true;
        if (lid)
        {
            lid.SetPositionAndRotation(lidOpenedPosition.position, lidOpenedPosition.rotation);
        }
    }

    public void Close()
    {
        if (!opened)
        {
            return;
        }
        opened = false;
        if (lid)
        {
            lid.SetPositionAndRotation(lidClosedPosition.position, lidClosedPosition.rotation);
        }
    }

    public void Tap()
    {
        if (opened)
        {
            Spawn();
            Util.PlayRandomAudioSource(tapSounds);
        }
    }

    private void FixedUpdate()
    {
        if (opened && Vector3.Angle(source.forward, Vector3.down) <= maxAngle)
        {
            Util.PlayRandomAudioSource(grainSpawnSounds);
            Spawn();
            if (grainFlowSound?.isPlaying != true)
            {
                grainFlowSound?.Play();
            }
        }
        else if (grainFlowSound?.isPlaying == true)
        {
            grainFlowSound.Stop();
        }
    }

    private void Spawn()
    {
        if (Time.time - _lastEject > _delay)
        {
            var grainIn = Instantiate(grain, source.position, Quaternion.Euler(Util.RandomRotation()));
            _lastEject = Time.time;
            foreach (var ic in item.colliderGroups.SelectMany(cg => cg.colliders))
            {
                var cs = grainIn.GetComponentsInChildren<Collider>(true);
                foreach (var cpg in cs)
                {
                    Physics.IgnoreCollision(ic, cpg, true);
                }
            }
            grainIn.SetActive(true);
            Destroy(grainIn, 5f);
        }
    }
}