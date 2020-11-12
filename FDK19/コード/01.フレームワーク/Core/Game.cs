﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Text;
using OpenTK;
using OpenTK.Graphics;

namespace FDK
{
	public class Game : GameWindow
	{
		/// <summary>
		/// 2020/10/09 Mr-Ojii 勝手に追加
		/// TJAPlayer3.app.DeviceをGame側で実装してしまえ！という試み
		/// </summary>
		public int Device
		{
			get
			{
				return 0;
			}
		}

		public Game()
			: base(GameWindowSize.Width, GameWindowSize.Height, GraphicsMode.Default, "TJAP3-f(OpenGL)Alpha")
		{
			FFmpeg.AutoGen.ffmpeg.RootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\ffmpeg\";

			if (!Directory.Exists(FFmpeg.AutoGen.ffmpeg.RootPath))
				throw new DirectoryNotFoundException("FFmpeg RootPath Not Found.");
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//CP932用
		}
	}
}