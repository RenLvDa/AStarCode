using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PathFind : MonoBehaviour
{
    [DllImport("PathFindCplusplus")]
    private static extern bool InitCPP(int w, int h, byte[] pathdata, int length);
    [DllImport("PathFindCplusplus")]
    private static extern bool ReleaseCPP();
    [DllImport("PathFindCplusplus")]
    private static extern bool FindCPP(Vector3 Start,Vector3 End, Vector2[] outPath,ref int pathCount);

    private bool isInitCPP = false;
    private bool forced;
    public Texture2D dataTex;
    byte[] data = null;
    List<Vector2> lstpath = new List<Vector2>();
    int data_width = 0;

    //起始节点
    private Node beginNode;
    private Node endNode;

    #region my node
    //Node存放寻路点信息
    public class Node : ICloneable
    {
        //public Vector2 coordinate;
        public int x;
        public int y;
        public bool walkable;
        public int gCost = 0;
        public int hCost = 0;
        public int fCost
        {
            get { return gCost + hCost; }
        }
        public Node parent; //父节点
        public Node(int x, int y, bool isWalkable)
        {
            this.x = x;
            this.y = y;
            gCost = 0;
            hCost = 0;
            walkable = isWalkable;
        }
        public static bool operator <(Node a, Node b)
        {
            if (a.fCost < b.fCost || a.fCost == b.fCost && a.hCost < b.hCost) return true;
            else return false;
        }
        public static bool operator >(Node a, Node b)
        {
            if (a.fCost > b.fCost || a.fCost == b.fCost && a.hCost > b.hCost) return true;
            else return false;
        }
        public static bool operator <=(Node a, Node b)
        {
            if (a.fCost <= b.fCost) return true;
            else return false;
        }
        public static bool operator >=(Node a, Node b)
        {
            if (a.fCost <= b.fCost) return true;
            else return false;
        }
        public static bool operator ==(Node a,Node b)
        {
            if (a.x == b.x && a.y == b.y) return true;
            else return false;
        }
        public static bool operator !=(Node a, Node b)
        {
            if (a.x != b.x || a.y != b.y) return true;
            else return false;
        }
        public override bool Equals(object obj)
        {
            var node = obj as Node;
            if (node.x == this.x && node.y == this.y) return true;
            else return false;
        }
        public object Clone()
        {
            Node instance = new Node(this.x, this.y, this.walkable);
            instance.gCost = this.gCost;
            instance.hCost = this.hCost;
            return instance;
        }
    }
    #endregion

    void LoadData()
    {
        data_width = dataTex.width;
        Color32[]  tempdata = dataTex.GetPixels32();
        data = new byte[tempdata.Length];
        for(int i=0;i< data.Length;i++)
        {
            data[i] = tempdata[i].r;

        }
    }
    public bool isWalkable(Vector2 v)
    {
        int x = (int)v.x;
        int y = (int)v.y;
        return data[y * data_width + x] != 0;
    }
    public bool isWalkable(int x,int y)
    {
        return data[y * data_width + x] != 0;
    }
    // Start is called before the first frame update
    void Start()
    {
        LoadData();
    }

    private void OnDestroy()
    {
        if (isInitCPP)
        {
            ReleaseCPP();
        }
    }
    //测试函数 仅用于演示
    public bool TestFind(Vector2 begin,Vector2 end,List<Vector2> lstPath)
    {
        lstPath.Clear();

        if(!isWalkable(begin))
        {
            return false;
        }
        if (!isWalkable(end))
        {
            return false;
        }
        lstPath.Add(begin);
        lstPath.Add(new Vector2(890,143));
        lstPath.Add(new Vector2(868, 901));
        lstPath.Add(new Vector2(858, 901));
        lstPath.Add(new Vector2(661, 358));
        lstPath.Add(end);

        return true;
    }
    //寻路函数入口 
    public bool Find(Vector2 begin, Vector2 end, List<Vector2> lstPath)
    {
        lstPath.Clear();
        //补充真正的寻路 如果寻路成功返回true 如果寻路失败返回false
        lstPath.Add(begin);
        Node beginNode = new Node((int)begin.x, (int)begin.y, isWalkable(begin));
        Node endNode = new Node((int)end.x, (int)end.y, isWalkable(end));
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();
        openList.Add(beginNode);

        while (openList.Count > 0)
        {
            //Step1:找出OpenList里f(n)=g(n)+h(n)最小的node
            /*
            Node currentNode = openList[0];
            for(int i = 0; i < openList.Count; i++)
            {
                if(openList[i].fCost < currentNode.fCost ||
                   openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost)
                {
                    currentNode = openList[i];
                }
            }
            */
            Node currentNode = (Node)openList[0].Clone();

            //Step2;从OpenList中移除currentNode，并且加入ClosedList
            /*
            openList.Remove(currentNode);
            closedList.Add(currentNode);
            */
            RemoveHeapTop(openList);
            closedList.Add(currentNode);

            //Step3:如果currentNode就是最终节点，停止寻路并且生成路径
            if (currentNode.x == endNode.x && currentNode.y == endNode.y)
            {
                Debug.Log("end");
                lstPath.Add(end);
                return true;
            }
            //Step4:遍历currentNode的所有邻居节点
            List<Node> neighbourList = GetNeighbours(currentNode);
            for (int i = 0; i < neighbourList.Count; i++)
            {
                Node node = neighbourList[i];
                if (!node.walkable || closedList.Contains(node))
                    continue;
                int newCont = currentNode.gCost + getDistanceNodes(currentNode, node);
                //当OpenList中没有node 或者 当前算出来的node新g(n)小于OpenList中的node旧g(n)
                if (newCont < node.gCost || !openList.Contains(node))
                {
                    node.gCost = newCont;
                    node.hCost = getDistanceNodes(node, endNode);

                    lstPath.Add(new Vector2(node.x, node.y));
                    if (!openList.Contains(node))
                    {
                        Debug.Log(openList.Count);
                        openList.Add(node);
                        upAdjust(openList);
                    }
                }
            }
        }
        return false;
    }
    #region utils
    private void OnGUI()
    {
        DrawDebug();
        DrawButton();
    }
    //显示按钮
    void DrawButton()
    {
        if (GUI.Button(new Rect(0, 0, 100, 100), "Find Example"))
        {
            if (TestFind(new Vector2(50, 50), new Vector2(512, 512), lstpath))
            {
                Debug.Log("find success");
            }
            else
            {
                Debug.Log("find failed");
            }
        }
        if (GUI.Button(new Rect(0, 100, 100, 100), "Find"))
        {
            if (Find(new Vector2(50, 50), new Vector2(600, 120), lstpath))
            {
                Debug.Log("find success");
            }
            else
            {
                Debug.Log("find failed");
            }
        }
        if (GUI.Button(new Rect(0, 200, 100, 100), "FindCPP"))
        {
            if(!isInitCPP)
            {
                InitCPP(data_width, data_width, data, data.Length);
                isInitCPP = true;
            }
            Vector2[] allPath = new Vector2[1024];
            int path_len = allPath.Length;
            if (FindCPP(new Vector2(50, 50), new Vector2(322, 536), allPath, ref path_len))
            {
                lstpath.Clear();
                for (int i = 0; i < path_len; i++)
                {
                    lstpath.Add(allPath[i]);
                }
                Debug.Log("find success");
            }
            else
            {
                lstpath.Clear();
                Debug.Log("find failed");
            }
        }
    }
    //显示地图和路径
    void DrawDebug(float scale = 0.5f)
    {
        GUI.DrawTexture(new Rect(0, 0, dataTex.width*scale, dataTex.height*scale), dataTex);
        for (int i = 1; i < lstpath.Count; i++)
        {
            Vector2 last = lstpath[i - 1];
            Vector2 next = lstpath[i];
            float length = (next - last).magnitude;
            if (length > 4.0f)
            {
                int count = (int)(length*0.25f);
                Vector2 step = (next - last) / count;
                for (int j = 0; j < count; j++)
                {
                    Vector2 current = last + step * j;
                    GUI.DrawTexture(new Rect(current.x* scale, current.y* scale, 1, 1), Texture2D.whiteTexture);
                }
            }
            else
            {
                GUI.DrawTexture(new Rect(next.x* scale, next.y* scale, 1, 1), Texture2D.whiteTexture);
            }
        }
    }
    #endregion
    #region functions
    //查找一个节点的所有邻居节点
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbourList = new List<Node>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                    continue;
                int tempX = (int)node.x + i;
                int tempY = (int)node.y + j;
                if (tempX < 1024 && !(tempX < 0) && tempY < 1024 && !(tempY < 0))
                {
                    neighbourList.Add(new Node(tempX, tempY, isWalkable(tempX, tempY)));
                }
            }
        }
        return neighbourList;
    }

    //估价函数h(n)
    public int getDistanceNodes(Node a, Node b)
    {
        int cntX = Mathf.Abs((int)a.x - (int)b.x);
        int cntY = Mathf.Abs((int)a.y - (int)b.y);
        if (cntX >= cntY)
            return 14 * cntY + 10 * (cntX - cntY);
        else
            return 14 * cntX + 10 * (cntY - cntX);
    }
    #endregion
    #region heap
    /// <summary>
    /// 堆
    /// </summary>
    //上浮调整
    public void upAdjust(List<Node> list)
    {
        int childIndex = list.Count - 1;
        int parentIndex = (childIndex - 1) / 2;
        //temp保存插入的叶子节点值，用于最后的赋值
        Node temp = list[childIndex];
        while (childIndex > 0 && temp < list[parentIndex])
        {
            //无需真正交换，单向赋值即可
            list[childIndex] = list[parentIndex];
            childIndex = parentIndex;
            parentIndex = (parentIndex - 1) / 2;
        }
        list[childIndex] = temp;
    }
    //下沉调整
    public void downAdjust(List<Node> list, int parentIndex, int length)
    {
        //temp保存父节点值，用于最后的赋值
        Node temp = (Node)list[parentIndex].Clone();
        int childIndex = 2 * parentIndex + 1;
        while (childIndex < length)
        {
            //如果有右孩子，且右孩子小于左孩子的值，则定位到右孩子
            if (childIndex + 1 < length && list[childIndex + 1] < list[childIndex])
            {
                childIndex++;
            }
            //如果父节点小于任何一个孩子的值，直接跳出
            if (temp <= list[childIndex])
            {
                break;
            }
            //无需真正交换，单向赋值即可
            list[parentIndex] = list[childIndex];
            parentIndex = childIndex;
            childIndex = 2 * childIndex + 1;
        }
        list[parentIndex] = temp;
    }
    //构建堆
    public void buildHeap(List<Node> list)
    {
        int index = list.Count / 2 - 1;
        if (index < 0) index = 0;
        //从最后一个非叶子节点开始，依次往下沉调整
        for (int i = index; i >= 0; i--)
        {
            downAdjust(list, i, list.Count - 1);
        }
    }
    //移除堆顶元素
    public void RemoveHeapTop(List<Node> list)
    {
        int last = list.Count - 1;
        list[0] = list[last];
        list.RemoveAt(last);
        if (list.Count > 1)
        {
            downAdjust(list, 0, list.Count - 1);
        }
    }
    #endregion
    /// <summary>
    /// JPS跳点寻路
    /// </summary>
    public bool JPS(Vector2 begin, Vector2 end, List<Vector2> lstPath)
    {
        //存放路径
        lstPath.Clear();
        lstPath.Add(begin);
        //初始化起始点和结束点
        beginNode = new Node((int)begin.x, (int)begin.y, isWalkable(begin));
        endNode = new Node((int)end.x, (int)end.y, isWalkable(end));
        //初始化openList和closedList
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();
        openList.Add(beginNode);
        while (openList.Count > 0)
        {
            //找出OpenList里f(n)=g(n)+h(n)最小的node
            Node currentNode = (Node)openList[0].Clone();
            Node parentNode = currentNode.parent;
            if(currentNode == endNode)
            {
                return true;
            }
            else
            {

            }
        }
        //TODO 记得删掉这个return
        return true;
    }

    private List<Node> GetSuccessors(Node currentNode)
    {
        Node jumpNode;
        List<Node> successors = new List<Node>();
        List<Node> neighbours = JpsGetNeighbours(currentNode);
        foreach(Node neighbour in neighbours)
        {
            int xDirection = (int)Mathf.Clamp(neighbour.x - currentNode.x, -1, 1);
            int yDirection = (int)Mathf.Clamp(neighbour.y - currentNode.y, -1, 1);

            jumpNode = Jump(neighbour, currentNode, xDirection, yDirection);

            if (jumpNode != null)
                successors.Add(jumpNode);
        }
        return successors;
    }

    //JPS算法获取邻居节点
    private List<Node> JpsGetNeighbours(Node currentNode)
    {
        List<Node> neighbours = new List<Node>();

        Node parentNode = currentNode.parent;
        if (parentNode == null)
        {
            neighbours = GetNeighbours(currentNode);
        }
        else
        {
            //非起点邻居点判断
            int xDirection = (int)Mathf.Clamp(currentNode.x - parentNode.x, -1, 1);
            int yDirection = (int)Mathf.Clamp(currentNode.y - parentNode.y, -1, 1);
            //判断是否水平方向
            if (xDirection != 0 && yDirection != 0)
            {
                //对角线方向
                //判断当前点四个方向邻居点是否可走
                bool neighbourUp = isWalkable(currentNode.x, currentNode.y + yDirection);
                bool neighbourRight = isWalkable(currentNode.x + xDirection, currentNode.y);
                bool neighbourLeft = isWalkable(currentNode.x - xDirection, currentNode.y);
                bool neighbourDown = isWalkable(currentNode.x, currentNode.y - yDirection);

                if (neighbourUp)
                    neighbours.Add(new Node(currentNode.x, currentNode.y + yDirection, true));

                if (neighbourRight)
                    neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y, true));

                if (neighbourUp || neighbourRight)
                    if (isWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
                        neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y + yDirection, true));

                if (!neighbourLeft && neighbourUp)
                    if (isWalkable(currentNode.x - xDirection, currentNode.y + yDirection))
                        neighbours.Add(new Node(currentNode.x - xDirection, currentNode.y + yDirection, true));

                if (!neighbourDown && neighbourRight)
                    if (isWalkable(currentNode.x + xDirection, currentNode.y - yDirection))
                        neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y - yDirection, true));
            }
            else
            {
                if (xDirection == 0)
                {
                    //y方向
                    if (isWalkable(currentNode.x, currentNode.y + yDirection))
                    {
                        neighbours.Add(new Node(currentNode.x, currentNode.y + yDirection,true));

                        if (!isWalkable(currentNode.x + 1, currentNode.y))
                            if (isWalkable(currentNode.x + 1, currentNode.y + yDirection))
                                neighbours.Add(new Node(currentNode.x + 1, currentNode.y + yDirection, true));

                        if (!isWalkable(currentNode.x - 1, currentNode.y))
                            if (isWalkable(currentNode.x - 1, currentNode.y + yDirection))
                                neighbours.Add(new Node(currentNode.x - 1, currentNode.y + yDirection, true));
                    }
                }
                else
                {
                    //x方向
                    if (isWalkable(currentNode.x + xDirection, currentNode.y))
                    {
                        neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y, true));
                        if (!isWalkable(currentNode.x, currentNode.y + 1))
                            neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y + 1, true));
                        if (!isWalkable(currentNode.x, currentNode.y - 1))
                            neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y - 1, true));
                    }
                }
            }
        }
        return neighbours;
    }

    private Node Jump(Node currentNode,Node parentNode,int xDirection,int yDirection)
    {
        if (currentNode == null || !isWalkable(currentNode.x, currentNode.y))
            return null;
        if(currentNode.x == endNode.x && currentNode.y == endNode.y)
        {
            forced = true;
            return currentNode;
        }
        forced = false;

        if (xDirection != 0 && yDirection != 0)
        {
            if (!isWalkable(currentNode.x - xDirection, currentNode.y) && isWalkable(currentNode.x - xDirection, currentNode.y + yDirection) ||
               !isWalkable(currentNode.x, currentNode.y - yDirection) && isWalkable(currentNode.x + xDirection, currentNode.y - yDirection))
            {
                return currentNode; //此时currentNode是跳点（因为有强迫邻居）
            }
            //TODO
        }

        return null;
    } 
}
