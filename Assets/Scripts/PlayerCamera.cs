using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float MouseSensitivity = 5f;
    public float CameraDistance = 5.5f;

    public float CameraCollisionBuffer = 0.2f;
    public float CameraKickAmount = 2f;
    public float CameraKickReturnSpeed = 20f;

    float VerticalRotation = 0f;
    float CameraKick = 0f;

    public Transform CameraTarget;
    public LayerMask CameraCollisionMask;

    Transform MainCamera;
    Vector3 DefaultCameraLocalPosition;

    void Start()
    {
        MainCamera = Camera.main.transform;

        DefaultCameraLocalPosition = MainCamera.localPosition;
    }

    void Update()
    {
        float MouseX = Input.GetAxis("Mouse X");
        float MouseY = Input.GetAxis("Mouse Y");

        CameraKick = Mathf.MoveTowards(CameraKick, 0f, CameraKickReturnSpeed * Time.deltaTime);

        VerticalRotation -= MouseY * MouseSensitivity;
        VerticalRotation = Mathf.Clamp(VerticalRotation, -15f, 35f);

        transform.Rotate(Vector3.up * MouseX * MouseSensitivity);

        MainCamera.localRotation = Quaternion.Euler(VerticalRotation - CameraKick, 0f, 0f);

        Vector3 DesiredCameraPosition = transform.TransformPoint(DefaultCameraLocalPosition);

        Vector3 Direction = DesiredCameraPosition - CameraTarget.position;
        float Distance = Direction.magnitude;

        if (Distance > 0f)
        {
            Direction.Normalize();

            RaycastHit Hit;

            if (Physics.SphereCast(
                CameraTarget.position,
                CameraCollisionBuffer,
                Direction,
                out Hit,
                Distance,
                CameraCollisionMask))
            {
                float SafeDistance = Hit.distance - CameraCollisionBuffer;

                if (SafeDistance < 0.8f)
                {
                    SafeDistance = 0.5f;
                }

                MainCamera.position =
                    CameraTarget.position + Direction * SafeDistance;
            }
            else
            {
                MainCamera.position = DesiredCameraPosition;
            }
        }
    }
    public void Kick()
    {
        CameraKick = CameraKickAmount;
    }

}