using KydlCore.Common;
using UnityEngine;

namespace WSADMounts.Testing.TestHooks;

/// <summary>
/// TEST ONLY (user 2026-11-25 12:58: "add a side camera to look at how they moving backwards"): a second camera, drawn over the game camera, that
/// follows the test mount from its right side every frame, so the person watching a lab run sees the legs while the mount walks backwards.
/// Made by hook WSADMounts.sideCam, removed by WSADMounts.sideCamOff and WSADMounts.rideReset. Same idea as GunPowdered's free camera
/// (Camera.CopyFrom(main), depth + 1).
/// </summary>
internal sealed class SideCamera : MonoBehaviour
{
    internal const string ObjectName = "WSADMounts_sideCam";

    private Transform _target;
    private float _metres;

    internal static SideCamera Show(Transform target, float metres)
    {
        Camera main = Camera.main;
        GameObject go = GameObject.Find(ObjectName) ?? new GameObject(ObjectName);
        Camera cam = go.GetComponent<Camera>() ?? go.AddComponent<Camera>();
        if (main != null)
        {
            cam.CopyFrom(main);
            cam.depth = main.depth + 1f;
        }

        SideCamera side = go.GetComponent<SideCamera>() ?? go.AddComponent<SideCamera>();
        side._target = target;
        side._metres = metres;
        side.LateUpdate();
        ModLog.Info($"SideCamera: on, {metres:0.#} m to the right of {(target != null ? target.name : "<none>")} (main camera {main != null})");
        return side;
    }

    internal static bool Hide(string why)
    {
        GameObject go = GameObject.Find(ObjectName);
        if (go != null)
        {
            Destroy(go);
        }

        ModLog.Info($"SideCamera: off ({why}, was there {go != null})");
        return go != null;
    }

    private void LateUpdate()
    {
        if (_target != null)
        {
            Vector3 centre = _target.position + (Vector3.up * 1.2f);
            transform.position = centre + (_target.right * _metres) + (Vector3.up * 0.8f);
            transform.LookAt(centre);
            ModLog.Every("SideCamera.LateUpdate", 2d, $"SideCamera: at {transform.position} looking at {_target.name}");
        }
    }
}
