using UnityEngine;

public class PingPongTexture
{
    private RenderTexture a;
    private RenderTexture b;
    private bool pingIsA = true;

    public RenderTexture Ping => pingIsA ? a : b;
    public RenderTexture Pong => pingIsA ? b : a;
    public RenderTextureFormat Format { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public PingPongTexture(int width, int height, RenderTextureFormat format = RenderTextureFormat.ARGBFloat)
    {
        Width = width;
        Height = height;
        Format = format;
        a = CreateTexture(width, height, format);
        b = CreateTexture(width, height, format);
    }

    public void Swap()
    {
        pingIsA = !pingIsA;
    }

    public void Resize(int newWidth, int newHeight)
    {
        Width = newWidth;
        Height = newHeight;
        Release();
        a = CreateTexture(newWidth, newHeight, Format);
        b = CreateTexture(newWidth, newHeight, Format);
    }

    public void Release()
    {
        if (a != null) { a.Release(); a = null; }
        if (b != null) { b.Release(); b = null; }
    }

    private static RenderTexture CreateTexture(int w, int h, RenderTextureFormat format)
    {
        var rt = new RenderTexture(w, h, 0, format);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
}
