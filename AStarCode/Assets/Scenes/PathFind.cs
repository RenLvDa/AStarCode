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
    public class Node : ICloneable,IHeapItem<Node>
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
        public object Clone()
        {
            Node instance = new Node(this.x, this.y, this.walkable);
            instance.gCost = this.gCost;
            instance.hCost = this.hCost;
            return instance;
        }

        private int _heapIndex = 0;
        public int HeapIndex
        {
            get
            {
                return _heapIndex;
            }
            set
            {
                _heapIndex = value;
            }
        }
        public int CompareTo(Node nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
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
        return (x >= 0 && x < data_width) && (y >= 0 && y < data_width) && (data[y * data_width + x] != 0);
    }
    public bool isWalkable(int x,int y)
    {
        return (x >= 0 && x < data_width) && (y >= 0 && y < data_width) && (data[y * data_width + x] != 0);
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
    //寻路函数入口(新)
    public bool Find(Vector2 begin, Vector2 end, List<Vector2> lstPath)
    {
        return JPS(begin, end, lstPath);
    }
    //寻路函数入口(旧) 
    public bool FindOld(Vector2 begin, Vector2 end, List<Vector2> lstPath)
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
            List<Node> list = new List<Node>();
            int n = 1024 * 1024;
            for(int i = 0; i < n; i++)
            {
                //list.Add(new Node(i, i, true));
            }
            for(int i = 0; i < n; i++)
            {
                //Debug.Log(1);
            }
            for(int i = 0; i < lstpath.Count; i++)
            {
                Debug.Log(lstpath[i]);
            }
            /*
            if (TestFind(new Vector2(50, 50), new Vector2(512, 512), lstpath))
            {
                Debug.Log("find success");
            }
            else
            {
                Debug.Log("find failed");
            }
            */
        }
        if (GUI.Button(new Rect(0, 100, 100, 100), "Find"))
        {
            if (Find(new Vector2(512,512), new Vector2(0, 0), lstpath))
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
    #region JPS
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
        //List<Node> openList = new List<Node>();
        Heap<Node> openSet = new Heap<Node>(data_width* data_width);
        HashSet<Node> openSetContainer = new HashSet<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Node currentNode;

        //openList.Add(beginNode);
        openSet.Add(beginNode);
        openSetContainer.Add(beginNode);
        while (openSet.Count > 0)
        {
            currentNode = openSet.RemoveFirst();
            //找出OpenList里f(n)=g(n)+h(n)最小的node
            //Node currentNode = (Node)openList[0].Clone();
            //RemoveHeapTop(openList);
            openSetContainer.Remove(currentNode); //warning
            if(currentNode.x == endNode.x && currentNode.y == endNode.y)
            {
                lstPath.Add(end);
                return true;
            }
            else
            {
                closedSet.Add(currentNode);
                List<Node> Nodes = GetSuccessors(currentNode);
                foreach(Node node in Nodes)
                {
                    if (closedSet.Contains(node))
                        continue;

                    int newGCost = currentNode.gCost + _GetDistance(currentNode, node);

                    if (newGCost < node.gCost || !openSetContainer.Contains(node))
                    {
                        node.gCost = newGCost;
                        node.hCost = _GetDistance(node, endNode);
                        node.parent = currentNode;

                        lstPath.Add(new Vector2(node.x, node.y)); //todo
                        if (!openSetContainer.Contains(node))
                        {
                            openSetContainer.Add(node);
                            openSet.Add(node);
                            //openList.Add(node);
                            //upAdjust(openList);
                        }
                        else
                        {
                            openSet.UpdateItem(node);
                            //buildHeap(openList); //todo
                        }
                    }
                }
            }
        }
        //TODO 记得删掉这个return
        return false;
    }

    private List<Node> GetSuccessors(Node currentNode)
    {
        Node jumpNode;
        List<Node> successors = new List<Node>();
        List<Node> neighbours = JpsGetNeighbours(currentNode);
        foreach(Node neighbour in neighbours)
        {
            int xDirection = neighbour.x - currentNode.x;
            int yDirection = neighbour.y - currentNode.y;

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
                    neighbours.Add(new Node(currentNode.x, currentNode.y + yDirection, isWalkable(currentNode.x, currentNode.y + yDirection)));

                if (neighbourRight)
                    neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y, isWalkable(currentNode.x + xDirection, currentNode.y)));

                if (neighbourUp || neighbourRight)
                    if (isWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
                        neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y + yDirection, isWalkable(currentNode.x + xDirection, currentNode.y + yDirection)));

                if (!neighbourLeft && neighbourUp)
                    if (isWalkable(currentNode.x - xDirection, currentNode.y + yDirection))
                        neighbours.Add(new Node(currentNode.x - xDirection, currentNode.y + yDirection, isWalkable(currentNode.x - xDirection, currentNode.y + yDirection)));

                if (!neighbourDown && neighbourRight)
                    if (isWalkable(currentNode.x + xDirection, currentNode.y - yDirection))
                        neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y - yDirection, isWalkable(currentNode.x + xDirection, currentNode.y - yDirection)));
            }
            else
            {
                if (xDirection == 0)
                {
                    //y方向
                    if (isWalkable(currentNode.x, currentNode.y + yDirection))
                    {
                        neighbours.Add(new Node(currentNode.x, currentNode.y + yDirection,isWalkable(currentNode.x, currentNode.y + yDirection)));

                        if (!isWalkable(currentNode.x + 1, currentNode.y))
                            if (isWalkable(currentNode.x + 1, currentNode.y + yDirection))
                                neighbours.Add(new Node(currentNode.x + 1, currentNode.y + yDirection, isWalkable(currentNode.x + 1, currentNode.y + yDirection)));

                        if (!isWalkable(currentNode.x - 1, currentNode.y))
                            if (isWalkable(currentNode.x - 1, currentNode.y + yDirection))
                                neighbours.Add(new Node(currentNode.x - 1, currentNode.y + yDirection, isWalkable(currentNode.x - 1, currentNode.y + yDirection)));
                    }
                }
                else
                {
                    //x方向
                    if (isWalkable(currentNode.x + xDirection, currentNode.y))
                    {
                        neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y, isWalkable(currentNode.x + xDirection, currentNode.y)));
                        if (!isWalkable(currentNode.x, currentNode.y + 1))
                            neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y + 1, isWalkable(currentNode.x + xDirection, currentNode.y + 1)));
                        if (!isWalkable(currentNode.x, currentNode.y - 1))
                            neighbours.Add(new Node(currentNode.x + xDirection, currentNode.y - 1, isWalkable(currentNode.x + xDirection, currentNode.y - 1)));
                    }
                }
            }
        }
        return neighbours;
    }

    //寻找跳点
    private Node Jump(Node currentNode,Node parentNode,int xDirection,int yDirection)
    {
        //如果当前节点为空或不可走 返回空
        if (currentNode == null || !isWalkable(currentNode.x, currentNode.y))
            return null;
        //如果当前节点为终点 返回当前节点
        if(currentNode.x == endNode.x && currentNode.y == endNode.y)
        {
            forced = true;
            return currentNode;
        }
        //开始寻找跳点
        forced = false;
        //情况：方向为对角线
        if (xDirection != 0 && yDirection != 0)
        {
            if ((!isWalkable(currentNode.x - xDirection, currentNode.y) && isWalkable(currentNode.x - xDirection, currentNode.y + yDirection)) ||
                (!isWalkable(currentNode.x, currentNode.y - yDirection) && isWalkable(currentNode.x + xDirection, currentNode.y - yDirection)))
            {
                return currentNode; //此时currentNode是跳点（因为有强迫邻居）
            }
            Node nextHorizontalNode = new Node(currentNode.x + xDirection, currentNode.y, isWalkable(currentNode.x + xDirection, currentNode.y));
            Node nextVerticalNode = new Node(currentNode.x, currentNode.y + yDirection, isWalkable(currentNode.x, currentNode.y + yDirection));
            if (nextHorizontalNode.walkable == false || nextVerticalNode.walkable == false)
            {
                bool found = false;
                if (nextHorizontalNode.walkable == true && isWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
                {
                    found = true;
                }
                if (nextVerticalNode.walkable == true && isWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
                {
                    found = true;
                }
                if (!found)
                    return null;
            }
            if (Jump(nextHorizontalNode, currentNode, xDirection, 0) != null || Jump(nextVerticalNode, currentNode, 0, yDirection) != null)
            {
                if (!forced)
                {
                    Node temp = new Node(currentNode.x + xDirection, currentNode.y + yDirection, isWalkable(currentNode.x + xDirection, currentNode.y + yDirection));
                    return Jump(temp, currentNode, xDirection, yDirection);
                }
                else
                {
                    return currentNode;
                }
            }
        }
        else//情况：方向为水平垂直线
        {
            //方向为水平
            if (xDirection != 0)
            {
                if((isWalkable(currentNode.x + xDirection, currentNode.y + 1) && !isWalkable(currentNode.x, currentNode.y + 1)) ||
                   (isWalkable(currentNode.x + xDirection, currentNode.y - 1) && !isWalkable(currentNode.x, currentNode.y - 1)))
                {
                    forced = true;
                    return currentNode;
                }
            }
            else//方向为垂直
            {
                if ((isWalkable(currentNode.x + 1, currentNode.y + yDirection) && !isWalkable(currentNode.x + 1, currentNode.y)) ||
                    (isWalkable(currentNode.x - 1, currentNode.y + yDirection) && !isWalkable(currentNode.x - 1, currentNode.y)))
                {
                    forced = true;
                    return currentNode;
                }
            }
        }
        Node nextNode = new Node(currentNode.x + xDirection, currentNode.y + yDirection, isWalkable(currentNode.x + xDirection, currentNode.y + yDirection));
        if (!isWalkable(currentNode.x + xDirection, currentNode.y + yDirection))
        {
            nextNode = null;
        }
        return Jump(nextNode, currentNode, xDirection, yDirection);
    }

    private int _GetDistance(Node a, Node b)
    {
        int distX = Mathf.Abs(a.x - b.x);
        int distY = Mathf.Abs(a.y - b.y);

        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);

        return 14 * distX + 10 * (distY - distX);
    }

    private void DoPath()
    {
        List<Node> path = _RetracePath();
        Vector2 vector2;
        for (int i = 0; i < path.Count; i++)
        {
           vector2 = new Vector2(path[i].x,path[i].y);
           lstpath.Add(vector2);
        }

    }
    private List<Node> _RetracePath()
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != beginNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    #endregion
}
