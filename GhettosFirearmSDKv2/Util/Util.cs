using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GhettosFirearmSDKv2.Attachments;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GhettosFirearmSDKv2;

public class Util
{
    public static Vector3 RandomCartridgeRotation()
    {
        return new Vector3(0, 0, Random.Range(0f, 360f));
    }

    public static Vector3 RandomRotation()
    {
        return new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
    }

    public static void RandomizeZRotation(Transform target)
    {
        var randomAngle = Random.Range(0f, 360f);
        target.Rotate(0f, 0f, randomAngle, Space.Self);
    }

    public static bool AllowLoadCartridge(Cartridge cartridge, string requiredCaliber)
    {
        return CheckCaliberMatch(cartridge.caliber, requiredCaliber);
    }

    public static bool AllowLoadCartridge(Cartridge cartridge, IAmmunitionLoadable magazine)
    {
        if (!Settings.doCaliberChecks && !magazine.GetForceCorrectCaliber())
        {
            return true;
        }

        return AllowLoadCartridge(cartridge.caliber, magazine);
    }

    public static bool AllowLoadCartridge(string cartridgeCaliber, IAmmunitionLoadable magazine, bool ignoreSameCaliber = false)
    {
        var correctCaliber =
            CheckCaliberMatch(cartridgeCaliber, magazine.GetCaliber(), magazine.GetForceCorrectCaliber()) ||
            CheckCaliberMatch(cartridgeCaliber, magazine.GetAlternativeCalibers(), magazine.GetForceCorrectCaliber());
        var magHasSameCaliber = ignoreSameCaliber || magazine.GetLoadedCartridges() is null || !magazine.GetLoadedCartridges().Any() || (magazine.GetLoadedCartridges().FirstOrDefault()?.caliber.Equals(cartridgeCaliber) ?? true);
        return correctCaliber && magHasSameCaliber;
    }

    public static bool AllowLoadCartridge(string cartridgeCaliber, string otherCaliber)
    {
        return CheckCaliberMatch(cartridgeCaliber, otherCaliber);
    }

    public static bool CheckCaliberMatch(string insertedCaliber, List<string> targetCalibers, bool ignoreCheat = false)
    {
        return targetCalibers is not null && targetCalibers.Any(targetCaliber => CheckCaliberMatch(insertedCaliber, targetCaliber, ignoreCheat));
    }

    public static bool CheckCaliberMatch(string insertedCaliber, string targetCaliber, bool ignoreCheat = false)
    {
        if (!Settings.doCaliberChecks && !ignoreCheat)
        {
            return true;
        }
        if (insertedCaliber.Equals("DEBUG UNIVERSAL"))
        {
            return true;
        }
        return insertedCaliber.Equals(targetCaliber) || CaliberSubstituteData.AllowSubstitution(insertedCaliber, targetCaliber);
    }

    public static bool AllowLoadMagazine(Magazine magazine, MagazineWell well)
    {
        if (magazine.currentWell || magazine.item.holder || well.currentMagazine)
        {
            return false;
        }
        if (!Settings.doMagazineTypeChecks)
        {
            return true;
        }
        if (magazine.magazineType.Equals("DEBUG UNIVERSAL"))
        {
            return true;
        }

        var sameType = magazine.magazineType.Equals(well.acceptedMagazineType) || 
                       well.alternateMagazineTypes.Any(t => t.Equals(magazine.magazineType));
        var compatibleCaliber = magazine.cartridges.Count == 0 ||
                                !Settings.doCaliberChecks ||
                                well.caliber.Equals(magazine.cartridges[0].caliber) ||
                                well.alternateCalibers.Any(x => x.Equals(magazine.cartridges[0].caliber));

        return sameType && compatibleCaliber;
    }

    public static void AlertAllCreaturesInRange(Vector3 point, float range)
    {
        foreach (var cr in Creature.allActive)
        {
            if (cr.animator.GetBoneTransform(HumanBodyBones.Head) is { } bone && Vector3.Distance(bone.position, point) <= range)
            {
                cr.brain.SetState(Brain.State.Alert);
            }
        }
    }

    public static bool CheckForCollisionWithColliders(List<Collider> theseColliders, List<Collider> otherColliders, Collision collision)
    {
        if (theseColliders is not null && otherColliders is null)
        {
            return collision.contacts.Any(con => theseColliders.Any(c => c == con.thisCollider));
        }
        if (theseColliders is null && otherColliders is not null)
        {
            return collision.contacts.Any(con => otherColliders.Any(c => c == con.otherCollider));
        }
        return theseColliders is not null && collision.contacts.Any(con => theseColliders.Any(c => c == con.thisCollider) && otherColliders.Any(oc => oc == con.otherCollider));
    }

    public static bool CheckForCollisionWithThisCollider(Collision collision, Collider thisCollider)
    {
        return collision.contacts.Any(con => con.thisCollider == thisCollider);
    }

    public static bool CheckForCollisionWithOtherCollider(Collision collision, Collider otherCollider)
    {
        return collision.contacts.Any(con => con.otherCollider == otherCollider);
    }

    public static bool CheckForCollisionWithBothColliders(Collision collision, Collider thisCollider, Collider otherCollider)
    {
        return collision.contacts.Any(con => con.thisCollider == thisCollider && con.otherCollider == otherCollider);
    }

    public static void IgnoreCollision(GameObject obj1, GameObject obj2, bool ignore)
    {
        if (!obj1 || !obj2)
        {
            return;
        }
        try
        {
            foreach (var c1 in obj1.GetComponentsInChildren<Collider>())
            {
                foreach (var c2 in obj2.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(c1, c2, ignore);
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void DelayIgnoreCollision(GameObject obj1, GameObject obj2, bool ignore, float delay, Item handler)
    {
        handler.StartCoroutine(DelayIgnoreCollisionCoroutine(obj1, obj2, ignore, delay));
    }

    private static IEnumerator DelayIgnoreCollisionCoroutine(GameObject obj1, GameObject obj2, bool ignore, float delay)
    {
        yield return new WaitForSeconds(delay);

        IgnoreCollision(obj1, obj2, ignore);
    }

    public static AudioSource PlayRandomAudioSource(IEnumerable<AudioSource> sources)
    {
        var source = GetRandomFromList(sources);
        if (!source) return null;
        source.PlayOneShot(source.clip);
        return source;
    }

    public static T GetRandomFromList<T>(IEnumerable<T> list)
    {
        var l = list?.ToList();
        if (l is null || !l.Any())
        {
            return default;
        }
        return l[Random.Range(0, l.Count)];
    }

    public static float AbsDist(Vector3 v1, Vector3 v2)
    {
        return Mathf.Abs(Vector3.Distance(v1, v2));
    }

    public static float AbsDist(Transform v1, Transform v2)
    {
        return Mathf.Abs(Vector3.Distance(v1.position, v2.position));
    }

    public static void DelayedExecute(float delay, Action action, MonoBehaviour handler)
    {
        handler.StartCoroutine(DelayedExecuteIE(delay, action));
    }

    private static IEnumerator DelayedExecuteIE(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);

        action.Invoke();
    }

    public static bool AllLocksUnlocked(List<Lock> locks)
    {
        return locks.All(t => t.IsUnlocked());
    }

    public static Transform RecursiveFindChild(Transform parent, string childName)
    {
        foreach (Transform parent1 in parent)
        {
            if (parent1.gameObject.name.Equals(childName))
            {
                return parent1;
            }
            var child = RecursiveFindChild(parent1, childName);
            if (child)
            {
                return child;
            }
        }
        return null;
    }

    public static void UpdateLightVolumeReceiver(LightVolumeReceiver receiverToBeUpdated, LightProbeVolume currentLightProbeVolume, List<LightProbeVolume> lightProbeVolumes)
    {
        var method = typeof(LightVolumeReceiver).GetMethod("OnParentVolumeChange", BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(receiverToBeUpdated, [currentLightProbeVolume, lightProbeVolumes]);
    }

    /**
     * <summary>
     *     Function to normalize angles to be within [-180, 180]
     * </summary>
     */
    public static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    public static void DisableCollision(Item item, bool disable)
    {
        foreach (var c in item.colliderGroups.SelectMany(i => i.colliders))
        {
            c.enabled = !disable;
        }
    }

    public static void ApplyAudioConfig(ICollection<AudioSource> sources, bool suppressed = false)
    {
        if (!Player.local || !Player.local.head || !Player.local.head.cam)
        {
            return;
        }

        float range = 800;
        foreach (var source in sources)
        {
            source.spatialBlend = 0.1f;
            if (suppressed)
            {
                source.volume = 0.7f;
                source.maxDistance = 50f;
            }
            else
            {
                source.volume = 1f;
                source.maxDistance = 100f;
            }

            var distance = Vector3.Distance(source.transform.position, Player.local.head.cam.transform.position);
            if (distance <= 2)
            {
                source.volume *= 1;
            }
            else if (distance >= range)
            {
                source.volume *= 0;
            }
            else
            {
                var decayFactor = Mathf.Exp(-distance / 50);
                source.volume *= (1f - 0.3f) * decayFactor;
            }
            //source.volume *= 1 - (distance / range);
        }
    }

    public static void SpawnItem(string id, string handler,
                                 Action<Item> callback,
                                 Vector3? position = null,
                                 Quaternion? rotation = null,
                                 Transform parent = null,
                                 bool pooled = true,
                                 List<ContentCustomData> customDataList = null,
                                 Item.Owner owner = Item.Owner.None)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }
        Catalog.GetData<ItemData>(GetSubstituteId(id, handler), Settings.debugMode)?.SpawnAsync(callback, position, rotation, parent, pooled, customDataList, owner);
    }

    private static ObsoleteIdData _obsoleteIdData;
    private static bool _triedLoadingObsoleteIds;

    public static string GetSubstituteId(string id, string handler, bool hideDebug = false)
    {
        if (_obsoleteIdData is null && !_triedLoadingObsoleteIds)
        {
            _triedLoadingObsoleteIds = true;
            _obsoleteIdData = Catalog.GetDataList<ObsoleteIdData>().FirstOrDefault();
        }

        if (_obsoleteIdData is not null)
        {
            if (_obsoleteIdData.IdMatches.TryGetValue(id, out var substituteId))
            {
                if (Settings.debugMode)
                {
                    Debug.Log($"OBSOLETE ID! {id} was replaced by {substituteId}! Handler: {handler}");
                }

                return substituteId;
            }
        }
        return id;
    }

    public static bool CompareSubstitutedIDs(string id, string id2)
    {
        return id.Equals(id2) ||
               GetSubstituteId(id, "", true).Equals(id2) ||
               id.Equals(GetSubstituteId(id2, "", true)) ||
               GetSubstituteId(id, "", true).Equals(GetSubstituteId(id2, "", true));
    }

    public static void DebugObject(object obj, string debugName)
    {
        var output = new StringBuilder();

        if (obj is null)
        {
            Debug.Log("The object is null.");
            return;
        }

        output.AppendLine(debugName);
        output.AppendLine();

        var type = obj.GetType();
        output.AppendLine("Object Type: " + type.Name);

        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj, null);
                output.AppendLine(property.Name + " (Property): " + value);
            }
            catch (Exception e)
            {
                output.AppendLine("Could not retrieve the value of property: " + property.Name + ". Error: " + e.Message);
            }
        }

        var fields = type.GetFields();
        foreach (var field in fields)
        {
            try
            {
                var value = field.GetValue(obj);
                output.AppendLine(field.Name + " (Field): " + value);
            }
            catch (Exception e)
            {
                output.AppendLine("Could not retrieve the value of field: " + field.Name + ". Error: " + e.Message);
            }
        }

        Debug.Log(output.ToString());
    }

    public static bool DoMalfunction(bool malfunctionEnabled, float baseChance, float multiplier, bool heldByAI)
    {
        var random = Random.Range(0f, 100f);
        var threashold = Mathf.Clamp(baseChance * multiplier * Settings.malfunctionMode, 0f, 100f);
        return malfunctionEnabled && !heldByAI && threashold >= random;
    }

    public static void AddInfoToBuilder(string name, object data, StringBuilder builder)
    {
        AddInfoToBuilder(name, new[] { data }, builder);
    }

    public static void AddInfoToBuilder(string name, IEnumerable<object> data, StringBuilder builder, bool ignoreEmpty = true)
    {
        var dataList = data?.ToList();
        var empty = dataList?.Any() != true;
        if (empty && ignoreEmpty)
        {
            return;
        }
        builder.Append(name);
        builder.Append(": ");
        if (empty)
        {
            return;
        }
        for (var i = 0; i < dataList.Count; i++)
        {
            if (i != 0)
            {
                builder.Append(", ");
            }
            builder.Append(dataList[i]);
        }
        builder.AppendLine();
    }
    
    public static IEnumerator RequestInitialization(GameObject manager, Action<InitializationData> initialization)
    {
        if (manager.GetComponent<Attachment>() is { } attachment)
        {
            yield return new WaitUntil(() => attachment.initialized);
            initialization.Invoke(new InitializationData(attachment.attachmentPoint.ConnectedManager, attachment, attachment.Node));
        }
        else if (manager.GetComponent<IAttachmentManager>() is { } attachmentManager)
        {
            yield return new WaitUntil(() => attachmentManager.SaveData != null);
            initialization.Invoke(new InitializationData(attachmentManager, attachmentManager, attachmentManager.SaveData.FirearmNode));
        }
    }

    public struct InitializationData
    {
        public InitializationData(IAttachmentManager manager, IInteractionProvider interactionProvider, FirearmSaveData.AttachmentTreeNode node)
        {
            Manager = manager;
            InteractionProvider = interactionProvider;
            Node = node;
        }

        public readonly IAttachmentManager Manager;
        public readonly IInteractionProvider InteractionProvider;
        public readonly FirearmSaveData.AttachmentTreeNode Node;
    }
}