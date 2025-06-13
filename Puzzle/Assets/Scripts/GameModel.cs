using System;
using System.Collections.Generic;
using UnityEngine;

public static class Consts
{
    public const int CELL_WIDTH = 60; //�ѽL����e
    public static readonly int FIT_ALLOW = Mathf.FloorToInt(CELL_WIDTH / 4f); //���Ͼa��l���Z��(1/4�ѽL��)
    public static readonly float[] ADD_NEAR_RATE = new float[] { 1.0f, 1.0f, 1.0f, 0.7f, 0.5f, 0.3f }; //���ϥͦ��ɪ���N�������v
    public const int MAX_BOARD_LENGTH = 10; //�̤j�ѽL�j�p
    public const int MIN_BOARD_LENGTH = 3;  //�̤p�ѽL�j�p
    public const int MAX_SIZE_BUFF = 3;     //�p����W�[buff�h�ƭ���
    public const int MIN_SIZE_BUFF = -3;    //�j����W�[buff�h�ƭ���
    public const int MAX_OBSTACLE_BUFF = 4; //���O�ʮ�buff�h�ƭ���
    public const int MAX_ONECELL_BUFF = 5;  //1x1���buff�h�ƭ���
    public const int MAX_BUFF_NUM = 3;      //�̤j�ﶵ�ƶq
}

//Action�ƥ�
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

//�C�����A
public enum EGameState
{
    GS_BUFF,
    GS_START,
    GS_HELP,
    GS_CONTINUE
}

//buff����
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
    private int boardLength = Consts.MIN_BOARD_LENGTH; //�ѽL�j�p
    private int stage = 1; //���d
    private List<List<int>> board = new List<List<int>>();       //�ѽL��T(��l��-1��, �ϥή��J)
    private List<List<int[]>> puzzles = new List<List<int[]>>(); //���ϸ�T([��N�ӫ���][��N��CELL][��CELL��x,y])
    private int[] buffData = new int[] { 0, 0, 0, 0, 0, 0, 0, 2 }; //buff�Ҧ������P��l��
    private string queryString = ""; //query string
    private bool helpMode = false;   //�䴩�Ҧ�

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

// �����ҡ]�i�Q Controller �ϥΡ^
public static class GameModel
{
    public static CGameModel ins = new CGameModel();
}
