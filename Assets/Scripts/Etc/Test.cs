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
        i = (i + 1) % 5;
        if (i == 0)            _flockManager.SetFormationType(FormationType.Circle);
        else if (i == 1)
            _flockManager.SetFormationType(FormationType.Square);
        else if (i == 2)
            _flockManager.SetFormationType(FormationType.Column);
        else if (i == 3)
            _flockManager.SetFormationType(FormationType.Wedge);
        else if (i == 4)
        _flockManager.SetFormationType(FormationType.Line);
    }
}