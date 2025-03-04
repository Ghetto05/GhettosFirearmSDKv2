using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IngameDebugConsole;
using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class AttachmentValidator : MonoBehaviour
{
    public Item item;
        
    public Light errorLight;
    public Light successLight;
        
    public AttachmentPoint slot;

    public AudioSource[] clickSounds;

    private List<string> _attachmentIds;
    private static List<string> _attachmentIdBanList =
    [
        "Ghetto05.Firearms.M2.TopGrip",
        "Ghetto05.Firearms.M870.Stocks.AG870",
        "Ghetto05.Firearms.M870.Stocks.MesaLeo",
        "Ghetto05.Firearms.M870.Stocks.Raptor",
        "Ghetto05.Firearms.M870.Stocks.SGA",
        "Ghetto05.Firearms.M870.Stocks.SGAFDE",
        "Ghetto05.Firearms.M870.Stocks.SPS",
        "Ghetto05.Firearms.M870.Stocks.SPSAcajou",
        "Ghetto05.Firearms.M870.Stocks.SPSOrange",
        "Ghetto05.Firearms.M870.Stocks.SPSTeal",
        "Ghetto05.Firearms.M870.Stocks.SPSWood",
        "Ghetto05.Firearms.M870.Stocks.SPSFDE",
        "Ghetto05.Firearms.Mosin.Grip",
        "Ghetto05.Firearms.Mosin.StockCarbine",
        "Ghetto05.Firearms.Mosin.StockCarbineSniper",
        "Ghetto05.Firearms.Mosin.StockInfantry",
        "Ghetto05.Firearms.Mosin.StockMonteCarlo",
        "Ghetto05.Firearms.Mosin.StockSawn",
        "Ghetto05.Firearms.Mosin.StockSawnSniper",
        "Ghetto05.Firearms.Mosin.StockWooden",
        "Ghetto05.Firearms.P90.Stock",
        "Ghetto05.Firearms.P90.StockFDE",
        "Ghetto05.Firearms.P90.StockOD",
        "Ghetto05.Firearms.P90.StockPink",
        "Ghetto05.Firearms.P90.StockWhite",
        "Ghetto05.Firearms.SKS.OPStock",
        "Ghetto05.Firearms.SKS.TapcoStock",
        "Ghetto05.Firearms.SKS.TapcoStockFDE",
        "Ghetto05.Firearms.SKS.TapcoStockOD",
        "Ghetto05.Firearms.SKS.UASStock",
        "Ghetto05.Firearms.SKS.WoodenStock"
    ];

    private StringBuilder _errors;

    private void Start()
    {
        DebugLogConsole.AddCommand("banAttachmentFromValidation", "Ban an attachment", AddAttachmentToBanList);
        _attachmentIds = Catalog.GetDataList<AttachmentData>().Select(x => x.id).ToList();
        _errors = new StringBuilder();
        Test();
    }

    private async void Test()
    {
        await Task.Delay(5000);
        item.physicBody.isKinematic = true;
        item.DisallowDespawn = true;
        Debug.LogWarning($"Validation started!\nValidating {_attachmentIds.Count} attachments!");
        successLight.enabled = true;
        errorLight.enabled = false;
        var counter = 0;
        foreach (var id in _attachmentIds)
        {
            if (_attachmentIdBanList.Contains(id))
            {
                Debug.LogWarning($"Skipping blacklisted attachment {id}...({counter + 1} of {_attachmentIds.Count} - {Math.Round((double)counter / _attachmentIds.Count, 2) * 100}% done)");
                continue;
            }

            Debug.LogWarning($"Validating {id}...({counter + 1} of {_attachmentIds.Count} - {Math.Round((double)counter / _attachmentIds.Count, 2) * 100}% done)");
            var attachment = await SpawnAttachments(id);
            TestAttachment(attachment, id, counter);
            attachment?.Detach();
            counter++;
        }
        Debug.LogWarning($"Validation finished!\nValidated {_attachmentIds.Count} attachments!");
        PrintErrors();
    }

    private Task<Attachment> SpawnAttachments(string id)
    {
        var tcs = new TaskCompletionSource<Attachment>();
        Timeout(tcs);
        Catalog.GetData<AttachmentData>(id).SpawnAndAttach(slot, attachment =>
        {
            tcs.TrySetResult(attachment);
        });
        return tcs.Task;
    }

    private async void Timeout(TaskCompletionSource<Attachment> tcs)
    {
        await Task.Delay(1000);
        tcs.TrySetResult(null);
    }

    private void TestAttachment(Attachment attachment, string id, int counter)
    {
        var localErrors = new StringBuilder();
        var hasErrors = false;
        Util.PlayRandomAudioSource(clickSounds);
        localErrors.AppendLine($"Checking attachment {id}....({Math.Round((double)counter / _attachmentIds.Count, 2) * 100}% done)");

        if (attachment)
        {
            TestAttachmentPoints(attachment, ref hasErrors, localErrors);
            TestHandles(attachment, ref hasErrors, localErrors);
        }
        else
        {
            hasErrors = true;
            localErrors.AppendLine("    > There was an exception!");
            slot.currentAttachments.Clear();
            Destroy(slot.transform.GetChild(0).gameObject);
        }

        localErrors.AppendLine();

        if (hasErrors)
        {
            _errors.AppendLine(localErrors.ToString());
            successLight.enabled = false;
            errorLight.enabled = true;
        }
    }

    private void TestAttachmentPoints(Attachment attachment, ref bool hasErrors, StringBuilder localErrors)
    {
        for (var i = 0; i < attachment.attachmentPoints.Count; i++)
        {
            if (!attachment.attachmentPoints[i])
            {
                hasErrors = true;
                localErrors.AppendLine("    > There are NULL attachment points!");
            }
        }
    }

    private void TestHandles(Attachment attachment, ref bool hasErrors, StringBuilder localErrors)
    {
        for (var i = 0; i < attachment.handles.Count; i++)
        {
            if (!attachment.handles[i])
            {
                hasErrors = true;
                localErrors.AppendLine("    > There are NULL handles!");
            }
            else if (!attachment.GetComponentsInChildren<Handle>().All(x => attachment.handles.Contains(x)))
            {
                hasErrors = true;
                localErrors.AppendLine("    > There are handles that are not in the handle collection!");
            }
        }
    }

    private void PrintErrors()
    {
        File.WriteAllText(FileManager.GetFullPath(FileManager.Type.JSONCatalog, FileManager.Source.Mods, $"AttachmentValidation__{DateTime.Now.ToString("dd_MM_yyyy")}.txt"), _errors.ToString());
    }

    public static void AddAttachmentToBanList(string id)
    {
        _attachmentIdBanList.Add(id);
    }
}