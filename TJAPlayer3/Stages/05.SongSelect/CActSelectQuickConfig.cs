﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.IO;
using FDK;

namespace TJAPlayer3
{
	internal class CActSelectQuickConfig : CActSelectPopupMenu
	{
		private readonly string QuickCfgTitle = "P Quick Config";
		// コンストラクタ

		public CActSelectQuickConfig()
		{
			CActSelectQuickConfigMain(0);
		}

		private void CActSelectQuickConfigMain(int nPlayer)
		{
			lci = new List<List<CItemBase>>();									// この画面に来る度に、メニューを作り直す。
			for ( int nConfSet = 0; nConfSet < 3; nConfSet++ )
			{
				lci.Add(MakeListCItemBase(nConfSet, nPlayer));									// ConfSet用の3つ分の枠。				
			}
			base.Initialize( lci[ 0 ], true, (nPlayer+1).ToString() + QuickCfgTitle, 0 );	// ConfSet=0
		}

		private List<CItemBase> MakeListCItemBase( int nConfigSet,int nPlayer )
		{
			this.nPlayer = nPlayer;
			List<CItemBase> l = new List<CItemBase>();

			#region [ 共通 Target/AutoMode/AutoLane ]
			#endregion
			#region [ 個別 ScrollSpeed ]
			l.Add( new CItemInteger( "ばいそく", 0, 1999, TJAPlayer3.ConfigIni.n譜面スクロール速度[nPlayer],
				"演奏時のドラム譜面のスクロールの\n" +
				"速度を指定します。\n" +
				"x0.1 ～ x200.0 を指定可能です。",
				"To change the scroll speed for the\n" +
				"drums lanes.\n" +
				"You can set it from x0.1 to x200.0.\n" +
				"(ScrollSpeed=x0.5 means half speed)" ) );
			#endregion
			#region [ 共通 Dark/Risky/PlaySpeed ]
			l.Add( new CItemInteger( "演奏速度", 5, 400, TJAPlayer3.ConfigIni.n演奏速度,
				"曲の演奏速度を、速くしたり遅くした\n" +
				"りすることができます。\n" +
				"（※一部のサウンドカードでは正しく\n" +
				"再生できない可能性があります。）",
				"It changes the song speed.\n" +
				"For example, you can play in half\n" +
				" speed by setting PlaySpeed = 0.500\n" +
				" for your practice.\n" +
				"Note: It also changes the songs' pitch." ) );
			#endregion
			#region [ 個別 Sud/Hid ]
			l.Add( new CItemList( "ランダム", CItemBase.Eパネル種別.通常, (int) TJAPlayer3.ConfigIni.eRandom[nPlayer],
				"いわゆるランダム。\n  RANDOM: ちょっと変わる\n  MIRROR: あべこべ \n  SUPER: そこそこヤバい\n  HYPER: 結構ヤバい\nなお、実装は適当な模様",
				"Drums chips come randomly.\n\n Part: swapping lanes randomly for each\n  measures.\n Super: swapping chip randomly\n Hyper: swapping randomly\n  (number of lanes also changes)",
				new string[] { "OFF", "RANDOM", "あべこべ", "SUPER", "HYPER" } ) );
			l.Add( new CItemList( "ドロン", CItemBase.Eパネル種別.通常, (int) TJAPlayer3.ConfigIni.eSTEALTH[nPlayer],
				"",
				new string[] { "OFF", "ドロン", "ステルス" } ) );
			l.Add( new CItemList( "ゲーム", CItemBase.Eパネル種別.通常, (int)TJAPlayer3.ConfigIni.eGameMode,
				"ゲームモード\n" +
				"TYPE-A: 完走!叩ききりまショー!\n" +
				"TYPE-B: 完走!叩ききりまショー!(激辛)\n" +
				" \n",
				" \n" +
				" \n" +
				" ",
				new string[] { "OFF", "完走!", "完走!激辛", "特訓" }) );

			l.Add(new CItemList("真打", CItemBase.Eパネル種別.通常, TJAPlayer3.ConfigIni.ShinuchiMode[nPlayer] ? 1 : 0, "", "", new string[] { "OFF", "ON" }));

			#endregion
			#region [ 共通 SET切り替え/More/Return ]
			l.Add( new CSwitchItemList( "More...", CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "" } ) );
			l.Add( new CSwitchItemList( "戻る", CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "", "" } ) );
			#endregion

			return l;
		}

		// メソッド
		public override void tActivatePopupMenu(int nPlayer)
		{
			this.CActSelectQuickConfigMain(nPlayer);
			base.tActivatePopupMenu(nPlayer);
		}

		//public void tDeativatePopupMenu()
		//{
		//	base.tDeativatePopupMenu();
		//}

		public override void t進行描画sub()
		{

		}

		public override void tEnter押下Main( int nSortOrder )
		{
			switch ( n現在の選択行 )
			{
				case (int) EOrder.ScrollSpeed:
					TJAPlayer3.ConfigIni.n譜面スクロール速度[nPlayer] = (int) GetObj現在値( (int) EOrder.ScrollSpeed );
					break;
				case (int) EOrder.PlaySpeed:
					TJAPlayer3.ConfigIni.n演奏速度 = (int) GetObj現在値( (int) EOrder.PlaySpeed );
					break;
				case (int) EOrder.Random:
					TJAPlayer3.ConfigIni.eRandom[nPlayer] = (Eランダムモード)GetIndex( (int)EOrder.Random );
					break;
				case (int) EOrder.Stealth:
					TJAPlayer3.ConfigIni.eSTEALTH[nPlayer] = (Eステルスモード)GetIndex( (int)EOrder.Stealth );
					break;
				case (int) EOrder.GameMode:
					EGame game = EGame.OFF;
					switch( (int) GetIndex( (int) EOrder.GameMode ) )
					{
						case 0: game = EGame.OFF; break;
						case 1: game = EGame.完走叩ききりまショー; break;
						case 2: game = EGame.完走叩ききりまショー激辛; break;
						case 3: game = EGame.特訓モード; break;
					}
					TJAPlayer3.ConfigIni.eGameMode = game;
					break;
				case (int)EOrder.ShinuchiMode:
					TJAPlayer3.ConfigIni.ShinuchiMode[nPlayer] = !TJAPlayer3.ConfigIni.ShinuchiMode[nPlayer];
					break;
				case (int) EOrder.More:
					this.bGotoDetailConfig = true;
					this.tDeativatePopupMenu();
					break;

				case (int) EOrder.Return:
					this.tDeativatePopupMenu();
					break;
				default:
					break;
			}
		}

		public override void tCancel()
		{
		}

		/// <summary>
		/// 1つの値を、全targetに適用する。RiskyやDarkなど、全tatgetで共通の設定となるもので使う。
		/// </summary>
		/// <param name="order">設定項目リストの順番</param>
		/// <param name="index">設定値(index)</param>
		private void SetValueToAllTarget( int order, int index )
		{
			lci[ nCurrentConfigSet ][ order ].SetIndex( index );
		}

		// CActivity 実装

		public override void On活性化()
		{
			this.ft表示用フォント = new Font( "Arial", 26f, FontStyle.Bold, GraphicsUnit.Pixel );
			base.On活性化();
			this.bGotoDetailConfig = false;
		}
		public override void On非活性化()
		{
			if ( this.ft表示用フォント != null )
			{
				this.ft表示用フォント.Dispose();
				this.ft表示用フォント = null;
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				//string pathパネル本体 = CSkin.Path( @"Graphics\ScreenSelect popup auto settings.png" );
				//if ( File.Exists( pathパネル本体 ) )
				//{
				//	this.txパネル本体 = CDTXMania.tテクスチャの生成( pathパネル本体, true );
				//}

				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if ( !base.b活性化してない )
			{
				//CDTXMania.tテクスチャの解放( ref this.txパネル本体 );
				TJAPlayer3.tテクスチャの解放( ref this.tx文字列パネル );
				base.OnManagedリソースの解放();
			}
		}

		#region [ private ]
		//-----------------
		private int nCurrentConfigSet = 0;
		private List<List<CItemBase>> lci;		// DrGtBs, ConfSet, 選択肢一覧。都合、3次のListとなる。
		private enum EOrder : int
		{
			ScrollSpeed = 0,
			PlaySpeed,
			Random,
			Stealth,
			GameMode,
			ShinuchiMode,
			More,
			Return, END,
			Default = 99
		};

		private Font ft表示用フォント;
		//private CTexture txパネル本体;
		private CTexture tx文字列パネル;
		private CTexture tx説明文1;
		private int nPlayer;
		//-----------------
		#endregion
	}


}
