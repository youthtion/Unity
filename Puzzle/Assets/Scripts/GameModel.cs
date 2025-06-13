using System;
using System.Collections.Generic;
using UnityEngine;

public static class Consts
{
    public const int CELL_WIDTH = 60; //棋盤格長寬
    public static readonly int FIT_ALLOW = Mathf.FloorToInt(CELL_WIDTH / 4f); //拼圖靠近吸附距離(1/4棋盤格)
    public static readonly float[] ADD_NEAR_RATE = new float[] { 1.0f, 1.0f, 1.0f, 0.7f, 0.5f, 0.3f }; //拼圖生成時長為N塊的機率
    public const int MAX_BOARD_LENGTH = 10; //最大棋盤大小
    public const int MIN_BOARD_LENGTH = 3;  //最小棋盤大小
    public const int MAX_SIZE_BUFF = 3;     //小方塊增加buff層數限制
    public const int MIN_SIZE_BUFF = -3;    //大方塊增加buff層數限制
    public const int MAX_OBSTACLE_BUFF = 4; //底板缺格buff層數限制
    public const int MAX_ONECELL_BUFF = 5;  //1x1方塊buff層數限制
    public const int MAX_BUFF_NUM = 3;      //最大選項數量
}

//Action事件
public static class GameEvents
{
    public static event Action addBuff;
    public static event Action<List<int[]>, string> pickPuzzle;
    public static event Action<List<int[]>, string> placePuzzle;
    public static event Action<HashSet<int>, EGameState> preloaded;

    public static void onSetBuffFinish() => addBuff?.Invoke();
    public static void onPickPuzzle(List<int[]> _fit_cell, string _name) => pickPuzzle?.Invoke(_fit_cell, _name);
    public static void onPlacePuzzle(List<int[]> _fit_cell, string _name) => placePuzzle?.Invoke(_fit_cell, _name);
    public static void onFoundPreloadData(HashSet<int> _obstacle_set, EGameState _state) => preloaded?.Invoke(_obstacle_set, _state);
}

//遊戲狀態
public enum EGameState
{
    GS_BUFF,
    GS_START,
    GS_HELP,
    GS_CONTINUE
}

//buff種類
public enum EBuffType
{
    BD_SIZE,
    BD_HUE,
    BD_ROTATE,
    BD_OBSTACLE,
    BD_ONECELL,
    BD_GRAVITY,
    BD_REFRESH,
    BD_BUFFNUM,
    BD_MAX
}

public class CGameModel
{
    private int boardLength = Consts.MIN_BOARD_LENGTH; //棋盤大小
    private int stage = 1; //關卡
    private List<List<int>> board = new List<List<int>>();       //棋盤資訊(初始為-1填滿, 使用格填入)
    private List<List<int[]>> puzzles = new List<List<int[]>>(); //拼圖資訊([第N個拼圖][第N個CELL][該CELL的x,y])
    private int[] buffData = new int[] { 0, 0, 0, 0, 0, 0, 0, 2 }; //buff所有種類與初始值
    private string queryString = ""; //query string
    private bool helpMode = false;   //支援模式

    public void addBoardLength() => boardLength++;
    public void decreaseBoardLength() => boardLength--;
    public void setBoardLength(int _set) => boardLength = _set;
    public void addStage() => stage++;
    public void setStage(int _set) => stage = _set;
    public void setBoard(List<List<int>> _set)
    {
        board = new List<List<int>>(_set.Count);
        foreach (var v in _set){
            board.Add(new List<int>(v));
        }
    }
    public void setPuzzles(List<List<int[]>> _set)
    {
        puzzles = new List<List<int[]>>(_set.Count);
        foreach (var v0 in _set){
            List<int[]> line = new List<int[]>(v0.Count);
            foreach (var v1 in v0){
                int[] v2 = new int[2] { v1[0], v1[1] };
                line.Add(v2);
            }
            puzzles.Add(line);
        }
    }
    public void addBuff(EBuffType _type) => buffData[(int)_type]++;
    public void decreaseBuff(EBuffType _type) => buffData[(int)_type]--;
    public void removeBuff(EBuffType _type) => buffData[(int)_type] = 0;
    public void setBuff(EBuffType _type, int _set) => buffData[(int)_type] = _set;
    public void setQueryString(string _set) => queryString = _set;
    public void setHelpMode(bool _set) => helpMode = _set;

    public int getBoardLength() => boardLength;
    public int getStage() => stage;
    public List<List<int>> getBoard() => board;
    public List<List<int[]>> getPuzzles() => puzzles;
    public int getBuff(EBuffType _type) => buffData[(int)_type];
    public string getQueryString() => queryString;
    public bool getHelpMode() => helpMode;
}

// 全域單例（可被 Controller 使用）
public static class GameModel
{
    public static CGameModel ins = new CGameModel();
}
