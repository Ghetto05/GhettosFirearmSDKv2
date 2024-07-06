using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class UpdateChecker
    {
        public static string message = "";

        public static string CheckForUpdates()
        {
            if (!FirearmsSettings.hideUpdateNotifications && AttemptDownload(out string content))
            {
                EventManager.onLevelLoad += EventManager_onLevelLoad;
                List<ModManager.ModData> modsToUpdate = new List<ModManager.ModData>();
                Dictionary<string, string> versionData = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                List<ModManager.ModData> mods = ModManager.loadedMods.ToList();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    foreach (ModManager.ModData mod in mods)
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
                    foreach (ModManager.ModData data in modsToUpdate)
                    {
                        message += "\n---->   - " + data.Name + ", new version: " + versionData[data.Name];
                    }
                    return message;
                }
                else
                {
                    message = "----> All checkable mods are up to date!";
                    return message;
                }
            }
            return "";
        }

        private static void EventManager_onLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
                return;
            GameManager.local.StartCoroutine(DelayedMessage());
        }

        private static IEnumerator DelayedMessage()
        {
            yield return new WaitForSeconds(6f);
            if (!string.IsNullOrWhiteSpace(message) && !message.Equals("----> All checkable mods are up to date!"))
                DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData(message, 100));
        }

        public static bool AttemptDownload(out string content)
        {
            content = "";
            using (WebClient wc = new WebClient())
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
            Version currentVersion = new Version(current);
            Version newerVersion = new Version(newest);
            //return currentVersion.CompareTo(newerVersion) == 0; // for debug
            return currentVersion.CompareTo(newerVersion) >= 0;
        }
    }
}