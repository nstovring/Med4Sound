using System.Collections;
using RootSystem = System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Windows.Kinect
{
    //
    // Windows.Kinect.AudioBeamFrameList
    //
    public sealed partial class AudioBeamFrameList : IList<AudioBeamFrame>, RootSystem.IDisposable, Helper.INativeWrapper

    {
        internal RootSystem.IntPtr _pNative;
        RootSystem.IntPtr Helper.INativeWrapper.nativePtr { get { return _pNative; } }

        // Constructors and Finalizers
        internal AudioBeamFrameList(RootSystem.IntPtr pNative)
        {
            _pNative = pNative;
            Windows_Kinect_AudioBeamFrameList_AddRefObject(ref _pNative);
        }

        public IEnumerator<AudioBeamFrame> GetEnumerator()
        {
            return beamFrames.GetEnumerator();
        }

        ~AudioBeamFrameList()
        {
            Dispose(false);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [RootSystem.Runtime.InteropServices.DllImport("KinectUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern void Windows_Kinect_AudioBeamFrameList_ReleaseObject(ref RootSystem.IntPtr pNative);
        [RootSystem.Runtime.InteropServices.DllImport("KinectUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern void Windows_Kinect_AudioBeamFrameList_AddRefObject(ref RootSystem.IntPtr pNative);
        private void Dispose(bool disposing)
        {
            if (_pNative == RootSystem.IntPtr.Zero)
            {
                return;
            }

            __EventCleanup();

            Helper.NativeObjectCache.RemoveObject<AudioBeamFrameList>(_pNative);

            if (disposing)
            {
                Windows_Kinect_AudioBeamFrameList_Dispose(_pNative);
            }
                Windows_Kinect_AudioBeamFrameList_ReleaseObject(ref _pNative);

            _pNative = RootSystem.IntPtr.Zero;
        }


        // Public Methods
        [RootSystem.Runtime.InteropServices.DllImport("KinectUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern void Windows_Kinect_AudioBeamFrameList_Dispose(RootSystem.IntPtr pNative);
        public void Dispose()
        {
            if (_pNative == RootSystem.IntPtr.Zero)
            {
                Debug.Log("Not being disposed");
                return;
            }
            Debug.Log("Disposing");
            Dispose(true);
            RootSystem.GC.SuppressFinalize(this);
        }

        private void __EventCleanup()
        {
        }
        // Array which contains beamFrames
        private List<AudioBeamFrame> beamFrames;
        //Bonus code from added interface

        public void Add(AudioBeamFrame item)
        {
            beamFrames.Add(item);
        }

        public void Clear()
        {
            beamFrames.Clear();
        }

        public bool Contains(AudioBeamFrame item)
        {
            return beamFrames.Contains(item);
            //throw new System.NotImplementedException();
        }

        public void CopyTo(AudioBeamFrame[] array, int arrayIndex)
        {
            beamFrames.CopyTo(array, arrayIndex);
        }

        public bool Remove(AudioBeamFrame item)
        {
            return beamFrames.Remove(item);
        }

        public int Count
        {
            get { return beamFrames.Count; } 
        }

        public bool IsReadOnly {
            get { return false;}
        }
        public int IndexOf(AudioBeamFrame item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, AudioBeamFrame item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            beamFrames.RemoveAt(index);
        }

        public AudioBeamFrame this[int index]
        {
            get { return beamFrames[index]; }
            set { beamFrames[index] = value; }
        }
    }

}
