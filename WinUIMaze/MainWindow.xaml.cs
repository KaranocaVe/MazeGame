using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Microsoft.UI.Xaml.Media.Imaging;
using Priority_Queue;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using SkiaSharp;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIMaze;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow
{
    public MainWindow()
    {
        AppWindow.SetIcon("240_F_184746023_GxYE38cPTUo151GePkK3zSR2JJDgRwmX.ico");
        this.InitializeComponent(); // 初始化组件
        this.ExtendsContentIntoTitleBar = true; // 扩展内容到标题栏
        this.SetTitleBar(CustomTitleBar); // 设置自定义标题栏
        this.SystemBackdrop = new MicaBackdrop(); // 设置系统背景为Mica
        // this.AppWindow.Resize(new SizeInt32(800, 860)); // 调整窗口大小
        SetAdaptiveWindowSize(); // 设置自适应窗口大小
        Page.KeyDown += KeyboradControl; // 添加键盘控制事件
    }

    private void SetAdaptiveWindowSize()
    {
        // 获取当前窗口的 DPI 缩放比例
        double dpiScaling = GetDpiScalingFactor();

        // 目标窗口大小（以缩放前的逻辑像素为基准）
        int targetWidth = (int)(600 * dpiScaling);
        int targetHeight = (int)(645 * dpiScaling);

        // 获取当前窗口的 AppWindow 对象
        AppWindow appWindow = GetAppWindowForCurrentWindow();
        appWindow.Resize(new Windows.Graphics.SizeInt32(targetWidth, targetHeight));
    }

    private double GetDpiScalingFactor()
    {
        // 获取当前窗口句柄
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // 调用 Win32 API 获取 DPI
        uint dpi = GetDpiForWindow(hwnd);

        // 将 DPI 转换为缩放比例（96 为默认 DPI）
        return dpi / 96.0;
    }

    private AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    [DllImport("User32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);


    private DrawingManager? _drawingManager; // 绘图管理器

    private void InitializeDrawingManager()
    {
        _drawingManager?.Dispose();
        _drawingManager = new DrawingManager(_canvasWidth, _canvasHeight); // 初始化绘图管理器
    }

    private void DisplayMap()
    {
        var bitmap = _drawingManager?.GetBitmap(); // 获取位图
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        var bitmapImage = new BitmapImage();
        bitmapImage.SetSource(stream.AsRandomAccessStream());
        ImageBox.Source = bitmapImage; // 设置图像框的源
    }
// 存储迷宫区块的列表
    private List<Chunk?> _grid = new();

// 当前处理的区块
    private Chunk? _current;

// 存储区块的栈
    private readonly Stack<Chunk?> _stack = new();

// 画布宽度
    private int _canvasWidth;

// 画布高度
    private int _canvasHeight;

// 迷宫区块宽度
    private int _mazeChunkWidth;

// 迷宫区块高度
    private int _mazeChunkHeight;

// 迷宫列数
    private int _cols;

// 迷宫行数
    private int _rows;

// 随机数生成器
    private readonly Random _randomGenerator = new();

// 存储路径的列表
    private readonly List<Chunk?> _path =
    [
    ];

// 初始化迷宫网格
    private void InitializeGrid()
    {
        // 清空网格、栈、路径和游戏栈
        _grid.Clear();
        _stack.Clear();
        _path.Clear();
        _gameStack.Clear();

        _canvasHeight =(int) CanvasGrid.ActualHeight;
        _canvasWidth =(int) CanvasGrid.ActualWidth;

        // 根据画布大小和滑块值设置行列数和区块大小
        if (_canvasWidth < _canvasHeight)
        {
            _rows = (int)SizeSlider.Value;
            _mazeChunkHeight = _canvasHeight / _rows;
            _mazeChunkWidth = _mazeChunkHeight;
            _cols = _canvasWidth / _mazeChunkWidth;
        }
        else
        {
            _cols = (int)SizeSlider.Value;
            _mazeChunkWidth = _canvasWidth / _cols;
            _mazeChunkHeight = _mazeChunkWidth;
            _rows = _canvasHeight / _mazeChunkHeight;
        }

        _canvasWidth = _cols * _mazeChunkWidth; // 调整画布宽度
        _canvasHeight = _rows * _mazeChunkHeight; // 调整画布高度

        // 创建区块并添加到网格中
        for (var y = 0; y < _rows; y++)
        {
            for (var x = 0; x < _cols; x++)
            {
                _grid.Add(new Chunk(x, y));
            }
        }

        // 设置第一个区块为当前区块并标记为已访问
        _current = _grid[0];
        if (_current != null)
        {
            _current.Visited = true;
        }
    }
    private readonly DispatcherTimer _timer = new DispatcherTimer(); // 定时器
    private bool _isTimerTickSet; // 定时器是否已设置

    private bool _isTipShowed; // 是否显示提示
    private void GenerateMaze(object sender, RoutedEventArgs e)
    {
        if (_isTipShowed == false)
        {
            GameStart.IsOpen = true; // 显示游戏开始提示
            _isTipShowed = true; // 标记提示已显示
        }
        _lastStep = null; // 重置上一步
        _thisStep = null; // 重置当前步
        _isGameStarted = false; // 标记游戏未开始
        _path.Clear(); // 清空路径
        _gameStack.Clear(); // 清空游戏栈
        InitializeGrid(); // 初始化网格
        InitializeDrawingManager(); // 初始化绘图管理器
        if (Math.Abs(SpeedSlider.Value - SpeedSlider.Maximum) < 0.1)
        {
            StaticDraw(); // 静态绘制迷宫
        }
        else
        {
            DynamicDraw(); // 动态绘制迷宫
        }
    }

    private void StaticDraw()
    {
        while (true)
        {
            var next = _current?.GetNeighbour(ref _grid, _cols, _rows, _randomGenerator); // 获取下一个邻居
            if (next != null)
            {
                next.Visited = true; // 标记邻居为已访问
                _stack.Push(_current); // 当前区块入栈
                Chunk.RemoveWalls(_current, next); // 移除墙壁
                _current = next; // 更新当前区块
            }
            else if (_stack.Count > 0)
            {
                _current = _stack.Pop(); // 从栈中弹出上一个区块
            }
            else
            {
                break; // 迷宫生成完成
            }
        }

        foreach (var t in _grid)
        {
            t?.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black); // 绘制区块
        }
        DisplayMap(); // 显示迷宫
    }
    private void DynamicDraw()
    {
        if (_timer.IsEnabled) // 如果定时器已启用
        {
            var nextChunk = _current?.GetNeighbour(ref _grid, _cols, _rows, _randomGenerator); // 获取下一个邻居区块

            if (nextChunk == null && _stack.Count == 0) // 如果没有下一个邻居且栈为空
            {
                _timer.Stop(); // 停止定时器
            }

            _current?.HighlightChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Red); // 突出显示当前区块
            DisplayMap(); // 显示地图
            _current?.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black); // 绘制当前区块

            if (_current == _grid[0]) // 如果当前区块是第一个区块
            {
                _current?.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black); // 绘制当前区块
                DisplayMap(); // 显示地图
            }
            if (nextChunk != null) // 如果有下一个邻居区块
            {
                nextChunk.Visited = true; // 标记邻居区块为已访问
                _stack.Push(_current); // 当前区块入栈
                Chunk.RemoveWalls(_current, nextChunk); // 移除墙壁
                _current = nextChunk; // 更新当前区块
            }
            else if (_stack.Count > 0) // 如果栈不为空
            {
                _current = _stack.Pop(); // 从栈中弹出上一个区块
            }
        }
        else // 如果定时器未启用
        {
            _timer.Interval = TimeSpan.FromMilliseconds(1000 - 10 * SpeedSlider.Value); // 设置定时器间隔
            if (!_isTimerTickSet)
            {
                _timer.Tick += (sender, e) => DynamicDraw(); // 定时器触发时调用DynamicDraw方法
                _isTimerTickSet = true; // 标记定时器已设置
            }
            _timer.Start(); // 启动定时器
        }
    }

    private readonly DispatcherTimer _pathTimer = new(); // 路径定时器
    private bool _isPathTimerTickSet; // 路径定时器是否已设置
    private void DrawPath(object? sender, RoutedEventArgs? e)
    {
        switch (PathAlgorithmChose.SelectedIndex)
        {
            case (0):
                if (_pathTimer.IsEnabled)
                {
                    if (_path.Count > 0)
                    {
                        // 突出显示路径中的第一个区块
                        // _path[0].HighlightChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColor.FromArgb(100, 61, 181, 54));
                        _path[0]?.HighlightChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Yellow);
                        DisplayMap();
                        // 从路径中移除第一个区块
                        _path.RemoveAt(0);
                    }
                    else
                    {
                        // 停止路径定时器
                        _pathTimer.Stop();
                    }
                }
                else
                {
                    // 清空路径
                    _path.Clear();
                    // 使用Dijkstra算法计算路径
                    Dijkstra();
                    // 设置路径定时器的间隔
                    _pathTimer.Interval = TimeSpan.FromMilliseconds(1000 - 10 * SpeedSlider.Value);
                    // 路径定时器触发时调用DrawPath方法
                    if (!_isPathTimerTickSet)
                    {
                        _pathTimer.Tick += (_, _) => DrawPath(null, null);
                        _isPathTimerTickSet = true;
                    }
                    // 启动路径定时器
                    _pathTimer.Start();
                }
                break;

            case (1):
                if (_pathTimer.IsEnabled)
                {
                    if (_path.Count > 0)
                    {
                        // 突出显示路径中的第一个区块
                        _path[0]?.HighlightChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Yellow);
                        DisplayMap();
                        // 从路径中移除第一个区块
                        _path.RemoveAt(0);
                    }
                    else
                    {
                        // 停止路径定时器
                        _pathTimer.Stop();
                    }
                }
                else
                {
                    // 清空路径
                    _path.Clear();
                    // 使用DFS算法计算路径
                    Dfs();
                    // 设置路径定时器的间隔
                    _pathTimer.Interval = TimeSpan.FromMilliseconds(1000 - 10 * SpeedSlider.Value);
                    // 路径定时器触发时调用DrawPath方法
                    if (!_isPathTimerTickSet)
                    {
                        _pathTimer.Tick += (_, _) => DrawPath(null, null);
                        _isPathTimerTickSet = true;
                    }
                    // 启动路径定时器
                    _pathTimer.Start();
                }
                break;

            case (2):
                if (_pathTimer.IsEnabled)
                {
                    if (_path.Count > 0)
                    {
                        // 突出显示路径中的第一个区块
                        _path[0]?.HighlightChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Yellow);
                        DisplayMap();
                        // 从路径中移除第一个区块
                        _path.RemoveAt(0);
                    }
                    else
                    {
                        // 停止路径定时器
                        _pathTimer.Stop();
                    }
                }
                else
                {
                    // 清空路径
                    _path.Clear();
                    // 使用BFS算法计算路径
                    Bfs();
                    // 设置路径定时器的间隔
                    _pathTimer.Interval = TimeSpan.FromMilliseconds(1000 - 10 * SpeedSlider.Value);
                    if (!_isPathTimerTickSet)
                    {
                        _pathTimer.Tick += (_, _) => DrawPath(null, null);
                        _isPathTimerTickSet = true;
                    }
                    // 启动路径定时器
                    _pathTimer.Start();
                }
                break;
        }
    }

    // 判断当前区块与邻居区块之间是否有路径
    private static bool HavePathToNeighbour(Chunk current, Chunk neighbour)
    {
        var x = current.X - neighbour.X; // 计算X轴上的差值
        var y = current.Y - neighbour.Y; // 计算Y轴上的差值

        // 判断是否存在路径
        return (x == 1 && !current.Walls[2] && !neighbour.Walls[3]) || // 当前区块左边与邻居区块右边无墙
               (x == -1 && !current.Walls[3] && !neighbour.Walls[2]) || // 当前区块右边与邻居区块左边无墙
               (y == 1 && !current.Walls[0] && !neighbour.Walls[1]) || // 当前区块上边与邻居区块下边无墙
               (y == -1 && !current.Walls[1] && !neighbour.Walls[0]); // 当前区块下边与邻居区块上边无墙
    }
    private void Dijkstra()
    {
        var start = _grid[0]; // 起始区块
        var end = _grid.Last(); // 终止区块

        var visited = new bool[_grid.Count]; // 访问标记数组

        var distance = Enumerable.Repeat(double.PositiveInfinity, _grid.Count).ToArray(); // 距离数组，初始为正无穷

        var unvisited = new SimplePriorityQueue<Chunk?>(); // 未访问区块的优先队列
        var chunkIndex = new Chunk?[_grid.Count]; // 区块索引数组

        for (var i = 0; i < _grid.Count; i++)
        {
            chunkIndex[i] = null; // 初始化区块索引数组
        }

        Chunk?[] previous = new Chunk[_grid.Count]; // 前驱区块数组

        for (var i = 0; i < _grid.Count; i++)
        {
            previous[i] = null; // 初始化前驱区块数组
        }

        distance[0] = 0; // 起始区块距离为0
        chunkIndex[0] = start; // 起始区块索引
        unvisited.Enqueue(start, 0); // 将起始区块加入优先队列

        while (unvisited.Count > 0)
        {
            var temp = unvisited.Dequeue(); // 取出优先队列中优先级最高的区块
            var temposIndex = temp.X + temp.Y * _cols; // 计算区块索引
            if (double.IsPositiveInfinity(distance[temposIndex])
                || (temp.X == end.X && temp.Y == end.Y))
            {
                break; // 如果距离为正无穷或到达终止区块，退出循环
            }

            foreach (var t in temp.Neighbors) // 遍历邻居区块
            {
                if (!HavePathToNeighbour(temp, t))
                {
                    continue; // 如果没有路径到邻居区块，跳过
                }
                var vposIndex = t.X + t.Y * _cols; // 计算邻居区块索引
                if (visited[vposIndex])
                {
                    continue; // 如果邻居区块已访问，跳过
                }
                double d = Math.Abs(temp.X - t.X) + Math.Abs(temp.Y - t.Y); // 计算距离
                var newDistance = distance[temposIndex] + d; // 计算新距离
                if (!(newDistance < distance[vposIndex]))
                {
                    continue; // 如果新距离不小于当前距离，跳过
                }
                var vChunk = chunkIndex[vposIndex];
                if (vChunk == null)
                {
                    vChunk = t; // 更新邻居区块
                    unvisited.Enqueue(vChunk, (float)newDistance); // 将邻居区块加入优先队列
                    chunkIndex[vposIndex] = vChunk; // 更新区块索引
                    distance[vposIndex] = newDistance; // 更新距离
                }
                else
                {
                    vChunk = t; // 更新邻居区块
                    unvisited.UpdatePriority(vChunk, (float)newDistance); // 更新优先队列中的优先级
                    distance[vposIndex] = newDistance; // 更新距离
                    vChunk.Parent = temp; // 设置父区块
                }
                previous[vposIndex] = temp; // 更新前驱区块
            }
            visited[temposIndex] = true; // 标记当前区块为已访问
        }
        var current = end; // 从终止区块开始

        while (current != null)
        {
            _path.Add(current); // 将区块加入路径
            current = previous[current.X + current.Y * _cols]; // 追溯前驱区块
        }
        _path.Reverse(); // 反转路径
    }

    private void Dfs()
    {
        var start = _grid[0]; // 起始区块
        var end = _grid.Last(); // 终止区块

        var st = new Stack<Chunk?>(); // 创建栈用于存储区块

        var visited = new bool[_grid.Count]; // 访问标记数组
        Chunk?[] previous = new Chunk[_grid.Count]; // 前驱区块数组
        for (var i = 0; i < _grid.Count; i++)
        {
            previous[i] = null; // 初始化前驱区块数组
        }

        st.Push(start); // 将起始区块压入栈

        while (st.Count > 0)
        {
            var current = st.Pop(); // 从栈中弹出当前区块

            if (current == end)
            {
                break; // 如果当前区块是终止区块，退出循环
            }

            visited[current.X + current.Y * _cols] = true; // 标记当前区块为已访问

            foreach (var t in current.Neighbors) // 遍历当前区块的邻居
            {
                if (!HavePathToNeighbour(current, t))
                {
                    continue; // 如果没有路径到邻居区块，跳过
                }
                var vposIndex = t.X + t.Y * _cols; // 计算邻居区块的索引
                if (visited[vposIndex])
                {
                    continue; // 如果邻居区块已访问，跳过
                }
                st.Push(t); // 将邻居区块压入栈
                previous[vposIndex] = current; // 设置邻居区块的前驱为当前区块
            }
        }

        var current1 = end; // 从终止区块开始

        while (current1 != null)
        {
            _path.Add(current1); // 将区块加入路径
            current1 = previous[current1.X + current1.Y * _cols]; // 追溯前驱区块
        }
        _path.Reverse(); // 反转路径
    }
    private void Bfs()
    {
        var start = _grid[0]; // 起始区块
        var end = _grid.Last(); // 终止区块

        var unvisited = new Queue<Chunk?>(); // 未访问区块的队列
        var visited = new bool[_grid.Count]; // 访问标记数组
        var previous = new Chunk?[_grid.Count]; // 前驱区块数组
        for (var i = 0; i < _grid.Count; i++)
        {
            previous[i] = null; // 初始化前驱区块数组
        }

        unvisited.Enqueue(start); // 将起始区块加入队列
        visited[start.X + start.Y * _cols] = true; // 标记起始区块为已访问

        while (unvisited.Count > 0)
        {
            var current = unvisited.Dequeue(); // 从队列中取出当前区块

            if (current == end)
            {
                break; // 如果当前区块是终止区块，退出循环
            }

            foreach (var t in current.Neighbors) // 遍历当前区块的邻居
            {
                if (!HavePathToNeighbour(current, t))
                {
                    continue; // 如果没有路径到邻居区块，跳过
                }
                var vposIndex = t.X + t.Y * _cols; // 计算邻居区块的索引
                if (visited[vposIndex])
                {
                    continue; // 如果邻居区块已访问，跳过
                }
                unvisited.Enqueue(t); // 将邻居区块加入队列
                visited[vposIndex] = true; // 标记邻居区块为已访问
                previous[vposIndex] = current; // 设置邻居区块的前驱为当前区块
            }
        }

        var current1 = end; // 从终止区块开始

        while (current1 != null)
        {
            _path.Add(current1); // 将区块加入路径
            current1 = previous[current1.X + current1.Y * _cols]; // 追溯前驱区块
        }
        _path.Reverse(); // 反转路径
    }
// 游戏是否开始的标志
    private bool _isGameStarted = false;

// 存储游戏区块的栈
    private readonly Stack<Chunk?> _gameStack = new();

// 当前处理的区块
    private Chunk? _thisStep;
// 上一步处理的区块
    private Chunk? _lastStep;
// 键盘控制方法
    private void KeyboradControl(object sender, KeyRoutedEventArgs e)
    {

        if (_isGameStarted)
        {
            switch (e.Key)
            {
                case VirtualKey.W:
                    // 上移
                    if (_thisStep.Walls[0])
                    {
                        return;
                    }
                    if (_thisStep.Y > 0)
                    {
                        if (_lastStep == _grid[_thisStep.X + (_thisStep.Y - 1) * _cols])
                        {
                            _thisStep.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black);
                            _thisStep = _lastStep;
                            _lastStep = _gameStack.Pop();
                        }
                        else
                        {
                            _gameStack.Push(_lastStep);
                            _lastStep = _thisStep;
                            _thisStep = _grid[_thisStep.X + (_thisStep.Y - 1) * _cols];
                            _thisStep.DrawStep(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Pink);
                        }
                        DisplayMap();
                    }
                    Check();
                    break;

                case VirtualKey.S:
                    // 下移
                    if (_thisStep.Walls[1])
                    {
                        return;
                    }
                    if (_thisStep.Y < _rows - 1)
                    {
                        if (_lastStep == _grid[_thisStep.X + (_thisStep.Y + 1) * _cols])
                        {
                            _thisStep.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black);
                            _thisStep = _lastStep;
                            _lastStep = _gameStack.Pop();
                        }
                        else
                        {
                            _gameStack.Push(_lastStep);
                            _lastStep = _thisStep;
                            _thisStep = _grid[_thisStep.X + (_thisStep.Y + 1) * _cols];
                            _thisStep.DrawStep(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Pink);
                        }
                        DisplayMap();
                    }
                    Check();
                    break;

                case VirtualKey.A:
                    // 左移
                    if (_thisStep.Walls[2])
                    {
                        return;
                    }
                    if (_thisStep.X > 0)
                    {
                        if (_lastStep == _grid[_thisStep.X - 1 + _thisStep.Y * _cols])
                        {
                            _thisStep.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black);
                            _thisStep = _lastStep;
                            _lastStep = _gameStack.Pop();
                        }
                        else
                        {
                            _gameStack.Push(_lastStep);
                            _lastStep = _thisStep;
                            _thisStep = _grid[_thisStep.X - 1 + _thisStep.Y * _cols];
                            _thisStep?.DrawStep(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Pink);
                        }
                        DisplayMap();
                    }
                    Check();
                    break;

                case VirtualKey.D:
                    // 右移
                    if (_thisStep.Walls[3])
                    {
                        return;
                    }
                    if (_thisStep.X < _cols - 1)
                    {
                        if (_lastStep == _grid[_thisStep.X + 1 + _thisStep.Y * _cols])
                        {
                            _thisStep.DrawChunk(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Black);
                            _thisStep = _lastStep;
                            _lastStep = _gameStack.Pop();
                        }
                        else
                        {
                            _gameStack.Push(_lastStep);
                            _lastStep = _thisStep;
                            _thisStep = _grid[_thisStep.X + 1 + _thisStep.Y * _cols];
                            _thisStep?.DrawStep(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Pink);
                        }
                        DisplayMap();
                    }
                    Check();
                    DisplayMap();
                    break;
            }
        }
        else
        {
            _isGameStarted = true;
            _gameStack.Clear();
            _gameStack.Push(_grid[0]);
            _thisStep = _grid[0];
            _lastStep = null;
            _thisStep?.DrawStep(_drawingManager, _mazeChunkWidth, _mazeChunkHeight, SKColors.Pink);
            DisplayMap();
        }
        return;

        // 检查是否到达终点
        void Check()
        {
            if (_thisStep == _grid.Last())
            {
                _isGameStarted = false; // 标记游戏结束
                _gameStack.Clear(); // 清空游戏栈
                GameEnd.IsOpen = true; // 显示游戏结束提示
            }
        }
    }

// 游戏结束提示按钮点击事件
    private void GameEnd_OnActionButtonClick(TeachingTip sender, object args)
    {
        GenerateMaze(new object(), new RoutedEventArgs());
    }
    private void ExitGame(TeachingTip sender, object args)
    {
        Application.Current.Exit();
    }
}
