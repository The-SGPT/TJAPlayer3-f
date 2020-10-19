﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TJAPlayer3.C曲リストノードComparers
{
	internal sealed class C曲リストノードComparerGenre : IComparer<C曲リストノード>
	{
		public C曲リストノードComparerGenre(int order)
		{
			this.order = order;
		}

		public int Compare(C曲リストノード n1, C曲リストノード n2)
		{
			if (order == 1)
			{
				//AC8_14
				return CStrジャンルtoNum.ForAC8_14(n1.strジャンル).CompareTo(CStrジャンルtoNum.ForAC8_14(n2.strジャンル));
			}
			else
			{
				//AC15
				return CStrジャンルtoNum.ForAC15(n1.strジャンル).CompareTo(CStrジャンルtoNum.ForAC15(n2.strジャンル));
			}
		}

		private int order;
	}
}