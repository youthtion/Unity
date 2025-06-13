using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PreloadController : MonoBehaviour
{
    private void Start()
    {
        if (loadQueryString() == false){ //尋找網址後query string
            loadCookie(); //無query string生成關卡, 讀cookie
        }
    }

    public bool loadStage()
    {
        //以網址或cookie生成關卡
        return false;
    }

    public bool loadQueryString()
    {
        //判斷query string各參數存在
        return false;
    }

    //產生query string
    public void setQueryString()
    {
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList();
        StringBuilder queryStr = new StringBuilder();
        queryStr.Append("b=").Append(board.Count); //底板大小
        queryStr.Append("&h=").Append(GameModel.ins.getBuff(EBuffType.BD_HUE));     //色差buff
        queryStr.Append("&r=").Append(GameModel.ins.getBuff(EBuffType.BD_ROTATE));  //旋轉buff
        queryStr.Append("&g=").Append(GameModel.ins.getBuff(EBuffType.BD_GRAVITY)); //重力buff
        //缺格位置
        StringBuilder o = new StringBuilder();
        for (int i = 0; i < board.Count; i++){
            for (int j = 0; j < board[i].Count; j++){
                if (board[i][j] == int.MaxValue){
                    o.Append(i).Append(j); //xy一組
                }
            }
        }
        //無缺格, 參數0
        if (o.Length == 0){
            queryStr.Append("&o=0");
        }
        else{
            queryStr.Append("&o=").Append(o.ToString());
        }
        //拼圖形狀
        List<List<int[]>> puzzles = GameModel.ins.getPuzzles().Select(v0 => v0.Select(v1 => (int[])v1.Clone()).ToList()).ToList();
        for (int i = 0; i < puzzles.Count; i++){
            queryStr.Append("&p=");
            for (int j = 0; j < puzzles[i].Count; j++){
                queryStr.Append(puzzles[i][j][0]).Append(puzzles[i][j][1]); //xy一組
            }
        }
        GameModel.ins.setQueryString(queryStr.ToString());
    }

    public void saveCookie()
    {
        //產生cookie
    }

    public void loadCookie()
    {
        //讀取cookie
    }
}
