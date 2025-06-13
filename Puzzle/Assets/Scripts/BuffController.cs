using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

//����buff����, id���buff�϶����s��
public enum EBuffUIType
{
    BU_GROW_STOP,
    BU_GROW_DEC,
    BU_SMALLER_PUZZLE,
    BU_BIGGER_PUZZLE,
    BU_LESS_HUE,
    BU_MORE_HUE,
    BU_ADD_OBSTACLE,
    BU_DEC_OBSTACLE,
    BU_GET_1X1_PUZZLE,
    BU_LOSE_1X1_PUZZLE,
    BU_CANNOT_ROTATE,
    BU_LESS_BUFF, BU_BUFF_START = BU_LESS_BUFF,
    BU_ONE_BUFF,
    BU_TWO_BUFF,
    BU_THREE_BUFF,
    BU_MORE_BUFF,
    BU_REFRESH,
    BU_GRAVITY,
    BU_BUOYANCY,
    BU_PASS,
}

public static class BuffText
{
    //����buff��ܤ�r, ���EBuffUIType
    private static readonly Dictionary<EBuffUIType, string> textMap = new Dictionary<EBuffUIType, string>{
        { EBuffUIType.BU_GROW_STOP, "���O���X�j(1��)" },
        { EBuffUIType.BU_GROW_DEC, "���O�Y�p" },
        { EBuffUIType.BU_SMALLER_PUZZLE, "�p����ܦh" },
        { EBuffUIType.BU_BIGGER_PUZZLE, "�j����ܦh" },
        { EBuffUIType.BU_LESS_HUE, "�����t�p" },
        { EBuffUIType.BU_MORE_HUE, "�����t�j" },
        { EBuffUIType.BU_ADD_OBSTACLE, "���O�ʮ�W�[" },
        { EBuffUIType.BU_DEC_OBSTACLE, "���O�ʮ���" },
        { EBuffUIType.BU_GET_1X1_PUZZLE, "��o1x1���" },
        { EBuffUIType.BU_LOSE_1X1_PUZZLE, "���h1x1���\n�X�{���O�Y�p" },
        { EBuffUIType.BU_CANNOT_ROTATE, "���i����(1��)\n�X�{���O�Y�p" },
        { EBuffUIType.BU_LESS_BUFF, "�ﶵ���\n��o��s����" },
        { EBuffUIType.BU_ONE_BUFF, "" },
        { EBuffUIType.BU_TWO_BUFF, "" },
        { EBuffUIType.BU_THREE_BUFF, "" },
        { EBuffUIType.BU_MORE_BUFF, "�ﶵ�W�[" },
        { EBuffUIType.BU_REFRESH, "��s�ﶵ" },
        { EBuffUIType.BU_GRAVITY, "���ͭ��O(1��)" },
        { EBuffUIType.BU_BUOYANCY, "���ͯB�O(1��)" },
        { EBuffUIType.BU_PASS, "���L" },
    };

    public static string get(EBuffUIType _type)
    {
        if (textMap.ContainsKey(_type))
            return textMap[_type];
        return string.Empty;
    }
}

public class BuffController : MonoBehaviour
{
    [SerializeField]
    private GameObject refreshBtn = null;
    [SerializeField]
    private TextMeshProUGUI freshLabel = null;
    [SerializeField]
    private GameObject buffBtn1 = null;
    [SerializeField]
    private GameObject buffBtn2 = null;
    [SerializeField]
    private GameObject buffBtn3 = null;
    [SerializeField]
    private TextMeshProUGUI buffLabel1 = null;
    [SerializeField]
    private TextMeshProUGUI buffLabel2 = null;
    [SerializeField]
    private TextMeshProUGUI buffLabel3 = null;
    [SerializeField]
    private Image buffSprite1 = null;
    [SerializeField]
    private Image buffSprite2 = null;
    [SerializeField]
    private Image buffSprite3 = null;
    [SerializeField]
    private int buffSpace = 0;
    [SerializeField]
    private SpriteAtlas buffAtlas = null;

    private GameObject[] buffBtns;
    private TextMeshProUGUI[] buffLabels;
    private Image[] buffSprites;
    private List<EBuffUIType> buffList = new List<EBuffUIType> { }; //�ŦX�[�J���H���C��buff

    private void Start()
    {
        buffBtns = new GameObject[] { buffBtn1, buffBtn2, buffBtn3 };
        buffLabels = new TextMeshProUGUI[] { buffLabel1, buffLabel2, buffLabel3 };
        buffSprites = new Image[] { buffSprite1, buffSprite2, buffSprite3 };
    }

    public void showBuffBtn()
    {
        for (int i = 0; i < buffBtns.Length; i++){
            //�ھڿﶵbuff�q���buff���s
            if (GameModel.ins.getBuff(EBuffType.BD_BUFFNUM) > i && i < buffList.Count){
                buffBtns[i].SetActive(true);
                buffBtns[i].transform.localPosition = new Vector3(buffSpace * (GameModel.ins.getBuff(EBuffType.BD_BUFFNUM) - 1) * (-0.5f) + i * buffSpace, 0, 0);
                buffLabels[i].text = BuffText.get(buffList[i]);
                int atlas_id = (int)buffList[i];
                //�ܧ�ﶵ�ƶq���ϥܮھڿﶵ�ƶq���
                if (buffList[i] == EBuffUIType.BU_LESS_BUFF){
                    atlas_id = (int)EBuffUIType.BU_BUFF_START + (GameModel.ins.getBuff(EBuffType.BD_BUFFNUM) - 1);
                }
                else if (buffList[i] == EBuffUIType.BU_MORE_BUFF){
                    atlas_id = (int)EBuffUIType.BU_BUFF_START + (GameModel.ins.getBuff(EBuffType.BD_BUFFNUM) + 1);
                }
                Sprite sprite = buffAtlas.GetSprite(atlas_id.ToString());
                if (sprite != null){
                    buffSprites[i].sprite = sprite;
                }
            }
            else{
                buffBtns[i].SetActive(false);
            }
        }
        //�ھګ�����sbuff����ܨ�sbuff���s�P����
        if (refreshBtn != null){
            refreshBtn.SetActive(GameModel.ins.getBuff(EBuffType.BD_REFRESH) > 0);
            if (freshLabel != null){
                freshLabel.text = GameModel.ins.getBuff(EBuffType.BD_REFRESH).ToString();
            }
        }
    }

    //��ܲ�N��buff���s
    public void onBuffClick(int _id)
    {
        setBuff(_id);
    }

    //��sbuff
    public void onRefreshClick()
    {
        GameModel.ins.decreaseBuff(EBuffType.BD_REFRESH);
        generateBuffList();
        showBuffBtn();
    }

    //���ϥͦ��ɪ���N�������v(�j�p���buff�վ�)
    public float[] getBuffAddNearRate()
    {
        float[] add_near_rate = (float[])Consts.ADD_NEAR_RATE.Clone();
        //�j�p���buff�̼h�ƽվ㭱�n4�H�W������ͦ����v
        for (int i = 3; i < add_near_rate.Length; i++){
            add_near_rate[i] += GameModel.ins.getBuff(EBuffType.BD_SIZE) * 0.1f;
            if (add_near_rate[i] > 1.0f){
                add_near_rate[i] = 1.0f;
            }
            else if (add_near_rate[i] < 0){
                add_near_rate[i] = 0;
            }
        }
        return add_near_rate;
    }

    //��ܫ��wbuff
    public void setBuff(int _id)
    {
        if (_id < 0 || _id >= buffList.Count)
        {
            return;
        }
        EBuffUIType type = buffList[_id];
        switch (type)
        {
            case EBuffUIType.BU_GROW_DEC:
                if (GameModel.ins.getBoardLength() > Consts.MIN_BOARD_LENGTH)
                {
                    GameModel.ins.decreaseBoardLength();
                }
                break;
            case EBuffUIType.BU_SMALLER_PUZZLE:
                GameModel.ins.decreaseBuff(EBuffType.BD_SIZE);
                break;
            case EBuffUIType.BU_BIGGER_PUZZLE:
                GameModel.ins.addBuff(EBuffType.BD_SIZE);
                break;
            case EBuffUIType.BU_LESS_HUE:
                GameModel.ins.addBuff(EBuffType.BD_HUE);
                break;
            case EBuffUIType.BU_MORE_HUE:
                GameModel.ins.decreaseBuff(EBuffType.BD_HUE);
                break;
            case EBuffUIType.BU_ADD_OBSTACLE:
                GameModel.ins.addBuff(EBuffType.BD_OBSTACLE);
                break;
            case EBuffUIType.BU_DEC_OBSTACLE:
                GameModel.ins.decreaseBuff(EBuffType.BD_OBSTACLE);
                break;
            case EBuffUIType.BU_GET_1X1_PUZZLE:
                GameModel.ins.addBuff(EBuffType.BD_ONECELL);
                break;
            case EBuffUIType.BU_LOSE_1X1_PUZZLE:
                GameModel.ins.decreaseBuff(EBuffType.BD_ONECELL);
                buffList[_id] = EBuffUIType.BU_GROW_DEC;
                showBuffBtn();
                return;
            case EBuffUIType.BU_CANNOT_ROTATE:
                GameModel.ins.setBuff(EBuffType.BD_ROTATE, 1);
                buffList[_id] = EBuffUIType.BU_GROW_DEC;
                showBuffBtn();
                return;
            case EBuffUIType.BU_LESS_BUFF:
                GameModel.ins.decreaseBuff(EBuffType.BD_BUFFNUM);
                GameModel.ins.addBuff(EBuffType.BD_REFRESH);
                buffList.RemoveAt(_id);
                showBuffBtn();
                return;
            case EBuffUIType.BU_MORE_BUFF:
                GameModel.ins.addBuff(EBuffType.BD_BUFFNUM);
                EBuffUIType temp = buffList[_id];
                buffList[_id] = buffList[buffList.Count - 1];
                buffList[buffList.Count - 1] = temp;
                showBuffBtn();
                return;
            case EBuffUIType.BU_REFRESH:
                generateBuffList();
                showBuffBtn();
                return;
            case EBuffUIType.BU_GRAVITY:
                GameModel.ins.setBuff(EBuffType.BD_GRAVITY, 1);
                break;
            case EBuffUIType.BU_BUOYANCY:
                GameModel.ins.setBuff(EBuffType.BD_GRAVITY, -1);
                break;
        }
        //���O����buff�b�D��ܩ��O����buff��T�w����1
        if (type > EBuffUIType.BU_GROW_DEC && GameModel.ins.getBoardLength() < Consts.MAX_BOARD_LENGTH){
            GameModel.ins.addBoardLength();
        }
        //�B�z��Ĳ�o�U���C���޿�
        GameEvents.onSetBuffFinish();
    }

    public void generateBuffList()
    {
        //�P�w�i�[�J�H���C��buff
        buffList.Clear();
        if (GameModel.ins.getBoardLength() < Consts.MAX_BOARD_LENGTH){
            buffList.Add(EBuffUIType.BU_GROW_STOP);
        }
        if (GameModel.ins.getBuff(EBuffType.BD_SIZE) > Consts.MIN_SIZE_BUFF){
            buffList.Add(EBuffUIType.BU_SMALLER_PUZZLE);
        }
        if (GameModel.ins.getBuff(EBuffType.BD_SIZE) < Consts.MAX_SIZE_BUFF){
            buffList.Add(EBuffUIType.BU_BIGGER_PUZZLE);
        }
        buffList.Add(EBuffUIType.BU_LESS_HUE);
        if (GameModel.ins.getBuff(EBuffType.BD_HUE) > 0){
            buffList.Add(EBuffUIType.BU_MORE_HUE);
        }
        if (GameModel.ins.getBuff(EBuffType.BD_OBSTACLE) < Consts.MAX_OBSTACLE_BUFF){
            buffList.Add(EBuffUIType.BU_ADD_OBSTACLE);
        }
        if (GameModel.ins.getBuff(EBuffType.BD_OBSTACLE) > 0){
            buffList.Add(EBuffUIType.BU_DEC_OBSTACLE);
        }
        if (GameModel.ins.getBuff(EBuffType.BD_ONECELL) < Consts.MAX_ONECELL_BUFF){
            buffList.Add(EBuffUIType.BU_GET_1X1_PUZZLE);
        }
        if (GameModel.ins.getBoardLength() > Consts.MIN_BOARD_LENGTH){
            if (GameModel.ins.getBuff(EBuffType.BD_ONECELL) > 0){
                buffList.Add(EBuffUIType.BU_LOSE_1X1_PUZZLE);
            }
            if (GameModel.ins.getBuff(EBuffType.BD_ROTATE) == 0){
                buffList.Add(EBuffUIType.BU_CANNOT_ROTATE);
            }
        }
        if (GameModel.ins.getBuff(EBuffType.BD_BUFFNUM) > 1){
            buffList.Add(EBuffUIType.BU_LESS_BUFF);
        }
        if (GameModel.ins.getBuff(EBuffType.BD_BUFFNUM) < Consts.MAX_BUFF_NUM){
            buffList.Add(EBuffUIType.BU_MORE_BUFF);
        }
        buffList.Add(EBuffUIType.BU_REFRESH);
        if (GameModel.ins.getBuff(EBuffType.BD_GRAVITY) == 0){
            buffList.Add(EBuffUIType.BU_GRAVITY);
            buffList.Add(EBuffUIType.BU_BUOYANCY);
        }
        buffList.Add(EBuffUIType.BU_PASS);
        //�[�J���N�C���H���Ƨ�
        for (int i = 0; i < buffList.Count; i++){
            int rand = UnityEngine.Random.Range(0, buffList.Count - i) + i;
            EBuffUIType temp = buffList[i];
            buffList[i] = buffList[rand];
            buffList[rand] = temp;
        }
    }
}
