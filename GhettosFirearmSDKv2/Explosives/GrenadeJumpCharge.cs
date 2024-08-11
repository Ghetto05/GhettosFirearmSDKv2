using GhettosFirearmSDKv2.Explosives;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class GrenadeJumpCharge : Explosive
    {
        public float jumpForce;

        public override void ActualDetonate()
        {
            item.physicBody.velocity = Vector3.zero;
            item.physicBody.velocity = Vector3.up * jumpForce;
            //item.rb.AddForce(impactNormal * jumpForce);
            //item.rb.AddForce(Vector3.up * jumpForce);
            base.ActualDetonate();
        }
    }
}
