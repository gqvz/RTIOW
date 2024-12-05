using System.Numerics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RTIOW;

internal class RTIOWWindow : PixelWindow.PixelWindow
{
    private readonly int _renderScale;

    private byte[] _render;
    private byte[] _render2;
    private readonly byte[] _image;
    private readonly ulong[] _blockHashes;
    private readonly ulong[] _previousBlockHashes;

    public RTIOWWindow(int width, int height, int renderScale) : base(width, height)
    {
        _renderScale = renderScale;
        var numBlocks = width * height;
        _render = new byte[width * height * 4 * _renderScale * _renderScale];
        _render2 = new byte[width * height * 4 * _renderScale * _renderScale];
        _image = new byte[width * height * 4];
        _blockHashes = new ulong[numBlocks];
        _previousBlockHashes = new ulong[numBlocks];
    }

    protected override unsafe void OnLoad()
    {
        base.OnLoad();

        // initialize a gradient in _render
        var t = new Thread(_ =>
        {
            for (var y = 0; y < ClientSize.Y * _renderScale; y++)
            {
                for (var x = 0; x < ClientSize.X * _renderScale; x++)
                {
                    var r = (byte)(255 * x / (ClientSize.X * _renderScale));
                    var g = (byte)(255 * y / (ClientSize.Y * _renderScale));
                    byte b = 0;
                    byte a = 255;

                    var index = (y * ClientSize.X * _renderScale + x) * 4;
                    _render[index] = r;
                    _render[index + 1] = g;
                    _render[index + 2] = b;
                    _render[index + 3] = a;
                    _render2[index] = b;
                    _render2[index + 1] = g;
                    _render2[index + 2] = r;
                    _render2[index + 3] = a;
                }
            }
        });
        t.Start();

        // Set the pointer for PixelWindow's data
        fixed (byte* imagePtr = _image)
            Data = imagePtr;
    }

    protected override unsafe void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Check for changes in `_render` and update `_image`
        UpdateImage();

        // Trigger a texture update for rendering
        UpdateTexture();

        if (KeyboardState.IsKeyDown(Keys.Escape))
            _render = _render2;
    }

    private void UpdateImage()
    {
        var unscaledWidth = ClientSize.X;
        var unscaledHeight = ClientSize.Y;
        var scaledRowStride = unscaledWidth * _renderScale * 4;

        for (var y = 0; y < unscaledHeight; y++)
        {
            for (var x = 0; x < unscaledWidth; x++)
            {
                // Compute the hash for the current block
                var blockIndex = y * unscaledWidth + x;
                var hash = ComputeBlockHash(x, y, scaledRowStride);

                // Check if the block has changed
                if (hash == _previousBlockHashes[blockIndex]) continue;

                // Update the block if it has changed
                _previousBlockHashes[blockIndex] = hash;

                var sums = Vector4.Zero;
                for (var ys = 0; ys < _renderScale; ys++)
                {
                    var rowStart = ((y * _renderScale + ys) * scaledRowStride) + x * _renderScale * 4;

                    for (var xs = 0; xs < _renderScale; xs++)
                    {
                        var pixelStart = rowStart + xs * 4;

                        sums.X += _render[pixelStart];       // Red
                        sums.Y += _render[pixelStart + 1];   // Green
                        sums.Z += _render[pixelStart + 2];   // Blue
                        sums.W += _render[pixelStart + 3];   // Alpha
                    }
                }

                var blockSize = _renderScale * _renderScale;
                var unscaledIndex = (y * unscaledWidth + x) * 4;

                _image[unscaledIndex] = (byte)(sums.X / blockSize);
                _image[unscaledIndex + 1] = (byte)(sums.Y / blockSize);
                _image[unscaledIndex + 2] = (byte)(sums.Z / blockSize);
                _image[unscaledIndex + 3] = (byte)(sums.W / blockSize);
            }
        }
    }

    private ulong ComputeBlockHash(int x, int y, int scaledRowStride)
    {
        var hash = 14695981039346656037UL; // FNV offset basis
        const ulong prime = 1099511628211UL; // FNV prime

        for (var ys = 0; ys < _renderScale; ys++)
        {
            var rowStart = ((y * _renderScale + ys) * scaledRowStride) + x * _renderScale * 4;

            for (var xs = 0; xs < _renderScale; xs++)
            {
                var pixelStart = rowStart + xs * 4;

                for (var i = 0; i < 4; i++)
                {
                    hash ^= _render[pixelStart + i];
                    hash *= prime;
                }
            }
        }

        return hash;
    }
}