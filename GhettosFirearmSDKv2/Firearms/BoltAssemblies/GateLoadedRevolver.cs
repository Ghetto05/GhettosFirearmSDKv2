using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class GateLoadedRevolver : BoltBase
    {
        private bool loadMode = false;
        private bool cocked = false;
        private int currentChamber;

        [Header("Cylinder")]
        public Transform cylinderAxis;
        public Vector3[] cylinderRotations;
        public Vector3[] cylinderLoadRotations;
        public int loadChamberOffset;
        [Space]
        public string[] calibers;
        public Cartridge[] loadedCartridges;
        public Transform[] mountPoints;

        [Header("Load Gate")]
        public Transform loadGateAxis;
        public Transform loadGateClosedPosition;
        public Transform loadGateOpenedPosition;
        public Collider loadCollider;

        [Header("Hammer")]
        public Transform hammerAxis;
        public Transform hammerIdlePosition;
        public Transform hammerCockedPosition;
        public Collider cockCollider;

        [Header("Ejector Rod")]
        public Transform roundReparent;
        public Rigidbody ejectorRB;
        public Transform ejectorAxis;
        public Transform ejectorRoot;
        public Transform ejectorEjectPoint;
        private ConfigurableJoint ejectorJoint;
        public float ejectForce;
        public Transform ejectDir;
        public Transform springRoot;
        public Vector3 springTargetScale = Vector3.one;

        [Header("Audio")]
        public List<AudioSource> insertSounds;
        public List<AudioSource> ejectSounds;
        public List<AudioSource> openSounds;
        public List<AudioSource> closeSounds;
        public List<AudioSource> hammerCockSounds;
        public List<AudioSource> hammerHitSounds;

        private bool roundReparented = false;
        private bool allowLoad = false;
        MagazineSaveData data;

        private void Start()
        {
            Invoke(nameof(InvokedStart), FirearmsSettings.invokeTime);
        }

        public void InvokedStart()
        {
            loadedCartridges = new Cartridge[mountPoints.Length];
            UpdateEjector();
            firearm.OnCockActionEvent += Cock;
            firearm.OnCollisionEvent += Firearm_OnCollisionEvent;
            firearm.OnAltActionEvent += Firearm_OnAltActionEvent;
            firearm.OnTriggerChangeEvent += Firearm_OnTriggerChangeEvent;
            firearm.OnCollisionEventTR += Firearm_OnCollisionEventTR;

            if (firearm.item.TryGetCustomData(out data))
            {
                for (int i = 0; i < data.contents.Length; i++)
                {
                    if (data.contents[i] != null)
                    {
                        int index = i;
                        Util.SpawnItem(data.contents[index], "Bolt Chamber", ci => { Cartridge c = ci.GetComponent<Cartridge>(); LoadChamber(index, c, false); }, transform.position + Vector3.up * 3);
                    }
                }
                UpdateChamberedRounds();
            }
            else
            {
                firearm.item.AddCustomData(new MagazineSaveData());
                firearm.item.TryGetCustomData(out data);
                data.contents = new string[loadedCartridges.Length];
            }
            allowLoad = true;
            UpdateChamberedRounds();
        }

        private void Firearm_OnCollisionEventTR(CollisionInstance collisionInstance)
        {
            if (cockCollider != null && collisionInstance.sourceCollider == cockCollider && collisionInstance.targetCollider.GetComponentInParent<Player>() != null)
            {
                Cock();
            }
        }

        private void FixedUpdate()
        {
            if (loadMode)
            {
                if (roundReparented && Vector3.Distance(ejectorRB.transform.localPosition, ejectorRoot.localPosition) <= 0.004f)
                {
                    roundReparented = false;
                    Cartridge c = loadedCartridges[LoadModeChamber()];
                    c.transform.parent = mountPoints[LoadModeChamber()];
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }

                if (springRoot != null)
                {
                    float time = Vector3.Distance(ejectorAxis.position, ejectorEjectPoint.position)/Vector3.Distance(ejectorRoot.position, ejectorEjectPoint.position);
                    springRoot.localScale = Vector3.Lerp(Vector3.one, springTargetScale, time);
                }

                if (!roundReparented && loadedCartridges[LoadModeChamber()] != null && Vector3.Distance(ejectorRB.transform.localPosition, ejectorRoot.localPosition) > 0.004f)
                {
                    roundReparented = true;
                    Cartridge c = loadedCartridges[LoadModeChamber()];
                    c.transform.parent = roundReparent;
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }

                if (roundReparented && Vector3.Distance(ejectorRB.transform.localPosition, ejectorEjectPoint.localPosition) <= 0.004f)
                {
                    roundReparented = false;
                    Cartridge c = loadedCartridges[LoadModeChamber()];
                    Util.PlayRandomAudioSource(ejectSounds);
                    loadedCartridges[LoadModeChamber()] = null;
                    Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                    c.ToggleCollision(true);
                    Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
                    Rigidbody rb = c.item.physicBody.rigidBody;
                    c.item.disallowDespawn = false;
                    c.transform.parent = null;
                    c.loaded = false;
                    rb.isKinematic = false;
                    rb.WakeUp();
                    if (ejectDir != null) AddForceToCartridge(c, ejectDir, ejectForce);
                    c.ToggleHandles(true);
                    InvokeEjectRound(c);
                    SaveCartridges();
                }
            }
            else
            {
                if (firearm.triggerState)
                    TryFire();
                if (springRoot != null)
                {
                    springRoot.localScale = Vector3.one;
                }
            }
        }

        private void Update()
        {
            BaseUpdate();
        }

        private void Firearm_OnAltActionEvent(bool longPress)
        {
            if (!longPress) AltPress();
        }

        private void Firearm_OnTriggerChangeEvent(bool isPulled)
        {
            if (isPulled) TriggerPress();
        }

        private void Firearm_OnCollisionEvent(Collision collision)
        {
            if (loadMode && allowLoad && Util.CheckForCollisionWithThisCollider(collision, loadCollider) && collision.gameObject.GetComponentInParent<Cartridge>() is Cartridge c && !c.loaded)
            {
                LoadChamber(LoadModeChamber(), c, true);
            }
        }

        public void SaveCartridges()
        {
            data.contents = new string[loadedCartridges.Length];
            for (int i = 0; i < loadedCartridges.Length; i++)
            {
                data.contents[i] = loadedCartridges[i]?.item.itemId;
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            for (int i = 0; i < mountPoints.Length; i++)
            {
                if (loadedCartridges[i] != null)
                {
                    loadedCartridges[i].GetComponent<Rigidbody>().isKinematic = true;
                    loadedCartridges[i].transform.parent = mountPoints[i];
                    loadedCartridges[i].transform.localPosition = Vector3.zero;
                    loadedCartridges[i].transform.localEulerAngles = Util.RandomCartridgeRotation();
                }
            }
        }

        public void LoadChamber(int index, Cartridge cartridge, bool overrideSave = true)
        {
            if (loadedCartridges[index] == null && Util.AllowLoadCartridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.disallowDespawn = true;
                cartridge.loaded = true;
                cartridge.ToggleHandles(false);
                cartridge.ToggleCollision(false);
                cartridge.UngrabAll();
                Util.IgnoreCollision(cartridge.gameObject, firearm.gameObject, true);
                cartridge.GetComponent<Rigidbody>().isKinematic = true;
                cartridge.transform.parent = mountPoints[index];
                cartridge.transform.localPosition = Vector3.zero;
                cartridge.transform.localEulerAngles = Util.RandomCartridgeRotation();
                if (overrideSave) SaveCartridges();
            }
            UpdateChamberedRounds();
        }

        public int LoadModeChamber()
        {
            int i = currentChamber + loadChamberOffset;
            if (i > mountPoints.Length - 1) i -= mountPoints.Length;
            return i;
        }

        [EasyButtons.Button]
        public void AltPress()
        {
            if (loadMode && Vector3.Distance(ejectorRB.transform.localPosition, ejectorRoot.localPosition) > 0.004f) return;
            loadMode = !loadMode;
            ApplyChamber();
            if (loadMode)
            {
                Util.PlayRandomAudioSource(openSounds);
                loadGateAxis.localPosition = loadGateOpenedPosition.localPosition;
                loadGateAxis.localEulerAngles = loadGateOpenedPosition.localEulerAngles;

                hammerAxis.localPosition = hammerCockedPosition.localPosition;
                hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
            }
            else
            {
                Util.PlayRandomAudioSource(closeSounds);
                loadGateAxis.localPosition = loadGateClosedPosition.localPosition;
                loadGateAxis.localEulerAngles = loadGateClosedPosition.localEulerAngles;

                if (!cocked)
                {
                    hammerAxis.localPosition = hammerIdlePosition.localPosition;
                    hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
                }
                Destroy(ejectorJoint);
            }
            UpdateEjector();
        }

        public void UpdateEjector()
        {
            InitializeEjectorJoint();
            if (loadMode)
            {
                Vector3 vec = GrandparentLocalPosition(ejectorEjectPoint, firearm.item.transform);
                ejectorJoint.anchor = new Vector3(vec.x, vec.y, vec.z + ((ejectorRoot.localPosition.z - ejectorEjectPoint.localPosition.z) / 2));
                SoftJointLimit limit = new SoftJointLimit();
                limit.limit = Vector3.Distance(ejectorEjectPoint.position, ejectorRoot.position) / 2;
                ejectorJoint.linearLimit = limit;

                ejectorAxis.SetParent(ejectorRB.transform);
                ejectorAxis.localPosition = Vector3.zero;
                ejectorAxis.localEulerAngles = Vector3.zero;
            }
            else
            {
                Vector3 vec = GrandparentLocalPosition(ejectorRoot, firearm.item.transform);
                ejectorJoint.anchor = new Vector3(vec.x, vec.y, vec.z);
                SoftJointLimit limit = new SoftJointLimit();
                limit.limit = 0f;
                ejectorJoint.linearLimit = limit;

                ejectorAxis.SetParent(ejectorRoot);
                ejectorAxis.localPosition = Vector3.zero;
                ejectorAxis.localEulerAngles = Vector3.zero;
            }
        }

        public void InitializeEjectorJoint()
        {
            if (ejectorJoint == null)
            {
                ejectorJoint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
                ejectorRB.transform.position = ejectorRoot.position;
                ejectorRB.transform.rotation = ejectorRoot.rotation;
                ejectorJoint.connectedBody = ejectorRB;
                ejectorJoint.massScale = 0.00001f;

                ejectorJoint.autoConfigureConnectedAnchor = false;
                ejectorJoint.connectedAnchor = Vector3.zero;
                ejectorJoint.xMotion = ConfigurableJointMotion.Locked;
                ejectorJoint.yMotion = ConfigurableJointMotion.Locked;
                ejectorJoint.zMotion = ConfigurableJointMotion.Limited;
                ejectorJoint.angularXMotion = ConfigurableJointMotion.Locked;
                ejectorJoint.angularYMotion = ConfigurableJointMotion.Locked;
                ejectorJoint.angularZMotion = ConfigurableJointMotion.Locked;
            }
        }

        [EasyButtons.Button]
        public void TriggerPress()
        {
            if (loadMode && Vector3.Distance(ejectorRB.transform.localPosition, ejectorRoot.localPosition) <= 0.004f)
            {
                currentChamber++;
                if (currentChamber >= cylinderRotations.Length) currentChamber = 0;
                ApplyChamber();
            }
        }

        public override void TryFire()
        {
            if (cocked)
            {
                Util.PlayRandomAudioSource(hammerHitSounds);
                cocked = false;
                hammerAxis.localPosition = hammerIdlePosition.localPosition;
                hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;

                Cartridge loadedCartridge = loadedCartridges[currentChamber];
                if (loadedCartridge == null || loadedCartridge.fired)
                {
                    InvokeFireLogicFinishedEvent();
                    return;
                }

                foreach (RagdollHand hand in firearm.item.handlers)
                {
                    if (hand.playerHand != null || hand.playerHand.controlHand != null)
                        hand.playerHand.controlHand.HapticShort(50f);
                }
                IncrementBreachSmokeTime();
                firearm.PlayFireSound(loadedCartridge);
                if (loadedCartridge.data.playFirearmDefaultMuzzleFlash)
                {
                    firearm.PlayMuzzleFlash(loadedCartridge);
                }
                FireMethods.ApplyRecoil(firearm.transform, firearm.item.physicBody.rigidBody, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.recoilModifiers);
                FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out List<Vector3> hits, out List<Vector3> trajectories, out List<Creature> hitCreatures, firearm.CalculateDamageMultiplier(), HeldByAI());
                loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, !FirearmsSettings.infiniteAmmo);
                InvokeFireEvent();
            }
            InvokeFireLogicFinishedEvent();
        }

        [EasyButtons.Button]
        public void Cock()
        {
            if (loadMode) return;
            if (!cocked)
            {
                Util.PlayRandomAudioSource(hammerCockSounds);
                cocked = true;
                hammerAxis.localPosition = hammerCockedPosition.localPosition;
                hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
                currentChamber++;
                if (currentChamber >= cylinderRotations.Length) currentChamber = 0;
                ApplyChamber();
            }
            else
            {
                cocked = false;
                hammerAxis.localPosition = hammerIdlePosition.localPosition;
                hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
            }
        }

        public void ApplyChamber()
        {
            cylinderAxis.localEulerAngles = loadMode ? cylinderLoadRotations[currentChamber] : cylinderRotations[currentChamber];
        }
    }
}
