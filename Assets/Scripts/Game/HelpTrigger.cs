using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpTrigger : MonoBehaviour
{
    [TextArea(2, 10)]
    public string helpText;

    public void TriggerEnter()
    {
        GameManager.instance.PlayHelpTriggerAudio();
        GameUIManager.instance.SetCenterText(helpText);
    }
}
