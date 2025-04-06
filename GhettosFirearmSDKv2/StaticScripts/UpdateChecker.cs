using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

// ReSharper disable once ClassNeverInstantiated.Global - Instantinated by game code
public class UpdateChecker
{
    public static string message = "";

    public static string CheckForUpdates()
    {
        if (!Settings.hideUpdateNotifications && AttemptDownload(out var content))
        {
            EventManager.onLevelLoad += EventManager_onLevelLoad;
            var modsToUpdate = new List<ModManager.ModData>();
            var versionData = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            var mods = ModManager.loadedMods.ToList();
            if (!string.IsNullOrWhiteSpace(content))
            {
                foreach (var mod in mods)
                {
                    if (versionData.ContainsKey(mod.Name) && !NewerOrEqual(mod.ModVersion, versionData[mod.Name]))
                    {
                        modsToUpdate.Add(mod);
                    }
                }
            }

            if (modsToUpdate.Count != 0)
            {
                message = "----> The following mods need to be updated:";
                foreach (var data in modsToUpdate)
                {
                    message += "\n---->   - " + data.Name + ", new version: " + versionData[data.Name];
                }
                return message;
            }

            message = "----> All checkable mods are up to date!";
            return message;
        }
        return "";
    }

    private static void EventManager_onLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
    {
        if (eventTime == EventTime.OnStart)
        {
            return;
        }
        GameManager.local.StartCoroutine(DelayedMessage());
    }

    private static IEnumerator DelayedMessage()
    {
        yield return new WaitForSeconds(6f);

        if (!string.IsNullOrWhiteSpace(message) && !message.Equals("----> All checkable mods are up to date!"))
        {
            DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData(message, 100));
        }
        yield return new WaitForSeconds(6f);

        CheckForLowPhysics();
    }

    private static void CheckForLowPhysics()
    {
        if (GameManager.options.physicTimeStep == TimeManager.PhysicTimeStep.Default)
        {
            DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData("WARNING!\nYou are playing with Physics set to low!\nThis will break all firearms with a physical bolt/slide!\nPlease adjust your settings!", 100));
        }
    }

    public static bool AttemptDownload(out string content)
    {
        content = "";
        using (var wc = new WebClient())
        {
            wc.Headers.Add("a", "a");
            try
            {
                content = wc.DownloadString("https://raw.githubusercontent.com/Ghetto05/ModVersions/main/versions.json");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }

    public static bool NewerOrEqual(string current, string newest)
    {
        var currentVersion = new Version(current);
        var newerVersion = new Version(newest);
        //return currentVersion.CompareTo(newerVersion) == 0; // for debug
        return currentVersion.CompareTo(newerVersion) >= 0;
    }
}