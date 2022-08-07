using UnityEngine;

public interface IPanel
{
    public GameObject Content { get; set; }
    public bool IsDisplayed { get; }
    public void Display(bool value);
    public void ToggleDisplay();
}
