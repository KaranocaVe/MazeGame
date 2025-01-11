using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI;
using SkiaSharp;

namespace WinUIMaze;

public class Chunk(int x, int y)
{
    // 迷宫区块类

    // 区块坐标
    public int X
    {
        get;
    } = x;
    public int Y
    {
        get;
    } = y;

    // 区块的四个墙壁
    // 分别表示上、下、左、右
    // 初始状态下所有墙壁都存在
    public bool[] Walls
    {
        get;
    } =
    [
        true, true, true, true
    ];

    // 区块是否被访问过
    public bool Visited
    {
        get;
        set;
    }

    //区块的邻居列表
    public List<Chunk?> Neighbors
    {
        get;
    } = [];

    // 区块的父区块
    public Chunk Parent
    {
        get;
        set;
    }

    // A*算法的F值（用于选择下一个节点的依据，最小化函数）
    public double F
    {
        get;
        set;
    }
    // A*算法的G值（从起点到该节点的路径长度）
    public double G
    {
        get;
        set;
    }
    // A*算法的H值（从该节点到终点的启发式路径长度）
    public double H
    {
        get;
        set;
    }

    // 删除指定方向的墙壁
    public void DeleteWall(int index)
    {
        Walls[index] = false;
    }

    // 获取区块的未访问邻居
    public Chunk? GetNeighbour(ref List<Chunk?> grid, int cols, int rows, Random randomGen)
    {
        List<Chunk> tempNeighbour = new();

        // 计算区块的索引
        int? Index(int x, int y)
        {
            if (x < 0 || y < 0 || x > cols - 1 || y > rows - 1)
            {
                return null;
            }
            return x + y * cols;
        }

        // 用于操作邻居的数组
        // 如果没有邻居则值为null
        // 对于迷宫边缘的区块是必要的
        int?[] allAroundChunk = [Index(X, Y - 1), Index(X + 1, Y), Index(X, Y + 1), Index(X - 1, Y)];

        // 如果邻居存在且未被访问，将其添加到可用邻居列表中
        foreach (var flag in allAroundChunk)
        {
            if (flag is not { } index || grid[index] is not { Visited: false } neighbour)
            {
                continue;
            }
            tempNeighbour.Add(neighbour);
            Neighbors.Add(neighbour);
        }

        // 随机选择一个邻居，该邻居将被访问并成为起始区块
        if (tempNeighbour.Count == 0)
        {
            return null;
        }
        return tempNeighbour[randomGen.Next(0, tempNeighbour.Count)];
    }

    public void DrawChunk(DrawingManager drawingManager, int chunkWidth, int chunkHeight, SKColor color)
    {
        var x = X * chunkWidth;
        var y = Y * chunkHeight;

        // 访问过的区块填充颜色
        if (Visited)
        {
            drawingManager.RegisterDrawAction((drawingSession) =>
            {

                drawingSession.DrawRect(x, y, chunkWidth, chunkHeight, new SKPaint()
                {
                    Color = SKColors.White,
                    Style = SKPaintStyle.Fill
                });
            });
        }

        // 绘制区块的墙壁
        for (var i = 0; i < Walls.Length; i++)
        {
            if (Walls[i])
            {
                var i1 = i;
                drawingManager.RegisterDrawAction((drawingSession) =>
                {
                    if (i1 == 0)
                    {
                        // 上墙
                        drawingSession.DrawLine(x, y, x + chunkWidth, y, new SKPaint(){Color = color, StrokeWidth = 2});
                    }
                    else if (i1 == 1)
                    {
                        // 下墙
                        drawingSession.DrawLine(x, y + chunkHeight, x + chunkWidth, y + chunkHeight, new SKPaint(){Color = color, StrokeWidth = 2});
                    }
                    else if (i1 == 2)
                    {
                        // 左墙
                        drawingSession.DrawLine(x, y, x, y + chunkHeight, new SKPaint(){Color = color, StrokeWidth = 2});
                    }
                    else if (i1 == 3)
                    {
                        // 右墙
                        drawingSession.DrawLine(x + chunkWidth, y, x + chunkWidth, y + chunkHeight, new SKPaint(){Color = color, StrokeWidth = 2});
                    }
                });
            }
        }

        //  绘制
        drawingManager.Draw();
    }

    // 绘制每一步
    public void DrawStep(DrawingManager? drawingManager, int chunkWidth, int chunkHeight, SKColor color)
    {
        var x = X * chunkWidth;
        var y = Y * chunkHeight;

        drawingManager?.RegisterDrawAction((drawingSession) =>
        {
            drawingSession.DrawRect(x + 5, y + 5, chunkWidth - 10, chunkHeight - 10, new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill
            });
        });
        drawingManager?.Draw();
    }


    // 在迷宫创建动画期间突出显示栈顶的节点
    public void HighlightChunk(DrawingManager drawingManager, int chunkWidth, int chunkHeight, SKColor color)
    {
        var x = X * chunkWidth;
        var y = Y * chunkHeight;

        drawingManager.RegisterDrawAction((drawingSession) =>
        {
            drawingSession.DrawRect(x + 2, y + 2, chunkWidth - 4, chunkHeight - 4, new SKPaint()
            {
                Color = color,
                Style = SKPaintStyle.Fill
            });
        });
        drawingManager.Draw();
    }

    public static void RemoveWalls(Chunk? current, Chunk next)
    {
        var x = current.X - next.X;
        switch (x)
        {
            case 1:
                current.DeleteWall(2);
                next.DeleteWall(3);
                break;
            case -1:
                current.DeleteWall(3);
                next.DeleteWall(2);
                break;
        }
        var y = current.Y - next.Y;
        switch (y)
        {
            case 1:
                current.DeleteWall(0);
                next.DeleteWall(1);
                break;
            case -1:
                current.DeleteWall(1);
                next.DeleteWall(0);
                break;
        }
    }
}
