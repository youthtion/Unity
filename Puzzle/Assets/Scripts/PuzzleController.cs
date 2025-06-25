using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleController : MonoBehaviour
{
    private const float CAMERA_Z = -10.0f;

    [SerializeField]
    private GameObject cellPrefab = null;
    [Range(0f, 1f)]
    [SerializeField]
    private float cellSaturation = 0f;
    [Range(0f, 1f)]
    [SerializeField]
    private float cellLightness = 0f;

    private List<int[]> fitCells = new List<int[]>(); //���ϭ��|�ѽL��id
    private float[] placePuzzlePos = new float[2] { 0f, 0f }; //���ϭ��|�ѽL��ѽL�y��(���ϧl�����m)
    private GameObject dragPuzzle = null; //�����������
    private Vector3 dragStartPos = Vector3.zero; //������ϫe��Ц�m(�p�Ⲿ�ʶq)
    private Vector3 puzzleStartPos = Vector3.zero; //������ϫe���Ϧ�m(�p�Ⲿ�ʶq)
    private int puzzleLayer; //���ϥ���ܼh
    private float curZ = -0.01f; //�m��z�y��
    private bool eventActive = false; //�ƥ�}��
    private bool isDragging = false;

    private void Start()
    {
        puzzleLayer = LayerMask.NameToLayer("PUZZLE"); //�]�w���ϥ���ܼh
    }

    private void Update()
    {
        if (!eventActive){
            return;
        }
        //���ϧ��&����ƥ�, �ǤJ�I�������ϸ`�I
        if (Input.GetMouseButtonDown(0)){
            Vector3 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast((Vector2)mouse_pos, Vector2.zero);
            //�H������ܼh�P�_�O�_�I�������Ϫ���
            if (hit.collider != null && hit.collider.gameObject.layer == puzzleLayer){
                onMouseDown(mouse_pos, hit.collider.gameObject.transform.parent.gameObject);
            }
        }
        //���ϲ��ʨƥ�
        if (dragPuzzle && Input.GetMouseButton(0)){
            Vector3 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            onMouseMove(mouse_pos);
        }
        //���ϩ�U�ƥ�
        if (dragPuzzle && Input.GetMouseButtonUp(0)){
            onMouseUp();
        }
    }

    public void setEventsActive(bool _set)
    {
        eventActive = _set;
    }

    //�ͦ����Ϲ���
    public void initPuzzle()
    {
        if (cellPrefab == null){
            return;
        }
        List<List<int>> board = GameModel.ins.getBoard(); //�qGameModel���o�ѽL��T
        List<List<int[]>> puzzles = GameModel.ins.getPuzzles().Select(v0 => v0.Select(v1 => (int[])v1.Clone()).ToList()).ToList(); //�qGameModel���o���ϸ�T
        List<Color> colors = generateHueColors(puzzles.Count * (GameModel.ins.getBuff(EBuffType.BD_HUE) + 1));
        //�M�����ϸ`�I�U�Ҧ�����
        for (int i = transform.childCount - 1; i >= 0; i--){
            Destroy(transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < puzzles.Count; i++){
            GameObject puzzle = new GameObject(i.ToString()); //�ͦ����ϸ`�I, �W��i
            //���ϲĤ@��CELL���y��bx,by
            int bx = puzzles[i][0][0];
            int by = puzzles[i][0][1];
            for (int j = 0; j < puzzles[i].Count; j++){
                GameObject cell = Instantiate(cellPrefab, puzzle.transform); //�ͦ�CELL
                //��CELL���y��x,y
                int x = puzzles[i][j][0];
                int y = puzzles[i][j][1];
                cell.transform.localPosition = new Vector3((x - bx) * Consts.CELL_WIDTH, (y - by) * Consts.CELL_WIDTH, 0); //�UCELL��m����ֲĤ@�y��bx,by�Z��, �����Ͼ��鲾�ʨ�0,0����
                SpriteRenderer cell_sprite = cell.GetComponent<SpriteRenderer>();
                if (cell_sprite != null){
                    cell_sprite.color = colors[i]; //�ק��C��
                }
                cell.AddComponent<BoxCollider2D>();
            }
            //�]�w���ϸm��
            puzzle.transform.localPosition = new Vector3(
                board.Count * Consts.CELL_WIDTH / puzzles.Count * i - (board.Count - 1) * Consts.CELL_WIDTH * 0.5f ,
                -(board.Count + 1) * Consts.CELL_WIDTH,
                curZ);
            curZ -= 0.01f;
            puzzle.transform.SetParent(transform, false);
        }
    }

    //���ϧ��&����ƥ�
    public void onMouseDown(Vector3 _cur_pos, GameObject _puzzle)
    {
        if (_puzzle.transform.parent == null){
            return;
        }
        //���ϸm���W�L�۾�z�d��, �վ�Ҧ�����z
        if (curZ < CAMERA_Z){
            curZ = -0.01f;
            foreach (UnityEngine.Transform puzzle in transform){
                puzzle.transform.position = new Vector3(puzzle.transform.position.x, puzzle.transform.position.y, curZ);
                curZ -= 0.01f;
            }
        }
        getFitCell(_puzzle); //���o���ϭ��|�ѽL���T
        //���|�ѽL��P���Ϥj�p�@�P, �B�z���ϲ��X�ѽL
        if (fitCells.Count == _puzzle.transform.childCount){
            GameEvents.onPickPuzzle(fitCells, _puzzle.name);
        }
        //���������Ϭ���puzzle�}�l��m�]�w, ��Ц�m, ���ϸ`�I
        if (dragPuzzle == null){
            _puzzle.transform.position = new Vector3(_puzzle.transform.position.x, _puzzle.transform.position.y, curZ); //�]�w�I�����ϸm��
            puzzleStartPos = _puzzle.transform.position;
            dragStartPos = _cur_pos;
            dragPuzzle = _puzzle;
            isDragging = false;
        }
        curZ -= 0.01f;
    }

    public void onMouseMove(Vector3 _cur_pos)
    {
        if(isDragging == false && Vector2.Distance(dragStartPos, _cur_pos) > 10f){
            isDragging = true;
        }
        if(isDragging == true){
            //�p���в��ʶq, �]�wpuzzle�}�l��m�t
            float dx = dragStartPos.x - _cur_pos.x;
            float dy = dragStartPos.y - _cur_pos.y;
            dragPuzzle.transform.position = new Vector3(puzzleStartPos.x - dx, puzzleStartPos.y - dy, dragPuzzle.transform.position.z);
        }
    }

    public void onMouseUp()
    {
        //�I�����
        if (isDragging == false && GameModel.ins.getBuff(EBuffType.BD_ROTATE) == 0){
            Vector3 puzzle_pos = dragPuzzle.transform.position; //��Цb�e���W���y���ഫ�bPuzzleController�����y��
            Vector3 offset = dragStartPos - puzzle_pos; //�y�лPpuzzle��m�]�w�t
            float rad = Mathf.PI / -2f; //���੷��(-90��)
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            //�]�w�s��m�H��ЦbPuzzleController�����y�Ь����त��, ���������Ppuzzle��m�]�w�t
            float rx = offset.x * cos - offset.y * sin;
            float ry = offset.x * sin + offset.y * cos;
            dragPuzzle.transform.position = new Vector3(dragStartPos.x - rx, dragStartPos.y - ry, dragPuzzle.transform.position.z);
            Vector3 euler = dragPuzzle.transform.eulerAngles;
            euler.z = (euler.z - 90f) % 360f;
            dragPuzzle.transform.eulerAngles = euler;
        }
        getFitCell(dragPuzzle); //���o���ϭ��|�ѽL���T
        //���|�ѽL��P���Ϥj�p�@�P, �B�z���ϩ�J�ѽL
        if (fitCells.Count == dragPuzzle.transform.childCount){
            GameEvents.onPlacePuzzle(fitCells, dragPuzzle.name);
        }
        else{
            dragPuzzle = null;
        }
        isDragging = false;
    }

    public void getFitCell(GameObject _puzzle)
    {
        float dx = 0f;
        float dy = 0f;
        fitCells.Clear();
        if (_puzzle.transform.parent == null){
            return;
        }
        List<List<int>> board = GameModel.ins.getBoard(); //�qGameModel���o�ѽL��T
        for (int i = 0; i < _puzzle.transform.childCount; i++){
            //�UCELL�b�e���W���y���ഫ�bPuzzleController�����y��x,y
            UnityEngine.Transform cell = _puzzle.transform.GetChild(i);
            Vector3 cur_pos = cell.position;
            Vector3 pos_in_parent = _puzzle.transform.parent.InverseTransformPoint(cur_pos);
            float x = pos_in_parent.x;
            float y = pos_in_parent.y;
            //�e���ѽL�y�Ь�����0, �W�U���k�U�@�b�ѽL����, �P�_x,y�O�_�b�ѽL�d��+�l���d��(FIT_ALLOW)��
            if (Mathf.Abs(x) > Consts.CELL_WIDTH * (board.Count - 1) / 2f + Consts.FIT_ALLOW || Mathf.Abs(y) > Consts.CELL_WIDTH * (board.Count - 1) / 2f + Consts.FIT_ALLOW){
                break;
            }
            //�p��CELL�y�ЬO�_�b�ѽL��l���d��, ���ƪ��ת��ѽL�����D�ѽL��վ�x,y
            if (board.Count % 2 == 0){
                x += Consts.CELL_WIDTH * 0.5f;
                y += Consts.CELL_WIDTH * 0.5f;
            }
            //CELL�y�а��H�ѽL��, �P�_�l�ƬO�_�b�@���檺+-�l���d��
            if (Mathf.Abs(x % Consts.CELL_WIDTH) > Consts.FIT_ALLOW && Mathf.Abs(x % Consts.CELL_WIDTH) < Consts.CELL_WIDTH - Consts.FIT_ALLOW){
                break;
            }
            if (Mathf.Abs(y % Consts.CELL_WIDTH) > Consts.FIT_ALLOW && Mathf.Abs(y % Consts.CELL_WIDTH) < Consts.CELL_WIDTH - Consts.FIT_ALLOW){
                break;
            }
            //�T�{CELL�b�ѽL�d�򤺥B�b�ѽL��l���d��, �p���ڰ��t�q
            if (dx == 0f && dy == 0f){
                dx = (x % Consts.CELL_WIDTH + Consts.CELL_WIDTH) % Consts.CELL_WIDTH;
                if (dx > Consts.FIT_ALLOW){
                    dx -= Consts.CELL_WIDTH;
                }
                dy = (y % Consts.CELL_WIDTH + Consts.CELL_WIDTH) % Consts.CELL_WIDTH;
                if (dy > Consts.FIT_ALLOW){
                    dy -= Consts.CELL_WIDTH;
                }
                //����puzzle��m�]�w�P�������t�q�᪺��m(�l���᪺��m)
                Vector3 puzzle_pos = _puzzle.transform.localPosition;
                placePuzzlePos[0] = puzzle_pos.x - dx;
                placePuzzlePos[1] = puzzle_pos.y - dy;
            }
            //�A�Nx,y�����l�����t�վ㬰�k�X�ѽL�檺�y��, ��K�p����ݴѽL��id
            x -= dx;
            y -= dy;
            //�y���ഫ���ѽL��id, �]��@�}�Cid�Υ|�ˤ��J����B�I�ƻ~�t
            fitCells.Add(new int[2] { Mathf.RoundToInt(x / Consts.CELL_WIDTH + Mathf.Floor((board.Count - 1) / 2f)), Mathf.RoundToInt(y / Consts.CELL_WIDTH + Mathf.Floor((board.Count - 1) / 2f)) });
        }
    }

    //���ϩ�W�ѽL�ƥ�B�z�������^��
    public void onPlacePuzzleCallback(int? _dy)
    {
        if (dragPuzzle && _dy != null){
            //�P�w���\�վ㬰�l�����m
            dragPuzzle.transform.localPosition = new Vector3(placePuzzlePos[0], placePuzzlePos[1] - _dy.Value * Consts.CELL_WIDTH, -0.01f);
            placePuzzlePos[0] = 0;
            placePuzzlePos[1] = 0;
        }
        dragPuzzle = null;
    }

    //HSL��RGB����
    public Color hslToRgb(float _h, float _s, float _l)
    {
        float r, g, b;
        if (_s == 0f){
            r = g = b = _l;
        }
        else{
            float hue2Rgb(float _p, float _q, float _t){
                if (_t < 0f) _t += 1f;
                if (_t > 1f) _t -= 1f;
                if (_t < 1f / 6f) return _p + (_q - _p) * 6f * _t;
                if (_t < 1f / 2f) return _q;
                if (_t < 2f / 3f) return _p + (_q - _p) * (2f / 3f - _t) * 6f;
                return _p;
            }
            float q = _l < 0.5f ? _l * (1f + _s) : _l + _s - _l * _s;
            float p = 2f * _l - q;
            r = hue2Rgb(p, q, _h + 1f / 3f);
            g = hue2Rgb(p, q, _h);
            b = hue2Rgb(p, q, _h - 1f / 3f);
        }
        return new Color(r, g, b);
    }

    //�ͦ�_n�Ӧ�۵��t���C��
    public List<Color> generateHueColors(int _n)
    {
        List<Color> colors = new List<Color>();
        for (int i = 0; i < _n; i++){
            float h = (float)i / _n;
            Color rgb = hslToRgb(h, cellSaturation, cellLightness);
            colors.Add(rgb);
        }
        return colors;
    }
}
