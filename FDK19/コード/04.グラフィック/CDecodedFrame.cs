using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDK
{
	public class CDecodedFrame : IDisposable
	{
		public double Time;
		public CTexture Tex;

		public void Dispose() 
		{
			Tex.Dispose();
		}
	}
}
