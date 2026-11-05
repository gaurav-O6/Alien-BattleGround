using System;
using UnityEngine;

public static class GunshotAlert
{
    public static event Action<Vector3> OnGunshot;

    public static void ReportShot(Vector3 shotPosition)
    {
        OnGunshot?.Invoke(shotPosition);
    }
}