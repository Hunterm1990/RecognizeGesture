using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LineDraw : MonoBehaviour
{
    public GameObject lineDrawPrefabs; // this is where we put the prefabs object
    public bool EditMode = false;
    public float TimeToRound = 25f, SpeedDownCoeff = 0.98f;
    public int CurScore = 0, BestScore = 0;

    public Text scoreText;
    public Text highScoreText;
    public Text timeText;

    public Text scoreGO;
    public Text highScoreGO;

    public GameObject GameUI, GameOverUI, CursorPS;

    private bool isMousePressed = false;
    private GameObject lineDrawPrefab, ShowFigure;
    private LineRenderer lineRenderer, ShowFigureLine;
    private List<Vector3> drawPoints = new List<Vector3>();

    struct TFigure
    {
        public Vector3[] Nodes;
        public int ID;
        public float[] Angles;
    }

    private List<TFigure> Figures = new List<TFigure>();

    // Use this for initialization
    void Start()
    {
        GameUI.SetActive(true);
        GameOverUI.SetActive(false);
        if (PlayerPrefs.HasKey("HighScore"))
        {
            BestScore = PlayerPrefs.GetInt("HighScore");
            highScoreText.text = "Лучший счет: " + BestScore.ToString();
        }

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

        ShowFigure = new GameObject("SampleFigure");
        ShowFigure.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y - 200f, 0f);
        ShowFigureLine = ShowFigure.AddComponent<LineRenderer>();
        ShowFigureLine.SetWidth(0.5f, 0.5f);
        DrawSample();

        LastTime = Time.time;
        TimeToRound = 25;
        CurScore = 0;        
    }

    int DrawID;
    void DrawSample()
    {
        DrawID = Random.Range(0, Figures.Count);
        ShowFigureLine.SetVertexCount(Figures[DrawID].Nodes.Length);
        ShowFigureLine.SetPositions(Figures[DrawID].Nodes);
        ShowFigureLine.SetVertexCount(Figures[DrawID].Nodes.Length + 1);
        ShowFigureLine.SetPosition(Figures[DrawID].Nodes.Length, Figures[DrawID].Nodes[0]);
    }

    bool CompareFigure()
    {
        bool CompareResult = false;
        float CurAngle;
        //Debug.Log("Compare: " + drawPoints.Count + " | " + Figures[DrawID].Nodes.Length);
        if (drawPoints.Count - 1 == Figures[DrawID].Nodes.Length)
        {
            for (int nodeID = 0; nodeID < drawPoints.Count - 1; nodeID++)
            {
                if (nodeID == 0) CurAngle = Vector3.Angle(drawPoints[drawPoints.Count - 2] - drawPoints[nodeID], drawPoints[nodeID + 1] - drawPoints[nodeID]);
                else CurAngle = Vector3.Angle(drawPoints[nodeID - 1] - drawPoints[nodeID], drawPoints[nodeID + 1] - drawPoints[nodeID]);
                if (Mathf.Abs(CurAngle - Figures[DrawID].Angles[nodeID]) > 10f) break;
                else if (nodeID == drawPoints.Count - 2) CompareResult = true;
                //Debug.Log("Node: " + nodeID);
            }
        }
        //Debug.Log("Result: " + CompareResult);
        return CompareResult;
    }

    void AddFigure(Vector3[] points)
    {
        Vector3 Center = Vector3.zero;
        for (int nodeID = 0; nodeID < points.Length; nodeID++)
            Center += points[nodeID];
        Center = Center / points.Length;
        Center.z = 0f;
        Center += new Vector3(0f, -20f, -4f); //Cam pos
        for (int nodeID = 0; nodeID < points.Length; nodeID++)
            points[nodeID] -= Center;

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
        CursorPS.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 20f));

        if (!GameUI.activeSelf) return;
        if (Time.time - LastTime > TimeToRound)
            GameOver();
        timeText.text = Mathf.Round((TimeToRound - (Time.time - LastTime))).ToString();

        if (Input.GetMouseButtonDown(1))
        {
            isMousePressed = false;

            // delete the LineRenderers when right mouse down
            GameObject[] delete = GameObject.FindGameObjectsWithTag("LineDraw");
            int deleteCount = delete.Length;
            for (int i = deleteCount - 1; i >= 0; i--)
                Destroy(delete[i]);
        }

        if (Input.GetKeyUp(KeyCode.Return)) EditMode = !EditMode;

        if (Input.GetMouseButtonDown(0))
        {
            ShowFigure.SetActive(false);
            // left mouse down, make a new line renderer
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
            
            // left mouse up, stop drawing
            isMousePressed = false;
            lineRenderer.SetPosition(drawPoints.Count - 1, drawPoints[0]);
            drawPoints.Clear();

            // delete the LineRenderers when right mouse down
            GameObject[] delete = GameObject.FindGameObjectsWithTag("LineDraw");
            int deleteCount = delete.Length;
            for (int i = deleteCount - 1; i >= 0; i--)
                Destroy(delete[i]);
        }

        if (isMousePressed)
        {
            // when the left mouse button pressed
            // continue to add vertex to line renderer
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
