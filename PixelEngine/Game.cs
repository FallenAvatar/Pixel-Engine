using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using PixelEngine.Extensions;
using PixelEngine.Utilities;

using static PixelEngine.Windows;

namespace PixelEngine {
	/// <summary> Fully functional Game boilerplate. </summary>
	public abstract class Game : Display {
		#region Members
		/// <summary> last x position of mouse </summary>
		public int MouseX { get; private set; }
		/// <summary> last y position of mouse </summary>
		public int MouseY { get; private set; }
		/// <summary> Current pixel blend mode </summary>
		public Pixel.Mode PixelMode { get; set; } = Pixel.Mode.Normal;
		/// <summary> Current Font </summary>
		public Font Font { get; set; }
		/// <summary> Alpha % to apply during alpha blend mode </summary>
		public float PixelBlend { get { return pixBlend; } set { pixBlend = Clamp(value, 0, 1); } }
		/// <summary> Current frame count </summary>
		public long FrameCount { get; private set; }
		/// <summary> Does this window have focus? </summary>
		public bool Focus { get; private set; }
		/// <summary> Does the mouse get clamped to the window? </summary>
		public bool ClampMouse { get; set; } = false;
		/// <summary> Current framerate </summary>
		public int FrameRate { get; private set; }
		/// <summary> Current mouse scroll setting  </summary>
		public Scroll MouseScroll { get; private set; }
		/// <summary> Clock since game start  </summary>
		public Clock Clock { get; private set; }
		/// <summary> Master audio volume </summary>
		public float Volume {
			get { return audio != null ? audio.Volume : 0; }
			set { if (audio != null) { audio.Volume = Clamp(value, 0, 1); } }
		}
		/// <summary> Time for audio sync </summary>
		public float AudioTime {
			get {
				if (audio == null) { return 0; }
				return audio.GlobalTime;
			}
		}
		/// <summary> Current pixel shader </summary>
		public Shader Shader {
			get { return shader; }
			set {
				shader = value;
				PixelMode = shader != null ? Pixel.Mode.Custom : Pixel.Mode.Normal;
			}
		}
		/// <summary> Current Draw target </summary>
		public Sprite DrawTarget {
			get { return drawTarget; }
			set { drawTarget = value ?? defDrawTarget; }
		}
		/// <summary> Window name </summary>
		public override string AppName {
			get { return base.AppName; }
			protected set {
				base.AppName = value;
				if (Handle != IntPtr.Zero) {
					SetWindowText(Handle, AppName);
				}
			}
		}

		/// <summary> Thread hosting the game loop </summary>
		private Thread gameLoop;

		/// <summary> Audio engine bound to this game </summary>
		private AudioEngine audio;
		/// <summary> Graphics object bound to this game </summary>
		private OpenGL canvas;

		/// <summary> field backing <see cref="PixelBlend"/> property </summary>
		private float pixBlend = 1;

		/// <summary> Timer for frame ticks </summary>
		private Timer frameTimer;

		/// <summary> Windows message processing handler </summary>
		private static WindowProcess proc;
		/// <summary> Mouse message processing handler  </summary>
		private static TimerProcess timeProc;

		/// <summary> Highres text </summary>
		private bool hrText;

		/// <summary> Game currently active? </summary>
		private bool active;
		/// <summary> Game currently paused? </summary>
		private bool paused;

		/// <summary> Keystate dictionary for fast lookup </summary>
		private readonly Dictionary<uint, Key> mapKeys = new Dictionary<uint, Key>();

		/// <summary> Current draw surface </summary>
		private Sprite drawTarget;
		/// <summary> Text drawing surface </summary>
		private Sprite textTarget;
		/// <summary> Default draw surface </summary>
		private Sprite defDrawTarget;

		/// <summary> Any key's state </summary>
		private Input anyKey;
		/// <summary> No key's state </summary>
		private Input noneKey;
		/// <summary> Any mouse button's state </summary>
		private Input anyMouse;
		/// <summary> No mouse button's state </summary>
		private Input noneMouse;

		/// <summary> Is the game currently delaying the next frame? </summary>
		private bool delaying;
		/// <summary> Time to wait before updating the next frame, if delaying. </summary>
		private float delayTime;

		/// <summary> Current pixel-shader function </summary>
		private Shader shader;

		/// <summary> Is the mouse outside of the window? </summary>
		private bool mouseOutOfWindow;
		
		/// <summary> Keyboard state snapshot for reading </summary>
		private readonly Input[] keyboard = new Input[256];
		/// <summary> Packed bitflags for current keyboard state </summary>
		private Bitflags newKeyboard = new Bitflags(256);
		/// <summary> Packed bitflags for previous keyboard state </summary>
		private Bitflags oldKeyboard = new Bitflags(256);

		/// <summary> Mouse state snapshot for reading </summary>
		private readonly Input[] mouse = new Input[3];
		/// <summary> Packed bitflags for current mouse state </summary>
		private IntFlags newMouse = 0;
		/// <summary> Packed bitflags for previous mouse state </summary>
		private IntFlags oldMouse = 0;
		#endregion

		#region Working
		/// <summary> Entry point for game </summary>
		public void Start() {
			RegisterClass();
			CreateWindow();

			active = true;

			gameLoop = new Thread(GameLoop);
			gameLoop.Start();

			MessagePump();
		}
		/// <summary> Constructs the window for the game </summary>
		/// <param name="width"> Width, in pixels. Default = 100 </param>
		/// <param name="height"> Height, in pixels. Default = 100</param>
		/// <param name="pixWidth"> Width in screen pixels of a single game pixel. Default = 5 </param>
		/// <param name="pixHeight"> Height in screen pixels of a single game pixel. Default = 5 </param>
		/// <param name="frameRate"> Target FrameRate, or -1 to run at sanic speed. Default = -1 </param>
		public void Construct(int width = 100, int height = 100, int pixWidth = 5, int pixHeight = 5, int frameRate = -1) {
			base.Construct(width, height, pixWidth, pixHeight);

			if (frameRate != -1) {
				FrameRate = frameRate;
				frameTimer = new Timer(1000.0f / FrameRate);
			}

			Font = Font.Presets.Retro;
			HandleDrawTarget();
		}

		/// <summary> Actual game loop. Runs in its own thread. </summary>
		private void GameLoop() {
			Clock = new Clock();

			Extension.Init(this);

			OnCreate();

			canvas = new OpenGL();
			canvas.Create(this);
			canvas.Initialize(defDrawTarget, textTarget);

			if (frameTimer != null) { frameTimer.Init(Clock.Start); }

			timeProc = MouseTimer;
			IntPtr timerProc = Marshal.GetFunctionPointerForDelegate<TimerProcess>(timeProc);
			SetTimer(Handle, TimerOne, 10, timerProc);
			
			while (active) {
				while (active) {
					
					if (frameTimer != null && !frameTimer.Tick()) {
						continue;
					}

					float delta = Clock.Delta;
					Clock.Tick();
					
					if (delaying) {
						delayTime -= delta;

						if (delayTime <= 0) {
							delayTime = 0;
							delaying = false;
						} else {
							continue;
						}
					}

					if (paused) { continue; }

					OnUpdate(delta);

					HandleKeyboard();
					HandleMouse();

					FrameCount++;

					canvas.Draw(defDrawTarget, textTarget);

				}

				OnDestroy();
			}

			if (audio != null) { audio.DestroyAudio(); }

			canvas.Destroy();

			PostMessage(Handle, (uint)WM.DESTROY, IntPtr.Zero, IntPtr.Zero);

			KillTimer(Handle, TimerOne);

			DestroyTempPath();
		}
		/// <summary> Update mouse states </summary>
		private void HandleMouse() {
			for (int i = 0; i < 3; i++) {
				mouse[i].Pressed = false;
				mouse[i].Released = false;

				if (newMouse[i] != oldMouse[i]) {
					if (newMouse[i]) {
						mouse[i].Pressed = !mouse[i].Down;
						mouse[i].Down = true;
					} else {
						mouse[i].Released = true;
						mouse[i].Down = false;
					}
				}

				if (mouse[i].Down) { OnMouseDown((Mouse)i); }

				oldMouse[i] = newMouse[i];
			}

			var swap = oldMouse;
			oldMouse = newMouse;
			newMouse = swap;
			
			anyMouse.Pressed = mouse.Any(m => m.Pressed);
			anyMouse.Down = mouse.Any(m => m.Down);
			anyMouse.Released = mouse.Any(m => m.Released);

			noneMouse.Pressed = !anyMouse.Pressed;
			noneMouse.Down = !anyMouse.Down;
			noneMouse.Released = !anyMouse.Released;

			MouseScroll = Scroll.None;
		}
		/// <summary> Update keyboard states </summary>
		private void HandleKeyboard() {
			for (int i = 0; i < 256; i++) {
				keyboard[i].Pressed = false;
				keyboard[i].Released = false;

				if (newKeyboard[i] != oldKeyboard[i]) {
					if (newKeyboard[i]) {
						keyboard[i].Pressed = !keyboard[i].Down;
						keyboard[i].Down = true;
					} else {
						keyboard[i].Released = true;
						keyboard[i].Down = false;
					}
				}

				if (keyboard[i].Down) { OnKeyDown((Key)i); }

				oldKeyboard[i] = newKeyboard[i];
			}

			anyKey.Pressed = keyboard.Any(k => k.Pressed);
			anyKey.Down = keyboard.Any(k => k.Down);
			anyKey.Released = keyboard.Any(k => k.Released);

			noneKey.Pressed = !anyKey.Pressed;
			noneKey.Down = !anyKey.Down;
			noneKey.Released = !anyKey.Released;
		}
		/// <summary> Resets draw target to default draw target. </summary>
		private void HandleDrawTarget() {
			defDrawTarget = new Sprite(ScreenWidth, ScreenHeight);
			DrawTarget = defDrawTarget;
		}
		/// <summary> Windows message handler </summary>
		private protected override IntPtr WndProc(IntPtr handle, uint msg, int wParam, int lParam) {
			switch (msg) {
				case (uint)WM.SETFOCUS:
					Focus = true;
					break;
				case (uint)WM.KILLFOCUS:
					Focus = false;
					break;
				case (uint)WM.KEYDOWN:
					if (!mapKeys.ContainsKey((uint)wParam))
						break;
					Key kd = mapKeys[(uint)wParam];
					newKeyboard[(int)kd] = true;
					OnKeyPress(kd);
					break;
				case (uint)WM.KEYUP:
					if (!mapKeys.ContainsKey((uint)wParam))
						break;
					Key ku = mapKeys[(uint)wParam];
					newKeyboard[(int)ku] = false;
					OnKeyRelease(ku);
					break;
				case (uint)WM.MOUSEWHEEL:
					short wheel = (short)(wParam >> 16);
					MouseScroll = (Scroll)(wheel / WheelDelta);
					OnMouseScroll();
					break;
				case (uint)WM.LBUTTONDOWN:
					newMouse[(int)Mouse.Left] = true;
					OnMousePress(Mouse.Left);
					break;
				case (uint)WM.LBUTTONUP:
					newMouse[(int)Mouse.Left] = false;
					OnMouseRelease(Mouse.Left);
					break;
				case (uint)WM.RBUTTONDOWN:
					newMouse[(int)Mouse.Right] = true;
					OnMousePress(Mouse.Right);
					break;
				case (uint)WM.RBUTTONUP:
					newMouse[(int)Mouse.Right] = false;
					OnMouseRelease(Mouse.Right);
					break;
				case (uint)WM.MBUTTONDOWN:
					newMouse[(int)Mouse.Middle] = true;
					OnMousePress(Mouse.Middle);
					break;
				case (uint)WM.MBUTTONUP:
					newMouse[(int)Mouse.Middle] = false;
					OnMouseRelease(Mouse.Middle);
					break;
				case (uint)WM.CLOSE:
					Finish();
					break;
				case (uint)WM.DESTROY:
					PostQuitMessage(0);
					break;
				default:
					return DefWindowProc(handle, msg, wParam, lParam);
			}
			return IntPtr.Zero;
		}
		#endregion

		#region Helpers
		#region Engine
		/// <summary> Polls the current state of a single key. </summary>
		/// <param name="k"> Key to poll for </param>
		/// <returns> State of given key on current frame </returns>
		public Input GetKey(Key k) {
			if (k == Key.Any) { return anyKey; }
			if (k == Key.None) { return noneKey; }
			return keyboard[(int)k];
		}
		/// <summary> Polls the current state of a single mouse button. </summary>
		/// <param name="m"> Mouse button to poll for </param>
		/// <returns> State of given mouse button on current frame </returns>
		public Input GetMouse(Mouse m) {
			if (m == Mouse.Any) { return anyMouse; }
			if (m == Mouse.None) { return noneMouse; }
			return mouse[(int)m];
		}
		/// <summary> Delay the next frame for <paramref name="time"/> seconds. </summary>
		/// <param name="time"> Seconds to delay next frame for. </param>
		public void Delay(float time) {
			if (!delaying) { delaying = true; }
			delayTime += time;
		}
		/// <summary> Reactivate game. Use in <see cref="OnDestroy"/> to rescue the game if you don't want it to stop. </summary>
		public void Continue() { active = true; }
		/// <summary> Deactivate game. </summary>
		public void Finish() { active = false; }

		/// <summary> Pause the game loop. </summary>
		public void NoLoop() { paused = true; }
		/// <summary> Restart the game loop. </summary>
		public void Loop() { paused = false; }

		/// <summary> Helper method to create a <see cref="Font"/> out of a new sequence of <see cref="Sprite"/> glyphs mapped to characters. </summary>
		/// <param name="glyphs"> Dictionary of <see cref="char"/> to <see cref="Sprite"/> to generate <see cref="Font"/> from </param>
		/// <returns> Created <see cref="Font"/> </returns>
		public Font CreateFont(Dictionary<char, Sprite> glyphs) { return new Font(glyphs); }

		/// <summary> Get the last <see cref="Pixel"/> color drawn to the screen at a given place </summary>
		/// <param name="x"> x coord to read </param>
		/// <param name="y"> y coord to read </param>
		/// <returns> <see cref="Pixel"/> color value drawn at given x/y </returns>
		public Pixel GetScreenPixel(int x, int y) { return defDrawTarget[x, y]; }
		/// <summary> Read the entire screen's <see cref="Pixel"/>s into a 2d array </summary>
		/// <returns> 2d array of <see cref="Pixel"/>s from the screen </returns>
		public Pixel[,] GetScreen() {
			Pixel[,] screen = new Pixel[ScreenWidth, ScreenHeight];
			for (int i = 0; i < ScreenWidth * ScreenHeight; i++) {
				int x = i % ScreenWidth;
				int y = i / ScreenWidth;
				screen[x, y] = defDrawTarget[x, y];
			}
			return screen;
		}
		#endregion

		#region Math

		/// <summary> PI constant </summary>
		public static readonly float PI = (float)Math.PI;

		/// <summary> Wrapper for <see cref="Math.Sin"/> </summary>
		public float Sin(float val) { return (float)Math.Sin(val); }
		/// <summary> Wrapper for <see cref="Math.Cos"/> </summary>
		public float Cos(float val) { return (float)Math.Cos(val); }
		/// <summary> Wrapper for <see cref="Math.Tan"/> </summary>
		public float Tan(float val) { return (float)Math.Tan(val); }

		/// <summary> Wrapper for <see cref="Math.Pow"/> </summary>
		public float Power(float val, float pow) { return (float)Math.Pow(val, pow); }
		/// <summary> Wrapper for <see cref="Math.Sqrt"/> </summary>
		public float Sqrt(float val) { return (float)Math.Sqrt(val); }
		/// <summary> Wrapper for <see cref="Math.Round(double)"/> </summary>
		public float Round(float val, int digits = 0) { return (float)Math.Round(val, digits); }

		/// <summary> Remap <paramref name="val"/> from [<paramref name="oMin"/>, <paramref name="oMax"/>] space to [<paramref name="nMin"/>, <paramref name="nMax"/>] space. </summary>
		public float Map(float val, float oMin, float oMax, float nMin, float nMax) { return (val - oMin) / (oMax - oMin) * (nMax - nMin) + nMin; }
		/// <summary> Clamp <paramref name="val"/> between [<paramref name="min"/>, <paramref name="max"/>] </summary>
		public float Clamp(float val, float min, float max) { return Math.Max(Math.Min(max, val), min); }
		/// <summary> Linearly Interpolate <paramref name="amt"/> between [<paramref name="start"/>, <paramref name="end"/>] </summary>
		public float Lerp(float start, float end, float amt) { return Map(amt, 0, 1, start, end); }
		/// <summary> Wrap <paramref name="val"/> so it stays inside of [<paramref name="min"/>, <paramref name="max"/>] </summary>
		public float Wrap(float val, float min, float max) {
			if (val > max) { return val - min; }
			if (val < min) { return val - max; }
			return val;
		}
		/// <summary> Crude distance between 2d points </summary>
		/// <param name="x1"> first x coord </param> <param name="y1"> first y coord</param>
		/// <param name="x2"> second x coord </param> <param name="y2"> second y coord</param>
		/// <returns> Distance between points </returns>
		public float Distance(float x1, float y1, float x2, float y2) { float a = x2-x1; float b = y2-y1; return Sqrt(a*a+b*b); }
		/// <summary> Crude magnitude of given 2d vector  </summary>
		/// <param name="x"> x coord </param> <param name="y"> y coord</param>
		/// <returns> Distance from point to origin </returns>
		public float Magnitude(float x, float y) { return Sqrt(x*x+y*y); }
		/// <summary> Test if <paramref name="val"/> is between [<paramref name="min"/>, <paramref name="max"/>] </summary>
		public bool Between(float val, float min, float max) { return val > min && val < max; }

		/// <summary> Reseed the RNG with the current Tick count </summary>
		public void Seed() { Randoms.Seed = ((int)DateTime.UtcNow.Ticks) % int.MaxValue; }
		/// <summary> Reseed the RNG with the given seed </summary>
		public void Seed(int s) { Randoms.Seed = s; }
		/// <summary> Get a random int from [0, <paramref name="max"/>) </summary>
		public int Random(int max) { return Random(0, max); }
		/// <summary> Get a random int from [<paramref name="min"/>, <paramref name="max"/>) </summary>
		public int Random(int min, int max) { return Randoms.RandomInt(min, max); }
		/// <summary> Get a random float from [0, 1) </summary>
		public float Random() { return Random(0f, 1f); }
		/// <summary> Get a random float from [0, <paramref name="max"/>) </summary>
		public float Random(float max) { return Random(0, max); }
		/// <summary> Get a random float from [<paramref name="min"/>, <paramref name="max"/>) </summary>
		public float Random(float min, float max) { return Randoms.RandomFloat(min, max); }
		/// <summary> Choose a random element from a given Array </summary>
		public T Random<T>(params T[] list) { return list[Random(list.Length)]; }
		/// <summary> Choose a random element from a given List </summary>
		public T Random<T>(List<T> list) { return list[Random(list.Count)]; }
		/// <summary> Choose a random element from a given Collection </summary>
		public T Random<T>(IEnumerable<T> list) { return Random(list.ToArray()); }
		/// <summary> Convert radians to degrees </summary>
		public float Degrees(float radians) { return (float)(radians * 180 / Math.PI); }
		/// <summary> Convert degrees to radians </summary>
		public float Radians(float degrees) { return (float)(degrees * Math.PI / 180); }
		#endregion

		#region Collections
		/// <summary> Helper that packs a comma separated list of parameters into an array </summary>
		public T[] MakeArray<T>(params T[] items) { return items; }
		/// <summary> Helper that takes a <paramref name="selector"/> function and generates an array of <paramref name="count"/> elements </summary>
		public T[] MakeArray<T>(int count, Func<int, T> selector) {
			T[] arr = new T[count];
			for (int i = 0; i < count; i++) {
				arr[i] = selector(i);
			}
			return arr;
		}

		/// <summary> Helper that packs a comma separated list of parameters into a List</summary>
		public List<T> MakeList<T>(params T[] items) { return items.ToList(); }
		/// <summary> Helper that takes a <paramref name="selector"/> function and generates a List of <paramref name="count"/> elements </summary>
		public List<T> MakeList<T>(int count, Func<int, T> selector) {
			List<T> list = new List<T>(count);
			for (int i = 0; i < count; i++) {
				list.Add(selector(i));
			}
			return list;
		}
		#endregion
		#endregion

		#region Inner
		/// <summary> Windows event handler callback. </summary>
		private void MouseTimer(IntPtr handle, uint message, IntPtr id, uint interval) {
			Windows.Point p = new Windows.Point();
			GetCursorPos(ref p);
			ScreenToClient(Handle, ref p);
			UpdateMouse(p.X, p.Y);
		}
		/// <summary> Update mouse x/y, and bind mouse to window if configured </summary>
		private void UpdateMouse(int x, int y) {
			if (ClampMouse) {
				MouseX = (int)(Clamp(x, 0, windowWidth - 1) / PixWidth);
				MouseY = (int)(Clamp(y, 0, windowHeight - 1) / PixHeight);
			} else {
				MouseX = x / PixWidth;
				MouseY = y / PixHeight;
			}

			if (!mouseOutOfWindow && (!Between(x / PixWidth, 0, ScreenWidth) || !Between(y / PixHeight, 0, ScreenHeight))) {
				mouseOutOfWindow = true;
			}
			
			if (mouseOutOfWindow && Between(x / PixWidth, 0, ScreenWidth) && Between(y / PixHeight, 0, ScreenHeight)) {
				mouseOutOfWindow = false;

				newMouse[(int)Mouse.Left] = GetKeyState((int)VK.LBUTTON) < 0;
				newMouse[(int)Mouse.Middle] = GetKeyState((int)VK.MBUTTON) < 0;
				newMouse[(int)Mouse.Right] = GetKeyState((int)VK.RBUTTON) < 0;

				HandleMouse();
			}
		}
		/// <summary> Window initialization logic </summary>
		private protected override void CreateWindow() {
			uint styleEx = (uint)(WindowStylesEx.AppWindow | WindowStylesEx.WindowEdge);
			uint style = (uint)(WindowStyles.Caption | WindowStyles.SysMenu | WindowStyles.Visible);
			WRect winRect = new WRect() { Left = 0, Top = 0, Right = windowWidth, Bottom = windowHeight };

			AdjustWindowRectEx(ref winRect, style, false, styleEx);

			int width = winRect.Right - winRect.Left;
			int height = winRect.Bottom - winRect.Top;

			if (string.IsNullOrWhiteSpace(AppName)) { AppName = GetType().Name; }

			WindowStyles winStyle = WindowStyles.Overlapped | WindowStyles.Visible | WindowStyles.Caption | WindowStyles.SysMenu | WindowStyles.MinimizeBox;
			Handle = CreateWindowEx(0, ClassName, AppName, (uint)winStyle,
					0, 0, width, height, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

			GetClientRect(Handle, out WRect r);
			ClientRect = r;

			MapKeyboard();
		}
		/// <summary> Windows class registration </summary>
		private protected override void RegisterClass() {
			WindowClassEx wc = new WindowClassEx {
				Icon = LoadIcon(IntPtr.Zero, (IntPtr)ApplicationIcon),
				Cursor = LoadCursorA(IntPtr.Zero, (IntPtr)ArrowCursor),
				Style = HRedraw | VRedraw | OwnDC,
				Instance = GetModuleHandle(null),
				WindowsProc = (proc = WndProc),
				ClsExtra = 0,
				WndExtra = 0,
				MenuName = null,
				Background = IntPtr.Zero,
				ClassName = GetType().FullName
			};
			wc.Size = (uint)Marshal.SizeOf(wc);

			RegisterClassEx(ref wc);
		}
		/// <summary> Assign all <see cref="VK"/> "Virtual Key" codes into a dictionary for lookup as <see cref="Key"/> enums </summary>
		private void MapKeyboard() {
			// A-Z
			mapKeys[0x41] = Key.A; mapKeys[0x42] = Key.B; mapKeys[0x43] = Key.C; mapKeys[0x44] = Key.D; mapKeys[0x45] = Key.E;
			mapKeys[0x46] = Key.F; mapKeys[0x47] = Key.G; mapKeys[0x48] = Key.H; mapKeys[0x49] = Key.I; mapKeys[0x4A] = Key.J;
			mapKeys[0x4B] = Key.K; mapKeys[0x4C] = Key.L; mapKeys[0x4D] = Key.M; mapKeys[0x4E] = Key.N; mapKeys[0x4F] = Key.O;
			mapKeys[0x50] = Key.P; mapKeys[0x51] = Key.Q; mapKeys[0x52] = Key.R; mapKeys[0x53] = Key.S; mapKeys[0x54] = Key.T;
			mapKeys[0x55] = Key.U; mapKeys[0x56] = Key.V; mapKeys[0x57] = Key.W; mapKeys[0x58] = Key.X; mapKeys[0x59] = Key.Y;
			mapKeys[0x5A] = Key.Z;
			// 0-9
			mapKeys[0x30] = Key.K0; mapKeys[0x31] = Key.K1; mapKeys[0x32] = Key.K2; mapKeys[0x33] = Key.K3; mapKeys[0x34] = Key.K4;
			mapKeys[0x35] = Key.K5; mapKeys[0x36] = Key.K6; mapKeys[0x37] = Key.K7; mapKeys[0x38] = Key.K8; mapKeys[0x39] = Key.K9;

			// Fs in chat
			// @WTF: F10-F12 do not work for some reason
			mapKeys[(uint)VK.F1] = Key.F1; mapKeys[(uint)VK.F2] = Key.F2; mapKeys[(uint)VK.F3] = Key.F3; mapKeys[(uint)VK.F4] = Key.F4;
			mapKeys[(uint)VK.F5] = Key.F5; mapKeys[(uint)VK.F6] = Key.F6; mapKeys[(uint)VK.F7] = Key.F7; mapKeys[(uint)VK.F8] = Key.F8;
			mapKeys[(uint)VK.F9] = Key.F9; mapKeys[(uint)VK.F10] = Key.F10; mapKeys[(uint)VK.F11] = Key.F11; mapKeys[(uint)VK.F12] = Key.F12;
			mapKeys[(uint)VK.F13] = Key.F13; mapKeys[(uint)VK.F14] = Key.F14; mapKeys[(uint)VK.F15] = Key.F15; mapKeys[(uint)VK.F16] = Key.F16;
			mapKeys[(uint)VK.F17] = Key.F17; mapKeys[(uint)VK.F18] = Key.F18; mapKeys[(uint)VK.F19] = Key.F19; mapKeys[(uint)VK.F20] = Key.F20;
			mapKeys[(uint)VK.F21] = Key.F21; mapKeys[(uint)VK.F22] = Key.F22; mapKeys[(uint)VK.F23] = Key.F23; mapKeys[(uint)VK.F24] = Key.F24;

			// Arrow Keys
			mapKeys[(uint)VK.UP] = Key.Up; mapKeys[(uint)VK.DOWN] = Key.Down;
			mapKeys[(uint)VK.LEFT] = Key.Left; mapKeys[(uint)VK.RIGHT] = Key.Right;
			
			// Misc but important keys
			mapKeys[(uint)VK.BACK] = Key.Back; mapKeys[(uint)VK.SPACE] = Key.Space;
			mapKeys[(uint)VK.ESCAPE] = Key.Escape; mapKeys[(uint)VK.RETURN] = Key.Enter; 
			mapKeys[(uint)VK.TAB] = Key.Tab; mapKeys[(uint)VK.CAPITAL] = Key.Capslock; 

			mapKeys[(uint)VK.HOME] = Key.Home; mapKeys[(uint)VK.END] = Key.End; 
			mapKeys[(uint)VK.PRIOR] = Key.PageUp; mapKeys[(uint)VK.NEXT] = Key.PageDown; 
			mapKeys[(uint)VK.INSERT] = Key.Insert; mapKeys[(uint)VK.DELETE] = Key.Delete;

			mapKeys[(uint)VK.LWIN] = Key.LWin; mapKeys[(uint)VK.RWIN] = Key.RWin; mapKeys[(uint)VK.APPS] = Key.Apps;
			mapKeys[(uint)VK.SHIFT] = Key.Shift; mapKeys[(uint)VK.LSHIFT] = Key.LShift; mapKeys[(uint)VK.RSHIFT] = Key.RShift;
			mapKeys[(uint)VK.CONTROL] = Key.Control; mapKeys[(uint)VK.LCONTROL] = Key.LControl; mapKeys[(uint)VK.RCONTROL] = Key.RControl;
			// @WTF: Alt key is weird and doesn't get recognized...
			mapKeys[(uint)VK.MENU] = Key.Alt; mapKeys[(uint)VK.LMENU] = Key.LAlt; mapKeys[(uint)VK.RMENU] = Key.RAlt; 

			mapKeys[(uint)VK.PAUSE] = Key.Pause; mapKeys[(uint)VK.SCROLL] = Key.Scroll; mapKeys[(uint)VK.SNAPSHOT] = Key.PrintScreen;

			// OEM keys are a bit weird 
			mapKeys[(uint)VK.OEM_1] = Key.Colon; 
			mapKeys[(uint)VK.OEM_2] = Key.Slash; 
			mapKeys[(uint)VK.OEM_3] = Key.Backtick; 
			mapKeys[(uint)VK.OEM_4] = Key.OpenBracket;
			mapKeys[(uint)VK.OEM_5] = Key.Backslash;
			mapKeys[(uint)VK.OEM_6] = Key.CloseBracket;
			mapKeys[(uint)VK.OEM_7] = Key.Quote;
			mapKeys[(uint)VK.OEM_MINUS] = Key.Minus; 
			mapKeys[(uint)VK.OEM_COMMA] = Key.Comma; 
			mapKeys[(uint)VK.OEM_PERIOD] = Key.Period; 
			mapKeys[(uint)VK.OEM_PLUS] = Key.Plus; 

			// Num pad since some people want to use it 
			mapKeys[(uint)VK.NUMLOCK] = Key.NumLock; mapKeys[(uint)VK.ADD] = Key.Add;
			mapKeys[(uint)VK.DIVIDE] = Key.Divide; mapKeys[(uint)VK.MULTIPLY] = Key.Multiply; mapKeys[(uint)VK.SUBTRACT] = Key.Sub;
			mapKeys[(uint)VK.NUMPAD1] = Key.Num1; mapKeys[(uint)VK.NUMPAD2] = Key.Num2; mapKeys[(uint)VK.NUMPAD3] = Key.Num3; 
			mapKeys[(uint)VK.NUMPAD4] = Key.Num4; mapKeys[(uint)VK.NUMPAD5] = Key.Num5; mapKeys[(uint)VK.NUMPAD6] = Key.Num6; 
			mapKeys[(uint)VK.NUMPAD7] = Key.Num7; mapKeys[(uint)VK.NUMPAD8] = Key.Num8; mapKeys[(uint)VK.NUMPAD9] = Key.Num9; 
			mapKeys[(uint)VK.NUMPAD0] = Key.Num0; mapKeys[(uint)VK.SEPARATOR] = Key.Separator;


		}
		#endregion

		#region Drawing
		/// <summary> Put a <see cref="Pixel"/> somewhere on the screen </summary>
		/// <param name="x"> x coord </param>
		/// <param name="y"> y coord </param>
		/// <param name="col"> color of pixel to draw </param>
		/// <remarks> Applies any blendmode settings configured by <see cref="PixelMode"/> when drawing the pixel. </remarks>
		public void Draw(int x, int y, Pixel col) { Draw(new Point(x, y), col); }
		/// <summary> Put a <see cref="Pixel"/> somewhere on the screen </summary>
		/// <param name="p"> coordinate points </param>
		/// <param name="col"> color of pixel to draw </param>
		/// <remarks> Applies any blendmode settings configured by <see cref="PixelMode"/> when drawing the pixel. </remarks>
		public void Draw(Point p, Pixel col) {
			if (drawTarget == null) { return; }

			// Internal helper function 
			void MakePixel(int a, int b, Pixel pix) {
				if (a >= 0 && a < drawTarget.Width && b >= 0 && b < drawTarget.Height) {
					drawTarget[a, b] = pix;
				}
			}

			switch (PixelMode) {
				case Pixel.Mode.Normal:
					MakePixel(p.X, p.Y, col);
					break;
				case Pixel.Mode.Mask:
					if (col.A == 255) { MakePixel(p.X, p.Y, col); }
					break;
				case Pixel.Mode.Alpha:
					Pixel d = drawTarget[p.X, p.Y];
					float a = col.A / 255.0f * PixelBlend;
					float c = 1.0f - a;
					float r = a * col.R + c * d.R;
					float g = a * col.G + c * d.G;
					float b = a * col.B + c * d.B;
					Pixel pix = new Pixel((byte)r, (byte)g, (byte)b);
					MakePixel(p.X, p.Y, pix);
					break;
				case Pixel.Mode.Custom:
					MakePixel(p.X, p.Y, Shader.Calculate(p.X, p.Y, drawTarget[p.X, p.Y], col));
					break;
			}
		}
		/// <summary> Draw a line of <see cref="Pixel"/>s betwen the given <see cref="Point"/>s </summary>
		/// <param name="p1"> First <see cref="Point"/> </param>
		/// <param name="p2"> Second <see cref="Point"/> </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawLine(Point p1, Point p2, Pixel col) {
			int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
			dx = p2.X - p1.X; dy = p2.Y - p1.Y;
			dx1 = Math.Abs(dx); dy1 = Math.Abs(dy);
			px = 2 * dy1 - dx1; py = 2 * dx1 - dy1;
			if (dy1 <= dx1) {
				if (dx >= 0) {
					x = p1.X; y = p1.Y; xe = p2.X;
				} else {
					x = p2.X; y = p2.Y; xe = p1.X;
				}

				Draw(x, y, col);

				for (i = 0; x < xe; i++) {
					x = x + 1;
					if (px < 0) { px = px + 2 * dy1; }
					else {
						if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) { y = y + 1; } 
						else { y = y - 1; }
						px = px + 2 * (dy1 - dx1);
					}
					Draw(x, y, col);
				}
			} else {
				if (dy >= 0) { x = p1.X; y = p1.Y; ye = p2.Y; }
				else { x = p2.X; y = p2.Y; ye = p1.Y; }

				Draw(x, y, col);

				for (i = 0; y < ye; i++) {
					y = y + 1;
					if (py <= 0) {
						py = py + 2 * dx1;
					} else {
						if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0)) { x = x + 1; } 
						else { x = x - 1; }
						py = py + 2 * (dx1 - dy1);
					}
					Draw(x, y, col);
				}
			}
		}
		/// <summary> Draw an empty cirlce of <see cref="Pixel"/>s centered around a <see cref="Point"/></summary>
		/// <param name="p"> <see cref="Point"/> at center </param>
		/// <param name="radius"> Radius around center </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawCircle(Point p, int radius, Pixel col) {
			int x0 = 0;
			int y0 = radius;
			int d = 3 - 2 * radius;

			if (radius == 0) { return; }

			while (y0 >= x0) {
				Draw(new Point(p.X - x0, p.Y - y0), col);
				Draw(new Point(p.X - y0, p.Y - x0), col);
				Draw(new Point(p.X + y0, p.Y - x0), col);
				Draw(new Point(p.X + x0, p.Y - y0), col);
				Draw(new Point(p.X - x0, p.Y + y0), col);
				Draw(new Point(p.X - y0, p.Y + x0), col);
				Draw(new Point(p.X + y0, p.Y + x0), col);
				Draw(new Point(p.X + x0, p.Y + y0), col);

				if (d < 0) { d += 4 * x0++ + 6; }
				else { d += 4 * (x0++ - y0--) + 10; }
			}
		}
		/// <summary> Draw a filled cirlce of <see cref="Pixel"/>s centered around a <see cref="Point"/></summary>
		/// <param name="p"> <see cref="Point"/> at center </param>
		/// <param name="radius"> Radius around center </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void FillCircle(Point p, int radius, Pixel col) {
			int x0 = 0;
			int y0 = radius;
			int d = 3 - 2 * radius;

			if (radius == 0) { return; }

			void MakeLine(int sx, int ex, int ny) {
				for (int i = sx; i <= ex; i++) {
					Draw(new Point(i, ny), col);
				}
			}

			while (y0 >= x0) {
				MakeLine(p.X - x0, p.X + x0, p.Y - y0);
				MakeLine(p.X - y0, p.X + y0, p.Y - x0);
				MakeLine(p.X - x0, p.X + x0, p.Y + y0);
				MakeLine(p.X - y0, p.X + y0, p.Y + x0);

				if (d < 0) { d += 4 * x0++ + 6; }
				else { d += 4 * (x0++ - y0--) + 10; }
			}
		}
		/// <summary> Draw an empty elipse of <see cref="Pixel"/>s centered around a <see cref="Point"/></summary>
		/// <param name="p"> <see cref="Point"/> at center </param>
		/// <param name="width"> Width around center </param>
		/// <param name="height"> Height around center </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawEllipse(Point p, int width, int height, Pixel col) {
			if (width == 0 || height == 0) {
				return;
			}
			int a2 = width * width;
			int b2 = height * height;
			int fa2 = 4 * a2, fb2 = 4 * b2;
			int sigma;

			sigma = 2 * b2 + a2 * (1 - 2 * height);
			for (int x = 0, y = height; b2 * x <= a2 * y; x++) {
				Draw(p.X + x, p.Y + y, col);
				Draw(p.X - x, p.Y + y, col);
				Draw(p.X + x, p.Y - y, col);
				Draw(p.X - x, p.Y - y, col);

				if (sigma >= 0) {
					sigma += fa2 * (1 - y--);
				}
				sigma += b2 * ((4 * x) + 6);
			}

			sigma = 2 * a2 + b2 * (1 - 2 * width);
			for (int x = width, y = 0; a2 * y <= b2 * x; y++) {
				Draw(p.X + x, p.Y + y, col);
				Draw(p.X - x, p.Y + y, col);
				Draw(p.X + x, p.Y - y, col);
				Draw(p.X - x, p.Y - y, col);

				if (sigma >= 0) {
					sigma += fb2 * (1 - x--);
				}
				sigma += a2 * ((4 * y) + 6);
			}
		}
		/// <summary> Draw a filled elipse of <see cref="Pixel"/>s centered around a <see cref="Point"/></summary>
		/// <param name="p"> <see cref="Point"/> at center </param>
		/// <param name="width"> Width around center </param>
		/// <param name="height"> Height around center </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void FillEllipse(Point p, int width, int height, Pixel col) {
			if (width == 0 || height == 0) {
				return;
			}
			void ScanLine(int sx, int ex, int y) {
				for (int i = sx; i <= ex; i++) {
					Draw(i, y, col);
				}
			}

			int a2 = width * width;
			int b2 = height * height;
			int fa2 = 4 * a2, fb2 = 4 * b2;
			int sigma;

			sigma = 2 * b2 + a2 * (1 - 2 * height);
			for (int x = 0, y = height; b2 * x <= a2 * y; x++) {
				ScanLine(p.X - x, p.X + x, p.Y - y);
				ScanLine(p.X - x, p.X + x, p.Y + y);

				if (sigma >= 0) { sigma += fa2 * (1 - y--); }
				sigma += b2 * ((4 * x) + 6);
			}

			sigma = 2 * a2 + b2 * (1 - 2 * width);
			for (int x = width, y = 0; a2 * y <= b2 * x; y++) {
				ScanLine(p.X - x, p.X + x, p.Y - y);
				ScanLine(p.X - x, p.X + x, p.Y + y);

				if (sigma >= 0) { sigma += fb2 * (1 - x--); }
				sigma += a2 * ((4 * y) + 6);
			}
		}
		/// <summary> Draw an empty rectangle of <see cref="Pixel"/>s </summary>
		/// <param name="p"> "Top Left" <see cref="Point"/> </param>
		/// <param name="w"> Width of rectangle </param>
		/// <param name="h"> Height of rectangle </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawRect(Point p, int w, int h, Pixel col) {
			DrawLine(new Point(p.X, p.Y), new Point(p.X + w, p.Y), col);
			DrawLine(new Point(p.X + w, p.Y), new Point(p.X + w, p.Y + h), col);
			DrawLine(new Point(p.X + w, p.Y + h), new Point(p.X, p.Y + h), col);
			DrawLine(new Point(p.X, p.Y + h), new Point(p.X, p.Y), col);
		}
		/// <summary> Draw an empty rectangle of <see cref="Pixel"/>s between two <see cref="Point"/>s </summary>
		/// <param name="p1"> First bounding <see cref="Point"/> </param>
		/// <param name="p2"> Second bounding <see cref="Point"/> </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawRect(Point p1, Point p2, Pixel col) {
			if (p1.X > p2.X && p1.Y > p2.Y) {
				Point temp = p1;
				p1 = p2;
				p2 = temp;
			}

			DrawRect(p1, Math.Abs(p2.X - p1.X - 1), Math.Abs(p2.Y - p1.Y - 1), col);
		}
		/// <summary> Draw a filled rectangle of <see cref="Pixel"/>s </summary>
		/// <param name="p"> "Top Left" <see cref="Point"/> </param>
		/// <param name="w"> Width of rectangle </param>
		/// <param name="h"> Height of rectangle </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void FillRect(Point p, int w, int h, Pixel col) {
			int Clip(int val, int min, int max) {
				if (val < min) { val = min; }
				if (val > max) { val = max; }
				return val;
			}

			int x2 = p.X + w;
			int y2 = p.Y + h;

			Clip(p.X, 0, ScreenWidth);
			Clip(p.Y, 0, ScreenHeight);

			Clip(x2, 0, ScreenWidth);
			Clip(y2, 0, ScreenHeight);

			for (int i = p.X; i < x2; i++) {
				for (int j = p.Y; j < y2; j++) {
					Draw(i, j, col);
				}
			}
		}
		/// <summary> Draw a filled rectangle of <see cref="Pixel"/>s between two <see cref="Point"/>s </summary>
		/// <param name="p1"> First bounding <see cref="Point"/> </param>
		/// <param name="p2"> Second bounding <see cref="Point"/> </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void FillRect(Point p1, Point p2, Pixel col) {
			if (p1.X > p2.X && p1.Y > p2.Y) {
				Point temp = p1;
				p1 = p2;
				p2 = temp;
			}

			FillRect(p1, Math.Abs(p2.X - p1.X - 1), Math.Abs(p2.Y - p1.Y - 1), col);
		}
		/// <summary> Draw an empty triangle of <see cref="Pixel"/>s between three <see cref="Point"/>s </summary>
		/// <param name="p1"> First bounding <see cref="Point"/> </param>
		/// <param name="p2"> Second bounding <see cref="Point"/> </param>
		/// <param name="p3"> Third bounding <see cref="Point"/> </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawTriangle(Point p1, Point p2, Point p3, Pixel col) {
			DrawLine(p1, p2, col);
			DrawLine(p2, p3, col);
			DrawLine(p3, p1, col);
		}
		/// <summary> Draw a filled triangle of <see cref="Pixel"/>s between three <see cref="Point"/>s </summary>
		/// <param name="p1"> First bounding <see cref="Point"/> </param>
		/// <param name="p2"> Second bounding <see cref="Point"/> </param>
		/// <param name="p3"> Third bounding <see cref="Point"/> </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void FillTriangle(Point p1, Point p2, Point p3, Pixel col) {
			void Swap(ref int a, ref int b) { int t = a; a = b; b = t; }
			void MakeLine(int sx, int ex, int ny) { for (int i = sx; i <= ex; i++) { Draw(i, ny, col); } }

			int x1 = p1.X, y1 = p1.Y;
			int x2 = p2.X, y2 = p2.Y;
			int x3 = p3.X, y3 = p3.Y;

			int t1x, t2x, y, minx, maxx, t1xp, t2xp;
			bool changed1 = false;
			bool changed2 = false;
			int signx1, signx2, dx1, dy1, dx2, dy2;
			int e1, e2;
			// Sort vertices
			if (y1 > y2) { Swap(ref y1, ref y2); Swap(ref x1, ref x2); }
			if (y1 > y3) { Swap(ref y1, ref y3); Swap(ref x1, ref x3); }
			if (y2 > y3) { Swap(ref y2, ref y3); Swap(ref x2, ref x3); }

			t1x = t2x = x1; y = y1;   // Starting points
			dx1 = x2 - x1; if (dx1 < 0) { dx1 = -dx1; signx1 = -1; } else { signx1 = 1; }
			dy1 = y2 - y1;

			dx2 = x3 - x1; if (dx2 < 0) { dx2 = -dx2; signx2 = -1; } else { signx2 = 1; }
			dy2 = y3 - y1;

			if (dy1 > dx1) {   // swap values
				Swap(ref dx1, ref dy1);
				changed1 = true;
			}
			if (dy2 > dx2) {   // swap values
				Swap(ref dy2, ref dx2);
				changed2 = true;
			}

			e2 = dx2 >> 1;
			// Flat top, just process the second half
			if (y1 == y2) { goto next; }
			e1 = dx1 >> 1;

			for (int i = 0; i < dx1;) {
				t1xp = 0; t2xp = 0;
				if (t1x < t2x) { minx = t1x; maxx = t2x; } 
				else { minx = t2x; maxx = t1x; }
				// process first line until y value is about to change
				while (i < dx1) {
					i++;
					e1 += dy1;
					while (e1 >= dx1) {
						e1 -= dx1;
						if (changed1) { t1xp = signx1; }//t1x += signx1;
						else { goto next1; }
					}
					if (changed1) { break; }
					else { t1x += signx1; }
				}
				// Move line
				next1:
				// process second line until y value is about to change
				while (true) {
					e2 += dy2;
					while (e2 >= dx2) {
						e2 -= dx2;
						if (changed2) {t2xp = signx2; }//t2x += signx2;
						else { goto next2; }
					}
					if (changed2) { break; }
					else {t2x += signx2; }
				}
				next2:
				if (minx > t1x) { minx = t1x; } if (minx > t2x) { minx = t2x; }
				if (maxx < t1x) { maxx = t1x; } if (maxx < t2x) { maxx = t2x; }
				MakeLine(minx, maxx, y);    // Draw line from min to max points found on the y
											// Now increase y
				if (!changed1) { t1x += signx1; }
				t1x += t1xp;
				if (!changed2) { t2x += signx2; }
				t2x += t2xp;
				y += 1;
				if (y == y2) { break; }

			}
			next:
			// Second half
			dx1 = x3 - x2; if (dx1 < 0) { dx1 = -dx1; signx1 = -1; } 
			else { signx1 = 1; }
			dy1 = y3 - y2;
			t1x = x2;

			if (dy1 > dx1) {   // swap values
				Swap(ref dy1, ref dx1);
				changed1 = true;
			} else { changed1 = false; }

			e1 = dx1 >> 1;

			for (int i = 0; i <= dx1; i++) {
				t1xp = 0; t2xp = 0;
				if (t1x < t2x) { minx = t1x; maxx = t2x; } 
				else { minx = t2x; maxx = t1x; }
				// process first line until y value is about to change
				while (i < dx1) {
					e1 += dy1;
					while (e1 >= dx1) {
						e1 -= dx1;
						if (changed1) { t1xp = signx1; break; }//t1x += signx1;
						else { goto next3; }
					}
					if (changed1) { break; }
					else { t1x += signx1; }
					if (i < dx1) { i++; }
				}
				next3:
				// process second line until y value is about to change
				while (t2x != x3) {
					e2 += dy2;
					while (e2 >= dx2) {
						e2 -= dx2;
						if (changed2) { t2xp = signx2; }
						else { goto next4; }
					}
					if (changed2) { break; }
					else { t2x += signx2; }
				}
				next4:
				
				if (minx > t1x) { minx = t1x; } if (minx > t2x) { minx = t2x; }
				if (maxx < t1x) { maxx = t1x; } if (maxx < t2x) { maxx = t2x; }
				MakeLine(minx, maxx, y);
				if (!changed1) { t1x += signx1; }
				t1x += t1xp;
				if (!changed2) { t2x += signx2; }
				t2x += t2xp;
				y += 1;
				if (y > y3) { return; }
			}
		}
		/// <summary> Draws a wireframe polygon defined by the given list of <see cref="Point"/>s </summary>
		/// <param name="verts"> <see cref="Point"/>[] to connect with lines </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawPolygon(Point[] verts, Pixel col) {
			for (int i = 0; i < verts.Length - 1; i++) {
				DrawLine(verts[i], verts[i + 1], col);
			}
			DrawLine(verts[verts.Length - 1], verts[0], col);
		}
		/// <summary> Draws a filled polygon defined by the given list of <see cref="Point"/>s </summary>
		/// <param name="verts"> <see cref="Point"/>[] to connect with triangles</param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void FillPolygon(Point[] verts, Pixel col) {
			for (int i = 1; i < verts.Length - 1; i++) {
				FillTriangle(verts[0], verts[i], verts[i + 1], col);
			}
		}
		/// <summary> Draws a wireframe path defined by the given list of <see cref="Point"/>s </summary>
		/// <param name="points"> <see cref="Point"/>[] to connect with lines </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw </param>
		public void DrawPath(Point[] points, Pixel col) {
			for (int i = 0; i < points.Length - 1; i++) {
				DrawLine(points[i], points[i + 1], col);
			}
		}
		/// <summary> Draw all the <see cref="Pixel"/>s in a given <see cref="ISprite"/> to the screen </summary>
		/// <param name="p"> World origin <see cref="Point"/> of given sprite </param>
		/// <param name="spr"> Given <see cref="ISprite"/> to draw </param>
		public void DrawSprite(Point p, ISprite spr) {
			if (spr == null) { return; }
			
			for (int i = 0; i < spr.Width; i++) {
				for (int j = 0; j < spr.Height; j++) {
					Draw(p.X + i, p.Y + j, spr[i, j]);
				}
			}
		}
		/// <summary> Draws a subset of <see cref="Pixel"/>s in a <see cref="ISprite"/> to the screen. Useful for Spritesheets. </summary>
		/// <param name="p"> World origin <see cref="Point"/> of drawn <see cref="Pixel"/>s </param>
		/// <param name="spr"> Source <see cref="ISprite"/> to draw from </param>
		/// <param name="op"> Offset <see cref="Point"/> into given <see cref="ISprite"/> to start copying pixels from </param>
		/// <param name="w"> Width of region to draw </param>
		/// <param name="h"> Height of region to draw </param>
		public void DrawPartialSprite(Point p, ISprite spr, Point op, int w, int h) {
			if (spr == null) { return; }
			
			for (int i = 0; i < w; i++) {
				for (int j = 0; j < h; j++) {
					Draw(p.X + i, p.Y + j, spr[i + op.X, j + op.Y]);
				}
			}
		}
		/// <summary> Clears the screen, so it has a given color. </summary>
		/// <param name="p"> <see cref="Pixel"/> color to clear to </param>
		public void Clear(Pixel p) {
			Pixel[] pixels = drawTarget.GetPixels();
			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = p;

			if (hrText) {
				pixels = textTarget.GetPixels();
				for (int i = 0; i < pixels.Length; i++) {
					pixels[i] = Pixel.Empty;
				}
			}
		}
		#endregion

		#region Subsystems

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		/// <summary> Enum of built-in subsystems </summary>
		public enum Subsystem { Fullscreen, Audio, HrText }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary> Enable a given <see cref="Subsystem"/>. </summary>
		/// <param name="subsystem"> <see cref="Subsystem"/> to enable. </param>
		public void Enable(Subsystem subsystem) {
			switch (subsystem) {
				case Subsystem.Fullscreen:
					MonitorInfo mi = new MonitorInfo();
					mi.Size = Marshal.SizeOf(mi);

					if (GetMonitorInfo(MonitorFromWindow(Handle, MonitorDefaultNearest), ref mi)) {
						int style = GetWindowLong(Handle, (int)WindowLongs.STYLE);

						SetWindowLong(Handle, (int)WindowLongs.STYLE, style & ~(int)WindowStyles.OverlappedWindow);

						SetWindowPos(Handle, WindowTop,
									 mi.Monitor.Left, mi.Monitor.Top,
									 mi.Monitor.Right - mi.Monitor.Left,
									 mi.Monitor.Bottom - mi.Monitor.Top,
									 (uint)(SWP.NoOwnerZOrder | SWP.FrameChanged));
					}

					GetClientRect(Handle, out WRect r);
					ClientRect = r;

					windowWidth = r.Right - r.Left;
					windowHeight = r.Bottom - r.Top;

					ScreenWidth = windowWidth / PixWidth;
					ScreenHeight = windowHeight / PixHeight;

					HandleDrawTarget();
					break;

				case Subsystem.Audio:
					if (audio == null) {
						audio = new AudioEngine() {
							OnSoundCreate = this.OnSoundCreate,
							OnSoundFilter = this.OnSoundFilter
						};
					}
					audio.CreateAudio();
					break;

				case Subsystem.HrText:
					hrText = true;
					textTarget = new Sprite(0, 0);
					break;
			}
		}

		#region Text
		/// <summary> Draws text to the screen. </summary>
		/// <param name="p"> Screen <see cref="Point"/> to draw text to </param>
		/// <param name="text"> Text to draw </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw text with </param>
		/// <param name="scale"> Scale to apply to resize text </param>
		public void DrawText(Point p, string text, Pixel col, int scale = 1) {
			if (string.IsNullOrWhiteSpace(text)) {
				return;
			}
			Pixel.Mode prev = PixelMode;
			if (PixelMode != Pixel.Mode.Custom) {
				if (col.A != 255) { PixelMode = Pixel.Mode.Alpha; }
				else { PixelMode = Pixel.Mode.Mask; }
			}

			int sx = 0;
			int sy = 0;

			foreach (char c in text) {
				if (c == '\n') {
					sx = 0;
					sy += Font.CharHeight * scale;
				} else {
					if (Font.Glyphs.TryGetValue(c, out Sprite cur)) {
						if (scale > 1) {
							for (int i = 0; i < cur.Width; i++){
								for (int j = 0; j < cur.Height; j++) {
									if (cur[i, j].R > 0) {
										for (int ax = 0; ax < scale; ax++) {
											for (int ay = 0; ay < scale; ay++) {
												Draw(p.X + sx + (i * scale) + ax, p.Y + sy + (j * scale) + ay, col);
											}
										}
									}
								}
							}
						} else {
							for (int i = 0; i < cur.Width; i++) {
								for (int j = 0; j < cur.Height; j++) {
									if (cur[i, j].R > 0) {
										Draw(p.X + sx + i, p.Y + sy + j, col);
									}
								}
							}
						}

						sx += cur.Width * scale;
					} else {
						sx += scale * 8;
					}
				}
			}

			if (PixelMode != Pixel.Mode.Custom) {
				PixelMode = prev;
			}

		}

		private Bitflags overdraw = new Bitflags(16*16);
		/// <summary> Draws outlined text to the screen. </summary>
		/// <param name="p"> Screen <see cref="Point"/> to draw text to </param>
		/// <param name="text"> Text to draw </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw text with </param>
		/// <param name="outlineCol"> <see cref="Pixel"/> color to draw text outline with </param>
		/// <param name="scale"> Scale to apply to resize text </param>
		public void DrawTextOutline(Point p, string text, Pixel col, Pixel outlineCol, int scale = 1) {
			if (string.IsNullOrWhiteSpace(text)) {
				return;
			}
			Pixel.Mode prev = PixelMode;
			Bitflags overdraw = null;

			// Outlines are drawn from (-1, GLYPH_DIM+1) in both X and Y
			int OUTLINE_HEIGHT = 2 + Font.CharHeight;
			if (PixelMode != Pixel.Mode.Custom) {
				if (col.A != 255 || outlineCol.A != 255) { 
					PixelMode = Pixel.Mode.Alpha; 
					// Prep overdraw checker with enough space to track each pixel for a double-wide character. 
					overdraw = this.overdraw;
					overdraw.Expand(OUTLINE_HEIGHT * OUTLINE_HEIGHT * 2 - 1);

				} 
				else { PixelMode = Pixel.Mode.Mask; }
			}
			
			bool AlreadyDrawn(int x, int y) {
				if (overdraw != null) {
					int xa = x + 1;
					int ya = y + 1;
					int index = xa + OUTLINE_HEIGHT * ya;
					return overdraw[index];
				}
				return false;
			}
			void Drawn(int x, int y) {
				if (overdraw != null) {
					int xa = x + 1;
					int ya = y + 1;
					int index = xa + OUTLINE_HEIGHT * ya;
					overdraw[index] = true;
				}
			}
			
			int sx = 0;
			int sy = 0;

			foreach (char c in text) {
				if (c == '\n') {
					sx = 0;
					sy += Font.CharHeight * scale;
				} else {
					if (Font.Glyphs.TryGetValue(c, out Sprite cur)) {
						overdraw?.Clear();

						if (scale > 1) {
							for (int i = 0; i < cur.Width; i++) {
								for (int j = 0; j < cur.Height; j++) {
									if (cur[i, j].R > 0) {
										bool outlineLeft	= !AlreadyDrawn(i-1, j) && (i == 0 || cur[i-1, j].R <= 0);
										bool outlineUp		= !AlreadyDrawn(i, j-1) && (j == 0 || cur[i, j-1].R <= 0);
										bool outlineRight	= !AlreadyDrawn(i+1, j) && (i == cur.Width - 1 || cur[i+1, j].R <= 0);
										bool outlineDown	= !AlreadyDrawn(i, j+1) && (j == cur.Height - 1 || cur[i, j+1].R <= 0);

										for (int ax = 0; ax < scale; ax++) {
											for (int ay = 0; ay < scale; ay++) {
												Draw(p.X + sx + (i * scale) + ax, p.Y + sy + (j * scale) + ay, col);
												if (outlineLeft)	{ Draw(p.X + sx + ((i-1) * scale) + ax, p.Y + sy + (j * scale) + ay, outlineCol); }
												if (outlineUp)		{ Draw(p.X + sx + (i * scale) + ax, p.Y + sy + ((j-1) * scale) + ay, outlineCol); }
												if (outlineRight)	{ Draw(p.X + sx + ((i+1) * scale) + ax, p.Y + sy + (j * scale) + ay, outlineCol); }
												if (outlineDown)	{ Draw(p.X + sx + (i * scale) + ax, p.Y + sy + ((j+1) * scale) + ay, outlineCol); }
											}
										}
										if (outlineLeft)	{ Drawn(i-1, j); }
										if (outlineUp)		{ Drawn(i, j-1); }
										if (outlineRight)	{ Drawn(i+1, j); }
										if (outlineDown)	{ Drawn(i, j+1); }
									}
								}
							}
						} else {
							for (int i = 0; i < cur.Width; i++) {
								for (int j = 0; j < cur.Height; j++) {
									if (cur[i, j].R > 0) {
										Draw(p.X + sx + i, p.Y + sy + j, col);
										if (!AlreadyDrawn(i-1,j) && (i == 0 || cur[i-1, j].R <= 0)) { Draw(p.X + sx + i - 1, p.Y + sy + j, outlineCol); Drawn(i-1,j); }
										if (!AlreadyDrawn(i,j-1) && (j == 0 || cur[i, j-1].R <= 0)) { Draw(p.X + sx + i, p.Y + sy + j - 1, outlineCol); Drawn(i,j-1); }
										if (!AlreadyDrawn(i+1,j) && (i == cur.Width-1 || cur[i+1, j].R <= 0)) { Draw(p.X + sx + i + 1, p.Y + sy + j, outlineCol); Drawn(i+1,j); }
										if (!AlreadyDrawn(i,j+1) && (j == cur.Height-1 || cur[i, j+1].R <= 0)) { Draw(p.X + sx + i , p.Y + sy + j + 1, outlineCol); Drawn(i,j+1); }
									}
								}
							}
						}

						sx += cur.Width * scale;

					} else {
						sx += scale * 8;
					}
				}
			}

			if (PixelMode != Pixel.Mode.Custom) { PixelMode = prev; }

		}




		/// <summary> Draws High-res text to the screen. Requires the <see cref="Subsystem.HrText"/> to be <see cref="Enable(Subsystem)"/>'d </summary>
		/// <param name="p"> Screen <see cref="Point"/> to draw text to </param>
		/// <param name="text"> Text to draw </param>
		/// <param name="col"> <see cref="Pixel"/> color to draw text with </param>
		/// <param name="scale"> Scale to apply to resize text </param>
		public void DrawTextHr(Point p, string text, Pixel col, int scale = 1) {
			const int TargetSizeStep = 25;

			if (!hrText || string.IsNullOrWhiteSpace(text)) {
				return;
			}
			
			void SetPixel(int i, int j) {
				if (i > textTarget.Width && i < windowWidth) {
					Sprite temp = new Sprite(Math.Min(textTarget.Width + TargetSizeStep, windowWidth), textTarget.Height);

					for (int y = 0; y < textTarget.Height; y++) {
						int index = y * textTarget.Width;
						int indexTemp = y * temp.Width;
						Array.Copy(textTarget.GetPixels(), index, temp.GetPixels(), indexTemp, textTarget.Width);
					}

					textTarget = temp;
				}

				if (j > textTarget.Height && j < windowHeight) {
					Sprite temp = new Sprite(textTarget.Width, Math.Min(textTarget.Height + TargetSizeStep, windowHeight));

					for (int y = 0; y < textTarget.Height; y++) {
						int index = y * textTarget.Width;
						int indexTemp = y * temp.Width;
						Array.Copy(textTarget.GetPixels(), index, temp.GetPixels(), indexTemp, textTarget.Width);
					}

					textTarget = temp;
				}

				switch (PixelMode) {
					case Pixel.Mode.Normal:
						if (i >= 0 && i < textTarget.Width && j >= 0 && j < textTarget.Height) {
							textTarget[i, j] = col;
						}
						break;
					case Pixel.Mode.Mask:
						if (col.A == 255) {
							if (i >= 0 && i < textTarget.Width && j >= 0 && j < textTarget.Height) {
								textTarget[i, j] = col;
							}
						}
						break;
					case Pixel.Mode.Alpha:
						Pixel d = drawTarget[p.X, p.Y];
						float a = col.A / 255.0f * PixelBlend;
						float c = 1.0f - a;
						float r = a * col.R + c * d.R;
						float g = a * col.G + c * d.G;
						float b = a * col.B + c * d.B;
						Pixel pix = new Pixel((byte)r, (byte)g, (byte)b);
						if (i >= 0 && i < textTarget.Width && j >= 0 && j < textTarget.Height) {
							textTarget[i, j] = col;
						}

						break;
				}
			}

			int sx = 0;
			int sy = 0;

			foreach (char c in text) {
				if (c == '\n') {
					sx = 0;
					sy += Font.CharHeight * scale;
				} else {
					if (Font.Glyphs.TryGetValue(c, out Sprite cur)) {
						if (scale > 1) {
							for (int i = 0; i < cur.Width; i++) {
								for (int j = 0; j < cur.Height; j++) {
									if (cur[i, j].R > 0) {
										for (int ax = 0; ax < scale; ax++) {
											for (int ay = 0; ay < scale; ay++) {
												SetPixel(p.X + sx + (i * scale) + ax, p.Y + sy + (j * scale) + ay);
											}
										}
									}
								}
							}
						} else {
							for (int i = 0; i < cur.Width; i++) {
								for (int j = 0; j < cur.Height; j++) {
									if (cur[i, j].R > 0) {
										SetPixel(p.X + sx + i, p.Y + sy + j);
									}
								}
							}
						}

						sx += cur.Width * scale;
					} else {
						sx += scale * 8;
					}
				}
			}
		}
		#endregion

		#region Audio
		/// <summary> Overide this to generate constant audio. </summary>
		/// <remarks> Callback passed to <see cref="AudioEngine"/> when <see cref="Subsystem.Audio"/> is <see cref="Enable(Subsystem)"/>'d </remarks>
		/// <param name="channels"> Channel audio sample requested for </param>
		/// <param name="globalTime"> Current global time </param>
		/// <param name="timeStep"> Time between audio steps </param>
		/// <returns> Audio Sample at given <paramref name="globalTime"/> </returns>
		public virtual float OnSoundCreate(int channels, float globalTime, float timeStep) { return 0; }
		/// <summary> Overide this to filter audio. </summary>
		/// <remarks> Callback passed to <see cref="AudioEngine"/> when <see cref="Subsystem.Audio"/> is <see cref="Enable(Subsystem)"/>'d </remarks>
		/// <param name="channels"> Channel audio filter requested for </param>
		/// <param name="globalTime"> Current global time </param>
		/// <param name="sample"> Input sample to filter </param>
		/// <returns> Final filtered sample </returns>
		public virtual float OnSoundFilter(int channels, float globalTime, float sample) { return sample; }

		/// <summary> Helper to ask the <see cref="AudioEngine"/> <see cref="Subsystem"/> to load a <see cref="Sound"/> </summary>
		/// <param name="path"> Filename of sound to load </param>
		/// <returns> Loaded <see cref="Sound"/> if successful, null if failed </returns>
		public Sound LoadSound(string path) {
			if (audio != null) { return audio.LoadSound(path); }
			return null;
		}
		/// <summary> Helper to ask the <see cref="AudioEngine"/> <see cref="Subsystem"/> to play a <see cref="Sound"/> </summary>
		/// <param name="s"> <see cref="Sound"/> to play </param>
		public void PlaySound(Sound s) {
			if (audio != null) { audio.PlaySound(s); }
		}
		/// <summary> Helper to ask the <see cref="AudioEngine"/> <see cref="Subsystem"/> to stop a <see cref="Sound"/> </summary>
		/// <param name="s"> <see cref="Sound"/> to play </param>
		public void StopSound(Sound s) {
			if (audio != null) { audio.StopSound(s); }
		}
		#endregion
		#endregion

		#region Functionality
		/// <summary> Override this to insert logic before the Game loop begins </summary>
		public virtual void OnCreate() { }
		/// <summary> Override this to have logic run every frame </summary>
		public virtual void OnUpdate(float delta) { }
		/// <summary> Override this to have logic run when any mouse button is pressed </summary>
		public virtual void OnMousePress(Mouse m) { }
		/// <summary> Override this to have logic run when any mouse button is released </summary>
		public virtual void OnMouseRelease(Mouse m) { }
		/// <summary> Override this to have logic run while any mouse key is held </summary>
		public virtual void OnMouseDown(Mouse m) { }
		/// <summary> Override this to have logic run when the mouse wheel is spun </summary>
		public virtual void OnMouseScroll() { }
		/// <summary> Override this to have logic run when any key is pressed </summary>
		public virtual void OnKeyPress(Key k) { }
		/// <summary> Override this to have logic run when any key is released </summary>
		public virtual void OnKeyRelease(Key k) { }
		/// <summary> Override this to have logic run when any key is held down </summary>
		public virtual void OnKeyDown(Key k) { }
		/// <summary> Override this to have logic run before the game is destroyed. </summary>
		public virtual void OnDestroy() { }
		#endregion
	}
}
