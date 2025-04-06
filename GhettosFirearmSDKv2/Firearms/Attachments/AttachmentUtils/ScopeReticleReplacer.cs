using UnityEngine;

namespace GhettosFirearmSDKv2;

public class ScopeReticleReplacer : MonoBehaviour
{
    public Attachment attachment;
    public MeshRenderer newReticle;

    private Scope _scope;
    private MeshRenderer _oldReticle;

    public void Start()
    {
        Invoke(nameof(DelayedStart), Settings.invokeTime);
    }

    public void DelayedStart()
    {
        _scope = GetComponentInParent<Scope>();

        if (!_scope)
        {
            return;
        }

        attachment.OnDetachEvent += AttachmentOnOnDetachEvent;

        _oldReticle = _scope.lens;
        _oldReticle.gameObject.SetActive(false);
        _scope.lens = newReticle;
        _scope.lenses.Remove(_oldReticle);
        _scope.lenses.Add(newReticle);
        _scope.UpdateRenderers();
    }

    private void AttachmentOnOnDetachEvent(bool despawnDetach)
    {
        if (despawnDetach)
        {
            return;
        }

        _oldReticle.gameObject.SetActive(true);
        _scope.lens = _oldReticle;
        _scope.lenses.Remove(newReticle);
        _scope.lenses.Add(_oldReticle);
        _scope.UpdateRenderers();
    }
}