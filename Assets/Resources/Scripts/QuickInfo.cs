using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuickInfo : MonoBehaviour
{
    public float showDuration = 2;
    public GameObject quickinfoPanel;
    public Text txtQuickInfo;

    private bool quickInfoIsActive = false;




    public void init()
    {
        StopAllCoroutines();

        quickinfoPanel.SetActive(false);
    }
    


    public void showQuickInfo(string textInfo, float tmpShowDuration = 0)
    {
        StartCoroutine(quickinfoCoroutine(textInfo, tmpShowDuration));
    }

    private IEnumerator quickinfoCoroutine(string textInfo, float tmpShowDuration)
    {
        while (quickInfoIsActive)
            yield return null;
        
        quickinfoPanel.SetActive(true);
        txtQuickInfo.text = textInfo;
        if(tmpShowDuration > 0)
            yield return new WaitForSeconds(tmpShowDuration);
        else
            yield return new WaitForSeconds(showDuration);

        quickinfoPanel.SetActive(false);
    }

}
