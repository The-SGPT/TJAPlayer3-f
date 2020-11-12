﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;

namespace FDK
{
	public class CTexture : IDisposable
	{
		// プロパティ
		public bool b加算合成
		{
			get;
			set;
		}
		public bool b乗算合成
		{
			get;
			set;
		}
		public bool b減算合成
		{
			get;
			set;
		}
		public bool bスクリーン合成
		{
			get;
			set;
		}
		public float fZ軸中心回転
		{
			get;
			set;
		}
		public int Opacity
		{
			get
			{
				return this._opacity;
			}
			set
			{
				if (value < 0)
				{
					this._opacity = 0;
				}
				else if (value > 0xff)
				{
					this._opacity = 0xff;
				}
				else
				{
					this._opacity = value;
				}
			}
		}
		public Size szテクスチャサイズ
		{
			get;
			private set;
		}
		public Size sz画像サイズ
		{
			get;
			protected set;
		}
		public int? texture
		{
			get;
			private set;
		}
		public System.Numerics.Vector3 vc拡大縮小倍率;
		private Vector3 vc;
		public string filename;

		// 画面が変わるたび以下のプロパティを設定し治すこと。

		public static Size sz論理画面 = Size.Empty;
		public static Size sz物理画面 = Size.Empty;
		public static Rectangle rc物理画面描画領域 = Rectangle.Empty;
		/// <summary>
		/// <para>論理画面を1とする場合の物理画面の倍率。</para>
		/// <para>論理値×画面比率＝物理値。</para>
		/// </summary>
		public static float f画面比率 = 1.0f;

		// コンストラクタ

		public CTexture()
		{
			this.sz画像サイズ = new Size(0, 0);
			this.szテクスチャサイズ = new Size(0, 0);
			this._opacity = 0xff;
			this.texture = null;
			this.bSharpDXTextureDispose完了済み = true;
			this.b加算合成 = false;
			this.fZ軸中心回転 = 0f;
			this.vc拡大縮小倍率 = new System.Numerics.Vector3(1f, 1f, 1f);
			this.vc = new Vector3(1f, 1f, 1f);
			this.filename = "";
			//			this._txData = null;
		}

		/// <summary>
		/// <para>指定された画像ファイルから Managed テクスチャを作成する。</para>
		/// <para>利用可能な画像形式は、BMP, JPG, PNG, TGA, DDS, PPM, DIB, HDR, PFM のいずれか。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="strファイル名">画像ファイル名。</param>
		/// <param name="format">テクスチャのフォーマット。</param>
		/// <param name="b黒を透過する">画像の黒（0xFFFFFFFF）を透過させるなら true。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(int device, string strファイル名)
			: this()
		{
			maketype = MakeType.filename;
			MakeTexture(device, strファイル名);
		}
		public CTexture(int device, Bitmap bitmap, bool b黒を透過する)
			: this()
		{
			maketype = MakeType.bitmap;
			MakeTexture(device, bitmap, b黒を透過する);
		}

		public void MakeTexture(int device, string strファイル名)
		{
			if (!File.Exists(strファイル名))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
				throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strファイル名));

			MakeTexture(device, new Bitmap(strファイル名), false);
		}
		public void MakeTexture(int device, Bitmap bitmap, bool b黒を透過する)
		{
			if (b黒を透過する)
				bitmap.MakeTransparent(Color.Black);
			bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
			try
			{
				this.sz画像サイズ = new Size(bitmap.Width, bitmap.Height);
				this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
				this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(device, this.sz画像サイズ);

				this.texture = GL.GenTexture();

				//テクスチャ用バッファのひもづけ
				GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

				//テクスチャの設定
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				//テクスチャ用バッファに色情報を流し込む
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

				bitmap.UnlockBits(data);

				this.bSharpDXTextureDispose完了済み = false;
			}
			catch
			{
				this.Dispose();
				throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n"));
			}

		}
		// メソッド

		// 2016.11.10 kairera0467 拡張
		// Rectangleを使う場合、座標調整のためにテクスチャサイズの値をそのまま使うとまずいことになるため、Rectragleから幅を取得して調整をする。
		public void t2D中心基準描画(int device, float x, float y)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
		}
		public void t2D中心基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height / 2), 1f, rc画像内の描画領域);
		}
		public void t2D中心基準描画(int device, float x, float y, float depth, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, (int)x - (rc画像内の描画領域.Width / 2), (int)y - (rc画像内の描画領域.Height / 2), depth, rc画像内の描画領域);
		}
		public void t2D拡大率考慮右上基準描画(int device, float x, float y)
		{
			this.t2D描画(device, (x - (this.rc全画像.Width * this.vc拡大縮小倍率.X)), y);
		}
		public void t2D拡大率考慮左下基準描画(int device, float x, float y)
		{
			this.t2D描画(device, x, y - (this.szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y));
		}

		// 下を基準にして描画する(拡大率考慮)メソッドを追加。 (AioiLight)
		public void t2D拡大率考慮下基準描画(int device, float x, float y)
		{
			this.t2D描画(device, x, y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮下基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x, y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
		}
		public void t2D下中心基準描画(int device, float x, float y)
		{
			this.t2D描画(device, x, y, this.rc全画像);
		}
		public void t2D下中心基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - szテクスチャサイズ.Height, 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮下中心基準描画(int device, float x, float y)
		{
			this.t2D拡大率考慮下中心基準描画(device, x, y, this.rc全画像);
		}
		public void t2D拡大率考慮下中心基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - ((rc画像内の描画領域.Width / 2)), y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
		}

		public void t2D拡大率考慮下拡大率考慮中心基準描画(int device, float x, float y)
		{
			this.t2D拡大率考慮下拡大率考慮中心基準描画(device, x, y, this.rc全画像);
		}
		public void t2D拡大率考慮下拡大率考慮中心基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width * this.vc拡大縮小倍率.X / 2), y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
		}


		public void t2D中央基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), rc画像内の描画領域);
		}
		public void t2D下中央基準描画(int device, float x, float y)
		{
			this.t2D下中央基準描画(device, x, y, this.rc全画像);
		}
		public void t2D下中央基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height), rc画像内の描画領域);
		}

		public void t2D拡大率考慮中央基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (rc画像内の描画領域.Width / 2 * this.vc拡大縮小倍率.X), y - (rc画像内の描画領域.Height / 2 * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮中央基準描画(int device, float x, float y)
		{
			this.t2D拡大率考慮中央基準描画(device, x, y, this.rc全画像);
		}

		/// <summary>
		/// テクスチャを 2D 画像と見なして描画する。
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
		/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
		public void t2D描画(int device, float x, float y)
		{
			this.t2D描画(device, x, y, 1f, this.rc全画像);
		}
		public void t2D描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x, y, 1f, rc画像内の描画領域);
		}
		public void t2D描画(int device, float x, float y, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				return;

			this.tレンダリングステートの設定(device);

			if (this.fZ軸中心回転 == 0f)
			{
				#region [ (A) 回転なし ]
				//-----------------
				float f補正値X = -GameWindowSize.Width / 2f - 0.5f;
				float f補正値Y = -GameWindowSize.Height / 2f - 0.5f;
				float w = rc画像内の描画領域.Width;
				float h = rc画像内の描画領域.Height;
				float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
				float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
				float f上V値 = ((float)(rc全画像.Bottom - rc画像内の描画領域.Top)) / ((float)this.szテクスチャサイズ.Height);
				float f下V値 = ((float)(rc全画像.Bottom - rc画像内の描画領域.Bottom)) / ((float)this.szテクスチャサイズ.Height);

				this.color = Color.FromArgb(this._opacity, this.color.R, this.color.G, this.color.B);

				LoadProjectionMatrix(Matrix4.Identity);

				GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

				GL.Begin(PrimitiveType.Quads);

				GL.Color4(this.color);

				GL.TexCoord2(f右U値, f上V値);
				GL.Vertex3(-(x + (w * this.vc拡大縮小倍率.X) + f補正値X), -(y + f補正値Y), depth);

				GL.TexCoord2(f左U値, f上V値);
				GL.Vertex3(-(x + f補正値X), -(y + f補正値Y), depth);

				GL.TexCoord2(f左U値, f下V値);
				GL.Vertex3(-(x + f補正値X), -((y + (h * this.vc拡大縮小倍率.Y)) + f補正値Y), depth);

				GL.TexCoord2(f右U値, f下V値);
				GL.Vertex3(-(x + (w * this.vc拡大縮小倍率.X) + f補正値X), -((y + (h * this.vc拡大縮小倍率.Y)) + f補正値Y), depth);

				GL.End();
				//-----------------
				#endregion
			}
			else
			{
				#region [ (B) 回転あり ]
				//-----------------
				float f中央X = ((float)rc画像内の描画領域.Width) / 2f;
				float f中央Y = ((float)rc画像内の描画領域.Height) / 2f;
				float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
				float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
				float f上V値 = ((float)rc画像内の描画領域.Top) / ((float)this.szテクスチャサイズ.Height);
				float f下V値 = ((float)rc画像内の描画領域.Bottom) / ((float)this.szテクスチャサイズ.Height);

				this.color = Color.FromArgb(this._opacity, this.color.R, this.color.G, this.color.B);

				float n描画領域内X = x + (rc画像内の描画領域.Width / 2.0f);
				float n描画領域内Y = y + (rc画像内の描画領域.Height / 2.0f);
				var vc3移動量 = new Vector3(n描画領域内X - (((float)GameWindowSize.Width) / 2f), -(n描画領域内Y - (((float)GameWindowSize.Height) / 2f)), 0f);

				this.vc.X = this.vc拡大縮小倍率.X;
				this.vc.Y = this.vc拡大縮小倍率.Y;
				this.vc.Z = this.vc拡大縮小倍率.Z;

				var matrix = Matrix4.Identity * Matrix4.CreateScale(this.vc);
				matrix *= Matrix4.CreateRotationZ(this.fZ軸中心回転);
				matrix *= Matrix4.CreateTranslation(vc3移動量);
				
				LoadProjectionMatrix(matrix);

				GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

				GL.Begin(PrimitiveType.Quads);

				GL.Color4(this.color);

				GL.TexCoord2(f右U値, f上V値);
				GL.Vertex3(-f中央X, -f中央Y, depth);

				GL.TexCoord2(f左U値, f上V値);
				GL.Vertex3(f中央X, -f中央Y, depth);

				GL.TexCoord2(f左U値, f下V値);
				GL.Vertex3(f中央X, f中央Y, depth);

				GL.TexCoord2(f右U値, f下V値);
				GL.Vertex3(-f中央X, f中央Y, depth);

				GL.End();
				//-----------------
				#endregion
			}
		}


		public void t2D幕用描画(int device, float x, float y, Rectangle rc画像内の描画領域, bool left, int num = 0)
		{
			if (this.texture == null)
				return;

			this.tレンダリングステートの設定(device);

			#region [ (A) 回転なし ]
			//-----------------
			float f補正値X = -GameWindowSize.Width / 2f - 0.5f;
			float f補正値Y = -GameWindowSize.Height / 2f - 0.5f;
			float w = rc画像内の描画領域.Width;
			float h = rc画像内の描画領域.Height;
			float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
			float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
			float f上V値 = ((float)(rc全画像.Bottom - rc画像内の描画領域.Top)) / ((float)this.szテクスチャサイズ.Height);
			float f下V値 = ((float)(rc全画像.Bottom - rc画像内の描画領域.Bottom)) / ((float)this.szテクスチャサイズ.Height);

			this.color = Color.FromArgb(this._opacity, this.color.R, this.color.G, this.color.B);

			LoadProjectionMatrix(Matrix4.Identity);

			GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

			GL.Begin(PrimitiveType.Quads);

			GL.Color4(this.color);

			GL.TexCoord2(f右U値, f上V値);
			GL.Vertex3(-(x + (w * this.vc拡大縮小倍率.X) + f補正値X), -(y + f補正値Y), 1f);

			GL.TexCoord2(f左U値, f上V値);
			GL.Vertex3(-(x + f補正値X), -(y + f補正値Y), 1f);

			GL.TexCoord2(f左U値, f下V値);
			GL.Vertex3(-(x + f補正値X) - ((!left) ? num : 0), -((y + (h * this.vc拡大縮小倍率.Y)) + f補正値Y), 1f);

			GL.TexCoord2(f右U値, f下V値);
			GL.Vertex3(-(x + (w * this.vc拡大縮小倍率.X) + f補正値X) + ((left) ? num : 0), -((y + (h * this.vc拡大縮小倍率.Y)) + f補正値Y), 1f);

			GL.End();

			//-----------------
			#endregion
		}

		public void t2D上下反転描画(int device, int x, int y)
		{
			this.t2D上下反転描画(device, x, y, 1f, this.rc全画像);
		}
		public void t2D上下反転描画(int device, int x, int y, Rectangle rc画像内の描画領域)
		{
			this.t2D上下反転描画(device, x, y, 1f, rc画像内の描画領域);
		}
		public void t2D上下反転描画(int device, int x, int y, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				throw new InvalidOperationException("テクスチャは生成されていません。");

			this.tレンダリングステートの設定(device);

			float f補正値X = -GameWindowSize.Width / 2f - 0.5f;
			float f補正値Y = -GameWindowSize.Height / 2f - 0.5f;
			float w = rc画像内の描画領域.Width;
			float h = rc画像内の描画領域.Height;
			float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
			float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
			float f上V値 = ((float)(rc全画像.Bottom - rc画像内の描画領域.Top)) / ((float)this.szテクスチャサイズ.Height);
			float f下V値 = ((float)(rc全画像.Bottom - rc画像内の描画領域.Bottom)) / ((float)this.szテクスチャサイズ.Height);

			this.color = Color.FromArgb(this._opacity, this.color.R, this.color.G, this.color.B);

			LoadProjectionMatrix(Matrix4.Identity);

			GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

			GL.Begin(PrimitiveType.Quads);

			GL.Color4(this.color);

			GL.TexCoord2(f右U値, f上V値);
			GL.Vertex3(-(x + (w * this.vc拡大縮小倍率.X) + f補正値X), -((y + (h * this.vc拡大縮小倍率.Y)) + f補正値Y), depth);

			GL.TexCoord2(f左U値, f上V値);
			GL.Vertex3(-(x + f補正値X), -((y + (h * this.vc拡大縮小倍率.Y)) + f補正値Y), depth);

			GL.TexCoord2(f左U値, f下V値);
			GL.Vertex3(-(x + f補正値X), -(y + f補正値Y), depth);

			GL.TexCoord2(f右U値, f下V値);
			GL.Vertex3(-(x + (w * this.vc拡大縮小倍率.X) + f補正値X), -(y + f補正値Y), depth);

			GL.End();
		}
		public void t2D上下反転描画(int device, Point pt)
		{
			this.t2D上下反転描画(device, pt.X, pt.Y, 1f, this.rc全画像);
		}
		public void t2D上下反転描画(int device, Point pt, Rectangle rc画像内の描画領域)
		{
			this.t2D上下反転描画(device, pt.X, pt.Y, 1f, rc画像内の描画領域);
		}
		public void t2D上下反転描画(int device, Point pt, float depth, Rectangle rc画像内の描画領域)
		{
			this.t2D上下反転描画(device, pt.X, pt.Y, depth, rc画像内の描画領域);
		}

		public static Vector3 t論理画面座標をワールド座標へ変換する(int x, int y)
		{
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3((float)x, (float)y, 0f));
		}
		public static Vector3 t論理画面座標をワールド座標へ変換する(float x, float y)
		{
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3(x, y, 0f));
		}
		public static Vector3 t論理画面座標をワールド座標へ変換する(Point pt論理画面座標)
		{
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3(pt論理画面座標.X, pt論理画面座標.Y, 0.0f));
		}
		public static Vector3 t論理画面座標をワールド座標へ変換する(Vector2 v2論理画面座標)
		{
			return CTexture.t論理画面座標をワールド座標へ変換する(new Vector3(v2論理画面座標.X, v2論理画面座標.Y, 0f));
		}
		public static Vector3 t論理画面座標をワールド座標へ変換する(Vector3 v3論理画面座標)
		{
			return new Vector3(
				(v3論理画面座標.X - (CTexture.sz論理画面.Width / 2.0f)) * CTexture.f画面比率,
				(-(v3論理画面座標.Y - (CTexture.sz論理画面.Height / 2.0f)) * CTexture.f画面比率),
				v3論理画面座標.Z);
		}

		/// <summary>
		/// テクスチャを 3D 画像と見なして描画する。
		/// </summary>
		public void t3D描画(int device, System.Numerics.Matrix4x4 mat)
		{
			this.t3D描画(device, mat, this.rc全画像);
		}
		public void t3D描画(int device, System.Numerics.Matrix4x4 mat, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				return;

			matrix.M11 = mat.M11;
			matrix.M12 = mat.M12;
			matrix.M13 = mat.M13;
			matrix.M14 = mat.M14;
			matrix.M21 = mat.M21;
			matrix.M22 = mat.M22;
			matrix.M23 = mat.M23;
			matrix.M24 = mat.M24;
			matrix.M31 = mat.M31;
			matrix.M32 = mat.M32;
			matrix.M33 = mat.M33;
			matrix.M34 = mat.M34;
			matrix.M41 = mat.M41;
			matrix.M42 = mat.M42;
			matrix.M43 = mat.M43;
			matrix.M44 = mat.M44;

			float x = ((float)rc画像内の描画領域.Width) / 2f;
			float y = ((float)rc画像内の描画領域.Height) / 2f;
			float z = 0.0f;
			float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
			float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
			float f上V値 = ((float)rc画像内の描画領域.Top) / ((float)this.szテクスチャサイズ.Height);
			float f下V値 = ((float)rc画像内の描画領域.Bottom) / ((float)this.szテクスチャサイズ.Height);

			this.color = Color.FromArgb(this._opacity, this.color.R, this.color.G, this.color.B);

			this.tレンダリングステートの設定(device);

			LoadProjectionMatrix(matrix);

			GL.BindTexture(TextureTarget.Texture2D, (int)this.texture);

			GL.Begin(PrimitiveType.Quads);

			GL.Color4(this.color);

			GL.TexCoord2(f右U値, f上V値);
			GL.Vertex3(x, y, z);

			GL.TexCoord2(f左U値, f上V値);
			GL.Vertex3(-x, y, z);

			GL.TexCoord2(f左U値, f下V値);
			GL.Vertex3(-x, -y, z);

			GL.TexCoord2(f右U値, f下V値);
			GL.Vertex3(x, -y, z);

			GL.End();

			//device.SetTransform(TransformState.World, matrix);
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if (!this.bDispose完了済み)
			{
				// テクスチャの破棄
				if (this.texture != null)
				{
					this.bSharpDXTextureDispose完了済み = true;
					GL.DeleteTexture((int)this.texture);
					this.texture = null;
				}

				this.bDispose完了済み = true;
			}
		}
		~CTexture()
		{
			// ファイナライザの動作時にtextureのDisposeがされていない場合は、
			// CTextureのDispose漏れと見做して警告をログ出力する
			if (!this.bSharpDXTextureDispose完了済み)//DTXManiaより
			{
				Trace.TraceWarning("CTexture: Dispose漏れを検出しました。(Size=({0}, {1}), filename={2}, maketype={3})", sz画像サイズ.Width, sz画像サイズ.Height, filename, maketype.ToString());
			}
			//マネージド リソースらしいので、解放はしない
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private int _opacity;
		private bool bDispose完了済み, bSharpDXTextureDispose完了済み;

		/// <summary>
		/// どれか一つが有効になります。
		/// </summary>
		/// <param name="device">Direct3Dのデバイス</param>
		private void tレンダリングステートの設定(int device)
		{
			if (this.b加算合成)
			{
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
			}
			else if (this.b乗算合成)
			{
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				GL.BlendFunc(BlendingFactor.Zero, BlendingFactor.SrcColor);
			}
			else if (this.b減算合成)
			{
				GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
			}
			else if (this.bスクリーン合成)
			{
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				GL.BlendFunc(BlendingFactor.OneMinusDstColor, BlendingFactor.One);
			}
			else
			{
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			}
		}
		private Size t指定されたサイズを超えない最適なテクスチャサイズを返す(int device, Size sz指定サイズ)
		{
			return sz指定サイズ;
		}
		private void LoadProjectionMatrix(Matrix4 mat)
		{
			Matrix4 tmpmat = CAction.Projection;
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref tmpmat);
			GL.MultMatrix(ref mat);
		}
		private enum MakeType
		{
			filename,
			bytearray,
			bitmap
		}

		// 2012.3.21 さらなる new の省略作戦

		protected Rectangle rc全画像;                              // テクスチャ作ったらあとは不変
		public Color color = Color.FromArgb(255, 255, 255, 255);
		private Matrix4 matrix = Matrix4.Identity;
		private MakeType maketype = MakeType.bytearray;
		//-----------------
		#endregion
	}
}
