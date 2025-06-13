using UnityEngine;

public class ScreenshotController : MonoBehaviour
{
    [SerializeField]
    private Camera screenshotCamera; //只照GAME層的相機(無UI)

    public void onScreenshotClick()
    {
        //Unity不可複製圖片到剪貼簿
    }

    public void toBlobCallBack()
    {
        //Unity不可複製圖片到剪貼簿
    }
}
