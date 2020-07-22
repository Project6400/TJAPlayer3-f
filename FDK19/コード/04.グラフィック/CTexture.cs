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
		public int texture
		{
			get;
			private set;
		}

		public Vector3 vc拡大縮小倍率;

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
			this.b加算合成 = false;
			this.fZ軸中心回転 = 0f;
			this.vc拡大縮小倍率 = new Vector3(1f, 1f, 1f);
			//			this._txData = null;
		}

		/// <summary>
		/// <para>指定されたビットマップオブジェクトから Managed テクスチャを作成する。</para>
		/// <para>テクスチャのサイズは、BITMAP画像のサイズ以上、かつ、D3D9デバイスで生成可能な最小のサイズに自動的に調節される。
		/// その際、テクスチャの調節後のサイズにあわせた画像の拡大縮小は行わない。</para>
		/// <para>その他、ミップマップ数は 1、Usage は None、Pool は Managed、イメージフィルタは Point、ミップマップフィルタは
		/// None、カラーキーは 0xFFFFFFFF（完全なる黒を透過）になる。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="bitmap">作成元のビットマップ。</param>
		/// <param name="format">テクスチャのフォーマット。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(int device, Bitmap bitmap)
			: this()
		{
			//投げる
			MakeTexture(device, bitmap, true);
		}


		/// <summary>
		/// <para>空のテクスチャを作成する。</para>
		/// <para>テクスチャのサイズは、指定された希望サイズ以上、かつ、D3D9デバイスで生成可能な最小のサイズに自動的に調節される。
		/// その際、テクスチャの調節後のサイズにあわせた画像の拡大縮小は行わない。</para>
		/// <para>テクスチャのテクセルデータは未初期化。（おそらくゴミデータが入ったまま。）</para>
		/// <para>その他、ミップマップ数は 1、Usage は None、イメージフィルタは Point、ミップマップフィルタは None、
		/// カラーキーは 0x00000000（透過しない）になる。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="n幅">テクスチャの幅（希望値）。</param>
		/// <param name="n高さ">テクスチャの高さ（希望値）。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(int device, int n幅, int n高さ)
			: this()
		{
			try
			{
				this.texture = GL.GenTexture();
				this.sz画像サイズ = new Size(n幅, n高さ);
				this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(device, this.sz画像サイズ);
				this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);

				using (var bitmap = new Bitmap(1, 1))
				{
					using (var graphics = Graphics.FromImage(bitmap))
					{
						graphics.FillRectangle(Brushes.Black, 0, 0, 1, 1);
					}
					GL.BindTexture(TextureTarget.Texture2D, texture);

					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

					bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

					BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

					bitmap.UnlockBits(data);
				}
			}
			catch
			{
				this.Dispose();
				throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。\n({0}x{1})", n幅, n高さ));
			}
		}

		/// <summary>
		/// <para>画像ファイルからテクスチャを生成する。</para>
		/// <para>利用可能な画像形式は、BMP, JPG, PNG, TGA, DDS, PPM, DIB, HDR, PFM のいずれか。</para>
		/// <para>テクスチャのサイズは、画像のサイズ以上、かつ、D3D9デバイスで生成可能な最小のサイズに自動的に調節される。
		/// その際、テクスチャの調節後のサイズにあわせた画像の拡大縮小は行わない。</para>
		/// <para>その他、ミップマップ数は 1、Usage は None、イメージフィルタは Point、ミップマップフィルタは None になる。</para>
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="strファイル名">画像ファイル名。</param>
		/// <param name="format">テクスチャのフォーマット。</param>
		/// <param name="b黒を透過する">画像の黒（0xFFFFFFFF）を透過させるなら true。</param>
		/// <param name="pool">テクスチャの管理方法。</param>
		/// <exception cref="CTextureCreateFailedException">テクスチャの作成に失敗しました。</exception>
		public CTexture(int device, string strファイル名, bool b黒を透過する)
			: this()
		{
			MakeTexture(device, strファイル名, b黒を透過する);
		}
		public void MakeTexture(int device, string strファイル名, bool b黒を透過する)
		{
			if (!File.Exists(strファイル名))     // #27122 2012.1.13 from: ImageInformation では FileNotFound 例外は返ってこないので、ここで自分でチェックする。わかりやすいログのために。
				throw new FileNotFoundException(string.Format("ファイルが存在しません。\n[{0}]", strファイル名));

			Bitmap _txData = new Bitmap(strファイル名);
			MakeTexture(device, _txData, b黒を透過する);
		}

		public CTexture(int device, Bitmap bitmap, bool b黒を透過する)
			: this()
		{
			MakeTexture(device, bitmap, b黒を透過する);
		}
		public void MakeTexture(int device, Bitmap bitmap, bool b黒を透過する)
		{
			this.texture = GL.GenTexture();
			this.sz画像サイズ = new Size(bitmap.Width, bitmap.Height);
			this.szテクスチャサイズ = this.t指定されたサイズを超えない最適なテクスチャサイズを返す(device, this.sz画像サイズ);
			this.rc全画像 = new Rectangle(0, 0, this.sz画像サイズ.Width, this.sz画像サイズ.Height);
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, texture);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

				bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

				bitmap.UnlockBits(data);
			}
			catch 
			{
				this.Dispose();
				throw new CTextureCreateFailedException(string.Format("テクスチャの生成に失敗しました。"));
			}
		}
		// メソッド

		// 2016.11.10 kairera0467 拡張
		// Rectangleを使う場合、座標調整のためにテクスチャサイズの値をそのまま使うとまずいことになるため、Rectragleから幅を取得して調整をする。
		public void t2D中心基準描画(int device, int x, int y)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
		}
		public void t2D中心基準描画(int device, int x, int y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height / 2), 1f, rc画像内の描画領域);
		}
		public void t2D中心基準描画(int device, float x, float y)
		{
			this.t2D描画(device, (int)x - (this.szテクスチャサイズ.Width / 2), (int)y - (this.szテクスチャサイズ.Height / 2), 1f, this.rc全画像);
		}
		public void t2D中心基準描画(int device, float x, float y, float depth, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, (int)x - (rc画像内の描画領域.Width / 2), (int)y - (rc画像内の描画領域.Height / 2), depth, rc画像内の描画領域);
		}

		// 下を基準にして描画する(拡大率考慮)メソッドを追加。 (AioiLight)
		public void t2D拡大率考慮下基準描画(int device, int x, int y)
		{
			this.t2D描画(device, x, y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮下基準描画(int device, int x, int y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x, y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮下中心基準描画(int device, int x, int y)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (szテクスチャサイズ.Height * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮下中心基準描画(int device, float x, float y)
		{
			this.t2D拡大率考慮下中心基準描画(device, (int)x, (int)y);
		}

		public void t2D拡大率考慮下中心基準描画(int device, int x, int y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - ((rc画像内の描画領域.Width / 2)), y - (rc画像内の描画領域.Height * this.vc拡大縮小倍率.Y), 1f, rc画像内の描画領域);
		}
		public void t2D拡大率考慮下中心基準描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D拡大率考慮下中心基準描画(device, (int)x, (int)y, rc画像内の描画領域);
		}
		public void t2D中央基準描画(int device, int x, int y)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), this.rc全画像);
		}
		public void t2D中央基準描画(int device, int x, int y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (this.szテクスチャサイズ.Height / 2), rc画像内の描画領域);
		}
		public void t2D下中央基準描画(int device, int x, int y)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2), y - (szテクスチャサイズ.Height), this.rc全画像);
		}
		public void t2D下中央基準描画(int device, int x, int y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x - (rc画像内の描画領域.Width / 2), y - (rc画像内の描画領域.Height), rc画像内の描画領域);
			//this.t2D描画(devicek x, y, rc画像内の描画領域;
		}


		public void t2D拡大率考慮中央基準描画(int device, int x, int y)
		{
			this.t2D描画(device, x - (this.szテクスチャサイズ.Width / 2 * this.vc拡大縮小倍率.X), y - (szテクスチャサイズ.Height / 2 * this.vc拡大縮小倍率.Y), 1f, this.rc全画像);
		}
		public void t2D拡大率考慮中央基準描画(int device, float x, float y)
		{
			this.t2D拡大率考慮中央基準描画(device, (int)x, (int)y);
		}


		/// <summary>
		/// テクスチャを 2D 画像と見なして描画する。
		/// </summary>
		/// <param name="device">Direct3D9 デバイス。</param>
		/// <param name="x">描画位置（テクスチャの左上位置の X 座標[dot]）。</param>
		/// <param name="y">描画位置（テクスチャの左上位置の Y 座標[dot]）。</param>
		public void t2D描画(int device, float x, float y, Rectangle rc画像内の描画領域)
		{
			this.t2D描画(device, x, y, 1f, rc画像内の描画領域);
		}
		public void t2D描画(int device, float x, float y)
		{
			this.t2D描画(device, x, y, 1f, this.rc全画像);
		}
		public void t2D描画(int device, float xi, float yi, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				return;

			this.tレンダリングステートの設定(device);

			#region [ (A) 回転あってもなくても ]
			//-----------------
			float f補正値X = -0.5f;    // -0.5 は座標とピクセルの誤差を吸収するための座標補正値。(MSDN参照)
			float f補正値Y = -0.5f;    //
			float w = rc画像内の描画領域.Width;
			float h = rc画像内の描画領域.Height;
			float f左U値 = ((float)rc画像内の描画領域.Left) / ((float)this.szテクスチャサイズ.Width);
			float f右U値 = ((float)rc画像内の描画領域.Right) / ((float)this.szテクスチャサイズ.Width);
			float f上V値 = ((float)rc画像内の描画領域.Top) / ((float)this.szテクスチャサイズ.Height);
			float f下V値 = ((float)rc画像内の描画領域.Bottom) / ((float)this.szテクスチャサイズ.Height);
			this.color4.A = ((float)this._opacity) / 255f;

			float x = xi / 100.0f * 1.15f;
			float y = yi / 100.0f * 1.15f;

			float left = (rc画像内の描画領域.X / rc全画像.Width);
			float right = (rc画像内の描画領域.X + rc画像内の描画領域.Width / rc全画像.Width);
			float up = (rc画像内の描画領域.Y + rc画像内の描画領域.Height / rc全画像.Height);
			float down = (rc画像内の描画領域.Y / rc全画像.Height);

			//メインのポリゴン表示
			GL.BindTexture(TextureTarget.Texture2D, this.texture);

			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Color4(new Color4(1f, 1f, 1f, 0.4f));
			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(right, up);
			GL.Vertex3(x + rc画像内の描画領域.Width / 2.0 / 100.0 * 1.15, y + rc画像内の描画領域.Height / 2.0 / 100.0 * 1.15, 0);
			GL.TexCoord2(left, up);
			GL.Vertex3(x - rc画像内の描画領域.Width / 2.0 / 100.0 * 1.15, y + rc画像内の描画領域.Height / 2.0 / 100.0 * 1.15, 0);
			GL.TexCoord2(left, down);
			GL.Vertex3(x - rc画像内の描画領域.Width / 2.0 / 100.0 * 1.15, y - rc画像内の描画領域.Height / 2.0 / 100.0 * 1.15, 0);
			GL.TexCoord2(right, down);
			GL.Vertex3(x + rc画像内の描画領域.Width / 2.0 / 100.0 * 1.15, y - rc画像内の描画領域.Height / 2.0 / 100.0 * 1.15, 0);
			GL.End();
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
		public void t2D上下反転描画(int device, int xi, int yi, float depth, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				throw new InvalidOperationException("テクスチャは生成されていません。");

			this.tレンダリングステートの設定(device);

			this.color4.A = ((float)this._opacity) / 255f;
			this.tレンダリングステートの設定(device);

			float x = xi / 100.0f * 1.15f;
			float y = yi / 100.0f * 1.15f;

			float left = (rc画像内の描画領域.X / rc全画像.Width);
			float right = (rc画像内の描画領域.X + rc画像内の描画領域.Width / rc全画像.Width);
			float up = (rc画像内の描画領域.Y + rc画像内の描画領域.Height / rc全画像.Height);
			float down = (rc画像内の描画領域.Y / rc全画像.Height);

			GL.BindTexture(TextureTarget.Texture2D, this.texture);

			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Color4(color4);
			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(right, down);
			GL.Vertex3(x + rc画像内の描画領域.Width / 100.0 * 1.15, y, 0);
			GL.TexCoord2(left, down);
			GL.Vertex3(x, y, 0);
			GL.TexCoord2(left, up);
			GL.Vertex3(x, y - rc画像内の描画領域.Height / 100.0 * 1.15, 0);
			GL.TexCoord2(right, up);
			GL.Vertex3(x + rc画像内の描画領域.Width / 100.0 * 1.15, y - rc画像内の描画領域.Height / 100.0 * 1.15, 0);
			GL.End();
		}

		/// <summary>
		/// テクスチャを 3D 画像と見なして描画する。
		/// </summary>
		public void t3D描画(int device, Matrix4 mat)
		{
			this.t3D描画(device, mat, this.rc全画像);
		}
		public void t3D描画(int device, Matrix4 mat, Rectangle rc画像内の描画領域)
		{
			if (this.texture == null)
				return;

			float xi = ((float)rc画像内の描画領域.Width) / 2f;
			float yi = ((float)rc画像内の描画領域.Height) / 2f;
			this.color4.A = ((float)this._opacity) / 255f;

			this.tレンダリングステートの設定(device);

			float x = xi / 100.0f * 1.15f;
			float y = yi / 100.0f * 1.15f;

			float left = (rc画像内の描画領域.X / rc全画像.Width);
			float right = (rc画像内の描画領域.X + rc画像内の描画領域.Width / rc全画像.Width);
			float up = (rc画像内の描画領域.Y + rc画像内の描画領域.Height / rc全画像.Height);
			float down = (rc画像内の描画領域.Y / rc全画像.Height);


			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Color4(color4);
			GL.Begin(BeginMode.Quads);
			GL.TexCoord2(right, down);
			GL.Vertex3(x + rc画像内の描画領域.Width / 100.0 * 1.15, y, 0);
			GL.TexCoord2(left, down);
			GL.Vertex3(x, y, 0);
			GL.TexCoord2(left, up);
			GL.Vertex3(x, y - rc画像内の描画領域.Height / 100.0 * 1.15, 0);
			GL.TexCoord2(right, up);
			GL.Vertex3(x + rc画像内の描画領域.Width / 100.0 * 1.15, y - rc画像内の描画領域.Height / 100.0 * 1.15, 0);
			GL.End();
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if (!this.bDispose完了済み)
			{
				GL.DeleteTexture(this.texture);
				this.bDispose完了済み = true;
			}
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private int _opacity;
		private bool bDispose完了済み;
		//		byte[] _txData;
		static object lockobj = new object();

		/// <summary>
		/// どれか一つが有効になります。
		/// </summary>
		/// <param name="device">Direct3Dのデバイス</param>
		private void tレンダリングステートの設定(int device)
		{
			if (this.b加算合成)
			{
				GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
			}
			else if (this.b乗算合成)
			{
				GL.BlendFunc(BlendingFactor.Zero, BlendingFactor.SrcColor);
			}
			else if (this.b減算合成)
			{
				GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
			}
			else if (this.bスクリーン合成)
			{
				GL.BlendFunc(BlendingFactor.OneMinusConstantColor, BlendingFactor.One);
			}
			else
			{
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			}
		}
		private Size t指定されたサイズを超えない最適なテクスチャサイズを返す(int device, Size sz指定サイズ)
		{
			return sz指定サイズ;
		}


		// 2012.3.21 さらなる new の省略作戦

		protected Rectangle rc全画像;                              // テクスチャ作ったらあとは不変
		public Color4 color4 = new Color4(1f, 1f, 1f, 1f);  // アルファ以外は不変
															//-----------------
		#endregion
	}
}
