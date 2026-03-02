using UnityEngine;
using WildTamer;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public class Test : MonoBehaviour
{
    [SerializeField] private QuarterViewCamera _quarterViewCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CameraShake()
    {
        _quarterViewCamera.CameraShake(1f, 0.1f);
    }
}