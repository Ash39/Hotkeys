﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Hotkeys {
    public sealed class HotKey : IDisposable {
        private readonly IntPtr _handle;

        private readonly int _id;

        private bool _isKeyRegistered;

        public HotKey(ModifierKeys modifierKeys, Key key, Window window, Action<HotKey> OnHotKeyPressed)
            : this(modifierKeys, key, new WindowInteropHelper(window), OnHotKeyPressed) {
        }

        public HotKey(ModifierKeys modifierKeys, Key key, WindowInteropHelper window, Action<HotKey> OnHotKeyPressed)
            : this(modifierKeys, key, window.Handle, OnHotKeyPressed) {
        }

        public HotKey(ModifierKeys modifierKeys, Key key, IntPtr windowHandle, Action<HotKey> OnHotKeyPressed) {
            Key = key;
            KeyModifier = modifierKeys;
            _id = GetHashCode();
            _handle = windowHandle;
            RegisterHotKey();
            ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessageMethod;

            OnHotKeyPressed += OnHotKeyPressed;
        }

        ~HotKey() {
            Dispose();
        }

        public event Action<HotKey> HotKeyPressed;

        public Key Key { get; private set; }

        public ModifierKeys KeyModifier { get; private set; }

        private int InteropKey {
            get {
                return KeyInterop.VirtualKeyFromKey(Key);
            }
        }

        public void Dispose() {
            ComponentDispatcher.ThreadPreprocessMessage -= ThreadPreprocessMessageMethod;
            UnregisterHotKey();
        }

        private void OnHotKeyPressed() {
            HotKeyPressed?.Invoke(this);
        }

        private void RegisterHotKey() {
            if(Key == Key.None) {
                return;
            }

            if(_isKeyRegistered) {
                UnregisterHotKey();
            }

            _isKeyRegistered = HotKeyWinApi.RegisterHotKey(_handle, _id, KeyModifier, InteropKey);

            if(!_isKeyRegistered) {
                throw new ApplicationException("Hotkey already in use");
            }
        }

        private void ThreadPreprocessMessageMethod(ref MSG msg, ref bool handled) {
            if(handled) {
                return;
            }

            if(msg.message != HotKeyWinApi.WmHotKey || (int)(msg.wParam) != _id) {
                return;
            }

            OnHotKeyPressed();
            handled = true;
        }

        private void UnregisterHotKey() {
            _isKeyRegistered = !HotKeyWinApi.UnregisterHotKey(_handle, _id);
        }
    }
}
