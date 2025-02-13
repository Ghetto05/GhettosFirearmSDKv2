using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class AttachmentFirearmModeManager : MonoBehaviour
    {
        private Firearm _connectedFirearm;
        private Handle _firearmTriggerHandle;
        public AttachmentFirearm attachmentFirearm;
        public bool addAttachmentFirearmMode;
        [CatalogPicker(new[] {Category.HandPose})]
        public string replacementTriggerHandlePose;
        [CatalogPicker(new[] {Category.HandPose})]
        public string replacementTriggerHandleTargetPose;
        public string[] allowReplacementOnItems;
        
        private HandPoseData _defaultHandPoseData;
        private HandPoseData _targetHandPoseData;
        private HandPoseData _replacementDefaultHandPoseData;
        private HandPoseData _replacementTargetHandPoseData;

        private void Start()
        {
            Invoke(nameof(InvokedStart), Settings.invokeTime + 0.06f);
        }

        private void InvokedStart()
        {
            if (attachmentFirearm.attachment.attachmentPoint.parentManager is not Firearm f)
                return;
            _connectedFirearm = f;
            _firearmTriggerHandle = _connectedFirearm.item.mainHandleLeft;
            
            LoadHandleData();
            _defaultHandPoseData = _firearmTriggerHandle.orientations.First().defaultHandPoseData;
            _targetHandPoseData = _firearmTriggerHandle.orientations.First().targetHandPoseData;
            
            _connectedFirearm.OnFiremodeChangedEvent += ConnectedFirearmOnOnFiremodeChangedEvent;
            _connectedFirearm.SavedAmmoItemChangedEvent += FirearmOnSavedAmmoItemChangedEvent;
            attachmentFirearm.SavedAmmoItemChangedEvent += FirearmOnSavedAmmoItemChangedEvent;
            attachmentFirearm.attachment.OnDetachEvent += AttachmentOnOnDetachEvent;
            
            AddAttachmentFirearmMode();
            
            ApplyDefaultAmmoItem();
        }

        private void FirearmOnSavedAmmoItemChangedEvent()
        {
            ApplyDefaultAmmoItem();
        }

        private void AttachmentOnOnDetachEvent(bool despawndetach)
        {
            if (despawndetach)
                return;

            if (addAttachmentFirearmMode)
            {
                foreach (var selector in _connectedFirearm.GetComponentsInChildren<FiremodeSelector>().ToList())
                {
                    var modes = selector.firemodes.ToList();
                    modes.Remove(FirearmBase.FireModes.AttachmentFirearm);
                    selector.firemodes = modes.ToArray();
                }

                ApplyHandleData();
            }
            
            _connectedFirearm.OnFiremodeChangedEvent -= ConnectedFirearmOnOnFiremodeChangedEvent;
            _connectedFirearm.SavedAmmoItemChangedEvent -= FirearmOnSavedAmmoItemChangedEvent;
            attachmentFirearm.SavedAmmoItemChangedEvent -= FirearmOnSavedAmmoItemChangedEvent;
            attachmentFirearm.attachment.OnDetachEvent -= AttachmentOnOnDetachEvent;
            
            _connectedFirearm.RemoveOverideAmmoItem(this);
        }

        private void AddAttachmentFirearmMode()
        {
            if (!addAttachmentFirearmMode)
                return;
            
            if (_connectedFirearm.GetComponentInChildren<FiremodeSelector>()?.firemodes.Contains(FirearmBase.FireModes.AttachmentFirearm) ?? false)
            {
                addAttachmentFirearmMode = false;
                return;
            }

            foreach (var selector in _connectedFirearm.item.GetComponentsInChildren<FiremodeSelector>().ToList())
            {
                var list = selector.firemodes.ToList();
                var index = list.Contains(FirearmBase.FireModes.Auto) ? 2 : 1;
                list.Insert(index, FirearmBase.FireModes.AttachmentFirearm);
                selector.firemodes = list.ToArray();
            }
        }

        private void ConnectedFirearmOnOnFiremodeChangedEvent()
        {
            ApplyHandleData();
            ApplyDefaultAmmoItem();
        }

        private void LoadHandleData()
        {
            if (replacementTriggerHandlePose.IsNullOrEmptyOrWhitespace() || replacementTriggerHandleTargetPose.IsNullOrEmptyOrWhitespace())
                return;

            _replacementDefaultHandPoseData = Catalog.GetData<HandPoseData>(replacementTriggerHandlePose);
            _replacementTargetHandPoseData = Catalog.GetData<HandPoseData>(replacementTriggerHandleTargetPose);
        }

        private void ApplyHandleData()
        {
            if (_firearmTriggerHandle == null || _replacementDefaultHandPoseData == null || _replacementTargetHandPoseData == null || !allowReplacementOnItems.Contains(_connectedFirearm.item.itemId))
                return;
            switch (_connectedFirearm.fireMode)
            {
                case FirearmBase.FireModes.AttachmentFirearm:
                    foreach (var pose in _firearmTriggerHandle.orientations)
                    {
                        pose.defaultHandPoseData = _replacementDefaultHandPoseData;
                        pose.targetHandPoseData = _replacementTargetHandPoseData;
                    }
                    break;
                default:
                    foreach (var pose in _firearmTriggerHandle.orientations)
                    {
                        pose.defaultHandPoseData = _defaultHandPoseData;
                        pose.targetHandPoseData = _targetHandPoseData;
                    }
                    break;
            }
            foreach (var hand in _firearmTriggerHandle.handlers)
            {
                hand.poser.SetDefaultPose(_firearmTriggerHandle.orientations.First().defaultHandPoseData);
                hand.poser.SetTargetPose(_firearmTriggerHandle.orientations.First().targetHandPoseData);
            }
        }

        private void ApplyDefaultAmmoItem()
        {
            _connectedFirearm.SetOverideAmmoItem(_connectedFirearm.fireMode == FirearmBase.FireModes.AttachmentFirearm ? attachmentFirearm.GetAmmoItem(true) : _connectedFirearm.GetAmmoItem(true), this);
        }
    }
}