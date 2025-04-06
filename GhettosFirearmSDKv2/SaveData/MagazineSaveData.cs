using System;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class MagazineSaveData : ContentCustomData
{
    public string ItemID;

    [JsonConverter(typeof(CartridgeSaveData.StringArrayToDataArrayConverter))]
    public CartridgeSaveData[] Contents;

    public void ApplyToMagazine(Magazine magazine)
    {
        if (Contents is null || Contents.Length == 0)
        {
            magazine.loadable = true;
            magazine.InvokeLoadFinished();
            return;
        }
        ApplyToMagazineRecurve(Contents.Length - 1, magazine, Contents.CloneJson());
    }

    public void ApplyToMagazine(StripperClip clip)
    {
        if (Contents is null || Contents.Length == 0)
        {
            clip.loadable = true;
            return;
        }
        ApplyToClipRecurve(Contents.Length - 1, clip, Contents.CloneJson());
    }

    private void ApplyToMagazineRecurve(int index, Magazine mag, CartridgeSaveData[] con)
    {
        if (index < 0)
        {
            mag.loadable = true;
            mag.InvokeLoadFinished();
            return;
        }
        try
        {
            Util.SpawnItem(con[index].ItemId, "Magazine save data", cartridge =>
            {
                var car = cartridge.GetComponent<Cartridge>();
                mag.InsertRound(car, true, true, false);
                con[index].Apply(car);
                ApplyToMagazineRecurve(index - 1, mag, con);
            }, mag.transform.position + Vector3.up * 3, null, null, false);
        }
        catch (Exception)
        {
            Debug.LogError($"Error mag: {mag}\n" +
                           $"Contents: {con}\n" +
                           $"Index: {index}\"" +
                           $"Contents length: {con.Length}\n" +
                           $"Cartridge: {con[index].ItemId}");
        }
    }

    private void ApplyToClipRecurve(int index, StripperClip clip, CartridgeSaveData[] con)
    {
        if (index < 0)
        {
            clip.loadable = true;
            return;
        }
        try
        {
            Util.SpawnItem(con[index].ItemId, "Clip save data", cartridge =>
            {
                var car = cartridge.GetComponent<Cartridge>();
                clip.InsertRound(car, true, true, false);
                con[index].Apply(car);
                ApplyToClipRecurve(index - 1, clip, con);
            }, clip.transform.position + Vector3.up * 3, null, null, false);
        }
        catch (Exception)
        {
            Debug.LogError($"Error clip: {clip}\n" +
                           $"Contents: {con}\n" +
                           $"Index: {index}\"" +
                           $"Contents length: {con.Length}\n" +
                           $"Cartridge: {con[index].ItemId}");
        }
    }

    public void GetFromUnknown(GameObject obj)
    {
        if (!obj)
        {
            return;
        }

        if (obj.GetComponent<Magazine>() is { } mag)
        {
            GetContentsFromMagazine(mag);
        }
        else if (obj.GetComponent<StripperClip>() is { } clip)
        {
            GetContentsFromClip(clip);
        }
    }

    public void GetContentsFromMagazine(Magazine magazine)
    {
        if (!magazine || magazine.cartridges is null)
        {
            return;
        }
        Contents = new CartridgeSaveData[magazine.cartridges.Count];
        for (var i = 0; i < magazine.cartridges.Count; i++)
        {
            var car = magazine.cartridges[i];
            Contents[i] = new CartridgeSaveData(car.item.itemId, car.Fired);
        }
    }

    public void GetContentsFromClip(StripperClip clip)
    {
        if (!clip || clip.loadedCartridges is null)
        {
            return;
        }
        Contents = new CartridgeSaveData[clip.loadedCartridges.Count];
        for (var i = 0; i < clip.loadedCartridges.Count; i++)
        {
            var car = clip.loadedCartridges[i];
            Contents[i] = new CartridgeSaveData(car.item.itemId, car.Fired);
        }
    }

    public void CloneTo(MagazineSaveData data)
    {
        data.ItemID = ItemID;
        data.Contents = new CartridgeSaveData[Contents.Length];
        Contents.CopyTo(data.Contents, 0);
    }

    public void Clear()
    {
        ItemID = null;
        Contents = null;
    }
}