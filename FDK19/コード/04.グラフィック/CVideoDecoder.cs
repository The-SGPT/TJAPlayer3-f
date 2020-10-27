﻿using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using SharpDX;
using SharpDX.Direct3D9;
using System.Threading;

namespace FDK
{
	public unsafe class CVideoDecoder : IDisposable
	{
		public CVideoDecoder(Device device, string filename)
		{
			this.device = device;
			if (!File.Exists(filename))
				throw new FileNotFoundException(filename + " not found...");

			format_context = ffmpeg.avformat_alloc_context();
			fixed (AVFormatContext** format_contexttmp = &format_context)
			{
				if (ffmpeg.avformat_open_input(format_contexttmp, filename, null, null) != 0)
					throw new FileLoadException("avformat_open_input failed\n");

				if (ffmpeg.avformat_find_stream_info(*format_contexttmp, null) < 0)
					throw new FileLoadException("avformat_find_stream_info failed\n");

				// find audio stream
				for (int i = 0; i < (int)format_context->nb_streams; i++)
				{
					if (format_context->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
					{
						video_stream = format_context->streams[i];
						break;
					}
				}
				if (video_stream == null)
					throw new FileLoadException("No video stream ...\n");

				// find decoder
				AVCodec* codec = ffmpeg.avcodec_find_decoder(video_stream->codecpar->codec_id);
				if (codec == null)
					throw new NotSupportedException("No supported decoder ...\n");

				codec_context = ffmpeg.avcodec_alloc_context3(codec);

				if (ffmpeg.avcodec_parameters_to_context(codec_context, video_stream->codecpar) < 0)
					Trace.WriteLine("avcodec_parameters_to_context failed\n");

				if (ffmpeg.avcodec_open2(codec_context, codec, null) != 0)
					Trace.WriteLine("avcodec_open2 failed\n");

				this.FrameSize = new Size(codec_context->width, codec_context->height);
				this.Duration = (video_stream->avg_frame_rate.num / (double)video_stream->avg_frame_rate.den) * video_stream->nb_frames;
				this.Framerate = video_stream->avg_frame_rate;

				frame = ffmpeg.av_frame_alloc();
				if (codec_context->pix_fmt != CVPxfmt)
				{
					convert_context = ffmpeg.sws_getContext(
						FrameSize.Width,
						FrameSize.Height,
						codec_context->pix_fmt,
						FrameSize.Width,
						FrameSize.Height,
						CVPxfmt,
						ffmpeg.SWS_FAST_BILINEAR, null, null, null);
					this.IsConvert = true;
					if (convert_context == null) throw new ApplicationException("Could not initialize the conversion context.\n");
				}
				decodedframes = new ConcurrentQueue<CDecodedFrame>();

				CTimer = new CTimer();

				_convertedFrameBufferPtr = Marshal.AllocHGlobal(ffmpeg.av_image_get_buffer_size(CVPxfmt, codec_context->width, codec_context->height, 1));

				_dstData = new byte_ptrArray4();
				_dstLinesize = new int_array4();
				ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_convertedFrameBufferPtr, CVPxfmt, codec_context->width, codec_context->height, 1);
			}
		}

		public void Dispose()
		{
			close = true;
			cts?.Cancel();
			while (DS != DecodingState.Stopped) ;
			Marshal.FreeHGlobal(_convertedFrameBufferPtr);
			ffmpeg.sws_freeContext(convert_context);
			ffmpeg.av_frame_unref(frame);
			ffmpeg.av_free(frame);

			ffmpeg.avcodec_flush_buffers(codec_context);
			if (ffmpeg.avcodec_close(codec_context) < 0)
				Trace.TraceError("codec context close error.");
			video_stream = null;
			fixed (AVFormatContext** format_contexttmp = &format_context)
			{
				ffmpeg.avformat_close_input(format_contexttmp);
			}
			if (lastTexture != null)
				lastTexture.Dispose();
			while (decodedframes.TryDequeue(out CDecodedFrame frame))
				frame.Dispose();
		}

		public void Start()
		{
			CTimer.tリセット();
			CTimer.t再開();
			this.bPlaying = true;
		}

		public void PauseControl()
		{
			if (this.bPlaying)
			{
				CTimer.t一時停止();
				this.bPlaying = false;
			}
			else
			{
				CTimer.t再開();
				this.bPlaying = true;
			}
		}

		public void Stop()
		{
			cts?.Cancel();
			CTimer.t一時停止();
			this.bPlaying = false;
		}

		public void InitRead()
		{
			if (!bqueueinitialized)
			{
				this.Seek(0);
				bqueueinitialized = true;
			}
			else
				Trace.TraceError("The class has already been initialized.\n");
		}

		public void Seek(long timestampms)
		{
			cts?.Cancel();
			while (DS != DecodingState.Stopped) ;
			if (ffmpeg.av_seek_frame(format_context, video_stream->index, timestampms, ffmpeg.AVSEEK_FLAG_BACKWARD) < 0)
				Trace.TraceError("av_seek_frame failed\n");
			ffmpeg.avcodec_flush_buffers(codec_context);
			CTimer.n現在時刻ms = timestampms;
			while (decodedframes.TryDequeue(out CDecodedFrame frame))
				frame.Dispose();
			this.EnqueueFrames();
			if (lastTexture != null)
				lastTexture.Dispose();
		}

		public void GetNowFrame(ref CTexture Texture)
		{
			if (this.bPlaying && decodedframes.Count != 0)
			{
				CTimer.t更新();
				if (decodedframes.TryPeek(out CDecodedFrame frame))
				{
					while (frame.Time <= (CTimer.n現在時刻ms * _dbPlaySpeed))
					{
						if (decodedframes.TryDequeue(out CDecodedFrame cdecodedframe)) {

							if (decodedframes.Count != 0)
								if (decodedframes.TryPeek(out frame))
									if (frame.Time <= (CTimer.n現在時刻ms * _dbPlaySpeed))
									{
										cdecodedframe.Dispose();
										continue;
									}

							GeneFrmTx(ref lastTexture, cdecodedframe.Bitmap);

							cdecodedframe.Dispose();
						}
						break;
					}
				}

				if (DS == DecodingState.Stopped)
					this.EnqueueFrames();
			}

			if (lastTexture == null)
				GeneFrmTx(ref lastTexture, new Bitmap(FrameSize.Width, FrameSize.Height));

			if (Texture == lastTexture)
				return;

			Texture = lastTexture;

		}

		private void EnqueueFrames()
		{
			if (DS != DecodingState.Running && !close)
			{
				cts = new CancellationTokenSource();
				Task.Factory.StartNew(() => EnqueueOneFrame());
			}
		}

		private void EnqueueOneFrame()
		{
			DS = DecodingState.Running;
			AVPacket* packet = ffmpeg.av_packet_alloc();
			try
			{
				while (true)
				{
					if (cts.IsCancellationRequested || close)
						return;

					//2020/10/27 Mr-Ojii 閾値フレームごとにパケット生成するのは無駄だと感じたので、ループに入ったら、パケット生成し、シークによるキャンセルまたは、EOFまで無限ループ
					if (decodedframes.Count < 5)//現在は適当に5
					{
						int error = ffmpeg.av_read_frame(format_context, packet);

						if (error >= 0)
						{
							if (packet->stream_index == video_stream->index)
							{
								if (ffmpeg.avcodec_send_packet(codec_context, packet) >= 0) 
								{
									do
									{
										error = ffmpeg.avcodec_receive_frame(codec_context, frame);
									} while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

									if (error != 0)
										Trace.TraceError("error.\n");//エラー時はどうしましょ。

									AVFrame* outframe = null;

									if (IsConvert)
									{
										ffmpeg.sws_scale(convert_context, frame->data, frame->linesize, 0, frame->height, _dstData, _dstLinesize);

										outframe = ffmpeg.av_frame_alloc();
										outframe->width = FrameSize.Width;
										outframe->height = FrameSize.Height;
										outframe->data = new byte_ptrArray8();
										outframe->data.UpdateFrom(_dstData);
										outframe->linesize = new int_array8();
										outframe->linesize.UpdateFrom(_dstLinesize);
									}
									else
									{
										outframe = frame;
									}

									decodedframes.Enqueue(new CDecodedFrame() { Time = (frame->best_effort_timestamp - video_stream->start_time) * ((double)video_stream->time_base.num / (double)video_stream->time_base.den) * 1000, Bitmap = getframebuf(outframe) });

									ffmpeg.av_frame_unref(frame);
									ffmpeg.av_frame_free(&outframe);
								}
							}

							//2020/10/27 Mr-Ojii packetが解放されない周回があった問題を修正。
							ffmpeg.av_packet_unref(packet);
						}
						else if (error == ffmpeg.AVERROR_EOF)
						{
							DS = DecodingState.Stopped;
							return;
						}
					}
					else 
					{
						//ポーズ中に無限ループに入り、CPU使用率が異常に高くなってしまうため、1ms待つ。
						//ネットを調べると、await Task.Delay()を使えというお話が出てくるが、unsafeなので、使えない
						Thread.Sleep(1);
					}
				}
			}
			catch (Exception e)
			{
				Trace.TraceError(e.ToString());
				DS = DecodingState.Stopped;
				return;
			}
			finally
			{
				ffmpeg.av_packet_free(&packet);
				DS = DecodingState.Stopped;
			}
		}

		private void GeneFrmTx(ref CTexture tex, Bitmap bitmap)
		{
			tex = new CTexture(this.device, bitmap, false);
		}
		private void GeneFrmTx(ref CTexture tex, byte[] bytearray)
		{
			//ポインタを使用し、データを直接バッファにコピーする
			if (tex == null)
				GeneFrmTx(ref tex, new Bitmap(FrameSize.Width, FrameSize.Height));

			DataRectangle data = tex.texture.LockRectangle(0, LockFlags.Discard);

			if (tex.szテクスチャサイズ.Width == FrameSize.Width)
			{
				Marshal.Copy(bytearray, 0, data.DataPointer, FrameSize.Width * FrameSize.Height * 4);
			}

			tex.texture.UnlockRectangle(0);
		}

		private byte[] getframebuf(AVFrame* frame) 
		{
			byte[] buf = new byte[FrameSize.Width * FrameSize.Height * 4];

			Marshal.Copy((IntPtr)(frame->data[0]), buf, 0, FrameSize.Width * FrameSize.Height * 4);

			return buf;
		}

		public Size FrameSize 
		{
			get;
			private set;
		}
		public double Duration 
		{
			get;
			private set;
		}

		public double dbPlaySpeed
		{
			get
			{
				return this._dbPlaySpeed;
			}
			set
			{
				if (value > 0)
				{
					this._dbPlaySpeed = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		#region[private]
		//for read & decode
		private bool close = false;
		private double _dbPlaySpeed = 1.0f;
		private static AVFormatContext* format_context;
		private AVStream* video_stream;
		private AVCodecContext* codec_context;
		private AVFrame* frame;
		private ConcurrentQueue<CDecodedFrame> decodedframes;
		private Device device;
		private CancellationTokenSource cts;
		private DecodingState DS = DecodingState.Stopped;
		private enum DecodingState
		{
			Stopped,
			Running
		}

		//for play
		private bool bPlaying = false;
		private CTimer CTimer;
		private AVRational Framerate;
		private CTexture lastTexture;
		private bool bqueueinitialized = false;

		//for convert
		private SwsContext* convert_context;
		private readonly byte_ptrArray4 _dstData;
		private readonly int_array4 _dstLinesize;
		private readonly IntPtr _convertedFrameBufferPtr;
		private const AVPixelFormat CVPxfmt = AVPixelFormat.AV_PIX_FMT_BGRA;
		private bool IsConvert = false;
		#endregion
	}
}
