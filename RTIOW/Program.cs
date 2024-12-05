using ConsoleAppFramework;

namespace RTIOW;

internal class Program
{
	/// <summary>
	/// Runs the raytracer
	/// </summary>
	/// <param name="width"> -w, Width of the image </param>
	/// <param name="height"> -h, Height of the image </param>
	/// <param name="renderScale"> -s, Scale o the render</param>
	private static unsafe void Run(int width = 800, int height = 800, int renderScale = 1)
	{
		using var window = new RTIOWWindow(width, height, renderScale);
		window.Run();
	}

	public static void Main(string[] args) =>
		ConsoleApp.Run(args, Run);
}