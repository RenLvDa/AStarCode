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
    public Texture2D dataTex;
    byte[] data = null;
    List<Vector2> lstpath = new List<Vector2>();
    int data_width = 0;

    //Node存放寻路点信息
    public class Node : ICloneable
    {
        public Vector2 coordinate;
        public bool walkable;
        public int gCost;
        public int hCost;
        public int fCost
        {
            get
            {
                return gCost + hCost;
            }
        }
        public Node(Vector2 coor, bool isWalkable)
        {
            this.coordinate = coor;
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
            Node instance = new Node(this.coordinate, this.walkable);
            instance.gCost = this.gCost;
            instance.hCost = this.hCost;
            return instance;
        }
    }
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
        Node beginNode = new Node(begin, isWalkable(begin));
        Node endNode = new Node(end, isWalkable(end));
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
            if (currentNode.coordinate == endNode.coordinate)
            {
                Debug.Log("end");
                lstPath.Add(end);
                return true;
            }
            //Step4:遍历currentNode的所有邻居节点
            for (int i = 0; i < GetNeibourhood(currentNode).Count; i++)
            {
                Node node = GetNeibourhood(currentNode)[i];
                if (!node.walkable || closedList.Contains(node))
                    continue;
                int newCont = currentNode.gCost + getDistanceNodes(currentNode, node);
                //当OpenList中没有node 或者 当前算出来的node新g(n)小于OpenList中的node旧g(n)
                if (newCont < node.gCost || !openList.Contains(node))
                {
                    node.gCost = newCont;
                    node.hCost = getDistanceNodes(node, endNode);

                    lstPath.Add(node.coordinate);
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
            if (Find(new Vector2(50, 50), new Vector2(322, 536), lstpath))
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
    //查找一个节点的所有邻居节点
    public List<Node> GetNeibourhood(Node node)
    {
        List<Node> neibourhood = new List<Node>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                    continue;
                int tempX = (int)node.coordinate.x + i;
                int tempY = (int)node.coordinate.y + j;
                if (tempX < 1024 && !(tempX < 0) && tempY < 1024 && !(tempY < 0))
                {
                    Vector2 vector2 = new Vector2(tempX, tempY);
                    neibourhood.Add(new Node(vector2, isWalkable(vector2)));
                }
            }
        }
        return neibourhood;
    }

    //估价函数h(n)
    public int getDistanceNodes(Node a, Node b)
    {
        int cntX = Mathf.Abs((int)a.coordinate.x - (int)b.coordinate.x);
        int cntY = Mathf.Abs((int)a.coordinate.y - (int)b.coordinate.y);
        if (cntX >= cntY)
            return 14 * cntY + 10 * (cntX - cntY);
        else
            return 14 * cntX + 10 * (cntY - cntX);
    }

    /*
     * 堆
     */
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

}
