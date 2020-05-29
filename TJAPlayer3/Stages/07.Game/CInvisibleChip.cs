using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using FDK;

namespace TJAPlayer3
{
	public class CInvisibleChip : IDisposable
	{
		/// <summary>ミス後表示する時間(ms)</summary>
		public int nDisplayTimeMs
		{
			get;
			set;
		}
		/// <summary>表示期間終了後、フェードアウトする時間</summary>
		public int nFadeoutTimeMs
		{
			get;
			set;
		}



		#region [ コンストラクタ ]
		public CInvisibleChip()
		{
			Initialize( 3000, 2000 );
		}
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="_dbDisplayTime">ミス時再表示する時間(秒)</param>
		/// <param name="_dbFadeoutTime">再表示後フェードアウトする時間(秒)</param>
		public CInvisibleChip( int _nDisplayTimeMs, int _nFadeoutTimeMs )
		{
			Initialize( _nDisplayTimeMs, _nFadeoutTimeMs );
		}
		private void Initialize( int _nDisplayTimeMs, int _nFadeoutTimeMs )
		{
			nDisplayTimeMs = _nDisplayTimeMs;
			nFadeoutTimeMs = _nFadeoutTimeMs;
			Reset();
		}
		#endregion

		/// <summary>
		/// 内部状態を初期化する
		/// </summary>
		public void Reset()
		{
			b演奏チップが１つでもバーを通過した = false;
			for ( int i = 0; i < 4; i++ )
			{
				ccounter[ i ] = new CCounter();
			}
		}

		/// <summary>
		/// 一時的にチップを表示するモードを開始する
		/// </summary>
		/// <param name="eInst">楽器パート</param>
		public void ShowChipTemporally( E楽器パート eInst )
		{
			ccounter[ (int) eInst ].t開始( 0, nDisplayTimeMs + nFadeoutTimeMs + 1, 1, TJAPlayer3.Timer );
		}

		/// <summary>
		/// チップの表示/非表示の状態
		/// </summary>
		public enum EChipInvisibleState
		{
			SHOW,			// Missなどしてチップを表示中
			FADEOUT,		// 表示期間終了後、フェードアウト中
			INVISIBLE		// 完全非表示
		}
				
		#region [ Dispose-Finalize パターン実装 ]
		//-----------------
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}
		protected void Dispose( bool disposeManagedObjects )
		{
			if( this.bDispose完了済み )
				return;

			if( disposeManagedObjects )
			{
				// (A) Managed リソースの解放
				for ( int i = 0; i < 4; i++ )
				{
					// ctInvisibleTimer[ i ].Dispose();
					ccounter[ i ].t停止();
					ccounter[ i ] = null;
				}
			}

			// (B) Unamanaged リソースの解放

			this.bDispose完了済み = true;
		}
		~CInvisibleChip()
		{
			this.Dispose( false );
		}
		//-----------------
		#endregion

		private STDGBVALUE<CCounter> ccounter;
		private bool bDispose完了済み = false;
		private bool b演奏チップが１つでもバーを通過した;
	}
}
