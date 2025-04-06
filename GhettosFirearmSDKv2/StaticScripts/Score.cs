using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class Score : ThunderScript
{
    public static Score local;

    public float ShotsFired;
    public float ShotsHit;
    public float Headshots;

    public override void ScriptEnable()
    {
        local = this;
    }

    public void ShotFired(bool hit, bool headshot)
    {
        ShotsFired += 1;
        if (hit)
        {
            ShotsHit += 1;
        }
        if (headshot)
        {
            Headshots += 1;
        }
    }

    public float Accuracy
    {
        get
        {
            return ShotsHit / ShotsFired;
        }
    }

    public float HeadshotAccuracy
    {
        get
        {
            return Headshots / ShotsFired;
        }
    }

    public string AccuracyString
    {
        get
        {
            return $"{Accuracy * 100}%";
        }
    }

    public string HeadshotAccuracyString
    {
        get
        {
            return $"{HeadshotAccuracy * 100}%";
        }
    }
}