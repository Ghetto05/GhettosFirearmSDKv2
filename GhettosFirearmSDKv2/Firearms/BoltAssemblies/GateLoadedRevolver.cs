using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class GateLoadedRevolver : BoltBase
    {
        private bool _loadMode;
        private bool _cocked;
        private int _currentChamber;

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
        public Transform hammerLoadPosition;
        public Collider cockCollider;

        [Header("Ejector Rod")]
        public Transform roundReparent;
        public Rigidbody ejectorRb;
        public Transform ejectorAxis;
        public Transform ejectorRoot;
        public Transform ejectorEjectPoint;
        private ConfigurableJoint _ejectorJoint;
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

        private bool _roundReparented;
        private bool _allowLoad;
        private MagazineSaveData _data;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime + 0.2f);
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

            if (firearm.item.TryGetCustomData(out _data))
            {
                for (var i = 0; i < _data.Contents.Length; i++)
                {
                    if (_data.Contents[i] != null)
                    {
                        var index = i;
                        Util.SpawnItem(_data.Contents[index]?.ItemId, "Bolt Chamber", ci =>
                        {
                            var c = ci.GetComponent<Cartridge>();
                            _data.Contents[index].Apply(c);
                            LoadChamber(index, c, false);
                        }, transform.position + Vector3.up * 3);
                    }
                }
                UpdateChamberedRounds();
            }
            else
            {
                firearm.item.AddCustomData(new MagazineSaveData());
                firearm.item.TryGetCustomData(out _data);
                _data.Contents = new CartridgeSaveData[loadedCartridges.Length];
            }
            _allowLoad = true;
            UpdateChamberedRounds();
        }

        private void Firearm_OnCollisionEventTR(CollisionInstance collisionInstance)
        {
            if (cockCollider != null && collisionInstance.sourceCollider == cockCollider && collisionInstance.targetCollider.GetComponentInParent<Player>() != null && !_cocked)
            {
                Cock();
            }
        }

        private void FixedUpdate()
        {
            if (_loadMode)
            {
                if (_roundReparented && Vector3.Distance(ejectorRb.transform.localPosition, ejectorRoot.localPosition) <= 0.004f)
                {
                    _roundReparented = false;
                    var c = loadedCartridges[LoadModeChamber()];
                    c.transform.parent = mountPoints[LoadModeChamber()];
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }

                if (springRoot != null)
                {
                    var time = Vector3.Distance(ejectorAxis.position, ejectorEjectPoint.position)/Vector3.Distance(ejectorRoot.position, ejectorEjectPoint.position);
                    springRoot.localScale = Vector3.Lerp(Vector3.one, springTargetScale, time);
                }

                if (!_roundReparented && loadedCartridges[LoadModeChamber()] && Vector3.Distance(ejectorRb.transform.localPosition, ejectorRoot.localPosition) > 0.004f)
                {
                    _roundReparented = true;
                    var c = loadedCartridges[LoadModeChamber()];
                    c.transform.parent = roundReparent;
                    c.transform.localPosition = Vector3.zero;
                    c.transform.localEulerAngles = Util.RandomCartridgeRotation();
                }

                if (_roundReparented && Vector3.Distance(ejectorRb.transform.localPosition, ejectorEjectPoint.localPosition) <= 0.004f)
                {
                    _roundReparented = false;
                    var c = loadedCartridges[LoadModeChamber()];
                    Util.PlayRandomAudioSource(ejectSounds);
                    loadedCartridges[LoadModeChamber()] = null;
                    Util.IgnoreCollision(c.gameObject, firearm.gameObject, true);
                    c.ToggleCollision(true);
                    Util.DelayIgnoreCollision(c.gameObject, firearm.gameObject, false, 3f, firearm.item);
                    var rb = c.item.physicBody.rigidBody;
                    c.item.DisallowDespawn = false;
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
                if (springRoot)
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
            if (_loadMode && _allowLoad && Util.CheckForCollisionWithThisCollider(collision, loadCollider) && collision.gameObject.GetComponentInParent<Cartridge>() is { } c && !c.loaded)
            {
                LoadChamber(LoadModeChamber(), c);
            }
        }

        public void SaveCartridges()
        {
            _data.Contents = new CartridgeSaveData[loadedCartridges.Length];
            for (var i = 0; i < loadedCartridges.Length; i++)
            {
                var car = loadedCartridges[i];
                _data.Contents[i] = new CartridgeSaveData(car?.item.itemId, car?.Fired ?? false);
            }
        }

        public override void UpdateChamberedRounds()
        {
            base.UpdateChamberedRounds();
            for (var i = 0; i < mountPoints.Length; i++)
            {
                if (loadedCartridges[i])
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
            if (!loadedCartridges[index] && Util.AllowLoadCartridge(cartridge, calibers[index]))
            {
                if (overrideSave) Util.PlayRandomAudioSource(insertSounds);
                loadedCartridges[index] = cartridge;
                cartridge.item.DisallowDespawn = true;
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
            var i = _currentChamber + loadChamberOffset;
            if (i > mountPoints.Length - 1) i -= mountPoints.Length;
            return i;
        }

        public void AltPress()
        {
            if (_loadMode && Vector3.Distance(ejectorRb.transform.localPosition, ejectorRoot.localPosition) > 0.004f)
                return;
            _loadMode = !_loadMode;
            ApplyChamber();
            if (_loadMode)
            {
                Util.PlayRandomAudioSource(openSounds);
                loadGateAxis.localPosition = loadGateOpenedPosition.localPosition;
                loadGateAxis.localEulerAngles = loadGateOpenedPosition.localEulerAngles;

                if (hammerAxis && hammerLoadPosition)
                {
                    hammerAxis.localPosition = hammerLoadPosition.localPosition;
                    hammerAxis.localEulerAngles = hammerLoadPosition.localEulerAngles;
                }
                else if (hammerAxis && hammerCockedPosition)
                {
                    hammerAxis.localPosition = hammerCockedPosition.localPosition;
                    hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
                }
                
            }
            else
            {
                Util.PlayRandomAudioSource(closeSounds);
                loadGateAxis.localPosition = loadGateClosedPosition.localPosition;
                loadGateAxis.localEulerAngles = loadGateClosedPosition.localEulerAngles;

                if (!_cocked)
                {
                    hammerAxis.localPosition = hammerIdlePosition.localPosition;
                    hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
                }
                else
                {
                    hammerAxis.localPosition = hammerCockedPosition.localPosition;
                    hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
                }
                Destroy(_ejectorJoint);
            }
            UpdateEjector();
        }

        public void UpdateEjector()
        {
            InitializeEjectorJoint();
            if (_loadMode)
            {
                var vec = GrandparentLocalPosition(ejectorEjectPoint, firearm.item.transform);
                _ejectorJoint.anchor = new Vector3(vec.x, vec.y, vec.z + ((ejectorRoot.localPosition.z - ejectorEjectPoint.localPosition.z) / 2));
                var limit = new SoftJointLimit();
                limit.limit = Vector3.Distance(ejectorEjectPoint.position, ejectorRoot.position) / 2;
                _ejectorJoint.linearLimit = limit;

                ejectorAxis.SetParent(ejectorRb.transform);
                ejectorAxis.localPosition = Vector3.zero;
                ejectorAxis.localEulerAngles = Vector3.zero;
            }
            else
            {
                var vec = GrandparentLocalPosition(ejectorRoot, firearm.item.transform);
                _ejectorJoint.anchor = new Vector3(vec.x, vec.y, vec.z);
                var limit = new SoftJointLimit();
                limit.limit = 0f;
                _ejectorJoint.linearLimit = limit;

                ejectorAxis.SetParent(ejectorRoot);
                ejectorAxis.localPosition = Vector3.zero;
                ejectorAxis.localEulerAngles = Vector3.zero;
            }
        }

        public void InitializeEjectorJoint()
        {
            if (!_ejectorJoint)
            {
                _ejectorJoint = firearm.item.gameObject.AddComponent<ConfigurableJoint>();
                ejectorRb.transform.position = ejectorRoot.position;
                ejectorRb.transform.rotation = ejectorRoot.rotation;
                _ejectorJoint.connectedBody = ejectorRb;
                _ejectorJoint.massScale = 0.00001f;

                _ejectorJoint.autoConfigureConnectedAnchor = false;
                _ejectorJoint.connectedAnchor = Vector3.zero;
                _ejectorJoint.xMotion = ConfigurableJointMotion.Locked;
                _ejectorJoint.yMotion = ConfigurableJointMotion.Locked;
                _ejectorJoint.zMotion = ConfigurableJointMotion.Limited;
                _ejectorJoint.angularXMotion = ConfigurableJointMotion.Locked;
                _ejectorJoint.angularYMotion = ConfigurableJointMotion.Locked;
                _ejectorJoint.angularZMotion = ConfigurableJointMotion.Locked;
            }
        }

        public void TriggerPress()
        {
            if (_loadMode && Vector3.Distance(ejectorRb.transform.localPosition, ejectorRoot.localPosition) <= 0.004f)
            {
                _currentChamber++;
                if (_currentChamber >= cylinderRotations.Length) _currentChamber = 0;
                ApplyChamber();
            }
        }

        public override void TryFire()
        {
            if (_cocked)
            {
                Util.PlayRandomAudioSource(hammerHitSounds);
                _cocked = false;
                hammerAxis.localPosition = hammerIdlePosition.localPosition;
                hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;

                var loadedCartridge = loadedCartridges[_currentChamber];
                if (!loadedCartridge || loadedCartridge.Fired)
                {
                    InvokeFireLogicFinishedEvent();
                    return;
                }

                foreach (var hand in firearm.item.handlers)
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
                FireMethods.ApplyRecoil(firearm.transform, firearm.item, loadedCartridge.data.recoil, loadedCartridge.data.recoilUpwardsModifier, firearm.recoilModifier, firearm.RecoilModifiers);
                FireMethods.Fire(firearm.item, firearm.actualHitscanMuzzle, loadedCartridge.data, out var hits, out var trajectories, out var hitCreatures, out var killedCreatures, firearm.CalculateDamageMultiplier(), firearm.HeldByAI());
                loadedCartridge.Fire(hits, trajectories, firearm.actualHitscanMuzzle, hitCreatures, killedCreatures, !Settings.infiniteAmmo);
                InvokeFireEvent();
                SaveCartridges();
            }
            InvokeFireLogicFinishedEvent();
        }

        public void Cock()
        {
            if (_loadMode) return;
            if (!_cocked)
            {
                Util.PlayRandomAudioSource(hammerCockSounds);
                _cocked = true;
                hammerAxis.localPosition = hammerCockedPosition.localPosition;
                hammerAxis.localEulerAngles = hammerCockedPosition.localEulerAngles;
                _currentChamber++;
                if (_currentChamber >= cylinderRotations.Length) _currentChamber = 0;
                ApplyChamber();
            }
            else
            {
                _cocked = false;
                hammerAxis.localPosition = hammerIdlePosition.localPosition;
                hammerAxis.localEulerAngles = hammerIdlePosition.localEulerAngles;
            }
        }

        public void ApplyChamber()
        {
            cylinderAxis.localEulerAngles = _loadMode ? cylinderLoadRotations[_currentChamber] : cylinderRotations[_currentChamber];
        }
    }
}
