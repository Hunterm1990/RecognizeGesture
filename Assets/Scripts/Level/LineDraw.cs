using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LineDraw : MonoBehaviour
{
    public GameObject lineDrawPrefabs; // Prefab объект линии компонента line renderer 
    public bool EditMode = false; // переменная включения/отключения Edit Mode
    public float TimeToRound = 25f, SpeedDownCoeff = 0.98f; // начальное значение отсчета времени и коэф. уменьшения его 
    public int CurScore = 0, BestScore = 0; // счетчики текущего и лучшего счета

    public Text scoreText; // ссылка на UI текста с текущем счетом
    public Text highScoreText; // ссылка на UI текста с лучшим счетом
    public Text timeText; // ссылка на UI текста для отсчета времени

    public Text scoreGO; // ссылка на UI текста с текущем счетом при проигрыше
    public Text highScoreGO; // ссылка на UI текста с лучшим счетом при проигрыше

    public GameObject GameUI, GameOverUI, CursorPS; // ссылки на игровые объекты с UI для игры, проигрыше и системы частиц

    private bool isMousePressed = false; // проверка нажата кнопка мыши
    private GameObject lineDrawPrefab, ShowFigure; // глобальный объект рисуемой и фигуры для задания
    private LineRenderer lineRenderer, ShowFigureLine; // переменная для доступа к компоненту line renderer для рисуемой и по заданию фигуры 
    private List<Vector3> drawPoints = new List<Vector3>(); // список опорных точек нарисованной фигуры

    // структура переменных для фигур (вершины, id, углы)
    struct TFigure
    {
        public Vector3[] Nodes;
        public int ID;
        public float[] Angles;
    }

    // массив хранения фигур
    private List<TFigure> Figures = new List<TFigure>();

    // Use this for initialization
    void Start()
    {
        GameUI.SetActive(true);
        GameOverUI.SetActive(false);

        // Проверка на наличие файла с лучшим результатом
        if (PlayerPrefs.HasKey("HighScore"))
        {
            BestScore = PlayerPrefs.GetInt("HighScore");
            highScoreText.text = "Лучший счет: " + BestScore.ToString();
        }

        // создание фигуры - треугольник1
        TFigure Fig = new TFigure();
        Fig.ID = 0;
        Fig.Nodes = new Vector3[3];
        Fig.Nodes[0] = new Vector3(10f, 10f, 20f);
        Fig.Nodes[1] = new Vector3(0, 30f, 20f);
        Fig.Nodes[2] = new Vector3(-10f, 10f, 20f);
        Fig.Angles = new float[3];
        Fig.Angles[0] = Vector3.Angle(Fig.Nodes[2] - Fig.Nodes[0], Fig.Nodes[1] - Fig.Nodes[0]);
        Fig.Angles[1] = Vector3.Angle(Fig.Nodes[0] - Fig.Nodes[1], Fig.Nodes[2] - Fig.Nodes[1]);
        Fig.Angles[2] = Vector3.Angle(Fig.Nodes[1] - Fig.Nodes[2], Fig.Nodes[0] - Fig.Nodes[2]);
        Figures.Add(Fig);

        // создание фигуры - квадрат
        Fig = new TFigure();
        Fig.ID = 1;
        Fig.Nodes = new Vector3[4];
        Fig.Nodes[0] = new Vector3(10f, 10f, 20f);
        Fig.Nodes[1] = new Vector3(10, 30f, 20f);
        Fig.Nodes[2] = new Vector3(-10f, 30f, 20f);
        Fig.Nodes[3] = new Vector3(-10f, 10f, 20f);
        Fig.Angles = new float[4];
        Fig.Angles[0] = Vector3.Angle(Fig.Nodes[3] - Fig.Nodes[0], Fig.Nodes[1] - Fig.Nodes[0]);
        Fig.Angles[1] = Vector3.Angle(Fig.Nodes[0] - Fig.Nodes[1], Fig.Nodes[2] - Fig.Nodes[1]);
        Fig.Angles[2] = Vector3.Angle(Fig.Nodes[1] - Fig.Nodes[2], Fig.Nodes[3] - Fig.Nodes[2]);
        Fig.Angles[3] = Vector3.Angle(Fig.Nodes[2] - Fig.Nodes[3], Fig.Nodes[0] - Fig.Nodes[3]);
        Figures.Add(Fig);

        // создание фигуры - треугольник2
        Fig = new TFigure();
        Fig.ID = 2;
        Fig.Nodes = new Vector3[3];
        Fig.Nodes[0] = new Vector3(0f, 10f, 20f);
        Fig.Nodes[1] = new Vector3(10, 30f, 20f);
        Fig.Nodes[2] = new Vector3(-10f, 30f, 20f);
        Fig.Angles = new float[3];
        Fig.Angles[0] = Vector3.Angle(Fig.Nodes[2] - Fig.Nodes[0], Fig.Nodes[1] - Fig.Nodes[0]);
        Fig.Angles[1] = Vector3.Angle(Fig.Nodes[0] - Fig.Nodes[1], Fig.Nodes[2] - Fig.Nodes[1]);
        Fig.Angles[2] = Vector3.Angle(Fig.Nodes[1] - Fig.Nodes[2], Fig.Nodes[0] - Fig.Nodes[2]);
        Figures.Add(Fig);

        // показ фигуры для решения и настройка параметром компонента line renderer
        ShowFigure = new GameObject("SampleFigure");
        ShowFigure.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - 200f, 0f);
        ShowFigureLine = ShowFigure.AddComponent<LineRenderer>();
        ShowFigureLine.SetWidth(0.5f, 0.5f);
        DrawSample();

        // инициализация времени и счета
        LastTime = Time.time;
        TimeToRound = 25;
        CurScore = 0;        
    }

    int DrawID; // переменная для хранения ID фигуры

    // метод для составления рандомной фигуры из заданных точек
    void DrawSample()
    {
        DrawID = Random.Range(0, Figures.Count);
        ShowFigureLine.SetVertexCount(Figures[DrawID].Nodes.Length);
        ShowFigureLine.SetPositions(Figures[DrawID].Nodes);
        ShowFigureLine.SetVertexCount(Figures[DrawID].Nodes.Length + 1);
        ShowFigureLine.SetPosition(Figures[DrawID].Nodes.Length, Figures[DrawID].Nodes[0]);
    }

    // метод сравнения нарисованной и сгенерированной фигуры
    bool CompareFigure()
    {
        bool CompareResult = false;
        float CurAngle;
        if (drawPoints.Count - 1 == Figures[DrawID].Nodes.Length)
        {
            for (int nodeID = 0; nodeID < drawPoints.Count - 1; nodeID++)
            {
                if (nodeID == 0) CurAngle = Vector3.Angle(drawPoints[drawPoints.Count - 2] - drawPoints[nodeID], drawPoints[nodeID + 1] - drawPoints[nodeID]);
                else CurAngle = Vector3.Angle(drawPoints[nodeID - 1] - drawPoints[nodeID], drawPoints[nodeID + 1] - drawPoints[nodeID]);
                if (Mathf.Abs(CurAngle - Figures[DrawID].Angles[nodeID]) > 10f) break;
                else if (nodeID == drawPoints.Count - 2) CompareResult = true;
            }
        }
        return CompareResult;
    }

    // метод добавления нарисованной фигуры в Edit mode 
    void AddFigure(Vector3[] points)
    {
        // ценрирование координат узлов фигуры относительно цетра масс
        Vector3 Center = Vector3.zero;
        for (int nodeID = 0; nodeID < points.Length; nodeID++)
            Center += points[nodeID];
        Center = Center / points.Length;
        Center.z = 0f;
        Center += new Vector3(0f, -20f, -4f); //Cam pos
        for (int nodeID = 0; nodeID < points.Length; nodeID++)
            points[nodeID] -= Center;

        // занесение фигуры в список с предварительным расчетом углов
        TFigure Fig = new TFigure();
        Fig.ID = Figures.Count;
        Fig.Nodes = new Vector3[points.Length - 1];
        Fig.Angles = new float[points.Length - 1];
        for (int nodeID = 0; nodeID < Fig.Nodes.Length; nodeID++)
        {
            Fig.Nodes[nodeID] = points[nodeID];
            if (nodeID == 0) Fig.Angles[nodeID] = Vector3.Angle(points[drawPoints.Count - 2] - points[nodeID], points[nodeID + 1] - points[nodeID]);
            else Fig.Angles[nodeID] = Vector3.Angle(points[nodeID - 1] - points[nodeID], points[nodeID + 1] - points[nodeID]);
        }
        Figures.Add(Fig);
    }

    // метод показывающий экран проигрыша с счетчиками результатов и кнопками выхода, игры сначала
    void GameOver()
    {
        scoreGO.text = "Счет: " + CurScore.ToString();
        highScoreGO.text = "Лучший счет: " + BestScore.ToString();
        GameUI.SetActive(false);
        GameOverUI.SetActive(true);
        ShowFigure.SetActive(false);
        LastTime = Time.time;
        TimeToRound = 25;
        CurScore = 0;
    }


    float DistMouseMove, MouseAngle, DistAngleSelect = 5f, LastTime = 0f;
    Vector3 point, MousePosBack, MousePosFront, MouseWorldPos;
    
    // Update is called once per frame
    void Update()
    {
        // ослеживание движения мыши для визуализации частиц в этом же месте
        CursorPS.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 20f));

        if (!GameUI.activeSelf) return;
        if (Time.time - LastTime > TimeToRound)
            GameOver();
        timeText.text = Mathf.Round((TimeToRound - (Time.time - LastTime))).ToString();

        // если нажата правая кнопка миши удаляет нарисованный объект
        if (Input.GetMouseButtonDown(1))
        {
            isMousePressed = false;

            GameObject[] delete = GameObject.FindGameObjectsWithTag("LineDraw");
            int deleteCount = delete.Length;
            for (int i = deleteCount - 1; i >= 0; i--)
                Destroy(delete[i]);
        }

        if (Input.GetKeyUp(KeyCode.Return)) EditMode = !EditMode; // переход в режим создания фигуры

        // Если нажата левая кнопка мыши происходит создание префаба с line renderer компонентом
        if (Input.GetMouseButtonDown(0))
        {
            ShowFigure.SetActive(false);
            CursorPS.SetActive(true);
            isMousePressed = true;
            lineDrawPrefab = GameObject.Instantiate(lineDrawPrefabs) as GameObject;
            lineRenderer = lineDrawPrefab.GetComponent<LineRenderer>();
            lineRenderer.SetVertexCount(2);
            MouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 20f));
            MousePosBack = MouseWorldPos;
            drawPoints.Add(MouseWorldPos);
            drawPoints.Add(MouseWorldPos);
            lineRenderer.SetPosition(0, MouseWorldPos);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ShowFigure.SetActive(true);
            CursorPS.SetActive(false);
            if (!EditMode) {
                if (CompareFigure()) {
                    DrawSample();
                    TimeToRound *= SpeedDownCoeff;
                    LastTime = Time.time;
                    
                    CurScore++;
                    scoreText.text = "Счет: " + CurScore.ToString();
                    if (CurScore > BestScore) {
                        BestScore = CurScore;
                        PlayerPrefs.SetInt("HighScore", BestScore);
                        highScoreText.text = "Лучший счет: " + BestScore.ToString();
                    }
                }
            }
            else AddFigure(drawPoints.ToArray());
            
            // отжата левая кнопка мыши рисование прекращается
            isMousePressed = false;
            lineRenderer.SetPosition(drawPoints.Count - 1, drawPoints[0]);
            drawPoints.Clear();

            // если нажата правая кнопка миши удаляет нарисованный объект
            GameObject[] delete = GameObject.FindGameObjectsWithTag("LineDraw");
            int deleteCount = delete.Length;
            for (int i = deleteCount - 1; i >= 0; i--)
                Destroy(delete[i]);
        }

        if (isMousePressed)
        {
            // если удерживается нажатая кнопка миши продолжаются создаваться точки для построения прямой
            MouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 20f));
            DistMouseMove = Vector3.Distance(MousePosBack, MouseWorldPos);
            if (DistMouseMove > DistAngleSelect)
            {
                MouseAngle = Vector3.Angle(MousePosFront - MousePosBack, MouseWorldPos - MousePosBack);
                if (MouseAngle < 5f)
                {
                    point = (drawPoints[drawPoints.Count - 1] - drawPoints[drawPoints.Count - 2]).normalized * 
                        Vector3.Distance(MouseWorldPos, drawPoints[drawPoints.Count - 1]) + drawPoints[drawPoints.Count - 1];
                    lineRenderer.SetPosition(drawPoints.Count - 1, point);
                }
                else
                {
                    MousePosBack = point;
                    drawPoints[drawPoints.Count - 1] = point;
                    drawPoints.Add(MouseWorldPos);
                    lineRenderer.SetVertexCount(drawPoints.Count);
                    lineRenderer.SetPosition(drawPoints.Count - 1, MouseWorldPos);
                    DistMouseMove = Vector3.Distance(MousePosBack, MouseWorldPos);
                    DistAngleSelect = DistMouseMove * 1.2f;
                    if (DistAngleSelect < 3f) DistAngleSelect = 3f;
                }
            }
            else
            {
                lineRenderer.SetPosition(drawPoints.Count - 1, MouseWorldPos);
                drawPoints[drawPoints.Count - 1] = MouseWorldPos;
                MousePosFront = MouseWorldPos;
            }
        }
    }
}
