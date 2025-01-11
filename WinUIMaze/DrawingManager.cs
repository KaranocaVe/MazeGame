using System;
using System.Collections.Generic;
using SkiaSharp;

namespace WinUIMaze;

public partial class DrawingManager : IDisposable
{
    // 用于绘制的位图
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    // 绘图任务列表
    private readonly List<Action<SKCanvas>> _drawActions =
    [
    ];

    public DrawingManager(int width, int height)
    {
        // 创建位图和画布
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.White); // 初始化为白色背景
    }

    // 注册绘图任务
    public void RegisterDrawAction(Action<SKCanvas> action)
    {
        _drawActions.Add(action);
    }

    // 执行绘图任务
    public void Draw()
    {
        foreach (var action in _drawActions)
        {
            action(_canvas);
        }

        _drawActions.Clear();
    }

    // 获取当前绘制结果
    public SKBitmap GetBitmap()
    {
        return _bitmap;
    }

    // 保存为图片文件
    public void SaveToFile(string filePath)
    {
        using var image = SKImage.FromBitmap(_bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = System.IO.File.OpenWrite(filePath);
        data.SaveTo(stream);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }
}
