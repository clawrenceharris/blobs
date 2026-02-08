using UnityEngine.UI;
using UnityEngine;
namespace Blobs.Core.UI{

public class PausePanelView : PanelView

{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _retryPauseButton;
    [SerializeField] private Button _menuPauseButton;


    public Button ResumeButton => _resumeButton;
    public Button RetryButton => _retryPauseButton;
    public Button MenuButton => _menuPauseButton;



}
}