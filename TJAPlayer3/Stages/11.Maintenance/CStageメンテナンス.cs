using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FDK;

namespace TJAPlayer3
{
    class CStageメンテナンス : CStage
    {
		// コンストラクタ

		public CStageメンテナンス()
		{
			base.eステージID = CStage.Eステージ.メンテ;
			base.b活性化してない = true;
		}
		// CStage 実装

		public override void On活性化()
		{
			Trace.TraceInformation("メンテナンスステージを活性化します。");
			Trace.Indent();
			try
			{
				Discord.UpdatePresence("", "Maintenance", TJAPlayer3.StartupTime);
				don =TJAPlayer3.ColorTexture("#ff4000",Width,Height);
				ka = TJAPlayer3.ColorTexture("#00c8ff",Width,Height);
				base.On活性化();
			}
			finally
			{
				Trace.TraceInformation("メンテナンスの活性化を完了しました。");
				Trace.Unindent();
			}

		}

		public override void On非活性化()
		{
			Trace.TraceInformation("メンテナンスステージを非活性化します。");
			Trace.Indent();
			try
			{
			}
			finally
			{
				Trace.TraceInformation("メンテナンスステージの非活性化を完了しました。");
				Trace.Unindent();
			}
			base.On非活性化();
		}

		public override void OnManagedリソースの作成()
		{
			if (!base.b活性化してない)
			{
				base.OnManagedリソースの作成();
			}

		}
		public override void OnManagedリソースの解放()
		{
			if (!base.b活性化してない)
			{
				base.OnManagedリソースの解放();
			}

		}

		public override int On進行描画()
		{
			if (base.b初めての進行描画)
			{
				base.b初めての進行描画 = false;
			}
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
				ka.t2D下中央基準描画(TJAPlayer3.app.Device, 640 - (Sabunn + Width) * 4, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed))
				don.t2D下中央基準描画(TJAPlayer3.app.Device, 640 - (Sabunn + Width) * 3, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
				don.t2D下中央基準描画(TJAPlayer3.app.Device, 640 - (Sabunn + Width) * 2, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
				ka.t2D下中央基準描画(TJAPlayer3.app.Device, 640 - (Sabunn + Width) * 1, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue2P))
				ka.t2D下中央基準描画(TJAPlayer3.app.Device, 640 + (Sabunn + Width) * 1, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed2P))
				don.t2D下中央基準描画(TJAPlayer3.app.Device, 640 + (Sabunn + Width) * 2, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed2P))
				don.t2D下中央基準描画(TJAPlayer3.app.Device, 640 + (Sabunn + Width) * 3, y);
			if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue2P))
				ka.t2D下中央基準描画(TJAPlayer3.app.Device, 640 + (Sabunn + Width) * 4, y);

			if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
				return 1;
			return 0;
		}
		#region[private]
		private CTexture don;
		private CTexture ka;
		private const int Width = 100;
		private const int Height = 100;
		private const int y = 480;

		private const int Sabunn = 16;
		#endregion
	}
}
