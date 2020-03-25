using Microsoft.MixedReality.WebRTC;
using System;

namespace Captura.Models.WebRTC
{
    public class SceneVideoSource : CustomVideoSource<Argb32VideoFrameStorage>
    {
        /// <summary>
        /// Callback invoked by the base class when the WebRTC track requires a new frame.
        /// </summary>
        /// <param name="request">The frame request to serve.</param>
        protected override void OnFrameRequested(in FrameRequest request)
        {
            // Try to dequeue a frame from the internal frame queue
            if (_frameQueue.TryDequeue(out Argb32VideoFrameStorage storage))
            {
                var frame = new Argb32VideoFrame
                {
                    width = storage.Width,
                    height = storage.Height,
                    stride = (int)storage.Width * 4
                };
                unsafe
                {
                    fixed (void* ptr = storage.Buffer)
                    {
                        // Complete the request with a view over the frame buffer (no allocation)
                        // while the buffer is pinned into memory. The native implementation will
                        // make a copy into a native memory buffer if necessary before returning.
                        frame.data = new IntPtr(ptr);
                        request.CompleteRequest(frame);
                    }
                }

                // Put the allocated buffer back in the pool for reuse
                _frameQueue.RecycleStorage(storage);
            }
        }

        /// <summary>
        /// Callback invoked by the command buffer when the scene frame GPU readback has completed
        /// and the frame is available in CPU memory.
        /// </summary>
        /// <param name="request">The completed and possibly failed GPU readback request.</param>
        public unsafe bool OnFrameReady(byte[] videoBuffer, int width, int height)
        {
            fixed (byte* ptr = videoBuffer)
            {
                // Enqueue a frame in the internal frame queue. This will make a copy
                // of the frame into a pooled buffer owned by the frame queue.
                var frame = new Argb32VideoFrame
                {
                    data = (IntPtr)ptr,
                    stride = width * 4,
                    width = (uint)width,
                    height = (uint)height
                };
                
                return _frameQueue.Enqueue(frame);
            }
        }
    }
}
