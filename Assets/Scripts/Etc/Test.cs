using UnityEngine;
using WildTamer;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public class Test : MonoBehaviour
{
    [SerializeField] private QuarterViewCamera _quarterViewCamera;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private FlockManager _flockManager;
    int i = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CameraShake()
    {
        _quarterViewCamera.CameraShake(1f, 0.1f);
    }
    public void Save()
    {
        _saveManager.Save();
    }
    public void Load()
    {
        _saveManager.Load();
    }   
    public void ChangeFormation()
    {
        _flockManager.NextFormation();
    }
}