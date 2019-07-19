建议使用unity2018.3打开，
打开SampleScene.unity场景
点击运行按钮
点击Find Example
观察结果

1 作业需要完善Find函数的实现
	public bool Find(Vector2 begin, Vector2 end, List<Vector2> lstPath)
2 TestFind只是演示用的函数，内部并没有实现完整的搜索算法，
3 DrawButton 显示按钮
4 DrawDebug 显示地图和路径
5 isWalkable 判断某个点是否可以行走
6 LoadData 加载寻路地图（一张tif图片）