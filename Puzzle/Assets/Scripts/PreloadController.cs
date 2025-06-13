using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PreloadController : MonoBehaviour
{
    private void Start()
    {
        if (loadQueryString() == false){ //�M����}��query string
            loadCookie(); //�Lquery string�ͦ����d, Ūcookie
        }
    }

    public bool loadStage()
    {
        //�H���}��cookie�ͦ����d
        return false;
    }

    public bool loadQueryString()
    {
        //�P�_query string�U�ѼƦs�b
        return false;
    }

    //����query string
    public void setQueryString()
    {
        List<List<int>> board = GameModel.ins.getBoard().Select(v => new List<int>(v)).ToList();
        StringBuilder queryStr = new StringBuilder();
        queryStr.Append("b=").Append(board.Count); //���O�j�p
        queryStr.Append("&h=").Append(GameModel.ins.getBuff(EBuffType.BD_HUE));     //��tbuff
        queryStr.Append("&r=").Append(GameModel.ins.getBuff(EBuffType.BD_ROTATE));  //����buff
        queryStr.Append("&g=").Append(GameModel.ins.getBuff(EBuffType.BD_GRAVITY)); //���Obuff
        //�ʮ��m
        StringBuilder o = new StringBuilder();
        for (int i = 0; i < board.Count; i++){
            for (int j = 0; j < board[i].Count; j++){
                if (board[i][j] == int.MaxValue){
                    o.Append(i).Append(j); //xy�@��
                }
            }
        }
        //�L�ʮ�, �Ѽ�0
        if (o.Length == 0){
            queryStr.Append("&o=0");
        }
        else{
            queryStr.Append("&o=").Append(o.ToString());
        }
        //���ϧΪ�
        List<List<int[]>> puzzles = GameModel.ins.getPuzzles().Select(v0 => v0.Select(v1 => (int[])v1.Clone()).ToList()).ToList();
        for (int i = 0; i < puzzles.Count; i++){
            queryStr.Append("&p=");
            for (int j = 0; j < puzzles[i].Count; j++){
                queryStr.Append(puzzles[i][j][0]).Append(puzzles[i][j][1]); //xy�@��
            }
        }
        GameModel.ins.setQueryString(queryStr.ToString());
    }

    public void saveCookie()
    {
        //����cookie
    }

    public void loadCookie()
    {
        //Ū��cookie
    }
}
